import axios from 'axios'
import {
  clearAuthState,
  CURRENT_PERSONNEL_STORAGE_KEY,
  getAccessToken,
} from '../constants/access'

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api'

const request = axios.create({
  baseURL,
  timeout: 10000,
})

const isLoginRequest = (url?: string): boolean => {
  if (!url) {
    return false
  }

  const normalized = (url.split('?')[0] ?? '').trim()
  if (!normalized) {
    return false
  }

  return /(^|\/)auth\/login\/?$/i.test(normalized)
}

const sanitizeParams = (params: unknown): unknown => {
  if (params === null || params === undefined) {
    return undefined
  }

  if (Array.isArray(params)) {
    return params
      .map((item) => sanitizeParams(item))
      .filter((item) => item !== undefined)
  }

  if (typeof params === 'object') {
    const result: Record<string, unknown> = {}
    for (const [key, value] of Object.entries(params as Record<string, unknown>)) {
      const sanitized = sanitizeParams(value)
      if (sanitized !== undefined) {
        result[key] = sanitized
      }
    }
    return result
  }

  if (typeof params === 'string') {
    const trimmed = params.trim()
    return trimmed.length > 0 ? trimmed : undefined
  }

  return params
}

request.interceptors.request.use((config) => {
  if (config.params) {
    config.params = sanitizeParams(config.params)
  }

  if (isLoginRequest(config.url)) {
    config.headers = config.headers ?? {}
    delete config.headers.Authorization
    delete config.headers['X-PMS-User-Id']
    return config
  }

  const rawPersonnelId = typeof window !== 'undefined'
    ? window.localStorage.getItem(CURRENT_PERSONNEL_STORAGE_KEY)
    : null
  const personnelId = rawPersonnelId ? Number(rawPersonnelId) : 1
  const resolvedPersonnelId = Number.isInteger(personnelId) && personnelId > 0 ? personnelId : 1

  config.headers = config.headers ?? {}
  config.headers['X-PMS-User-Id'] = String(resolvedPersonnelId)

  const accessToken = getAccessToken()
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }

  return config
})

request.interceptors.response.use(
  (response) => response.data,
  (error) => {
    const status = error?.response?.status
    if (status === 401) {
      clearAuthState()
      if (typeof window !== 'undefined' && window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }

    return Promise.reject(error)
  },
)

export default request
