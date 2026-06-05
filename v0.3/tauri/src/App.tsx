import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  ChevronRight,
  Database,
  FileCode2,
  Gauge,
  Mail,
  Play,
  Search,
  Server,
  Settings,
  ShieldCheck,
  Shuffle,
  Table2,
} from 'lucide-react'
import { useMemo, useState } from 'react'
import { navigationItems, ophAdminService } from './services/mockOphAdminService'
import type { NavigationItem, OphModule, OphServer, ValidationItem } from './types/domain'

type AppRoute =
  | '/dashboard'
  | '/servers'
  | '/databases'
  | '/modules'
  | '/query'
  | '/migration'
  | '/settings'

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
}

const routeIcons: Record<AppRoute, typeof Gauge> = {
  '/dashboard': Gauge,
  '/servers': Server,
  '/databases': Database,
  '/modules': Table2,
  '/query': FileCode2,
  '/migration': Shuffle,
  '/settings': Settings,
}

function getInitialRoute(): AppRoute {
  const hash = window.location.hash.replace('#', '')
  const knownRoutes = navigationItems.map((item) => item.path)
  if (hash.startsWith('/modules/')) return '/modules'
  return knownRoutes.includes(hash) ? (hash as AppRoute) : '/dashboard'
}

function getInitialModuleGuid(): string {
  const hash = window.location.hash.replace('#', '')
  return hash.startsWith('/modules/') ? hash.split('/')[2] || 'mod-invoice' : 'mod-invoice'
}

function getStatusClass(status: string): string {
  if (['online', 'healthy', 'active'].includes(status)) return 'status status-good'
  if (['warning', 'needs-review', 'review'].includes(status)) return 'status status-warning'
  return 'status status-bad'
}

function App() {
  const [route, setRoute] = useState<AppRoute>(getInitialRoute)
  const [selectedModuleGuid, setSelectedModuleGuid] = useState(getInitialModuleGuid)
  const servers = ophAdminService.listServers()
  const databases = ophAdminService.listDatabases()
  const modules = ophAdminService.listModules()
  const validations = ophAdminService.listValidations()
  const selectedModule = ophAdminService.getModule(selectedModuleGuid) ?? modules[0]

  const content = useMemo(() => {
    switch (route) {
      case '/servers':
        return <ServersPage servers={servers} />
      case '/databases':
        return <DatabasesPage />
      case '/modules':
        return (
          <ModulesPage
            modules={modules}
            selectedModule={selectedModule}
            onSelectModule={selectModule}
          />
        )
      case '/query':
        return <QueryPage />
      case '/migration':
        return <MigrationPage />
      case '/settings':
        return <SettingsPage />
      default:
        return <DashboardPage />
    }
  }, [modules, route, selectedModule, servers])

  function changeRoute(nextRoute: string) {
    setRoute(nextRoute as AppRoute)
    window.location.hash = nextRoute
  }

  function selectModule(moduleGuid: string) {
    setSelectedModuleGuid(moduleGuid)
    setRoute('/modules')
    window.location.hash = `/modules/${moduleGuid}`
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-block">
          <div className="brand-mark">OPH</div>
          <div>
            <strong>OPH Control Studio</strong>
            <span>Legacy admin workspace</span>
          </div>
        </div>
        <nav className="nav-list">
          {navigationItems.map((item) => (
            <NavButton key={item.path} item={item} active={route === item.path} onClick={changeRoute} />
          ))}
        </nav>
        <div className="sidebar-footer">
          <span>Current scope</span>
          <strong>Copy legacy features cleanly</strong>
        </div>
      </aside>

      <main className="workspace">
        <header className="topbar">
          <div className="search-box">
            <Search size={16} />
            <span>Search servers, databases, modules...</span>
          </div>
          <button className="primary-button">Test Connection</button>
        </header>
        <div className="content-grid">
          <section className="main-panel">{content}</section>
          <RightPanel validations={validations} selectedModule={selectedModule} />
        </div>
      </main>
    </div>
  )
}

function NavButton({ item, active, onClick }: { item: NavigationItem; active: boolean; onClick: (path: string) => void }) {
  const Icon = routeIcons[item.path as AppRoute]

  return (
    <button className={`nav-button ${active ? 'nav-button-active' : ''}`} onClick={() => onClick(item.path)}>
      <Icon size={18} />
      <span>
        <strong>{item.label}</strong>
        <small>{item.description}</small>
      </span>
    </button>
  )
}

function SectionHeader({ eyebrow, title, description, action }: SectionHeaderProps) {
  return (
    <div className="section-header">
      <div>
        <span className="eyebrow">{eyebrow}</span>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      {action ? <button className="primary-button">{action}</button> : null}
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

function DashboardPage() {
  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Dashboard"
        title="OPH estate overview"
        description="A compact view of legacy OPH servers, metadata health, and migration readiness."
        action="Run Validation"
      />
      <div className="metrics-grid">
        <MetricCard label="Servers" value="3" detail="2 reachable, 1 offline" />
        <MetricCard label="Databases" value="29" detail="1 core, 28 account/event" />
        <MetricCard label="Modules" value="190" detail="3 require metadata review" />
        <MetricCard label="Migration" value="12%" detail="EventDB preview coverage" />
      </div>
      <div className="two-column">
        <div className="panel-card">
          <h2>Active workbench</h2>
          <ul className="timeline-list">
            <li><CheckCircle2 size={16} />Production OPH connection passed.</li>
            <li><AlertTriangle size={16} />HR module status requires review.</li>
            <li><Activity size={16} />EventDB schema mapping is in progress.</li>
          </ul>
        </div>
        <div className="panel-card">
          <h2>Recommended next actions</h2>
          <div className="action-list">
            <button>Open module validation</button>
            <button>Review server certificates</button>
            <button>Prepare EventDB migration plan</button>
          </div>
        </div>
      </div>
    </div>
  )
}

function ServersPage({ servers }: { servers: OphServer[] }) {
  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Server Explorer"
        title="Connection manager"
        description="Add, edit, delete, test, and save OPH server connection profiles locally."
        action="Add Server"
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

function DatabasesPage() {
  const databases = ophAdminService.listDatabases()

  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Database Explorer"
        title="OPH database inventory"
        description="Detect oph_core, account databases, status, module count, and metadata entry points."
        action="Refresh"
      />
      <div className="database-grid">
        {databases.map((database) => (
          <article className="database-card" key={database.id}>
            <div className="card-title-row">
              <Database size={20} />
              <strong>{database.name}</strong>
            </div>
            <span className={getStatusClass(database.status)}>{database.status}</span>
            <dl>
              <dt>Type</dt><dd>{database.type}</dd>
              <dt>Modules</dt><dd>{database.modules}</dd>
              <dt>Size</dt><dd>{database.size}</dd>
              <dt>Updated</dt><dd>{database.updatedAt}</dd>
            </dl>
            <button className="ghost-button">Open Metadata</button>
          </article>
        ))}
      </div>
    </div>
  )
}

function ModulesPage({
  modules,
  selectedModule,
  onSelectModule,
}: {
  modules: OphModule[]
  selectedModule: OphModule
  onSelectModule: (moduleGuid: string) => void
}) {
  const columns = ophAdminService.listModuleColumns(selectedModule.moduleGuid)
  const approvals = ophAdminService.listModuleApprovals(selectedModule.moduleGuid)

  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Module Explorer"
        title="Legacy module metadata"
        description="Inspect module definitions, columns, approvals, numbering, and mail configuration."
        action="Save Module"
      />
      <div className="module-layout">
        <div className="module-list panel-card">
          <h2>Modules</h2>
          {modules.map((module) => (
            <button
              className={`module-row ${module.moduleGuid === selectedModule.moduleGuid ? 'module-row-active' : ''}`}
              key={module.moduleGuid}
              onClick={() => onSelectModule(module.moduleGuid)}
            >
              <span>
                <strong>{module.moduleId}</strong>
                <small>{module.description}</small>
              </span>
              <ChevronRight size={16} />
            </button>
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

function QueryPage() {
  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Query Studio"
        title="SQL workbench"
        description="Run focused metadata queries against selected OPH databases with clear result grids."
        action="Run Query"
      />
      <div className="query-card">
        <div className="query-toolbar"><span>oph_core / modl</span><button><Play size={14} />Execute</button></div>
        <pre>{`select moduleguid, moduleid, moduledescription\nfrom modl\norder by moduleid`}</pre>
      </div>
      <div className="empty-result">Query results will appear here after a connection is selected.</div>
    </div>
  )
}

function MigrationPage() {
  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Migration Center"
        title="Legacy MSSQL to EventDB"
        description="Plan metadata extraction, validate mappings, and track staged migration progress."
        action="Create Plan"
      />
      <div className="migration-track">
        {['Extract legacy metadata', 'Validate module model', 'Map EventDB schema', 'Dry run migration'].map((step, index) => (
          <article className="migration-step" key={step}>
            <span>{index + 1}</span>
            <strong>{step}</strong>
            <small>{index === 0 ? 'Ready' : 'Pending'}</small>
          </article>
        ))}
      </div>
    </div>
  )
}

function SettingsPage() {
  return (
    <div className="page-stack">
      <SectionHeader
        eyebrow="Settings"
        title="Local workspace preferences"
        description="Manage local configuration, connection storage behavior, and command-layer preferences."
      />
      <div className="settings-grid">
        <label><span>Encrypt local connection config</span><input type="checkbox" defaultChecked /></label>
        <label><span>Trust server certificate by default</span><input type="checkbox" /></label>
        <label><span>Prefer sqlcmd sidecar for diagnostics</span><input type="checkbox" defaultChecked /></label>
      </div>
    </div>
  )
}

function RightPanel({ validations, selectedModule }: { validations: ValidationItem[]; selectedModule: OphModule }) {
  return (
    <aside className="right-panel">
      <div className="panel-card properties-card">
        <span className="eyebrow">Properties</span>
        <h2>{selectedModule.moduleId}</h2>
        <dl>
          <dt>Module GUID</dt><dd>{selectedModule.moduleGuid}</dd>
          <dt>Columns</dt><dd>{selectedModule.columns}</dd>
          <dt>Approvals</dt><dd>{selectedModule.approvals}</dd>
          <dt>Need Login</dt><dd>{selectedModule.needLogin ? 'Yes' : 'No'}</dd>
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

export default App
