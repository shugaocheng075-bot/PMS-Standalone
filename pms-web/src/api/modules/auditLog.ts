import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { AuditLogItem, AuditLogQuery, AuditLogSummary } from '../../types/auditLog'

export function fetchAuditLogs(params: AuditLogQuery) {
  return request.get<any, ApiResponse<PagedResult<AuditLogItem>>>('/audit-logs', { params })
}

export function fetchAuditLogSummary() {
  return request.get<any, ApiResponse<AuditLogSummary>>('/audit-logs/summary')
}
