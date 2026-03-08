import request from '../request'
import type { ApiResponse } from '../../types/project'

export type AlertCenterItem = {
  id: string
  source: '合同' | '交接' | '巡检' | string
  level: '严重' | '警告' | '提醒' | string
  priority: number
  hospitalName: string
  title: string
  detail: string
  owner: string
  overdueDays: number
  relatedPath: string
  relatedQuery: Record<string, string>
}

export type AlertCenterSummary = {
  total: number
  severe: number
  warning: number
  reminder: number
  contract: number
  handover: number
  inspection: number
}

export type AlertCenterPagedResult = {
  items: AlertCenterItem[]
  total: number
  page: number
  size: number
  summary: AlertCenterSummary
}

export type AlertCenterQuery = {
  source?: string
  level?: string
  keyword?: string
  page?: number
  size?: number
}

export function fetchAlertCenter(params: AlertCenterQuery) {
  return request.get<any, ApiResponse<AlertCenterPagedResult>>('/alerts/center', {
    params,
  })
}

export function exportAlertCenter(params: AlertCenterQuery) {
  return request.get<any, Blob>('/alerts/center/export', {
    params,
    responseType: 'blob',
  })
}
