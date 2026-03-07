export interface WorkHoursReportRow {
  opportunityNumber: string
  hospitalName: string
  productName: string
  province: string
  groupName: string
  salesName: string
  maintenancePersonName: string
  implementationStatus: string
  afterSalesProjectType: string
  workHoursManDays: number
  personnelCount: number
  personnel1: string
  personnel2: string
  personnel3: string
  personnel4: string
  personnel5: string
}

export interface WorkHoursReportQuery {
  groupName?: string
  implementationStatus?: string
}

export interface MonthlyReportGenerateRequest {
  reportMonth: string
  groupName: string
  submittedBy?: string
  teamDataSource?: MonthlyReportTeamDataSourceInput
}

export interface MonthlyReportTeamDataSourceInput {
  authorizedHeadcount?: number
  centralStandardServiceCount?: number
  centralOnsiteCount?: number
  northwestStandardServiceCount?: number
  northwestOnsiteCount?: number
  sickLeaveCount?: number
  personalLeaveCount?: number
  otherSpecialCount?: number
  otherSpecialRemark?: string
  excludedCustomerCount?: number
  excludedProjectCount?: number
}

export interface MonthlyReportSourceMetricBlock {
  headcount: number
  customerCount: number
  productCount: number
  customerPerPerson: number
  productPerPerson: number
}

export interface MonthlyReportSourceTeamSummary {
  authorizedHeadcount: number
  totalHeadcount: number
  onsiteCount: number
  remoteCount: number
  centralStandardServiceCount: number
  centralOnsiteCount: number
  northwestStandardServiceCount: number
  northwestOnsiteCount: number
  unmatchedPersonnelCount: number
  sickLeaveCount: number
  personalLeaveCount: number
  otherSpecialCount: number
  otherSpecialRemark: string
}

export interface MonthlyReportSourceProjectSummary {
  totalCustomerCount: number
  totalProductCount: number
  centralCustomerCount: number
  centralProductCount: number
  northwestCustomerCount: number
  northwestProductCount: number
  onsiteDeductedCustomerCount: number
  onsiteDeductedProductCount: number
}

export interface MonthlyReportSourcePersonnelItem {
  personnelId: number
  name: string
  department: string
  groupName: string
  roleType: string
  systemRole: string
  serviceMode: string
  isOnsite: boolean
  supervisorName: string
  responsibilityHospitalCount: number
  responsibilityProductCount: number
  hospitalNames: string[]
  productNames: string[]
  matchingStatus: string
}

export interface MonthlyReportSourceOnsiteDeduction {
  personnelId: number
  name: string
  deductedHospitalCount: number
  deductedProductCount: number
  hospitalNames: string[]
  productNames: string[]
}

export interface MonthlyReportSourcePreview {
  reportMonth: string
  groupName: string
  supervisorName: string
  teamSummary: MonthlyReportSourceTeamSummary
  projectSummary: MonthlyReportSourceProjectSummary
  perCapitaMetrics: {
    allPersonnelAverage: MonthlyReportSourceMetricBlock
    excludeOnsiteAverage: MonthlyReportSourceMetricBlock
  }
  personnelItems: MonthlyReportSourcePersonnelItem[]
  onsiteDeductionItems: MonthlyReportSourceOnsiteDeduction[]
  warnings: string[]
}

export interface MonthlyReportExportQuery {
  reportMonth: string
  groupName: string
  submittedBy?: string
}
