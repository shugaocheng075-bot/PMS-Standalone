export interface ProjectItem {
  id: number
  hospitalName: string
  productName: string
  province: string
  groupName: string
  salesName: string
  maintenancePersonName: string
  afterSalesStartDate: string
  afterSalesEndDate: string
  hospitalLevel: string
  contractStatus: string
  contractValidityStatus: string
  maintenanceAmount: number
  overdueDays: number
  opportunityNumber: string
  implementationStatus: string
  workHoursManDays: number
  personnelCount: number
  personnel1: string
  personnel2: string
  personnel3: string
  personnel4: string
  personnel5: string
  afterSalesProjectType: string
  remarks: string
}

export interface ProjectQuery {
  hospitalName?: string
  productName?: string
  province?: string
  groupName?: string
  salesName?: string
  maintenancePersonName?: string
  afterSalesEndDateFrom?: string
  afterSalesEndDateTo?: string
  hospitalLevel?: string
  contractStatus?: string
  contractValidityStatus?: string
  page?: number
  size?: number
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  size: number
}

export interface ApiResponse<T> {
  code: number
  message: string
  data: T
}
