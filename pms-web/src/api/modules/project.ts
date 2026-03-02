import request from '../request'
import type { ApiResponse, PagedResult, ProjectItem, ProjectQuery } from '../../types/project'

export function fetchProjectList(params: ProjectQuery) {
  return request.get<any, ApiResponse<PagedResult<ProjectItem>>>('/projects', {
    params,
  })
}

export function batchUpdateProjects(payload: {
  projectIds: number[]
  contractStatus?: string
  groupName?: string
  salesName?: string
  maintenancePersonName?: string
  hospitalLevel?: string
}) {
  return request.post<any, ApiResponse<any>>('/projects/batch-update', payload)
}

export function exportProjects(params: ProjectQuery) {
  return request.get<any, Blob>('/projects/export', {
    params,
    responseType: 'blob',
  })
}
