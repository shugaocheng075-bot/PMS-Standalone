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
