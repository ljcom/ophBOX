import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Database,
  FileCode2,
  Gauge,
  KeyRound,
  Layers3,
  MonitorCog,
  Play,
  Search,
  Server,
  Settings,
  ShieldCheck,
  Table2,
  UserRoundCog,
} from 'lucide-react'
import { FormEvent, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { ophAdminService } from './services/mockOphAdminService'
import type {
  OphConnectionConfig,
  OphDatabase,
  MetadataRow,
  OphServer,
  OphTreeNode,
  TreeNodeKind,
  WorkspaceSelection,
} from './types/domain'

type MetricCardProps = {
  label: string
  value: string
  detail: string
}

type SectionHeaderProps = {
  eyebrow: string
  title: string
  description: string
  action?: string
  onAction?: () => void
}

const treeIcons: Record<TreeNodeKind, typeof Server> = {
  root: Server,
  server: Server,
  database: Database,
  modules: Layers3,
  'module-category': Table2,
  module: FileCode2,
  'module-action': Table2,
  'module-column': Table2,
  security: ShieldCheck,
  'security-user': UserRoundCog,
  'security-group': ShieldCheck,
  interface: MonitorCog,
  theme: MonitorCog,
  account: UserRoundCog,
}

function App() {
  const [connectionConfig, setConnectionConfig] = useState<OphConnectionConfig | null>(null)
  const [discoveredDatabases, setDiscoveredDatabases] = useState<OphDatabase[]>([])
  const [moduleRowsByDatabaseId, setModuleRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [columnRowsByDatabaseId, setColumnRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [subAccountRowsByDatabaseId, setSubAccountRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [themeRowsByDatabaseId, setThemeRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [userRowsByDatabaseId, setUserRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [userGroupRowsByDatabaseId, setUserGroupRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [isLoadingConfig, setIsLoadingConfig] = useState(true)
  const [initialConnectionError, setInitialConnectionError] = useState('')
  const tree = useMemo(
    () => {
      if (!connectionConfig) return null
      if (initialConnectionError) return ophAdminService.buildFallbackTree(connectionConfig)
      return ophAdminService.buildTree(
        connectionConfig,
        discoveredDatabases,
        moduleRowsByDatabaseId,
        columnRowsByDatabaseId,
        subAccountRowsByDatabaseId,
        themeRowsByDatabaseId,
        userRowsByDatabaseId,
        userGroupRowsByDatabaseId,
      )
    },
    [
      connectionConfig,
      discoveredDatabases,
      initialConnectionError,
      moduleRowsByDatabaseId,
      columnRowsByDatabaseId,
      subAccountRowsByDatabaseId,
      themeRowsByDatabaseId,
      userRowsByDatabaseId,
      userGroupRowsByDatabaseId,
    ],
  )
  const firstDatabase = tree?.children?.[0]?.children?.[0]
  const [selection, setSelection] = useState<WorkspaceSelection>(() => ({
    id: firstDatabase?.id ?? 'servers',
    label: firstDatabase?.label ?? 'Servers',
    kind: firstDatabase?.kind ?? 'root',
    description: firstDatabase?.description,
    accountId: firstDatabase?.accountId,
    databaseName: firstDatabase?.databaseName,
    databaseId: firstDatabase?.databaseId,
    serverId: firstDatabase?.serverId,
    moduleGuid: firstDatabase?.moduleGuid,
    columnGuid: firstDatabase?.columnGuid,
    themeGuid: firstDatabase?.themeGuid,
    userGuid: firstDatabase?.userGuid,
    userGroupGuid: firstDatabase?.userGroupGuid,
    settingMode: firstDatabase?.settingMode,
  }))

  const servers = connectionConfig?.servers ?? []

  async function loadModuleRowsByDatabase(
    config: OphConnectionConfig,
    databases: OphDatabase[],
  ): Promise<Record<string, MetadataRow[]>> {
    const entries = await Promise.all(
      databases.map(async (database) => {
        try {
          return [
            database.id,
            await ophAdminService.listModuleTree(config, database.name, database.databaseName),
          ] as const
        } catch {
          return [database.id, []] as const
        }
      }),
    )

    return Object.fromEntries(entries)
  }

  async function loadColumnRowsByDatabase(
    config: OphConnectionConfig,
    databases: OphDatabase[],
  ): Promise<Record<string, MetadataRow[]>> {
    const entries = await Promise.all(
      databases.map(async (database) => {
        try {
          return [
            database.id,
            await ophAdminService.listModuleColumnTree(config, database.name, database.databaseName),
          ] as const
        } catch {
          return [database.id, []] as const
        }
      }),
    )

    return Object.fromEntries(entries)
  }

  async function loadSubAccountRowsByDatabase(
    config: OphConnectionConfig,
    databases: OphDatabase[],
  ): Promise<Record<string, MetadataRow[]>> {
    const entries = await Promise.all(
      databases.map(async (database) => {
        try {
          return [
            database.id,
            await ophAdminService.listSubAccounts(config, database.name, database.databaseName),
          ] as const
        } catch {
          return [database.id, []] as const
        }
      }),
    )

    return Object.fromEntries(entries)
  }

  async function loadThemeRowsByDatabase(
    config: OphConnectionConfig,
    databases: OphDatabase[],
  ): Promise<Record<string, MetadataRow[]>> {
    const entries = await Promise.all(
      databases.map(async (database) => {
        try {
          return [
            database.id,
            await ophAdminService.listThemes(config, database.name, database.databaseName),
          ] as const
        } catch {
          return [database.id, []] as const
        }
      }),
    )

    return Object.fromEntries(entries)
  }

  async function loadUserRowsByDatabase(
    config: OphConnectionConfig,
    databases: OphDatabase[],
  ): Promise<Record<string, MetadataRow[]>> {
    const entries = await Promise.all(
      databases.map(async (database) => {
        try {
          return [
            database.id,
            await ophAdminService.listUsers(config, database.name, database.databaseName),
          ] as const
        } catch {
          return [database.id, []] as const
        }
      }),
    )

    return Object.fromEntries(entries)
  }

  async function loadUserGroupRowsByDatabase(
    config: OphConnectionConfig,
    databases: OphDatabase[],
  ): Promise<Record<string, MetadataRow[]>> {
    const entries = await Promise.all(
      databases.map(async (database) => {
        try {
          return [
            database.id,
            await ophAdminService.listUserGroups(config, database.name, database.databaseName),
          ] as const
        } catch {
          return [database.id, []] as const
        }
      }),
    )

    return Object.fromEntries(entries)
  }

  async function activateConnection(config: OphConnectionConfig) {
    const loadedDatabases = await ophAdminService.listOphDatabases(config)
    const loadedModuleRows = await loadModuleRowsByDatabase(config, loadedDatabases)
    const loadedColumnRows = await loadColumnRowsByDatabase(config, loadedDatabases)
    const loadedSubAccountRows = await loadSubAccountRowsByDatabase(config, loadedDatabases)
    const loadedThemeRows = await loadThemeRowsByDatabase(config, loadedDatabases)
    const loadedUserRows = await loadUserRowsByDatabase(config, loadedDatabases)
    const loadedUserGroupRows = await loadUserGroupRowsByDatabase(config, loadedDatabases)
    const loadedTree = ophAdminService.buildTree(
      config,
      loadedDatabases,
      loadedModuleRows,
      loadedColumnRows,
      loadedSubAccountRows,
      loadedThemeRows,
      loadedUserRows,
      loadedUserGroupRows,
    )
    const loadedDatabase = loadedTree.children?.[0]?.children?.[0]

    setDiscoveredDatabases(loadedDatabases)
    setModuleRowsByDatabaseId(loadedModuleRows)
    setColumnRowsByDatabaseId(loadedColumnRows)
    setSubAccountRowsByDatabaseId(loadedSubAccountRows)
    setThemeRowsByDatabaseId(loadedThemeRows)
    setUserRowsByDatabaseId(loadedUserRows)
    setUserGroupRowsByDatabaseId(loadedUserGroupRows)
    setInitialConnectionError('')
    setConnectionConfig(config)
    setSelection({
      id: loadedDatabase?.id ?? 'servers',
      label: loadedDatabase?.label ?? 'Servers',
      kind: loadedDatabase?.kind ?? 'root',
      description: loadedDatabase?.description,
      accountId: loadedDatabase?.accountId,
      databaseName: loadedDatabase?.databaseName,
      databaseId: loadedDatabase?.databaseId,
      serverId: loadedDatabase?.serverId,
      moduleGuid: loadedDatabase?.moduleGuid,
      columnGuid: loadedDatabase?.columnGuid,
      themeGuid: loadedDatabase?.themeGuid,
      userGuid: loadedDatabase?.userGuid,
      userGroupGuid: loadedDatabase?.userGroupGuid,
      settingMode: loadedDatabase?.settingMode,
    })
  }

  useEffect(() => {
    let isMounted = true

    async function loadInitialConnection() {
      const config = await ophAdminService.loadConnectionConfig()
      if (config && isMounted) {
        try {
          await activateConnection(config)
        } catch (error) {
          setInitialConnectionError(error instanceof Error ? error.message : String(error))
          setConnectionConfig(config)
          setDiscoveredDatabases([])
          setModuleRowsByDatabaseId({})
          setColumnRowsByDatabaseId({})
          setSubAccountRowsByDatabaseId({})
          setThemeRowsByDatabaseId({})
          setUserRowsByDatabaseId({})
          setUserGroupRowsByDatabaseId({})
        }
      }
    }

    loadInitialConnection()
      .finally(() => {
        if (isMounted) {
          setIsLoadingConfig(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [])

  async function saveConnection(config: OphConnectionConfig) {
    const savedConfig = await ophAdminService.saveConnectionConfig(config)
    await activateConnection(savedConfig)
  }

  async function refreshConnection() {
    if (!connectionConfig) return

    try {
      await activateConnection(connectionConfig)
    } catch (error) {
      setInitialConnectionError(error instanceof Error ? error.message : String(error))
    }
  }

  if (isLoadingConfig) {
    return (
      <main className="connection-screen">
        <section className="connection-card">
          <div className="brand-block connection-brand">
            <div className="brand-mark">OPH</div>
            <div>
              <strong>OPH Control Studio</strong>
              <span>Loading saved connection</span>
            </div>
          </div>
        </section>
      </main>
    )
  }

  if (!connectionConfig || !tree) {
    return <AddConnectionScreen initialError={initialConnectionError} onSave={saveConnection} />
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-block">
          <div className="brand-mark">OPH</div>
          <div>
            <strong>OPH Control Studio</strong>
            <span>Connection workspace</span>
          </div>
        </div>
        <TreeView root={tree} selectionId={selection.id} onSelect={setSelection} />
      </aside>

      <main className="workspace">
        <header className="topbar">
          <div className="search-box">
            <Search size={16} />
            <span>Search current OPH connection...</span>
          </div>
          <button className="primary-button">Test Connection</button>
        </header>
        <div className="content-grid content-grid-full">
          <section className="main-panel">
            <Workspace
              connectionConfig={connectionConfig}
              connectionError={initialConnectionError}
              onRefreshConnection={refreshConnection}
              selection={selection}
              servers={servers}
              tree={tree}
            />
          </section>
        </div>
      </main>
    </div>
  )
}

function AddConnectionScreen({
  initialError,
  onSave,
}: {
  initialError?: string
  onSave: (config: OphConnectionConfig) => void | Promise<void>
}) {
  const [authType, setAuthType] = useState<'sql' | 'windows'>('sql')
  const [testResult, setTestResult] = useState<string>('')
  const [saveError, setSaveError] = useState<string>('')
  const [isSaving, setIsSaving] = useState(false)

  function readServer(form: FormData): OphServer {
    return {
      id: 'srv-prod',
      name: String(form.get('name') || 'Production OPH'),
      host: String(form.get('host') || 'localhost'),
      port: Number(form.get('port') || 1433),
      authType,
      defaultDatabase: String(form.get('defaultDatabase') || 'oph_core'),
      username: authType === 'sql' ? String(form.get('username') || '') : undefined,
      password: authType === 'sql' ? String(form.get('password') || '') : undefined,
      trustServerCertificate: form.get('trustServerCertificate') === 'on',
      encrypt: form.get('encrypt') === 'on',
      status: 'online',
      databases: 1,
      lastChecked: 'Just now',
    }
  }

  async function submitConnection(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaveError('')
    setIsSaving(true)
    const server = readServer(new FormData(event.currentTarget))

    try {
      await onSave({ servers: [server], selectedServerId: server.id })
    } catch (error) {
      setSaveError(error instanceof Error ? error.message : String(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function testConnection(formElement: HTMLFormElement) {
    const server = readServer(new FormData(formElement))
    setTestResult('')
    setSaveError('')

    try {
      const result = await ophAdminService.testConnection(server)
      setTestResult(result.message)
      setSaveError(result.success ? '' : result.message)
      if (result.success) {
        await onSave({ servers: [server], selectedServerId: server.id })
      }
    } catch (error) {
      setSaveError(error instanceof Error ? error.message : String(error))
    }
  }

  return (
    <main className="connection-screen">
      <section className="connection-card">
        <div className="brand-block connection-brand">
          <div className="brand-mark">OPH</div>
          <div>
            <strong>OPH Control Studio</strong>
            <span>No saved connection found</span>
          </div>
        </div>
        <SectionHeader
          eyebrow="Add Connection"
          title="Connect to OPH core"
          description="Save a server profile first. After the connection exists, the workspace opens with a server and database tree."
        />
        <form className="connection-form" onSubmit={submitConnection}>
          <label>
            Connection Name
            <input name="name" defaultValue="Production OPH" />
          </label>
          <label>
            SQL Server Host
            <input name="host" defaultValue="10.10.1.20" />
          </label>
          <label>
            Port
            <input name="port" type="number" defaultValue={1433} />
          </label>
          <label>
            Default Database
            <input name="defaultDatabase" defaultValue="oph_core" />
          </label>
          <label>
            Authentication
            <select value={authType} onChange={(event) => setAuthType(event.target.value as 'sql' | 'windows')}>
              <option value="sql">SQL Login</option>
              <option value="windows">Windows Authentication</option>
            </select>
          </label>
          {authType === 'sql' ? (
            <>
              <label>
                Username
                <input name="username" defaultValue="oph_admin" />
              </label>
              <label>
                Password
                <input name="password" type="password" defaultValue="password" />
              </label>
            </>
          ) : null}
          <label className="check-row">
            <span>Encrypt connection</span>
            <input name="encrypt" type="checkbox" defaultChecked />
          </label>
          <label className="check-row">
            <span>Trust server certificate</span>
            <input name="trustServerCertificate" type="checkbox" defaultChecked />
          </label>
          <div className="connection-actions">
            <button className="primary-button" type="submit">
              {isSaving ? 'Testing Connection...' : 'Save Connection'}
            </button>
            <button
              className="ghost-button"
              type="button"
              onClick={(event) => {
                const form = event.currentTarget.form
                if (form) void testConnection(form)
              }}
            >
              Test Connection
            </button>
          </div>
          {testResult ? <div className="connection-result">{testResult}</div> : null}
          {initialError ? <div className="connection-error">{initialError}</div> : null}
          {saveError ? <div className="connection-error">{saveError}</div> : null}
        </form>
      </section>
    </main>
  )
}

function TreeView({
  root,
  selectionId,
  onSelect,
}: {
  root: OphTreeNode
  selectionId: string
  onSelect: (selection: WorkspaceSelection) => void
}) {
  return (
    <div className="tree-view">
      <TreeNodeView node={root} depth={0} selectionId={selectionId} onSelect={onSelect} />
    </div>
  )
}

function TreeNodeView({
  node,
  depth,
  selectionId,
  onSelect,
}: {
  node: OphTreeNode
  depth: number
  selectionId: string
  onSelect: (selection: WorkspaceSelection) => void
}) {
  const [expanded, setExpanded] = useState(depth < 2)
  const hasChildren = Boolean(node.children?.length)
  const Icon = treeIcons[node.kind]

  function selectNode() {
    if (hasChildren) setExpanded(true)
    onSelect({
      id: node.id,
      label: node.label,
      kind: node.kind,
      description: node.description,
      accountId: node.accountId,
      databaseName: node.databaseName,
      databaseId: node.databaseId,
      serverId: node.serverId,
      moduleGuid: node.moduleGuid,
      columnGuid: node.columnGuid,
      themeGuid: node.themeGuid,
      userGuid: node.userGuid,
      userGroupGuid: node.userGroupGuid,
      settingMode: node.settingMode,
    })
  }

  return (
    <div>
      <button
        className={`tree-node ${selectionId === node.id ? 'tree-node-active' : ''}`}
        style={{ paddingLeft: 10 + depth * 16 }}
        onClick={selectNode}
      >
        <span className="tree-expander" onClick={(event) => {
          event.stopPropagation()
          setExpanded(!expanded)
        }}>
          {hasChildren ? expanded ? <ChevronDown size={14} /> : <ChevronRight size={14} /> : null}
        </span>
        <Icon size={16} />
        <span>
          <strong>{node.label}</strong>
          {node.description ? <small>{node.description}</small> : null}
        </span>
      </button>
      {expanded && hasChildren ? (
        <div>
          {node.children?.map((child) => (
            <TreeNodeView
              key={child.id}
              node={child}
              depth={depth + 1}
              selectionId={selectionId}
              onSelect={onSelect}
            />
          ))}
        </div>
      ) : null}
    </div>
  )
}

function getModuleSettingMode(label: string): number | undefined {
  const settingModes: Record<string, number> = {
    Core: 0,
    Master: 1,
    Transaction: 4,
    Report: 5,
    Blank: 6,
    View: 7,
  }

  return settingModes[label]
}

function Workspace({
  connectionConfig,
  connectionError,
  onRefreshConnection,
  selection,
  servers,
  tree,
}: {
  connectionConfig: OphConnectionConfig
  connectionError: string
  onRefreshConnection: () => void | Promise<void>
  selection: WorkspaceSelection
  servers: OphServer[]
  tree: OphTreeNode
}) {
  if (connectionError) {
    return <ConnectionIssuePage error={connectionError} onRefresh={onRefreshConnection} />
  }

  if (selection.kind === 'server' || selection.kind === 'root') {
    return <ServersPage servers={servers} />
  }

  if (selection.kind === 'database') {
    return <DatabaseWorkspace selection={selection} tree={tree} />
  }

  if (selection.kind === 'module' && selection.moduleGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Module Info"
        sourceTable="modlinfo"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listModuleInfo(config, accountId, databaseName, selection.moduleGuid ?? '')
        }
      />
    )
  }

  if (selection.kind === 'module-action' && selection.label === 'Columns' && selection.moduleGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Columns"
        sourceTable="modlcolm"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listModuleColumns(config, accountId, databaseName, selection.moduleGuid ?? '')
        }
      />
    )
  }

  if (selection.kind === 'module-column' && selection.columnGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Column Info"
        sourceTable="modlcolminfo"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listModuleColumnInfo(config, accountId, databaseName, selection.columnGuid ?? '')
        }
      />
    )
  }

  if (selection.kind === 'module-action' && selection.label === 'Children' && selection.moduleGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Children"
        sourceTable="modl"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listChildModules(config, accountId, databaseName, selection.moduleGuid ?? '')
        }
      />
    )
  }

  if (selection.kind === 'module-action' && selection.label === 'Approvals' && selection.moduleGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Approvals"
        sourceTable="modlappr"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listModuleApprovals(config, accountId, databaseName, selection.moduleGuid ?? '')
        }
      />
    )
  }

  if (selection.kind === 'module-action' && selection.label === 'Numbering' && selection.moduleGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Numbering"
        sourceTable="modldocn"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listModuleNumbering(config, accountId, databaseName, selection.moduleGuid ?? '')
        }
      />
    )
  }

  if (selection.kind === 'module-action' && selection.label === 'Mails' && selection.moduleGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Mails"
        sourceTable="modlmail"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listModuleMails(config, accountId, databaseName, selection.moduleGuid ?? '')
        }
      />
    )
  }

  if (selection.kind === 'module' || selection.kind === 'module-action' || selection.kind === 'module-column') {
    return <DomainWorkspace icon={<FileCode2 size={22} />} selection={selection} title="Module" />
  }

  if (selection.kind === 'modules' || selection.kind === 'module-category') {
    if (selection.label === 'Module Status') {
      return (
        <MetadataWorkspace
          config={connectionConfig}
          selection={selection}
          title="Module Status"
          sourceTable="msta"
          loadRows={ophAdminService.listModuleStatuses}
        />
      )
    }

    if (selection.label === 'Module Groups') {
      return (
        <MetadataWorkspace
          config={connectionConfig}
          selection={selection}
          title="Module Groups"
          sourceTable="modg"
          loadRows={ophAdminService.listModuleGroups}
        />
      )
    }

    const settingMode = getModuleSettingMode(selection.label)
    if (settingMode !== undefined) {
      return (
        <MetadataWorkspace
          config={connectionConfig}
          selection={selection}
          title="Modules"
          sourceTable="modl"
          loadRows={(config, accountId, databaseName) =>
            ophAdminService.listModulesBySettingMode(config, accountId, databaseName, settingMode)
          }
        />
      )
    }

    return <DomainWorkspace icon={<Layers3 size={22} />} selection={selection} title="Modules" />
  }

  if (selection.kind === 'security') {
    if (selection.label === 'Users') {
      return (
        <MetadataWorkspace
          config={connectionConfig}
          selection={selection}
          title="Users"
          sourceTable="[user]"
          loadRows={ophAdminService.listUsers}
        />
      )
    }

    if (selection.label === 'User Groups') {
      return (
        <MetadataWorkspace
          config={connectionConfig}
          selection={selection}
          title="User Groups"
          sourceTable="ugrp"
          loadRows={ophAdminService.listUserGroups}
        />
      )
    }

    return <DomainWorkspace icon={<ShieldCheck size={22} />} selection={selection} title="Security" />
  }

  if (selection.kind === 'security-user' && selection.userGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="User Info"
        sourceTable="userinfo"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listUserInfo(config, accountId, databaseName, selection.userGuid ?? '')
        }
      />
    )
  }

  if (selection.kind === 'security-group' && selection.userGroupGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="User Group Modules"
        sourceTable="ugrpmodl"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listUserGroupModules(config, accountId, databaseName, selection.userGroupGuid ?? '')
        }
      />
    )
  }

  if (selection.kind === 'interface') {
    if (selection.label === 'Themes') {
      return (
        <MetadataWorkspace
          config={connectionConfig}
          selection={selection}
          title="Themes"
          sourceTable="thme"
          loadRows={ophAdminService.listThemes}
        />
      )
    }

    if (selection.label === 'Menus') {
      return (
        <MetadataWorkspace
          config={connectionConfig}
          selection={selection}
          title="Menus"
          sourceTable="menu"
          loadRows={ophAdminService.listMenus}
        />
      )
    }

    if (selection.label === 'Translator') {
      return (
        <MetadataWorkspace
          config={connectionConfig}
          selection={selection}
          title="Translator"
          sourceTable="word"
          loadRows={ophAdminService.listTranslatorWords}
        />
      )
    }

    return <DomainWorkspace icon={<MonitorCog size={22} />} selection={selection} title="Interface" />
  }

  if (selection.kind === 'theme' && selection.themeGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Theme Pages"
        sourceTable="thmepage"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listThemePages(config, accountId, databaseName, selection.themeGuid ?? '')
        }
      />
    )
  }

  if (selection.kind === 'account' && selection.label === 'Account') {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Account"
        sourceTable="acctinfo"
        loadRows={ophAdminService.listAccountInfo}
      />
    )
  }

  if (selection.kind === 'account' && selection.label === 'Sub Accounts') {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Sub Accounts"
        sourceTable="acct"
        loadRows={ophAdminService.listSubAccounts}
      />
    )
  }

  if (selection.kind === 'account' && selection.label === 'Databases') {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Databases"
        sourceTable="acctdbse"
        loadRows={ophAdminService.listAccountDatabases}
      />
    )
  }



  if (selection.kind === 'account' && selection.label === 'Parameters') {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Parameters"
        sourceTable="para"
        loadRows={ophAdminService.listParameters}
      />
    )
  }

  if (selection.kind === 'account' && selection.label === 'Widgets') {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Widgets"
        sourceTable="widg"
        loadRows={ophAdminService.listWidgets}
      />
    )
  }

  if (selection.kind === 'account' && selection.label === 'Mail') {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Mail"
        sourceTable="mail"
        loadRows={ophAdminService.listMailProfiles}
      />
    )
  }

  if (selection.kind === 'account') {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Sub Account"
        sourceTable="acctinfo"
        loadRows={ophAdminService.listAccountInfo}
      />
    )
  }

  return <DomainWorkspace icon={<UserRoundCog size={22} />} selection={selection} title="Account" />
}

function ConnectionIssuePage({ error, onRefresh }: { error: string; onRefresh: () => void | Promise<void> }) {
  const [isRefreshing, setIsRefreshing] = useState(false)

  async function refreshConnection() {
    setIsRefreshing(true)
    try {
      await onRefresh()
    } finally {
      setIsRefreshing(false)
    }
  }

  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Connection Issue"
        title="Connection saved, but currently unavailable"
        description="The saved connection config is kept. Fix the network or SQL Server issue, then restart or refresh the app."
        action={isRefreshing ? 'Checking...' : 'Refresh Connection'}
        onAction={refreshConnection}
      />
      <div className="connection-error">{error}</div>
    </div>
  )
}


function SectionHeader({ eyebrow, title, description, action, onAction }: SectionHeaderProps) {
  return (
    <div className="section-header">
      <div>
        <span className="eyebrow">{eyebrow}</span>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      {action ? <button className="primary-button" onClick={onAction}>{action}</button> : null}
    </div>
  )
}

function MetricCard({ label, value, detail }: MetricCardProps) {
  return (
    <article className="metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{detail}</small>
    </article>
  )
}

function ServersPage({ servers }: { servers: OphServer[] }) {
  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Servers"
        title="Saved OPH connections"
        description="Connection config is loaded before the workspace opens. Use the tree to select a server or database."
        action="Add Connection"
      />
      <div className="table-card">
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Host</th>
              <th>Auth</th>
              <th>Databases</th>
              <th>Status</th>
              <th>Last Checked</th>
            </tr>
          </thead>
          <tbody>
            {servers.map((server) => (
              <tr key={server.id}>
                <td><strong>{server.name}</strong></td>
                <td>{server.host}:{server.port}</td>
                <td>{server.authType === 'sql' ? 'SQL Login' : 'Windows Auth'}</td>
                <td>{server.databases}</td>
                <td><span className={getStatusClass(server.status)}>{server.status}</span></td>
                <td>{server.lastChecked}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function findTreeNode(root: OphTreeNode, nodeId: string): OphTreeNode | undefined {
  if (root.id === nodeId) return root

  for (const child of root.children ?? []) {
    const result = findTreeNode(child, nodeId)
    if (result) return result
  }

  return undefined
}

function countNodes(node: OphTreeNode | undefined, predicate: (node: OphTreeNode) => boolean): number {
  if (!node) return 0

  const selfCount = predicate(node) ? 1 : 0
  return selfCount + (node.children ?? []).reduce((total, child) => total + countNodes(child, predicate), 0)
}

function countLeafChildren(node: OphTreeNode | undefined): number {
  if (!node) return 0
  return node.children?.length ?? 0
}

function DatabaseWorkspace({ selection, tree }: { selection: WorkspaceSelection; tree: OphTreeNode }) {
  const databaseNode = findTreeNode(tree, selection.id)
  const modulesNode = databaseNode?.children?.find((child) => child.label === 'Modules')
  const securityNode = databaseNode?.children?.find((child) => child.label === 'Security')
  const interfaceNode = databaseNode?.children?.find((child) => child.label === 'Interface')
  const accountNode = databaseNode?.children?.find((child) => child.label === 'Account')

  const moduleCount = countNodes(modulesNode, (node) => node.kind === 'module')
  const securityCount = countLeafChildren(securityNode)
  const interfaceCount = countLeafChildren(interfaceNode)
  const accountCount = countLeafChildren(accountNode)

  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Database"
        title={selection.label}
        description="This database was listed from OPH core account metadata. Domain groups are available under this database node."
        action="Refresh Database"
      />
      <div className="metrics-grid">
        <MetricCard label="Modules" value={String(moduleCount)} detail="Grouped by setting mode" />
        <MetricCard label="Security" value={String(securityCount)} detail="Users and groups" />
        <MetricCard label="Interface" value={String(interfaceCount)} detail="Themes, menus, translator" />
        <MetricCard label="Account" value={String(accountCount)} detail="Parameters and mail" />
      </div>
      <div className="panel-card">
        <h2>Database workflow</h2>
        <ul className="timeline-list">
          <li><CheckCircle2 size={16} />Connection config loaded.</li>
          <li><CheckCircle2 size={16} />Database list read from OPH core.</li>
          <li><Activity size={16} />Select Modules, Security, Interface, or Account to continue.</li>
        </ul>
      </div>
    </div>
  )
}

function DomainWorkspace({
  icon,
  selection,
  title,
}: {
  icon: ReactNode
  selection: WorkspaceSelection
  title: string
}) {
  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow={title}
        title={selection.label}
        description="This area mirrors the legacy tree flow, with actions and detail editing moved into a cleaner workspace."
        action="Refresh"
      />
      <div className="domain-grid">
        <article className="domain-card">{icon}<strong>Browse</strong><span>Open records for this area.</span></article>
        <article className="domain-card"><FileCode2 size={22} /><strong>Edit Details</strong><span>Update metadata using a focused form.</span></article>
        <article className="domain-card"><KeyRound size={22} /><strong>Validate</strong><span>Check required fields and references.</span></article>
      </div>
    </div>
  )
}

function MetadataWorkspace({
  config,
  selection,
  title,
  sourceTable,
  loadRows,
}: {
  config: OphConnectionConfig
  selection: WorkspaceSelection
  title: string
  sourceTable: string
  loadRows: (config: OphConnectionConfig, accountId: string, databaseName: string) => Promise<MetadataRow[]>
}) {
  const [rows, setRows] = useState<MetadataRow[]>([])
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    const accountId = selection.accountId
    const databaseName = selection.databaseName

    if (!accountId || !databaseName) {
      setRows([])
      setError('No account database is selected.')
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setError('')
    loadRows(config, accountId, databaseName)
      .then((loadedRows) => {
        if (isMounted) setRows(loadedRows)
      })
      .catch((loadError) => {
        if (isMounted) setError(loadError instanceof Error ? loadError.message : String(loadError))
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [config, loadRows, selection.accountId, selection.databaseName])

  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow={title}
        title={selection.label}
        description={`Loaded from ${sourceTable} in ${selection.databaseName}.`}
        action="Refresh"
      />
      {isLoading ? <div className="empty-result">Loading metadata...</div> : null}
      {error ? <div className="connection-error">{error}</div> : null}
      {!isLoading && !error ? (
        <MetadataTable config={config} rows={rows} selection={selection} sourceTable={sourceTable} />
      ) : null}
    </div>
  )
}

function MetadataTable({
  config,
  rows,
  selection,
  sourceTable,
}: {
  config: OphConnectionConfig
  rows: MetadataRow[]
  selection: WorkspaceSelection
  sourceTable: string
}) {
  const [tableRows, setTableRows] = useState<MetadataRow[]>(rows)
  const [selectedRow, setSelectedRow] = useState<MetadataRow | null>(null)
  const [selectedRowIndex, setSelectedRowIndex] = useState<number | null>(null)
  const [draftRow, setDraftRow] = useState<Record<string, string>>({})
  const [isEditing, setIsEditing] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [actionError, setActionError] = useState('')
  const visibleColumnMap: Record<string, string[]> = {
    '[user]': ['userid', 'username', 'email', 'expirydate'],
    userinfo: ['infokey', 'infovalue'],
    ugrp: ['groupid', 'groupdescription'],
    ugrpmodl: ['moduleguid', 'allowaccess', 'allowadd', 'allowedit', 'allowdelete', 'allowforce', 'allowwipe'],
    msta: ['modulestatusname', 'isdefault', 'createddate', 'updateddate'],
    modg: ['modulegroupid', 'modulegroupname', 'modulegroupdescription'],
    thme: ['themecode', 'themename', 'themefolder'],
    thmepage: ['pageurl', 'isdefault'],
    menu: ['menucode', 'menudescription', 'createddate', 'updateddate'],
    word: ['originstatements', 'createddate', 'updateddate'],
    para: ['parameterid', 'parameterdescription', 'createddate', 'updateddate'],
    widg: ['widgetid', 'createddate', 'updateddate'],
    mail: ['profilename', 'accountname', 'displayname', 'emailaddress', 'bcc', 'createddate', 'updateddate'],
    modlinfo: ['infokey', 'infovalue'],
    modlcolm: ['colkey', 'coltype', 'titlecaption', 'colorder', 'collength'],
    modlcolminfo: ['infokey', 'infovalue'],
    modlappr: ['approvalgroupguid', 'uppergroupguid', 'lvl', 'sqlfilter', 'zonegroup'],
    modldocn: ['format', 'month', 'no'],
    modlmail: ['mailguid', 'actionguid', 'tokenstatus', 'additional', 'cc', 'subject', 'body', 'reportattachment', 'definedtable'],
    modl: [
      'moduleid',
      'moduledescription',
      'settingmode',
      'accountdb',
      'parentmodule',
      'orderno',
      'needlogin',
      'themepage',
      'modulestatus',
      'modulegroup',
    ],
  }
  const columnLabels: Record<string, string> = {
    userid: 'User ID',
    username: 'User Name',
    email: 'Email',
    expirydate: 'Expiry Date',
    groupid: 'Group ID',
    groupdescription: 'Group Description',
    allowaccess: 'Allow Access',
    allowadd: 'Allow Add',
    allowedit: 'Allow Edit',
    allowdelete: 'Allow Delete',
    allowforce: 'Allow Force',
    allowwipe: 'Allow Wipe',
    modulestatusname: 'Module Status Name',
    isdefault: 'Is Default',
    modulegroupid: 'Module Group ID',
    modulegroupname: 'Module Group Name',
    modulegroupdescription: 'Module Group Description',
    themecode: 'Theme Code',
    themename: 'Theme Name',
    themefolder: 'Theme Folder',
    pageurl: 'Page URL',
    menuid: 'Menu ID',
    menucode: 'Menu Code',
    menudescription: 'Menu Description',
    createddate: 'Created Date',
    updateddate: 'Updated Date',
    originstatements: 'Origin Statements',
    parameterid: 'Parameter ID',
    parameterdescription: 'Parameter Description',
    widgetid: 'Widget ID',
    profilename: 'Profile Name',
    accountname: 'Account Name',
    displayname: 'Display Name',
    emailaddress: 'Email Address',
    bcc: 'BCC',
    colkey: 'Column Key',
    coltype: 'Column Type',
    titlecaption: 'Title Caption',
    colorder: 'Column Order',
    collength: 'Column Length',
    infokey: 'Info Key',
    infovalue: 'Info Value',
    approvalgroupguid: 'Approval Group',
    uppergroupguid: 'Upper Group',
    lvl: 'Level',
    sqlfilter: 'SQL Filter',
    zonegroup: 'Zone Group',
    format: 'Format',
    month: 'Month',
    no: 'No',
    mailguid: 'Mail',
    actionguid: 'Action',
    tokenstatus: 'Token Status',
    additional: 'Additional',
    cc: 'CC',
    subject: 'Subject',
    body: 'Body',
    reportattachment: 'Report Attachment',
    definedtable: 'Defined Table',
    moduleid: 'Module ID',
    moduleguid: 'Module',
    moduledescription: 'Module Description',
    settingmode: 'Setting Mode',
    accountdb: 'Account DB',
    parentmodule: 'Parent Module',
    orderno: 'Order No',
    needlogin: 'Need Login',
    themepage: 'Theme Page',
    modulestatus: 'Module Status',
    modulegroup: 'Module Group',
  }
  const hiddenColumns = new Set([
    'accountguid',
    'accountinfoguid',
    'accountdbguid',
    'accountid',
    'userguid',
    'ugroupguid',
    'userinfoguid',
    'accessguid',
    'parentaccountguid',
    'moduleguid',
    'columnguid',
    'moduleinfoguid',
    'columninfoguid',
    'approvalguid',
    'docnumberguid',
    'modulemailguid',
    'parentmoduleguid',
    'accountdbguid',
    'themepageguid',
    'themeguid',
    'password',
    'lockmode',
  ])
  useEffect(() => {
    setTableRows(rows)
    setSelectedRow(null)
    setSelectedRowIndex(null)
    setDraftRow({})
    setIsEditing(false)
    setIsCreating(false)
    setActionError('')
  }, [rows])

  const rowColumns = Array.from(new Set(tableRows.flatMap((row) => Object.keys(row))))
  const sourceKey = sourceTable.toLowerCase()
  const allowedColumns = visibleColumnMap[sourceKey]
  const columns = allowedColumns
    ? rowColumns.length === 0
      ? allowedColumns
      : allowedColumns.filter((column) => rowColumns.some((rowColumn) => rowColumn.toLowerCase() === column))
    : rowColumns.filter((column) => {
      const normalizedColumn = column.toLowerCase()
      return !hiddenColumns.has(normalizedColumn) && !normalizedColumn.endsWith('guid')
    })

  function getCellValue(row: MetadataRow, column: string) {
    const actualColumn = Object.keys(row).find((rowColumn) => rowColumn.toLowerCase() === column)
    return actualColumn ? row[actualColumn] : ''
  }
  const overlayColumns = columns.filter((column) => !['createddate', 'updateddate'].includes(column.toLowerCase()))

  function openRow(row: MetadataRow, index: number) {
    setSelectedRow(row)
    setSelectedRowIndex(index)
    setDraftRow(Object.fromEntries(overlayColumns.map((column) => [column, String(getCellValue(row, column) ?? '')])))
    setIsEditing(true)
    setIsCreating(false)
    setActionError('')
  }

  function openCreate() {
    setSelectedRow({})
    setSelectedRowIndex(null)
    setDraftRow(Object.fromEntries(overlayColumns.map((column) => [column, ''])))
    setIsEditing(true)
    setIsCreating(true)
    setActionError('')
  }

  async function saveDraft() {
    if (!selection.databaseName) {
      setActionError('Cannot save row: database is missing.')
      return
    }

    const nextRow = { ...(selectedRow ?? {}) }
    overlayColumns.forEach((column) => {
      nextRow[column] = draftRow[column] ?? ''
    })

    try {
      await ophAdminService.saveMetadataRow(
        config,
        selection.databaseName,
        sourceTable,
        selectedRow ?? {},
        nextRow,
        selection.moduleGuid,
        selection.columnGuid,
        selection.themeGuid,
        selection.userGuid,
        selection.userGroupGuid,
      )
    } catch (saveError) {
      setActionError(saveError instanceof Error ? saveError.message : String(saveError))
      return
    }

    if (isCreating || selectedRowIndex === null) {
      setTableRows((currentRows) => [...currentRows, nextRow])
      setSelectedRowIndex(tableRows.length)
    } else {
      setTableRows((currentRows) => currentRows.map((row, index) => (index === selectedRowIndex ? nextRow : row)))
    }

    setSelectedRow(null)
    setSelectedRowIndex(null)
    setDraftRow({})
    setIsEditing(false)
    setIsCreating(false)
    setActionError('')
  }

  function cancelEdit() {
    setSelectedRow(null)
    setSelectedRowIndex(null)
    setDraftRow({})
    setIsEditing(false)
    setIsCreating(false)
    setActionError('')
  }

  async function deleteSelectedRow() {
    if (selectedRowIndex === null) {
      setSelectedRow(null)
      setDraftRow({})
      setIsCreating(false)
      setIsEditing(false)
      return
    }

    if (!selection.databaseName || !selectedRow) {
      setActionError('Cannot delete row: database or row is missing.')
      return
    }

    try {
      await ophAdminService.deleteMetadataRow(config, selection.databaseName, sourceTable, selectedRow)
    } catch (deleteError) {
      setActionError(deleteError instanceof Error ? deleteError.message : String(deleteError))
      return
    }

    setTableRows((currentRows) => currentRows.filter((_, index) => index !== selectedRowIndex))
    setSelectedRow(null)
    setSelectedRowIndex(null)
    setDraftRow({})
    setIsEditing(false)
    setIsCreating(false)
    setActionError('')
  }

  return (
    <div className="metadata-table-shell">
      <div className="metadata-toolbar">
        <button type="button" onClick={openCreate}>Add</button>
      </div>
      <div className="table-card metadata-table">
        {tableRows.length === 0 ? (
          <div className="empty-result">No rows found.</div>
        ) : (
          <table>
            <thead>
              <tr>
                {columns.map((column) => (
                  <th key={column}>{columnLabels[column.toLowerCase()] ?? column}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {tableRows.map((row, index) => (
                <tr
                  className={`clickable-row ${selectedRowIndex === index ? 'selected-row' : ''}`}
                  key={index}
                  onClick={() => openRow(row, index)}
                >
                  {columns.map((column) => (
                    <td key={column}>{String(getCellValue(row, column) ?? '')}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
      {selectedRow ? (
        <aside className="row-detail-overlay">
          <div className="row-detail-header">
            <div>
              <span className="eyebrow">{isCreating ? 'New Row' : 'Selected Row'}</span>
              <h2>{sourceTable}</h2>
            </div>
            <button className="overlay-close-button" type="button" onClick={() => {
              cancelEdit()
            }}>
              ×
            </button>
          </div>
          <form className="row-detail-form">
            {overlayColumns.map((column) => (
              <label key={column}>
                <span>{columnLabels[column.toLowerCase()] ?? column}</span>
                {(draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')).length > 80 ? (
                  <textarea
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    readOnly={!isEditing}
                    rows={4}
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  />
                ) : (
                  <input
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    readOnly={!isEditing}
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  />
                )}
              </label>
            ))}
          </form>
          <div className="row-detail-actions">
            <button type="button" onClick={saveDraft}>Save</button>
            <button type="button" onClick={cancelEdit}>Cancel</button>
            <button className="danger-button" type="button" onClick={deleteSelectedRow}>Delete</button>
          </div>
          {actionError ? <div className="connection-error">{actionError}</div> : null}
        </aside>
      ) : null}
    </div>
  )
}


function getStatusClass(status: string): string {
  if (['online', 'healthy', 'active'].includes(status)) return 'status status-good'
  if (['warning', 'needs-review', 'review'].includes(status)) return 'status status-warning'
  return 'status status-bad'
}

export default App
