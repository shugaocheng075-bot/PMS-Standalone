import request from '../request'
import type { ApiResponse, PagedResult, ProjectItem, ProjectQuery } from '../../types/project'

export function fetchProjectList(params: ProjectQuery) {
  return request.get<any, ApiResponse<PagedResult<ProjectItem>>>('/projects', {
    params,
  })
}
