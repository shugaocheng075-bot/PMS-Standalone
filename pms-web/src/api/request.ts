import axios from 'axios'
import {
  clearAuthState,
  CURRENT_PERSONNEL_STORAGE_KEY,
  getAccessToken,
} from '../constants/access'

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api'

const resolveInitialPersonnelId = (): number => {
  if (typeof window === 'undefined') {
    return 1
  }

  const raw = window.localStorage.getItem(CURRENT_PERSONNEL_STORAGE_KEY)
  const parsed = raw ? Number(raw) : 1
  return Number.isInteger(parsed) && parsed > 0 ? parsed : 1
}

let cachedPersonnelId = resolveInitialPersonnelId()

if (typeof window !== 'undefined') {
  window.addEventListener('storage', (event) => {
    if (event.key !== CURRENT_PERSONNEL_STORAGE_KEY) {
      return
    }

    const parsed = event.newValue ? Number(event.newValue) : 1
    cachedPersonnelId = Number.isInteger(parsed) && parsed > 0 ? parsed : 1
  })
}

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

  if (typeof window !== 'undefined') {
    const raw = window.localStorage.getItem(CURRENT_PERSONNEL_STORAGE_KEY)
    const parsed = raw ? Number(raw) : 1
    cachedPersonnelId = Number.isInteger(parsed) && parsed > 0 ? parsed : 1
  }

  const resolvedPersonnelId = cachedPersonnelId

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
      if (typeof window !== 'undefined' && !window.location.pathname.startsWith('/login')) {
        window.location.assign('/login')
      }
    }

    return Promise.reject(error)
  },
)

export default request
