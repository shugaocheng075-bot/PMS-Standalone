import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { InspectionPlanItem, InspectionQuery, InspectionSummary } from '../../types/inspection'

export function fetchInspectionSummary() {
  return request.get<any, ApiResponse<InspectionSummary>>('/inspections/summary')
}

export function fetchInspections(params: InspectionQuery) {
  return request.get<any, ApiResponse<PagedResult<InspectionPlanItem>>>('/inspections', {
    params,
  })
}
