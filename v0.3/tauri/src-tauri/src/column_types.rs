// coltype stores legacy SQL Server system_type_id values, even after migration.
// Keep distinct legacy IDs when several SQL Server types share a PostgreSQL type.
const POSTGRESQL_COLUMN_TYPES: &[(i32, &str)] = &[
    (0, "nonfield"),
    (34, "bytea (image)"),
    (35, "text (text)"),
    (36, "uuid (uniqueidentifier)"),
    (40, "date (date)"),
    (41, "time (time)"),
    (42, "timestamp (datetime2)"),
    (43, "timestamptz (datetimeoffset)"),
    (48, "smallint (tinyint)"),
    (52, "smallint (smallint)"),
    (56, "integer (int)"),
    (58, "timestamp (smalldatetime)"),
    (59, "real (real)"),
    (60, "numeric (money)"),
    (61, "timestamp (datetime)"),
    (62, "double precision (float)"),
    (98, "jsonb (sql_variant)"),
    (99, "text (ntext)"),
    (104, "boolean (bit)"),
    (106, "numeric (decimal)"),
    (108, "numeric (numeric)"),
    (122, "numeric (smallmoney)"),
    (127, "bigint (bigint)"),
    (165, "bytea (varbinary)"),
    (167, "varchar (varchar)"),
    (173, "bytea (binary)"),
    (175, "char (char)"),
    (231, "varchar (nvarchar)"),
    (239, "char (nchar)"),
    (241, "xml (xml)"),
];

pub(crate) fn postgresql_column_types() -> Vec<serde_json::Value> {
    let mut types = POSTGRESQL_COLUMN_TYPES.to_vec();
    types[1..].sort_by_key(|(_, name)| *name);
    types
        .into_iter()
        .map(|(id, name)| serde_json::json!({ "coltype": id, "typename": name }))
        .collect()
}

#[cfg(test)]
mod tests {
    #[test]
    fn preserves_migrated_column_type_ids_and_legacy_choices() {
        let types = super::postgresql_column_types();
        assert_eq!(
            types[0],
            serde_json::json!({"coltype": 0, "typename": "nonfield"})
        );
        // Choices explicitly offered by v0.2, plus the actual MSSQL datetime ID.
        for (id, pg_type) in [
            (36, "uuid"),
            (42, "timestamp"),
            (60, "numeric"),
            (127, "bigint"),
            (104, "boolean"),
            (165, "bytea"),
            (231, "varchar"),
            (61, "timestamp"),
        ] {
            let row = types.iter().find(|row| row["coltype"] == id).unwrap();
            assert!(row["typename"].as_str().unwrap().starts_with(pg_type));
        }
        let ids: std::collections::HashSet<_> = types
            .iter()
            .map(|row| row["coltype"].as_i64().unwrap())
            .collect();
        assert_eq!(ids.len(), types.len());
    }
}
