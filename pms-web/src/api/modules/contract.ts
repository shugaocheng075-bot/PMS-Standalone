import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { ContractAlertItem, ContractAlertQuery, ContractAlertSummary } from '../../types/contract'

export function fetchContractAlertSummary() {
  return request.get<any, ApiResponse<ContractAlertSummary>>('/contracts/alerts/summary')
}

export function fetchContractAlerts(params: ContractAlertQuery) {
  return request.get<any, ApiResponse<PagedResult<ContractAlertItem>>>('/contracts/alerts', {
    params,
  })
}
