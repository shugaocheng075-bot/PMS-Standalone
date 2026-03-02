import request from '../request'
import type { ApiResponse } from '../../types/project'

export function runAutoImport() {
  return request.post<any, ApiResponse<any>>('/admin/import/excel-auto')
}

export function runCleanup() {
  return request.post<any, ApiResponse<any>>('/admin/import/cleanup')
}

export function fetchOwnershipAudit() {
  return request.get<any, ApiResponse<{ total: number; items: Array<{ hospitalName: string; productName: string; groupName: string; hospitalLevel: string; province: string; itemCount: number; amount: number }> }>>('/admin/import/ownership-audit')
}

export function reassignOwnership(data: { hospitalName: string; productName: string; groupName: string }) {
  return request.post<any, ApiResponse<any>>('/admin/import/ownership/reassign', data)
}
