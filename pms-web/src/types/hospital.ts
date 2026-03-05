export interface HospitalSummary {
  total: number
  threeTierCount: number
  twoTierCount: number
  oneTierCount: number
  regionCounts: Record<string, number>
}

export interface HospitalItem {
  id: number
  hospitalName: string
  tier: string
  province: string
  city: string
  address: string
  contactPerson: string
  contactPhone: string
  departmentCount: string
  productCount: number
  contractCount: number
  emrRatingLevel?: string
  interopRatingLevel?: string
  createdAt: string
}

export interface HospitalQuery {
  hospitalName?: string
  tier?: string
  province?: string
  city?: string
  page?: number
  size?: number
}

export interface HospitalUpsert {
  hospitalName: string
  tier: string
  province: string
  city: string
  address: string
  contactPerson: string
  contactPhone: string
  departmentCount: string
}

export interface HospitalRating {
  emrRatingLevel?: string
  interopRatingLevel?: string
}
