import request from '../request'
import type { ApiResponse } from '../../types/project'
import type {
  OperationsTaskPageResult,
  OperationsTaskQuery,
  OperationsTaskSummary,
} from '../../types/operations'

export function fetchOperationsTaskSummary(params: OperationsTaskQuery = {}) {
  return request.get<any, ApiResponse<OperationsTaskSummary>>('/operations/tasks/summary', {
    params,
  })
}

export function fetchOperationsTasks(params: OperationsTaskQuery = {}) {
  return request.get<any, ApiResponse<OperationsTaskPageResult>>('/operations/tasks', {
    params,
  })
}
