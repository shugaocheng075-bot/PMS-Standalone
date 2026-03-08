export const CURRENT_PERSONNEL_STORAGE_KEY = 'pms-current-personnel-id'
export const AUTH_SESSION_STORAGE_KEY = 'pms-auth-session'
export const AUTH_TOKEN_STORAGE_KEY = 'pms-auth-token'

export const ROUTE_PERMISSION_ENTRIES: Array<{ path: string; permission: string }> = [
  { path: '/dashboard', permission: 'dashboard.view' },
  { path: '/alert/center', permission: 'alert-center.view' },
  { path: '/project/list', permission: 'project.view' },
  { path: '/contract/alerts', permission: 'contract.view' },
  { path: '/handover/list', permission: 'handover.view' },
  { path: '/inspection/plan', permission: 'inspection.view' },
  { path: '/annual-report/list', permission: 'annual-report.view' },
  { path: '/hospital/list', permission: 'hospital.view' },
  { path: '/personnel/list', permission: 'permission.manage' },
  { path: '/permission/manage', permission: 'permission.manage' },
  { path: '/product/list', permission: 'product.view' },
  { path: '/major-demand/list', permission: 'major-demand.view' },
  { path: '/maintenance/data', permission: 'maintenance.manage' },
  { path: '/repair/list', permission: 'repair.view' },
  { path: '/workhours/list', permission: 'workhours.view' },
  { path: '/audit/log', permission: 'audit.view' },
]

export const ROUTE_PERMISSION_MAP = ROUTE_PERMISSION_ENTRIES.reduce<Record<string, string>>((acc, current) => {
  acc[current.path] = current.permission
  return acc
}, {})

export const resolveRoutePermission = (path: string): string | undefined => ROUTE_PERMISSION_MAP[path]

export const isAuthenticated = (): boolean => {
  if (typeof window === 'undefined') {
    return false
  }

  return Boolean(window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY))
}

export const setAuthenticated = (authenticated: boolean): void => {
  if (typeof window === 'undefined') {
    return
  }

  if (authenticated) {
    window.localStorage.setItem(AUTH_SESSION_STORAGE_KEY, '1')
  } else {
    window.localStorage.removeItem(AUTH_SESSION_STORAGE_KEY)
  }
}

export const getAccessToken = (): string => {
  if (typeof window === 'undefined') {
    return ''
  }

  return window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY) ?? ''
}

export const setAccessToken = (token: string): void => {
  if (typeof window === 'undefined') {
    return
  }

  const value = token.trim()
  if (!value) {
    window.localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY)
    return
  }

  window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, value)
}

export const clearAuthState = (): void => {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.removeItem(AUTH_SESSION_STORAGE_KEY)
  window.localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY)
}