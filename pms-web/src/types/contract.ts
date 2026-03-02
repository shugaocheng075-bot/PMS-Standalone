export interface ContractAlertItem {
  projectId: number
  hospitalName: string
  province: string
  groupName: string
  salesName: string
  contractStatus: string
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
  province?: string
  groupName?: string
  salesName?: string
  page?: number
  size?: number
}
