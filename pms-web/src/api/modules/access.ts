import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type {
  AccessProfile,
  AccessUserItem,
  AccessUserQuery,
  PermissionDefinition,
  PersonnelActor,
  UpdateUserPermissionPayload,
} from '../../types/access'

export function fetchCurrentAccessProfile() {
  return request.get<any, ApiResponse<AccessProfile>>('/access/me')
}

export function fetchAccessActors() {
  return request.get<any, ApiResponse<PersonnelActor[]>>('/access/actors')
}

export function fetchPermissionCatalog() {
  return request.get<any, ApiResponse<PermissionDefinition[]>>('/access/permissions')
}

export function fetchAccessUsers(params: AccessUserQuery) {
  return request.get<any, ApiResponse<PagedResult<AccessUserItem>>>('/access/users', { params })
}

export function fetchUserAccessProfile(personnelId: number) {
  return request.get<any, ApiResponse<AccessProfile>>(`/access/users/${personnelId}`)
}

export function updateUserPermissions(personnelId: number, data: UpdateUserPermissionPayload) {
  return request.put<any, ApiResponse<AccessProfile>>(`/access/users/${personnelId}/permissions`, data)
}