import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type {
  AccessProfile,
  AccessUserItem,
  AccessUserQuery,
  DataScope,
  PermissionDefinition,
  PersonnelActor,
  SetSupervisorPayload,
  SetSystemRolePayload,
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

export function setUserSystemRole(personnelId: number, data: SetSystemRolePayload) {
  return request.put<any, ApiResponse<AccessProfile>>(`/access/users/${personnelId}/role`, data)
}

export function setUserSupervisor(personnelId: number, data: SetSupervisorPayload) {
  return request.put<any, ApiResponse<AccessProfile>>(`/access/users/${personnelId}/supervisor`, data)
}

export function fetchDataScope() {
  return request.get<any, ApiResponse<DataScope>>('/access/data-scope')
}

export function fetchHospitalScope(personnelId: number) {
  return request.get<any, ApiResponse<string[]>>(`/access/users/${personnelId}/hospital-scope`)
}

export function updateHospitalScope(personnelId: number, hospitalNames: string[]) {
  return request.put<any, ApiResponse<AccessProfile>>(`/access/users/${personnelId}/hospital-scope`, {
    hospitalNames,
  })
}