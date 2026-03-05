export interface MonthlyReportItem {
  id: number
  hospitalName: string
  reportMonth: string
  submittedBy: string
  title: string
  content: string
  attachments: string[]
  status: string
  createdAt: string
  updatedAt: string
}

export interface MonthlyReportUpsert {
  hospitalName: string
  reportMonth: string
  title: string
  content: string
  attachments: string[]
  status?: string
}

export interface MonthlyReportQuery {
  hospitalName?: string
  reportMonth?: string
  submittedBy?: string
  status?: string
  page?: number
  size?: number
}
