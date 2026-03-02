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
}

export interface AccessProfile {
  personnelId: number
  personnelName: string
  roleType: string
  isAdmin: boolean
  permissions: string[]
}

export interface AccessUserItem {
  personnelId: number
  personnelName: string
  department: string
  groupName: string
  roleType: string
  isAdmin: boolean
  permissionCount: number
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