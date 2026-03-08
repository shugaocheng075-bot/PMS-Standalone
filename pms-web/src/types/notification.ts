export interface NotificationItem {
  id: number
  type: string
  title: string
  content: string
  relatedPath: string
  isRead: boolean
  createdAt: string
}

export interface NotificationSummary {
  total: number
  unreadCount: number
}

export interface NotificationQuery {
  isRead?: boolean
  page?: number
  size?: number
}
