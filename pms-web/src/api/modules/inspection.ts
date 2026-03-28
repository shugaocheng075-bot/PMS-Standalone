import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type {
  InspectionPlanItem,
  InspectionPlanUpsert,
  InspectionQuery,
  InspectionResult,
  InspectionRiskItem,
  InspectionResultQuery,
  InspectionSummary,
} from '../../types/inspection'

export function fetchInspectionSummary() {
  return request.get<any, ApiResponse<InspectionSummary>>('/inspections/summary')
}

export function fetchInspections(params: InspectionQuery) {
  return request.get<any, ApiResponse<PagedResult<InspectionPlanItem>>>('/inspections', {
    params,
  })
}

export function exportInspections(params: InspectionQuery) {
  return request.get<any, Blob>('/inspections/export', {
    params,
    responseType: 'blob',
  })
}

export function updateInspection(id: number, payload: InspectionPlanUpsert) {
  return request.put<any, ApiResponse<InspectionPlanItem>>(`/inspections/${id}`, payload)
}

export function createInspection(payload: InspectionPlanUpsert) {
  return request.post<any, ApiResponse<InspectionPlanItem>>('/inspections', payload)
}

export function deleteInspection(id: number) {
  return request.delete<any, ApiResponse<boolean>>(`/inspections/${id}`)
}

// ---- 巡检结果（由 SystemAuditTool 推送） ----

export function fetchInspectionResults(params: InspectionResultQuery) {
  return request.get<any, ApiResponse<PagedResult<InspectionResult>>>('/inspections/results', {
    params,
  })
}

export function fetchLatestInspectionResult(hospitalName: string, productName: string) {
  return request.get<any, ApiResponse<InspectionResult>>('/inspections/results/latest', {
    params: { hospitalName, productName },
  })
}

export interface InspectionResultSubmitItem {
  hospitalName: string
  productName: string
  inspectedAt: string
  inspector: string
  success: boolean
  errorMessage?: string | null
  durationSeconds: number
  riskCount: number
  warningCount: number
  criticalCount: number
  healthLevel: string
  overallScore: number
  databaseVersion?: string | null
  storageUsedPercent?: number | null
  tablespaceUsedPercent?: number | null
  backupStatus?: string | null
  daysToFull?: number | null
  topRisks?: InspectionRiskItem[]
}

export function submitInspectionResults(results: InspectionResultSubmitItem[]) {
  return request.post<any, ApiResponse<{ message: string }>>('/inspections/results/batch', results)
}
