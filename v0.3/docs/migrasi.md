Berikut instruksi untuk Codex:

# Codex Instruction: Build OPH Control Studio
## Goal
Migrate legacy `ophBOX` into a modern cross-platform desktop app called **OPH Control Studio**.
The app must run on:
- Windows
- macOS
- Linux
Use:
- Tauri v2
- React
- Vite
- TypeScript
- Node.js sidecar only if needed for MSSQL/sqlcmd/IIS/file operations
Tauri is suitable because it supports cross-platform desktop apps using a web frontend and Rust backend. Vite is suitable for React/TypeScript frontend with fast HMR development.
## Main Principle
Do not simply rewrite the old VB.NET WinForms UI.
Instead, create a modern OPH administration platform:
```text
OPH Control Studio
├── Server Explorer
├── Database Explorer
├── Module Editor
├── Column / Field Editor
├── Approval Editor
├── Numbering Editor
├── Mail Template Editor
├── Menu / Theme Editor
├── EventDB Monitor
├── Migration Center
├── Query Studio
└── Backup / Restore

Architecture

Tauri Desktop Shell
  ↓
React/Vite UI
  ↓
OPH Admin Service / Command Layer
  ↓
MSSQL Legacy OPH
  ↓
Future EventDB / PostgreSQL

Important Rule

All business logic must be placed in a reusable service layer, not inside React components.

The desktop UI should only call commands/services.

Legacy Reference

Use ljcom/ophBOX as reference only.

Important legacy concepts:

* Server
* Database
* Account
* Module
* Module Column
* Module Approval
* Module Numbering
* Module Mail
* User
* User Group
* Theme
* Menu
* Translator

Legacy tables include:

* acct
* acctdbse
* modl
* modlcolm
* modlinfo
* modlcolminfo
* modlappr
* modldocn
* modlmail
* ugrp
* msta
* modg
* thme
* thmepage

First Milestone

Create the basic Tauri + React/Vite application.

Pages required:

/dashboard
/servers
/databases
/modules
/modules/:moduleId
/query
/migration
/settings

UI Layout

Use a layout similar to database management tools:

Left Sidebar:
- Servers
- Databases
- Modules
- EventDB
- Migration
- Query
- Settings
Main Area:
- Data grid
- Detail editor
- Tabs
Right Panel:
- Properties
- Validation
- Action buttons

Core Features Phase 1

1. Server Manager

Allow user to:

* Add server
* Edit server
* Delete server
* Test connection
* Save connection config locally

Fields:

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

2. Database Explorer

Allow user to:

* List OPH databases
* Detect oph_core
* Detect account databases
* Show database status
* Open database metadata

3. Module Explorer

Read module metadata from legacy OPH MSSQL.

Use table:

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

4. Module Editor

Allow edit:

* module id
* description
* setting mode
* parent module
* status
* module group
* need login
* order number

5. Column Editor

Read and edit:

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

6. Approval Editor

Read and edit:

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

7. Numbering Editor

Read and edit:

select
  DocNumberGUID,
  ModuleGUID,
  Format,
  Month,
  No
from modldocn
where moduleguid = @moduleguid
order by Format, Month

8. Mail Editor

Read and edit:

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

Technical Structure

Create folders:

src/
  app/
  components/
  features/
    servers/
    databases/
    modules/
    query/
    migration/
    settings/
  services/
  types/
  utils/
src-tauri/
  src/
    commands/
    db/
    config/

Data Access Rule

Do not hardcode SQL directly in React components.

Use service functions:

serverService.testConnection()
databaseService.listDatabases()
moduleService.listModules()
moduleService.getModule()
moduleService.saveModule()
moduleService.listColumns()
moduleService.saveColumns()

Local Config

Store local config securely where possible.

Initial config format:

{
  "servers": [],
  "lastOpenedServerId": null,
  "theme": "system"
}

Security

Never expose passwords in UI logs.

Mask passwords:

********

Do not commit secrets.

Future Direction

Prepare architecture for:

MSSQL Legacy Read Layer
EventDB Writer Layer
PostgreSQL Future Read Layer

Do not lock the app only to MSSQL.

Use adapter pattern:

interface OphMetadataAdapter {
  listModules(): Promise<OphModule[]>
  getModule(id: string): Promise<OphModule>
  saveModule(module: OphModule): Promise<void>
  listColumns(moduleId: string): Promise<OphColumn[]>
}

Implement first adapter:

MssqlOphMetadataAdapter

Later:

EventDbMetadataAdapter
PostgresOphMetadataAdapter

Deliverables

1. Working Tauri + React/Vite desktop app.
2. Sidebar layout.
3. Server Manager page.
4. MSSQL connection test.
5. Module list page.
6. Module detail editor.
7. Column editor.
8. Basic settings page.
9. README with setup instructions.
10. No business logic inside React components.

Do Not Do

* Do not copy VB.NET code directly.
* Do not build everything in one huge component.
* Do not hardcode connection strings.
* Do not store password in plain text if avoidable.
* Do not mix UI logic and database logic.
* Do not implement EventDB yet, only prepare adapter structure.

Suggested App Name

oph-control-studio

Suggested Package Names

@oph/studio
@oph/admin-service
@oph/metadata-adapter
Refs: Tauri supports cross-platform apps using a web frontend and Rust backend, while Vite supports React/TypeScript and fast HMR development.  [oai_citation:0‡Wikipedia](https://en.wikipedia.org/wiki/Tauri_%28software_framework%29?utm_source=chatgpt.com)  [oai_citation:1‡Wikipedia](https://en.wikipedia.org/wiki/Vite?utm_source=chatgpt.com)