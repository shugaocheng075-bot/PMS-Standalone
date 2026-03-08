export interface ExpiringContract {
  projectId: number
  hospitalName: string
  productName: string
  afterSalesEndDate: string
  daysRemaining: number
}

export interface PendingInspection {
  id: number
  hospitalName: string
  productName: string
  inspector: string
  planDate: string
  status: string
}

export interface UnresolvedRepair {
  id: number
  hospitalName: string
  productName: string
  description: string
  reportedAt: string | null
  urgency: string
}

export interface WorkbenchData {
  myProjects: number
  pendingRepairCount: number
  pendingInspectionCount: number
  thisMonthWorkHours: number
  expiringContracts: ExpiringContract[]
  pendingInspections: PendingInspection[]
  unresolvedRepairs: UnresolvedRepair[]
}
