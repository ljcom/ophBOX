# OPH Control Studio v0.3 Summary

## Objective

`v0.3` is the current migration track for turning legacy `ophBOX` into **OPH Control Studio**, a modern cross-platform desktop administration tool for existing OPH MSSQL installations and future EventDB/PostgreSQL migration work.

The direction is not a direct VB.NET WinForms rewrite. The current goal is to copy the legacy feature flow first, but present it with cleaner wording, a database-admin style tree, reusable command/service layers, and a more compact UI.

## Current Stack

- Desktop shell: Tauri v2
- Frontend: React 19, Vite 6, TypeScript
- Rust backend: Tauri commands in `v0.3/tauri/src-tauri/src/lib.rs`
- MSSQL access: Rust `tiberius` over TCP/TDS
- Icons/UI: `lucide-react`, local CSS
- App folder: `v0.3/tauri`
- Main docs: `v0.3/docs/migrasi.md`, `AGENTS.md`, `TODO-sam.md`

## Running

From the desktop app folder:

```bash
cd v0.3/tauri
npm run tauri dev
```

Important distinction:

- `npm run tauri dev` runs the real desktop app with Tauri commands and real SQL Server access.
- `npm run dev` only runs the Vite web preview and should not be treated as the real app.

## High-Level Architecture

```text
Tauri Desktop Shell
  ↓
React/Vite UI
  ↓
OPH Admin Service wrapper
  ↓
Tauri Rust commands
  ↓
Legacy OPH MSSQL
```

Frontend files:

- `v0.3/tauri/src/App.tsx`: current UI composition, workspace routing, metadata grids, overlay editor.
- `v0.3/tauri/src/services/mockOphAdminService.ts`: frontend service wrapper; still named mock, but now mostly calls real Tauri commands.
- `v0.3/tauri/src/types/domain.ts`: OPH domain types and tree node types.
- `v0.3/tauri/src/styles.css`: current app styling.

Backend file:

- `v0.3/tauri/src-tauri/src/lib.rs`: connection config, SQL connection, metadata loaders, limited CRUD commands.

## Connection Flow

Current behavior:

1. On startup, app loads local connection config.
2. If config is missing, app opens Add Connection.
3. Add Connection has default database `oph_core`.
4. Save/Test uses real SQL Server through Rust/Tiberius.
5. Save tests the connection first; if test fails, config is not saved and error is shown.
6. If config exists but server/database is unavailable, app stays in main workspace with a connection issue view and refresh action.
7. App no longer jumps back to Add Connection on connection loss.

Local config is stored using Tauri app config dir as `connection-config.json`.

## Database Tree Flow

Current main tree flow follows the legacy OPH v0.2 structure:

```text
Servers
└── Server
    └── Database / Account
        ├── Modules
        ├── Security
        ├── Interface
        └── Account
```

Database discovery behavior:

- If default database is `oph_core`, account list is read from `oph_core` using `acct` + `acctdbse` where `ismaster = 1` and `version = '4.0'`.
- The displayed database level uses account id.
- The physical metadata database is taken from `acctdbse.databasename`.
- `oph_core` itself is not displayed as a database node in the normal account flow.
- If default database is not `oph_core`, that database is shown directly.

Sidebar behavior:

- Sidebar is full height and vertically scrollable.
- Database nodes default collapsed below server level.
- The old footer text `Navigation flow / Server → Database → Domain groups` has been removed.

## Main Panel Database Stats

When a database node is clicked, main panel metrics are dynamic:

- `Modules`: counted from module nodes in the current tree.
- `Security`: count of security child groups.
- `Interface`: count of interface child groups.
- `Account`: count of account child groups.

The old static metrics (`128`, `42`, `18`, `5`) have been replaced.

## Metadata Table UX

All metadata list pages use a common table component:

- Horizontal scroll is enabled for wide lists.
- Visible columns are allowlisted per source table.
- GUID and internal key columns are hidden unless explicitly needed.
- Each list has an `Add` button that opens an empty overlay form.
- Clicking a row opens a right-side overlay panel.
- Overlay opens directly in edit mode.
- Overlay fields are vertical form controls.
- `createddate` and `updateddate` stay visible in the list, but are hidden in the overlay.
- Overlay buttons are fixed at the bottom: `Save`, `Cancel`, `Delete`.
- `Save`, `Cancel`, and `Delete` close the overlay.

## Current CRUD Status

The overlay can call real backend commands for Save/Delete through:

- `save_metadata_row`
- `delete_metadata_row`

CRUD is intentionally mapped through a backend whitelist, not raw arbitrary table names.

Currently mapped sources include:

- `modlinfo`
- `modlcolminfo`
- `modlcolm`
- `modlappr`
- `modldocn`
- `modlmail`
- `modl`
- `menu`
- `thme`
- `thmepage`
- `userinfo`
- `ugrpmodl`

If a table is not mapped, Save/Delete returns an explicit unsupported-table error.

Note: current CRUD command uses generated `newid()` for inserted key columns and string SQL literals. It is functional for current migration work, but should later be hardened with typed parameter binding, better value conversion, and table-specific validation.

## Implemented Domain Areas

### Account

Implemented:

- `acctinfo` list for current account.
- `acctdbse` database list.
- Recursive sub account tree under `Sub Accounts`.
- Clicking a sub account node loads related `acctinfo`.
- Parameters, widgets, and mail metadata lists.

Current queries include:

- `acctinfo`
- `acctdbse`
- `acct` recursive child account tree
- `para`
- `widg`
- `mail`

### Modules

Implemented tree and metadata flow:

```text
Modules
├── Core          settingmode = 0
├── Master        settingmode = 1
├── Transaction   settingmode = 4
├── Report        settingmode = 5
├── Blank         settingmode = 6
├── View          settingmode = 7
├── Module Status
└── Module Groups
```

Module category nodes contain root modules. Each module node contains:

- `Columns`
- `Children`
- `Approvals`
- `Numbering`
- `Mails`

Implemented recursive module behavior:

- `Children` lists child modules where `parentmoduleguid` matches the current module.
- Child modules recursively contain `Columns` and `Children`.
- Cycle protection exists in the frontend builder through visited GUID sets.

Implemented module metadata lists:

- `modl` grouped by `settingmode`
- `modlinfo` when module node is clicked
- `modlcolm` under Columns
- `modlcolminfo` when a column node is clicked
- `modlappr` under Approvals
- `modldocn` under Numbering
- `modlmail` under Mails
- `msta` for Module Status
- `modg` for Module Groups

Module list display columns:

- `moduleid`
- `moduledescription`
- `settingmode`
- `accountdb` from `acctdbse.databasename`
- `parentmodule` from parent `modl.moduleid`
- `orderno`
- `needlogin`
- `themepage` from `thmepage.pageurl`
- `modulestatus` from `msta.modulestatusname`
- `modulegroup` from `modg.modulegroupname`

### Security

Implemented:

- Users list from `[user]`.
- User groups list from `ugrp`.
- Sidebar child user list under `Users`.
- Sidebar child user group list under `User Groups`.
- Clicking user node loads `userinfo`.
- Clicking user group node loads `ugrpmodl`.

User list visible columns:

- `userid`
- `username`
- `email`
- `expirydate`

User group visible columns:

- `groupid`
- `groupdescription`

### Interface

Implemented:

- Themes list from `thme`.
- Sidebar theme list under `Themes`.
- Clicking theme node loads `thmepage`.
- Menus list from `menu`.
- Translator list from `word`.

Theme visible columns:

- `themecode`
- `themename`
- `themefolder`

Theme page visible columns:

- `pageurl`
- `isdefault`

Menu visible columns:

- `menucode`
- `menudescription`
- `createddate`
- `updateddate`

Translator visible columns:

- `originstatements`
- `createddate`
- `updateddate`

## Current Open TODOs

As of this summary, `TODO-sam.md` still shows unfinished work for:

- Menu tree children:
  - show menu list under sidebar `Menus`
  - clicking menu child should show `menusmnu`
- Translator tree children:
  - show `word` list under sidebar `Translator`
  - clicking word child should show `wordlang`
- Parameter tree children:
  - show parameter list under sidebar `Parameters`
  - clicking parameter child should show `paravalu`

These should likely follow the same pattern already implemented for users, user groups, themes, modules, columns, and sub accounts:

1. Include key GUID/id columns in list query.
2. Load rows during connection activation.
3. Add child tree nodes in `buildDatabaseChildren`.
4. Add detail Tauri command for child table.
5. Add frontend service wrapper.
6. Route child node selection in `Workspace`.
7. Add metadata table allowlist and CRUD mapping if editing is needed.

## Validation Status

Recent validation commands have passed:

```bash
cd v0.3/tauri
npm run build
```

```bash
cd v0.3/tauri/src-tauri
cargo check
```

## Known Engineering Notes

- `mockOphAdminService.ts` is now a real service wrapper for most operations despite its legacy filename.
- `App.tsx` has grown large and should eventually be split into smaller components and hooks.
- SQL command code is concentrated in `src-tauri/src/lib.rs`; it is functional but should later be factored by domain.
- CRUD is currently whitelist-based but still uses generated SQL strings. It should eventually move to parameterized SQL and stronger table-specific conversion.
- Some tables use user-facing joined labels in list mode, while hidden GUIDs remain available for CRUD and tree navigation.
- The project is currently optimized for matching legacy OPH flow quickly before deeper cleanup.
