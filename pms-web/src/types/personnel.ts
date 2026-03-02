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
