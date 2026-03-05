export interface ContractAlertItem {
  projectId: number
  contractType: '产品实施合同' | '重大需求合同' | string
  hospitalName: string
  province: string
  groupName: string
  salesName: string
  contractStatus: string
  contractValidityStatus: '有效' | '待续签' | '已过期' | string
  maintenanceAmount: number
  overdueDays: number
  alertLevel: '提醒' | '警告' | '严重'
}

export interface ContractAlertSummary {
  reminderCount: number
  warningCount: number
  criticalCount: number
  total: number
}

export interface ContractAlertQuery {
  alertLevel?: string
  contractType?: string
  contractValidityStatus?: string
  province?: string
  groupName?: string
  salesName?: string
  page?: number
  size?: number
}
