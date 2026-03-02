import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { AnnualReportItem, AnnualReportQuery, AnnualReportSummary } from '../../types/annual-report'

export function fetchAnnualReportSummary() {
  return request.get<any, ApiResponse<AnnualReportSummary>>('/annual-reports/summary')
}

export function fetchAnnualReportList(params: AnnualReportQuery) {
  return request.get<any, ApiResponse<PagedResult<AnnualReportItem>>>('/annual-reports', {
    params,
  })
}
