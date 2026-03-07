export interface WorkHoursItem {
  id: number
  projectId: number
  personnelName: string
  hospitalName: string
  workDate: string
  hours: number
  workType: string
  description: string
  createdAt: string
  updatedAt: string
  opportunityNumber: string
  productName: string
  implementationStatus: string
}

export interface WorkHoursUpsert {
  projectId: number
  hospitalName: string
  workDate: string
  hours: number
  workType: string
  description: string
}

export interface WorkHoursQuery {
  personnelName?: string
  hospitalName?: string
  workDateFrom?: string
  workDateTo?: string
  workType?: string
  page?: number
  size?: number
}

export interface WorkHoursSummary {
  total: number
  totalHours: number
  onsiteCount: number
  remoteCount: number
  travelCount: number
}
