import request from '../request'
import type { ApiResponse } from '../../types/project'

export interface SystemInfo {
  appName: string
  version: string
  dotnetVersion: string
  os: string
  serverTime: string
  startTime: string
  environment: string
}

export function fetchSystemInfo() {
  return request.get<any, ApiResponse<SystemInfo>>('/system/info')
}

export function downloadBackup() {
  return request.get<any, Blob>('/system/backup/download', { responseType: 'blob' })
}

export function uploadRestore(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return request.post<any, ApiResponse<{ message: string }>>('/system/backup/restore', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}
