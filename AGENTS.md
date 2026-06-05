# OPH Control Studio Agent Guide

This repository is migrating legacy `ophBOX` into a modern cross-platform
desktop administration tool called **OPH Control Studio**.

The migration source notes live in `v0.3/docs/migrasi.md`. Treat that document
as project direction, but use this file as the working instruction set for
implementation.

## Product Direction

- Build a modern OPH administration platform, not a direct VB.NET WinForms UI
  rewrite.
- For the initial phase, preserve and copy the current legacy feature coverage,
  but implement it with a cleaner, more concise product and code structure.
- The app should run on Windows, macOS, and Linux.
- The desktop app belongs under `v0.3/tauri`.
- Use the legacy `ljcom/ophBOX` behavior and schema as reference material only.
- Design for current MSSQL-based OPH systems and future EventDB/PostgreSQL
  migration paths.

## Product Areas

OPH Control Studio should be organized around these administration areas:

- Server Explorer
- Database Explorer
- Module Editor
- Column / Field Editor
- Approval Editor
- Numbering Editor
- Mail Template Editor
- Menu / Theme Editor
- EventDB Monitor
- Migration Center
- Query Studio
- Backup / Restore

## Wording and UX Guidelines

- Treat all wording as communication with the end user, not with developers.
- Developer-facing names, implementation notes, table names, and code concepts
  should not leak into user-facing labels unless the screen is explicitly a
  developer, diagnostic, metadata, or query tool.
- Use the name **OPH Control Studio** consistently for the desktop app.
- Prefer domain wording familiar to OPH administrators: server, database,
  account, module, column, approval, numbering, mail template, menu, theme, and
  translator.
- Keep labels concise and operational. Examples: `Test Connection`,
  `Open Metadata`, `Save Server`, `Run Query`, `Validate`, `Backup`, `Restore`.
- Avoid legacy implementation wording in the UI when it does not help the user.
  For example, prefer `Module` over raw table names such as `modl` in visible UI.
- Raw table and column names may appear in developer tools, diagnostics, query
  views, or metadata inspectors.
- Make validation and error messages specific: identify the server, database,
  module, field, or operation involved.
- Keep migration copy explicit about source and target systems. Example:
  `Migrate legacy MSSQL module metadata to EventDB`.

## Coding Guidelines

- Use Tauri v2, React, Vite, and TypeScript for the desktop application.
- Use a Node.js sidecar only when it is clearly needed for MSSQL, `sqlcmd`, IIS,
  or filesystem operations that are not practical in the Tauri/Rust layer.
- Put business logic in reusable service or command layers, not in React
  components.
- React components should focus on presentation, state orchestration, and calls
  into commands/services.
- Keep OPH domain types explicit and reusable across the frontend and command
  boundary where practical.
- Prefer small, focused modules over large all-purpose files.
- Do not introduce broad rewrites unless required by the current migration step.

## Target Architecture

```text
Tauri Desktop Shell
  ↓
React/Vite UI
  ↓
OPH Admin Service / Command Layer
  ↓
MSSQL Legacy OPH
  ↓
Future EventDB / PostgreSQL
```

## First Milestone

Create the basic Tauri + React/Vite application in `v0.3/tauri` with these
routes:

- `/dashboard`
- `/servers`
- `/databases`
- `/modules`
- `/modules/:moduleId`
- `/query`
- `/migration`
- `/settings`

## Layout Direction

Use a layout similar to database management tools:

- Left sidebar: Servers, Databases, Modules, EventDB, Migration, Query, Settings.
- Main area: data grids, detail editors, tabs, and primary workspace content.
- Right panel: properties, validation, and action buttons.

## Phase 1 Domain Model

Server manager fields:

```ts
type OphServer = {
  id: string
  name: string
  host: string
  port: number
  authType: 'sql' | 'windows'
  username?: string
  password?: string
  trustServerCertificate?: boolean
  encrypt?: boolean
}
```

Phase 1 must support adding, editing, deleting, testing, and locally saving
server connection configs.

## Legacy Concepts

Important OPH concepts:

- Server
- Database
- Account
- Module
- Module Column
- Module Approval
- Module Numbering
- Module Mail
- User
- User Group
- Theme
- Menu
- Translator

Important legacy tables:

- `acct`
- `acctdbse`
- `modl`
- `modlcolm`
- `modlinfo`
- `modlcolminfo`
- `modlappr`
- `modldocn`
- `modlmail`
- `ugrp`
- `msta`
- `modg`
- `thme`
- `thmepage`

## Phase 1 Data Access

Module explorer query:

```sql
select
  moduleguid,
  accountguid,
  moduleid,
  moduledescription,
  settingmode,
  accountdbguid,
  parentmoduleguid,
  orderno,
  needlogin,
  themepageguid,
  modulestatusguid,
  modulegroupguid
from modl
order by moduleid
```

Column editor query:

```sql
select
  columnguid,
  moduleguid,
  colkey,
  coltype,
  titleCaption,
  colOrder,
  collength
from modlcolm
where moduleguid = @moduleguid
order by colOrder
```

Approval editor query:

```sql
select
  ApprovalGUID,
  ModuleGUID,
  ApprovalGroupGUID,
  UpperGroupGUID,
  Lvl,
  SQLfilter,
  ZoneGroup
from modlappr
where moduleguid = @moduleguid
order by lvl
```

Numbering editor query:

```sql
select
  DocNumberGUID,
  ModuleGUID,
  Format,
  Month,
  No
from modldocn
where moduleguid = @moduleguid
order by Format, Month
```

Mail editor query:

```sql
select
  ModuleMailGUID,
  ModuleGUID,
  MailGUID,
  ActionGUID,
  TokenStatus,
  Additional,
  CC,
  Subject,
  Body,
  ReportAttachment,
  DefinedTable
from modlmail
where moduleguid = @moduleguid
```
