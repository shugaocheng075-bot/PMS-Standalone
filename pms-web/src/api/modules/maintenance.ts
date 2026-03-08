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

export function uploadProjectLedger(file: File, sheetName?: string) {
  const formData = new FormData()
  formData.append('file', file)
  if (sheetName) formData.append('sheetName', sheetName)
  return request.post<any, ApiResponse<{ fileName: string; sheetName: string; importedRowCount: number; message: string }>>('/admin/import/upload/project-ledger', formData)
}

export function uploadMajorDemand(file: File, sheetName?: string) {
  const formData = new FormData()
  formData.append('file', file)
  if (sheetName) formData.append('sheetName', sheetName)
  return request.post<any, ApiResponse<{ fileName: string; sheetName: string; importedColumnCount: number; importedRowCount: number; message: string }>>('/admin/import/upload/major-demand', formData)
}
