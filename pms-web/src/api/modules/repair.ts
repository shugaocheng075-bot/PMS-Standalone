import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type {
  RepairRecordAcceptRequest,
  RepairRecordItem,
  RepairRecordQuery,
  RepairRecordReopenRequest,
  RepairRecordResolveRequest,
  RepairRecordSummary,
  RepairRecordUpsert,
} from '../../types/repair'

export function fetchRepairSummary() {
  return request.get<any, ApiResponse<RepairRecordSummary>>('/repair-records/summary')
}

export function fetchRepairRecords(params: RepairRecordQuery) {
  return request.get<any, ApiResponse<PagedResult<RepairRecordItem>>>('/repair-records', { params })
}

export function fetchRepairRecordById(id: number) {
  return request.get<any, ApiResponse<RepairRecordItem>>(`/repair-records/${id}`)
}

export function createRepairRecord(data: RepairRecordUpsert) {
  return request.post<any, ApiResponse<RepairRecordItem>>('/repair-records', data)
}

export function updateRepairRecord(id: number, data: RepairRecordUpsert) {
  return request.put<any, ApiResponse<RepairRecordItem>>(`/repair-records/${id}`, data)
}

export function acceptRepairRecord(id: number, data: RepairRecordAcceptRequest = {}) {
  return request.patch<any, ApiResponse<RepairRecordItem>>(`/repair-records/${id}/accept`, data)
}

export function resolveRepairRecord(id: number, data: RepairRecordResolveRequest) {
  return request.patch<any, ApiResponse<RepairRecordItem>>(`/repair-records/${id}/resolve`, data)
}

export function reopenRepairRecord(id: number, data: RepairRecordReopenRequest = {}) {
  return request.patch<any, ApiResponse<RepairRecordItem>>(`/repair-records/${id}/reopen`, data)
}

export function deleteRepairRecord(id: number) {
  return request.delete<any, ApiResponse<any>>(`/repair-records/${id}`)
}

export function exportRepairRecords(params: RepairRecordQuery) {
  return request.get<any, Blob>('/repair-records/export', {
    params,
    responseType: 'blob',
  })
}
