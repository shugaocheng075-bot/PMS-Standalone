import request from '../request'
import type { ApiResponse } from '../../types/project'

export type MajorDemandComment = {
  id: string
  content: string
  createdBy: string
  createdAt: string
}

export type MajorDemandLog = {
  id: string
  action: string
  detail: string
  createdBy: string
  createdAt: string
}

export type MajorDemandWorkflow = {
  rowId: string
  status: string
  owner: string
  dueDate: string
  updatedAt: string
  comments: MajorDemandComment[]
  logs: MajorDemandLog[]
}

export type MajorDemandSnapshot = {
  columns: string[]
  rows: Array<Record<string, string>>
  workflows: MajorDemandWorkflow[]
  sourceFilePath: string
  sheetName: string
  importedAt: string
}

export function fetchMajorDemands() {
  return request.get<any, ApiResponse<MajorDemandSnapshot>>('/major-demands')
}

export function importMajorDemandsFromDesktop() {
  return request.post<any, ApiResponse<any>>('/admin/import/major-demand', {
    filePath: 'C:\\Users\\R9000P\\Desktop\\项目明细.xlsx',
    sheetName: '重大需求明细',
  })
}

export function batchUpdateMajorDemandStatus(payload: { rowIds: string[]; status: string }) {
  return request.post<any, ApiResponse<any>>('/major-demands/batch/status', payload)
}

export function batchAssignMajorDemandOwner(payload: { rowIds: string[]; owner: string }) {
  return request.post<any, ApiResponse<any>>('/major-demands/batch/owner', payload)
}

export function batchUpdateMajorDemandDueDate(payload: { rowIds: string[]; dueDate: string }) {
  return request.post<any, ApiResponse<any>>('/major-demands/batch/due-date', payload)
}

export function addMajorDemandComment(rowId: string, content: string) {
  return request.post<any, ApiResponse<any>>(`/major-demands/${encodeURIComponent(rowId)}/comments`, { content })
}

export function exportMajorDemandCsv() {
  return request.get<any, Blob>('/major-demands/export', {
    responseType: 'blob',
  })
}

export function updateMajorDemandCell(rowId: string, column: string, value: string) {
  return request.put<any, ApiResponse<any>>(`/major-demands/${encodeURIComponent(rowId)}/cell`, { column, value })
}

export function addMajorDemandRow() {
  return request.post<any, ApiResponse<{ rowId: string }>>('/major-demands/rows')
}

export function deleteMajorDemandRows(rowIds: string[]) {
  return request.post<any, ApiResponse<any>>('/major-demands/rows/delete', { rowIds })
}

export function exportMajorDemandExcel() {
  return request.get<any, Blob>('/major-demands/export-excel', {
    responseType: 'blob',
  })
}