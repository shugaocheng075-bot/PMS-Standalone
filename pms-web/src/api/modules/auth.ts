import axios from 'axios'
import request from '../request'
import type { ApiResponse } from '../../types/project'
import type { LoginRequest, LoginResult, ChangePasswordRequest } from '../../types/auth'

const authBaseURL = import.meta.env.VITE_API_BASE_URL || '/api'

const authClient = axios.create({
  baseURL: authBaseURL,
  timeout: 10000,
})

export function login(data: LoginRequest) {
  return authClient
    .post<ApiResponse<LoginResult>>('/auth/login', data)
    .then((response) => response.data)
}

export function logout() {
  return request.post<any, ApiResponse<boolean>>('/auth/logout')
}

export function changePassword(data: ChangePasswordRequest) {
  return request.post<any, ApiResponse<boolean>>('/auth/change-password', data)
}
