import request from '../request'
import type { ApiResponse, PagedResult } from '../../types/project'
import type { ProductItem, ProductQuery, ProductSummary, ProductUpsert } from '../../types/product'

export function fetchProductSummary() {
  return request.get<any, ApiResponse<ProductSummary>>('/products/summary')
}

export function fetchProducts(params: ProductQuery) {
  return request.get<any, ApiResponse<PagedResult<ProductItem>>>('/products', { params })
}

export function fetchProductById(id: number) {
  return request.get<any, ApiResponse<ProductItem>>(`/products/${id}`)
}

export function createProduct(data: ProductUpsert) {
  return request.post<any, ApiResponse<ProductItem>>('/products', data)
}

export function updateProduct(id: number, data: ProductUpsert) {
  return request.put<any, ApiResponse<ProductItem>>(`/products/${id}`, data)
}

export function deleteProduct(id: number) {
  return request.delete<any, { code: number; message: string }>(`/products/${id}`)
}

export function batchDeleteProducts(ids: number[]) {
  return request.post<any, { code: number; message: string }>('/products/batch-delete', { ids })
}
