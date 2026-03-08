export interface AnnualReportItem {
  id: number
  opportunityNumber: string
  hospitalName: string
  productName: string
  province: string
  groupName: string
  servicePerson: string
  implementationStatus: string
  maintenanceStartDate: string
  maintenanceEndDate: string
  dueMonth: string
  reportYear: number
  status: '未开始' | '编写中' | '已提交' | '已完成'
  submitDate?: string | null
  remarks?: string | null
}

export interface AnnualReportUpsert {
  opportunityNumber?: string | null
  hospitalName?: string | null
  productName?: string | null
  province?: string | null
  groupName?: string | null
  servicePerson?: string | null
  implementationStatus?: string | null
  maintenanceStartDate?: string | null
  maintenanceEndDate?: string | null
  reportYear?: number | null
  status?: string | null
  submitDate?: string | null
  remarks?: string | null
}

export interface AnnualReportSummary {
  notStartedCount: number
  writingCount: number
  submittedCount: number
  completedCount: number
  thisYearCount: number
  dueThisMonthCount: number
  overdueCount: number
  total: number
}

export interface AnnualReportQuery {
  status?: string
  reportYear?: number
  dueMonth?: string
  groupName?: string
  servicePerson?: string
  page?: number
  size?: number
}
