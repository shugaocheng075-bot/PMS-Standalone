export interface RepairRecordItem {
  id: number
  projectId: number
  hospitalName: string
  productName: string
  projectName: string
  productCategory: string
  issueCategory: string
  reporterName: string
  severity: string
  functionModule: string
  description: string
  reportedAt?: string
  actualWorkHours?: number
  content: string
  resolution: string
  attachmentImages: string
  registrationStatus: string
  workHoursDetail: string
  status: string
  urgency: string
  createdAt: string
  updatedAt: string
}

export interface RepairRecordUpsert {
  projectId: number
  hospitalName: string
  productName: string
  projectName: string
  productCategory: string
  issueCategory: string
  reporterName: string
  severity: string
  functionModule: string
  description: string
  reportedAt?: string
  actualWorkHours?: number
  content: string
  resolution: string
  attachmentImages: string
  registrationStatus: string
  status: string
  urgency: string
}

export interface RepairRecordQuery {
  hospitalName?: string
  reporterName?: string
  status?: string
  page?: number
  size?: number
}

export interface RepairRecordSummary {
  total: number
  pendingCount: number
  inProgressCount: number
  completedCount: number
}
