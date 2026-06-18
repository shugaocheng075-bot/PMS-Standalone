export interface OperationsTaskItem {
  id: string | number
  level: string
  source: string
  owner: string
  hospitalName: string
  productName?: string
  title: string
  detail?: string
  dueAt?: string | null
  status?: string
  overdueDays: number
  relatedPath: string
  relatedQuery: Record<string, string>
}

export interface OperationsTaskSummary {
  total: number
  severe?: number
  warning?: number
  reminder?: number
  overdue?: number
}

export interface OperationsTaskQuery {
  level?: string
  source?: string
  owner?: string
  hospitalName?: string
  keyword?: string
  page?: number
  size?: number
}

export interface OperationsTaskPageResult {
  items: OperationsTaskItem[]
  total: number
  page: number
  size: number
  summary?: OperationsTaskSummary
}
