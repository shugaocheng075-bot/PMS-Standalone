export interface ContractStatusItem {
  status: string
  count: number
}

export interface ContractStatusDistribution {
  total: number
  items: ContractStatusItem[]
}

export interface HospitalProductCoverage {
  hospitalCount: number
  productCount: number
  coveragePairs: number
  avgProductsPerHospital: number
}

export interface RateMetric {
  total: number
  completed: number
  rate: number
}

export interface PersonnelLoadRate {
  totalPersonnel: number
  activePersonnel: number
  rate: number
}

export interface DashboardV3Data {
  contractStatusDistribution: ContractStatusDistribution
  hospitalProductCoverage: HospitalProductCoverage
  repairProcessingRate: RateMetric
  personnelLoadRate: PersonnelLoadRate
  inspectionCompletionRate: RateMetric
}
