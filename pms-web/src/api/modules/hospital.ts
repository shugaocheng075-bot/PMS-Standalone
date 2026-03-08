import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { HospitalItem, HospitalQuery, HospitalRating, HospitalSummary, HospitalUpsert } from '../../types/hospital'

export function fetchHospitalSummary() {
  return request.get<any, ApiResponse<HospitalSummary>>('/hospitals/summary')
}

export function fetchHospitalStatistics() {
  return request.get<any, ApiResponse<HospitalSummary>>('/hospitals/statistics')
}

export function fetchHospitals(params: HospitalQuery) {
  return request.get<any, ApiResponse<PagedResult<HospitalItem>>>('/hospitals', { params })
}

export function fetchHospitalById(id: number) {
  return request.get<any, ApiResponse<HospitalItem>>(`/hospitals/${id}`)
}

export function createHospital(data: HospitalUpsert) {
  return request.post<any, ApiResponse<HospitalItem>>('/hospitals', data)
}

export function updateHospital(id: number, data: HospitalUpsert) {
  return request.put<any, ApiResponse<HospitalItem>>(`/hospitals/${id}`, data)
}

export function updateHospitalRating(id: number, data: HospitalRating) {
  return request.put<any, ApiResponse<HospitalItem>>(`/hospitals/${id}/rating`, data)
}

export function exportHospitals(params: HospitalQuery) {
  return request.get<any, Blob>('/hospitals/export', {
    params,
    responseType: 'blob',
  })

}

export function deleteHospital(id: number) {
  return request.delete<any, { code: number; message: string }>(`/hospitals/${id}`)
}
