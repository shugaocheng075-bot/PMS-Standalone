export interface ProductSummary {
  total: number
  activeCount: number
  pilotCount: number
  retiredCount: number
}

export interface ProductItem {
  id: number
  productName: string
  version: string
  category: string
  status: string
  deployHospitalCount: number
  createdAt: string
}

export interface ProductQuery {
  productName?: string
  category?: string
  status?: string
  page?: number
  size?: number
}

export interface ProductUpsert {
  productName: string
  version: string
  category: string
  status: string
  deployHospitalCount: number
}
