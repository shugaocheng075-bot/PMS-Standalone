import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { WorkHoursItem, WorkHoursQuery, WorkHoursSummary, WorkHoursUpsert } from '../../types/workhours'

export function fetchWorkHoursSummary() {
  return request.get<any, ApiResponse<WorkHoursSummary>>('/workhours/summary')
}

export function fetchWorkHours(params: WorkHoursQuery) {
  return request.get<any, ApiResponse<PagedResult<WorkHoursItem>>>('/workhours', { params })
}

export function fetchWorkHoursById(id: number) {
  return request.get<any, ApiResponse<WorkHoursItem>>(`/workhours/${id}`)
}

export function createWorkHours(data: WorkHoursUpsert) {
  return request.post<any, ApiResponse<WorkHoursItem>>('/workhours', data)
}

export function updateWorkHours(id: number, data: WorkHoursUpsert) {
  return request.put<any, ApiResponse<WorkHoursItem>>(`/workhours/${id}`, data)
}

export function deleteWorkHours(id: number) {
  return request.delete<any, ApiResponse<any>>(`/workhours/${id}`)
}
