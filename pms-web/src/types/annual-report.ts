export interface AnnualReportItem {
  id: number
  hospitalName: string
  province: string
  groupName: string
  servicePerson: string
  reportYear: number
  status: '未开始' | '编写中' | '已提交' | '已完成'
  submitDate?: string | null
}

export interface AnnualReportSummary {
  notStartedCount: number
  writingCount: number
  submittedCount: number
  completedCount: number
  thisYearCount: number
  total: number
}

export interface AnnualReportQuery {
  status?: string
  reportYear?: number
  groupName?: string
  servicePerson?: string
  page?: number
  size?: number
}
