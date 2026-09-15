import {
  Activity,
  AlertTriangle,
  BadgeDollarSign,
  Banknote,
  BarChart3,
  BookOpen,
  Boxes,
  Building2,
  CalendarDays,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  ClipboardCheck,
  CreditCard,
  Database,
  FileCode2,
  FileChartColumn,
  FolderTree,
  Gauge,
  GraduationCap,
  HandCoins,
  History,
  House,
  IdCard,
  KeyRound,
  Layers3,
  LayoutDashboard,
  Link,
  ListChecks,
  Menu as MenuBars,
  MessageSquare,
  MonitorCog,
  Package,
  PackageCheck,
  Pin,
  Play,
  ReceiptText,
  RefreshCw,
  ShoppingBag,
  ShoppingCart,
  Store,
  Target,
  Truck,
  ArrowRight,
  ArrowUp,
  Search,
  Server,
  Settings,
  ShieldCheck,
  Table2,
  User,
  UserPlus,
  UserRoundCheck,
  UserRoundCog,
  Users,
  WalletCards,
  Warehouse,
  X,
} from 'lucide-react'
import { FormEvent, useEffect, useMemo, useRef, useState } from 'react'
import type { ComponentType, ReactNode } from 'react'
import menuIcons from './data/menu-icons.json'
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
  onClick?: () => void
}

type MenuIconDefinition = {
  key: string
  label: string
  component: string
  aliases: string[]
}

const menuIconCatalog = menuIcons as MenuIconDefinition[]
const auditLogKey = 'oph-control-studio.audit-log'

function appendAuditLog(operation: string, target: string, result: 'success' | 'failed', detail: string) {
  try {
    const current = JSON.parse(window.localStorage.getItem(auditLogKey) ?? '[]') as unknown[]
    window.localStorage.setItem(auditLogKey, JSON.stringify([
      { timestamp: new Date().toISOString(), operation, target, result, detail },
      ...current,
    ].slice(0, 500)))
  } catch {
    // Audit persistence must not block the requested administration operation.
  }
}
const moduleSettingModeOptions = [
  { value: '0', label: 'Core' },
  { value: '1', label: 'Master' },
  { value: '4', label: 'Transaction' },
  { value: '5', label: 'Report' },
  { value: '6', label: 'Blank' },
  { value: '7', label: 'View' },
]
const menuIconComponents: Record<string, ComponentType<{ size?: number; 'aria-hidden'?: boolean }>> = {
  LayoutDashboard, House, Users, User, UserPlus, UserRoundCheck, IdCard, Store,
  ShoppingCart, ShoppingBag, Package, Boxes, PackageCheck, Warehouse, Truck, Link,
  Target, WalletCards, Banknote, HandCoins, BadgeDollarSign, CreditCard, ReceiptText,
  BarChart3, FileChartColumn, History, MessageSquare, CalendarDays, FolderTree,
  GraduationCap, BookOpen, Building2, ListChecks, ClipboardCheck, ShieldCheck,
  Settings, Database,
}

function findMenuIcon(value: string) {
  const normalizedValue = value.trim().toLowerCase()
  return menuIconCatalog.find((icon) =>
    icon.key.toLowerCase() === normalizedValue
    || icon.aliases.some((alias) => alias.toLowerCase() === normalizedValue),
  )
}

function MenuIcon({ value, withLabel = false }: { value: string; withLabel?: boolean }) {
  const definition = findMenuIcon(value)
  const Icon = definition ? menuIconComponents[definition.component] : null
  return (
    <span className="menu-icon-preview">
      {Icon ? <Icon size={18} aria-hidden /> : <span className="menu-icon-placeholder">—</span>}
      {withLabel ? <span>{definition?.label ?? (value || 'No icon')}</span> : null}
    </span>
  )
}

type SectionHeaderProps = {
  eyebrow: string
  title: string
  description: string
  action?: string
  onAction?: () => void
  onTitleClick?: () => void
  parentLabel?: string
  onParentClick?: () => void
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
    sourceTable: 'modlinfo' | 'widg'
    row: MetadataRow
    sqlField?: 'infovalue' | 'sqlstr'
  }
}

type PinnedWorkspaceTab = {
  id: string
  selection: WorkspaceSelection
}

type ReportDesignerTab = {
  id: string
  title: string
  databaseName: string
  xml: string
  error: string
  isSaving: boolean
  saveNotice: string
  metadataSource: {
    sourceTable: 'modlinfo'
    row: MetadataRow
  }
}

type SavedWorkspaceState = {
  mainSelection: WorkspaceSelection
  pinnedTabs: PinnedWorkspaceTab[]
  queryTabs: QueryTab[]
  reportTabs: ReportDesignerTab[]
  activeTabId: string
}

const workspaceStateKey = 'oph-control-studio.workspace-state.v1'

function loadSavedWorkspaceState(): SavedWorkspaceState | null {
  try {
    const saved = window.localStorage.getItem(workspaceStateKey)
    if (!saved) return null
    const parsed = JSON.parse(saved) as Partial<SavedWorkspaceState>
    if (!parsed.mainSelection || !Array.isArray(parsed.pinnedTabs) || !Array.isArray(parsed.queryTabs)) return null
    return {
      mainSelection: parsed.mainSelection,
      pinnedTabs: parsed.pinnedTabs,
      queryTabs: parsed.queryTabs.map((tab) => ({
        ...tab,
        results: [],
        error: '',
        isRunning: false,
        isSaving: false,
        saveNotice: '',
      })),
      reportTabs: Array.isArray(parsed.reportTabs) ? parsed.reportTabs.map((tab) => ({
        ...tab,
        error: '',
        isSaving: false,
        saveNotice: '',
      })) : [],
      activeTabId: typeof parsed.activeTabId === 'string' ? parsed.activeTabId : 'main',
    }
  } catch {
    return null
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
  const savedWorkspaceState = useRef(loadSavedWorkspaceState())
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
  const [moduleGroupRowsByDatabaseId, setModuleGroupRowsByDatabaseId] = useState<Record<string, MetadataRow[]>>({})
  const [isLoadingConfig, setIsLoadingConfig] = useState(true)
  const [isAddingConnection, setIsAddingConnection] = useState(false)
  const [initialConnectionError, setInitialConnectionError] = useState('')
  const [queryTabs, setQueryTabs] = useState<QueryTab[]>(savedWorkspaceState.current?.queryTabs ?? [])
  const [reportTabs, setReportTabs] = useState<ReportDesignerTab[]>(savedWorkspaceState.current?.reportTabs ?? [])
  const [pinnedTabs, setPinnedTabs] = useState<PinnedWorkspaceTab[]>(savedWorkspaceState.current?.pinnedTabs ?? [])
  const [activeTabId, setActiveTabId] = useState(savedWorkspaceState.current?.activeTabId ?? 'main')
  const [isWorkspaceStateReady, setIsWorkspaceStateReady] = useState(false)
  const [openAppMenu, setOpenAppMenu] = useState<'view' | 'window' | null>(null)
  const [isMobileExplorerOpen, setIsMobileExplorerOpen] = useState(false)
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
        moduleGroupRowsByDatabaseId,
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
      moduleGroupRowsByDatabaseId,
    ],
  )
  const firstDatabase = tree?.children?.[0]?.children?.[0]
  const [selection, setSelection] = useState<WorkspaceSelection>(() => savedWorkspaceState.current?.mainSelection ?? ({
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

  useEffect(() => {
    if (!isWorkspaceStateReady) return
    const persistedQueryTabs = queryTabs.map((tab) => ({
      ...tab,
      results: [],
      error: '',
      isRunning: false,
      isSaving: false,
      saveNotice: '',
    }))
    const state: SavedWorkspaceState = {
      mainSelection: selection,
      pinnedTabs,
      queryTabs: persistedQueryTabs,
      reportTabs: reportTabs.map((tab) => ({ ...tab, error: '', isSaving: false, saveNotice: '' })),
      activeTabId,
    }
    try {
      window.localStorage.setItem(workspaceStateKey, JSON.stringify(state))
    } catch {
      // The workspace remains usable if local persistence is unavailable or full.
    }
  }, [activeTabId, isWorkspaceStateReady, pinnedTabs, queryTabs, reportTabs, selection])

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
      const widgetGuid = String(detail.row.widgetguid ?? '')
      const sourceTable = widgetGuid ? 'widg' : 'modlinfo'
      const sourceGuid = widgetGuid || moduleInfoGuid
      const infoKey = widgetGuid ? String(detail.row.widgetid ?? 'Widget') : String(detail.row.infokey ?? 'modlinfo')
      const sqlField = widgetGuid ? 'sqlstr' : 'infovalue'
      if (!sourceGuid || !detail.databaseName) return
      const id = `${sourceTable}-query-${sourceGuid}`
      setQueryTabs((tabs) => {
        if (tabs.some((tab) => tab.id === id)) return tabs
        return [...tabs, {
          id,
          title: `${infoKey} [${sourceGuid.slice(0, 8)}]`,
          databaseName: detail.databaseName,
          sql: String(detail.row[sqlField] ?? ''),
          results: [],
          error: '',
          isRunning: false,
          metadataSource: { sourceTable, row: detail.row, sqlField },
        }]
      })
      setActiveTabId(id)
    }

    window.addEventListener('oph:open-metadata-query', handleOpenMetadataQuery)
    return () => window.removeEventListener('oph:open-metadata-query', handleOpenMetadataQuery)
  }, [])

  useEffect(() => {
    function handleOpenReportDesigner(event: Event) {
      const detail = (event as CustomEvent<{ databaseName: string; row: MetadataRow }>).detail
      const moduleInfoGuid = String(detail.row.moduleinfoguid ?? '')
      if (!moduleInfoGuid || !detail.databaseName) return
      const id = `report-designer-${moduleInfoGuid}`
      setReportTabs((tabs) => {
        if (tabs.some((tab) => tab.id === id)) return tabs
        return [...tabs, {
          id,
          title: `Report — ${moduleInfoGuid.slice(0, 8)}`,
          databaseName: detail.databaseName,
          xml: String(detail.row.infovalue ?? ''),
          error: '',
          isSaving: false,
          saveNotice: '',
          metadataSource: { sourceTable: 'modlinfo', row: detail.row },
        }]
      })
      setActiveTabId(id)
    }

    window.addEventListener('oph:open-report-designer', handleOpenReportDesigner)
    return () => window.removeEventListener('oph:open-report-designer', handleOpenReportDesigner)
  }, [])

  function closeQueryTab(id: string) {
    setQueryTabs((tabs) => tabs.filter((tab) => tab.id !== id))
    if (activeTabId === id) setActiveTabId('main')
  }

  function updateReportTab(id: string, changes: Partial<ReportDesignerTab>) {
    setReportTabs((tabs) => tabs.map((tab) => tab.id === id ? { ...tab, ...changes } : tab))
  }

  function closeReportTab(id: string) {
    setReportTabs((tabs) => tabs.filter((tab) => tab.id !== id))
    if (activeTabId === id) setActiveTabId('main')
  }

  async function saveReportTab(tab: ReportDesignerTab) {
    if (!connectionConfig) return
    updateReportTab(tab.id, { isSaving: true, error: '', saveNotice: '' })
    const originalRow = tab.metadataSource.row
    const nextRow = { ...originalRow, infovalue: tab.xml }
    try {
      await ophAdminService.saveMetadataRow(
        connectionConfig,
        tab.databaseName,
        tab.metadataSource.sourceTable,
        originalRow,
        nextRow,
      )
      updateReportTab(tab.id, {
        isSaving: false,
        saveNotice: 'Report layout saved to modlinfo.dplx_rpt.',
        metadataSource: { ...tab.metadataSource, row: nextRow },
      })
    } catch (saveError) {
      updateReportTab(tab.id, { isSaving: false, error: saveError instanceof Error ? saveError.message : String(saveError) })
    }
  }

  function togglePinnedTab(nextSelection: WorkspaceSelection) {
    const id = `pinned-${nextSelection.id}`
    const isPinned = pinnedTabs.some((tab) => tab.id === id)
    if (isPinned) {
      setPinnedTabs((tabs) => tabs.filter((tab) => tab.id !== id))
      if (activeTabId === id) setActiveTabId('main')
      return
    }

    setPinnedTabs((tabs) => [...tabs, { id, selection: nextSelection }])
    setActiveTabId(id)
  }

  function closePinnedTab(id: string) {
    setPinnedTabs((tabs) => tabs.filter((tab) => tab.id !== id))
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
    const sqlField = tab.metadataSource.sqlField ?? 'infovalue'
    const nextRow = { ...originalRow, [sqlField]: tab.sql }
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
        saveNotice: `Saved directly to ${String(originalRow.infokey ?? originalRow.widgetid ?? tab.metadataSource.sourceTable)}.`,
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

  async function loadModuleGroupRowsByDatabase(config: OphConnectionConfig, databases: OphDatabase[]) {
    const entries = await Promise.all(databases.map(async (database) => {
      try {
        return [database.id, await ophAdminService.listModuleGroups(config, database.name, database.databaseName)] as const
      } catch {
        return [database.id, []] as const
      }
    }))
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
    const loadedModuleGroupRows = await loadModuleGroupRowsByDatabase(config, loadedDatabases)
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
      loadedModuleGroupRows,
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
    setModuleGroupRowsByDatabaseId(loadedModuleGroupRows)
    setInitialConnectionError('')
    setConnectionConfig(config)
    const restoredSelectionNode = preferredAccountId ? loadedDatabase : findTreeNode(loadedTree, selection.id)
    const nextSelectionNode = restoredSelectionNode ?? loadedDatabase ?? loadedTree
    setSelection(workspaceSelectionFromNode(nextSelectionNode))

    const restoredPinnedTabs = pinnedTabs.flatMap((tab) => {
      const currentNode = findTreeNode(loadedTree, tab.selection.id)
      return currentNode ? [{ id: tab.id, selection: workspaceSelectionFromNode(currentNode) }] : []
    })
    setPinnedTabs(restoredPinnedTabs)
    const availableTabIds = new Set(['main', ...restoredPinnedTabs.map((tab) => tab.id), ...queryTabs.map((tab) => tab.id), ...reportTabs.map((tab) => tab.id)])
    if (!availableTabIds.has(activeTabId)) setActiveTabId('main')
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
          setIsWorkspaceStateReady(true)
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

  async function refreshModuleTreeNode(node: OphTreeNode) {
    if (!connectionConfig) return

    if (node.kind === 'server') {
      await refreshServerConnection(node.serverId ?? node.id)
      return
    }

    if (!node.databaseId) return
    const database = discoveredDatabases.find((candidate) => candidate.id === node.databaseId)
    if (!database) return

    if ((node.kind === 'interface' && node.label === 'Menus') || node.kind === 'menu') {
      const menus = await ophAdminService.listMenus(connectionConfig, database.name, database.databaseName)
      setMenuRowsByDatabaseId((current) => ({ ...current, [database.id]: menus }))
      return
    }

    if (node.kind === 'module-category' && node.label === 'Module Groups') {
      const groups = await ophAdminService.listModuleGroups(connectionConfig, database.name, database.databaseName)
      setModuleGroupRowsByDatabaseId((current) => ({ ...current, [database.id]: groups }))
      return
    }

    if (node.kind === 'module-category' && node.settingMode !== undefined) {
      const [modules, columns] = await Promise.all([
        ophAdminService.listModuleTree(connectionConfig, database.name, database.databaseName),
        ophAdminService.listModuleColumnTree(connectionConfig, database.name, database.databaseName),
      ])
      setModuleRowsByDatabaseId((current) => ({ ...current, [database.id]: modules }))
      setColumnRowsByDatabaseId((current) => ({ ...current, [database.id]: columns }))
      return
    }

    if (node.kind === 'module') {
      const [modules, columns] = await Promise.all([
        ophAdminService.listModuleTree(connectionConfig, database.name, database.databaseName),
        ophAdminService.listModuleColumnTree(connectionConfig, database.name, database.databaseName),
      ])
      setModuleRowsByDatabaseId((current) => ({ ...current, [database.id]: modules }))
      setColumnRowsByDatabaseId((current) => ({ ...current, [database.id]: columns }))
      return
    }

    if (node.kind === 'module-column' || (node.kind === 'module-action' && node.label === 'Columns')) {
      const columns = await ophAdminService.listModuleColumnTree(connectionConfig, database.name, database.databaseName)
      setColumnRowsByDatabaseId((current) => ({ ...current, [database.id]: columns }))
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
    try {
      await ophAdminService.deleteAccount(connectionConfig, serverId, accountId)
      appendAuditLog('delete-account', accountId, 'success', `Account marked as deleted on server ${serverId}.`)
      await refreshServerConnection(serverId)
    } catch (error) {
      appendAuditLog('delete-account', accountId, 'failed', error instanceof Error ? error.message : String(error))
      throw error
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

  if (isAddingConnection) {
    return (
      <AddConnectionScreen
        onSave={addConnection}
        onCancel={() => setIsAddingConnection(false)}
      />
    )
  }

  const activeQueryTab = queryTabs.find((tab) => tab.id === activeTabId)
  const activeReportTab = reportTabs.find((tab) => tab.id === activeTabId)
  const activePinnedTab = pinnedTabs.find((tab) => tab.id === activeTabId)
  const activeSelection = activePinnedTab?.selection ?? selection

  function navigateToFirst(kind: TreeNodeKind) {
    const node = findFirstTreeNodeByKind(tree!, kind)
    if (node) {
      setSelection(workspaceSelectionFromNode(node))
      setActiveTabId('main')
    }
    setIsMobileExplorerOpen(false)
  }

  return (
    <div className="app-shell">
      {isMobileExplorerOpen ? (
        <button
          type="button"
          className="mobile-sidebar-scrim"
          aria-label="Close explorer"
          onClick={() => setIsMobileExplorerOpen(false)}
        />
      ) : null}
      <aside className={isMobileExplorerOpen ? 'sidebar mobile-sidebar-open' : 'sidebar'}>
        <div className="brand-block">
          <div className="brand-mark">OPH</div>
          <div>
            <strong>OPH Control Studio</strong>
            <span>Connection workspace</span>
          </div>
          <button type="button" className="mobile-sidebar-close" aria-label="Close explorer" onClick={() => setIsMobileExplorerOpen(false)}>
            <X size={20} />
          </button>
        </div>
        <TreeView
          root={tree}
          selectionId={activeSelection.id}
          pinnedIds={new Set(pinnedTabs.map((tab) => tab.selection.id))}
          onPin={togglePinnedTab}
          onRefresh={refreshModuleTreeNode}
          onSelect={(nextSelection) => {
          setSelection(nextSelection)
          setActiveTabId('main')
          setIsMobileExplorerOpen(false)
          }}
        />
      </aside>

      <main className="workspace">
        <header className="topbar">
          <button type="button" className="mobile-explorer-button" aria-label="Open server explorer" onClick={() => setIsMobileExplorerOpen(true)}>
            <MenuBars size={21} />
          </button>
          <div className="mobile-page-title">
            <small>{activeSelection.databaseName ?? 'OPH Control Studio'}</small>
            <strong>{activeQueryTab?.title ?? activeReportTab?.title ?? activeSelection.label}</strong>
          </div>
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
                  {pinnedTabs.map((tab) => (
                    <button key={tab.id} type="button" className={activeTabId === tab.id ? 'active-menu-item' : ''} onClick={() => { setActiveTabId(tab.id); setOpenAppMenu(null) }}>
                      {tab.selection.label} <small>Pinned</small>
                    </button>
                  ))}
                  {queryTabs.map((tab) => (
                    <button key={tab.id} type="button" className={activeTabId === tab.id ? 'active-menu-item' : ''} onClick={() => { setActiveTabId(tab.id); setOpenAppMenu(null) }}>
                      {tab.title} <small>{tab.databaseName || 'No database'}</small>
                    </button>
                  ))}
                  {reportTabs.map((tab) => (
                    <button key={tab.id} type="button" className={activeTabId === tab.id ? 'active-menu-item' : ''} onClick={() => { setActiveTabId(tab.id); setOpenAppMenu(null) }}>
                      {tab.title} <small>{tab.databaseName}</small>
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
          {pinnedTabs.map((tab) => (
            <div key={tab.id} className={activeTabId === tab.id ? 'workspace-tab active-workspace-tab' : 'workspace-tab'}>
              <button type="button" onClick={() => setActiveTabId(tab.id)}><Pin size={12} /> {tab.selection.label}</button>
              <button type="button" className="tab-close-button" aria-label={`Close ${tab.selection.label}`} onClick={() => closePinnedTab(tab.id)}><X size={13} /></button>
            </div>
          ))}
          {queryTabs.map((tab) => (
            <div key={tab.id} className={activeTabId === tab.id ? 'workspace-tab active-workspace-tab' : 'workspace-tab'}>
              <button type="button" onClick={() => setActiveTabId(tab.id)}>{tab.title}</button>
              <button type="button" className="tab-close-button" aria-label={`Close ${tab.title}`} onClick={() => closeQueryTab(tab.id)}><X size={13} /></button>
            </div>
          ))}
          {reportTabs.map((tab) => (
            <div key={tab.id} className={activeTabId === tab.id ? 'workspace-tab active-workspace-tab' : 'workspace-tab'}>
              <button type="button" onClick={() => setActiveTabId(tab.id)}><FileCode2 size={12} /> {tab.title}</button>
              <button type="button" className="tab-close-button" aria-label={`Close ${tab.title}`} onClick={() => closeReportTab(tab.id)}><X size={13} /></button>
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
            ) : activeReportTab ? (
              <ReportDesignerWorkspace
                tab={activeReportTab}
                onChange={(xml) => updateReportTab(activeReportTab.id, { xml, error: '', saveNotice: '' })}
                onSave={() => saveReportTab(activeReportTab)}
              />
            ) : (
              <Workspace
                connectionConfig={connectionConfig}
                connectionError={initialConnectionError}
                onAddConnection={() => setIsAddingConnection(true)}
                onRefreshConnection={refreshConnection}
                onRefreshServer={refreshServerConnection}
                onDeleteAccount={deleteAccount}
                onRefreshTreeNode={refreshModuleTreeNode}
                onNavigate={(nextSelection) => {
                  setSelection(nextSelection)
                  setActiveTabId('main')
                }}
                selection={activeSelection}
                servers={servers}
                tree={tree}
              />
            )}
          </section>
        </div>
        <nav className="mobile-bottom-nav" aria-label="Primary navigation">
          <button type="button" className={activeTabId === 'main' && activeSelection.kind === 'root' ? 'active' : ''} onClick={() => navigateToFirst('root')}>
            <LayoutDashboard size={20} /><span>Home</span>
          </button>
          <button type="button" onClick={() => setIsMobileExplorerOpen(true)}>
            <FolderTree size={20} /><span>Explorer</span>
          </button>
          <button type="button" className={activeQueryTab ? 'active' : ''} onClick={openNewQuery}>
            <FileCode2 size={20} /><span>Query</span>
          </button>
          <button type="button" onClick={() => setIsAddingConnection(true)}>
            <Server size={20} /><span>Servers</span>
          </button>
        </nav>
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

const dplxBandNames = ['template', 'header', 'detail', 'footer'] as const
const dplxElementNames = new Set(['label', 'symbol', 'recordBox', 'recordArea', 'rectangle', 'placeholder', 'line', 'image', 'subReport', 'contentGroup', 'formattedRecordArea', 'pageBreak', 'noSplitZone', 'softBreak', 'pageNumberingLabel'])
const dplxTypographyElementNames = new Set(['label', 'recordBox'])
const dplxTypographyAttributes = new Set(['align', 'fontSize', 'font'])

type DplxRenderElement = {
  element: Element
  index: number
  xOffset: number
  yOffset: number
}

function collectDplxRenderElements(band: Element): DplxRenderElement[] {
  const result: DplxRenderElement[] = []
  Array.from(band.children).forEach((element, index) => {
    if (!dplxElementNames.has(element.tagName)) return
    result.push({ element, index, xOffset: 0, yOffset: 0 })
    if (element.tagName !== 'contentGroup') return
    const groupX = Number(element.getAttribute('x') ?? 0)
    const groupY = Number(element.getAttribute('y') ?? 0)
    Array.from(element.children).forEach((child) => {
      if (dplxElementNames.has(child.tagName) && child.tagName !== 'contentGroup') {
        result.push({ element: child, index, xOffset: groupX, yOffset: groupY })
      }
    })
  })
  return result
}

function dplxElementText(element: Element) {
  if (element.tagName === 'label') return element.getAttribute('text') ?? ''
  if (element.tagName === 'symbol') return element.getAttribute('text') || element.getAttribute('symbol') || '●'
  if (['recordBox', 'recordArea'].includes(element.tagName)) return `[${element.getAttribute('field') || 'field'}]`
  if (element.tagName === 'image') return `[Image: ${element.getAttribute('path') || 'Select image'}]`
  if (element.tagName === 'pageNumberingLabel') return element.getAttribute('text') || 'Page %%CP%% of %%TP%%'
  if (element.tagName === 'placeholder') return `[${element.getAttribute('name') || 'Placeholder'}]`
  if (element.tagName === 'pageBreak') return 'Page Break'
  if (element.tagName === 'noSplitZone') return 'No Split Zone'
  if (element.tagName === 'softBreak') return 'Soft Break'
  if (element.tagName === 'formattedRecordArea') {
    const source = Array.from(element.children).find((child) => child.tagName === 'text')?.textContent ?? ''
    const html = new DOMParser().parseFromString(source.replace(/<br\s*\/?>/gi, '\n').replace(/<\/p>/gi, '\n'), 'text/html')
    return (html.body.textContent ?? '').replace(/\u00a0/g, ' ').trim()
  }
  return element.tagName === 'subReport' ? 'subReport' : ''
}

function DplxAttributeInput({ name, value, onChange }: { name: string; value: string; onChange: (value: string) => void }) {
  if (!['x', 'y', 'x1', 'x2', 'y1', 'y2', 'width', 'height'].includes(name)) return <input value={value} onChange={(event) => onChange(event.target.value)} />
  const adjust = (amount: number) => {
    const current = Number(value)
    onChange(String((Number.isFinite(current) ? current : 0) + amount))
  }
  return (
    <div className="report-pixel-input">
      <input type="number" step="any" value={value} onChange={(event) => onChange(event.target.value)} />
      <div className="report-pixel-stepper">
        <button type="button" aria-label={`Increase ${name} by 1 pixel`} onClick={() => adjust(1)}>▲</button>
        <button type="button" aria-label={`Decrease ${name} by 1 pixel`} onClick={() => adjust(-1)}>▼</button>
      </div>
    </div>
  )
}

function parseDplx(xml: string): { document: XMLDocument | null; report: Element | null; error: string } {
  if (!xml.trim()) return { document: null, report: null, error: 'DPLX XML is empty.' }
  const document = new DOMParser().parseFromString(xml, 'application/xml')
  const parserError = document.querySelector('parsererror')
  if (parserError) return { document: null, report: null, error: parserError.textContent?.trim() || 'Invalid DPLX XML.' }
  const report = document.documentElement.tagName === 'report' ? document.documentElement : document.querySelector('report')
  return report ? { document, report, error: '' } : { document, report: null, error: 'DPLX report node not found.' }
}

function ReportDesignerWorkspace({
  tab,
  onChange,
  onSave,
}: {
  tab: ReportDesignerTab
  onChange: (xml: string) => void
  onSave: () => void
}) {
  const [mode, setMode] = useState<'design' | 'xml'>('design')
  const [selectedBand, setSelectedBand] = useState<string>('detail')
  const [selectedElement, setSelectedElement] = useState<{ band: string; index: number } | null>(null)
  const [selectedSubReportBand, setSelectedSubReportBand] = useState<string | null>(null)
  const [selectedSubReportElement, setSelectedSubReportElement] = useState<{ band: string; index: number } | null>(null)
  const [fieldType, setFieldType] = useState<'recordBox' | 'recordArea' | 'formattedRecordArea'>('recordBox')
  const [labelType, setLabelType] = useState<'label' | 'symbol'>('label')
  const [imageType, setImageType] = useState<'image' | 'placeholder'>('image')
  const [breakType, setBreakType] = useState<'pageBreak' | 'noSplitZone' | 'softBreak'>('pageBreak')
  const [previewZoom, setPreviewZoom] = useState(100)
  const parsed = useMemo(() => parseDplx(tab.xml), [tab.xml])
  const bands = parsed.report ? dplxBandNames.flatMap((name) => Array.from(parsed.report?.children ?? []).filter((child) => child.tagName === name)) : []
  const activeElement = selectedElement && parsed.report
    ? Array.from(parsed.report.children)
      .find((child) => child.tagName === selectedElement.band)
      ?.children.item(selectedElement.index) ?? null
    : null
  const selectedSubReport = activeElement && ['subReport', 'contentGroup', 'conditionalFooter'].includes(activeElement.tagName) ? activeElement : null
  const selectedContainerType = selectedSubReport?.tagName === 'contentGroup'
    ? 'Content Group'
    : selectedSubReport?.tagName === 'conditionalFooter'
      ? 'Conditional Footer'
      : 'Subreport'
  const selectedFlatContainer = selectedSubReport && ['contentGroup', 'conditionalFooter'].includes(selectedSubReport.tagName)
  const activeSubReportBand = selectedSubReport && selectedSubReportBand
    ? selectedFlatContainer
      ? null
      : Array.from(selectedSubReport.children).find((child) => child.tagName === selectedSubReportBand) ?? null
    : null
  const activeSubReportElement = selectedSubReport && selectedSubReportElement
    ? selectedFlatContainer
      ? selectedSubReport.children.item(selectedSubReportElement.index)
      : Array.from(selectedSubReport.children)
        .find((child) => child.tagName === selectedSubReportElement.band)
        ?.children.item(selectedSubReportElement.index) ?? null
    : null
  const activeBand = !selectedElement && parsed.report && dplxBandNames.includes(selectedBand as typeof dplxBandNames[number])
    ? Array.from(parsed.report.children).find((child) => child.tagName === selectedBand) ?? null
    : null
  const pageSizes: Record<string, { width: number; height: number }> = {
    letter: { width: 612, height: 792 },
    legal: { width: 612, height: 1008 },
    a4: { width: 595, height: 842 },
    a3: { width: 842, height: 1191 },
    a5: { width: 420, height: 595 },
  }
  const reportPageSize = String(parsed.report?.getAttribute('pageSize') || 'letter').toLowerCase()
  const standardPage = pageSizes[reportPageSize] ?? pageSizes.letter
  const explicitWidth = Number(parsed.report?.getAttribute('pageWidth') || 0)
  const explicitHeight = Number(parsed.report?.getAttribute('pageHeight') || 0)
  const declaredOrientation = String(parsed.report?.getAttribute('pageOrientation') || parsed.report?.getAttribute('orientation') || '').toLowerCase()
  const orientedStandardPage = declaredOrientation === 'landscape'
    ? { width: Math.max(standardPage.width, standardPage.height), height: Math.min(standardPage.width, standardPage.height) }
    : standardPage
  const baseWidth = explicitWidth > 0 ? explicitWidth : orientedStandardPage.width
  const baseHeight = explicitHeight > 0 ? explicitHeight : orientedStandardPage.height
  const pageWidth = baseWidth
  const pageHeight = baseHeight
  const orientation = pageWidth > pageHeight ? 'landscape' : 'portrait'
  const pageMargins = {
    top: Number(parsed.report?.getAttribute('topMargin') || 50),
    right: Number(parsed.report?.getAttribute('rightMargin') || 50),
    bottom: Number(parsed.report?.getAttribute('bottomMargin') || 50),
    left: Number(parsed.report?.getAttribute('leftMargin') || 50),
  }
  const reportBodyWidth = Math.max(100, pageWidth - pageMargins.left - pageMargins.right)
  const reportBodyHeight = Math.max(100, pageHeight - pageMargins.top - pageMargins.bottom)
  const visibleBands = selectedSubReport
    ? selectedFlatContainer
      ? [selectedSubReport]
      : Array.from(selectedSubReport.children).filter((child) => ['header', 'detail', 'footer'].includes(child.tagName) && Number(child.getAttribute('height') ?? 0) > 0)
    : selectedBand === 'template'
      ? bands.filter((band) => band.tagName === 'template')
      : bands.filter((band) => band.tagName !== 'template' && (!['header', 'footer'].includes(band.tagName) || Number(band.getAttribute('height') ?? 0) > 0))
  const previewBodyWidth = selectedSubReport
    ? Math.max(100, Number(selectedSubReport.getAttribute('width') || reportBodyWidth))
    : reportBodyWidth

  function changeXml(mutator: (document: XMLDocument, report: Element) => void) {
    const next = parseDplx(tab.xml)
    if (!next.document || !next.report) return
    mutator(next.document, next.report)
    onChange(new XMLSerializer().serializeToString(next.document))
  }

  function updateElementAttribute(name: string, value: string) {
    if (!selectedElement) return
    changeXml((_document, report) => {
      const band = Array.from(report.children).find((child) => child.tagName === selectedElement.band)
      band?.children.item(selectedElement.index)?.setAttribute(name, value)
    })
  }

  function updateBandAttribute(name: string, value: string) {
    changeXml((_document, report) => {
      Array.from(report.children).find((child) => child.tagName === selectedBand)?.setAttribute(name, value)
    })
  }

  function updateSubReportBandAttribute(name: string, value: string) {
    if (!selectedElement || !selectedSubReportBand) return
    changeXml((_document, report) => {
      const parentBand = Array.from(report.children).find((child) => child.tagName === selectedElement.band)
      const subReport = parentBand?.children.item(selectedElement.index)
      if (subReport && ['contentGroup', 'conditionalFooter'].includes(subReport.tagName) && selectedSubReportBand === 'detail') subReport.setAttribute(name, value)
      else Array.from(subReport?.children ?? []).find((child) => child.tagName === selectedSubReportBand)?.setAttribute(name, value)
    })
  }

  function updateSubReportElementAttribute(name: string, value: string) {
    if (!selectedElement || !selectedSubReportElement) return
    changeXml((_document, report) => {
      const parentBand = Array.from(report.children).find((child) => child.tagName === selectedElement.band)
      const subReport = parentBand?.children.item(selectedElement.index)
      const internalBand = subReport && ['contentGroup', 'conditionalFooter'].includes(subReport.tagName)
        ? subReport
        : Array.from(subReport?.children ?? []).find((child) => child.tagName === selectedSubReportElement.band)
      internalBand?.children.item(selectedSubReportElement.index)?.setAttribute(name, value)
    })
  }

  function deleteSubReportElement() {
    if (!selectedElement || !selectedSubReportElement) return
    changeXml((_document, report) => {
      const parentBand = Array.from(report.children).find((child) => child.tagName === selectedElement.band)
      const subReport = parentBand?.children.item(selectedElement.index)
      const internalBand = subReport && ['contentGroup', 'conditionalFooter'].includes(subReport.tagName)
        ? subReport
        : Array.from(subReport?.children ?? []).find((child) => child.tagName === selectedSubReportElement.band)
      internalBand?.children.item(selectedSubReportElement.index)?.remove()
    })
    setSelectedSubReportElement(null)
  }

  function updateReportAttribute(name: string, value: string) {
    changeXml((_document, report) => report.setAttribute(name, value))
  }

  function queryContainer(report: Element, target: 'report' | 'subreport') {
    if (target === 'report') return report
    if (!selectedElement) return null
    const parentBand = Array.from(report.children).find((child) => child.tagName === selectedElement.band)
    const selected = parentBand?.children.item(selectedElement.index) ?? null
    return selected?.tagName === 'subReport' ? selected : null
  }

  function ensureStoredProcedure(document: XMLDocument, container: Element) {
    let query = Array.from(container.children).find((child) => child.tagName === 'query')
    if (!query) {
      query = document.createElement('query')
      container.insertBefore(query, container.firstChild)
    }
    let procedure = Array.from(query.children).find((child) => child.tagName === 'storedProcedure')
    if (!procedure) {
      procedure = document.createElement('storedProcedure')
      procedure.setAttribute('name', '')
      query.appendChild(procedure)
    }
    return procedure
  }

  function updateQueryProcedure(target: 'report' | 'subreport', value: string) {
    changeXml((document, report) => {
      const container = queryContainer(report, target)
      if (container) ensureStoredProcedure(document, container).setAttribute('name', value)
    })
  }

  function updateQueryParameter(target: 'report' | 'subreport', index: number, name: string, value: string) {
    changeXml((document, report) => {
      const container = queryContainer(report, target)
      const parameter = container ? Array.from(ensureStoredProcedure(document, container).children).filter((child) => child.tagName === 'parameter')[index] : null
      parameter?.setAttribute(name, value)
    })
  }

  function addQueryParameter(target: 'report' | 'subreport') {
    changeXml((document, report) => {
      const container = queryContainer(report, target)
      if (!container) return
      const parameter = document.createElement('parameter')
      parameter.setAttribute('name', '@parameter')
      parameter.setAttribute('type', 'nvarchar')
      parameter.setAttribute('value', '')
      ensureStoredProcedure(document, container).appendChild(parameter)
    })
  }

  function queryProperties(container: Element | null, target: 'report' | 'subreport') {
    const query = container ? Array.from(container.children).find((child) => child.tagName === 'query') : null
    const procedure = query ? Array.from(query.children).find((child) => child.tagName === 'storedProcedure') : null
    const parameters = procedure ? Array.from(procedure.children).filter((child) => child.tagName === 'parameter') : []
    return (
      <div className="report-query-properties">
        <strong>Query</strong>
        <label>
          <span>Stored procedure</span>
          <input value={procedure?.getAttribute('name') ?? ''} placeholder="Procedure name" onChange={(event) => updateQueryProcedure(target, event.target.value)} />
        </label>
        {parameters.map((parameter, index) => (
          <div key={index} className="report-query-parameter">
            <span>Parameter {index + 1}</span>
            <input aria-label={`Parameter ${index + 1} name`} value={parameter.getAttribute('name') ?? ''} placeholder="Name" onChange={(event) => updateQueryParameter(target, index, 'name', event.target.value)} />
            <input aria-label={`Parameter ${index + 1} type`} value={parameter.getAttribute('type') ?? ''} placeholder="Type" onChange={(event) => updateQueryParameter(target, index, 'type', event.target.value)} />
            <input aria-label={`Parameter ${index + 1} value`} value={parameter.getAttribute('value') ?? ''} placeholder="Value" onChange={(event) => updateQueryParameter(target, index, 'value', event.target.value)} />
          </div>
        ))}
        <button type="button" onClick={() => addQueryParameter(target)}>+ Query parameter</button>
      </div>
    )
  }

  function updatePageSize(value: string) {
    changeXml((_document, report) => {
      report.setAttribute('pageSize', value.toLowerCase())
      if (value === 'custom') {
        report.setAttribute('pageWidth', String(baseWidth))
        report.setAttribute('pageHeight', String(baseHeight))
      } else {
        const dimensions = pageSizes[value] ?? pageSizes.letter
        report.setAttribute('pageWidth', String(orientation === 'landscape' ? dimensions.height : dimensions.width))
        report.setAttribute('pageHeight', String(orientation === 'landscape' ? dimensions.width : dimensions.height))
      }
      report.removeAttribute('pageOrientation')
      report.removeAttribute('orientation')
    })
  }

  function updatePageOrientation(value: string) {
    changeXml((_document, report) => {
      const shortSide = Math.min(baseWidth, baseHeight)
      const longSide = Math.max(baseWidth, baseHeight)
      report.setAttribute('pageWidth', String(value === 'landscape' ? longSide : shortSide))
      report.setAttribute('pageHeight', String(value === 'landscape' ? shortSide : longSide))
      report.removeAttribute('pageOrientation')
      report.removeAttribute('orientation')
    })
  }

  function updateCustomPageDimension(name: 'pageWidth' | 'pageHeight', value: string) {
    changeXml((_document, report) => {
      report.setAttribute('pageSize', 'custom')
      report.setAttribute('pageWidth', name === 'pageWidth' ? value : String(baseWidth))
      report.setAttribute('pageHeight', name === 'pageHeight' ? value : String(baseHeight))
    })
  }

  function addElement(name: 'label' | 'symbol' | 'recordBox' | 'recordArea' | 'rectangle' | 'placeholder' | 'line' | 'image' | 'pageBreak' | 'noSplitZone' | 'softBreak' | 'pageNumberingLabel') {
    changeXml((document, report) => {
      let band = Array.from(report.children).find((child) => child.tagName === selectedBand)
      if (!band) {
        band = document.createElement(selectedBand)
        if (selectedBand === 'detail') band.setAttribute('autoSplit', 'false')
        report.appendChild(band)
      }
      const contentGroup = selectedFlatContainer && selectedElement
        ? band.children.item(selectedElement.index)
        : null
      const target = contentGroup && ['contentGroup', 'conditionalFooter'].includes(contentGroup.tagName) ? contentGroup : band
      const element = document.createElement(name)
      if (['pageBreak', 'softBreak'].includes(name)) {
        Object.entries({ x: '0', y: '40', width: String(reportBodyWidth), height: '2' }).forEach(([key, value]) => element.setAttribute(key, value))
      } else if (name === 'noSplitZone') {
        Object.entries({ x: '0', y: '20', width: String(reportBodyWidth), height: '80' }).forEach(([key, value]) => element.setAttribute(key, value))
      } else if (name === 'line') {
        Object.entries({ x1: '20', y1: '20', x2: '160', y2: '20' }).forEach(([key, value]) => element.setAttribute(key, value))
      } else {
        Object.entries({ x: '20', y: '20', width: '140', height: '20' }).forEach(([key, value]) => element.setAttribute(key, value))
        if (name === 'label') element.setAttribute('text', 'New label')
        if (dplxTypographyElementNames.has(name)) {
          element.setAttribute('align', 'left')
          element.setAttribute('fontSize', '10')
          element.setAttribute('font', 'Helvetica')
        }
        if (name === 'symbol') element.setAttribute('text', '●')
        if (name === 'placeholder') element.setAttribute('name', 'Placeholder')
        if (name === 'pageNumberingLabel') {
          element.setAttribute('text', 'Page %%CP%% of %%TP%%')
          element.setAttribute('align', 'right')
          element.setAttribute('width', '200')
        }
        if (name === 'image') element.setAttribute('path', 'image.png')
        if (['recordBox', 'recordArea'].includes(name)) {
          element.setAttribute('field', 'FieldName')
          element.setAttribute('expandable', 'false')
        }
      }
      target.appendChild(element)
      if (target === band) setSelectedElement({ band: selectedBand, index: band.children.length - 1 })
      else {
        setSelectedSubReportBand(null)
        setSelectedSubReportElement({ band: target.tagName, index: target.children.length - 1 })
      }
    })
  }

  function addRecordArea() {
    changeXml((document, report) => {
      let band = Array.from(report.children).find((child) => child.tagName === selectedBand)
      if (!band) {
        band = document.createElement(selectedBand)
        report.appendChild(band)
      }
      const contentGroup = selectedFlatContainer && selectedElement
        ? band.children.item(selectedElement.index)
        : null
      const target = contentGroup && ['contentGroup', 'conditionalFooter'].includes(contentGroup.tagName) ? contentGroup : band
      const area = document.createElement('formattedRecordArea')
      Object.entries({ x: '20', y: '20', width: '180', height: '40', expandable: 'true' }).forEach(([key, value]) => area.setAttribute(key, value))
      const text = document.createElement('text')
      text.appendChild(document.createCDATASection('<P>#FieldName#</P>'))
      area.appendChild(text)
      target.appendChild(area)
      if (target === band) setSelectedElement({ band: selectedBand, index: band.children.length - 1 })
      else {
        setSelectedSubReportBand(null)
        setSelectedSubReportElement({ band: target.tagName, index: target.children.length - 1 })
      }
    })
  }

  function addField() {
    if (fieldType === 'formattedRecordArea') addRecordArea()
    else addElement(fieldType)
  }

  function addContentGroup() {
    const targetBand = ['header', 'detail', 'footer'].includes(selectedBand) ? selectedBand : 'detail'
    changeXml((document, report) => {
      let band = Array.from(report.children).find((child) => child.tagName === targetBand)
      if (!band) {
        band = document.createElement(targetBand)
        report.appendChild(band)
      }
      const group = document.createElement('contentGroup')
      Object.entries({ x: '20', y: '20', width: '220', height: '100' }).forEach(([key, value]) => group.setAttribute(key, value))
      band.appendChild(group)
      setSelectedBand(targetBand)
      setSelectedElement({ band: targetBand, index: band.children.length - 1 })
      setSelectedSubReportBand(null)
      setSelectedSubReportElement(null)
    })
  }

  function addSubReport() {
    const targetBand = ['header', 'detail', 'footer'].includes(selectedBand) ? selectedBand : 'detail'
    changeXml((document, report) => {
      let band = Array.from(report.children).find((child) => child.tagName === targetBand)
      if (!band) {
        band = document.createElement(targetBand)
        report.appendChild(band)
      }
      const subReport = document.createElement('subReport')
      Object.entries({ x: '20', y: '20', width: '472', height: '120' }).forEach(([key, value]) => subReport.setAttribute(key, value))
      ;['header', 'detail', 'footer'].forEach((bandName) => {
        const childBand = document.createElement(bandName)
        childBand.setAttribute('height', bandName === 'detail' ? '40' : '20')
        if (bandName === 'detail') childBand.setAttribute('autoSplit', 'false')
        subReport.appendChild(childBand)
      })
      band.appendChild(subReport)
      setSelectedBand(targetBand)
      setSelectedElement({ band: targetBand, index: band.children.length - 1 })
      setSelectedSubReportBand(null)
      setSelectedSubReportElement(null)
    })
  }

  function addConditionalFooter() {
    changeXml((document, report) => {
      let footer = Array.from(report.children).find((child) => child.tagName === 'footer')
      if (!footer) {
        footer = document.createElement('footer')
        footer.setAttribute('height', '80')
        report.appendChild(footer)
      }
      const conditionalFooter = document.createElement('conditionalFooter')
      conditionalFooter.setAttribute('id', `ConditionalFooter${Array.from(footer.children).filter((child) => child.tagName === 'conditionalFooter').length + 1}`)
      conditionalFooter.setAttribute('condition', 'LastPage')
      footer.appendChild(conditionalFooter)
      setSelectedBand('footer')
      setSelectedElement({ band: 'footer', index: footer.children.length - 1 })
      setSelectedSubReportBand(null)
      setSelectedSubReportElement(null)
    })
  }

  function deleteElement() {
    if (!selectedElement) return
    changeXml((_document, report) => {
      const band = Array.from(report.children).find((child) => child.tagName === selectedElement.band)
      band?.children.item(selectedElement.index)?.remove()
    })
    setSelectedElement(null)
    setSelectedSubReportBand(null)
    setSelectedSubReportElement(null)
  }

  return (
    <div className="report-designer-workspace">
      <div className="report-designer-header">
        <div>
          <span className="eyebrow">DPLX Report Designer</span>
          <h1>{tab.title}</h1>
          <small>{tab.databaseName} · database locked to metadata source</small>
        </div>
        <div className="report-designer-actions">
          {mode === 'design' ? (
            <div className="report-zoom-controls" aria-label="Preview zoom">
              <button type="button" aria-label="Zoom out" disabled={previewZoom <= 25} onClick={() => setPreviewZoom((value) => Math.max(25, value - 25))}>−</button>
              <button type="button" className="report-zoom-value" title="Reset zoom" onClick={() => setPreviewZoom(100)}>{previewZoom}%</button>
              <button type="button" aria-label="Zoom in" disabled={previewZoom >= 200} onClick={() => setPreviewZoom((value) => Math.min(200, value + 25))}>+</button>
            </div>
          ) : null}
          <button type="button" className={mode === 'design' ? 'active-tool' : ''} onClick={() => setMode('design')}>Design</button>
          <button type="button" className={mode === 'xml' ? 'active-tool' : ''} onClick={() => setMode('xml')}>XML</button>
          <button type="button" disabled={tab.isSaving || Boolean(parsed.error)} onClick={onSave}>{tab.isSaving ? 'Saving…' : 'Save Report'}</button>
        </div>
      </div>
      {tab.error || parsed.error ? <div className="connection-error">{tab.error || parsed.error}</div> : null}
      {tab.saveNotice ? <div className="action-notice">{tab.saveNotice}</div> : null}
      {mode === 'xml' ? (
        <textarea className="report-xml-editor" spellCheck={false} value={tab.xml} onChange={(event) => onChange(event.target.value)} />
      ) : (
        <div className="report-designer-grid">
          <aside className="report-toolbox">
            <strong>Page</strong>
            <button type="button" className={selectedBand === 'page' ? 'active-tool' : ''} onClick={() => { setSelectedBand('page'); setSelectedElement(null); setSelectedSubReportBand(null); setSelectedSubReportElement(null) }}>Page</button>
            <strong>Bands</strong>
            {dplxBandNames.map((bandName) => {
              const band = parsed.report ? Array.from(parsed.report.children).find((child) => child.tagName === bandName) : null
              const nestedContainers = band ? Array.from(band.children).map((element, index) => ({ element, index })).filter(({ element }) => ['subReport', 'contentGroup', 'conditionalFooter'].includes(element.tagName)) : []
              return (
                <div key={bandName} className="report-band-tree-item">
                  <button type="button" className={selectedBand === bandName && !selectedElement ? 'active-tool' : ''} onClick={() => { setSelectedBand(bandName); setSelectedElement(null); setSelectedSubReportBand(null); setSelectedSubReportElement(null) }}>{bandName}</button>
                  {bandName !== 'template' ? nestedContainers.map(({ element, index }, containerIndex) => (
                    <div key={index} className="report-subreport-tree-item">
                      <button type="button" className={selectedBand === bandName && selectedElement?.index === index && !selectedSubReportBand ? 'active-tool report-subreport-active' : 'report-subreport-button'} onClick={() => { setSelectedBand(bandName); setSelectedElement({ band: bandName, index }); setSelectedSubReportBand(null); setSelectedSubReportElement(null) }}>
                        ↳ {element.tagName === 'contentGroup' ? 'Content Group' : element.tagName === 'conditionalFooter' ? 'Conditional Footer' : 'Subreport'} {containerIndex + 1}
                      </button>
                      {(['contentGroup', 'conditionalFooter'].includes(element.tagName) ? [] : Array.from(element.children).filter((child) => ['header', 'detail', 'footer'].includes(child.tagName)).map((child) => child.tagName)).map((childName) => (
                        <button key={childName} type="button" className={selectedBand === bandName && selectedElement?.index === index && selectedSubReportBand === childName && !selectedSubReportElement ? 'report-subreport-band-button active-tool' : 'report-subreport-band-button'} onClick={() => { setSelectedBand(bandName); setSelectedElement({ band: bandName, index }); setSelectedSubReportBand(childName); setSelectedSubReportElement(null) }}>
                          {childName}
                        </button>
                      ))}
                    </div>
                  )) : null}
                </div>
              )
            })}
            <strong>Controls</strong>
            <div className="report-toolbox-control-row">
              <label className="report-toolbox-field">
                <span>Label type</span>
                <select value={labelType} onChange={(event) => setLabelType(event.target.value as typeof labelType)}>
                  <option value="label">label</option>
                  <option value="symbol">symbol</option>
                </select>
              </label>
              <button type="button" onClick={() => addElement(labelType)}>+ Label</button>
            </div>
            <label className="report-toolbox-field">
              <span>Field type</span>
              <select value={fieldType} onChange={(event) => setFieldType(event.target.value as typeof fieldType)}>
                <option value="recordBox">recordBox</option>
                <option value="recordArea">recordArea</option>
                <option value="formattedRecordArea">formattedRecordArea</option>
              </select>
            </label>
            <button type="button" onClick={addField}>+ Field</button>
            <button type="button" onClick={() => addElement('rectangle')}>+ Rectangle</button>
            <label className="report-toolbox-field">
              <span>Break type</span>
              <select value={breakType} onChange={(event) => setBreakType(event.target.value as typeof breakType)}>
                <option value="pageBreak">pageBreak</option>
                <option value="noSplitZone">noSplitZone</option>
                <option value="softBreak">softBreak</option>
              </select>
            </label>
            <button type="button" onClick={() => addElement(breakType)}>+ Break</button>
            <button type="button" onClick={() => addElement('pageNumberingLabel')}>+ Page Numbering</button>
            <button type="button" onClick={() => addElement('line')}>+ Line</button>
            <label className="report-toolbox-field">
              <span>Image type</span>
              <select value={imageType} onChange={(event) => setImageType(event.target.value as typeof imageType)}>
                <option value="image">image</option>
                <option value="placeholder">placeholder</option>
              </select>
            </label>
            <button type="button" onClick={() => addElement(imageType)}>+ Image</button>
            <button type="button" onClick={addContentGroup}>+ Content Group</button>
            <button type="button" onClick={addSubReport}>+ Subreport</button>
            <button type="button" onClick={addConditionalFooter}>+ Conditional Footer</button>
          </aside>
          <div className="report-canvas-scroll">
            <div
              className={`report-page-canvas ${selectedSubReport ? 'subreport-preview-canvas' : ''}`}
              style={selectedSubReport
                ? { width: previewBodyWidth + 48, minHeight: 280, padding: 24, zoom: `${previewZoom}%` }
                : { width: pageWidth, minHeight: pageHeight, padding: `${pageMargins.top}px ${pageMargins.right}px ${pageMargins.bottom}px ${pageMargins.left}px`, zoom: `${previewZoom}%` }}
            >
              {selectedSubReport ? <div className="subreport-preview-title">{selectedContainerType}</div> : null}
              {visibleBands.map((band) => {
                const elements = collectDplxRenderElements(band)
                const contentHeight = elements.reduce((max, { element, yOffset }) => Math.max(max, yOffset + Number(element.getAttribute('y') ?? element.getAttribute('y2') ?? 0) + Number(element.getAttribute('height') ?? 24)), 0)
                const configuredBandHeight = Number(band.getAttribute('height') ?? 0)
                const bandHeight = selectedSubReport
                  ? configuredBandHeight
                  : band.tagName === 'template'
                  ? Math.max(Number(band.getAttribute('height') ?? 0), contentHeight, reportBodyHeight)
                  : configuredBandHeight > 0 ? configuredBandHeight : Math.max(contentHeight, 80)
                return (
                  <section key={band.tagName} className={`report-band ${selectedSubReport ? 'subreport-band' : ''} ${band.tagName === 'template' ? 'report-template-area' : ''} ${selectedSubReport ? selectedSubReportBand === band.tagName ? 'selected-report-band' : '' : selectedBand === band.tagName ? 'selected-report-band' : ''}`} style={{ width: previewBodyWidth, height: bandHeight }} onClick={() => {
                    if (selectedSubReport) {
                      setSelectedSubReportBand(selectedFlatContainer ? null : band.tagName)
                      setSelectedSubReportElement(null)
                    } else {
                      setSelectedBand(band.tagName)
                      setSelectedElement(null)
                      setSelectedSubReportBand(null)
                      setSelectedSubReportElement(null)
                    }
                  }}>
                    {band.tagName !== 'contentGroup' ? <span className="report-band-label">{band.tagName}</span> : null}
                    {elements.map(({ element, index, xOffset, yOffset }, renderIndex) => {
                      const isLine = element.tagName === 'line'
                      const x1 = Number(element.getAttribute('x1') ?? 0)
                      const y1 = Number(element.getAttribute('y1') ?? 0)
                      const x2 = Number(element.getAttribute('x2') ?? x1)
                      const y2 = Number(element.getAttribute('y2') ?? y1)
                      const verticalLine = isLine && Math.abs(y2 - y1) > Math.abs(x2 - x1)
                      const x = xOffset + (isLine ? Math.min(x1, x2) : Number(element.getAttribute('x') ?? 0))
                      const y = yOffset + (isLine ? Math.min(y1, y2) : Number(element.getAttribute('y') ?? 0))
                      const width = isLine ? Math.max(1, Math.abs(x2 - x1)) : Number(element.getAttribute('width') ?? 120)
                      const height = isLine ? Math.max(1, Math.abs(y2 - y1)) : Number(element.getAttribute('height') ?? 20)
                      const text = dplxElementText(element)
                      const selected = selectedSubReport
                        ? selectedSubReportElement?.band === band.tagName && selectedSubReportElement.index === index
                        : selectedElement?.band === band.tagName && selectedElement.index === index
                      const fontSize = Number(element.getAttribute('fontSize') || 10)
                      const align = element.getAttribute('align') as 'left' | 'center' | 'right' | null
                      const font = element.getAttribute('font') || 'Helvetica'
                      return <button key={`${element.tagName}-${index}-${renderIndex}`} type="button" className={`report-layout-element report-${element.tagName.toLowerCase()} ${verticalLine ? 'report-line-vertical' : ''} ${selected ? 'selected-report-element' : ''}`} style={{ left: x, top: y, width, height, fontSize, fontFamily: font, textAlign: align ?? 'left', fontWeight: /bold/i.test(font) ? 700 : undefined, backgroundColor: element.getAttribute('fillColor') || undefined, borderColor: element.getAttribute('borderColor') || undefined }} onClick={(event) => {
                        event.stopPropagation()
                        if (selectedSubReport) {
                          setSelectedSubReportBand(selectedFlatContainer ? null : band.tagName)
                          setSelectedSubReportElement({ band: band.tagName, index })
                        } else {
                          setSelectedBand(band.tagName)
                          setSelectedElement({ band: band.tagName, index })
                          setSelectedSubReportBand(null)
                          setSelectedSubReportElement(null)
                        }
                      }}>{text}</button>
                    })}
                  </section>
                )
              })}
            </div>
          </div>
          <aside className="report-properties">
            <strong>Properties</strong>
            {activeSubReportElement ? (
              <>
                <span className="report-element-type">{selectedContainerType} {activeSubReportElement.tagName}</span>
                {Array.from(activeSubReportElement.attributes).filter((attribute) => attribute.name !== 'expandable' && (!dplxTypographyElementNames.has(activeSubReportElement.tagName) || !dplxTypographyAttributes.has(attribute.name))).map((attribute) => (
                  <label key={attribute.name}>
                    <span>{attribute.name}</span>
                    <DplxAttributeInput name={attribute.name} value={attribute.value} onChange={(value) => updateSubReportElementAttribute(attribute.name, value)} />
                  </label>
                ))}
                {['recordBox', 'recordArea', 'formattedRecordArea'].includes(activeSubReportElement.tagName) ? (
                  <label><span>expandable</span><select value={activeSubReportElement.getAttribute('expandable') ?? 'false'} onChange={(event) => updateSubReportElementAttribute('expandable', event.target.value)}><option value="false">false</option><option value="true">true</option></select></label>
                ) : null}
                {dplxTypographyElementNames.has(activeSubReportElement.tagName) ? (
                  <>
                    <label><span>align</span><select value={activeSubReportElement.getAttribute('align') || 'left'} onChange={(event) => updateSubReportElementAttribute('align', event.target.value)}><option value="left">left</option><option value="center">center</option><option value="right">right</option></select></label>
                    <label><span>fontSize</span><input type="number" min="1" value={activeSubReportElement.getAttribute('fontSize') || '10'} onChange={(event) => updateSubReportElementAttribute('fontSize', event.target.value)} /></label>
                    <label><span>font</span><input value={activeSubReportElement.getAttribute('font') || 'Helvetica'} onChange={(event) => updateSubReportElementAttribute('font', event.target.value)} /></label>
                  </>
                ) : null}
                <label><span>New attribute</span><button type="button" onClick={() => updateSubReportElementAttribute('fontSize', '10')}>Add fontSize</button></label>
                <button type="button" className="danger-button" onClick={deleteSubReportElement}>Delete control</button>
              </>
            ) : activeSubReportBand ? (
              <>
                <span className="report-element-type">{selectedContainerType} Detail</span>
                {Array.from(activeSubReportBand.attributes).filter((attribute) => attribute.name !== 'autoSplit').map((attribute) => (
                  <label key={attribute.name}>
                    <span>{attribute.name}</span>
                    <DplxAttributeInput name={attribute.name} value={attribute.value} onChange={(value) => updateSubReportBandAttribute(attribute.name, value)} />
                  </label>
                ))}
                {activeSubReportBand.tagName === 'detail' && selectedSubReport?.tagName === 'subReport' ? (
                  <label><span>autoSplit</span><select value={activeSubReportBand.getAttribute('autoSplit') ?? 'false'} onChange={(event) => updateSubReportBandAttribute('autoSplit', event.target.value)}><option value="false">false</option><option value="true">true</option></select></label>
                ) : null}
                {!activeSubReportBand.hasAttribute('height') ? (
                  <label><span>Height</span><button type="button" onClick={() => updateSubReportBandAttribute('height', '40')}>Add height</button></label>
                ) : null}
                {activeSubReportBand.attributes.length === 0 ? <p>This {selectedContainerType} detail has no XML attributes yet.</p> : null}
              </>
            ) : activeElement ? (
              <>
                <span className="report-element-type">{activeElement.tagName}</span>
                {Array.from(activeElement.attributes).filter((attribute) => attribute.name !== 'expandable' && (!dplxTypographyElementNames.has(activeElement.tagName) || !dplxTypographyAttributes.has(attribute.name))).map((attribute) => (
                  <label key={attribute.name}><span>{attribute.name}</span><DplxAttributeInput name={attribute.name} value={attribute.value} onChange={(value) => updateElementAttribute(attribute.name, value)} /></label>
                ))}
                {['recordBox', 'recordArea', 'formattedRecordArea'].includes(activeElement.tagName) ? (
                  <label><span>expandable</span><select value={activeElement.getAttribute('expandable') ?? 'false'} onChange={(event) => updateElementAttribute('expandable', event.target.value)}><option value="false">false</option><option value="true">true</option></select></label>
                ) : null}
                {dplxTypographyElementNames.has(activeElement.tagName) ? (
                  <>
                    <label><span>align</span><select value={activeElement.getAttribute('align') || 'left'} onChange={(event) => updateElementAttribute('align', event.target.value)}><option value="left">left</option><option value="center">center</option><option value="right">right</option></select></label>
                    <label><span>fontSize</span><input type="number" min="1" value={activeElement.getAttribute('fontSize') || '10'} onChange={(event) => updateElementAttribute('fontSize', event.target.value)} /></label>
                    <label><span>font</span><input value={activeElement.getAttribute('font') || 'Helvetica'} onChange={(event) => updateElementAttribute('font', event.target.value)} /></label>
                  </>
                ) : null}
                <label><span>New attribute</span><button type="button" onClick={() => updateElementAttribute('fontSize', '10')}>Add fontSize</button></label>
                <button type="button" className="danger-button" onClick={deleteElement}>Delete control</button>
                {activeElement.tagName === 'subReport' ? queryProperties(activeElement, 'subreport') : null}
              </>
            ) : selectedBand === 'page' ? (
              <>
                <span className="report-element-type">Report Page</span>
                <label>
                  <span>Paper size</span>
                  <select value={pageSizes[reportPageSize] ? reportPageSize : 'custom'} onChange={(event) => updatePageSize(event.target.value)}>
                    <option value="letter">Letter</option>
                    <option value="legal">Legal</option>
                    <option value="a4">A4</option>
                    <option value="a3">A3</option>
                    <option value="a5">A5</option>
                    <option value="custom">Custom</option>
                  </select>
                </label>
                <label>
                  <span>Orientation</span>
                  <select value={orientation === 'landscape' ? 'landscape' : 'portrait'} onChange={(event) => updatePageOrientation(event.target.value)}>
                    <option value="portrait">Portrait</option>
                    <option value="landscape">Landscape</option>
                  </select>
                </label>
                <label><span>Page width</span><input type="number" min="1" value={baseWidth} onChange={(event) => updateCustomPageDimension('pageWidth', event.target.value)} /></label>
                <label><span>Page height</span><input type="number" min="1" value={baseHeight} onChange={(event) => updateCustomPageDimension('pageHeight', event.target.value)} /></label>
                <label><span>Top margin</span><input type="number" value={pageMargins.top} onChange={(event) => updateReportAttribute('topMargin', event.target.value)} /></label>
                <label><span>Right margin</span><input type="number" value={pageMargins.right} onChange={(event) => updateReportAttribute('rightMargin', event.target.value)} /></label>
                <label><span>Bottom margin</span><input type="number" value={pageMargins.bottom} onChange={(event) => updateReportAttribute('bottomMargin', event.target.value)} /></label>
                <label><span>Left margin</span><input type="number" value={pageMargins.left} onChange={(event) => updateReportAttribute('leftMargin', event.target.value)} /></label>
                {queryProperties(parsed.report, 'report')}
              </>
            ) : activeBand ? (
              <>
                <span className="report-element-type">{activeBand.tagName} Band</span>
                {Array.from(activeBand.attributes).filter((attribute) => attribute.name !== 'autoSplit').map((attribute) => (
                  <label key={attribute.name}>
                    <span>{attribute.name}</span>
                    <DplxAttributeInput name={attribute.name} value={attribute.value} onChange={(value) => updateBandAttribute(attribute.name, value)} />
                  </label>
                ))}
                {activeBand.tagName === 'detail' ? (
                  <label><span>autoSplit</span><select value={activeBand.getAttribute('autoSplit') ?? 'false'} onChange={(event) => updateBandAttribute('autoSplit', event.target.value)}><option value="false">false</option><option value="true">true</option></select></label>
                ) : null}
                {!activeBand.hasAttribute('height') ? (
                  <label><span>Height</span><button type="button" onClick={() => updateBandAttribute('height', '80')}>Add height</button></label>
                ) : null}
                {activeBand.attributes.length === 0 ? <p>This band has no XML attributes yet.</p> : null}
              </>
            ) : <p>Select Page, a band, or a control to edit its properties.</p>}
          </aside>
        </div>
      )}
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
  const [databaseEngine, setDatabaseEngine] = useState<'mssql' | 'postgresql'>('mssql')
  const [testResult, setTestResult] = useState<string>('')
  const [saveError, setSaveError] = useState<string>('')
  const [isSaving, setIsSaving] = useState(false)

  function readServer(form: FormData): OphServer {
    return {
      id: serverId,
      name: String(form.get('name') || 'Production OPH'),
      host: String(form.get('host') || 'localhost'),
      port: Number(form.get('port') || 1433),
      databaseEngine,
      authType,
      defaultDatabase: String(form.get('defaultDatabase') || 'oph_core'),
      username: databaseEngine === 'postgresql' || authType === 'sql' ? String(form.get('username') || '') : undefined,
      password: databaseEngine === 'postgresql' || authType === 'sql' ? String(form.get('password') || '') : undefined,
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
            Database Engine
            <select value={databaseEngine} onChange={(event) => {
              const engine = event.target.value as 'mssql' | 'postgresql'
              setDatabaseEngine(engine)
              if (engine === 'postgresql') setAuthType('sql')
            }}>
              <option value="mssql">Microsoft SQL Server</option>
              <option value="postgresql">PostgreSQL</option>
            </select>
          </label>
          <label>
            {databaseEngine === 'postgresql' ? 'PostgreSQL Host' : 'SQL Server Host'}
            <input name="host" defaultValue="10.10.1.20" />
          </label>
          <label>
            Port
            <input key={databaseEngine} name="port" type="number" defaultValue={databaseEngine === 'postgresql' ? 5432 : 1433} />
          </label>
          <label>
            Default Database
            <input name="defaultDatabase" defaultValue="oph_core" />
          </label>
          {databaseEngine === 'mssql' ? <label>
            Authentication
            <select value={authType} onChange={(event) => setAuthType(event.target.value as 'sql' | 'windows')}>
              <option value="sql">SQL Login</option>
              <option value="windows">Windows Authentication</option>
            </select>
          </label> : null}
          {databaseEngine === 'postgresql' || authType === 'sql' ? (
            <>
              <label>
                Username
                <input name="username" defaultValue={databaseEngine === 'postgresql' ? 'postgres' : 'oph_admin'} />
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
  pinnedIds,
  onPin,
  onRefresh,
  onSelect,
}: {
  root: OphTreeNode
  selectionId: string
  pinnedIds: Set<string>
  onPin: (selection: WorkspaceSelection) => void
  onRefresh: (node: OphTreeNode) => Promise<void>
  onSelect: (selection: WorkspaceSelection) => void
}) {
  const treeSearchHistoryKey = 'oph-control-studio.tree-search-history'
  const treeViewRef = useRef<HTMLDivElement>(null)
  const [treeSearch, setTreeSearch] = useState('')
  const [treeSearchHistory, setTreeSearchHistory] = useState<string[]>(() => {
    try {
      return JSON.parse(window.localStorage.getItem(treeSearchHistoryKey) ?? '[]')
    } catch {
      return []
    }
  })
  const normalizedTreeSearch = treeSearch.trim().toLocaleLowerCase()
  const treeSearchTerms = useMemo(() => normalizedTreeSearch.split(/\s+/).filter(Boolean), [normalizedTreeSearch])
  const treeSearchResults = useMemo(() => {
    if (treeSearchTerms.length === 0) return []
    const results: Array<{ node: OphTreeNode; path: string[] }> = []
    function collect(node: OphTreeNode, parentPath: string[]) {
      const path = node.kind === 'root' || node.kind === 'server' ? parentPath : [...parentPath, node.label]
      const searchable = `${path.join(' ')} ${node.description ?? ''} ${node.accountId ?? ''} ${node.databaseName ?? ''} ${node.searchText ?? ''}`.toLocaleLowerCase()
      if (node.kind !== 'root' && node.kind !== 'server' && treeSearchTerms.every((term) => searchable.includes(term))) {
        results.push({ node, path })
      }
      node.children?.forEach((child) => collect(child, path))
    }
    collect(root, [])
    return results.slice(0, 100)
  }, [root, treeSearchTerms])

  function rememberTreeSearch(value = treeSearch) {
    const normalized = value.trim()
    if (!normalized) return
    setTreeSearchHistory((current) => {
      const next = [normalized, ...current.filter((item) => item.toLocaleLowerCase() !== normalized.toLocaleLowerCase())].slice(0, 10)
      window.localStorage.setItem(treeSearchHistoryKey, JSON.stringify(next))
      return next
    })
  }

  function clearTreeSearchHistory() {
    setTreeSearchHistory([])
    window.localStorage.removeItem(treeSearchHistoryKey)
  }

  useEffect(() => {
    const frameId = window.requestAnimationFrame(() => {
      const selectedNode = Array.from(
        treeViewRef.current?.querySelectorAll<HTMLElement>('[data-tree-node-id]') ?? [],
      ).find((element) => element.dataset.treeNodeId === selectionId)
      selectedNode?.scrollIntoView({ block: 'nearest', inline: 'nearest' })
    })

    return () => window.cancelAnimationFrame(frameId)
  }, [selectionId])

  return (
    <div className="tree-view-shell">
      <label className="tree-search">
        <Search size={15} />
        <input
          type="search"
          value={treeSearch}
          placeholder="Search tree..."
          aria-label="Search tree"
          onChange={(event) => setTreeSearch(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') rememberTreeSearch()
          }}
        />
        {treeSearch ? <button type="button" aria-label="Clear tree search" onClick={() => setTreeSearch('')}><X size={13} /></button> : null}
      </label>
      {!treeSearch && treeSearchHistory.length > 0 ? (
        <div className="tree-search-history">
          <div><span>Recent searches</span><button type="button" onClick={clearTreeSearchHistory}>Clear</button></div>
          <div>
            {treeSearchHistory.map((item) => (
              <button key={item} type="button" title={`Search for ${item}`} onClick={() => setTreeSearch(item)}>{item}</button>
            ))}
          </div>
        </div>
      ) : null}
      <div ref={treeViewRef} className="tree-view">
        {normalizedTreeSearch ? (
          treeSearchResults.length > 0 ? (
            <div className="tree-search-results">
              <span className="tree-search-result-count">{treeSearchResults.length} result(s)</span>
              {treeSearchResults.map(({ node, path }) => {
                const ResultIcon = treeIcons[node.kind]
                return (
                  <button type="button" key={node.id} className="tree-search-result" onClick={() => {
                    rememberTreeSearch()
                    onSelect(workspaceSelectionFromNode(node))
                  }}>
                    <ResultIcon size={16} />
                    <span>
                      <strong>{node.label}</strong>
                      <small>{path.join(' : ')}</small>
                      {node.description ? <small className="tree-search-result-description">{node.description}</small> : null}
                    </span>
                  </button>
                )
              })}
            </div>
          ) : <div className="tree-search-empty">No matching nodes.</div>
        ) : (
          <TreeNodeView node={root} depth={0} selectionId={selectionId} pinnedIds={pinnedIds} onPin={onPin} onRefresh={onRefresh} onSelect={onSelect} />
        )}
      </div>
    </div>
  )
}

function workspaceSelectionFromNode(node: OphTreeNode): WorkspaceSelection {
  return {
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
    moduleGroupGuid: node.moduleGroupGuid,
    userGuid: node.userGuid,
    userGroupGuid: node.userGroupGuid,
    settingMode: node.settingMode,
  }
}

function TreeNodeView({
  node,
  depth,
  expandAll = false,
  selectionId,
  pinnedIds,
  onPin,
  onRefresh,
  onSelect,
}: {
  node: OphTreeNode
  depth: number
  expandAll?: boolean
  selectionId: string
  pinnedIds: Set<string>
  onPin: (selection: WorkspaceSelection) => void
  onRefresh: (node: OphTreeNode) => Promise<void>
  onSelect: (selection: WorkspaceSelection) => void
}) {
  const [expanded, setExpanded] = useState(depth < 2)
  const [manuallyCollapsed, setManuallyCollapsed] = useState(false)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const hasChildren = Boolean(node.children?.length)
  const containsSelection = Boolean(findTreeNode(node, selectionId))
  const isExpanded = expandAll || (!manuallyCollapsed && (expanded || (containsSelection && node.id !== selectionId)))
  const Icon = treeIcons[node.kind]

  useEffect(() => {
    setManuallyCollapsed(false)
  }, [selectionId])

  function nodeSelection(): WorkspaceSelection {
    return workspaceSelectionFromNode(node)
  }

  function selectNode() {
    if (hasChildren) {
      setExpanded(true)
      setManuallyCollapsed(false)
    }
    onSelect(nodeSelection())
  }

  return (
    <div>
      <button
        data-tree-node-id={node.id}
        className={`tree-node ${selectionId === node.id ? 'tree-node-active' : ''}`}
        style={{ paddingLeft: 10 + depth * 16 }}
        onClick={selectNode}
      >
        <span className="tree-expander" onClick={(event) => {
          event.stopPropagation()
          setExpanded(!isExpanded)
          setManuallyCollapsed(isExpanded)
        }}>
          {hasChildren ? isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} /> : null}
        </span>
        <Icon size={16} />
        <span>
          <strong>{node.label}</strong>
          {node.description ? <small>{node.description}</small> : null}
        </span>
        {node.kind === 'server'
        || node.kind === 'module'
        || node.kind === 'module-column'
        || (node.kind === 'module-action' && node.label === 'Columns')
        || (node.kind === 'module-category' && node.settingMode !== undefined)
        || (node.kind === 'module-category' && node.label === 'Module Groups')
        || (node.kind === 'interface' && node.label === 'Menus')
        || node.kind === 'menu' ? (
          <span
            role="button"
            tabIndex={0}
            className="tree-refresh-button"
            aria-label={`Refresh ${node.label}`}
            title={node.kind === 'server'
              ? 'Refresh all accounts on this server'
              : node.kind === 'interface' || node.kind === 'menu'
                ? 'Refresh menus tree'
              : node.kind === 'module-category'
                ? `Refresh ${node.label.toLowerCase()} tree`
              : `Refresh ${node.kind === 'module' ? 'module and column' : 'column'} tree`}
            onClick={(event) => {
              event.stopPropagation()
              if (isRefreshing) return
              setIsRefreshing(true)
              void onRefresh(node).finally(() => setIsRefreshing(false))
            }}
            onKeyDown={(event) => {
              if (event.key !== 'Enter' && event.key !== ' ') return
              event.preventDefault()
              event.stopPropagation()
              if (isRefreshing) return
              setIsRefreshing(true)
              void onRefresh(node).finally(() => setIsRefreshing(false))
            }}
          >
            <RefreshCw size={14} className={isRefreshing ? 'tree-refresh-spinning' : ''} />
          </span>
        ) : <span />}
        <span
          role="button"
          tabIndex={0}
          className={`tree-pin-button ${pinnedIds.has(node.id) ? 'tree-pin-active' : ''}`}
          aria-label={`${pinnedIds.has(node.id) ? 'Unpin' : 'Pin'} ${node.label}`}
          title={pinnedIds.has(node.id) ? 'Unpin from workspace' : 'Pin as workspace tab'}
          onClick={(event) => {
            event.stopPropagation()
            onPin(nodeSelection())
          }}
          onKeyDown={(event) => {
            if (event.key === 'Enter' || event.key === ' ') {
              event.preventDefault()
              event.stopPropagation()
              onPin(nodeSelection())
            }
          }}
        >
          <Pin size={14} fill={pinnedIds.has(node.id) ? 'currentColor' : 'none'} />
        </span>
      </button>
      {isExpanded && hasChildren ? (
        <div>
          {node.children?.map((child) => (
            <TreeNodeView
              key={child.id}
              node={child}
              depth={depth + 1}
              expandAll={expandAll}
              selectionId={selectionId}
              pinnedIds={pinnedIds}
              onPin={onPin}
              onRefresh={onRefresh}
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
  onRefreshTreeNode,
  onNavigate,
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
  onRefreshTreeNode: (node: OphTreeNode) => Promise<void>
  onNavigate: (selection: WorkspaceSelection) => void
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
        config={connectionConfig}
        selection={selection}
        tree={tree}
        onRefresh={() => selection.serverId ? onRefreshServer(selection.serverId) : undefined}
        onDelete={() => selection.serverId && selection.accountId
          ? onDeleteAccount(selection.serverId, selection.accountId)
          : undefined}
        onNavigate={onNavigate}
      />
    )
  }

  if (selection.kind === 'module' && selection.moduleGuid) {
    const parentModuleList = findTreeParent(tree, selection.id)
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Module Info"
        sourceTable="modlinfo"
        onNavigate={onNavigate}
        parentSelection={parentModuleList ? workspaceSelectionFromNode(parentModuleList) : undefined}
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listModuleInfo(config, accountId, databaseName, selection.moduleGuid ?? '')
        }
        onRefreshTree={() => {
          const node = findTreeNode(tree, selection.id)
          return node ? onRefreshTreeNode(node) : Promise.resolve()
        }}
      />
    )
  }

  if (selection.kind === 'module-action' && selection.label === 'Columns' && selection.moduleGuid) {
    const parentModule = findTreeNode(tree, `${selection.databaseId}:module:${selection.moduleGuid}`)
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Columns"
        sourceTable="modlcolm"
        onNavigate={onNavigate}
        parentSelection={parentModule ? workspaceSelectionFromNode(parentModule) : undefined}
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listModuleColumns(config, accountId, databaseName, selection.moduleGuid ?? '')
        }
        onRefreshTree={() => {
          const node = findTreeNode(tree, selection.id)
          return node ? onRefreshTreeNode(node) : Promise.resolve()
        }}
      />
    )
  }

  if (selection.kind === 'module-column' && selection.columnGuid) {
    const parentColumns = findTreeNode(tree, `${selection.databaseId}:module:${selection.moduleGuid}:columns`)
    return (
      <MetadataWorkspace
        config={connectionConfig}
        selection={selection}
        title="Column Info"
        sourceTable="modlcolminfo"
        onNavigate={onNavigate}
        parentSelection={parentColumns ? workspaceSelectionFromNode(parentColumns) : undefined}
        loadRows={(config, accountId, databaseName) =>
          ophAdminService.listModuleColumnInfo(config, accountId, databaseName, selection.columnGuid ?? '')
        }
        onRefreshTree={() => {
          const node = findTreeNode(tree, selection.id)
          return node ? onRefreshTreeNode(node) : Promise.resolve()
        }}
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
    if (selection.moduleGroupGuid) {
      return (
        <MetadataWorkspace
          config={connectionConfig}
          selection={selection}
          title={`${selection.label} Info`}
          sourceTable="modginfo"
          loadRows={(config, _accountId, databaseName) =>
            ophAdminService.listModuleGroupInfo(config, databaseName, selection.moduleGroupGuid ?? '')
          }
        />
      )
    }

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
          onRefreshTree={() => {
            const node = findTreeNode(tree, selection.id)
            return node ? onRefreshTreeNode(node) : Promise.resolve()
          }}
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
          onRefreshTree={() => {
            const node = findTreeNode(tree, selection.id)
            return node ? onRefreshTreeNode(node) : Promise.resolve()
          }}
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
          onRefreshTree={() => {
            const node = findTreeNode(tree, selection.id)
            return node ? onRefreshTreeNode(node) : Promise.resolve()
          }}
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
        onRefreshTree={() => {
          const node = findTreeNode(tree, selection.id)
          return node ? onRefreshTreeNode(node) : Promise.resolve()
        }}
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
      <AccountDatabasesWorkspace
        config={connectionConfig}
        selection={selection}
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


function SectionHeader({ eyebrow, title, description, action, onAction, onTitleClick, parentLabel, onParentClick }: SectionHeaderProps) {
  return (
    <div className="section-header">
      <div className="section-header-title-area">
        {onParentClick ? (
          <button type="button" className="section-parent-link" onClick={onParentClick} title={`Back to ${parentLabel ?? 'parent'}`} aria-label={`Back to ${parentLabel ?? 'parent'}`}>
            <ArrowUp size={18} />
          </button>
        ) : null}
        <div>
          <span className="eyebrow">{eyebrow}</span>
          {onTitleClick ? (
            <button type="button" className="section-title-shortcut" onClick={onTitleClick} title={`Edit ${title}`}>
              <h1>{title}</h1>
              <span>Edit details <ArrowRight size={14} /></span>
            </button>
          ) : <h1>{title}</h1>}
          <p>{description}</p>
        </div>
      </div>
      {action ? <button className="primary-button" onClick={onAction}>{action}</button> : null}
    </div>
  )
}

function MetricCard({ label, value, detail, onClick }: MetricCardProps) {
  return (
    <button type="button" className="metric-card" onClick={onClick} disabled={!onClick}>
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{detail}</small>
      {onClick ? <span className="metric-card-shortcut">Open detail <ArrowRight size={14} /></span> : null}
    </button>
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
              <th>Engine / Auth</th>
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
                <td>{server.databaseEngine === 'postgresql' ? 'PostgreSQL / Password' : `SQL Server / ${server.authType === 'sql' ? 'SQL Login' : 'Windows Auth'}`}</td>
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
            <tr><th>Database Engine</th><td>{server.databaseEngine === 'postgresql' ? 'PostgreSQL' : 'Microsoft SQL Server'}</td></tr>
            <tr><th>Authentication</th><td>{server.databaseEngine === 'postgresql' ? 'Password' : server.authType === 'sql' ? 'SQL Login' : 'Windows Auth'}</td></tr>
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

function findFirstTreeNodeByKind(root: OphTreeNode, kind: TreeNodeKind): OphTreeNode | undefined {
  if (root.kind === kind) return root
  for (const child of root.children ?? []) {
    const result = findFirstTreeNodeByKind(child, kind)
    if (result) return result
  }
  return undefined
}

function findTreeParent(root: OphTreeNode, nodeId: string): OphTreeNode | undefined {
  if (root.children?.some((child) => child.id === nodeId)) return root

  for (const child of root.children ?? []) {
    const parent = findTreeParent(child, nodeId)
    if (parent) return parent
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

function formatFileSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  const unitIndex = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1)
  const value = bytes / (1024 ** unitIndex)
  return `${value >= 10 || unitIndex === 0 ? value.toFixed(0) : value.toFixed(1)} ${units[unitIndex]}`
}

const editorFieldTypeOptions = [
  ['11', 'Text box'],
  ['12', 'Password'],
  ['13', 'Rich text editor'],
  ['14', 'Text area'],
  ['15', 'URL'],
  ['19', 'Hidden box'],
  ['20', 'Label / non-field'],
  ['21', 'Button / non-field'],
  ['31', 'Checkbox'],
  ['32', 'Autosuggest'],
  ['33', 'Token box'],
  ['34', 'Radio'],
  ['41', 'Date'],
  ['42', 'Time'],
  ['43', 'Date and time'],
  ['44', 'Month and year'],
  ['45', 'Year'],
  ['51', 'Media attachment'],
  ['52', 'Profile image'],
  ['53', 'Media image'],
  ['54', 'Vector signature'],
  ['56', 'Get current location'],
  ['57', 'Set current location'],
] as const

function DatabaseWorkspace({
  config,
  selection,
  tree,
  onRefresh,
  onDelete,
  onNavigate,
}: {
  config: OphConnectionConfig
  selection: WorkspaceSelection
  tree: OphTreeNode
  onRefresh: () => void | Promise<void>
  onDelete: () => void | Promise<void>
  onNavigate: (selection: WorkspaceSelection) => void
}) {
  const [isConfirmingDelete, setIsConfirmingDelete] = useState(false)
  const [confirmation, setConfirmation] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [actionError, setActionError] = useState('')
  const [backups, setBackups] = useState<MetadataRow[]>([])
  const [isLoadingBackups, setIsLoadingBackups] = useState(true)
  const [backupError, setBackupError] = useState('')
  const [selectedBackup, setSelectedBackup] = useState<MetadataRow | null>(null)
  const [targetDatabaseName, setTargetDatabaseName] = useState('')
  const [isRestoring, setIsRestoring] = useState(false)
  const [restoreError, setRestoreError] = useState('')
  const [restoreNotice, setRestoreNotice] = useState('')
  const databaseNode = findTreeNode(tree, selection.id)
  const modulesNode = databaseNode?.children?.find((child) => child.label === 'Modules')
  const securityNode = databaseNode?.children?.find((child) => child.label === 'Security')
  const interfaceNode = databaseNode?.children?.find((child) => child.label === 'Interface')
  const accountNode = databaseNode?.children?.find((child) => child.label === 'Account')

  const moduleCount = countNodes(modulesNode, (node) => node.kind === 'module')
  const securityCount = countLeafChildren(securityNode)
  const interfaceCount = countLeafChildren(interfaceNode)
  const accountCount = countLeafChildren(accountNode)

  useEffect(() => {
    let cancelled = false
    if (!selection.accountId || !selection.databaseName) return
    setIsLoadingBackups(true)
    setBackupError('')
    ophAdminService.listDatabaseBackups(config, selection.accountId, selection.databaseName)
      .then((rows) => { if (!cancelled) setBackups(rows) })
      .catch((error) => { if (!cancelled) setBackupError(error instanceof Error ? error.message : String(error)) })
      .finally(() => { if (!cancelled) setIsLoadingBackups(false) })
    return () => { cancelled = true }
  }, [config, selection.accountId, selection.databaseName])

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

  function openRestore(backup: MetadataRow) {
    setSelectedBackup(backup)
    setTargetDatabaseName(`${selection.databaseName}_001`)
    setRestoreError('')
    setRestoreNotice('')
  }

  async function restoreBackup() {
    if (!selectedBackup) return
    const backupFile = String(selectedBackup.backupFile ?? '')
    const destination = targetDatabaseName.trim()
    setIsRestoring(true)
    setRestoreError('')
    setRestoreNotice('')
    try {
      await ophAdminService.restoreDatabaseBackup(config, backupFile, destination)
      await onRefresh()
      setRestoreNotice(`Database ${destination} restored successfully.`)
      appendAuditLog('restore-database', destination, 'success', `Restored from ${backupFile}.`)
    } catch (restoreFailure) {
      const detail = restoreFailure instanceof Error ? restoreFailure.message : String(restoreFailure)
      setRestoreError(detail)
      appendAuditLog('restore-database', destination, 'failed', detail)
    } finally {
      setIsRestoring(false)
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
        <MetricCard label="Modules" value={String(moduleCount)} detail="Grouped by setting mode" onClick={modulesNode ? () => onNavigate(workspaceSelectionFromNode(modulesNode)) : undefined} />
        <MetricCard label="Security" value={String(securityCount)} detail="Users and groups" onClick={securityNode ? () => onNavigate(workspaceSelectionFromNode(securityNode)) : undefined} />
        <MetricCard label="Interface" value={String(interfaceCount)} detail="Themes, menus, translator" onClick={interfaceNode ? () => onNavigate(workspaceSelectionFromNode(interfaceNode)) : undefined} />
        <MetricCard label="Account" value={String(accountCount)} detail="Parameters and mail" onClick={accountNode ? () => onNavigate(workspaceSelectionFromNode(accountNode)) : undefined} />
      </div>
      <div className="table-card">
        <div className="metadata-toolbar">
          <h2>S3 backups</h2>
          <span>{backups.length} backup files for {selection.databaseName}</span>
        </div>
        {isLoadingBackups ? <div className="empty-result">Loading S3 backups...</div> : null}
        {backupError ? <div className="connection-error">{backupError}</div> : null}
        {!isLoadingBackups && !backupError && backups.length === 0 ? <div className="empty-result">No S3 backups found for this database.</div> : null}
        {!isLoadingBackups && !backupError && backups.length > 0 ? (
          <table>
            <thead><tr><th>Backup File</th><th>Size</th><th>Last Modified</th><th>Storage</th><th>Action</th></tr></thead>
            <tbody>{backups.map((backup, index) => (
              <tr key={`${backup.backupFile}-${index}`}>
                <td><strong>{String(backup.backupFile ?? '')}</strong></td>
                <td>{formatFileSize(Number(backup.sizeBytes ?? 0))}</td>
                <td>{String(backup.lastModified ?? '-')}</td>
                <td>{String(backup.storageClass ?? '-')}</td>
                <td><button type="button" onClick={() => openRestore(backup)}>Restore</button></td>
              </tr>
            ))}</tbody>
          </table>
        ) : null}
      </div>
      {selectedBackup ? (
        <div className="row-detail-backdrop" onMouseDown={() => !isRestoring && setSelectedBackup(null)}>
          <aside className="row-detail-overlay" onMouseDown={(event) => event.stopPropagation()}>
            <div className="row-detail-header">
              <div><span className="eyebrow">Restore S3 Backup</span><h2>{selection.databaseName}</h2></div>
              <button className="overlay-close-button" type="button" disabled={isRestoring} onClick={() => setSelectedBackup(null)}>×</button>
            </div>
            <div className="row-detail-form">
              <label><span>Backup File</span><input value={String(selectedBackup.backupFile ?? '')} disabled /></label>
              <label>
                <span>New Database Name</span>
                <input autoFocus value={targetDatabaseName} disabled={isRestoring} onChange={(event) => setTargetDatabaseName(event.target.value)} placeholder="example_001" />
              </label>
              <p className="delete-warning">Restore creates a new database. An existing database will never be overwritten.</p>
              <button type="button" disabled={isRestoring || !targetDatabaseName.trim()} onClick={restoreBackup}>
                {isRestoring ? 'Restoring Database…' : 'Restore as New Database'}
              </button>
            </div>
            {restoreError ? <div className="connection-error">{restoreError}</div> : null}
            {restoreNotice ? <div className="connection-notice">{restoreNotice}</div> : null}
          </aside>
        </div>
      ) : null}
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
  onNavigate,
  parentSelection,
  onRefreshTree,
}: {
  config: OphConnectionConfig
  selection: WorkspaceSelection
  title: string
  sourceTable: string
  loadRows: (config: OphConnectionConfig, accountId: string, databaseName: string) => Promise<MetadataRow[]>
  onNavigate?: (selection: WorkspaceSelection) => void
  parentSelection?: WorkspaceSelection
  onRefreshTree?: () => void | Promise<void>
}) {
  const [rows, setRows] = useState<MetadataRow[]>([])
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isParentDetailOpen, setIsParentDetailOpen] = useState(false)
  const parentDetailSource = sourceTable.toLowerCase() === 'modlinfo' && selection.kind === 'module'
    ? 'modl'
    : sourceTable.toLowerCase() === 'modlcolminfo' && selection.kind === 'module-column'
      ? 'modlcolm'
      : sourceTable.toLowerCase() === 'userinfo' && selection.kind === 'security-user'
        ? '[user]'
        : selection.kind === 'security-group' && (sourceTable.toLowerCase() === 'ugrpinfo' || sourceTable.toLowerCase() === 'ugrpmodl')
          ? 'ugrp'
          : ''

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

  async function refreshWorkspace() {
    if (!selection.accountId || !selection.databaseName) return
    setIsLoading(true)
    setError('')
    try {
      const [loadedRows] = await Promise.all([
        loadRows(config, selection.accountId, selection.databaseName),
        onRefreshTree?.(),
      ])
      setRows(loadedRows)
    } catch (refreshError) {
      setError(refreshError instanceof Error ? refreshError.message : String(refreshError))
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow={title}
        title={selection.label}
        description={`Loaded from ${sourceTable} in ${selection.databaseName}.`}
        action="Refresh"
        onAction={() => { void refreshWorkspace() }}
        onTitleClick={parentDetailSource ? () => setIsParentDetailOpen(true) : undefined}
        parentLabel={parentSelection?.label}
        onParentClick={parentSelection && onNavigate ? () => onNavigate(parentSelection) : undefined}
      />
      {isLoading ? <div className="empty-result">Loading metadata...</div> : null}
      {error ? <div className="connection-error">{error}</div> : null}
      {!isLoading && !error ? (
        <MetadataTable config={config} rows={rows} selection={selection} sourceTable={sourceTable} onNavigate={onNavigate} />
      ) : null}
      {isParentDetailOpen && parentDetailSource ? (
        parentDetailSource === 'modl' ? (
          <ModuleDetailOverlay config={config} selection={selection} onNavigate={onNavigate} onClose={() => setIsParentDetailOpen(false)} />
        ) : (
          <ParentRecordOverlay config={config} selection={selection} sourceTable={parentDetailSource} onClose={() => setIsParentDetailOpen(false)} />
        )
      ) : null}
    </div>
  )
}

function AccountDatabasesWorkspace({
  config,
  selection,
}: {
  config: OphConnectionConfig
  selection: WorkspaceSelection
}) {
  const [databases, setDatabases] = useState<MetadataRow[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false
    if (!selection.accountId || !selection.databaseName) {
      setError('No account database is selected.')
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setError('')
    ophAdminService.listAccountDatabases(config, selection.accountId, selection.databaseName)
      .then((rows) => { if (!cancelled) setDatabases(rows) })
      .catch((loadError) => {
        if (!cancelled) setError(loadError instanceof Error ? loadError.message : String(loadError))
      })
      .finally(() => { if (!cancelled) setIsLoading(false) })

    return () => { cancelled = true }
  }, [config, selection.accountId, selection.databaseName])

  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Account"
        title="Physical Databases"
        description={`Physical databases registered for ${selection.accountId}.`}
      />
      {isLoading ? <div className="empty-result">Loading physical databases...</div> : null}
      {error ? <div className="connection-error">{error}</div> : null}
      {!isLoading && !error && databases.length === 0 ? (
        <div className="empty-result">No physical databases are registered for this account.</div>
      ) : null}
      {!isLoading && !error && databases.length > 0 ? (
        <div className="table-card">
          <table>
            <thead><tr><th>Database</th><th>Version</th><th>Primary</th></tr></thead>
            <tbody>{databases.map((database, index) => (
              <tr key={String(database.accountdbguid ?? `${database.databasename}-${index}`)}>
                <td><strong>{String(database.databasename ?? '')}</strong></td>
                <td>{String(database.version ?? '-')}</td>
                <td>{String(database.ismaster ?? '').toLowerCase() === 'true' || Number(database.ismaster) === 1 ? 'Yes' : 'No'}</td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      ) : null}
    </div>
  )
}
function ModuleDetailOverlay({
  config,
  selection,
  onNavigate,
  onClose,
}: {
  config: OphConnectionConfig
  selection: WorkspaceSelection
  onNavigate?: (selection: WorkspaceSelection) => void
  onClose: () => void
}) {
  const [originalRow, setOriginalRow] = useState<MetadataRow | null>(null)
  const [draft, setDraft] = useState<Record<string, string>>({})
  const [options, setOptions] = useState<Record<string, Array<{ value: string; label: string }>>>({})
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const fields = [
    'moduleid',
    'moduledescription',
    'settingmode',
    'parentmoduleguid',
    'accountdbguid',
    'orderno',
    'needlogin',
    'themepageguid',
    'modulestatusguid',
    'modulegroupguid',
  ]
  const labels: Record<string, string> = {
    moduleid: 'Module ID',
    moduledescription: 'Module Description',
    settingmode: 'Setting Mode',
    parentmoduleguid: 'Parent Module',
    accountdbguid: 'Account DB',
    orderno: 'Order No',
    needlogin: 'Need Login',
    themepageguid: 'Theme Page',
    modulestatusguid: 'Module Status',
    modulegroupguid: 'Module Group',
  }

  function rowValue(row: MetadataRow, field: string) {
    const key = Object.keys(row).find((candidate) => candidate.toLowerCase() === field)
    return key ? String(row[key] ?? '') : ''
  }

  useEffect(() => {
    let cancelled = false
    if (!selection.accountId || !selection.databaseName || !selection.moduleGuid || selection.settingMode === undefined) {
      setError('Module context is incomplete.')
      setIsLoading(false)
      return
    }

    Promise.all([
      ophAdminService.listModulesBySettingMode(config, selection.accountId, selection.databaseName, Number(selection.settingMode)),
      ophAdminService.listModuleStatuses(config, selection.accountId, selection.databaseName),
      ophAdminService.listModuleGroups(config, selection.accountId, selection.databaseName),
      ophAdminService.listAccountDatabases(config, selection.accountId, selection.databaseName),
      ophAdminService.listModuleThemePages(config, selection.accountId, selection.databaseName),
      ophAdminService.listModuleTree(config, selection.accountId, selection.databaseName),
    ]).then(([modules, statuses, groups, databases, themePages, allModules]) => {
      if (cancelled) return
      const moduleRow = modules.find((row) => rowValue(row, 'moduleguid') === selection.moduleGuid)
      if (!moduleRow) throw new Error(`Module ${selection.label} was not found.`)
      setOriginalRow(moduleRow)
      setDraft(Object.fromEntries(fields.map((field) => [field, rowValue(moduleRow, field)])))
      setOptions({
        parentmoduleguid: allModules
          .filter((row) => rowValue(row, 'moduleguid') !== selection.moduleGuid)
          .map((row) => ({
            value: rowValue(row, 'moduleguid'),
            label: [rowValue(row, 'moduleid'), rowValue(row, 'moduledescription')].filter(Boolean).join(' — '),
          })),
        accountdbguid: databases.map((row) => ({ value: rowValue(row, 'accountdbguid'), label: rowValue(row, 'databasename') || rowValue(row, 'accountdbguid') })),
        themepageguid: themePages.map((row) => ({ value: rowValue(row, 'themepageguid'), label: [rowValue(row, 'themecode'), rowValue(row, 'pageurl')].filter(Boolean).join(' — ') })),
        modulestatusguid: statuses.map((row) => ({ value: rowValue(row, 'modulestatusguid'), label: rowValue(row, 'modulestatusname') || rowValue(row, 'modulestatusguid') })),
        modulegroupguid: groups.map((row) => ({ value: rowValue(row, 'modulegroupguid'), label: rowValue(row, 'modulegroupid') || rowValue(row, 'modulegroupname') })),
      })
    }).catch((loadError) => {
      if (!cancelled) setError(loadError instanceof Error ? loadError.message : String(loadError))
    }).finally(() => {
      if (!cancelled) setIsLoading(false)
    })

    return () => { cancelled = true }
  }, [config, selection.accountId, selection.databaseName, selection.moduleGuid, selection.settingMode])

  async function saveModule() {
    if (!originalRow || !selection.databaseName) return
    setIsSaving(true)
    setError('')
    setNotice('')
    const nextRow = { ...originalRow, ...draft }
    try {
      await ophAdminService.saveMetadataRow(
        config,
        selection.databaseName,
        'modl',
        originalRow,
        nextRow,
        selection.moduleGuid,
        undefined,
        undefined,
        undefined,
        undefined,
        selection.accountId,
      )
      setOriginalRow(nextRow)
      setNotice(`Module ${draft.moduleid || selection.label} saved.`)
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : String(saveError))
    } finally {
      setIsSaving(false)
    }
  }

  function openModuleArea(label: 'Info' | 'Columns' | 'Children' | 'Approvals' | 'Numbering' | 'Mails') {
    if (!selection.moduleGuid || !onNavigate) return
    const moduleBaseId = `${selection.databaseId}:module:${selection.moduleGuid}`
    onClose()
    onNavigate(label === 'Info' ? {
      ...selection,
      id: moduleBaseId,
      kind: 'module',
      label: draft.moduleid || selection.label,
    } : {
      ...selection,
      id: `${moduleBaseId}:${label.toLowerCase()}`,
      kind: 'module-action',
      label,
    })
  }

  return (
    <div className="row-detail-backdrop" onMouseDown={() => !isSaving && onClose()}>
      <aside className="row-detail-overlay" onMouseDown={(event) => event.stopPropagation()}>
        <div className="row-detail-header">
          <div><span className="eyebrow">Module</span><h2>{selection.label}</h2></div>
          <button className="overlay-close-button" type="button" disabled={isSaving} onClick={onClose}>×</button>
        </div>
        {onNavigate ? (
          <nav className="row-detail-links" aria-label="Open module metadata">
            {(['Info', 'Columns', 'Children', 'Approvals', 'Numbering', 'Mails'] as const).map((label) => (
              <a key={label} href="#" aria-disabled={isSaving} onClick={(event) => {
                event.preventDefault()
                if (!isSaving) openModuleArea(label)
              }}>Open {label}</a>
            ))}
          </nav>
        ) : null}
        {isLoading ? <div className="empty-result">Loading module information...</div> : null}
        {!isLoading && originalRow ? (
          <form className="row-detail-form" onSubmit={(event) => { event.preventDefault(); void saveModule() }}>
            {fields.map((field) => (
              <label key={field}>
                <span>{labels[field]}</span>
                {field === 'needlogin' ? (
                  <CheckboxInput value={draft[field] ?? ''} readOnly={isSaving} onChange={(value) => setDraft((current) => ({ ...current, [field]: value }))} />
                ) : field === 'settingmode' ? (
                  <select value={draft[field] ?? ''} disabled={isSaving} onChange={(event) => setDraft((current) => ({ ...current, [field]: event.target.value }))}>
                    {moduleSettingModeOptions.map((option) => <option key={option.value} value={option.value}>{option.value} — {option.label}</option>)}
                  </select>
                ) : options[field] ? (
                  <select value={draft[field] ?? ''} disabled={isSaving} onChange={(event) => setDraft((current) => ({ ...current, [field]: event.target.value }))}>
                    <option value="">None</option>
                    {options[field].filter((option) => option.value).map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                  </select>
                ) : (
                  <input value={draft[field] ?? ''} disabled={isSaving} onChange={(event) => setDraft((current) => ({ ...current, [field]: event.target.value }))} />
                )}
              </label>
            ))}
            <div className="row-detail-actions">
              <button type="submit" disabled={isSaving}>{isSaving ? 'Saving…' : 'Save'}</button>
              <button type="button" disabled={isSaving} onClick={onClose}>Cancel</button>
            </div>
          </form>
        ) : null}
        {error ? <div className="connection-error">{error}</div> : null}
        {notice ? <div className="action-notice">{notice}</div> : null}
      </aside>
    </div>
  )
}

function ParentRecordOverlay({
  config,
  selection,
  sourceTable,
  onClose,
}: {
  config: OphConnectionConfig
  selection: WorkspaceSelection
  sourceTable: string
  onClose: () => void
}) {
  const [row, setRow] = useState<MetadataRow | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false
    if (!selection.accountId || !selection.databaseName) {
      setError('Record context is incomplete.')
      return
    }

    const request = sourceTable === 'modlcolm' && selection.moduleGuid
      ? ophAdminService.listModuleColumns(config, selection.accountId, selection.databaseName, selection.moduleGuid)
      : sourceTable === '[user]'
        ? ophAdminService.listUsers(config, selection.accountId, selection.databaseName)
        : sourceTable === 'ugrp'
          ? ophAdminService.listUserGroups(config, selection.accountId, selection.databaseName)
          : Promise.resolve([])

    request.then((rows) => {
      if (cancelled) return
      const keyField = sourceTable === 'modlcolm' ? 'columnguid' : sourceTable === '[user]' ? 'userguid' : 'ugroupguid'
      const targetGuid = sourceTable === 'modlcolm' ? selection.columnGuid : sourceTable === '[user]' ? selection.userGuid : selection.userGroupGuid
      const selected = rows.find((candidate) => {
        const actualKey = Object.keys(candidate).find((key) => key.toLowerCase() === keyField)
        return actualKey && String(candidate[actualKey] ?? '') === targetGuid
      })
      if (!selected) throw new Error(`${selection.label} was not found in ${sourceTable}.`)
      setRow(selected)
    }).catch((loadError) => {
      if (!cancelled) setError(loadError instanceof Error ? loadError.message : String(loadError))
    })

    return () => { cancelled = true }
  }, [config, selection.accountId, selection.columnGuid, selection.databaseName, selection.label, selection.moduleGuid, selection.userGroupGuid, selection.userGuid, sourceTable])

  if (row) {
    return (
      <MetadataTable
        config={config}
        rows={[row]}
        selection={selection}
        sourceTable={sourceTable}
        autoOpenFirstRow
        overlayOnly
        onOverlayClose={onClose}
      />
    )
  }

  return (
    <div className="row-detail-backdrop" onMouseDown={onClose}>
      <aside className="row-detail-overlay" onMouseDown={(event) => event.stopPropagation()}>
        <div className="row-detail-header">
          <div><span className="eyebrow">Loading details</span><h2>{selection.label}</h2></div>
          <button className="overlay-close-button" type="button" onClick={onClose}>×</button>
        </div>
        {error ? <div className="connection-error">{error}</div> : <div className="empty-result">Loading information...</div>}
      </aside>
    </div>
  )
}

function MetadataTable({
  config,
  rows,
  selection,
  sourceTable,
  onNavigate,
  autoOpenFirstRow = false,
  overlayOnly = false,
  onOverlayClose,
}: {
  config: OphConnectionConfig
  rows: MetadataRow[]
  selection: WorkspaceSelection
  sourceTable: string
  onNavigate?: (selection: WorkspaceSelection) => void
  autoOpenFirstRow?: boolean
  overlayOnly?: boolean
  onOverlayClose?: () => void
}) {
  const [tableRows, setTableRows] = useState<MetadataRow[]>(rows)
  const [selectedRow, setSelectedRow] = useState<MetadataRow | null>(null)
  const [selectedRowIndex, setSelectedRowIndex] = useState<number | null>(null)
  const [checkedRowIndexes, setCheckedRowIndexes] = useState<Set<number>>(new Set())
  const [isCopyTargetOpen, setIsCopyTargetOpen] = useState(false)
  const [copyTargets, setCopyTargets] = useState<MetadataRow[]>([])
  const [copyTargetGuid, setCopyTargetGuid] = useState('')
  const [copyTargetSearch, setCopyTargetSearch] = useState('')
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
  const [isBulkDeleteOpen, setIsBulkDeleteOpen] = useState(false)
  const [isBulkDeleting, setIsBulkDeleting] = useState(false)
  const [listSearch, setListSearch] = useState('')
  const [columnListMode, setColumnListMode] = useState<'table' | 'visual'>('table')
  const [activeColumnPage, setActiveColumnPage] = useState('')
  const [userTokenOptions, setUserTokenOptions] = useState<Array<{ value: string; label: string }>>([])
  const [moduleGroupTokenOptions, setModuleGroupTokenOptions] = useState<Array<{ value: string; label: string }>>([])
  const [moduleRelationOptions, setModuleRelationOptions] = useState<Record<string, Array<{ value: string; label: string }>>>({})
  const [columnTypeOptions, setColumnTypeOptions] = useState<Array<{ value: string; label: string }>>([])
  const [mailActionOptions, setMailActionOptions] = useState<Array<{ value: string; label: string }>>([])
  const [mailStatusOptions, setMailStatusOptions] = useState<Array<{ value: string; label: string }>>([])
  const [mailProfileOptions, setMailProfileOptions] = useState<Array<{ value: string; label: string }>>([])
  const visibleColumnMap: Record<string, string[]> = {
    '[user]': ['userid', 'username', 'email', 'expirypwd'],
    acctinfo: ['infokey', 'infovalue'],
    acct: ['accountid'],
    acctdbse: ['databasename', 'ismaster', 'version', 's3backupinfo'],
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
    modginfo: ['infokey', 'infovalue'],
    modlcolm: ['colkey', 'coltype', 'titlecaption', 'colorder', 'collength', 'pageno', 'sectionno', 'columnno', 'rowno', 'fieldno', 'isviewable', 'isbrowsable', 'iseditable'],
    modlcolminfo: ['infokey', 'infovalue'],
    modlappr: ['approvalgroup', 'uppergroup', 'lvl', 'sqlfilter', 'zonegroup'],
    modldocn: ['format', 'month', 'no'],
    modlmail: ['mail', 'action', 'status', 'additional', 'cc', 'subject', 'body', 'reportattachment', 'definedtable'],
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
    expirypwd: 'Expiry Date',
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
    pageno: 'Page No',
    sectionno: 'Section No',
    columnno: 'Column No',
    rowno: 'Row No',
    fieldno: 'Field No',
    isviewable: 'Viewable',
    isbrowsable: 'Browsable',
    iseditable: 'Editable',
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
    mail: 'Mail',
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
    databasename: 'Physical Database',
    ismaster: 'Primary',
    version: 'Version',
    s3backupinfo: 'S3 Backup',
    themepageguid: 'Theme Page',
    modulestatusguid: 'Module Status',
    modulegroupguid: 'Module Group',
    uppersubmenuguid: 'Upper Submenu',
    type: 'Menu Type',
    icon_fa: 'Menu Icon',
    icon_url: 'Icon URL',
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
    'envinfoguid',
    'columninfoguid',
    'approvalguid',
    'docnumberguid',
    'modulemailguid',
    'modulestatusdetailguid',
    'parentmoduleguid',
    'submenuguid',
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
    'isviewable',
    'isbrowsable',
    'iseditable',
  ])
  const copyableTables = new Set(['modl', 'modlinfo', 'modlcolm', 'modlcolminfo', 'modlappr', 'modldocn', 'modlmail'])
  useEffect(() => {
    setTableRows(rows)
    setCheckedRowIndexes(new Set())
    setIsCopyTargetOpen(false)
    setCopyTargets([])
    setCopyTargetGuid('')
    setCopyTargetSearch('')
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
    setListSearch('')
    setIsBulkDeleteOpen(false)
  }, [rows])

  const sourceKey = sourceTable.toLowerCase()
  const normalizedListSearch = listSearch.trim().toLocaleLowerCase()
  const filteredRows = tableRows
    .map((row, originalIndex) => ({ row, originalIndex }))
    .filter(({ row }) => !normalizedListSearch || Object.values(row).some((value) =>
      String(value ?? '').toLocaleLowerCase().includes(normalizedListSearch),
    ))
  const displayedRows = sourceKey === 'menusmnu' && !normalizedListSearch
    ? orderSubmenuRows(filteredRows)
    : filteredRows
  const submenuOptions = sourceKey === 'menusmnu'
    ? tableRows
      .filter((row) => ['treeview', 'treeroot'].includes(String(getCellValue(row, 'type') ?? '').toLowerCase()))
      .map((row) => ({
        value: String(getCellValue(row, 'submenuguid') ?? getCellValue(row, 'menudetailguid') ?? ''),
        label: String(getCellValue(row, 'submenudescription') ?? getCellValue(row, 'caption') ?? ''),
      }))
      .filter((option) => option.value && option.label)
    : []
  const menuTypeOptions = [
    { value: 'label', label: 'Label' },
    { value: 'treeview', label: 'Treeview' },
    { value: 'disable', label: 'Disable' },
  ]
  function displayCellValue(row: MetadataRow, column: string) {
    const value = String(getCellValue(row, column) ?? '')
    if (sourceKey === 'menusmnu' && column.toLowerCase() === 'uppersubmenuguid') {
      return submenuOptions.find((option) => option.value.toLowerCase() === value.toLowerCase())?.label ?? value
    }
    if (sourceKey === 'menusmnu' && column.toLowerCase() === 'type') {
      return menuTypeOptions.find((option) => option.value === value.toLowerCase())?.label ?? value
    }
    if (sourceKey === 'menusmnu' && column.toLowerCase() === 'icon_fa') {
      return <MenuIcon value={value} withLabel />
    }
    if (sourceKey === 'modl' && column.toLowerCase() === 'settingmode') {
      const option = moduleSettingModeOptions.find((candidate) => candidate.value === value)
      return option ? `${option.value} — ${option.label}` : value
    }
    return value
  }
  const allRowsChecked = displayedRows.length > 0
    && displayedRows.every(({ originalIndex }) => checkedRowIndexes.has(originalIndex))

  function toggleRowChecked(index: number) {
    setCheckedRowIndexes((currentIndexes) => {
      const nextIndexes = new Set(currentIndexes)
      if (nextIndexes.has(index)) nextIndexes.delete(index)
      else nextIndexes.add(index)
      return nextIndexes
    })
  }

  function toggleAllRows() {
    setCheckedRowIndexes((currentIndexes) => {
      const nextIndexes = new Set(currentIndexes)
      displayedRows.forEach(({ originalIndex }) => {
        if (allRowsChecked) nextIndexes.delete(originalIndex)
        else nextIndexes.add(originalIndex)
      })
      return nextIndexes
    })
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
    setCopyTargetSearch('')
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
  useEffect(() => {
    if (sourceKey !== 'ugrp' || !selection.databaseName) {
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
  }, [config, selection.databaseName, sourceKey])
  useEffect(() => {
    if (sourceKey !== 'modlmail' || !selection.accountId || !selection.databaseName) {
      setMailActionOptions([])
      setMailStatusOptions([])
      setMailProfileOptions([])
      return
    }

    let cancelled = false
    Promise.all([
      ophAdminService.listParameters(config, selection.accountId, selection.databaseName),
      ophAdminService.listMailProfiles(config, selection.accountId, selection.databaseName),
    ]).then(async ([parameters, profiles]) => {
        const mact = parameters.find((row) => String(row.parameterid ?? '').toUpperCase() === 'MACT')
        const mlst = parameters.find((row) => String(row.parameterid ?? '').toUpperCase() === 'MLST')
        const [actions, statuses] = await Promise.all([
          mact ? ophAdminService.listParameterValues(config, selection.accountId ?? '', selection.databaseName ?? '', String(mact.parameterguid ?? '')) : Promise.resolve([]),
          mlst ? ophAdminService.listParameterValues(config, selection.accountId ?? '', selection.databaseName ?? '', String(mlst.parameterguid ?? '')) : Promise.resolve([]),
        ])
        if (cancelled) return
        setMailProfileOptions(profiles.map((row) => ({
          value: String(row.mailguid ?? ''),
          label: [row.profilename, row.displayname, row.emailaddress].filter(Boolean).join(' — ') || String(row.mailguid ?? ''),
        })).filter((option) => option.value))
        setMailActionOptions(actions.map((row) => ({
          value: String(row.parametervalueguid ?? ''),
          label: String(row.parametervalue ?? row.parameterdescription ?? ''),
        })).filter((option) => option.value))
        setMailStatusOptions(statuses.map((row) => ({
          value: String(row.parametervalueguid ?? ''),
          label: [row.parametervalue, row.parameterdescription].filter(Boolean).join(' — '),
        })).filter((option) => option.value))
      })
      .catch(() => {
        if (!cancelled) {
          setMailActionOptions([])
          setMailStatusOptions([])
          setMailProfileOptions([])
        }
      })

    return () => { cancelled = true }
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
    if (!selection.accountId || !selection.databaseName || !['modl', 'modlappr', 'ugrpmodl'].includes(sourceKey)) {
      setModuleRelationOptions({})
      return
    }

    let cancelled = false
    if (sourceKey === 'modlappr') {
      ophAdminService.listUserGroups(config, selection.accountId, selection.databaseName)
        .then((groups) => {
          if (cancelled) return
          const options = groups.map((row) => ({
            value: String(row.ugroupguid ?? ''),
            label: [row.groupid, row.groupdescription].filter(Boolean).join(' — ') || String(row.ugroupguid ?? ''),
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

    if (sourceKey === 'ugrpmodl') {
      ophAdminService.listModuleTree(config, selection.accountId, selection.databaseName)
        .then((modules) => {
          if (cancelled) return
          setModuleRelationOptions({
            moduleguid: modules.map((row) => ({
              value: String(row.moduleguid ?? ''),
              label: [row.moduleid, row.moduledescription].filter(Boolean).join(' — ') || String(row.moduleguid ?? ''),
            })).filter((option) => option.value),
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
      ophAdminService.listModuleTree(config, selection.accountId, selection.databaseName),
    ]).then(([statuses, groups, accountDatabases, themePages, modules]) => {
      if (cancelled) return
      setModuleRelationOptions({
        parentmoduleguid: modules.map((row) => ({
          value: String(row.moduleguid ?? ''),
          label: [row.moduleid, row.moduledescription].filter(Boolean).join(' — ') || String(row.moduleguid ?? ''),
        })),
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
    if (sourceKey !== 'ugrp' || !selection.databaseName) {
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
  }, [config, selection.databaseName, sourceKey])
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
  const columnPages = sourceKey === 'modlcolm'
    ? Array.from(new Set(displayedRows.map(({ row }) => String(getCellValue(row, 'pageno') || '0'))))
      .sort((left, right) => Number(left) - Number(right) || left.localeCompare(right))
    : []
  const selectedColumnPage = columnPages.includes(activeColumnPage) ? activeColumnPage : columnPages[0] ?? '0'
  const visualColumnRows = sourceKey === 'modlcolm'
    ? displayedRows
      .filter(({ row }) => String(getCellValue(row, 'pageno') || '0') === selectedColumnPage)
      .sort((left, right) => ['sectionno', 'columnno', 'rowno', 'fieldno'].reduce((difference, field) => {
        if (difference) return difference
        return Number(getCellValue(left.row, field) || 0) - Number(getCellValue(right.row, field) || 0)
      }, 0))
    : []
  function metadataFlag(row: MetadataRow, field: string) {
    const value = String(getCellValue(row, field) ?? '').trim().toLowerCase()
    if (!value || ['0', 'false', 'no'].includes(value)) return false
    const numericValue = Number(value)
    return Number.isFinite(numericValue) ? numericValue !== 0 : true
  }

  function renderVisualField(row: MetadataRow, originalIndex: number) {
    return (
      <button key={originalIndex} type="button" className="column-visual-field" onClick={() => openRow(row, originalIndex)}>
        <span className="column-visual-position">R{String(getCellValue(row, 'rowno') || '0')} · F{String(getCellValue(row, 'fieldno') || '0')}</span>
        <strong>{String(getCellValue(row, 'titlecaption') || getCellValue(row, 'colkey') || 'Untitled field')}</strong>
        <small>{String(getCellValue(row, 'colkey') || '')}</small>
        <span className="column-visual-flags">
          <i className={metadataFlag(row, 'isviewable') ? 'enabled' : ''}>Viewable</i>
          <i className={metadataFlag(row, 'isbrowsable') ? 'enabled' : ''}>Browsable</i>
          <i className={metadataFlag(row, 'iseditable') ? 'enabled' : ''}>Editable</i>
        </span>
      </button>
    )
  }
  const overlayColumnMap: Record<string, string[]> = {
    ugrp: ['groupid', 'groupdescription', 'allexceptuser', 'tokenuser', 'allexceptenv', 'tokenenv', 'allexceptmodule'],
    ugrpmodl: ['moduleguid', 'allowaccess', 'allowadd', 'allowedit', 'allowdelete', 'allowforce', 'allowwipe'],
    widg: ['widgetid', 'widgetdescription', 'sqlstr'],
    modlcolm: ['colkey', 'coltype', 'titlecaption', 'colorder', 'collength'],
    modl: ['moduleid', 'moduledescription', 'settingmode', 'parentmoduleguid', 'accountdbguid', 'orderno', 'needlogin', 'themepageguid', 'modulestatusguid', 'modulegroupguid'],
    modlappr: ['approvalgroupguid', 'uppergroupguid', 'lvl', 'sqlfilter', 'zonegroup'],
    modlmail: ['mailguid', 'actionguid', 'tokenstatus', 'additional', 'cc', 'subject', 'body', 'reportattachment', 'definedtable'],
  }
  const overlayColumns = overlayColumnMap[sourceKey]
    ?? columns.filter((column) => !['createddate', 'updateddate'].includes(column.toLowerCase()))

  useEffect(() => {
    if (!autoOpenFirstRow || !tableRows[0] || selectedRow) return
    openRow(tableRows[0], 0)
  }, [autoOpenFirstRow, tableRows, selectedRow])

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
    setIsResettingPassword(false)
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
    setIsResettingPassword(false)
  }

  async function saveDraft() {
    if (!selection.databaseName) {
      setActionError('Cannot save row: database is missing.')
      return
    }

    const infoTables = new Set(['modlinfo', 'modginfo', 'modlcolminfo', 'userinfo', 'acctinfo'])
    if (infoTables.has(sourceKey)) {
      const infoKey = String(draftRow.infokey ?? '').trim()
      const infoValue = String(draftRow.infovalue ?? '')
      if (!infoKey) {
        setActionError('Info Key is required.')
        return
      }
      const duplicateInfoKey = tableRows.some((row, index) =>
        index !== selectedRowIndex
        && String(getCellValue(row, 'infokey') ?? '').trim().toLowerCase() === infoKey.toLowerCase())
      if (duplicateInfoKey) {
        setActionError(`Info Key "${infoKey}" already exists.`)
        return
      }
      if (sourceKey === 'modlinfo' && /^dplx(?:_rpt)?$/i.test(infoKey)) {
        if (!infoValue.trim()) {
          setActionError('Info Value must contain the DPLX report XML.')
          return
        }
        const dplx = parseDplx(infoValue)
        if (dplx.error) {
          setActionError(`Invalid DPLX XML: ${dplx.error}`)
          return
        }
      }
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
        selection.moduleGroupGuid,
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
    if (overlayOnly) onOverlayClose?.()
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
    setIsResettingPassword(false)
    if (overlayOnly) onOverlayClose?.()
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

  async function deleteCheckedRows() {
    if (!selection.databaseName || checkedRowIndexes.size === 0) return
    setIsBulkDeleting(true)
    setActionError('')
    setActionNotice('')
    const selectedEntries = Array.from(checkedRowIndexes)
      .sort((left, right) => left - right)
      .map((index) => ({ index, row: tableRows[index] }))
      .filter((entry): entry is { index: number; row: MetadataRow } => Boolean(entry.row))
    const deletedIndexes = new Set<number>()
    const failures: string[] = []

    for (const entry of selectedEntries) {
      try {
        await ophAdminService.deleteMetadataRow(config, selection.databaseName, sourceTable, entry.row)
        deletedIndexes.add(entry.index)
      } catch (deleteError) {
        failures.push(deleteError instanceof Error ? deleteError.message : String(deleteError))
      }
    }

    setTableRows((currentRows) => currentRows.filter((_, index) => !deletedIndexes.has(index)))
    setCheckedRowIndexes(new Set())
    setSelectedRow(null)
    setSelectedRowIndex(null)
    setDraftRow({})
    setIsBulkDeleting(false)
    setIsBulkDeleteOpen(false)
    if (deletedIndexes.size > 0) setActionNotice(`${deletedIndexes.size} row(s) deleted.`)
    if (failures.length > 0) {
      setActionError(`${failures.length} row(s) could not be deleted. ${failures[0]}`)
    }
  }

  async function resetSelectedUserPassword() {
    if (!selection.databaseName || !selection.accountId || !selectedRow) {
      setActionError('Cannot reset password: database, account, or user is missing.')
      return
    }

    const userGuid = String(getCellValue(selectedRow, 'userguid') || selection.userGuid || '')
    const userId = String(getCellValue(selectedRow, 'userid') || selection.label || '')
    if (!userGuid && !userId) {
      setActionError('Cannot reset password: User ID and user key are missing.')
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
      await Promise.race([
        ophAdminService.resetUserPassword(
          config,
          selection.databaseName,
          selection.accountId,
          userGuid,
          userId,
          newPassword,
        ),
        new Promise<never>((_resolve, reject) => {
          window.setTimeout(() => reject(new Error(`Password reset for ${userId} did not return after 25 seconds. No detailed response was received from the desktop command.`)), 25_000)
        }),
      ])
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
      {!overlayOnly ? <>
      <div className="metadata-toolbar">
        <label className="metadata-search">
          <Search size={16} />
          <input
            type="search"
            value={listSearch}
            placeholder={`Search ${sourceTable}...`}
            aria-label={`Search ${sourceTable}`}
            onChange={(event) => setListSearch(event.target.value)}
          />
        </label>
        <button type="button" onClick={openCreate}>Add</button>
        {sourceKey === 'modlcolm' ? (
          <div className="column-view-toggle" aria-label="Column list mode">
            <button type="button" className={columnListMode === 'table' ? 'active' : ''} onClick={() => setColumnListMode('table')}>Table</button>
            <button type="button" className={columnListMode === 'visual' ? 'active' : ''} onClick={() => setColumnListMode('visual')}>Visual</button>
          </div>
        ) : null}
        <button type="button" disabled={displayedRows.length === 0} onClick={toggleAllRows}>
          {allRowsChecked ? 'Clear All' : 'Select All'}
        </button>
        {copyableTables.has(sourceKey) ? (
          <button type="button" disabled={checkedRowIndexes.size === 0} onClick={openCopyTo}>Copy To</button>
        ) : null}
        <button
          type="button"
          className="danger-button"
          disabled={checkedRowIndexes.size === 0}
          onClick={() => setIsBulkDeleteOpen(true)}
        >
          Delete Selected
        </button>
        <span className="selection-count">{checkedRowIndexes.size} selected</span>
      </div>
      {!selectedRow && actionError ? <div className="connection-error">{actionError}</div> : null}
      {!selectedRow && actionNotice ? <div className="action-notice">{actionNotice}</div> : null}
      {sourceKey === 'modlcolm' && columnListMode === 'visual' ? (
        <div className="column-visual-designer">
          <div className="column-page-tabs" role="tablist" aria-label="Column pages">
            {columnPages.map((page) => (
              <button key={page} type="button" role="tab" aria-selected={selectedColumnPage === page} className={selectedColumnPage === page ? 'active' : ''} onClick={() => setActiveColumnPage(page)}>Page {page}</button>
            ))}
          </div>
          {visualColumnRows.length === 0 ? <div className="empty-result">No columns found on this page.</div> : (
            <div className="column-visual-split">
              {(['browse', 'form'] as const).map((visualArea) => {
                const visibilityField = visualArea === 'browse' ? 'isbrowsable' : 'isviewable'
                const areaRows = visualColumnRows.filter(({ row }) => metadataFlag(row, visibilityField))
                const availableRows = visualColumnRows.filter(({ row }) => !metadataFlag(row, visibilityField))
                return (
                  <section key={visualArea} className="column-visual-area">
                    <header><strong>{visualArea === 'browse' ? 'Browse' : 'Form'}</strong><span>{areaRows.length} field(s)</span></header>
                    {areaRows.length === 0 ? <div className="empty-result">No active {visualArea} fields on Page {selectedColumnPage}.</div> : (
                      visualArea === 'browse' ? (
                        <div className="column-visual-fields">
                          {areaRows.map(({ row, originalIndex }) => renderVisualField(row, originalIndex))}
                        </div>
                      ) : (
                        <div className="column-form-sections">
                          {Array.from(new Set(areaRows.map(({ row }) => String(getCellValue(row, 'sectionno') || '0')))).map((sectionNo) => {
                            const sectionRows = areaRows.filter(({ row }) => String(getCellValue(row, 'sectionno') || '0') === sectionNo)
                            const columnNos = Array.from(new Set(sectionRows.map(({ row }) => String(getCellValue(row, 'columnno') || '0'))))
                              .sort((left, right) => Number(left) - Number(right) || left.localeCompare(right))
                            return (
                              <section key={sectionNo} className="column-form-section">
                                <header><strong>Section {sectionNo}</strong><span>{sectionRows.length} field(s)</span></header>
                                <div className="column-form-columns" style={{ gridTemplateColumns: `repeat(${Math.max(1, columnNos.length)}, minmax(180px, 1fr))` }}>
                                  {columnNos.map((columnNo) => (
                                    <div key={columnNo} className="column-form-column">
                                      <strong>Column {columnNo}</strong>
                                      <div>{sectionRows.filter(({ row }) => String(getCellValue(row, 'columnno') || '0') === columnNo).map(({ row, originalIndex }) => renderVisualField(row, originalIndex))}</div>
                                    </div>
                                  ))}
                                </div>
                              </section>
                            )
                          })}
                        </div>
                      )
                    )}
                    <section className="column-available-fields">
                      <header><strong>Available Columns</strong><span>{availableRows.length} field(s)</span></header>
                      {availableRows.length === 0 ? <div className="empty-result">All columns are already used in {visualArea}.</div> : (
                        <div className="column-visual-fields">
                          {availableRows.map(({ row, originalIndex }) => renderVisualField(row, originalIndex))}
                        </div>
                      )}
                    </section>
                  </section>
                )
              })}
            </div>
          )}
        </div>
      ) : <div className="table-card metadata-table">
        {displayedRows.length === 0 ? (
          <div className="empty-result">{tableRows.length === 0 ? 'No rows found.' : 'No matching rows found.'}</div>
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
              {displayedRows.map(({ row, originalIndex }) => (
                <tr
                  className={`clickable-row ${selectedRowIndex === originalIndex ? 'selected-row' : ''} ${checkedRowIndexes.has(originalIndex) ? 'checked-row' : ''}`}
                  key={originalIndex}
                  onClick={() => openRow(row, originalIndex)}
                >
                  <td className="selection-column" onClick={(event) => event.stopPropagation()}>
                    <input
                      type="checkbox"
                      aria-label={`Select row ${originalIndex + 1}`}
                      checked={checkedRowIndexes.has(originalIndex)}
                      onChange={() => toggleRowChecked(originalIndex)}
                    />
                  </td>
                  {columns.map((column) => (
                    <td key={column}>{displayCellValue(row, column)}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>}
      </> : null}
      {isBulkDeleteOpen ? (
        <div className="row-detail-backdrop" onMouseDown={() => !isBulkDeleting && setIsBulkDeleteOpen(false)}>
          <aside className="row-detail-overlay" onMouseDown={(event) => event.stopPropagation()}>
            <div className="row-detail-header">
              <div><span className="eyebrow">Delete Selected</span><h2>{sourceTable}</h2></div>
              <button className="overlay-close-button" type="button" disabled={isBulkDeleting} onClick={() => setIsBulkDeleteOpen(false)}>×</button>
            </div>
            <div className="row-detail-form">
              <p className="delete-warning">Delete {checkedRowIndexes.size} selected row(s)? This action cannot be undone.</p>
              <div className="row-detail-actions">
                <button className="danger-button" type="button" disabled={isBulkDeleting} onClick={() => { void deleteCheckedRows() }}>
                  {isBulkDeleting ? 'Deleting…' : `Delete ${checkedRowIndexes.size} Row(s)`}
                </button>
                <button type="button" disabled={isBulkDeleting} onClick={() => setIsBulkDeleteOpen(false)}>Cancel</button>
              </div>
            </div>
          </aside>
        </div>
      ) : null}
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
                {column.toLowerCase() === 'mailguid' && sourceKey === 'modlmail' ? (
                  <select
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    disabled={!isEditing}
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  >
                    <option value="">Select mail profile</option>
                    {mailProfileOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                  </select>
                ) : column.toLowerCase() === 'actionguid' && sourceKey === 'modlmail' ? (
                  <select
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    disabled={!isEditing}
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  >
                    <option value="">Select mail action</option>
                    {mailActionOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                  </select>
                ) : column.toLowerCase() === 'coltype' && sourceKey === 'modlcolm' ? (
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
                ) : sourceKey === 'modl' && column.toLowerCase() === 'settingmode' ? (
                  <select
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    disabled={!isEditing}
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  >
                    <option value="">Select setting mode</option>
                    {moduleSettingModeOptions.map((option) => <option key={option.value} value={option.value}>{option.value} — {option.label}</option>)}
                  </select>
                ) : sourceKey === 'menusmnu' && column.toLowerCase() === 'uppersubmenuguid' ? (
                  <select
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    disabled={!isEditing}
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  >
                    <option value="">None</option>
                    {submenuOptions
                      .filter((option) => option.value !== String(getCellValue(selectedRow, 'menudetailguid') ?? ''))
                      .map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                  </select>
                ) : sourceKey === 'menusmnu' && column.toLowerCase() === 'type' ? (
                  <select
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    disabled={!isEditing}
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  >
                    <option value="">Select menu type</option>
                    {menuTypeOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                  </select>
                ) : sourceKey === 'menusmnu' && column.toLowerCase() === 'icon_fa' ? (
                  <div className="menu-icon-field">
                    <MenuIcon value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')} withLabel />
                    <select
                      value={findMenuIcon(draftRow[column] ?? String(getCellValue(selectedRow, column) ?? ''))?.key ?? (draftRow[column] ?? String(getCellValue(selectedRow, column) ?? ''))}
                      disabled={!isEditing}
                      onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                    >
                      <option value="">No icon</option>
                      {menuIconCatalog.map((icon) => <option key={icon.key} value={icon.key}>{icon.label}</option>)}
                    </select>
                  </div>
                ) : moduleRelationOptions[column.toLowerCase()] ? (
                  <select
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    disabled={!isEditing}
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  >
                    <option value="">None</option>
                    {moduleRelationOptions[column.toLowerCase()]
                      .filter((option) => option.value && !(column.toLowerCase() === 'parentmoduleguid'
                        && option.value === String(getCellValue(selectedRow, 'moduleguid') ?? '')))
                      .map((option) => (
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
                        : sourceKey === 'modlmail' && column.toLowerCase() === 'tokenstatus'
                          ? mailStatusOptions
                        : []}
                    inputLabel={column.toLowerCase() === 'tokenuser'
                      ? 'User ID'
                      : sourceKey === 'modlmail' && column.toLowerCase() === 'tokenstatus'
                        ? 'Mail Status'
                        : 'Module Group ID'}
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
                ) : sourceKey === 'modlcolminfo'
                  && column.toLowerCase() === 'infovalue'
                  && String(getCellValue({ ...selectedRow, ...draftRow }, 'infokey') ?? '').toLowerCase() === 'coltype' ? (
                  <>
                    <input
                      list="editor-field-type-options"
                      value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                      readOnly={!isEditing}
                      placeholder="Select or enter a field type"
                      onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                    />
                    <datalist id="editor-field-type-options">
                      {editorFieldTypeOptions.map(([value, label]) => (
                        <option key={value} value={value}>{label}</option>
                      ))}
                    </datalist>
                  </>
                ) : column.toLowerCase() === 'infovalue' || (sourceKey === 'modlmail' && column.toLowerCase() === 'body') ? (
                  <textarea
                    className={column.toLowerCase() === 'infovalue' ? 'raw-metadata-editor' : undefined}
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    readOnly={!isEditing}
                    rows={8}
                    spellCheck={false}
                    autoCorrect="off"
                    autoCapitalize="none"
                    data-gramm="false"
                    data-enable-grammarly="false"
                    onChange={(event) => setDraftRow((currentDraft) => ({ ...currentDraft, [column]: event.target.value }))}
                  />
                ) : (draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')).length > 80 ? (
                  <textarea
                    value={draftRow[column] ?? String(getCellValue(selectedRow, column) ?? '')}
                    readOnly={!isEditing}
                    rows={4}
                    spellCheck={false}
                    autoCorrect="off"
                    autoCapitalize="none"
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
          {sourceKey === 'modlcolm' && !isCreating && onNavigate ? (
            <div className="row-detail-links row-detail-inline-links">
              <a href="#" onClick={(event) => {
                event.preventDefault()
                const columnGuid = String(getCellValue(selectedRow, 'columnguid') ?? '')
                const columnLabel = String(getCellValue(selectedRow, 'colkey') || 'Column')
                if (!columnGuid) {
                  setActionError(`Cannot open Column Info for ${columnLabel}: column key is missing.`)
                  return
                }
                cancelEdit()
                onNavigate({
                  ...selection,
                  id: `${selection.databaseId}:module:${selection.moduleGuid}:columns:${columnGuid}`,
                  kind: 'module-column',
                  label: columnLabel,
                  columnGuid,
                })
              }}>Open Column Info</a>
            </div>
          ) : null}
          {sourceKey === 'modg' && !isCreating && onNavigate ? (
            <div className="row-detail-links row-detail-inline-links">
              <a href="#" onClick={(event) => {
                event.preventDefault()
                const moduleGroupGuid = String(getCellValue(selectedRow, 'modulegroupguid') ?? '')
                const moduleGroupLabel = String(
                  getCellValue(selectedRow, 'modulegroupid')
                  || getCellValue(selectedRow, 'modulegroupname')
                  || 'Module Group',
                )
                if (!moduleGroupGuid) {
                  setActionError(`Cannot open Module Group Info for ${moduleGroupLabel}: module group is missing.`)
                  return
                }
                cancelEdit()
                onNavigate({
                  ...selection,
                  id: `${selection.databaseId}:modules:groups:${moduleGroupGuid}`,
                  kind: 'module-category',
                  label: moduleGroupLabel,
                  moduleGroupGuid,
                })
              }}>Open Module Group Info</a>
            </div>
          ) : null}
          <div className="row-detail-actions">
            <button type="button" onClick={saveDraft}>Save</button>
            {sourceKey === 'modlinfo'
              && !isCreating
              && /^(view(?:_|$)|script(?:_|$))/i.test(String(getCellValue(selectedRow, 'infokey') ?? '')) ? (
                <button type="button" onClick={() => {
                  window.dispatchEvent(new CustomEvent('oph:open-metadata-query', {
                    detail: { databaseName: selection.databaseName, row: selectedRow },
                  }))
                  cancelEdit()
                }}>
                  See in Query
                </button>
              ) : null}
            {sourceKey === 'widg' && !isCreating && String(getCellValue(selectedRow, 'sqlstr') ?? '').trim() ? (
              <button type="button" onClick={() => {
                window.dispatchEvent(new CustomEvent('oph:open-metadata-query', {
                  detail: { databaseName: selection.databaseName, row: selectedRow },
                }))
                cancelEdit()
              }}>
                See in Query
              </button>
            ) : null}
            {sourceKey === 'modlinfo'
              && !isCreating
              && /^dplx(?:_rpt)?$/i.test(String(getCellValue(selectedRow, 'infokey') ?? '')) ? (
                <button type="button" onClick={() => {
                  window.dispatchEvent(new CustomEvent('oph:open-report-designer', {
                    detail: { databaseName: selection.databaseName, row: selectedRow },
                  }))
                  cancelEdit()
                }}>
                  Design Report
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
              <input
                type="search"
                value={copyTargetSearch}
                disabled={isLoadingCopyTargets || isCopyingRows}
                placeholder="Search destination parent..."
                onChange={(event) => setCopyTargetSearch(event.target.value)}
              />
              <select value={copyTargetGuid} disabled={isLoadingCopyTargets || isCopyingRows} onChange={(event) => setCopyTargetGuid(event.target.value)}>
                <option value="">{isLoadingCopyTargets ? 'Loading destinations…' : 'Select destination'}</option>
                {copyTargets.filter((target) => {
                  if (String(target.targetguid) === copyTargetGuid) return true
                  const search = copyTargetSearch.trim().toLocaleLowerCase()
                  if (!search) return true
                  return `${String(target.targetlabel ?? '')} ${String(target.targetdescription ?? '')} ${String(target.targetguid ?? '')}`.toLocaleLowerCase().includes(search)
                }).map((target) => (
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

function orderSubmenuRows(rows: Array<{ row: MetadataRow; originalIndex: number }>) {
  function field(row: MetadataRow, name: string) {
    const key = Object.keys(row).find((candidate) => candidate.toLowerCase() === name)
    return key ? String(row[key] ?? '') : ''
  }

  const rowByGuid = new Map(rows.map((entry) => [
    (field(entry.row, 'menudetailguid') || field(entry.row, 'submenuguid')).toLowerCase(),
    entry,
  ]))
  const childrenByParent = new Map<string, Array<{ row: MetadataRow; originalIndex: number }>>()
  const roots: Array<{ row: MetadataRow; originalIndex: number }> = []

  rows.forEach((entry) => {
    const parentGuid = field(entry.row, 'uppersubmenuguid').toLowerCase()
    if (!parentGuid || !rowByGuid.has(parentGuid)) {
      roots.push(entry)
      return
    }
    const children = childrenByParent.get(parentGuid) ?? []
    children.push(entry)
    childrenByParent.set(parentGuid, children)
  })

  const ordered: Array<{ row: MetadataRow; originalIndex: number }> = []
  const visited = new Set<number>()
  function append(entry: { row: MetadataRow; originalIndex: number }) {
    if (visited.has(entry.originalIndex)) return
    visited.add(entry.originalIndex)
    ordered.push(entry)
    const guid = (field(entry.row, 'menudetailguid') || field(entry.row, 'submenuguid')).toLowerCase()
    childrenByParent.get(guid)?.forEach(append)
  }

  roots.forEach(append)
  rows.forEach(append)
  return ordered
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
