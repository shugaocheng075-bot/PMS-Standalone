export interface PermissionDefinition {
  key: string
  name: string
  module: string
  description: string
}

export interface PersonnelActor {
  personnelId: number
  personnelName: string
  roleType: string
  systemRole: string
}

export interface DataScope {
  scopeType: 'own' | 'subordinates' | 'all'
  accessiblePersonnelNames: string[]
  accessibleHospitalNames: string[]
}

export interface AccessProfile {
  personnelId: number
  personnelName: string
  roleType: string
  systemRole: string
  isAdmin: boolean
  permissions: string[]
  supervisorId?: number
  supervisorName?: string
  dataScope?: DataScope
}

export interface AccessUserItem {
  personnelId: number
  personnelName: string
  department: string
  groupName: string
  roleType: string
  systemRole: string
  isAdmin: boolean
  permissionCount: number
  supervisorId?: number
  supervisorName?: string
  updatedAt: string
}

export interface AccessUserQuery {
  name?: string
  page?: number
  size?: number
}

export interface UpdateUserPermissionPayload {
  permissionKeys: string[]
  isAdmin?: boolean
}

export interface SetSystemRolePayload {
  systemRole: string
}

export interface SetSupervisorPayload {
  supervisorId: number | null
}