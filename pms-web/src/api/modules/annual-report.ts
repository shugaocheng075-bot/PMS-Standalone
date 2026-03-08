import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { AnnualReportItem, AnnualReportQuery, AnnualReportSummary, AnnualReportUpsert } from '../../types/annual-report'

export function fetchAnnualReportSummary() {
  return request.get<any, ApiResponse<AnnualReportSummary>>('/annual-reports/summary')
}

export function fetchAnnualReportList(params: AnnualReportQuery) {
  return request.get<any, ApiResponse<PagedResult<AnnualReportItem>>>('/annual-reports', {
    params,
  })
}

export function updateAnnualReport(id: number, payload: Partial<AnnualReportUpsert>) {
  return request.put<any, ApiResponse<AnnualReportItem>>(`/annual-reports/${id}`, payload)
}

export function exportAnnualReports(params: AnnualReportQuery) {
  return request.get<any, Blob>('/annual-reports/export', {
    params,
    responseType: 'blob',
  })
}
