# OPH Control Studio TODO

## Priority: Backup and Restore

- [ ] Test S3 object listing against every configured account database.
- [ ] Test `RESTORE DATABASE FROM URL` against the production SQL Server version.
- [ ] Show restore progress and SQL Server status messages in the restore overlay.
- [x] Refresh the server/database tree automatically after a successful restore.
- [x] Add restore history with backup file, destination database, start time, duration, and result.
- [x] Add backup search, date filtering, sorting, and pagination beyond the first 1,000 S3 objects.
- [x] Validate backup integrity with `RESTORE VERIFYONLY` before starting a restore.
- [x] Support cancellation or clear recovery instructions for interrupted restores.
- [ ] Confirm S3 credential handling for AWS S3 and compatible endpoints.
- [ ] Remove the runtime AWS CLI dependency by moving S3 listing into the Rust command layer.

## Priority: Security and Reliability

- [ ] Store saved SQL Server passwords securely using the operating-system credential store.
- [ ] Prevent secrets from appearing in logs, SQL errors, and user-facing diagnostics.
- [x] Add confirmation and audit logging for destructive account/database operations.
- [ ] Add integration tests for password reset using User ID and User Key lookup.
- [ ] Review and resolve GitHub Dependabot findings.
- [ ] Add structured backend error codes so the UI can provide specific recovery actions.

## Database Administration

- [ ] Display physical database size, status, compatibility level, and last backup time.
- [ ] Add database refresh after account creation, deletion, restore, and migration.
- [ ] Add native backup creation and upload to the shared S3 location.
- [ ] Add database detach, attach, rename, and maintenance workflows with safeguards.
- [ ] Add backup retention policy visibility and cleanup controls.

## Metadata Editors

- [ ] Complete validation rules for Module, Column, Approval, Numbering, and Mail editors.
- [ ] Add change previews before saving metadata.
- [ ] Add bulk editing and safer cross-account copy mapping.
- [ ] Add undo or revision history for metadata changes.
- [ ] Expand Report Designer validation and preview coverage.
- [ ] Test Mail Action and Mail Status initialization on legacy database versions.

## Product Areas

- [ ] Complete Dashboard operational metrics.
- [ ] Implement EventDB Monitor.
- [ ] Implement MSSQL-to-EventDB Migration Center workflows.
- [ ] Complete Query Studio saved queries, history, and export.
- [ ] Complete Menu, Theme, and Translator administration.
- [ ] Add application settings for S3, diagnostics, appearance, and update channels.

## Packaging and Release

- [ ] Add macOS code signing and notarization.
- [ ] Build and publish an Intel (`x86_64`) or universal macOS package.
- [ ] Verify Windows NSIS packaging and release workflow.
- [ ] Add Linux AppImage or Debian packaging.
- [ ] Add automated release checks, artifact checksums, and release notes.
- [ ] Add an in-app update mechanism with stable and pre-release channels.

## Testing

- [ ] Add frontend component tests for tree navigation, metadata forms, and overlays.
- [x] Add Rust command tests for SQL escaping, database-name validation, and S3 configuration.
- [ ] Add end-to-end tests for connection, browsing, backup listing, restore, and password reset.
- [ ] Test Windows, macOS, and Linux against supported SQL Server versions.
