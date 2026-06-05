use serde::{Deserialize, Serialize};
use std::{fs, path::PathBuf};
use tauri::Manager;
use tiberius::{AuthMethod, Client, Config, EncryptionLevel, Row};
use tokio::net::TcpStream;
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
    row.get::<&str, _>(column).unwrap_or_default().to_string()
}

fn escape_sql_value(value: &str) -> String {
    value.replace('\'', "''")
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

#[derive(Clone)]
struct CrudMapping {
    table_name: &'static str,
    key_column: &'static str,
    key_field: &'static str,
    parent_column: Option<&'static str>,
    parent_context: Option<&'static str>,
    writable_columns: &'static [&'static str],
}

fn crud_mapping(source_table: &str) -> Option<CrudMapping> {
    match source_table.to_lowercase().as_str() {
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
            parent_column: None,
            parent_context: None,
            writable_columns: &["moduleid", "moduledescription", "settingmode", "orderno", "needlogin"],
        }),
        "menu" => Some(CrudMapping {
            table_name: "menu",
            key_column: "menuguid",
            key_field: "menuid",
            parent_column: None,
            parent_context: None,
            writable_columns: &["menucode", "menudescription", "createddate", "updateddate"],
        }),
        "thme" => Some(CrudMapping {
            table_name: "thme",
            key_column: "themeguid",
            key_field: "themeguid",
            parent_column: None,
            parent_context: None,
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
) -> Result<(), String> {
    let mapping = crud_mapping(&source_table)
        .ok_or_else(|| format!("Save is not supported for {source_table} yet."))?;
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;
    let row_key = json_field(&original_row, mapping.key_field);

    let sql = if row_key.trim().is_empty() {
        let parent_value = match mapping.parent_context {
            Some("moduleGuid") => module_guid.unwrap_or_default(),
            Some("columnGuid") => column_guid.unwrap_or_default(),
            Some("themeGuid") => theme_guid.unwrap_or_default(),
            Some("userGuid") => user_guid.unwrap_or_default(),
            Some("userGroupGuid") => user_group_guid.unwrap_or_default(),
            _ => String::new(),
        };

        if mapping.parent_column.is_some() && parent_value.trim().is_empty() {
            return Err(format!("Cannot create {source_table}: parent key is missing."));
        }

        let mut insert_columns = vec![mapping.key_column.to_string()];
        let mut insert_values = vec!["newid()".to_string()];

        if let Some(parent_column) = mapping.parent_column {
            insert_columns.push(parent_column.to_string());
            insert_values.push(format!("N'{}'", escape_sql_value(&parent_value)));
        }

        for column in mapping.writable_columns {
            insert_columns.push(column.to_string());
            insert_values.push(format!("N'{}'", escape_sql_value(&json_field(&draft_row, column))));
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
            .map(|column| {
                let value = escape_sql_value(&json_field(&draft_row, column));
                format!("{column} = N'{value}'")
            })
            .collect::<Vec<_>>();

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
                left join acctdbse d
                  on d.accountguid = a.accountguid
                 and d.ismaster = 1
                 and d.version = '4.0'
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
    _account_id: String,
    database_name: String,
) -> Result<Vec<serde_json::Value>, String> {
    let server = selected_server(&config)?;
    let mut client = connect_sql_server_database(server, &database_name).await?;

    query_json(
        &mut client,
            r#"
            select (
              select
                a.accountid,
                d.accountdbguid,
                d.accountguid,
                d.databasename,
                d.ismaster,
                d.version
              from acctdbse d
              inner join acct a on a.accountguid = d.accountguid
              order by a.accountid, d.databasename
              for json path
            ) as json
            "#
            .to_string(),
    )
    .await
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
                expirypwd as expirydate
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
                groupdescription
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
                accessguid,
                ugroupguid,
                moduleguid,
                allowaccess,
                allowadd,
                allowedit,
                allowdelete,
                allowforce,
                allowwipe
              from ugrpmodl
              where ugroupguid = N'{user_group_guid}'
              order by moduleguid
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
                parentmoduleguid
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
                c.colkey
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
                columnguid,
                moduleguid,
                colkey,
                coltype,
                titlecaption,
                colorder,
                collength
              from modlcolm
              where moduleguid = '{module_guid}'
              order by colorder, colkey
              for json path
            ) as json
            "#
        ),
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
                approvalguid,
                moduleguid,
                approvalgroupguid,
                uppergroupguid,
                lvl,
                sqlfilter,
                zonegroup
              from modlappr
              where moduleguid = '{module_guid}'
              order by lvl
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
                modulemailguid,
                moduleguid,
                mailguid,
                actionguid,
                tokenstatus,
                additional,
                cc,
                subject,
                body,
                reportattachment,
                definedtable
              from modlmail
              where moduleguid = '{module_guid}'
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
            declare @select nvarchar(max) = N'modulestatusname'
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
async fn list_widgets(
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
            declare @select nvarchar(max) = N'widgetid'
              + case when col_length('widg', 'createddate') is not null then N', createddate' else N', cast(null as datetime) as createddate' end
              + case when col_length('widg', 'updateddate') is not null then N', updateddate' else N', cast(null as datetime) as updateddate' end;
            declare @sql nvarchar(max) = N'
              select (
                select ' + @select + N'
                from widg
                where accountguid = (
                  select accountguid from acct where accountid = @account_id
                )
                order by widgetid
                for json path
              ) as json';
            exec sp_executesql @sql, N'@account_id nvarchar(255)', @account_id = @account_id;
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
            save_connection_config,
            delete_connection_config,
            save_metadata_row,
            delete_metadata_row,
            test_connection,
            list_oph_databases,
            list_account_info,
            list_account_databases,
            list_sub_accounts,
            list_users,
            list_user_groups,
            list_user_info,
            list_user_group_modules,
            list_module_tree,
            list_module_column_tree,
            list_module_info,
            list_module_columns,
            list_module_column_info,
            list_child_modules,
            list_module_approvals,
            list_module_numbering,
            list_module_mails,
            list_modules,
            list_module_statuses,
            list_module_groups,
            list_themes,
            list_theme_pages,
            list_menus,
            list_translator_words,
            list_parameters,
            list_widgets,
            list_mail_profiles
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
