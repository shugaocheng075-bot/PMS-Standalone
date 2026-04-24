import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type {
  HandoverItem,
  HandoverKanbanColumn,
  HandoverQuery,
  HandoverStageUpdateRequest,
  HandoverSummary,
} from '../../types/handover'

export function fetchHandoverSummary() {
  return request.get<any, ApiResponse<HandoverSummary>>('/handovers/summary')
}

export function fetchHandovers(params: HandoverQuery) {
  return request.get<any, ApiResponse<PagedResult<HandoverItem>>>('/handovers', {
    params,
  })
}

export function fetchHandoverById(id: number) {
  return request.get<any, ApiResponse<HandoverItem>>(`/handovers/${id}`)
}

export function fetchHandoverKanban() {
  return request.get<any, ApiResponse<HandoverKanbanColumn[]>>('/handovers/kanban')
}

export function sendHandoverEmail(id: number) {
  return request.patch<any, ApiResponse<HandoverItem>>(`/handovers/${id}/send-email`)
}

export function startHandover(id: number) {
  return request.patch<any, ApiResponse<HandoverItem>>(`/handovers/${id}/start`)
}

export function completeHandover(id: number) {
  return request.patch<any, ApiResponse<HandoverItem>>(`/handovers/${id}/complete`)
}

export function rollbackHandover(id: number) {
  return request.patch<any, ApiResponse<HandoverItem>>(`/handovers/${id}/rollback`)
}

export function updateHandoverStage(id: number, payload: HandoverStageUpdateRequest) {
  return request.put<any, ApiResponse<HandoverItem>>(`/handovers/${id}/stage`, payload)
}

export function exportHandovers(params: HandoverQuery) {
  return request.get<any, Blob>('/handovers/export', {
    params,
    responseType: 'blob',
  })
}
