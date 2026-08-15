use serde::{Deserialize, Serialize};
use std::{fs, path::PathBuf};
use tauri::Manager;
use tiberius::{AuthMethod, Client, ColumnData, Config, EncryptionLevel, Row};
use tokio::net::TcpStream;
use tokio::time::{timeout, Duration};
use tokio_util::compat::TokioAsyncWriteCompatExt;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OphServer {
    id: String,
    name: String,
    host: String,
    port: u16,
    auth_type: String,
    #[serde(default = "default_database")]
    default_database: String,
    username: Option<String>,
    password: Option<String>,
    trust_server_certificate: Option<bool>,
    encrypt: Option<bool>,
    status: String,
    databases: u32,
    last_checked: String,
}

fn default_database() -> String {
    "oph_core".to_string()
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OphConnectionConfig {
    servers: Vec<OphServer>,
    selected_server_id: Option<String>,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
struct TestConnectionResult {
    success: bool,
    message: String,
    server_name: String,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
struct OphDatabase {
    id: String,
    name: String,
    database_name: String,
    server_id: String,
    r#type: String,
    status: String,
    modules: u32,
    size: String,
    updated_at: String,
}

fn connection_config_path(app: &tauri::AppHandle) -> Result<PathBuf, String> {
    let config_dir = app
        .path()
        .app_config_dir()
        .map_err(|error| format!("Cannot resolve app config directory: {error}"))?;

    Ok(config_dir.join("connection-config.json"))
}

#[tauri::command]
fn load_connection_config(app: tauri::AppHandle) -> Result<Option<OphConnectionConfig>, String> {
    let config_path = connection_config_path(&app)?;

    if !config_path.exists() {
        return Ok(None);
    }

    let raw_config = fs::read_to_string(&config_path)
        .map_err(|error| format!("Cannot read connection config: {error}"))?;
    let config = serde_json::from_str::<OphConnectionConfig>(&raw_config)
        .map_err(|error| format!("Cannot parse connection config: {error}"))?;

    if config.servers.is_empty() {
        Ok(None)
    } else {
        Ok(Some(config))
    }
}

fn sql_config(server: &OphServer) -> Result<Config, String> {
    if server.host.trim().is_empty() {
        return Err("Enter a server host before testing the connection.".to_string());
    }

    if server.auth_type == "windows" {
        return Err("Windows Authentication is not implemented yet. Use SQL Login for the current connection test.".to_string());
    }

    let username = server
        .username
        .as_deref()
        .filter(|value| !value.trim().is_empty())
        .ok_or_else(|| "Enter a username before testing the connection.".to_string())?;
    let password = server.password.as_deref().unwrap_or_default();

    let mut config = Config::new();
    config.host(server.host.trim());
    config.port(server.port);
    let database = if server.default_database.trim().is_empty() {
        "oph_core"
    } else {
        server.default_database.trim()
    };

    config.database(database);
    config.authentication(AuthMethod::sql_server(username, password));

    if server.trust_server_certificate.unwrap_or(false) {
        config.trust_cert();
    }

    if server.encrypt.unwrap_or(false) {
        config.encryption(EncryptionLevel::Required);
    } else {
        config.encryption(EncryptionLevel::Off);
    }

    Ok(config)
}

async fn connect_sql_server(server: &OphServer) -> Result<Client<tokio_util::compat::Compat<TcpStream>>, String> {
    let config = sql_config(server)?;
    let tcp = TcpStream::connect(config.get_addr())
        .await
        .map_err(|error| format!("Cannot reach SQL Server {}:{}: {error}", server.host, server.port))?;

    tcp.set_nodelay(true)
        .map_err(|error| format!("Cannot prepare SQL Server connection: {error}"))?;

    let client = Client::connect(config, tcp.compat_write())
        .await
        .map_err(|error| format!("SQL Server login failed: {error}"))?;

    Ok(client)
}

async fn test_sql_server_connection(server: &OphServer) -> Result<TestConnectionResult, String> {
    let mut client = connect_sql_server(server).await?;

    client
        .simple_query("select 1")
        .await
        .map_err(|error| format!("SQL Server validation query failed: {error}"))?;

    Ok(TestConnectionResult {
        success: true,
        message: "Connection successful.".to_string(),
        server_name: server.name.clone(),
    })
}

async fn connect_sql_server_database(
    server: &OphServer,
    database_name: &str,
) -> Result<Client<tokio_util::compat::Compat<TcpStream>>, String> {
    let mut server_for_database = server.clone();
    server_for_database.default_database = database_name.to_string();
    connect_sql_server(&server_for_database).await
}

fn selected_server(config: &OphConnectionConfig) -> Result<&OphServer, String> {
    config
        .selected_server_id
        .as_ref()
        .and_then(|server_id| config.servers.iter().find(|server| &server.id == server_id))
        .or_else(|| config.servers.first())
        .ok_or_else(|| "Add at least one server before loading OPH databases.".to_string())
}

fn database_name(server: &OphServer) -> String {
    if server.default_database.trim().is_empty() {
        "oph_core".to_string()
    } else {
        server.default_database.trim().to_string()
    }
}

fn read_string(row: &Row, column: &str) -> String {
    row.try_get::<&str, _>(column)
        .ok()
        .flatten()
        .unwrap_or_default()
        .to_string()
}

fn escape_sql_value(value: &str) -> String {
    value.replace('\'', "''")
}

fn valid_database_name(value: &str) -> bool {
    !value.is_empty()
        && value.len() <= 128
        && value.chars().all(|character| character.is_ascii_alphanumeric() || character == '_')
}

fn valid_s3_configuration(access_key: &str, access_secret: &str, bucket: &str, require_host: Option<&str>) -> bool {
    !access_key.trim().is_empty()
        && !access_secret.trim().is_empty()
        && !bucket.trim().is_empty()
        && require_host.is_none_or(|host| !host.trim().is_empty())
}

fn quote_sql_identifier(value: &str) -> String {
    format!("[{}]", value.replace(']', "]]"))
}

fn json_field(row: &serde_json::Value, field: &str) -> String {
    row.as_object()
        .and_then(|object| {
            object
                .iter()
                .find(|(key, _)| key.eq_ignore_ascii_case(field))
                .map(|(_, value)| value)
        })
        .map(|value| match value {
            serde_json::Value::Null => String::new(),
            serde_json::Value::String(text) => text.clone(),
            serde_json::Value::Bool(flag) => flag.to_string(),
            serde_json::Value::Number(number) => number.to_string(),
            _ => value.to_string(),
        })
        .unwrap_or_default()
}

fn draft_field(row: &serde_json::Value, column: &str) -> String {
    match column {
        "expirypwd" => {
            let value = json_field(row, "expirypwd");
            if value.is_empty() { json_field(row, "expirydate") } else { value }
        }
        _ => json_field(row, column),
    }
}

fn draft_sql_value(row: &serde_json::Value, column: &str) -> String {
    let value = draft_field(row, column);
    if column.ends_with("guid") && value.trim().is_empty() {
        "null".to_string()
    } else {
        format!("N'{}'", escape_sql_value(&value))
    }
}

#[derive(Clone)]
struct CrudMapping {
    table_name: &'static str,
    key_column: &'static str,
    key_field: &'static str,
    parent_column: Option<&'static str>,
    parent_context: Option<&'static str>,
    writable_columns: &'static [&'static str],
}

struct CopyMapping {
    table_name: &'static str,
    key_column: &'static str,
    parent_column: &'static str,
    duplicate_columns: &'static [&'static str],
    target_kind: &'static str,
}

fn copy_mapping(source_table: &str) -> Option<CopyMapping> {
    match source_table.to_lowercase().as_str() {
        "modl" => Some(CopyMapping {
            table_name: "modl",
            key_column: "moduleguid",
            parent_column: "parentmoduleguid",
            duplicate_columns: &["moduleid"],
            target_kind: "module",
        }),
        "modlinfo" => Some(CopyMapping {
            table_name: "modlinfo",
            key_column: "moduleinfoguid",
            parent_column: "moduleguid",
            duplicate_columns: &["infokey"],
            target_kind: "module",
        }),
        "modlcolm" => Some(CopyMapping {
            table_name: "modlcolm",
            key_column: "columnguid",
            parent_column: "moduleguid",
            duplicate_columns: &["colkey"],
            target_kind: "module",
        }),
        "modlcolminfo" => Some(CopyMapping {
            table_name: "modlcolminfo",
            key_column: "columninfoguid",
            parent_column: "columnguid",
            duplicate_columns: &["infokey"],
            target_kind: "column",
        }),
        "modlappr" => Some(CopyMapping {
            table_name: "modlappr",
            key_column: "approvalguid",
            parent_column: "moduleguid",
            duplicate_columns: &["approvalgroupguid", "lvl"],
            target_kind: "module",
        }),
        "modldocn" => Some(CopyMapping {
            table_name: "modldocn",
            key_column: "docnumberguid",
            parent_column: "moduleguid",
            duplicate_columns: &["format", "month"],
            target_kind: "module",
        }),
        "modlmail" => Some(CopyMapping {
            table_name: "modlmail",
            key_column: "modulemailguid",
            parent_column: "moduleguid",
            duplicate_columns: &["mailguid", "actionguid"],
            target_kind: "module",
        }),
        _ => None,
    }
}

fn crud_mapping(source_table: &str) -> Option<CrudMapping> {
    match source_table.to_lowercase().as_str() {
        "[user]" => Some(CrudMapping {
            table_name: "[user]",
            key_column: "userguid",
            key_field: "userguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["userid", "username", "email", "expirypwd"],
        }),
        "acctinfo" => Some(CrudMapping {
            table_name: "acctinfo",
            key_column: "accountinfoguid",
            key_field: "accountinfoguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["infokey", "infovalue"],
        }),
        "acct" => Some(CrudMapping {
            table_name: "acct",
            key_column: "accountguid",
            key_field: "accountguid",
            parent_column: Some("parentaccountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["accountid"],
        }),
        "acctdbse" => Some(CrudMapping {
            table_name: "acctdbse",
            key_column: "accountdbguid",
            key_field: "accountdbguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["databasename", "ismaster", "version"],
        }),
        "msta" => Some(CrudMapping {
            table_name: "msta",
            key_column: "modulestatusguid",
            key_field: "modulestatusguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["modulestatusname", "modulestatusdescription"],
        }),
        "mstastat" => Some(CrudMapping {
            table_name: "mstastat",
            key_column: "modulestatusdetailguid",
            key_field: "modulestatusdetailguid",
            parent_column: Some("modulestatusguid"),
            parent_context: Some("moduleStatusGuid"),
            writable_columns: &["stateid", "statecode", "statename", "statedesc", "isdefault"],
        }),
        "modg" => Some(CrudMapping {
            table_name: "modg",
            key_column: "modulegroupguid",
            key_field: "modulegroupguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["modulegroupid", "modulegroupname", "modulegroupdescription"],
        }),
        "modginfo" => Some(CrudMapping {
            table_name: "modginfo",
            key_column: "envinfoguid",
            key_field: "envinfoguid",
            parent_column: Some("modulegroupguid"),
            parent_context: Some("moduleGroupGuid"),
            writable_columns: &["infokey", "infovalue"],
        }),
        "menusmnu" => Some(CrudMapping {
            table_name: "menusmnu",
            key_column: "menudetailguid",
            key_field: "menudetailguid",
            parent_column: Some("menuguid"),
            parent_context: Some("menuGuid"),
            writable_columns: &["submenudescription", "tag", "url", "orderno", "caption", "type", "uppersubmenuguid", "icon_fa", "icon_url"],
        }),
        "para" => Some(CrudMapping {
            table_name: "para",
            key_column: "parameterguid",
            key_field: "parameterguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["parameterid", "parameterdescription"],
        }),
        "paravalu" => Some(CrudMapping {
            table_name: "paravalu",
            key_column: "parametervalueguid",
            key_field: "parametervalueguid",
            parent_column: Some("parameterguid"),
            parent_context: Some("parameterGuid"),
            writable_columns: &["parametervalue", "parameterdescription"],
        }),
        "widg" => Some(CrudMapping {
            table_name: "widg",
            key_column: "widgetguid",
            key_field: "widgetguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["widgetid", "widgetdescription", "sqlstr"],
        }),
        "mail" => Some(CrudMapping {
            table_name: "mail",
            key_column: "mailguid",
            key_field: "mailguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["profilename", "accountname", "displayname", "emailaddress", "bcc"],
        }),
        "word" => Some(CrudMapping {
            table_name: "word",
            key_column: "wordguid",
            key_field: "wordguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["originstatements"],
        }),
        "modlinfo" => Some(CrudMapping {
            table_name: "modlinfo",
            key_column: "moduleinfoguid",
            key_field: "moduleinfoguid",
            parent_column: Some("moduleguid"),
            parent_context: Some("moduleGuid"),
            writable_columns: &["infokey", "infovalue"],
        }),
        "modlcolminfo" => Some(CrudMapping {
            table_name: "modlcolminfo",
            key_column: "columninfoguid",
            key_field: "columninfoguid",
            parent_column: Some("columnguid"),
            parent_context: Some("columnGuid"),
            writable_columns: &["infokey", "infovalue"],
        }),
        "modlcolm" => Some(CrudMapping {
            table_name: "modlcolm",
            key_column: "columnguid",
            key_field: "columnguid",
            parent_column: Some("moduleguid"),
            parent_context: Some("moduleGuid"),
            writable_columns: &["colkey", "coltype", "titlecaption", "colorder", "collength"],
        }),
        "modlappr" => Some(CrudMapping {
            table_name: "modlappr",
            key_column: "approvalguid",
            key_field: "approvalguid",
            parent_column: Some("moduleguid"),
            parent_context: Some("moduleGuid"),
            writable_columns: &["approvalgroupguid", "uppergroupguid", "lvl", "sqlfilter", "zonegroup"],
        }),
        "modldocn" => Some(CrudMapping {
            table_name: "modldocn",
            key_column: "docnumberguid",
            key_field: "docnumberguid",
            parent_column: Some("moduleguid"),
            parent_context: Some("moduleGuid"),
            writable_columns: &["format", "month", "no"],
        }),
        "modlmail" => Some(CrudMapping {
            table_name: "modlmail",
            key_column: "modulemailguid",
            key_field: "modulemailguid",
            parent_column: Some("moduleguid"),
            parent_context: Some("moduleGuid"),
            writable_columns: &[
                "mailguid",
                "actionguid",
                "tokenstatus",
                "additional",
                "cc",
                "subject",
                "body",
                "reportattachment",
                "definedtable",
            ],
        }),
        "modl" => Some(CrudMapping {
            table_name: "modl",
            key_column: "moduleguid",
            key_field: "moduleguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &[
                "moduleid",
                "moduledescription",
                "settingmode",
                "parentmoduleguid",
                "accountdbguid",
                "orderno",
                "needlogin",
                "themepageguid",
                "modulestatusguid",
                "modulegroupguid",
            ],
        }),
        "menu" => Some(CrudMapping {
            table_name: "menu",
            key_column: "menuguid",
            key_field: "menuid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["menucode", "menudescription"],
        }),
        "thme" => Some(CrudMapping {
            table_name: "thme",
            key_column: "themeguid",
            key_field: "themeguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &["themecode", "themename", "themefolder"],
        }),
        "thmepage" => Some(CrudMapping {
            table_name: "thmepage",
            key_column: "themepageguid",
            key_field: "themepageguid",
            parent_column: Some("themeguid"),
            parent_context: Some("themeGuid"),
            writable_columns: &["pageurl", "isdefault"],
        }),
        "userinfo" => Some(CrudMapping {
            table_name: "userinfo",
            key_column: "userinfoguid",
            key_field: "userinfoguid",
            parent_column: Some("userguid"),
            parent_context: Some("userGuid"),
            writable_columns: &["infokey", "infovalue"],
        }),
        "ugrp" => Some(CrudMapping {
            table_name: "ugrp",
            key_column: "ugroupguid",
            key_field: "ugroupguid",
            parent_column: Some("accountguid"),
            parent_context: Some("accountId"),
            writable_columns: &[
                "groupid",
                "groupdescription",
                "allexceptuser",
                "tokenuser",
                "allexceptenv",
                "tokenenv",
                "allexceptmodule",
            ],
        }),
        "ugrpmodl" => Some(CrudMapping {
            table_name: "ugrpmodl",
            key_column: "accessguid",
            key_field: "accessguid",
            parent_column: Some("ugroupguid"),
            parent_context: Some("userGroupGuid"),
            writable_columns: &[
                "moduleguid",
                "allowaccess",
                "allowadd",
                "allowedit",
                "allowdelete",
                "allowforce",
                "allowwipe",
            ],
        }),
        _ => None,
    }
}

async fn query_json(client: &mut Client<tokio_util::compat::Compat<TcpStream>>, sql: String) -> Result<Vec<serde_json::Value>, String> {
    let rows = client
        .query(sql, &[])
        .await
        .map_err(|error| format!("Cannot run metadata query: {error}"))?
        .into_first_result()
        .await
        .map_err(|error| format!("Cannot load metadata rows: {error}"))?;

    let raw_json = rows
        .first()
        .and_then(|row| row.get::<&str, _>("json"))
        .unwrap_or("[]");

    serde_json::from_str::<Vec<serde_json::Value>>(raw_json)
        .map_err(|error| format!("Cannot parse metadata rows: {error}"))
}

fn column_data_json(value: &ColumnData<'_>) -> serde_json::Value {
    match value {
        ColumnData::U8(value) => value.map_or(serde_json::Value::Null, |value| value.into()),
        ColumnData::I16(value) => value.map_or(serde_json::Value::Null, |value| value.into()),
        ColumnData::I32(value) => value.map_or(serde_json::Value::Null, |value| value.into()),
        ColumnData::I64(value) => value.map_or(serde_json::Value::Null, |value| value.into()),
        ColumnData::F32(value) => value.and_then(|value| serde_json::Number::from_f64(value as f64)).map_or(serde_json::Value::Null, serde_json::Value::Number),
        ColumnData::F64(value) => value.and_then(serde_json::Number::from_f64).map_or(serde_json::Value::Null, serde_json::Value::Number),
        ColumnData::Bit(value) => value.map_or(serde_json::Value::Null, serde_json::Value::Bool),
        ColumnData::String(value) => value.as_ref().map_or(serde_json::Value::Null, |value| value.to_string().into()),
        ColumnData::Guid(value) => value.map_or(serde_json::Value::Null, |value| value.to_string().into()),
        ColumnData::Binary(value) => value.as_ref().map_or(serde_json::Value::Null, |bytes| {
            let hex = bytes.iter().map(|byte| format!("{byte:02x}")).collect::<String>();
            format!("0x{hex}").into()
        }),
        ColumnData::Numeric(value) => value.as_ref().map_or(serde_json::Value::Null, |value| value.to_string().into()),
        ColumnData::Xml(value) => value.as_ref().map_or(serde_json::Value::Null, |value| value.to_string().into()),
        ColumnData::DateTime(value) => value.as_ref().map_or(serde_json::Value::Null, |value| format!("{value:?}").into()),
        ColumnData::SmallDateTime(value) => value.as_ref().map_or(serde_json::Value::Null, |value| format!("{value:?}").into()),
        ColumnData::Time(value) => value.as_ref().map_or(serde_json::Value::Null, |value| format!("{value:?}").into()),
        ColumnData::Date(value) => value.as_ref().map_or(serde_json::Value::Null, |value| format!("{value:?}").into()),
        ColumnData::DateTime2(value) => value.as_ref().map_or(serde_json::Value::Null, |value| format!("{value:?}").into()),
        ColumnData::DateTimeOffset(value) => value.as_ref().map_or(serde_json::Value::Null, |value| format!("{value:?}").into()),
    }
}

#[tauri::command]
async fn run_query(
    config: OphConnectionConfig,
    database_name: String,
    sql: String,
) -> Result<Vec<serde_json::Value>, String> {
    let statement = sql.trim().trim_end_matches(';').trim();
    if statement.is_empty() {
        return Err("Enter a SELECT query before running it.".to_string());
    }
    if !statement.to_lowercase().starts_with("select") {
        return Err("Query Page currently supports read-only SELECT statements only.".to_string());
    }
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let rows = client
        .query(statement, &[])
        .await
        .map_err(|error| format!("Cannot run query in {database_name}: {error}"))?
        .into_first_result()
        .await
        .map_err(|error| format!("Cannot load query results from {database_name}: {error}"))?;

    Ok(rows.into_iter().map(|row| {
        let mut result = serde_json::Map::new();
        let mut name_counts = std::collections::HashMap::<String, usize>::new();
        for (index, (column, value)) in row.cells().enumerate() {
            let base_name = if column.name().trim().is_empty() {
                format!("column_{}", index + 1)
            } else {
                column.name().to_string()
            };
            let count = name_counts.entry(base_name.to_lowercase()).or_insert(0);
            *count += 1;
            let name = if *count == 1 { base_name } else { format!("{base_name}_{}", *count) };
            result.insert(name, column_data_json(value));
        }
        serde_json::Value::Object(result)
    }).collect())
}

#[tauri::command]
async fn save_metadata_row(
    config: OphConnectionConfig,
    database_name: String,
    source_table: String,
    original_row: serde_json::Value,
    draft_row: serde_json::Value,
    module_guid: Option<String>,
    column_guid: Option<String>,
    theme_guid: Option<String>,
    user_guid: Option<String>,
    user_group_guid: Option<String>,
    account_id: Option<String>,
    menu_guid: Option<String>,
    parameter_guid: Option<String>,
    module_status_guid: Option<String>,
    module_group_guid: Option<String>,
) -> Result<(), String> {
    let mapping = crud_mapping(&source_table)
        .ok_or_else(|| format!("Save is not supported for {source_table} yet."))?;
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let row_key = json_field(&original_row, mapping.key_field);
    let normalized_source = source_table.to_lowercase();
    if ["modlinfo", "modlcolminfo", "userinfo", "acctinfo"].contains(&normalized_source.as_str()) {
        let info_key = json_field(&draft_row, "infokey");
        if info_key.trim().is_empty() {
            return Err(format!("Cannot save {source_table}: Info Key is required."));
        }
        if normalized_source == "modlinfo"
            && ["dplx", "dplx_rpt"].contains(&info_key.trim().to_lowercase().as_str())
            && json_field(&draft_row, "infovalue").trim().is_empty()
        {
            return Err("Cannot save modlinfo: Info Value must contain the DPLX report XML.".to_string());
        }
    }

    let sql = if row_key.trim().is_empty() {
        let parent_value = match mapping.parent_context {
            Some("moduleGuid") => module_guid.unwrap_or_default(),
            Some("columnGuid") => column_guid.unwrap_or_default(),
            Some("themeGuid") => theme_guid.unwrap_or_default(),
            Some("userGuid") => user_guid.unwrap_or_default(),
            Some("userGroupGuid") => user_group_guid.unwrap_or_default(),
            Some("accountId") => account_id.unwrap_or_default(),
            Some("menuGuid") => menu_guid.unwrap_or_default(),
            Some("parameterGuid") => parameter_guid.unwrap_or_default(),
            Some("moduleStatusGuid") => module_status_guid.unwrap_or_default(),
            Some("moduleGroupGuid") => module_group_guid.unwrap_or_default(),
            _ => String::new(),
        };

        if mapping.parent_column.is_some() && parent_value.trim().is_empty() {
            return Err(format!("Cannot create {source_table}: parent key is missing."));
        }

        let mut insert_columns = vec![mapping.key_column.to_string()];
        let mut insert_values = vec!["newid()".to_string()];

        if let Some(parent_column) = mapping.parent_column {
            insert_columns.push(parent_column.to_string());
            if mapping.parent_context == Some("accountId") {
                insert_values.push(format!(
                    "(select accountguid from acct where accountid = N'{}')",
                    escape_sql_value(&parent_value)
                ));
            } else {
                insert_values.push(format!("N'{}'", escape_sql_value(&parent_value)));
            }
        }

        for column in mapping.writable_columns {
            insert_columns.push(column.to_string());
            insert_values.push(draft_sql_value(&draft_row, column));
        }

        if mapping.table_name == "[user]" {
            insert_columns.push("password".to_string());
            insert_values.push("N''".to_string());
        }

        format!(
            "insert into {} ({}) values ({})",
            mapping.table_name,
            insert_columns.join(", "),
            insert_values.join(", ")
        )
    } else {
        let assignments = mapping
            .writable_columns
            .iter()
            .filter(|column| draft_field(&draft_row, column) != draft_field(&original_row, column))
            .map(|column| {
                format!("{column} = {}", draft_sql_value(&draft_row, column))
            })
            .collect::<Vec<_>>();

        if assignments.is_empty() {
            return Ok(());
        }

        format!(
            "update {} set {} where {} = N'{}'",
            mapping.table_name,
            assignments.join(", "),
            mapping.key_column,
            escape_sql_value(&row_key)
        )
    };

    client
        .simple_query(sql)
        .await
        .map_err(|error| format!("Cannot save {source_table}: {error}"))?;

    Ok(())
}

#[tauri::command]
async fn delete_metadata_row(
    config: OphConnectionConfig,
    database_name: String,
    source_table: String,
    original_row: serde_json::Value,
) -> Result<(), String> {
    let mapping = crud_mapping(&source_table)
        .ok_or_else(|| format!("Delete is not supported for {source_table} yet."))?;
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let row_key = json_field(&original_row, mapping.key_field);

    if row_key.trim().is_empty() {
        return Err(format!("Cannot delete {source_table}: row key is missing."));
    }

    client
        .simple_query(format!(
            "delete from {} where {} = N'{}'",
            mapping.table_name,
            mapping.key_column,
            escape_sql_value(&row_key)
        ))
        .await
        .map_err(|error| format!("Cannot delete {source_table}: {error}"))?;

    Ok(())
}

#[tauri::command]
async fn list_copy_accounts(
    config: OphConnectionConfig,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, "oph_core").await?;

    query_json(
        &mut client,
        r#"
        select (
          select
            a.accountid,
            a.accountguid,
            d.databasename
          from acct a
          inner join acctdbse d on d.accountguid = a.accountguid
            and d.ismaster = 1
            and d.version = '4.0'
          where isnull(a.isdeleted, 0) <> 1
          order by a.accountid
          for json path
        ) as json
        "#
        .to_string(),
    )
    .await
}

#[tauri::command]
async fn list_copy_targets(
    config: OphConnectionConfig,
    database_name: String,
    source_table: String,
    account_id: String,
) -> Result<Vec<serde_json::Value>, String> {
    let mapping = copy_mapping(&source_table)
        .ok_or_else(|| format!("Copy To is not supported for {source_table} yet."))?;
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = escape_sql_value(&account_id);

    let sql = match mapping.target_kind {
        "module" => format!(
            r#"
            select (
              select
                moduleguid as targetguid,
                moduleid as targetlabel,
                moduledescription as targetdescription
              from modl
              where accountguid = (select accountguid from acct where accountid = N'{account_id}')
              order by moduleid
              for json path
            ) as json
            "#
        ),
        "column" => format!(
            r#"
            select (
              select
                c.columnguid as targetguid,
                m.moduleid + N' / ' + c.colkey as targetlabel,
                c.titlecaption as targetdescription
              from modlcolm c
              inner join modl m on m.moduleguid = c.moduleguid
              where m.accountguid = (select accountguid from acct where accountid = N'{account_id}')
              order by m.moduleid, c.colorder, c.colkey
              for json path
            ) as json
            "#
        ),
        _ => return Err(format!("Cannot resolve Copy To targets for {source_table}.")),
    };

    query_json(&mut client, sql).await
}

#[tauri::command]
async fn copy_metadata_rows(
    config: OphConnectionConfig,
    database_name: String,
    source_table: String,
    selected_rows: Vec<serde_json::Value>,
    target_guid: String,
    target_database_name: String,
    target_account_id: String,
    source_account_id: String,
) -> Result<u64, String> {
    let mapping = copy_mapping(&source_table)
        .ok_or_else(|| format!("Copy To is not supported for {source_table} yet."))?;
    if selected_rows.is_empty() {
        return Err("Select at least one row to copy.".to_string());
    }
    if target_guid.trim().is_empty() {
        return Err("Select a destination before copying.".to_string());
    }

    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let target_guid = escape_sql_value(&target_guid);
    let target_account_id = escape_sql_value(&target_account_id);
    let source_database = quote_sql_identifier(&database_name);
    let target_database = quote_sql_identifier(&target_database_name);
    let is_cross_account = !source_account_id.eq_ignore_ascii_case(&target_account_id)
        || !database_name.eq_ignore_ascii_case(&target_database_name);
    if is_cross_account && matches!(mapping.table_name, "modlappr" | "modlmail") {
        return Err(format!(
            "Cross-account Copy To for {} requires approval/mail relation mapping and is not enabled yet.",
            mapping.table_name
        ));
    }
    let duplicate_predicate = mapping
        .duplicate_columns
        .iter()
        .map(|column| format!("(destination.[{column}] = source.[{column}] or (destination.[{column}] is null and source.[{column}] is null))"))
        .collect::<Vec<_>>()
        .join(" and ");
    let mut copied = 0;

    for row in selected_rows {
        let source_guid = json_field(&row, mapping.key_column);
        if source_guid.trim().is_empty() {
            continue;
        }
        let source_guid = escape_sql_value(&source_guid);
        let sql = if mapping.table_name == "modl" {
            format!(
                r#"
                declare @columns nvarchar(max);
                declare @source_columns nvarchar(max);
                select @columns = string_agg(quotename(source_column.name), N',') within group (order by source_column.column_id)
                from {source_db}.sys.columns source_column
                inner join {target_db}.sys.columns target_column on target_column.name = source_column.name
                  and target_column.object_id = object_id(N'{target_db}.dbo.modl')
                where source_column.object_id = object_id(N'{source_db}.dbo.modl')
                  and source_column.name not in (N'moduleguid', N'accountguid', N'parentmoduleguid', N'accountdbguid', N'themepageguid', N'modulestatusguid', N'modulegroupguid')
                  and source_column.is_identity = 0
                  and source_column.is_computed = 0
                  and source_column.system_type_id <> 189;
                select @source_columns = string_agg(N'source.' + quotename(source_column.name), N',') within group (order by source_column.column_id)
                from {source_db}.sys.columns source_column
                inner join {target_db}.sys.columns target_column on target_column.name = source_column.name
                  and target_column.object_id = object_id(N'{target_db}.dbo.modl')
                where source_column.object_id = object_id(N'{source_db}.dbo.modl')
                  and source_column.name not in (N'moduleguid', N'accountguid', N'parentmoduleguid', N'accountdbguid', N'themepageguid', N'modulestatusguid', N'modulegroupguid')
                  and source_column.is_identity = 0
                  and source_column.is_computed = 0
                  and source_column.system_type_id <> 189;

                declare @sql nvarchar(max) = N'
                  insert into {target_db}.dbo.modl
                    (moduleguid, accountguid, parentmoduleguid, accountdbguid, themepageguid, modulestatusguid, modulegroupguid, ' + @columns + N')
                  select
                    newid(),
                    destination_account.accountguid,
                    @target,
                    destination_accountdb.accountdbguid,
                    destination_page.themepageguid,
                    destination_status.modulestatusguid,
                    destination_group.modulegroupguid,
                    ' + @source_columns + N'
                  from {source_db}.dbo.modl source
                  cross apply (
                    select accountguid from {target_db}.dbo.acct where accountid = @target_account_id
                  ) destination_account
                  left join {source_db}.dbo.acctdbse source_accountdb on source_accountdb.accountdbguid = source.accountdbguid
                  outer apply (
                    select top 1 accountdbguid
                    from {target_db}.dbo.acctdbse
                    where accountguid = destination_account.accountguid
                      and version = source_accountdb.version
                      and ismaster = source_accountdb.ismaster
                    order by databasename
                  ) destination_accountdb
                  left join {source_db}.dbo.msta source_status on source_status.modulestatusguid = source.modulestatusguid
                  outer apply (
                    select top 1 modulestatusguid
                    from {target_db}.dbo.msta
                    where accountguid = destination_account.accountguid
                      and modulestatusname = source_status.modulestatusname
                  ) destination_status
                  left join {source_db}.dbo.modg source_group on source_group.modulegroupguid = source.modulegroupguid
                  outer apply (
                    select top 1 modulegroupguid
                    from {target_db}.dbo.modg
                    where accountguid = destination_account.accountguid
                      and modulegroupid = source_group.modulegroupid
                  ) destination_group
                  left join {source_db}.dbo.thmepage source_page on source_page.themepageguid = source.themepageguid
                  left join {source_db}.dbo.thme source_theme on source_theme.themeguid = source_page.themeguid
                  outer apply (
                    select top 1 destination_page_inner.themepageguid
                    from {target_db}.dbo.thmepage destination_page_inner
                    inner join {target_db}.dbo.thme destination_theme on destination_theme.themeguid = destination_page_inner.themeguid
                    where destination_theme.accountguid = destination_account.accountguid
                      and destination_theme.themecode = source_theme.themecode
                      and destination_page_inner.pageurl = source_page.pageurl
                  ) destination_page
                  where source.moduleguid = @source
                    and not exists (
                      select 1 from {target_db}.dbo.modl destination
                      where destination.parentmoduleguid = @target
                        and (destination.moduleid = source.moduleid or (destination.moduleid is null and source.moduleid is null))
                    );';
                exec sp_executesql
                  @sql,
                  N'@target uniqueidentifier, @source uniqueidentifier, @target_account_id nvarchar(255)',
                  @target = N'{target}',
                  @source = N'{source}',
                  @target_account_id = N'{target_account}';
                "#,
                source_db = source_database,
                target_db = target_database,
                target = target_guid,
                source = source_guid,
                target_account = target_account_id,
            )
        } else {
            format!(
            r#"
            declare @columns nvarchar(max);
            select @columns = string_agg(quotename(source_column.name), N',') within group (order by source_column.column_id)
            from {source_db}.sys.columns source_column
            inner join {target_db}.sys.columns target_column on target_column.name = source_column.name
              and target_column.object_id = object_id(N'{target_db}.dbo.{table}')
            where source_column.object_id = object_id(N'{source_db}.dbo.{table}')
              and source_column.name not in (N'{key}', N'{parent}')
              and source_column.is_identity = 0
              and source_column.is_computed = 0
              and source_column.system_type_id <> 189;

            declare @sql nvarchar(max) = N'
              insert into {target_db}.dbo.[{table}] ([{key}], [{parent}], ' + @columns + N')
              select newid(), @target, ' + @columns + N'
              from {source_db}.dbo.[{table}] source
              where source.[{key}] = @source
                and not exists (
                  select 1
                  from {target_db}.dbo.[{table}] destination
                  where destination.[{parent}] = @target
                    and {duplicates}
                );';
            exec sp_executesql
              @sql,
              N'@target uniqueidentifier, @source uniqueidentifier',
              @target = N'{target}',
              @source = N'{source}';
            "#,
            source_db = source_database,
            target_db = target_database,
            table = mapping.table_name,
            key = mapping.key_column,
            parent = mapping.parent_column,
            duplicates = duplicate_predicate,
            target = target_guid,
            source = source_guid,
        )};

        let result = client
            .execute(sql, &[])
            .await
            .map_err(|error| format!("Cannot copy {source_table}: {error}"))?;
        copied += result.total();
    }

    Ok(copied)
}

#[tauri::command]
async fn save_connection_config(
    app: tauri::AppHandle,
    config: OphConnectionConfig,
) -> Result<OphConnectionConfig, String> {
    if config.servers.is_empty() {
        return Err("Add at least one server before saving the connection config.".to_string());
    }

    let selected_server = config
        .selected_server_id
        .as_ref()
        .and_then(|server_id| config.servers.iter().find(|server| &server.id == server_id))
        .or_else(|| config.servers.first())
        .ok_or_else(|| "Add at least one server before saving the connection config.".to_string())?;

    let test_result = test_sql_server_connection(selected_server).await?;
    if !test_result.success {
        return Err(test_result.message);
    }

    let config_path = connection_config_path(&app)?;
    let config_dir = config_path
        .parent()
        .ok_or_else(|| "Cannot resolve connection config directory.".to_string())?;

    fs::create_dir_all(config_dir)
        .map_err(|error| format!("Cannot create config directory: {error}"))?;

    let raw_config = serde_json::to_string_pretty(&config)
        .map_err(|error| format!("Cannot serialize connection config: {error}"))?;

    fs::write(&config_path, raw_config)
        .map_err(|error| format!("Cannot save connection config: {error}"))?;

    Ok(config)
}

#[tauri::command]
fn delete_connection_config(app: tauri::AppHandle) -> Result<(), String> {
    let config_path = connection_config_path(&app)?;

    if config_path.exists() {
        fs::remove_file(&config_path)
            .map_err(|error| format!("Cannot delete connection config: {error}"))?;
    }

    Ok(())
}

#[tauri::command]
async fn test_connection(server: OphServer) -> Result<TestConnectionResult, String> {
    test_sql_server_connection(&server).await
}

#[tauri::command]
async fn add_account(
    config: OphConnectionConfig,
    server_id: String,
    account_id: String,
) -> Result<(), String> {
    let account_id = account_id.trim();
    if account_id.is_empty() {
        return Err("Account ID cannot be empty.".to_string());
    }

    let server = config
        .servers
        .iter()
        .find(|server| server.id == server_id)
        .ok_or_else(|| format!("Cannot add account: server {server_id} was not found."))?;
    let mut client = connect_sql_server_database(server, "oph_core").await?;
    let account_id = escape_sql_value(account_id);

    client
        .execute(
            format!("insert into dbo.acct (accountid) values (N'{account_id}')"),
            &[],
        )
        .await
        .map_err(|error| format!("Cannot add account {account_id} to oph_core: {error}"))?;

    Ok(())
}

#[tauri::command]
async fn delete_account(
    config: OphConnectionConfig,
    server_id: String,
    account_id: String,
) -> Result<(), String> {
    let account_id = account_id.trim();
    if account_id.is_empty() {
        return Err("Account ID cannot be empty.".to_string());
    }

    let server = config
        .servers
        .iter()
        .find(|server| server.id == server_id)
        .ok_or_else(|| format!("Cannot delete account: server {server_id} was not found."))?;
    let mut client = connect_sql_server_database(server, "oph_core").await?;
    let account_id = escape_sql_value(account_id);

    client
        .execute(
            format!(
                "update dbo.acct set isdeleted = 1 where accountid = N'{account_id}' and isnull(isdeleted, 0) <> 1"
            ),
            &[],
        )
        .await
        .map_err(|error| format!("Cannot delete account {account_id} from oph_core: {error}"))?;

    Ok(())
}

#[tauri::command]
async fn list_oph_databases(config: OphConnectionConfig) -> Result<Vec<OphDatabase>, String> {
    let server = selected_server(&config)?;
    let default_database = database_name(server);

    if default_database.eq_ignore_ascii_case("oph_core") {
        let mut client = connect_sql_server(server).await?;
        let rows = client
            .query(
                r#"
                select
                  a.accountid,
                  coalesce(d.databasename, a.accountid) as databasename
                from acct a
                inner join acctdbse d
                  on d.accountguid = a.accountguid
                 and d.ismaster = 1
                 and d.version = '4.0'
                where isnull(a.isdeleted, 0) <> 1
                order by a.accountid
                "#,
                &[],
            )
            .await
            .map_err(|error| format!("Cannot read accounts from oph_core: {error}"))?
            .into_first_result()
            .await
            .map_err(|error| format!("Cannot load OPH account list: {error}"))?;

        let databases = rows
            .iter()
            .map(|row| {
                let account_id = read_string(row, "accountid");
                let database_name = read_string(row, "databasename");
                OphDatabase {
                    id: format!("{}:{}", server.id, account_id),
                    name: account_id,
                    database_name: database_name.clone(),
                    server_id: server.id.clone(),
                    r#type: if database_name.eq_ignore_ascii_case("oph_core") {
                        "core".to_string()
                    } else {
                        "account".to_string()
                    },
                    status: "healthy".to_string(),
                    modules: 0,
                    size: "-".to_string(),
                    updated_at: "Loaded from oph_core".to_string(),
                }
            })
            .collect();

        Ok(databases)
    } else {
        Ok(vec![OphDatabase {
            id: format!("{}:{}", server.id, default_database),
            name: default_database.clone(),
            database_name: default_database,
            server_id: server.id.clone(),
            r#type: "account".to_string(),
            status: "healthy".to_string(),
            modules: 0,
            size: "-".to_string(),
            updated_at: "Default database".to_string(),
        }])
    }
}

#[tauri::command]
async fn list_account_info(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = escape_sql_value(&account_id);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                a.accountid,
                i.accountinfoguid,
                i.accountguid,
                i.infokey,
                i.infovalue
              from acctinfo i
              inner join acct a on a.accountguid = i.accountguid
              where a.accountid = N'{account_id}'
              order by i.infokey
              for json path
            ) as json
        "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_account_databases(
    config: OphConnectionConfig,
    account_id: String,
    _database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, "oph_core").await?;
    let account_id = escape_sql_value(&account_id);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                a.accountid,
                d.accountdbguid,
                d.accountguid,
                d.databasename,
                d.ismaster,
                d.version,
                nullif(stuff((
                  select N'; ' + i.infokey + N': ' +
                    case
                      when lower(i.infokey) like '%secret%'
                        or lower(i.infokey) like '%password%'
                        or lower(i.infokey) like '%accesskey%'
                        or lower(i.infokey) like '%token%'
                      then N'••••••••'
                      else coalesce(i.infovalue, N'')
                    end
                  from acctinfo i
                  where i.accountguid = a.accountguid
                    and (
                      lower(i.infokey) like '%s3%'
                      or lower(i.infokey) like '%backup%'
                    )
                  order by i.infokey
                  for xml path(''), type
                ).value('.', 'nvarchar(max)'), 1, 2, N''), N'') as s3backupinfo
              from acctdbse d
              inner join acct a on a.accountguid = d.accountguid
              where a.accountid = N'{account_id}'
              order by a.accountid, d.databasename
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_database_backups(
    config: OphConnectionConfig,
    _account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, "oph_core").await?;
    let settings = query_json(
        &mut client,
        r#"
            select (
              select selected.infokey, selected.infovalue
              from (
                select
                  i.infokey,
                  i.infovalue,
                  row_number() over (
                    partition by upper(i.infokey)
                    order by case
                      when lower(a.accountid) = 'oph_core' then 0
                      when lower(a.accountid) = 'oph' then 1
                      else 2
                    end
                  ) as preference
                from acctinfo i
                inner join acct a on a.accountguid = i.accountguid
                where upper(i.infokey) in (
                  'BACKUP_ACCESS_ID', 'BACKUP_ACCESS_KEY', 'BACKUP_ACCESS_SECRET',
                  'BACKUP_BUCKET', 'BACKUP_HOST', 'BACKUP_PREFIX'
                )
              ) selected
              where selected.preference = 1
              order by selected.infokey
              for json path
            ) as json
            "#
        .to_string(),
    )
    .await?;

    let setting = |key: &str| -> String {
        settings.iter().find_map(|row| {
            let row_key = row.get("infokey")?.as_str()?;
            row_key.eq_ignore_ascii_case(key).then(|| {
                row.get("infovalue").and_then(|value| value.as_str()).unwrap_or_default().trim().to_string()
            })
        }).unwrap_or_default()
    };

    let access_key = setting("BACKUP_ACCESS_KEY");
    let access_secret = setting("BACKUP_ACCESS_SECRET");
    let bucket = setting("BACKUP_BUCKET");
    let host = setting("BACKUP_HOST");
    let configured_prefix = setting("BACKUP_PREFIX");
    if !valid_s3_configuration(&access_key, &access_secret, &bucket, None) {
        return Err("The shared S3 backup configuration in oph_core.acctinfo is incomplete.".to_string());
    }

    let requested_prefix = configured_prefix
        .replace("{database}", &database_name)
        .replace("{DATABASE}", &database_name);
    let mut command = std::process::Command::new("aws");
    command.args(["s3api", "list-objects-v2", "--bucket", &bucket, "--output", "json"]);
    if !host.is_empty() {
        let endpoint = if host.starts_with("http://") || host.starts_with("https://") {
            host
        } else {
            format!("https://{host}")
        };
        command.args(["--endpoint-url", &endpoint]);
    }
    if !requested_prefix.is_empty() {
        command.args(["--prefix", &requested_prefix]);
    }
    command
        .env("AWS_ACCESS_KEY_ID", access_key)
        .env("AWS_SECRET_ACCESS_KEY", access_secret)
        .env("AWS_DEFAULT_REGION", "us-east-1")
        .env("AWS_EC2_METADATA_DISABLED", "true");

    let output = command.output().map_err(|error| {
        format!("Cannot run the AWS CLI. Install or configure the `aws` command first: {error}")
    })?;
    if !output.status.success() {
        let detail = String::from_utf8_lossy(&output.stderr);
        return Err(format!("Cannot load S3 backups for {database_name}: {}", detail.trim()));
    }

    let response: serde_json::Value = serde_json::from_slice(&output.stdout)
        .map_err(|error| format!("Cannot read the S3 response for {database_name}: {error}"))?;
    let database_filter = database_name.to_lowercase();
    let prefix_has_database = requested_prefix.to_lowercase().contains(&database_filter);
    let backups = response.get("Contents").and_then(|value| value.as_array())
        .into_iter().flatten()
        .filter(|object| {
            prefix_has_database || object.get("Key").and_then(|value| value.as_str())
                .is_some_and(|key| key.to_lowercase().contains(&database_filter))
        })
        .map(|object| serde_json::json!({
            "backupFile": object.get("Key").and_then(|value| value.as_str()).unwrap_or_default(),
            "sizeBytes": object.get("Size").and_then(|value| value.as_u64()).unwrap_or_default(),
            "lastModified": object.get("LastModified").and_then(|value| value.as_str()).unwrap_or_default(),
            "storageClass": object.get("StorageClass").and_then(|value| value.as_str()).unwrap_or_default(),
        }))
        .collect();
    Ok(backups)
}

#[tauri::command]
async fn restore_database_backup(
    config: OphConnectionConfig,
    backup_key: String,
    target_database_name: String,
) -> Result<(), String> {
    if backup_key.trim().is_empty() {
        return Err("Select an S3 backup file to restore.".to_string());
    }
    if !valid_database_name(&target_database_name) {
        return Err("New database name may only contain letters, numbers, and underscores.".to_string());
    }

    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, "oph_core").await?;
    let settings = query_json(
        &mut client,
        r#"
        select (
          select selected.infokey, selected.infovalue
          from (
            select i.infokey, i.infovalue,
              row_number() over (partition by upper(i.infokey) order by case
                when lower(a.accountid) = 'oph_core' then 0
                when lower(a.accountid) = 'oph' then 1 else 2 end) as preference
            from acctinfo i
            inner join acct a on a.accountguid = i.accountguid
            where upper(i.infokey) in ('BACKUP_ACCESS_KEY', 'BACKUP_ACCESS_SECRET', 'BACKUP_BUCKET', 'BACKUP_HOST')
          ) selected
          where selected.preference = 1
          for json path
        ) as json
        "#.to_string(),
    ).await?;
    let setting = |key: &str| -> String {
        settings.iter().find_map(|row| {
            row.get("infokey")?.as_str()?.eq_ignore_ascii_case(key).then(||
                row.get("infovalue").and_then(|value| value.as_str()).unwrap_or_default().trim().to_string()
            )
        }).unwrap_or_default()
    };
    let access_key = setting("BACKUP_ACCESS_KEY");
    let access_secret = setting("BACKUP_ACCESS_SECRET");
    let bucket = setting("BACKUP_BUCKET");
    let host = setting("BACKUP_HOST").trim_end_matches('/').to_string();
    if !valid_s3_configuration(&access_key, &access_secret, &bucket, Some(&host)) {
        return Err("The shared S3 restore configuration in oph_core.acctinfo is incomplete.".to_string());
    }

    let endpoint = host.trim_start_matches("https://").trim_start_matches("http://");
    let credential_url = format!("s3://{endpoint}/{bucket}");
    let object_url = format!("{credential_url}/{}", backup_key.trim_start_matches('/'));
    let credential_url_sql = escape_sql_value(&credential_url);
    let credential_identifier = credential_url.replace(']', "]]" );
    let object_url_sql = escape_sql_value(&object_url);
    let credential_secret_sql = escape_sql_value(&format!("{access_key}:{access_secret}"));
    let target_sql = escape_sql_value(&target_database_name);

    let exists = client.query(
        format!("select name from sys.databases where name = N'{target_sql}'"), &[]
    ).await.map_err(|error| format!("Cannot validate destination database: {error}"))?
        .into_first_result().await.map_err(|error| format!("Cannot validate destination database: {error}"))?;
    if !exists.is_empty() {
        return Err(format!("Database {target_database_name} already exists. Enter a new database name."));
    }

    client.simple_query(format!(
        r#"
        if exists (select 1 from sys.credentials where name = N'{credential_url_sql}')
          alter credential [{credential_identifier}] with identity = 'S3 Access Key', secret = N'{credential_secret_sql}';
        else
          create credential [{credential_identifier}] with identity = 'S3 Access Key', secret = N'{credential_secret_sql}';
        "#
    )).await.map_err(|_| "SQL Server could not configure the S3 restore credential.".to_string())?
      .into_results().await.map_err(|_| "SQL Server could not configure the S3 restore credential.".to_string())?;

    client.simple_query(format!(
        "restore verifyonly from url = N'{object_url_sql}'"
    )).await
      .map_err(|error| format!("Backup verification failed for {backup_key}: {error}"))?
      .into_results().await
      .map_err(|error| format!("Backup verification did not complete for {backup_key}: {error}"))?;

    let files = client.query(
        format!("restore filelistonly from url = N'{object_url_sql}'"), &[]
    ).await.map_err(|error| format!("Cannot inspect backup file {backup_key}: {error}"))?
      .into_first_result().await.map_err(|error| format!("Cannot inspect backup file {backup_key}: {error}"))?;
    if files.is_empty() {
        return Err("The selected backup does not contain restorable database files.".to_string());
    }

    let path_rows = client.query(
        "select convert(nvarchar(4000), serverproperty('InstanceDefaultDataPath')) as datapath, convert(nvarchar(4000), serverproperty('InstanceDefaultLogPath')) as logpath",
        &[],
    ).await.map_err(|error| format!("Cannot read SQL Server storage paths: {error}"))?
      .into_first_result().await.map_err(|error| format!("Cannot read SQL Server storage paths: {error}"))?;
    let path_row = path_rows.first().ok_or_else(|| "SQL Server did not return its storage paths.".to_string())?;
    let data_path = read_string(path_row, "datapath");
    let log_path = read_string(path_row, "logpath");
    if data_path.is_empty() || log_path.is_empty() {
        return Err("SQL Server default data or log path is not configured.".to_string());
    }

    let mut moves = Vec::new();
    let mut data_index = 0usize;
    let mut log_index = 0usize;
    for file in &files {
        let logical_name = escape_sql_value(&read_string(file, "LogicalName"));
        let file_type = read_string(file, "Type");
        let (path, filename) = if file_type.eq_ignore_ascii_case("L") {
            log_index += 1;
            (&log_path, if log_index == 1 { format!("{target_database_name}_log.ldf") } else { format!("{target_database_name}_log{log_index}.ldf") })
        } else {
            data_index += 1;
            (&data_path, if data_index == 1 { format!("{target_database_name}.mdf") } else { format!("{target_database_name}_{data_index}.ndf") })
        };
        let separator = if path.ends_with('/') || path.ends_with('\\') { "" } else if path.contains('\\') { "\\" } else { "/" };
        let physical_path = escape_sql_value(&format!("{path}{separator}{filename}"));
        moves.push(format!("move N'{logical_name}' to N'{physical_path}'"));
    }
    let target_identifier = target_database_name.replace(']', "]]" );
    let restore_sql = format!(
        "restore database [{target_identifier}] from url = N'{object_url_sql}' with {}, recovery, stats = 5",
        moves.join(", ")
    );
    client.simple_query(restore_sql).await
        .map_err(|error| format!("Cannot restore {backup_key} as {target_database_name}: {error}"))?
        .into_results().await
        .map_err(|error| format!("Restore of {target_database_name} did not complete: {error}"))?;
    Ok(())
}

#[tauri::command]
async fn list_sub_accounts(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = escape_sql_value(&account_id);

    query_json(
        &mut client,
        format!(
            r#"
            with account_tree as (
              select
                accountguid,
                parentaccountguid,
                accountid,
                cast(accountid as nvarchar(max)) as accountpath,
                0 as level
              from acct
              where parentaccountguid = (
                select accountguid from acct where accountid = N'{account_id}'
              )
                and isnull(isdeleted, 0) <> 1

              union all

              select
                child.accountguid,
                child.parentaccountguid,
                child.accountid,
                cast(parent.accountpath + N' / ' + child.accountid as nvarchar(max)) as accountpath,
                parent.level + 1 as level
              from acct child
              inner join account_tree parent
                on child.parentaccountguid = parent.accountguid
              where isnull(child.isdeleted, 0) <> 1
            )
            select (
              select
                accountid,
                accountpath,
                level,
                accountguid,
                parentaccountguid
              from account_tree
              order by accountpath
              for json path
            ) as json
        "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_sub_account_users(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = escape_sql_value(&account_id);

    query_json(
        &mut client,
        format!(
            r#"
            with account_tree as (
              select accountguid
              from acct
              where parentaccountguid = (
                select accountguid from acct where accountid = N'{account_id}'
              )
                and isnull(isdeleted, 0) <> 1

              union all

              select child.accountguid
              from acct child
              inner join account_tree parent
                on child.parentaccountguid = parent.accountguid
              where isnull(child.isdeleted, 0) <> 1
            )
            select (
              select
                u.accountguid,
                u.userguid,
                u.userid,
                u.username,
                u.email,
                u.expirypwd as expirydate
              from [user] u
              inner join account_tree a on a.accountguid = u.accountguid
              order by u.accountguid, u.userid
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_users(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                userguid,
                userid,
                username,
                email,
                expirypwd
              from [user]
              where accountguid = (
                select accountguid from acct where accountid = '{account_id}'
              )
              order by userid
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_all_users(
    config: OphConnectionConfig,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;

    query_json(
        &mut client,
        r#"
        select (
          select
            u.userguid,
            u.userid,
            u.username,
            u.accountguid,
            a.accountid
          from [user] u
          left join acct a on a.accountguid = u.accountguid
          order by u.userid
          for json path
        ) as json
        "#
        .to_string(),
    )
    .await
}

#[tauri::command]
async fn reset_user_password(
    config: OphConnectionConfig,
    database_name: String,
    account_id: String,
    user_guid: String,
    user_id: String,
    new_password: String,
) -> Result<(), String> {
    if new_password.is_empty() {
        return Err("New password cannot be empty.".to_string());
    }
    if user_guid.trim().is_empty() && user_id.trim().is_empty() {
        return Err("User ID and user key are missing.".to_string());
    }

    let server = selected_server(&config)?;
    let mut client = timeout(
        Duration::from_secs(5),
        connect_sql_server_database(server, &database_name),
    )
    .await
    .map_err(|_| format!("Cannot reset password: connection to database {database_name} timed out after 5 seconds."))??;
    let account_id = escape_sql_value(&account_id);
    let user_guid = escape_sql_value(&user_guid);
    let user_id = escape_sql_value(&user_id);
    let new_password = escape_sql_value(&new_password);

    let users = timeout(
        Duration::from_secs(5),
        client.query(
            format!(
                r#"
                select top 1 convert(nvarchar(36), u.userguid) as userguid, u.userid
                from [user] u
                inner join acct a on a.accountguid = u.accountguid
                where a.accountid = N'{account_id}'
                  and (
                    (N'{user_guid}' <> N'' and convert(nvarchar(36), u.userguid) = N'{user_guid}')
                    or (N'{user_id}' <> N'' and u.userid = N'{user_id}')
                  )
                "#
            ),
            &[],
        ),
    )
        .await
        .map_err(|_| format!("Cannot reset password: finding user {user_id} in database {database_name} timed out after 5 seconds."))?
        .map_err(|error| format!("Cannot resolve the selected user: {error}"))?
        .into_first_result()
        .await
        .map_err(|error| format!("Cannot resolve the selected user: {error}"))?;
    let user = users.first().ok_or_else(|| "The selected user was not found in this account.".to_string())?;
    let resolved_user_guid = escape_sql_value(&read_string(user, "userguid"));
    let resolved_user_id = escape_sql_value(&read_string(user, "userid"));

    let reset_query = format!(
        r#"
        set nocount on;
        set xact_abort on;
        set lock_timeout 5000;
        begin try
          begin transaction;

          if exists (
            select 1
            from userinfo
            where userguid = '{resolved_user_guid}'
              and infokey = N'verifycode'
          )
          begin
            update userinfo
            set infokey = N'verifycode',
                infovalue = N'8888'
            where userguid = '{resolved_user_guid}'
              and infokey = N'verifycode';
          end
          else
          begin
            insert into userinfo (userguid, infokey, infovalue)
            values ('{resolved_user_guid}', N'verifycode', N'8888');
          end;

          exec gen.resetPassword
            null,
            N'{resolved_user_id}',
            '{resolved_user_guid}',
            N'{new_password}',
            @accountid = N'{account_id}',
            @secretcode = N'8888';

          commit transaction;
        end try
        begin catch
          if @@trancount > 0 rollback transaction;
          throw;
        end catch
        "#
    );
    timeout(Duration::from_secs(10), client.execute(reset_query, &[]))
        .await
        .map_err(|_| format!("gen.resetPassword timed out after 10 seconds for user {resolved_user_id} in database {database_name}. Check blocking transactions on this database."))?
        .map_err(|error| format!("gen.resetPassword failed for user {resolved_user_id} in database {database_name}: {error}"))?;

    Ok(())
}

#[tauri::command]
async fn list_user_groups(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                ugroupguid,
                groupid,
                groupdescription,
                allexceptuser,
                tokenuser,
                allexceptenv,
                tokenenv,
                allexceptmodule
              from ugrp
              where accountguid = (
                select accountguid from acct where accountid = '{account_id}'
              )
              order by groupid
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_user_info(
    config: OphConnectionConfig,
    database_name: String,
    user_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let user_guid = escape_sql_value(&user_guid);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                userinfoguid,
                userguid,
                infokey,
                infovalue
              from userinfo
              where userguid = N'{user_guid}'
              order by infokey
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_user_group_modules(
    config: OphConnectionConfig,
    database_name: String,
    user_group_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let user_group_guid = escape_sql_value(&user_group_guid);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                gm.accessguid,
                gm.ugroupguid,
                gm.moduleguid,
                m.moduleid,
                m.moduledescription,
                gm.allowaccess,
                gm.allowadd,
                gm.allowedit,
                gm.allowdelete,
                gm.allowforce,
                gm.allowwipe
              from ugrpmodl gm
              left join modl m on m.moduleguid = gm.moduleguid
              where gm.ugroupguid = N'{user_group_guid}'
              order by m.moduleid
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_tree(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                moduleguid,
                moduleid,
                moduledescription,
                settingmode,
                parentmoduleguid,
                stuff((
                  select N' ' + isnull(mi.infokey, N'')
                  from modlinfo mi
                  where mi.moduleguid = modl.moduleguid
                  order by mi.infokey
                  for xml path(''), type
                ).value('.', 'nvarchar(max)'), 1, 1, N'') as searchtext
              from modl
              where settingmode in (0, 1, 4, 5, 6, 7)
                and accountguid = (
                  select accountguid from acct where accountid = '{account_id}'
                )
              order by settingmode, moduleid
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_column_tree(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                c.columnguid,
                c.moduleguid,
                c.colkey,
                stuff((
                  select N' ' + isnull(ci.infokey, N'')
                  from modlcolminfo ci
                  where ci.columnguid = c.columnguid
                  order by ci.infokey
                  for xml path(''), type
                ).value('.', 'nvarchar(max)'), 1, 1, N'') as searchtext
              from modlcolm c
              inner join modl m on m.moduleguid = c.moduleguid
              where m.accountguid = (
                select accountguid from acct where accountid = '{account_id}'
              )
              order by m.moduleid, c.colorder, c.colkey
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_columns(
    config: OphConnectionConfig,
    database_name: String,
    module_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let module_guid = module_guid.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                c.columnguid,
                c.moduleguid,
                c.colkey,
                c.coltype,
                c.titlecaption,
                c.colorder,
                c.collength,
                layout.pageno,
                layout.sectionno,
                layout.columnno,
                layout.rowno,
                layout.fieldno,
                layout.isviewable,
                layout.isbrowsable,
                layout.iseditable
              from modlcolm c
              outer apply (
                select
                  max(case when lower(i.infokey) = N'pageno' then i.infovalue end) as pageno,
                  max(case when lower(i.infokey) = N'sectionno' then i.infovalue end) as sectionno,
                  max(case when lower(i.infokey) in (N'columnno', N'colno') then i.infovalue end) as columnno,
                  max(case when lower(i.infokey) = N'rowno' then i.infovalue end) as rowno,
                  max(case when lower(i.infokey) = N'fieldno' then i.infovalue end) as fieldno,
                  max(case when lower(i.infokey) = N'isviewable' then i.infovalue end) as isviewable,
                  max(case when lower(i.infokey) = N'isbrowsable' then i.infovalue end) as isbrowsable,
                  max(case when lower(i.infokey) = N'iseditable' then i.infovalue end) as iseditable
                from modlcolminfo i
                where i.columnguid = c.columnguid
              ) layout
              where c.moduleguid = '{module_guid}'
              order by c.colorder, c.colkey
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_mssql_column_types(
    config: OphConnectionConfig,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;

    query_json(
        &mut client,
        r#"
        select (
          select coltype, typename
          from (
            select cast(0 as int) as coltype, cast(N'nonfield' as nvarchar(128)) as typename, 0 as sortorder
            union all
            select cast(system_type_id as int), cast(name as nvarchar(128)), 1
            from sys.types
            where is_user_defined = 0
              and system_type_id = user_type_id
              and name not in (N'sysname', N'timestamp')
          ) available_types
          order by sortorder, typename
          for json path
        ) as json
        "#
        .to_string(),
    )
    .await
}

#[tauri::command]
async fn list_module_info(
    config: OphConnectionConfig,
    database_name: String,
    module_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let module_guid = module_guid.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                moduleinfoguid,
                moduleguid,
                infokey,
                infovalue
              from modlinfo
              where moduleguid = '{module_guid}'
              order by infokey
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_column_info(
    config: OphConnectionConfig,
    database_name: String,
    column_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let column_guid = column_guid.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                columninfoguid,
                columnguid,
                infokey,
                infovalue
              from modlcolminfo
              where columnguid = '{column_guid}'
              order by infokey
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_child_modules(
    config: OphConnectionConfig,
    database_name: String,
    module_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let module_guid = module_guid.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                m.moduleguid,
                m.moduleid,
                m.moduledescription,
                m.settingmode,
                m.parentmoduleguid,
                m.accountdbguid,
                m.themepageguid,
                m.modulestatusguid,
                m.modulegroupguid,
                d.databasename as accountdb,
                p.moduleid as parentmodule,
                m.orderno,
                m.needlogin,
                tp.pageurl as themepage,
                s.modulestatusname as modulestatus,
                g.modulegroupname as modulegroup
              from modl m
              left join acctdbse d on d.accountdbguid = m.accountdbguid
              left join modl p on p.moduleguid = m.parentmoduleguid
              left join thmepage tp on tp.themepageguid = m.themepageguid
              left join msta s on s.modulestatusguid = m.modulestatusguid
              left join modg g on g.modulegroupguid = m.modulegroupguid
              where m.parentmoduleguid = '{module_guid}'
              order by m.moduleid
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_approvals(
    config: OphConnectionConfig,
    database_name: String,
    module_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let module_guid = module_guid.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                approval.approvalguid,
                approval.moduleguid,
                approval.approvalgroupguid,
                approval.uppergroupguid,
                coalesce(approval_group.groupid, convert(nvarchar(36), approval.approvalgroupguid), N'') as approvalgroup,
                coalesce(upper_group.groupid, convert(nvarchar(36), approval.uppergroupguid), N'') as uppergroup,
                approval.lvl,
                approval.sqlfilter,
                approval.zonegroup
              from modlappr approval
              left join ugrp approval_group on approval_group.ugroupguid = approval.approvalgroupguid
              left join ugrp upper_group on upper_group.ugroupguid = approval.uppergroupguid
              where approval.moduleguid = '{module_guid}'
              order by approval.lvl
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_numbering(
    config: OphConnectionConfig,
    database_name: String,
    module_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let module_guid = module_guid.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                docnumberguid,
                moduleguid,
                format,
                month,
                no
              from modldocn
              where moduleguid = '{module_guid}'
              order by format, month
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_mails(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
    module_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let module_guid = module_guid.replace('\'', "''");
    let account_id = escape_sql_value(&account_id);

    client
        .simple_query(format!(
            r#"
            declare @accountguid uniqueidentifier = (
              select accountguid from acct where accountid = N'{account_id}'
            );
            if @accountguid is null throw 50001, 'Cannot initialize mail parameters: account was not found.', 1;

            if not exists (select 1 from para where accountguid = @accountguid and upper(parameterid) = 'MACT')
              insert into para (parameterguid, accountguid, parameterid, parameterdescription)
              values (newid(), @accountguid, N'MACT', N'Mail Action');

            if not exists (select 1 from para where accountguid = @accountguid and upper(parameterid) = 'MLST')
              insert into para (parameterguid, accountguid, parameterid, parameterdescription)
              values (newid(), @accountguid, N'MLST', N'Mail Status');

            declare @defaults table (parameterid nvarchar(50), parametervalue nvarchar(100), parameterdescription nvarchar(255));
            insert into @defaults values
              (N'MACT', N'DELETE', N'DELETE'),
              (N'MACT', N'EMAIL', N'EMAIL'),
              (N'MACT', N'EXECUTE', N'EXECUTE'),
              (N'MACT', N'FORCE', N'FORCE'),
              (N'MACT', N'REOPEN', N'REOPEN'),
              (N'MACT', N'SAVE', N'SAVE'),
              (N'MACT', N'WIPE', N'WIPE'),
              (N'MLST', N'0', N'Draft'),
              (N'MLST', N'100', N'On Approval'),
              (N'MLST', N'300', N'Rejected'),
              (N'MLST', N'400', N'Released'),
              (N'MLST', N'500', N'Force'),
              (N'MLST', N'999', N'Deleted');

            insert into paravalu (parametervalueguid, parameterguid, parametervalue, parameterdescription)
            select newid(), p.parameterguid, d.parametervalue, d.parameterdescription
            from @defaults d
            inner join para p on p.accountguid = @accountguid and upper(p.parameterid) = d.parameterid
            where not exists (
              select 1 from paravalu existing
              where existing.parameterguid = p.parameterguid
                and upper(existing.parametervalue) = upper(d.parametervalue)
            );
            "#
        ))
        .await
        .map_err(|error| format!("Cannot initialize MACT/MLST for {account_id}: {error}"))?
        .into_results()
        .await
        .map_err(|error| format!("Cannot complete MACT/MLST initialization for {account_id}: {error}"))?;

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                mm.modulemailguid,
                mm.moduleguid,
                mm.mailguid,
                coalesce(mail_profile.profilename, mail_profile.displayname, convert(nvarchar(36), mm.mailguid), N'') as mail,
                mm.actionguid,
                coalesce(action_value.parametervalue, convert(nvarchar(36), mm.actionguid), N'') as action,
                mm.tokenstatus,
                coalesce(status_values.status, N'') as status,
                mm.additional,
                mm.cc,
                mm.subject,
                mm.body,
                mm.reportattachment,
                mm.definedtable
              from modlmail mm
              left join mail mail_profile on mail_profile.mailguid = mm.mailguid
              left join paravalu action_value on action_value.parametervalueguid = mm.actionguid
              outer apply (
                select string_agg(coalesce(status_value.parameterdescription, status_value.parametervalue, token.value), N', ') as status
                from string_split(isnull(mm.tokenstatus, N''), N'*') token
                left join paravalu status_value on convert(nvarchar(36), status_value.parametervalueguid) = ltrim(rtrim(token.value))
                where ltrim(rtrim(token.value)) <> N''
              ) status_values
              where mm.moduleguid = '{module_guid}'
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_modules(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
    setting_mode: i32,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                m.moduleguid,
                m.moduleid,
                m.moduledescription,
                m.settingmode,
                m.accountdbguid,
                m.themepageguid,
                m.modulestatusguid,
                m.modulegroupguid,
                d.databasename as accountdb,
                p.moduleid as parentmodule,
                m.orderno,
                m.needlogin,
                tp.pageurl as themepage,
                s.modulestatusname as modulestatus,
                g.modulegroupname as modulegroup
              from modl m
              left join acctdbse d on d.accountdbguid = m.accountdbguid
              left join modl p on p.moduleguid = m.parentmoduleguid
              left join thmepage tp on tp.themepageguid = m.themepageguid
              left join msta s on s.modulestatusguid = m.modulestatusguid
              left join modg g on g.modulegroupguid = m.modulegroupguid
              where m.settingmode = {setting_mode}
                and m.accountguid = (
                  select accountguid from acct where accountid = '{account_id}'
                )
              order by m.moduleid
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_statuses(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            declare @account_id nvarchar(255) = N'{account_id}';
            declare @select nvarchar(max) = N'modulestatusguid, accountguid, modulestatusname'
              + case when col_length('msta', 'modulestatusdescription') is not null then N', modulestatusdescription' else N', cast(null as nvarchar(max)) as modulestatusdescription' end
              + case when col_length('msta', 'isdefault') is not null then N', isdefault' else N', cast(null as bit) as isdefault' end
              + case when col_length('msta', 'createddate') is not null then N', createddate' else N', cast(null as datetime) as createddate' end
              + case when col_length('msta', 'updateddate') is not null then N', updateddate' else N', cast(null as datetime) as updateddate' end;
            declare @sql nvarchar(max) = N'
              select (
                select ' + @select + N'
                from msta
                where accountguid = (
                  select accountguid from acct where accountid = @account_id
                )
                order by modulestatusname
                for json path
              ) as json';
            exec sp_executesql @sql, N'@account_id nvarchar(255)', @account_id = @account_id;
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_status_states(
    config: OphConnectionConfig,
    database_name: String,
    module_status_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let module_status_guid = escape_sql_value(&module_status_guid);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                modulestatusdetailguid,
                modulestatusguid,
                stateid,
                statecode,
                statename,
                statedesc,
                isdefault
              from mstastat
              where modulestatusguid = N'{module_status_guid}'
              order by stateid, statecode
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_theme_pages(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = escape_sql_value(&account_id);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                tp.themepageguid,
                tp.pageurl,
                t.themecode,
                t.themename
              from thmepage tp
              inner join thme t on t.themeguid = tp.themeguid
              where t.accountguid = (
                select accountguid from acct where accountid = N'{account_id}'
              )
              order by t.themecode, tp.pageurl
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_module_groups(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                modulegroupid,
                modulegroupname,
                modulegroupdescription,
                modulegroupguid,
                accountguid,
                accountdbguid
              from modg
              where accountguid = (
                select accountguid from acct where accountid = '{account_id}'
              )
              order by modulegroupid
              for json path
            ) as json
        "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_all_module_groups(
    config: OphConnectionConfig,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;

    query_json(
        &mut client,
        r#"
        select (
          select
            g.modulegroupguid,
            g.modulegroupid,
            g.modulegroupname,
            g.accountguid,
            a.accountid
          from modg g
          left join acct a on a.accountguid = g.accountguid
          order by g.modulegroupid
          for json path
        ) as json
        "#
        .to_string(),
    )
    .await
}

#[tauri::command]
async fn list_module_group_info(
    config: OphConnectionConfig,
    database_name: String,
    module_group_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let module_group_guid = escape_sql_value(&module_group_guid);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select envinfoguid, modulegroupguid, infokey, infovalue
              from modginfo
              where modulegroupguid = N'{module_group_guid}'
              order by infokey
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_themes(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = escape_sql_value(&account_id);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                themeguid,
                accountguid,
                themecode,
                themename,
                themefolder
              from thme
              where accountguid = (
                select accountguid from acct where accountid = N'{account_id}'
              )
              order by themecode
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_theme_pages(
    config: OphConnectionConfig,
    database_name: String,
    theme_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let theme_guid = escape_sql_value(&theme_guid);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                themepageguid,
                themeguid,
                pageurl,
                isdefault
              from thmepage
              where themeguid = N'{theme_guid}'
              order by pageurl
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_menus(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                menuguid as menuid,
                menucode,
                menudescription,
                createddate,
                updateddate,
                lockmode
              from menu
              where accountguid = (
                select accountguid from acct where accountid = '{account_id}'
              )
              order by menucode
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_menu_submenus(
    config: OphConnectionConfig,
    database_name: String,
    menu_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let menu_guid = escape_sql_value(&menu_guid);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                menudetailguid,
                menudetailguid as submenuguid,
                menuguid,
                submenudescription,
                tag,
                url,
                orderno,
                caption,
                type,
                uppersubmenuguid,
                icon_fa,
                icon_url
              from menusmnu
              where menuguid = N'{menu_guid}'
              order by orderno, submenudescription
              for json path
            ) as json
            "#
        ),
    )
    .await
}



#[tauri::command]
async fn list_parameters(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                parameterguid,
                parameterid,
                parameterdescription,
                createddate,
                updateddate
              from para
              where accountguid = (
                select accountguid from acct where accountid = '{account_id}'
              )
              order by parameterid
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_parameter_values(
    config: OphConnectionConfig,
    database_name: String,
    parameter_guid: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let parameter_guid = escape_sql_value(&parameter_guid);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                parametervalueguid,
                parameterguid,
                parametervalue,
                parameterdescription
              from paravalu
              where parameterguid = N'{parameter_guid}'
              order by parametervalue
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_widgets(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = escape_sql_value(&account_id);

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                widgetguid,
                accountguid,
                widgetid,
                widgetdescription,
                sqlstr
              from widg
              where accountguid = (
                select accountguid from acct where accountid = N'{account_id}'
              )
              order by widgetid
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_mail_profiles(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                mailguid,
                profilename,
                accountname,
                displayname,
                emailaddress,
                bcc,
                createddate,
                updateddate
              from mail
              where accountguid = (
                select accountguid from acct where accountid = '{account_id}'
              )
              order by profilename
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[tauri::command]
async fn list_translator_words(
    config: OphConnectionConfig,
    account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let account_id = account_id.replace('\'', "''");

    query_json(
        &mut client,
        format!(
            r#"
            select (
              select
                wordguid,
                originstatements,
                createddate,
                updateddate
              from word
              where accountguid = (
                select accountguid from acct where accountid = '{account_id}'
              )
              order by originstatements
              for json path
            ) as json
            "#
        ),
    )
    .await
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![
            load_connection_config,
            run_query,
            save_connection_config,
            delete_connection_config,
            save_metadata_row,
            delete_metadata_row,
            list_copy_accounts,
            list_copy_targets,
            copy_metadata_rows,
            test_connection,
            add_account,
            delete_account,
            list_oph_databases,
            list_account_info,
            list_account_databases,
            list_database_backups,
            restore_database_backup,
            list_sub_accounts,
            list_sub_account_users,
            list_users,
            list_all_users,
            reset_user_password,
            list_user_groups,
            list_user_info,
            list_user_group_modules,
            list_module_tree,
            list_module_column_tree,
            list_module_info,
            list_module_columns,
            list_mssql_column_types,
            list_module_column_info,
            list_child_modules,
            list_module_approvals,
            list_module_numbering,
            list_module_mails,
            list_modules,
            list_module_statuses,
            list_module_status_states,
            list_module_theme_pages,
            list_module_groups,
            list_all_module_groups,
            list_module_group_info,
            list_themes,
            list_theme_pages,
            list_menus,
            list_menu_submenus,
            list_translator_words,
            list_parameters,
            list_parameter_values,
            list_widgets,
            list_mail_profiles
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#[cfg(test)]
mod tests {
    use super::{escape_sql_value, valid_database_name, valid_s3_configuration};

    #[test]
    fn escapes_apostrophes_without_changing_json_quotes() {
        let value = r#"[{"url":"javascript:btn_function('submit')","caption":"submit"}]"#;
        assert_eq!(escape_sql_value(value), r#"[{"url":"javascript:btn_function(''submit'')","caption":"submit"}]"#);
    }

    #[test]
    fn validates_safe_database_names() {
        assert!(valid_database_name("account_v4_001"));
        assert!(!valid_database_name(""));
        assert!(!valid_database_name("account-v4"));
        assert!(!valid_database_name("account];drop database master;--"));
        assert!(!valid_database_name(&"a".repeat(129)));
    }

    #[test]
    fn validates_s3_configuration_requirements() {
        assert!(valid_s3_configuration("access", "secret", "bucket", None));
        assert!(valid_s3_configuration("access", "secret", "bucket", Some("s3.example.com")));
        assert!(!valid_s3_configuration("", "secret", "bucket", None));
        assert!(!valid_s3_configuration("access", "", "bucket", None));
        assert!(!valid_s3_configuration("access", "secret", "", None));
        assert!(!valid_s3_configuration("access", "secret", "bucket", Some("")));
    }
}
