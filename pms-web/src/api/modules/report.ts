import request from '../request'
import type { ApiResponse } from '../../types/project'
import type {
  WorkHoursReportRow,
  WorkHoursReportRowUpdatePayload,
  WorkHoursReportQuery,
  WorkHoursReportImportResult,
  MonthlyReportGenerateRequest,
  MonthlyReportExportQuery,
  MonthlyReportSourcePreview,
} from '../../types/report'
import type { MonthlyReportItem } from '../../types/monthly-report'

export function fetchWorkHoursReport(params: WorkHoursReportQuery) {
  return request.get<any, ApiResponse<{ total: number; rows: WorkHoursReportRow[] }>>('/reports/workhours', { params })
}

export function updateWorkHoursReportRow(id: number, data: WorkHoursReportRowUpdatePayload, reportMonth?: string) {
  return request.put<any, ApiResponse<WorkHoursReportRow>>(`/reports/workhours/${id}`, data, {
    params: reportMonth ? { reportMonth } : undefined,
  })
}

export function deleteWorkHoursReportRow(id: number, reportMonth?: string) {
  return request.delete<any, ApiResponse<any>>(`/reports/workhours/${id}`, {
    params: reportMonth ? { reportMonth } : undefined,
  })
}

export function addWorkHoursReportRow(data: WorkHoursReportRowUpdatePayload, reportMonth?: string) {
  return request.post<any, ApiResponse<WorkHoursReportRow>>('/reports/workhours/row', data, {
    params: reportMonth ? { reportMonth } : undefined,
  })
}

export function importWorkHoursReport(file: File, reportMonth?: string, autoCalculate = true) {
  const formData = new FormData()
  formData.append('file', file)
  return request.post<any, ApiResponse<WorkHoursReportImportResult>>('/reports/workhours/import', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    params: { reportMonth, autoCalculate },
  })
}

export function generateMonthlyReport(data: MonthlyReportGenerateRequest) {
  return request.post<any, ApiResponse<MonthlyReportItem>>('/reports/monthly/generate', data)
}

export function fetchMonthlyReportSourcePreview(params: { reportMonth: string; groupName?: string; supervisorName?: string }) {
  return request.get<any, ApiResponse<MonthlyReportSourcePreview>>('/reports/monthly/source-preview', { params })
}

export function exportWorkHoursReport(params: WorkHoursReportQuery) {
  return request.get<any, Blob>('/reports/workhours/export', {
    params,
    responseType: 'blob',
  })
}

export function regenerateWorkHoursReport(reportMonth?: string) {
  return request.post<any, ApiResponse<{ reportMonth: string; total: number }>>('/reports/workhours/regenerate', null, {
    params: reportMonth ? { reportMonth } : undefined,
  })
}

export function exportMonthlyReport(params: MonthlyReportExportQuery) {
  return request.get<any, Blob>('/reports/monthly/export', {
    params,
    responseType: 'blob',
  })
}
