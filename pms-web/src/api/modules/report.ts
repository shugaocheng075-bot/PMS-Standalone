import request from '../request'
import type { ApiResponse } from '../../types/project'
import type {
  WorkHoursReportRow,
  WorkHoursReportQuery,
  MonthlyReportGenerateRequest,
  MonthlyReportExportQuery,
  MonthlyReportSourcePreview,
} from '../../types/report'
import type { MonthlyReportItem } from '../../types/monthly-report'

export function fetchWorkHoursReport(params: WorkHoursReportQuery) {
  return request.get<any, ApiResponse<{ total: number; rows: WorkHoursReportRow[] }>>('/reports/workhours', { params })
}

export function generateMonthlyReport(data: MonthlyReportGenerateRequest) {
  return request.post<any, ApiResponse<MonthlyReportItem>>('/reports/monthly/generate', data)
}

export function fetchMonthlyReportSourcePreview(params: { reportMonth: string; groupName: string }) {
  return request.get<any, ApiResponse<MonthlyReportSourcePreview>>('/reports/monthly/source-preview', { params })
}

export function exportWorkHoursReport(params: WorkHoursReportQuery) {
  return request.get<any, Blob>('/reports/workhours/export', {
    params,
    responseType: 'blob',
  })
}

export function exportMonthlyReport(params: MonthlyReportExportQuery) {
  return request.get<any, Blob>('/reports/monthly/export', {
    params,
    responseType: 'blob',
  })
}
