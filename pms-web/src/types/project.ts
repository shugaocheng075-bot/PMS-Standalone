export interface ProjectItem {
  id: number
  hospitalName: string
  productName: string
  province: string
  groupName: string
  hospitalLevel: string
  contractStatus: string
  maintenanceAmount: number
  overdueDays: number
}

export interface ProjectQuery {
  hospitalName?: string
  productName?: string
  province?: string
  groupName?: string
  hospitalLevel?: string
  contractStatus?: string
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
