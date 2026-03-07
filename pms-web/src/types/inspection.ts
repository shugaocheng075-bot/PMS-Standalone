export interface InspectionPlanItem {
  id: number
  hospitalName: string
  productName: string
  province: string
  groupName: string
  inspector: string
  planDate: string
  actualDate?: string | null
  status: '已计划' | '执行中' | '已完成' | '已取消'
  inspectionType: '现场' | '远程'
}

export interface InspectionPlanUpsert {
  groupName: string
  inspector: string
  planDate: string
  actualDate?: string | null
  status: '已计划' | '执行中' | '已完成' | '已取消'
  inspectionType: '现场' | '远程'
}

export interface InspectionSummary {
  plannedCount: number
  inProgressCount: number
  completedCount: number
  cancelledCount: number
  thisMonthCount: number
  total: number
}

export interface InspectionQuery {
  status?: string
  province?: string
  productName?: string
  groupName?: string
  inspector?: string
  page?: number
  size?: number
}

// ---- 巡检结果（由 SystemAuditTool 推送） ----

export interface InspectionRiskItem {
  level: string
  category: string
  title: string
  description: string
  currentValue: string
  thresholdValue: string
}

export interface InspectionResult {
  id: number
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
  healthLevel: '良好' | '警告' | '严重' | string
  overallScore: number
  databaseVersion?: string | null
  storageUsedPercent?: number | null
  tablespaceUsedPercent?: number | null
  backupStatus?: string | null
  daysToFull?: number | null
  topRisks: InspectionRiskItem[]
}

export interface InspectionResultQuery {
  hospitalName?: string
  productName?: string
  inspector?: string
  healthLevel?: string
  from?: string
  to?: string
  page?: number
  size?: number
}
