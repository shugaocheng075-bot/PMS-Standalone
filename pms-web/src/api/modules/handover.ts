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

export function fetchHandoverKanban() {
  return request.get<any, ApiResponse<HandoverKanbanColumn[]>>('/handovers/kanban')
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
