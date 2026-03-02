import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { PersonnelItem, PersonnelQuery, PersonnelSummary, PersonnelUpsert } from '../../types/personnel'

export function fetchPersonnelSummary() {
  return request.get<any, ApiResponse<PersonnelSummary>>('/personnel/summary')
}

export function fetchPersonnel(params: PersonnelQuery) {
  return request.get<any, ApiResponse<PagedResult<PersonnelItem>>>('/personnel', { params })
}

export function fetchPersonnelById(id: number) {
  return request.get<any, ApiResponse<PersonnelItem>>(`/personnel/${id}`)
}

export function createPersonnel(data: PersonnelUpsert) {
  return request.post<any, ApiResponse<PersonnelItem>>('/personnel', data)
}

export function updatePersonnel(id: number, data: PersonnelUpsert) {
  return request.put<any, ApiResponse<PersonnelItem>>(`/personnel/${id}`, data)
}

export function deletePersonnel(id: number) {
  return request.delete<any, { code: number; message: string }>(`/personnel/${id}`)
}
