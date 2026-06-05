import { invoke } from '@tauri-apps/api/core'
import type {
  OphConnectionConfig,
  OphApproval,
  OphDatabase,
  OphModule,
  OphModuleColumn,
  OphServer,
  OphTreeNode,
  MetadataRow,
  TestConnectionResult,
  ValidationItem,
} from '../types/domain'

const connectionConfigKey = 'oph-control-studio.connection-config'

const servers: OphServer[] = [
  {
    id: 'srv-prod',
    name: 'Production OPH',
    host: '10.10.1.20',
    port: 1433,
    authType: 'sql',
    defaultDatabase: 'oph_core',
    username: 'oph_admin',
    trustServerCertificate: true,
    encrypt: true,
    status: 'online',
    databases: 18,
    lastChecked: '2 minutes ago',
  },
  {
    id: 'srv-staging',
    name: 'Staging OPH',
    host: 'staging-sql.local',
    port: 1433,
    authType: 'windows',
    defaultDatabase: 'oph_core',
    trustServerCertificate: true,
    encrypt: false,
    status: 'warning',
    databases: 7,
    lastChecked: '14 minutes ago',
  },
  {
    id: 'srv-archive',
    name: 'Archive Node',
    host: '172.16.8.44',
    port: 1433,
    authType: 'sql',
    defaultDatabase: 'oph_core',
    username: 'readonly',
    trustServerCertificate: false,
    encrypt: true,
    status: 'offline',
    databases: 4,
    lastChecked: '1 hour ago',
  },
]

const databases: OphDatabase[] = [
  {
    id: 'db-core',
    name: 'oph_core',
    databaseName: 'oph_core',
    serverId: 'srv-prod',
    type: 'core',
    status: 'healthy',
    modules: 128,
    size: '14.2 GB',
    updatedAt: 'Today 09:42',
  },
  {
    id: 'db-finance',
    name: 'oph_account_finance',
    databaseName: 'oph_account_finance',
    serverId: 'srv-prod',
    type: 'account',
    status: 'healthy',
    modules: 38,
    size: '6.8 GB',
    updatedAt: 'Today 09:30',
  },
  {
    id: 'db-hr',
    name: 'oph_account_hr',
    databaseName: 'oph_account_hr',
    serverId: 'srv-prod',
    type: 'account',
    status: 'needs-review',
    modules: 24,
    size: '3.1 GB',
    updatedAt: 'Yesterday 18:10',
  },
  {
    id: 'db-event',
    name: 'oph_eventdb_preview',
    databaseName: 'oph_eventdb_preview',
    serverId: 'srv-staging',
    type: 'eventdb',
    status: 'needs-review',
    modules: 0,
    size: '860 MB',
    updatedAt: 'Yesterday 15:22',
  },
]

const modules: OphModule[] = [
  {
    moduleGuid: 'mod-invoice',
    accountGuid: 'acct-finance',
    moduleId: 'FIN.INVOICE',
    description: 'Customer Invoice',
    settingMode: 'transaction',
    accountDbGuid: 'db-finance',
    orderNo: 10,
    needLogin: true,
    themePageGuid: 'theme-finance-form',
    moduleStatusGuid: 'active',
    moduleGroupGuid: 'finance',
    columns: 42,
    approvals: 3,
    numbering: 'INV/{yyyy}/{MM}/####',
  },
  {
    moduleGuid: 'mod-payment',
    accountGuid: 'acct-finance',
    moduleId: 'FIN.PAYMENT',
    description: 'Payment Receipt',
    settingMode: 'transaction',
    accountDbGuid: 'db-finance',
    parentModuleGuid: 'mod-invoice',
    orderNo: 20,
    needLogin: true,
    themePageGuid: 'theme-finance-form',
    moduleStatusGuid: 'active',
    moduleGroupGuid: 'finance',
    columns: 31,
    approvals: 2,
    numbering: 'PAY/{yyyy}/{MM}/####',
  },
  {
    moduleGuid: 'mod-employee',
    accountGuid: 'acct-hr',
    moduleId: 'HR.EMPLOYEE',
    description: 'Employee Master',
    settingMode: 'master',
    accountDbGuid: 'db-hr',
    orderNo: 5,
    needLogin: true,
    themePageGuid: 'theme-master-detail',
    moduleStatusGuid: 'review',
    moduleGroupGuid: 'human-resource',
    columns: 56,
    approvals: 1,
    numbering: 'EMP/####',
  },
]

const columns: OphModuleColumn[] = [
  {
    columnGuid: 'col-invoice-no',
    moduleGuid: 'mod-invoice',
    colKey: 'InvoiceNo',
    colType: 'nvarchar',
    titleCaption: 'Invoice No',
    colOrder: 10,
    colLength: 40,
  },
  {
    columnGuid: 'col-customer',
    moduleGuid: 'mod-invoice',
    colKey: 'CustomerGUID',
    colType: 'uniqueidentifier',
    titleCaption: 'Customer',
    colOrder: 20,
    colLength: 16,
  },
  {
    columnGuid: 'col-total',
    moduleGuid: 'mod-invoice',
    colKey: 'GrandTotal',
    colType: 'decimal',
    titleCaption: 'Grand Total',
    colOrder: 90,
    colLength: 18,
  },
]

const approvals: OphApproval[] = [
  {
    approvalGuid: 'appr-fin-spv',
    moduleGuid: 'mod-invoice',
    approvalGroupGuid: 'Finance Supervisor',
    level: 1,
    sqlFilter: 'GrandTotal <= 10000000',
    zoneGroup: 'FIN-JKT',
  },
  {
    approvalGuid: 'appr-fin-mgr',
    moduleGuid: 'mod-invoice',
    approvalGroupGuid: 'Finance Manager',
    upperGroupGuid: 'Finance Supervisor',
    level: 2,
    sqlFilter: 'GrandTotal > 10000000',
    zoneGroup: 'FIN-ID',
  },
]

const validations: ValidationItem[] = [
  {
    id: 'val-cert',
    severity: 'warning',
    title: 'Certificate trust is enabled',
    detail: 'Production OPH uses trustServerCertificate. Review before rollout.',
  },
  {
    id: 'val-hr-status',
    severity: 'info',
    title: 'Module status needs review',
    detail: 'HR.EMPLOYEE is marked review and should be validated before migration.',
  },
  {
    id: 'val-eventdb',
    severity: 'warning',
    title: 'EventDB preview is incomplete',
    detail: 'EventDB has no migrated modules yet.',
  },
]

function getAccountId(database: OphDatabase): string {
  const [, accountId] = database.id.split(':')
  return accountId || database.name
}

const moduleCategories = [
  { key: 'core', label: 'Core', settingMode: 0 },
  { key: 'master', label: 'Master', settingMode: 1 },
  { key: 'transaction', label: 'Transaction', settingMode: 4 },
  { key: 'report', label: 'Report', settingMode: 5 },
  { key: 'blank', label: 'Blank', settingMode: 6 },
  { key: 'view', label: 'View', settingMode: 7 },
]

const moduleActions = ['Columns', 'Children', 'Approvals', 'Numbering', 'Mails']

function buildModuleNode(
  database: OphDatabase,
  accountId: string,
  module: MetadataRow,
  moduleRows: MetadataRow[] = [],
  columnRows: MetadataRow[] = [],
  actionLabels: string[] = moduleActions,
  visitedModuleGuids = new Set<string>(),
): OphTreeNode {
  const moduleGuid = String(module.moduleguid ?? '')
  const moduleId = String(module.moduleid ?? moduleGuid)
  const settingMode = Number(module.settingmode)
  const columns = columnRows.filter((column) => String(column.moduleguid ?? '') === moduleGuid)
  const nextVisitedModuleGuids = new Set([...visitedModuleGuids, moduleGuid])
  const childModules = moduleRows.filter((childModule) => {
    const childModuleGuid = String(childModule.moduleguid ?? '')
    return String(childModule.parentmoduleguid ?? '') === moduleGuid && !nextVisitedModuleGuids.has(childModuleGuid)
  })

  return {
    id: `${database.id}:module:${moduleGuid}`,
    label: moduleId,
    kind: 'module',
    description: String(module.moduledescription ?? ''),
    accountId,
    databaseName: database.databaseName,
    databaseId: database.id,
    moduleGuid,
    settingMode,
    children: actionLabels.map((action) => {
      const isColumns = action === 'Columns'
      const isChildren = action === 'Children'

      return {
        id: `${database.id}:module:${moduleGuid}:${action.toLowerCase()}`,
        label: action,
        kind: 'module-action' as const,
        accountId,
        databaseName: database.databaseName,
        databaseId: database.id,
        moduleGuid,
        settingMode,
        children: isColumns
          ? columns.map((column) => ({
              id: `${database.id}:module:${moduleGuid}:columns:${String(column.columnguid ?? column.colkey ?? '')}`,
              label: String(column.colkey ?? ''),
              kind: 'module-column' as const,
              accountId,
              databaseName: database.databaseName,
              databaseId: database.id,
              moduleGuid,
              columnGuid: String(column.columnguid ?? ''),
              settingMode,
            }))
          : isChildren
            ? childModules.map((childModule) =>
                buildModuleNode(
                  database,
                  accountId,
                  childModule,
                  moduleRows,
                  columnRows,
                  ['Columns', 'Children'],
                  nextVisitedModuleGuids,
                ),
              )
          : undefined,
      }
    }),
  }
}

function buildDatabaseChildren(
  database: OphDatabase,
  moduleRows: MetadataRow[] = [],
  columnRows: MetadataRow[] = [],
  subAccountRows: MetadataRow[] = [],
  themeRows: MetadataRow[] = [],
  userRows: MetadataRow[] = [],
  userGroupRows: MetadataRow[] = [],
): OphTreeNode[] {
  const accountId = getAccountId(database)

  function buildSubAccountNode(account: MetadataRow, visitedAccountGuids = new Set<string>()): OphTreeNode {
    const accountGuid = String(account.accountguid ?? '')
    const childAccountId = String(account.accountid ?? accountGuid)
    const nextVisitedAccountGuids = new Set([...visitedAccountGuids, accountGuid])
    const children = subAccountRows.filter((childAccount) => {
      const childAccountGuid = String(childAccount.accountguid ?? '')
      return String(childAccount.parentaccountguid ?? '') === accountGuid && !nextVisitedAccountGuids.has(childAccountGuid)
    })

    return {
      id: `${database.id}:account:sub-account:${accountGuid || childAccountId}`,
      label: childAccountId,
      kind: 'account',
      description: String(account.accountpath ?? ''),
      accountId: childAccountId,
      databaseName: database.databaseName,
      databaseId: database.id,
      children: children.map((childAccount) => buildSubAccountNode(childAccount, nextVisitedAccountGuids)),
    }
  }

  const childAccountGuids = new Set(subAccountRows.map((account) => String(account.accountguid ?? '')))
  const rootSubAccounts = subAccountRows.filter((account) => !childAccountGuids.has(String(account.parentaccountguid ?? '')))

  return [
    {
      id: `${database.id}:modules`,
      label: 'Modules',
      kind: 'modules',
      accountId,
      databaseName: database.databaseName,
      databaseId: database.id,
      children: [
        ...moduleCategories.map((category) => ({
          id: `${database.id}:modules:${category.key}`,
          label: category.label,
          kind: 'module-category' as const,
          accountId,
          databaseName: database.databaseName,
          databaseId: database.id,
          settingMode: category.settingMode,
          children: moduleRows
            .filter((module) => {
              const parentModuleGuid = String(module.parentmoduleguid ?? '')
              return Number(module.settingmode) === category.settingMode && parentModuleGuid === ''
            })
            .map((module) => buildModuleNode(database, accountId, module, moduleRows, columnRows)),
        })),
        { id: `${database.id}:modules:status`, label: 'Module Status', kind: 'module-category', accountId, databaseName: database.databaseName, databaseId: database.id },
        { id: `${database.id}:modules:groups`, label: 'Module Groups', kind: 'module-category', accountId, databaseName: database.databaseName, databaseId: database.id },
      ],
    },
    {
      id: `${database.id}:security`,
      label: 'Security',
      kind: 'security',
      accountId,
      databaseName: database.databaseName,
      databaseId: database.id,
      children: [
        {
          id: `${database.id}:security:users`,
          label: 'Users',
          kind: 'security',
          accountId,
          databaseName: database.databaseName,
          databaseId: database.id,
          children: userRows.map((user) => ({
            id: `${database.id}:security:user:${String(user.userguid ?? user.userid ?? '')}`,
            label: String(user.userid ?? ''),
            kind: 'security-user' as const,
            description: String(user.username ?? ''),
            accountId,
            databaseName: database.databaseName,
            databaseId: database.id,
            userGuid: String(user.userguid ?? ''),
          })),
        },
        {
          id: `${database.id}:security:user-groups`,
          label: 'User Groups',
          kind: 'security',
          accountId,
          databaseName: database.databaseName,
          databaseId: database.id,
          children: userGroupRows.map((group) => ({
            id: `${database.id}:security:user-group:${String(group.ugroupguid ?? group.groupid ?? '')}`,
            label: String(group.groupid ?? ''),
            kind: 'security-group' as const,
            description: String(group.groupdescription ?? ''),
            accountId,
            databaseName: database.databaseName,
            databaseId: database.id,
            userGroupGuid: String(group.ugroupguid ?? ''),
          })),
        },
      ],
    },
    {
      id: `${database.id}:interface`,
      label: 'Interface',
      kind: 'interface',
      accountId,
      databaseName: database.databaseName,
      databaseId: database.id,
      children: [
        {
          id: `${database.id}:interface:themes`,
          label: 'Themes',
          kind: 'interface',
          accountId,
          databaseName: database.databaseName,
          databaseId: database.id,
          children: themeRows.map((theme) => ({
            id: `${database.id}:interface:theme:${String(theme.themeguid ?? theme.themecode ?? '')}`,
            label: String(theme.themecode ?? theme.themename ?? ''),
            kind: 'theme' as const,
            description: String(theme.themename ?? ''),
            accountId,
            databaseName: database.databaseName,
            databaseId: database.id,
            themeGuid: String(theme.themeguid ?? ''),
          })),
        },
        { id: `${database.id}:interface:menus`, label: 'Menus', kind: 'interface', accountId, databaseName: database.databaseName, databaseId: database.id },
        { id: `${database.id}:interface:translator`, label: 'Translator', kind: 'interface', accountId, databaseName: database.databaseName, databaseId: database.id },
      ],
    },
    {
      id: `${database.id}:account`,
      label: 'Account',
      kind: 'account',
      accountId,
      databaseName: database.databaseName,
      databaseId: database.id,
      children: [
        {
          id: `${database.id}:account:sub-accounts`,
          label: 'Sub Accounts',
          kind: 'account',
          accountId,
          databaseName: database.databaseName,
          databaseId: database.id,
          children: rootSubAccounts.map((account) => buildSubAccountNode(account)),
        },
        { id: `${database.id}:account:databases`, label: 'Databases', kind: 'account', accountId, databaseName: database.databaseName, databaseId: database.id },
        { id: `${database.id}:account:parameters`, label: 'Parameters', kind: 'account', accountId, databaseName: database.databaseName, databaseId: database.id },
        { id: `${database.id}:account:widgets`, label: 'Widgets', kind: 'account', accountId, databaseName: database.databaseName, databaseId: database.id },
        { id: `${database.id}:account:mail`, label: 'Mail', kind: 'account', accountId, databaseName: database.databaseName, databaseId: database.id },
      ],
    },
  ]
}

function buildTree(
  config: OphConnectionConfig,
  discoveredDatabases: OphDatabase[],
  moduleRowsByDatabaseId: Record<string, MetadataRow[]> = {},
  columnRowsByDatabaseId: Record<string, MetadataRow[]> = {},
  subAccountRowsByDatabaseId: Record<string, MetadataRow[]> = {},
  themeRowsByDatabaseId: Record<string, MetadataRow[]> = {},
  userRowsByDatabaseId: Record<string, MetadataRow[]> = {},
  userGroupRowsByDatabaseId: Record<string, MetadataRow[]> = {},
): OphTreeNode {
  return {
    id: 'servers',
    label: 'Servers',
    kind: 'root',
    children: config.servers.map((server) => ({
      id: server.id,
      label: server.name,
      kind: 'server',
      description: `${server.host}:${server.port}`,
      status: server.status,
      serverId: server.id,
      children: discoveredDatabases
        .filter((database) => database.serverId === server.id)
        .map((database) => ({
          id: database.id,
          label: database.name,
          kind: 'database',
          description:
            database.updatedAt === 'Default database'
              ? 'Default database'
              : 'Account database from OPH core',
          status: database.status,
          serverId: server.id,
          databaseId: database.id,
          accountId: getAccountId(database),
          databaseName: database.databaseName,
          children: buildDatabaseChildren(
            database,
            moduleRowsByDatabaseId[database.id] ?? [],
            columnRowsByDatabaseId[database.id] ?? [],
            subAccountRowsByDatabaseId[database.id] ?? [],
            themeRowsByDatabaseId[database.id] ?? [],
            userRowsByDatabaseId[database.id] ?? [],
            userGroupRowsByDatabaseId[database.id] ?? [],
          ),
        })),
    })),
  }
}

function buildFallbackTree(config: OphConnectionConfig): OphTreeNode {
  return {
    id: 'servers',
    label: 'Servers',
    kind: 'root',
    children: config.servers.map((server) => ({
      id: server.id,
      label: server.name,
      kind: 'server',
      description: `${server.host}:${server.port}`,
      status: 'offline',
      serverId: server.id,
      children: [],
    })),
  }
}

async function loadConnectionConfig(): Promise<OphConnectionConfig | null> {
  try {
    return await invoke<OphConnectionConfig | null>('load_connection_config')
  } catch {
    return null
  }
}

async function saveConnectionConfig(config: OphConnectionConfig): Promise<OphConnectionConfig> {
  try {
    return await invoke<OphConnectionConfig>('save_connection_config', { config })
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error)
    if (message.includes('__TAURI') || message.includes('invoke')) {
      throw new Error('Run the desktop app to test and save a real SQL Server connection.')
    }
    throw new Error(message)
  }
}

async function clearConnectionConfig(): Promise<void> {
  try {
    await invoke('delete_connection_config')
  } catch {
    window.localStorage.removeItem(connectionConfigKey)
  }
}

async function testConnection(server: OphServer): Promise<TestConnectionResult> {
  try {
    return await invoke<TestConnectionResult>('test_connection', { server })
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error)
    if (message.includes('__TAURI') || message.includes('invoke')) {
      return {
        success: false,
        message: 'Run the desktop app to test a real SQL Server connection.',
        serverName: server.name,
      }
    }
    throw new Error(message)
  }
}

async function listOphDatabases(config: OphConnectionConfig): Promise<OphDatabase[]> {
  try {
    return await invoke<OphDatabase[]>('list_oph_databases', { config })
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error)
    if (message.includes('__TAURI') || message.includes('invoke')) {
      throw new Error('Run the desktop app to load OPH databases from SQL Server.')
    }
    throw new Error(message)
  }
}

async function listAccountInfo(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_account_info', { config, accountId, databaseName })
}

async function listAccountDatabases(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_account_databases', { config, accountId, databaseName })
}

async function listSubAccounts(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_sub_accounts', { config, accountId, databaseName })
}

async function listUsers(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_users', { config, accountId, databaseName })
}

async function listUserGroups(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_user_groups', { config, accountId, databaseName })
}

async function listUserInfo(config: OphConnectionConfig, _accountId: string, databaseName: string, userGuid: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_user_info', { config, databaseName, userGuid })
}

async function listUserGroupModules(config: OphConnectionConfig, _accountId: string, databaseName: string, userGroupGuid: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_user_group_modules', { config, databaseName, userGroupGuid })
}

async function listModulesBySettingMode(
  config: OphConnectionConfig,
  accountId: string,
  databaseName: string,
  settingMode: number,
): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_modules', { config, accountId, databaseName, settingMode })
}

async function listModuleTree(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_module_tree', { config, accountId, databaseName })
}

async function listModuleColumnTree(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_module_column_tree', { config, accountId, databaseName })
}

async function listModuleColumns(config: OphConnectionConfig, _accountId: string, databaseName: string, moduleGuid: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_module_columns', { config, databaseName, moduleGuid })
}

async function listModuleInfo(config: OphConnectionConfig, _accountId: string, databaseName: string, moduleGuid: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_module_info', { config, databaseName, moduleGuid })
}

async function listModuleColumnInfo(config: OphConnectionConfig, _accountId: string, databaseName: string, columnGuid: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_module_column_info', { config, databaseName, columnGuid })
}

async function listChildModules(config: OphConnectionConfig, _accountId: string, databaseName: string, moduleGuid: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_child_modules', { config, databaseName, moduleGuid })
}

async function listModuleApprovals(config: OphConnectionConfig, _accountId: string, databaseName: string, moduleGuid: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_module_approvals', { config, databaseName, moduleGuid })
}

async function listModuleNumbering(config: OphConnectionConfig, _accountId: string, databaseName: string, moduleGuid: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_module_numbering', { config, databaseName, moduleGuid })
}

async function listModuleMails(config: OphConnectionConfig, _accountId: string, databaseName: string, moduleGuid: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_module_mails', { config, databaseName, moduleGuid })
}

async function listModuleStatuses(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_module_statuses', { config, accountId, databaseName })
}

async function listModuleGroups(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_module_groups', { config, accountId, databaseName })
}

async function listThemes(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_themes', { config, accountId, databaseName })
}

async function listThemePages(config: OphConnectionConfig, _accountId: string, databaseName: string, themeGuid: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_theme_pages', { config, databaseName, themeGuid })
}

async function listMenus(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_menus', { config, accountId, databaseName })
}



async function listParameters(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_parameters', { config, accountId, databaseName })
}

async function listWidgets(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_widgets', { config, accountId, databaseName })
}

async function listMailProfiles(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_mail_profiles', { config, accountId, databaseName })
}

async function listTranslatorWords(config: OphConnectionConfig, accountId: string, databaseName: string): Promise<MetadataRow[]> {
  return invoke<MetadataRow[]>('list_translator_words', { config, accountId, databaseName })
}

async function saveMetadataRow(
  config: OphConnectionConfig,
  databaseName: string,
  sourceTable: string,
  originalRow: MetadataRow,
  draftRow: MetadataRow,
  moduleGuid?: string,
  columnGuid?: string,
  themeGuid?: string,
  userGuid?: string,
  userGroupGuid?: string,
): Promise<void> {
  return invoke<void>('save_metadata_row', {
    config,
    databaseName,
    sourceTable,
    originalRow,
    draftRow,
    moduleGuid,
    columnGuid,
    themeGuid,
    userGuid,
    userGroupGuid,
  })
}

async function deleteMetadataRow(
  config: OphConnectionConfig,
  databaseName: string,
  sourceTable: string,
  originalRow: MetadataRow,
): Promise<void> {
  return invoke<void>('delete_metadata_row', { config, databaseName, sourceTable, originalRow })
}

function createSampleConnectionConfig(): OphConnectionConfig {
  return {
    servers: [servers[0]],
    selectedServerId: servers[0].id,
  }
}

export const ophAdminService = {
  loadConnectionConfig,
  saveConnectionConfig,
  createSampleConnectionConfig,
  clearConnectionConfig,
  testConnection,
  listOphDatabases,
  listAccountInfo,
  listAccountDatabases,
  listSubAccounts,
  listUsers,
  listUserGroups,
  listUserInfo,
  listUserGroupModules,
  listModuleTree,
  listModuleColumnTree,
  listModuleInfo,
  listModuleColumns,
  listModuleColumnInfo,
  listChildModules,
  listModuleApprovals,
  listModuleNumbering,
  listModuleMails,
  listModulesBySettingMode,
  listModuleStatuses,
  listModuleGroups,
  listThemes,
  listThemePages,
  listMenus,
  listTranslatorWords,
  listParameters,
  listWidgets,
  listMailProfiles,
  saveMetadataRow,
  deleteMetadataRow,
  buildTree,
  buildFallbackTree,
  listServers: () => servers,
  listDatabases: () => databases,
  listModules: () => modules,
  listValidations: () => validations,
  getModule: (moduleGuid: string) => modules.find((module) => module.moduleGuid === moduleGuid),
}
