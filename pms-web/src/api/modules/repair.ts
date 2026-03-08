import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { RepairRecordItem, RepairRecordQuery, RepairRecordSummary, RepairRecordUpsert } from '../../types/repair'

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

export function deleteRepairRecord(id: number) {
  return request.delete<any, ApiResponse<any>>(`/repair-records/${id}`)
}

export function exportRepairRecords(params: RepairRecordQuery) {
  return request.get<any, Blob>('/repair-records/export', {
    params,
    responseType: 'blob',
  })
}
