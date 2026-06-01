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

export function syncProjectLedger(file: File, sheetName?: string, includeGroupName?: string) {
  const formData = new FormData()
  formData.append('file', file)
  if (sheetName) formData.append('sheetName', sheetName)
  if (includeGroupName) formData.append('includeGroupName', includeGroupName)
  return request.post<any, ApiResponse<{
    fileName: string
    sheetName: string
    includeGroupName: string
    sourceRowCount: number
    existingProjectCount: number
    matchedProjectCount: number
    updatedProjectCount: number
    unchangedMatchedProjectCount: number
    addedProjectCount: number
    preservedUnmatchedProjectCount: number
    skippedSourceRowCount: number
    finalProjectCount: number
    message: string
  }>>('/admin/import/upload/sync-project-ledger', formData)
}

export function uploadMajorDemand(file: File, sheetName?: string) {
  const formData = new FormData()
  formData.append('file', file)
  if (sheetName) formData.append('sheetName', sheetName)
  return request.post<any, ApiResponse<{ fileName: string; sheetName: string; importedColumnCount: number; importedRowCount: number; message: string }>>('/admin/import/upload/major-demand', formData)
}

export function downloadImportTemplate(type: 'project-ledger' | 'major-demand' | 'workhours') {
  return request.get<any, Blob>(`/admin/import/template/${type}`, { responseType: 'blob' })
}

export function validateProjectLedger(file: File, sheetName?: string) {
  const formData = new FormData()
  formData.append('file', file)
  if (sheetName) formData.append('sheetName', sheetName)
  return request.post<any, ApiResponse<{
    valid: boolean
    totalRows: number
    errors: string[]
    warnings: string[]
    preview: Array<{ hospitalName: string; productName: string; groupName: string; maintenancePersonName: string; opportunityNumber: string }>
  }>>('/admin/import/validate/project-ledger', formData)
}

export function validateWorkHours(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return request.post<any, ApiResponse<{
    valid: boolean
    totalRows: number
    errors: string[]
    warnings: string[]
    preview: Array<{ opportunityNumber: string; hospitalName: string; productName: string; implementationStatus: string; workHoursManDays: number }>
  }>>('/admin/import/validate/workhours', formData)
}
