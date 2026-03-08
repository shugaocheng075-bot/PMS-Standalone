export interface AuditLogItem {
  id: number
  operator: string
  operatorId: number
  action: string
  module: string
  target: string
  detail: string
  ipAddress: string
  createdAt: string
}

export interface AuditLogQuery {
  action?: string
  module?: string
  operatorName?: string
  startDate?: string
  endDate?: string
  page?: number
  size?: number
}

export interface AuditLogSummary {
  total: number
  todayCount: number
  actionCounts: Record<string, number>
}
