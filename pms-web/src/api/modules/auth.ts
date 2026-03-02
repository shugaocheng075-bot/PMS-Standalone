import request from '../request'
import type { ApiResponse } from '../../types/project'
import type { LoginRequest, LoginResult } from '../../types/auth'

export function login(data: LoginRequest) {
  return request.post<any, ApiResponse<LoginResult>>('/auth/login', data)
}

export function logout() {
  return request.post<any, ApiResponse<boolean>>('/auth/logout')
}
