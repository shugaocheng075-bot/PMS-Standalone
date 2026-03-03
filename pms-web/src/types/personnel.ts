export interface PersonnelSummary {
  total: number
  serviceCount: number
  implementationCount: number
  onsiteCount: number
}

export interface PersonnelItem {
  id: number
  name: string
  department: string
  groupName: string
  roleType: string
  phone: string
  isOnsite: boolean
  projectCount: number
  overdueCount: number
  createdAt: string
  sourceColumns?: Record<string, string>
}

export interface PersonnelQuery {
  name?: string
  department?: string
  groupName?: string
  roleType?: string
  isOnsite?: boolean
  page?: number
  size?: number
}

export interface PersonnelUpsert {
  name: string
  department: string
  groupName: string
  roleType: string
  phone: string
  isOnsite: boolean
  projectCount: number
  overdueCount: number
}

export interface ExternalSyncResult {
  success: boolean
  skipped: boolean
  reason: string
  parsedCount: number
  addedCount: number
  updatedCount: number
  attemptedAt: string
  lastSuccessAt: string | null
  sourceUrl: string | null
}
