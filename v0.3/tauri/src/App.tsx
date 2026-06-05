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
  Mail,
  MonitorCog,
  Play,
  Search,
  Server,
  Settings,
  ShieldCheck,
  Table2,
  UserRoundCog,
} from 'lucide-react'
import { FormEvent, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { ophAdminService } from './services/mockOphAdminService'
import type {
  OphConnectionConfig,
  OphModule,
  OphServer,
  OphTreeNode,
  TreeNodeKind,
  ValidationItem,
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
  security: ShieldCheck,
  interface: MonitorCog,
  account: UserRoundCog,
}

function App() {
  const [connectionConfig, setConnectionConfig] = useState<OphConnectionConfig | null>(() =>
    ophAdminService.loadConnectionConfig(),
  )
  const tree = useMemo(
    () => (connectionConfig ? ophAdminService.buildTree(connectionConfig) : null),
    [connectionConfig],
  )
  const firstDatabase = tree?.children?.[0]?.children?.[0]
  const [selection, setSelection] = useState<WorkspaceSelection>(() => ({
    id: firstDatabase?.id ?? 'servers',
    label: firstDatabase?.label ?? 'Servers',
    kind: firstDatabase?.kind ?? 'root',
    description: firstDatabase?.description,
    databaseId: firstDatabase?.databaseId,
    serverId: firstDatabase?.serverId,
  }))

  const servers = connectionConfig?.servers ?? []
  const modules = ophAdminService.listModules()
  const validations = ophAdminService.listValidations()
  const selectedModule = modules[0]

  function saveConnection(config: OphConnectionConfig) {
    const savedConfig = ophAdminService.saveConnectionConfig(config)
    const savedTree = ophAdminService.buildTree(savedConfig)
    const savedDatabase = savedTree.children?.[0]?.children?.[0]
    setConnectionConfig(savedConfig)
    setSelection({
      id: savedDatabase?.id ?? 'servers',
      label: savedDatabase?.label ?? 'Servers',
      kind: savedDatabase?.kind ?? 'root',
      description: savedDatabase?.description,
      databaseId: savedDatabase?.databaseId,
      serverId: savedDatabase?.serverId,
    })
  }

  function resetConnection() {
    ophAdminService.clearConnectionConfig()
    setConnectionConfig(null)
  }

  if (!connectionConfig || !tree) {
    return <AddConnectionScreen onSave={saveConnection} />
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
        <div className="tree-toolbar">
          <span>Saved Config</span>
          <button onClick={resetConnection}>Reset</button>
        </div>
        <TreeView root={tree} selectionId={selection.id} onSelect={setSelection} />
        <div className="sidebar-footer">
          <span>Navigation flow</span>
          <strong>Server → Database → Domain groups</strong>
        </div>
      </aside>

      <main className="workspace">
        <header className="topbar">
          <div className="search-box">
            <Search size={16} />
            <span>Search current OPH connection...</span>
          </div>
          <button className="primary-button">Test Connection</button>
        </header>
        <div className="content-grid">
          <section className="main-panel">
            <Workspace selection={selection} servers={servers} selectedModule={selectedModule} />
          </section>
          <RightPanel validations={validations} selectedModule={selectedModule} selection={selection} />
        </div>
      </main>
    </div>
  )
}

function AddConnectionScreen({ onSave }: { onSave: (config: OphConnectionConfig) => void }) {
  const [authType, setAuthType] = useState<'sql' | 'windows'>('sql')

  function submitConnection(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const server: OphServer = {
      id: 'srv-user-config',
      name: String(form.get('name') || 'Production OPH'),
      host: String(form.get('host') || 'localhost'),
      port: Number(form.get('port') || 1433),
      authType,
      username: authType === 'sql' ? String(form.get('username') || '') : undefined,
      password: authType === 'sql' ? String(form.get('password') || '') : undefined,
      trustServerCertificate: form.get('trustServerCertificate') === 'on',
      encrypt: form.get('encrypt') === 'on',
      status: 'online',
      databases: 1,
      lastChecked: 'Just now',
    }

    onSave({ servers: [server], selectedServerId: server.id })
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
              Save Connection
            </button>
            <button className="ghost-button" type="button" onClick={() => onSave(ophAdminService.createSampleConnectionConfig())}>
              Use Sample Config
            </button>
          </div>
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
  const [expanded, setExpanded] = useState(depth < 3)
  const hasChildren = Boolean(node.children?.length)
  const Icon = treeIcons[node.kind]

  function selectNode() {
    onSelect({
      id: node.id,
      label: node.label,
      kind: node.kind,
      description: node.description,
      databaseId: node.databaseId,
      serverId: node.serverId,
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

function Workspace({
  selection,
  servers,
  selectedModule,
}: {
  selection: WorkspaceSelection
  servers: OphServer[]
  selectedModule: OphModule
}) {
  if (selection.kind === 'server' || selection.kind === 'root') {
    return <ServersPage servers={servers} />
  }

  if (selection.kind === 'database') {
    return <DatabaseWorkspace selection={selection} />
  }

  if (selection.kind === 'modules' || selection.kind === 'module-category') {
    return <ModulesWorkspace selection={selection} selectedModule={selectedModule} />
  }

  if (selection.kind === 'security') {
    return <DomainWorkspace icon={<ShieldCheck size={22} />} selection={selection} title="Security" />
  }

  if (selection.kind === 'interface') {
    return <DomainWorkspace icon={<MonitorCog size={22} />} selection={selection} title="Interface" />
  }

  return <DomainWorkspace icon={<UserRoundCog size={22} />} selection={selection} title="Account" />
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

function DatabaseWorkspace({ selection }: { selection: WorkspaceSelection }) {
  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Database"
        title={selection.label}
        description="This database was listed from OPH core account metadata. Domain groups are available under this database node."
        action="Refresh Database"
      />
      <div className="metrics-grid">
        <MetricCard label="Modules" value="128" detail="Grouped by setting mode" />
        <MetricCard label="Security" value="42" detail="Users and groups" />
        <MetricCard label="Interface" value="18" detail="Themes, menus, translator" />
        <MetricCard label="Account" value="5" detail="Parameters and mail" />
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

function ModulesWorkspace({ selection, selectedModule }: { selection: WorkspaceSelection; selectedModule: OphModule }) {
  const modules = ophAdminService.listModules()
  const columns = ophAdminService.listModuleColumns(selectedModule.moduleGuid)
  const approvals = ophAdminService.listModuleApprovals(selectedModule.moduleGuid)

  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Modules"
        title={selection.label}
        description="Module groups follow the legacy OPH tree while keeping the workspace clean and editable."
        action="Save Module"
      />
      <div className="module-layout">
        <div className="module-list panel-card">
          <h2>Module List</h2>
          {modules.map((module) => (
            <div className="module-row" key={module.moduleGuid}>
              <span>
                <strong>{module.moduleId}</strong>
                <small>{module.description}</small>
              </span>
              <ChevronRight size={16} />
            </div>
          ))}
        </div>
        <div className="module-detail">
          <div className="panel-card">
            <div className="detail-header">
              <div>
                <h2>{selectedModule.moduleId}</h2>
                <p>{selectedModule.description}</p>
              </div>
              <span className={getStatusClass(selectedModule.moduleStatusGuid)}>{selectedModule.moduleStatusGuid}</span>
            </div>
            <div className="form-grid">
              <label>Setting Mode<input value={selectedModule.settingMode} readOnly /></label>
              <label>Module Group<input value={selectedModule.moduleGroupGuid} readOnly /></label>
              <label>Order Number<input value={selectedModule.orderNo} readOnly /></label>
              <label>Need Login<input value={selectedModule.needLogin ? 'Yes' : 'No'} readOnly /></label>
              <label>Numbering<input value={selectedModule.numbering} readOnly /></label>
              <label>Account DB<input value={selectedModule.accountDbGuid} readOnly /></label>
            </div>
          </div>
          <div className="tabs-card">
            <div className="tabs"><button className="tab-active">Columns</button><button>Approvals</button><button>Numbering</button><button>Mail</button></div>
            <table>
              <thead><tr><th>Column Key</th><th>Type</th><th>Caption</th><th>Order</th><th>Length</th></tr></thead>
              <tbody>
                {columns.map((column) => (
                  <tr key={column.columnGuid}>
                    <td><strong>{column.colKey}</strong></td>
                    <td>{column.colType}</td>
                    <td>{column.titleCaption}</td>
                    <td>{column.colOrder}</td>
                    <td>{column.colLength}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="approval-strip">
              {approvals.map((approval) => (
                <span key={approval.approvalGuid}><ShieldCheck size={14} />L{approval.level} {approval.approvalGroupGuid}</span>
              ))}
            </div>
          </div>
        </div>
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

function RightPanel({
  validations,
  selectedModule,
  selection,
}: {
  validations: ValidationItem[]
  selectedModule: OphModule
  selection: WorkspaceSelection
}) {
  return (
    <aside className="right-panel">
      <div className="panel-card properties-card">
        <span className="eyebrow">Selection</span>
        <h2>{selection.label}</h2>
        <dl>
          <dt>Type</dt><dd>{selection.kind}</dd>
          <dt>Database</dt><dd>{selection.databaseId ?? '-'}</dd>
          <dt>Server</dt><dd>{selection.serverId ?? '-'}</dd>
          <dt>Sample Module</dt><dd>{selectedModule.moduleId}</dd>
        </dl>
      </div>
      <div className="panel-card validation-card">
        <span className="eyebrow">Validation</span>
        {validations.map((item) => (
          <div className={`validation-item validation-${item.severity}`} key={item.id}>
            <strong>{item.title}</strong>
            <p>{item.detail}</p>
          </div>
        ))}
      </div>
      <div className="panel-card action-card">
        <button>Validate</button>
        <button>Export Metadata</button>
        <button><Mail size={14} />Open Mail Templates</button>
      </div>
    </aside>
  )
}

function getStatusClass(status: string): string {
  if (['online', 'healthy', 'active'].includes(status)) return 'status status-good'
  if (['warning', 'needs-review', 'review'].includes(status)) return 'status status-warning'
  return 'status status-bad'
}

export default App
