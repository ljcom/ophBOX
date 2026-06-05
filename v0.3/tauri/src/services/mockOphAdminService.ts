import type {
  OphConnectionConfig,
  OphApproval,
  OphDatabase,
  OphModule,
  OphModuleColumn,
  OphServer,
  OphTreeNode,
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

function buildDatabaseChildren(database: OphDatabase): OphTreeNode[] {
  return [
    {
      id: `${database.id}:modules`,
      label: 'Modules',
      kind: 'modules',
      databaseId: database.id,
      children: [
        { id: `${database.id}:modules:core`, label: 'Core', kind: 'module-category', databaseId: database.id },
        { id: `${database.id}:modules:master`, label: 'Master', kind: 'module-category', databaseId: database.id },
        { id: `${database.id}:modules:transaction`, label: 'Transaction', kind: 'module-category', databaseId: database.id },
        { id: `${database.id}:modules:report`, label: 'Report', kind: 'module-category', databaseId: database.id },
        { id: `${database.id}:modules:blank`, label: 'Blank', kind: 'module-category', databaseId: database.id },
        { id: `${database.id}:modules:view`, label: 'View', kind: 'module-category', databaseId: database.id },
        { id: `${database.id}:modules:status`, label: 'Module Status', kind: 'module-category', databaseId: database.id },
        { id: `${database.id}:modules:groups`, label: 'Module Groups', kind: 'module-category', databaseId: database.id },
      ],
    },
    {
      id: `${database.id}:security`,
      label: 'Security',
      kind: 'security',
      databaseId: database.id,
      children: [
        { id: `${database.id}:security:users`, label: 'Users', kind: 'security', databaseId: database.id },
        { id: `${database.id}:security:user-groups`, label: 'User Groups', kind: 'security', databaseId: database.id },
      ],
    },
    {
      id: `${database.id}:interface`,
      label: 'Interface',
      kind: 'interface',
      databaseId: database.id,
      children: [
        { id: `${database.id}:interface:themes`, label: 'Themes', kind: 'interface', databaseId: database.id },
        { id: `${database.id}:interface:menus`, label: 'Menus', kind: 'interface', databaseId: database.id },
        { id: `${database.id}:interface:translator`, label: 'Translator', kind: 'interface', databaseId: database.id },
      ],
    },
    {
      id: `${database.id}:account`,
      label: 'Account',
      kind: 'account',
      databaseId: database.id,
      children: [
        { id: `${database.id}:account:sub-accounts`, label: 'Sub Accounts', kind: 'account', databaseId: database.id },
        { id: `${database.id}:account:databases`, label: 'Databases', kind: 'account', databaseId: database.id },
        { id: `${database.id}:account:parameters`, label: 'Parameters', kind: 'account', databaseId: database.id },
        { id: `${database.id}:account:widgets`, label: 'Widgets', kind: 'account', databaseId: database.id },
        { id: `${database.id}:account:mail`, label: 'Mail', kind: 'account', databaseId: database.id },
      ],
    },
  ]
}

function buildTree(config: OphConnectionConfig): OphTreeNode {
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
      children: databases
        .filter((database) => database.serverId === server.id)
        .map((database) => ({
          id: database.id,
          label: database.name,
          kind: 'database',
          description: database.type === 'core' ? 'OPH core database' : 'Account database from oph_core',
          status: database.status,
          serverId: server.id,
          databaseId: database.id,
          children: buildDatabaseChildren(database),
        })),
    })),
  }
}

function loadConnectionConfig(): OphConnectionConfig | null {
  const rawConfig = window.localStorage.getItem(connectionConfigKey)
  if (!rawConfig) return null

  try {
    const config = JSON.parse(rawConfig) as OphConnectionConfig
    return config.servers.length > 0 ? config : null
  } catch {
    return null
  }
}

function saveConnectionConfig(config: OphConnectionConfig): OphConnectionConfig {
  window.localStorage.setItem(connectionConfigKey, JSON.stringify(config))
  return config
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
  clearConnectionConfig: () => window.localStorage.removeItem(connectionConfigKey),
  buildTree,
  listServers: () => servers,
  listDatabases: () => databases,
  listModules: () => modules,
  listModuleColumns: (moduleGuid: string) => columns.filter((column) => column.moduleGuid === moduleGuid),
  listModuleApprovals: (moduleGuid: string) => approvals.filter((approval) => approval.moduleGuid === moduleGuid),
  listValidations: () => validations,
  getModule: (moduleGuid: string) => modules.find((module) => module.moduleGuid === moduleGuid),
}
