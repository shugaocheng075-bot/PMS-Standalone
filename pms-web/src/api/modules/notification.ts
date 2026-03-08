import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { NotificationItem, NotificationQuery, NotificationSummary } from '../../types/notification'

export function fetchNotificationSummary() {
  return request.get<any, ApiResponse<NotificationSummary>>('/notifications/summary')
}

export function fetchNotifications(params: NotificationQuery) {
  return request.get<any, ApiResponse<PagedResult<NotificationItem>>>('/notifications', { params })
}

export function markNotificationAsRead(id: number) {
  return request.put<any, ApiResponse<any>>(`/notifications/${id}/read`)
}

export function markAllNotificationsAsRead() {
  return request.put<any, ApiResponse<any>>('/notifications/read-all')
}
