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
  X,
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

type QueryTab = {
  id: string
  title: string
  databaseName: string
  sql: string
  results: MetadataRow[]
  error: string
  isRunning: boolean
  isSaving?: boolean
  saveNotice?: string
  metadataSource?: {
    sourceTable: 'modlinfo'
    row: MetadataRow
  }
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
  menu: MonitorCog,
  parameter: Table2,
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
  const [menuRowsByDatabaseId, setMenuRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [subAccountUserRowsByDatabaseId, setSubAccountUserRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [parameterRowsByDatabaseId, setParameterRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [moduleStatusRowsByDatabaseId, setModuleStatusRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [isLoadingConfig, setIsLoadingConfig] = useState(true)
  const [isAddingConnection, setIsAddingConnection] = useState(false)
  const [initialConnectionError, setInitialConnectionError] = useState('')
  const [queryTabs, setQueryTabs] = useState<QueryTab[]>([])
  const [activeTabId, setActiveTabId] = useState('main')
  const [openAppMenu, setOpenAppMenu] = useState<'view' | 'window' | null>(null)
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
        menuRowsByDatabaseId,
        subAccountUserRowsByDatabaseId,
        parameterRowsByDatabaseId,
        moduleStatusRowsByDatabaseId,
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
      menuRowsByDatabaseId,
      subAccountUserRowsByDatabaseId,
      parameterRowsByDatabaseId,
      moduleStatusRowsByDatabaseId,
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

  function openNewQuery() {
    const queryNumber = queryTabs.length + 1
    const id = `query-${Date.now()}`
    const databaseName = selection.databaseName ?? discoveredDatabases[0]?.databaseName ?? ''
    setQueryTabs((tabs) => [...tabs, {
      id,
      title: `Query ${queryNumber}`,
      databaseName,
      sql: 'select top 100 *\nfrom ',
      results: [],
      error: '',
      isRunning: false,
    }])
    setActiveTabId(id)
    setOpenAppMenu(null)
  }

  function updateQueryTab(id: string, changes: Partial<QueryTab>) {
    setQueryTabs((tabs) => tabs.map((tab) => tab.id === id ? { ...tab, ...changes } : tab))
  }

  useEffect(() => {
    function handleOpenMetadataQuery(event: Event) {
      const detail = (event as CustomEvent<{ databaseName: string; row: MetadataRow }>).detail
      const moduleInfoGuid = String(detail.row.moduleinfoguid ?? '')
      const infoKey = String(detail.row.infokey ?? 'modlinfo')
      if (!moduleInfoGuid || !detail.databaseName) return
      const id = `modlinfo-query-${moduleInfoGuid}`
      setQueryTabs((tabs) => {
        if (tabs.some((tab) => tab.id === id)) return tabs
        return [...tabs, {
          id,
          title: `${infoKey} [${moduleInfoGuid.slice(0, 8)}]`,
          databaseName: detail.databaseName,
          sql: String(detail.row.infovalue ?? ''),
          results: [],
          error: '',
          isRunning: false,
          metadataSource: { sourceTable: 'modlinfo', row: detail.row },
        }]
      })
      setActiveTabId(id)
    }

    window.addEventListener('oph:open-metadata-query', handleOpenMetadataQuery)
    return () => window.removeEventListener('oph:open-metadata-query', handleOpenMetadataQuery)
  }, [])

  function closeQueryTab(id: string) {
    setQueryTabs((tabs) => tabs.filter((tab) => tab.id !== id))
    if (activeTabId === id) setActiveTabId('main')
  }

  async function runQueryTab(tab: QueryTab) {
    if (!connectionConfig) return
    updateQueryTab(tab.id, { isRunning: true, error: '' })
    try {
      const results = await ophAdminService.runQuery(connectionConfig, tab.databaseName, tab.sql)
      updateQueryTab(tab.id, { results, isRunning: false })
    } catch (queryError) {
      updateQueryTab(tab.id, {
        results: [],
        isRunning: false,
        error: queryError instanceof Error ? queryError.message : String(queryError),
      })
    }
  }

  async function saveQueryTab(tab: QueryTab) {
    if (!connectionConfig || !tab.metadataSource) return
    updateQueryTab(tab.id, { isSaving: true, error: '', saveNotice: '' })
    const originalRow = tab.metadataSource.row
    const nextRow = { ...originalRow, infovalue: tab.sql }
    try {
      await ophAdminService.saveMetadataRow(
        connectionConfig,
        tab.databaseName,
        tab.metadataSource.sourceTable,
        originalRow,
        nextRow,
      )
      updateQueryTab(tab.id, {
        isSaving: false,
        saveNotice: `Saved directly to ${String(originalRow.infokey ?? 'modlinfo')}.`,
        metadataSource: { ...tab.metadataSource, row: nextRow },
      })
    } catch (saveError) {
      updateQueryTab(tab.id, {
        isSaving: false,
        error: saveError instanceof Error ? saveError.message : String(saveError),
      })
    }
  }

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

  async function loadSubAccountUserRowsByDatabase(
    config: OphConnectionConfig,
    databases: OphDatabase[],
  ): Promise<Record<string, MetadataRow[]>> {
    const entries = await Promise.all(
      databases.map(async (database) => {
        try {
          return [
            database.id,
            await ophAdminService.listSubAccountUsers(config, database.name, database.databaseName),
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

  async function loadMenuRowsByDatabase(
    config: OphConnectionConfig,
    databases: OphDatabase[],
  ): Promise<Record<string, MetadataRow[]>> {
    const entries = await Promise.all(
      databases.map(async (database) => {
        try {
          return [database.id, await ophAdminService.listMenus(config, database.name, database.databaseName)] as const
        } catch {
          return [database.id, []] as const
        }
      }),
    )

    return Object.fromEntries(entries)
  }

  async function loadParameterRowsByDatabase(
    config: OphConnectionConfig,
    databases: OphDatabase[],
  ): Promise<Record<string, MetadataRow[]>> {
    const entries = await Promise.all(
      databases.map(async (database) => {
        try {
          return [database.id, await ophAdminService.listParameters(config, database.name, database.databaseName)] as const
        } catch {
          return [database.id, []] as const
        }
      }),
    )

    return Object.fromEntries(entries)
  }

  async function loadModuleStatusRowsByDatabase(
    config: OphConnectionConfig,
    databases: OphDatabase[],
  ): Promise<Record<string, MetadataRow[]>> {
    const entries = await Promise.all(
      databases.map(async (database) => {
        try {
          return [database.id, await ophAdminService.listModuleStatuses(config, database.name, database.databaseName)] as const
        } catch {
          return [database.id, []] as const
        }
      }),
    )

    return Object.fromEntries(entries)
  }

  async function activateConnection(config: OphConnectionConfig, preferredAccountId?: string) {
    const loadedDatabases = await ophAdminService.listOphDatabases(config)
    const loadedModuleRows = await loadModuleRowsByDatabase(config, loadedDatabases)
    const loadedColumnRows = await loadColumnRowsByDatabase(config, loadedDatabases)
    const loadedSubAccountRows = await loadSubAccountRowsByDatabase(config, loadedDatabases)
    const loadedSubAccountUserRows = await loadSubAccountUserRowsByDatabase(config, loadedDatabases)
    const loadedThemeRows = await loadThemeRowsByDatabase(config, loadedDatabases)
    const loadedUserRows = await loadUserRowsByDatabase(config, loadedDatabases)
    const loadedUserGroupRows = await loadUserGroupRowsByDatabase(config, loadedDatabases)
    const loadedMenuRows = await loadMenuRowsByDatabase(config, loadedDatabases)
    const loadedParameterRows = await loadParameterRowsByDatabase(config, loadedDatabases)
    const loadedModuleStatusRows = await loadModuleStatusRowsByDatabase(config, loadedDatabases)
    const loadedTree = ophAdminService.buildTree(
      config,
      loadedDatabases,
      loadedModuleRows,
      loadedColumnRows,
      loadedSubAccountRows,
      loadedThemeRows,
      loadedUserRows,
      loadedUserGroupRows,
      loadedMenuRows,
      loadedSubAccountUserRows,
      loadedParameterRows,
      loadedModuleStatusRows,
    )
    const loadedDatabase = loadedTree.children?.[0]?.children?.find((database) =>
      preferredAccountId
        ? database.accountId?.toLowerCase() === preferredAccountId.toLowerCase()
        : true)
      ?? loadedTree.children?.[0]?.children?.[0]

    setDiscoveredDatabases(loadedDatabases)
    setModuleRowsByDatabaseId(loadedModuleRows)
    setColumnRowsByDatabaseId(loadedColumnRows)
    setSubAccountRowsByDatabaseId(loadedSubAccountRows)
    setSubAccountUserRowsByDatabaseId(loadedSubAccountUserRows)
    setThemeRowsByDatabaseId(loadedThemeRows)
    setUserRowsByDatabaseId(loadedUserRows)
    setUserGroupRowsByDatabaseId(loadedUserGroupRows)
    setMenuRowsByDatabaseId(loadedMenuRows)
    setParameterRowsByDatabaseId(loadedParameterRows)
    setModuleStatusRowsByDatabaseId(loadedModuleStatusRows)
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
          setSubAccountUserRowsByDatabaseId({})
          setThemeRowsByDatabaseId({})
          setUserRowsByDatabaseId({})
          setUserGroupRowsByDatabaseId({})
          setMenuRowsByDatabaseId({})
          setParameterRowsByDatabaseId({})
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

  async function addConnection(config: OphConnectionConfig) {
    if (!connectionConfig) {
      await saveConnection(config)
      setIsAddingConnection(false)
      return
    }

    const newServer = config.servers[0]
    if (!newServer) return
    const mergedConfig: OphConnectionConfig = {
      servers: [...connectionConfig.servers.filter((server) => server.id !== newServer.id), newServer],
      selectedServerId: newServer.id,
    }
    await saveConnection(mergedConfig)
    setIsAddingConnection(false)
  }

  async function refreshConnection() {
    if (!connectionConfig) return

    try {
      await activateConnection(connectionConfig)
    } catch (error) {
      setInitialConnectionError(error instanceof Error ? error.message : String(error))
    }
  }

  async function refreshServerConnection(serverId: string, expectedAccountId?: string) {
    if (!connectionConfig) return
    const nextConfig = { ...connectionConfig, selectedServerId: serverId }
    const savedConfig = await ophAdminService.saveConnectionConfig(nextConfig)

    if (expectedAccountId) {
      const maximumAttempts = 30
      for (let attempt = 0; attempt < maximumAttempts; attempt += 1) {
        const databases = await ophAdminService.listOphDatabases(savedConfig)
        const accountIsReady = databases.some((database) =>
          database.name.toLowerCase() === expectedAccountId.toLowerCase())
        if (accountIsReady) {
          await activateConnection(savedConfig, expectedAccountId)
          return
        }
        await new Promise((resolve) => window.setTimeout(resolve, 1000))
      }

      await activateConnection(savedConfig)
      throw new Error(`Account ${expectedAccountId} was created, but its databases are not ready yet. Refresh the server tree again.`)
    }

    await activateConnection(savedConfig)
  }

  async function deleteAccount(serverId: string, accountId: string) {
    if (!connectionConfig) return
    await ophAdminService.deleteAccount(connectionConfig, serverId, accountId)
    await refreshServerConnection(serverId)
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

  if (isAddingConnection) {
    return (
      <AddConnectionScreen
        onSave={addConnection}
        onCancel={() => setIsAddingConnection(false)}
      />
    )
  }

  const activeQueryTab = queryTabs.find((tab) => tab.id === activeTabId)

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
        <TreeView root={tree} selectionId={selection.id} onSelect={(nextSelection) => {
          setSelection(nextSelection)
          setActiveTabId('main')
        }} />
      </aside>

      <main className="workspace">
        <header className="topbar">
          <nav className="app-menu-bar">
            <div className="app-menu">
              <button type="button" onClick={() => setOpenAppMenu(openAppMenu === 'view' ? null : 'view')}>View</button>
              {openAppMenu === 'view' ? (
                <div className="app-menu-popover">
                  <button type="button" onClick={openNewQuery}><Play size={14} /> New Query</button>
                </div>
              ) : null}
            </div>
            <div className="app-menu">
              <button type="button" onClick={() => setOpenAppMenu(openAppMenu === 'window' ? null : 'window')}>Window</button>
              {openAppMenu === 'window' ? (
                <div className="app-menu-popover window-menu-popover">
                  <button type="button" className={activeTabId === 'main' ? 'active-menu-item' : ''} onClick={() => { setActiveTabId('main'); setOpenAppMenu(null) }}>
                    Main Page
                  </button>
                  {queryTabs.map((tab) => (
                    <button key={tab.id} type="button" className={activeTabId === tab.id ? 'active-menu-item' : ''} onClick={() => { setActiveTabId(tab.id); setOpenAppMenu(null) }}>
                      {tab.title} <small>{tab.databaseName || 'No database'}</small>
                    </button>
                  ))}
                </div>
              ) : null}
            </div>
          </nav>
          <div className="search-box">
            <Search size={16} />
            <span>Search current OPH connection...</span>
          </div>
          <button className="primary-button">Test Connection</button>
        </header>
        <div className="workspace-tabs">
          <button type="button" className={activeTabId === 'main' ? 'workspace-tab active-workspace-tab' : 'workspace-tab'} onClick={() => setActiveTabId('main')}>
            Main Page
          </button>
          {queryTabs.map((tab) => (
            <div key={tab.id} className={activeTabId === tab.id ? 'workspace-tab active-workspace-tab' : 'workspace-tab'}>
              <button type="button" onClick={() => setActiveTabId(tab.id)}>{tab.title}</button>
              <button type="button" className="tab-close-button" aria-label={`Close ${tab.title}`} onClick={() => closeQueryTab(tab.id)}><X size={13} /></button>
            </div>
          ))}
          <button type="button" className="new-query-tab-button" onClick={openNewQuery}>+</button>
        </div>
        <div className="content-grid content-grid-full">
          <section className="main-panel">
            {activeQueryTab ? (
              <QueryWorkspace
                tab={activeQueryTab}
                databases={discoveredDatabases}
                onChange={(changes) => updateQueryTab(activeQueryTab.id, changes)}
                onRun={() => runQueryTab(activeQueryTab)}
                onSave={() => saveQueryTab(activeQueryTab)}
              />
            ) : (
              <Workspace
                connectionConfig={connectionConfig}
                connectionError={initialConnectionError}
                onAddConnection={() => setIsAddingConnection(true)}
                onRefreshConnection={refreshConnection}
                onRefreshServer={refreshServerConnection}
                onDeleteAccount={deleteAccount}
                selection={selection}
                servers={servers}
                tree={tree}
              />
            )}
          </section>
        </div>
      </main>
    </div>
  )
}

function QueryWorkspace({
  tab,
  databases,
  onChange,
  onRun,
  onSave,
}: {
  tab: QueryTab
  databases: OphDatabase[]
  onChange: (changes: Partial<QueryTab>) => void
  onRun: () => void
  onSave: () => void
}) {
  const columns = Array.from(new Set(tab.results.flatMap((row) => Object.keys(row))))

  return (
    <div className="page-stack query-workspace">
      <SectionHeader
        eyebrow="Query Studio"
        title={tab.title}
        description="Run a read-only SELECT query against the selected OPH database."
      />
      <div className="table-card query-editor-card">
        <div className="query-toolbar">
          <label>
            <span>Database</span>
            <select value={tab.databaseName} disabled={tab.isRunning || Boolean(tab.metadataSource)} onChange={(event) => onChange({ databaseName: event.target.value, results: [], error: '' })}>
              <option value="">Select database</option>
              {databases.map((database) => (
                <option key={database.id} value={database.databaseName}>{database.name} — {database.databaseName}</option>
              ))}
            </select>
          </label>
          <button type="button" disabled={tab.isRunning || !tab.databaseName || !tab.sql.trim()} onClick={onRun}>
            <Play size={15} /> {tab.isRunning ? 'Running…' : 'Run Query'}
          </button>
          {tab.metadataSource ? (
            <button type="button" disabled={tab.isSaving || tab.isRunning} onClick={onSave}>
              {tab.isSaving ? 'Saving…' : 'Save to modlinfo'}
            </button>
          ) : null}
        </div>
        <textarea
          className="sql-editor"
          spellCheck={false}
          value={tab.sql}
          onChange={(event) => onChange({ sql: event.target.value })}
          onKeyDown={(event) => {
            if ((event.metaKey || event.ctrlKey) && event.key === 'Enter') {
              event.preventDefault()
              onRun()
              return
            }
            if (event.key === 'Tab') {
              event.preventDefault()
              const editor = event.currentTarget
              const start = editor.selectionStart
              const end = editor.selectionEnd
              const lineStart = tab.sql.lastIndexOf('\n', start - 1) + 1
              const selectedBlock = tab.sql.slice(lineStart, end)
              let nextSql: string
              let nextStart: number
              let nextEnd: number

              if (event.shiftKey) {
                const lines = selectedBlock.split('\n')
                let removedBeforeStart = 0
                let totalRemoved = 0
                const outdented = lines.map((line, index) => {
                  const removable = line.startsWith('\t') ? 1 : Math.min(2, line.match(/^ */)?.[0].length ?? 0)
                  if (index === 0) removedBeforeStart = Math.min(removable, start - lineStart)
                  totalRemoved += removable
                  return line.slice(removable)
                }).join('\n')
                nextSql = `${tab.sql.slice(0, lineStart)}${outdented}${tab.sql.slice(end)}`
                nextStart = Math.max(lineStart, start - removedBeforeStart)
                nextEnd = Math.max(nextStart, end - totalRemoved)
              } else if (start === end) {
                nextSql = `${tab.sql.slice(0, start)}  ${tab.sql.slice(end)}`
                nextStart = start + 2
                nextEnd = nextStart
              } else {
                const indented = selectedBlock.split('\n').map((line) => `  ${line}`).join('\n')
                const lineCount = selectedBlock.split('\n').length
                nextSql = `${tab.sql.slice(0, lineStart)}${indented}${tab.sql.slice(end)}`
                nextStart = start + 2
                nextEnd = end + lineCount * 2
              }

              onChange({ sql: nextSql })
              window.requestAnimationFrame(() => editor.setSelectionRange(nextStart, nextEnd))
            }
          }}
        />
      </div>
      {tab.error ? <div className="connection-error">{tab.error}</div> : null}
      {tab.saveNotice ? <div className="action-notice">{tab.saveNotice}</div> : null}
      <div className="table-card query-results-card">
        <div className="query-results-header">
          <strong>Results</strong>
          <span>{tab.results.length} row(s)</span>
        </div>
        {tab.results.length === 0 ? (
          <div className="empty-result">Run a query to display results.</div>
        ) : (
          <div className="query-results-scroll">
            <table>
              <thead><tr>{columns.map((column) => <th key={column}>{column}</th>)}</tr></thead>
              <tbody>
                {tab.results.map((row, index) => (
                  <tr key={index}>{columns.map((column) => <td key={column}>{String(row[column] ?? '')}</td>)}</tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

function AddConnectionScreen({
  initialError,
  onSave,
  onCancel,
}: {
  initialError?: string
  onSave: (config: OphConnectionConfig) => void | Promise<void>
  onCancel?: () => void
}) {
  const [serverId] = useState(() => `srv-${crypto.randomUUID()}`)
  const [authType, setAuthType] = useState<'sql' | 'windows'>('sql')
  const [testResult, setTestResult] = useState<string>('')
  const [saveError, setSaveError] = useState<string>('')
  const [isSaving, setIsSaving] = useState(false)

  function readServer(form: FormData): OphServer {
    return {
      id: serverId,
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
          action={onCancel ? 'Cancel' : undefined}
          onAction={onCancel}
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
      menuGuid: node.menuGuid,
      parameterGuid: node.parameterGuid,
      moduleStatusGuid: node.moduleStatusGuid,
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
  onAddConnection,
  onRefreshConnection,
  onRefreshServer,
  onDeleteAccount,
  selection,
  servers,
  tree,
}: {
  connectionConfig: OphConnectionConfig
  connectionError: string
  onAddConnection: () => void
  onRefreshConnection: () => void | Promise<void>
  onRefreshServer: (serverId: string, expectedAccountId?: string) => void | Promise<void>
  onDeleteAccount: (serverId: string, accountId: string) => void | Promise<void>
  selection: WorkspaceSelection
  servers: OphServer[]
  tree: OphTreeNode
}) {
  if (connectionError) {
    return <ConnectionIssuePage error={connectionError} onRefresh={onRefreshConnection} />
  }

  if (selection.kind === 'root') {
    return <ServersPage servers={servers} onAddConnection={onAddConnection} />
  }

  if (selection.kind === 'server' && selection.serverId) {
    const server = servers.find((candidate) => candidate.id === selection.serverId)
    return server ? (
      <ServerPage
        config={connectionConfig}
        server={server}
        onRefresh={(accountId) => onRefreshServer(server.id, accountId)}
      />
    ) : <ServersPage servers={servers} onAddConnection={onAddConnection} />
  }

  if (selection.kind === 'database') {
    return (
      <DatabaseWorkspace
        selection={selection}
        tree={tree}
        onRefresh={() => selection.serverId ? onRefreshServer(selection.serverId) : undefined}
        onDelete={() => selection.serverId && selection.accountId
          ? onDeleteAccount(selection.serverId, selection.accountId)
          : undefined}
      />
    )
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
    if (selection.moduleStatusGuid) {
      return (
        <MetadataWorkspace
          config={connectionConfig}
          selection={selection}
          title={`${selection.label} States`}
          sourceTable="mstastat"
          loadRows={(config, _accountId, databaseName) =>
            ophAdminService.listModuleStatusStates(config, databaseName, selection.moduleStatusGuid ?? '')
          }
        />
      )
    }

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

  if (selection.kind === 'menu' && selection.menuGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title={`${selection.label} Submenus`}
        sourceTable="menusmnu"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listMenuSubmenus(config, accountId, databaseName, selection.menuGuid ?? '')
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

  if (selection.kind === 'parameter' && selection.parameterGuid) {
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title={`${selection.label} Values`}
        sourceTable="paravalu"
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listParameterValues(config, accountId, databaseName, selection.parameterGuid ?? '')
        }
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

function ServersPage({ servers, onAddConnection }: { servers: OphServer[]; onAddConnection: () => void }) {
  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Servers"
        title="Saved OPH connections"
        description="Connection config is loaded before the workspace opens. Use the tree to select a server or database."
        action="Add Connection"
        onAction={onAddConnection}
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

function ServerPage({
  config,
  server,
  onRefresh,
}: {
  config: OphConnectionConfig
  server: OphServer
  onRefresh: (accountId?: string) => void | Promise<void>
}) {
  const [isAddingAccount, setIsAddingAccount] = useState(false)
  const [accountId, setAccountId] = useState('')
  const [isSaving, setIsSaving] = useState(false)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState('')

  async function submitAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const normalizedAccountId = accountId.trim()
    if (!normalizedAccountId) {
      setError('Enter an Account ID.')
      return
    }

    setIsSaving(true)
    setError('')
    try {
      await ophAdminService.addAccount(config, server.id, normalizedAccountId)
      await onRefresh(normalizedAccountId)
      setAccountId('')
      setIsAddingAccount(false)
    } catch (addError) {
      setError(addError instanceof Error ? addError.message : String(addError))
    } finally {
      setIsSaving(false)
    }
  }

  async function refreshTree() {
    setIsRefreshing(true)
    setError('')
    try {
      await onRefresh()
    } catch (refreshError) {
      setError(refreshError instanceof Error ? refreshError.message : String(refreshError))
    } finally {
      setIsRefreshing(false)
    }
  }

  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Server"
        title={server.name}
        description={`${server.host}:${server.port}. Add an OPH account; the database trigger prepares its data and v4 databases.`}
        action="Add Account"
        onAction={() => setIsAddingAccount(true)}
      />
      <div className="metadata-toolbar server-toolbar">
        <button type="button" disabled={isRefreshing} onClick={refreshTree}>
          {isRefreshing ? 'Refreshing Tree…' : 'Refresh Tree'}
        </button>
      </div>
      <div className="table-card">
        <table>
          <tbody>
            <tr><th>Host</th><td>{server.host}:{server.port}</td></tr>
            <tr><th>Authentication</th><td>{server.authType === 'sql' ? 'SQL Login' : 'Windows Auth'}</td></tr>
            <tr><th>Core Database</th><td>oph_core</td></tr>
            <tr><th>Status</th><td><span className={getStatusClass(server.status)}>{server.status}</span></td></tr>
          </tbody>
        </table>
      </div>
      {!isAddingAccount && error ? <div className="connection-error">{error}</div> : null}
      {isAddingAccount ? (
        <div className="row-detail-backdrop" onMouseDown={() => !isSaving && setIsAddingAccount(false)}>
          <aside className="row-detail-overlay account-create-overlay" onMouseDown={(event) => event.stopPropagation()}>
            <div className="row-detail-header">
              <div>
                <span className="eyebrow">Add Account</span>
                <h2>{server.name}</h2>
              </div>
              <button className="overlay-close-button" type="button" disabled={isSaving} onClick={() => setIsAddingAccount(false)}>×</button>
            </div>
            <form className="row-detail-form" onSubmit={submitAccount}>
              <label>
                <span>Account ID</span>
                <input
                  autoFocus
                  value={accountId}
                  placeholder="Enter Account ID"
                  onChange={(event) => setAccountId(event.target.value)}
                />
              </label>
              <p className="field-help">The OPH core trigger will create the account databases automatically.</p>
              <button className="primary-button" type="submit" disabled={isSaving}>
                {isSaving ? 'Creating Account…' : 'Add Account'}
              </button>
            </form>
            {error ? <div className="connection-error">{error}</div> : null}
          </aside>
        </div>
      ) : null}
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

function DatabaseWorkspace({
  selection,
  tree,
  onRefresh,
  onDelete,
}: {
  selection: WorkspaceSelection
  tree: OphTreeNode
  onRefresh: () => void | Promise<void>
  onDelete: () => void | Promise<void>
}) {
  const [isConfirmingDelete, setIsConfirmingDelete] = useState(false)
  const [confirmation, setConfirmation] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [actionError, setActionError] = useState('')
  const databaseNode = findTreeNode(tree, selection.id)
  const modulesNode = databaseNode?.children?.find((child) => child.label === 'Modules')
  const securityNode = databaseNode?.children?.find((child) => child.label === 'Security')
  const interfaceNode = databaseNode?.children?.find((child) => child.label === 'Interface')
  const accountNode = databaseNode?.children?.find((child) => child.label === 'Account')

  const moduleCount = countNodes(modulesNode, (node) => node.kind === 'module')
  const securityCount = countLeafChildren(securityNode)
  const interfaceCount = countLeafChildren(interfaceNode)
  const accountCount = countLeafChildren(accountNode)

  async function refreshTree() {
    setIsRefreshing(true)
    setActionError('')
    try {
      await onRefresh()
    } catch (error) {
      setActionError(error instanceof Error ? error.message : String(error))
    } finally {
      setIsRefreshing(false)
    }
  }

  async function confirmDelete() {
    if (confirmation !== selection.accountId) {
      setActionError(`Type ${selection.accountId} exactly to confirm deletion.`)
      return
    }

    setIsDeleting(true)
    setActionError('')
    try {
      await onDelete()
      setIsConfirmingDelete(false)
    } catch (error) {
      setActionError(error instanceof Error ? error.message : String(error))
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Database"
        title={selection.label}
        description="This database was listed from OPH core account metadata. Domain groups are available under this database node."
      />
      <div className="metadata-toolbar database-toolbar">
        <button type="button" disabled={isRefreshing} onClick={refreshTree}>
          {isRefreshing ? 'Refreshing Tree…' : 'Refresh Tree'}
        </button>
        <button className="danger-button" type="button" onClick={() => {
          setConfirmation('')
          setActionError('')
          setIsConfirmingDelete(true)
        }}>Delete Account</button>
      </div>
      {actionError && !isConfirmingDelete ? <div className="connection-error">{actionError}</div> : null}
      <div className="metrics-grid">
        <MetricCard label="Modules" value={String(moduleCount)} detail="Grouped by setting mode" />
        <MetricCard label="Security" value={String(securityCount)} detail="Users and groups" />
        <MetricCard label="Interface" value={String(interfaceCount)} detail="Themes, menus, translator" />
        <MetricCard label="Account" value={String(accountCount)} detail="Parameters and mail" />
      </div>
      {isConfirmingDelete ? (
        <div className="row-detail-backdrop" onMouseDown={() => !isDeleting && setIsConfirmingDelete(false)}>
          <aside className="row-detail-overlay account-create-overlay" onMouseDown={(event) => event.stopPropagation()}>
            <div className="row-detail-header">
              <div>
                <span className="eyebrow">Delete Account</span>
                <h2>{selection.accountId}</h2>
              </div>
              <button className="overlay-close-button" type="button" disabled={isDeleting} onClick={() => setIsConfirmingDelete(false)}>×</button>
            </div>
            <div className="row-detail-form">
              <p className="delete-warning">This marks the account as deleted in OPH core. Its physical databases are not removed.</p>
              <label>
                <span>Type Account ID to confirm</span>
                <input autoFocus value={confirmation} onChange={(event) => setConfirmation(event.target.value)} />
              </label>
              <button className="danger-action-button" type="button" disabled={isDeleting} onClick={confirmDelete}>
                {isDeleting ? 'Deleting Account…' : 'Delete Account'}
              </button>
            </div>
            {actionError ? <div className="connection-error">{actionError}</div> : null}
          </aside>
        </div>
      ) : null}
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
  const [checkedRowIndexes, setCheckedRowIndexes] = useState<Set<number>>(new Set())
  const [isCopyTargetOpen, setIsCopyTargetOpen] = useState(false)
  const [copyTargets, setCopyTargets] = useState<MetadataRow[]>([])
  const [copyTargetGuid, setCopyTargetGuid] = useState('')
  const [copyAccounts, setCopyAccounts] = useState<MetadataRow[]>([])
  const [copyAccountIndex, setCopyAccountIndex] = useState('')
  const [copyTargetDatabaseName, setCopyTargetDatabaseName] = useState('')
  const [copyTargetAccountId, setCopyTargetAccountId] = useState('')
  const [isLoadingCopyTargets, setIsLoadingCopyTargets] = useState(false)
  const [isCopyingRows, setIsCopyingRows] = useState(false)
  const [draftRow, setDraftRow] = useState<Record<string, string>>({})
  const [isEditing, setIsEditing] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [actionError, setActionError] = useState('')
  const [actionNotice, setActionNotice] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [isResettingPassword, setIsResettingPassword] = useState(false)
  const [userTokenOptions, setUserTokenOptions] = useState<Array<{ value: string; label: string }>>([])
  const [moduleGroupTokenOptions, setModuleGroupTokenOptions] = useState<Array<{ value: string; label: string }>>([])
  const [moduleRelationOptions, setModuleRelationOptions] = useState<Record<string, Array<{ value: string; label: string }>>>({})
  const [columnTypeOptions, setColumnTypeOptions] = useState<Array<{ value: string; label: string }>>([])
  const visibleColumnMap: Record<string, string[]> = {
    '[user]': ['userid', 'username', 'email', 'expirydate'],
    acctinfo: ['infokey', 'infovalue'],
    acct: ['accountid'],
    acctdbse: ['databasename', 'ismaster', 'version'],
    userinfo: ['infokey', 'infovalue'],
    ugrp: ['groupid', 'groupdescription'],
    ugrpmodl: ['moduleid', 'moduledescription', 'allowaccess', 'allowadd', 'allowedit', 'allowdelete', 'allowforce', 'allowwipe'],
    msta: ['modulestatusname', 'modulestatusdescription'],
    mstastat: ['stateid', 'statecode', 'statename', 'statedesc', 'isdefault'],
    modg: ['modulegroupid', 'modulegroupname', 'modulegroupdescription'],
    thme: ['themecode', 'themename', 'themefolder'],
    thmepage: ['pageurl', 'isdefault'],
    menu: ['menucode', 'menudescription', 'createddate', 'updateddate'],
    menusmnu: ['submenudescription', 'tag', 'url', 'orderno', 'caption', 'type', 'uppersubmenuguid', 'icon_fa', 'icon_url'],
    word: ['originstatements', 'createddate', 'updateddate'],
    para: ['parameterid', 'parameterdescription', 'createddate', 'updateddate'],
    paravalu: ['parametervalue', 'parameterdescription'],
    widg: ['widgetid', 'widgetdescription'],
    mail: ['profilename', 'accountname', 'displayname', 'emailaddress', 'bcc', 'createddate', 'updateddate'],
    modlinfo: ['infokey', 'infovalue'],
    modlcolm: ['colkey', 'coltype', 'titlecaption', 'colorder', 'collength'],
    modlcolminfo: ['infokey', 'infovalue'],
    modlappr: ['approvalgroup', 'uppergroup', 'lvl', 'sqlfilter', 'zonegroup'],
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
    allexceptuser: 'All Except User',
    tokenuser: 'User Tokens',
    allexceptenv: 'All Except Environment',
    tokenenv: 'Environment Tokens',
    allexceptmodule: 'All Except Module',
    allowaccess: 'Allow Access',
    allowadd: 'Allow Add',
    allowedit: 'Allow Edit',
    allowdelete: 'Allow Delete',
    allowforce: 'Allow Force',
    allowwipe: 'Allow Wipe',
    modulestatusname: 'Module Status Name',
    stateid: 'State ID',
    statecode: 'State Code',
    statename: 'State Name',
    statedesc: 'State Description',
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
    parametervalue: 'Parameter Value',
    widgetid: 'Widget ID',
    widgetdescription: 'Description',
    sqlstr: 'SQL',
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
    approvalgroup: 'Approval Group',
    uppergroup: 'Upper Group',
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
    accountdbguid: 'Account DB',
    themepageguid: 'Theme Page',
    modulestatusguid: 'Module Status',
    modulegroupguid: 'Module Group',
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
    'modulestatusdetailguid',
    'parentmoduleguid',
    'accountdbguid',
    'themepageguid',
    'themeguid',
    'password',
    'lockmode',
  ])
  const checkboxColumns = new Set([
    'needlogin',
    'ismaster',
    'isdefault',
    'allowaccess',
    'allowadd',
    'allowedit',
    'allowdelete',
    'allowforce',
    'allowwipe',
    'reportattachment',
  ])
  const copyableTables = new Set(['modl', 'modlinfo', 'modlcolm', 'modlcolminfo', 'modlappr', 'modldocn', 'modlmail'])
  useEffect(() => {
    setTableRows(rows)
    setCheckedRowIndexes(new Set())
    setIsCopyTargetOpen(false)
    setCopyTargets([])
    setCopyTargetGuid('')
    setCopyAccounts([])
    setCopyAccountIndex('')
    setCopyTargetDatabaseName('')
    setCopyTargetAccountId('')
    setSelectedRow(null)
    setSelectedRowIndex(null)
    setDraftRow({})
    setIsEditing(false)
    setIsCreating(false)
    setActionError('')
    setActionNotice('')
    setNewPassword('')
    setConfirmPassword('')
  }, [rows])

  const allRowsChecked = tableRows.length > 0 && checkedRowIndexes.size === tableRows.length

  function toggleRowChecked(index: number) {
    setCheckedRowIndexes((currentIndexes) => {
      const nextIndexes = new Set(currentIndexes)
      if (nextIndexes.has(index)) nextIndexes.delete(index)
      else nextIndexes.add(index)
      return nextIndexes
    })
  }

  function toggleAllRows() {
    setCheckedRowIndexes(allRowsChecked ? new Set() : new Set(tableRows.map((_, index) => index)))
  }

  async function openCopyTo() {
    if (!selection.databaseName || !selection.accountId || checkedRowIndexes.size === 0) return
    setIsCopyTargetOpen(true)
    setCopyTargets([])
    setCopyTargetGuid('')
    setCopyAccounts([])
    setCopyAccountIndex('')
    setIsLoadingCopyTargets(true)
    setActionError('')
    setActionNotice('')
    try {
      const accounts = await ophAdminService.listCopyAccounts(config)
      setCopyAccounts(accounts)
      const initialIndex = accounts.findIndex((account) =>
        String(account.accountid ?? '') === selection.accountId
        && String(account.databasename ?? '') === selection.databaseName)
      const selectedIndex = initialIndex >= 0 ? initialIndex : 0
      const initialAccount = accounts[selectedIndex]
      if (!initialAccount) throw new Error('No destination account is available.')
      const targetDatabaseName = String(initialAccount.databasename ?? '')
      const targetAccountId = String(initialAccount.accountid ?? '')
      setCopyAccountIndex(String(selectedIndex))
      setCopyTargetDatabaseName(targetDatabaseName)
      setCopyTargetAccountId(targetAccountId)
      const targets = await ophAdminService.listCopyTargets(
        config,
        targetDatabaseName,
        sourceTable,
        targetAccountId,
      )
      setCopyTargets(targets)
    } catch (copyError) {
      setActionError(copyError instanceof Error ? copyError.message : String(copyError))
    } finally {
      setIsLoadingCopyTargets(false)
    }
  }

  async function changeCopyAccount(indexValue: string) {
    setCopyAccountIndex(indexValue)
    setCopyTargetGuid('')
    setCopyTargets([])
    const account = copyAccounts[Number(indexValue)]
    if (!account) return
    const targetDatabaseName = String(account.databasename ?? '')
    const targetAccountId = String(account.accountid ?? '')
    setCopyTargetDatabaseName(targetDatabaseName)
    setCopyTargetAccountId(targetAccountId)
    setIsLoadingCopyTargets(true)
    setActionError('')
    try {
      setCopyTargets(await ophAdminService.listCopyTargets(
        config,
        targetDatabaseName,
        sourceTable,
        targetAccountId,
      ))
    } catch (copyError) {
      setActionError(copyError instanceof Error ? copyError.message : String(copyError))
    } finally {
      setIsLoadingCopyTargets(false)
    }
  }

  async function copySelectedRows() {
    if (!selection.databaseName || !copyTargetGuid || !copyTargetDatabaseName || !copyTargetAccountId) {
      setActionError('Select a Copy To destination.')
      return
    }
    const selectedRows = tableRows.filter((_, index) => checkedRowIndexes.has(index))
    setIsCopyingRows(true)
    setActionError('')
    try {
      const copied = await ophAdminService.copyMetadataRows(
        config,
        selection.databaseName,
        sourceTable,
        selectedRows,
        copyTargetGuid,
        copyTargetDatabaseName,
        copyTargetAccountId,
        selection.accountId ?? '',
      )
      const skipped = selectedRows.length - copied
      setActionNotice(`${copied} row(s) copied.${skipped > 0 ? ` ${skipped} duplicate or invalid row(s) skipped.` : ''}`)
      setCheckedRowIndexes(new Set())
      setIsCopyTargetOpen(false)
      setCopyTargets([])
      setCopyTargetGuid('')
      setCopyAccounts([])
      setCopyAccountIndex('')
    } catch (copyError) {
      setActionError(copyError instanceof Error ? copyError.message : String(copyError))
    } finally {
      setIsCopyingRows(false)
    }
  }

  const rowColumns = Array.from(new Set(tableRows.flatMap((row) => Object.keys(row))))
  const sourceKey = sourceTable.toLowerCase()
  useEffect(() => {
    if (sourceKey !== 'ugrp' || !selection.accountId || !selection.databaseName) {
      setUserTokenOptions([])
      return
    }

    let cancelled = false
    ophAdminService.listAllUsers(config, selection.databaseName)
      .then((users) => {
        if (cancelled) return
        setUserTokenOptions(users.map((user) => ({
          value: String(user.userguid ?? ''),
          label: String(user.userid ?? user.userguid ?? ''),
        })).filter((option) => option.value && option.label))
      })
      .catch(() => {
        if (!cancelled) setUserTokenOptions([])
      })

    return () => {
      cancelled = true
    }
  }, [config, selection.accountId, selection.databaseName, sourceKey])
  useEffect(() => {
    if (sourceKey !== 'modlcolm' || !selection.databaseName) {
      setColumnTypeOptions([])
      return
    }

    let cancelled = false
    ophAdminService.listMssqlColumnTypes(config, selection.databaseName)
      .then((types) => {
        if (cancelled) return
        setColumnTypeOptions(types.map((row) => ({
          value: String(row.coltype ?? ''),
          label: `${String(row.typename ?? row.coltype ?? '')} = ${String(row.coltype ?? '')}`,
        })))
      })
      .catch(() => {
        if (!cancelled) setColumnTypeOptions([{ value: '0', label: 'nonfield = 0' }])
      })

    return () => {
      cancelled = true
    }
  }, [config, selection.databaseName, sourceKey])
  useEffect(() => {
    if (!selection.accountId || !selection.databaseName || !['modl', 'modlappr'].includes(sourceKey)) {
      setModuleRelationOptions({})
      return
    }

    let cancelled = false
    if (sourceKey === 'modlappr') {
      ophAdminService.listModuleGroups(config, selection.accountId, selection.databaseName)
        .then((groups) => {
          if (cancelled) return
          const options = groups.map((row) => ({
            value: String(row.modulegroupguid ?? ''),
            label: [row.modulegroupid, row.modulegroupname].filter(Boolean).join(' — ') || String(row.modulegroupguid ?? ''),
          }))
          setModuleRelationOptions({
            approvalgroupguid: options,
            uppergroupguid: options,
          })
        })
        .catch(() => {
          if (!cancelled) setModuleRelationOptions({})
        })

      return () => {
        cancelled = true
      }
    }

    Promise.all([
      ophAdminService.listModuleStatuses(config, selection.accountId, selection.databaseName),
      ophAdminService.listModuleGroups(config, selection.accountId, selection.databaseName),
      ophAdminService.listAccountDatabases(config, selection.accountId, selection.databaseName),
      ophAdminService.listModuleThemePages(config, selection.accountId, selection.databaseName),
    ]).then(([statuses, groups, accountDatabases, themePages]) => {
      if (cancelled) return
      setModuleRelationOptions({
        modulestatusguid: statuses.map((row) => ({
          value: String(row.modulestatusguid ?? ''),
          label: String(row.modulestatusname ?? row.modulestatusguid ?? ''),
        })),
        modulegroupguid: groups.map((row) => ({
          value: String(row.modulegroupguid ?? ''),
          label: String(row.modulegroupid ?? row.modulegroupname ?? row.modulegroupguid ?? ''),
        })),
        accountdbguid: accountDatabases.map((row) => ({
          value: String(row.accountdbguid ?? ''),
          label: String(row.databasename ?? row.accountdbguid ?? ''),
        })),
        themepageguid: themePages.map((row) => ({
          value: String(row.themepageguid ?? ''),
          label: [row.themecode, row.pageurl].filter(Boolean).join(' — ') || String(row.themepageguid ?? ''),
        })),
      })
    }).catch(() => {
      if (!cancelled) setModuleRelationOptions({})
    })

    return () => {
      cancelled = true
    }
  }, [config, selection.accountId, selection.databaseName, sourceKey])
  useEffect(() => {
    if (sourceKey !== 'ugrp' || !selection.accountId || !selection.databaseName) {
      setModuleGroupTokenOptions([])
      return
    }

    let cancelled = false
    ophAdminService.listAllModuleGroups(config, selection.databaseName)
      .then((groups) => {
        if (cancelled) return
        setModuleGroupTokenOptions(groups.map((group) => ({
          value: String(group.modulegroupguid ?? ''),
          label: String(group.modulegroupid ?? group.modulegroupguid ?? ''),
        })).filter((option) => option.value && option.label))
      })
      .catch(() => {
        if (!cancelled) setModuleGroupTokenOptions([])
      })

    return () => {
      cancelled = true
    }
  }, [config, selection.accountId, selection.databaseName, sourceKey])
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
  const overlayColumnMap: Record<string, string[]> = {
    ugrp: ['groupid', 'groupdescription', 'allexceptuser', 'tokenuser', 'allexceptenv', 'tokenenv', 'allexceptmodule'],
    ugrpmodl: ['moduleguid', 'allowaccess', 'allowadd', 'allowedit', 'allowdelete', 'allowforce', 'allowwipe'],
    widg: ['widgetid', 'widgetdescription', 'sqlstr'],
    modl: ['moduleid', 'moduledescription', 'settingmode', 'accountdbguid', 'orderno', 'needlogin', 'themepageguid', 'modulestatusguid', 'modulegroupguid'],
    modlappr: ['approvalgroupguid', 'uppergroupguid', 'lvl', 'sqlfilter', 'zonegroup'],
  }
  const overlayColumns = overlayColumnMap[sourceKey]
    ?? columns.filter((column) => !['createddate', 'updateddate'].includes(column.toLowerCase()))

  function openRow(row: MetadataRow, index: number) {
    setSelectedRow(row)
    setSelectedRowIndex(index)
    setDraftRow(Object.fromEntries(overlayColumns.map((column) => [column, String(getCellValue(row, column) ?? '')])))
    setIsEditing(true)
    setIsCreating(false)
    setActionError('')
    setActionNotice('')
    setNewPassword('')
    setConfirmPassword('')
  }

  function openCreate() {
    setSelectedRow({})
    setSelectedRowIndex(null)
    setDraftRow(Object.fromEntries(overlayColumns.map((column) => [column, ''])))
    setIsEditing(true)
    setIsCreating(true)
    setActionError('')
    setActionNotice('')
    setNewPassword('')
    setConfirmPassword('')
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
        selection.accountId,
        selection.menuGuid,
        selection.parameterGuid,
        selection.moduleStatusGuid,
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
    setActionNotice('')
    setNewPassword('')
    setConfirmPassword('')
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
    setCheckedRowIndexes((currentIndexes) => new Set(
      Array.from(currentIndexes)
        .filter((index) => index !== selectedRowIndex)
        .map((index) => index > selectedRowIndex ? index - 1 : index),
    ))
    setSelectedRow(null)
    setSelectedRowIndex(null)
    setDraftRow({})
    setIsEditing(false)
    setIsCreating(false)
    setActionError('')
  }

  async function resetSelectedUserPassword() {
    if (!selection.databaseName || !selection.accountId || !selectedRow) {
      setActionError('Cannot reset password: database, account, or user is missing.')
      return
    }

    const userGuid = String(getCellValue(selectedRow, 'userguid') || selection.userGuid || '')
    const userId = String(getCellValue(selectedRow, 'userid') || selection.label || '')
    if (!userGuid || !userId) {
      setActionError('Cannot reset password: User ID or user key is missing.')
      return
    }
    if (!newPassword) {
      setActionError(`Enter a new password for ${userId}.`)
      return
    }
    if (newPassword !== confirmPassword) {
      setActionError(`Password confirmation for ${userId} does not match.`)
      return
    }

    setIsResettingPassword(true)
    setActionError('')
    setActionNotice('')
    try {
      await ophAdminService.resetUserPassword(
        config,
        selection.databaseName,
        selection.accountId,
        userGuid,
        userId,
        newPassword,
      )
      setNewPassword('')
      setConfirmPassword('')
      setActionNotice(`Password for ${userId} was reset successfully.`)
    } catch (resetError) {
      setActionError(resetError instanceof Error ? resetError.message : String(resetError))
    } finally {
      setIsResettingPassword(false)
    }
  }

  return (
    <div className="metadata-table-shell">
      <div className="metadata-toolbar">
        <button type="button" onClick={openCreate}>Add</button>
        <button type="button" disabled={tableRows.length === 0} onClick={toggleAllRows}>
          {allRowsChecked ? 'Clear All' : 'Select All'}
        </button>
        {copyableTables.has(sourceKey) ? (
          <button type="button" disabled={checkedRowIndexes.size === 0} onClick={openCopyTo}>Copy To</button>
        ) : null}
        <span className="selection-count">{checkedRowIndexes.size} selected</span>
      </div>
      {!selectedRow && actionError ? <div className="connection-error">{actionError}</div> : null}
      {!selectedRow && actionNotice ? <div className="action-notice">{actionNotice}</div> : null}
      <div className="table-card metadata-table">
        {tableRows.length === 0 ? (
          <div className="empty-result">No rows found.</div>
        ) : (
          <table>
            <thead>
              <tr>
                <th className="selection-column">
                  <input
                    type="checkbox"
                    aria-label="Select all rows"
                    checked={allRowsChecked}
                    disabled={tableRows.length === 0}
                    onChange={toggleAllRows}
                  />
                </th>
                {columns.map((column) => (
                  <th key={column}>{columnLabels[column.toLowerCase()] ?? column}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {tableRows.map((row, index) => (
                <tr
                  className={`clickable-row ${selectedRowIndex === index ? 'selected-row' : ''} ${checkedRowIndexes.has(index) ? 'checked-row' : ''}`}
                  key={index}
                  onClick={() => openRow(row, index)}
                >
                  <td className="selection-column" onClick={(event) => event.stopPropagation()}>
                    <input
                      type="checkbox"
                      aria-label={`Select row ${index + 1}`}
                      checked={checkedRowIndexes.has(index)}
                      onChange={() => toggleRowChecked(index)}
                    />
                  </td>
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
        <div className="row-detail-backdrop" onMouseDown={cancelEdit}>
        <aside className="row-detail-overlay" onMouseDown={(event) => event.stopPropagation()}>
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
                {column.toLowerCase() === 'coltype' && sourceKey === 'modlcolm' ? (
                  <select
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    disabled={!isEditing}
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  >
                    <option value="">Select column type</option>
                    {columnTypeOptions.map((option) => (
                      <option key={`${option.value}-${option.label}`} value={option.value}>{option.label}</option>
                    ))}
                  </select>
                ) : moduleRelationOptions[column.toLowerCase()] ? (
                  <select
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    disabled={!isEditing}
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  >
                    <option value="">None</option>
                    {moduleRelationOptions[column.toLowerCase()].filter((option) => option.value).map((option) => (
                      <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                  </select>
                ) : column.toLowerCase().startsWith('token') ? (
                  <TokenInput
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    readOnly={!isEditing}
                    options={column.toLowerCase() === 'tokenuser'
                      ? userTokenOptions
                      : column.toLowerCase() === 'tokenenv'
                        ? moduleGroupTokenOptions
                        : []}
                    inputLabel={column.toLowerCase() === 'tokenuser' ? 'User ID' : 'Module Group ID'}
                    onChange={(value) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: value }))}
                  />
                ) : column.toLowerCase().startsWith('allexcept') ? (
                  <SwitchInput
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    readOnly={!isEditing}
                    onChange={(value) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: value }))}
                  />
                ) : checkboxColumns.has(column.toLowerCase()) ? (
                  <CheckboxInput
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    readOnly={!isEditing}
                    onChange={(value) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: value }))}
                  />
                ) : (draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')).length > 80 ? (
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
          {(sourceKey === '[user]' || selection.kind === 'security-user') && !isCreating ? (
            <div className="password-reset-panel">
              <div>
                <strong>Reset Password</strong>
                <p>Set a new password for {String(getCellValue(selectedRow, 'userid') || selection.label || 'this user')}.</p>
              </div>
              <label>
                <span>New Password</span>
                <input
                  type="password"
                  autoComplete="new-password"
                  value={newPassword}
                  onChange={(event) => setNewPassword(event.target.value)}
                />
              </label>
              <label>
                <span>Confirm Password</span>
                <input
                  type="password"
                  autoComplete="new-password"
                  value={confirmPassword}
                  onChange={(event) => setConfirmPassword(event.target.value)}
                />
              </label>
              <button type="button" disabled={isResettingPassword} onClick={resetSelectedUserPassword}>
                {isResettingPassword ? 'Resetting…' : 'Reset Password'}
              </button>
            </div>
          ) : null}
          <div className="row-detail-actions">
            <button type="button" onClick={saveDraft}>Save</button>
            {sourceKey === 'modlinfo'
              && !isCreating
              && /^(view_|script_)/i.test(String(getCellValue(selectedRow, 'infokey') ?? '')) ? (
                <button type="button" onClick={() => {
                  window.dispatchEvent(new CustomEvent('oph:open-metadata-query', {
                    detail: { databaseName: selection.databaseName, row: selectedRow },
                  }))
                  cancelEdit()
                }}>
                  See in Query
                </button>
              ) : null}
            <button type="button" onClick={cancelEdit}>Cancel</button>
            <button className="danger-button" type="button" onClick={deleteSelectedRow}>Delete</button>
          </div>
          {actionError ? <div className="connection-error">{actionError}</div> : null}
          {actionNotice ? <div className="action-notice">{actionNotice}</div> : null}
        </aside>
        </div>
      ) : null}
      {isCopyTargetOpen ? (
        <div className="row-detail-backdrop" onMouseDown={() => !isCopyingRows && setIsCopyTargetOpen(false)}>
          <aside className="row-detail-overlay copy-target-overlay" onMouseDown={(event) => event.stopPropagation()}>
            <div className="row-detail-header">
              <div>
                <span className="eyebrow">Copy selected rows</span>
                <h2>Copy To</h2>
              </div>
              <button className="overlay-close-button" type="button" disabled={isCopyingRows} onClick={() => setIsCopyTargetOpen(false)}>×</button>
            </div>
            <p className="copy-target-summary">Copy {checkedRowIndexes.size} selected row(s) from {sourceTable}. Duplicate keys will be skipped.</p>
            <label className="copy-target-field">
              <span>Destination account</span>
              <select value={copyAccountIndex} disabled={isCopyingRows} onChange={(event) => changeCopyAccount(event.target.value)}>
                <option value="">Select account</option>
                {copyAccounts.map((account, index) => (
                  <option key={`${String(account.databasename)}-${String(account.accountid)}`} value={String(index)}>
                    {String(account.accountid)} — {String(account.databasename)}
                  </option>
                ))}
              </select>
            </label>
            <label className="copy-target-field">
              <span>Destination parent</span>
              <select value={copyTargetGuid} disabled={isLoadingCopyTargets || isCopyingRows} onChange={(event) => setCopyTargetGuid(event.target.value)}>
                <option value="">{isLoadingCopyTargets ? 'Loading destinations…' : 'Select destination'}</option>
                {copyTargets.map((target) => (
                  <option key={String(target.targetguid)} value={String(target.targetguid)}>
                    {String(target.targetlabel ?? target.targetguid)}{target.targetdescription ? ` — ${String(target.targetdescription)}` : ''}
                  </option>
                ))}
              </select>
            </label>
            <div className="row-detail-actions">
              <button type="button" disabled={!copyTargetGuid || isCopyingRows} onClick={copySelectedRows}>
                {isCopyingRows ? 'Copying…' : 'Copy'}
              </button>
              <button type="button" disabled={isCopyingRows} onClick={() => setIsCopyTargetOpen(false)}>Cancel</button>
            </div>
            {actionError ? <div className="connection-error">{actionError}</div> : null}
          </aside>
        </div>
      ) : null}
    </div>
  )
}

function CheckboxInput({
  value,
  readOnly,
  onChange,
}: {
  value: string
  readOnly: boolean
  onChange: (value: string) => void
}) {
  const isChecked = ['1', 'true', 'yes', 'on'].includes(value.toLowerCase())

  return (
    <div className="checkbox-input">
      <input
        type="checkbox"
        checked={isChecked}
        disabled={readOnly}
        onChange={(event) => onChange(event.target.checked ? '1' : '0')}
      />
      <span>{isChecked ? 'Checked' : 'Unchecked'}</span>
    </div>
  )
}

function SwitchInput({
  value,
  readOnly,
  onChange,
}: {
  value: string
  readOnly: boolean
  onChange: (value: string) => void
}) {
  const isOn = ['1', 'true', 'yes', 'on'].includes(value.toLowerCase())

  return (
    <button
      className={`switch-input ${isOn ? 'switch-input-on' : 'switch-input-off'}`}
      type="button"
      role="switch"
      aria-checked={isOn}
      disabled={readOnly}
      onClick={() => onChange(isOn ? '0' : '1')}
    >
      <span className="switch-track"><span className="switch-thumb" /></span>
      <span className="switch-status">
        <strong>{isOn ? 'ON' : 'OFF'}</strong>
        <small>{isOn ? 'Exception is active' : 'Exception is inactive'}</small>
      </span>
    </button>
  )
}

function TokenInput({
  value,
  readOnly,
  options,
  inputLabel,
  onChange,
}: {
  value: string
  readOnly: boolean
  options: Array<{ value: string; label: string }>
  inputLabel: string
  onChange: (value: string) => void
}) {
  const [pendingToken, setPendingToken] = useState('')
  const tokens = value.split('*').map((token) => token.trim()).filter(Boolean)

  function commitToken() {
    const rawToken = pendingToken.trim().replace(/\*/g, '')
    const matchedOption = options.find((option) =>
      option.value.toLowerCase() === rawToken.toLowerCase()
      || option.label.toLowerCase() === rawToken.toLowerCase())
    const nextToken = matchedOption?.value ?? rawToken
    if (!nextToken || tokens.includes(nextToken)) {
      setPendingToken('')
      return
    }

    onChange([...tokens, nextToken].join('*'))
    setPendingToken('')
  }

  function removeToken(tokenToRemove: string) {
    onChange(tokens.filter((token) => token !== tokenToRemove).join('*'))
  }

  function addToken(token: string) {
    if (!token || tokens.some((existingToken) => existingToken.toLowerCase() === token.toLowerCase())) return
    onChange([...tokens, token].join('*'))
  }

  return (
    <div className={`token-input ${readOnly ? 'token-input-readonly' : ''}`}>
      {tokens.map((token) => (
        <span className="token-chip" key={token}>
          {options.find((option) => option.value.toLowerCase() === token.toLowerCase())?.label ?? token}
          {!readOnly ? (
            <button
              type="button"
              aria-label={`Remove ${token}`}
              onMouseDown={(event) => {
                event.preventDefault()
                event.stopPropagation()
              }}
              onClick={(event) => {
                event.preventDefault()
                event.stopPropagation()
                removeToken(token)
              }}
            >×</button>
          ) : null}
        </span>
      ))}
      {!readOnly ? (
        <select
          aria-label={`Select ${inputLabel}`}
          value=""
          onChange={(event) => addToken(event.target.value)}
        >
          <option value="">Select {inputLabel}</option>
          {options.filter((option) => !tokens.some((token) => token.toLowerCase() === option.value.toLowerCase())).map((option) => (
            <option key={option.value} value={option.value}>{option.label}</option>
          ))}
        </select>
      ) : null}
      {!readOnly ? (
        <input
          aria-label="Add token"
          value={pendingToken}
          placeholder={options.length > 0 ? `Enter ${inputLabel}, then press Enter` : 'Paste GUID, then press Enter'}
          onChange={(event) => {
            const nextValue = event.target.value
            if (nextValue.includes('*')) {
              const incomingTokens = nextValue.split('*').map((token) => {
                const normalizedToken = token.trim()
                return options.find((option) =>
                  option.value.toLowerCase() === normalizedToken.toLowerCase()
                  || option.label.toLowerCase() === normalizedToken.toLowerCase())?.value ?? normalizedToken
              }).filter(Boolean)
              onChange(Array.from(new Set([...tokens, ...incomingTokens])).join('*'))
              setPendingToken('')
              return
            }
            setPendingToken(nextValue)
          }}
          onBlur={commitToken}
          onKeyDown={(event) => {
            if (event.key === 'Enter' || event.key === '*') {
              event.preventDefault()
              commitToken()
            }
          }}
        />
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
