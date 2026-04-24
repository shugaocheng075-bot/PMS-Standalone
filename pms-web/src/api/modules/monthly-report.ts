import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type {
  MonthlyReportApprovalPayload,
  MonthlyReportItem,
  MonthlyReportQuery,
  MonthlyReportUpsert,
} from '../../types/monthly-report'

export function fetchMonthlyReports(params: MonthlyReportQuery) {
  return request.get<any, ApiResponse<PagedResult<MonthlyReportItem>>>('/monthly-reports', { params })
}

export function fetchMonthlyReportById(id: number) {
  return request.get<any, ApiResponse<MonthlyReportItem>>(`/monthly-reports/${id}`)
}

export function createMonthlyReport(data: MonthlyReportUpsert) {
  return request.post<any, ApiResponse<MonthlyReportItem>>('/monthly-reports', data)
}

export function updateMonthlyReport(id: number, data: MonthlyReportUpsert) {
  return request.put<any, ApiResponse<MonthlyReportItem>>(`/monthly-reports/${id}`, data)
}

export function submitMonthlyReport(id: number) {
  return request.patch<any, ApiResponse<MonthlyReportItem>>(`/monthly-reports/${id}/submit`)
}

export function approveMonthlyReport(id: number) {
  return request.patch<any, ApiResponse<MonthlyReportItem>>(`/monthly-reports/${id}/approve`)
}

export function rejectMonthlyReport(id: number, data: MonthlyReportApprovalPayload) {
  return request.patch<any, ApiResponse<MonthlyReportItem>>(`/monthly-reports/${id}/reject`, data)
}

export function deleteMonthlyReport(id: number) {
  return request.delete<any, ApiResponse<any>>(`/monthly-reports/${id}`)
}
