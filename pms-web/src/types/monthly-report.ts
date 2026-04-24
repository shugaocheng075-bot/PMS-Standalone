export interface MonthlyReportItem {
  id: number
  hospitalName: string
  reportMonth: string
  submittedBy: string
  title: string
  content: string
  attachments: string[]
  status: string
  approvedBy: string
  approvedAt: string | null
  rejectionReason: string
  createdAt: string
  updatedAt: string
  groupName: string
  teamTotal: number
  teamOnsiteJson: string
  teamSummaryJson: string
  projectOverviewJson: string
  perCapitaMetricsJson: string
  handoverItemsJson: string
  weeklyReportRate: number
  monthlyReportRate: number
  majorDemandAcceptanceJson: string
  inspectionRecordsJson: string
  annualServiceReportsJson: string
  incidentsJson: string
  nextMonthInspectionPlanJson: string
  nextMonthAnnualReportPlanJson: string
  nextMonthOtherPlanJson: string
}

export interface MonthlyReportUpsert {
  hospitalName: string
  reportMonth: string
  title: string
  content: string
  attachments: string[]
  groupName?: string
  teamTotal?: number
  teamOnsiteJson?: string
  teamSummaryJson?: string
  projectOverviewJson?: string
  perCapitaMetricsJson?: string
  handoverItemsJson?: string
  weeklyReportRate?: number
  monthlyReportRate?: number
  majorDemandAcceptanceJson?: string
  inspectionRecordsJson?: string
  annualServiceReportsJson?: string
  incidentsJson?: string
  nextMonthInspectionPlanJson?: string
  nextMonthAnnualReportPlanJson?: string
  nextMonthOtherPlanJson?: string
}

export interface MonthlyReportApprovalPayload {
  rejectionReason?: string
}

export interface MonthlyReportQuery {
  hospitalName?: string
  reportMonth?: string
  submittedBy?: string
  status?: string
  groupName?: string
  page?: number
  size?: number
}
