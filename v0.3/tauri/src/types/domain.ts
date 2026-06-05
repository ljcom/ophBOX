export type AuthType = 'sql' | 'windows'

export type OphServer = {
  id: string
  name: string
  host: string
  port: number
  authType: AuthType
  username?: string
  password?: string
  trustServerCertificate?: boolean
  encrypt?: boolean
  status: 'online' | 'warning' | 'offline'
  databases: number
  lastChecked: string
}

export type OphDatabase = {
  id: string
  name: string
  serverId: string
  type: 'core' | 'account' | 'eventdb' | 'unknown'
  status: 'healthy' | 'needs-review' | 'offline'
  modules: number
  size: string
  updatedAt: string
}

export type OphModule = {
  moduleGuid: string
  accountGuid: string
  moduleId: string
  description: string
  settingMode: string
  accountDbGuid: string
  parentModuleGuid?: string
  orderNo: number
  needLogin: boolean
  themePageGuid?: string
  moduleStatusGuid: string
  moduleGroupGuid: string
  columns: number
  approvals: number
  numbering: string
}

export type OphModuleColumn = {
  columnGuid: string
  moduleGuid: string
  colKey: string
  colType: string
  titleCaption: string
  colOrder: number
  colLength: number
}

export type OphApproval = {
  approvalGuid: string
  moduleGuid: string
  approvalGroupGuid: string
  upperGroupGuid?: string
  level: number
  sqlFilter: string
  zoneGroup: string
}

export type ValidationItem = {
  id: string
  severity: 'info' | 'warning' | 'error'
  title: string
  detail: string
}

export type NavigationItem = {
  path: string
  label: string
  description: string
}
