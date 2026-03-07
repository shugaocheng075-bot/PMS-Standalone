import request from '../request'
import type { ApiResponse } from '../../types/project'
import type { DashboardV3Data } from '../../types/dashboard-kpi'

export type DashboardV2Query = {
  source?: string
  level?: string
  keyword?: string
  owner?: string
  months?: number
}

export type DashboardV2Summary = {
  total: number
  severe: number
  warning: number
  reminder: number
}

export type DashboardV2TrendItem = {
  month: string
  total: number
  severe: number
  warning: number
  reminder: number
  contract: number
  handover: number
  inspection: number
}

export type DashboardV2SourceItem = {
  source: string
  count: number
  total: number
  severe: number
  warning: number
  reminder: number
}

export type DashboardV2OwnerItem = {
  owner: string
  total: number
  severe: number
  warning: number
  reminder: number
}

export type DashboardV2Data = {
  summary: DashboardV2Summary
  trend: DashboardV2TrendItem[]
  sourceDistribution: DashboardV2SourceItem[]
  ownerWorkload: DashboardV2OwnerItem[]
}

export function fetchDashboardV2(params: DashboardV2Query) {
  return request.get<any, ApiResponse<DashboardV2Data>>('/dashboard/v2', {
    params,
  })
}

export function fetchDashboardV3() {
  return request.get<any, ApiResponse<DashboardV3Data>>('/dashboard/v3')
}
