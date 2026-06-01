export const DATA_CHANGED_EVENT = 'pms:data-changed'
export const DATA_SYNC_CHANNEL = 'pms-realtime-sync'

export type DataChangePayload = {
  sourceScope: string
  scopes: string[]
  reason?: string
  at: number
}

const MUTATION_METHODS = new Set(['post', 'put', 'patch', 'delete'])

const SCOPE_DEPENDENCIES: Record<string, string[]> = {
  global: ['global'],
  maintenance: [
    'global',
    'dashboard',
    'project',
    'hospital',
    'product',
    'personnel',
    'contract',
    'alert',
    'handover',
    'inspection',
    'major-demand',
    'repair',
    'workhours',
    'monthly-report',
    'annual-report',
    'report',
  ],
  project: ['project', 'global', 'dashboard', 'contract', 'alert', 'monthly-report', 'annual-report', 'report'],
  hospital: ['hospital', 'global', 'dashboard', 'project', 'contract', 'alert', 'handover', 'inspection', 'major-demand', 'repair', 'monthly-report', 'annual-report'],
  product: ['product', 'global', 'dashboard', 'project', 'contract', 'alert', 'handover', 'inspection', 'repair', 'monthly-report', 'annual-report'],
  personnel: ['personnel', 'global', 'dashboard', 'project', 'handover', 'inspection', 'repair', 'workhours', 'monthly-report', 'annual-report', 'access'],
  contract: ['contract', 'global', 'dashboard', 'alert'],
  alert: ['alert', 'global', 'dashboard'],
  handover: ['handover', 'global', 'dashboard', 'alert'],
  inspection: ['inspection', 'global', 'dashboard', 'alert', 'monthly-report', 'annual-report'],
  'major-demand': ['major-demand', 'global', 'dashboard', 'alert'],
  repair: ['repair', 'global', 'dashboard', 'alert'],
  workhours: ['workhours', 'global', 'dashboard', 'monthly-report', 'report'],
  'monthly-report': ['monthly-report', 'global', 'dashboard', 'report', 'alert'],
  'annual-report': ['annual-report', 'global', 'dashboard', 'alert'],
  report: ['report', 'global', 'dashboard', 'workhours'],
  access: ['access', 'personnel', 'global', 'dashboard'],
  audit: ['audit', 'global'],
}

const PATH_SCOPE_RULES: Array<[RegExp, string]> = [
  [/^\/admin\/import\b/i, 'maintenance'],
  [/^\/reports\/workhours\b/i, 'report'],
  [/^\/projects\b/i, 'project'],
  [/^\/hospitals\b/i, 'hospital'],
  [/^\/products\b/i, 'product'],
  [/^\/personnel\b/i, 'personnel'],
  [/^\/access\b/i, 'access'],
  [/^\/repair-records\b/i, 'repair'],
  [/^\/inspections\b/i, 'inspection'],
  [/^\/major-demands\b/i, 'major-demand'],
  [/^\/workhours\b/i, 'workhours'],
  [/^\/monthly-reports\b/i, 'monthly-report'],
  [/^\/annual-reports\b/i, 'annual-report'],
  [/^\/handovers\b/i, 'handover'],
  [/^\/contract\b/i, 'contract'],
  [/^\/alerts\b/i, 'alert'],
  [/^\/audit\b/i, 'audit'],
  [/^\/auth\/change-password\b/i, 'access'],
]

let sharedChannel: BroadcastChannel | null = null

const normalizeScope = (scope: string) => scope.trim().toLowerCase()

export const expandDataChangeScopes = (sourceScope: string): string[] => {
  const normalized = normalizeScope(sourceScope)
  const scopes = SCOPE_DEPENDENCIES[normalized] ?? [normalized, 'global']
  return Array.from(new Set(scopes.map(normalizeScope).filter(Boolean)))
}

export const shouldRefreshForScopes = (registeredScopes: string[], changedScopes: string[]): boolean => {
  if (registeredScopes.includes('global')) {
    return true
  }

  const changed = new Set(changedScopes.map(normalizeScope))
  return registeredScopes.some((scope) => changed.has(normalizeScope(scope)))
}

export const createDataChangePayload = (sourceScope: string, reason?: string): DataChangePayload => {
  const normalized = normalizeScope(sourceScope)
  return {
    sourceScope: normalized,
    scopes: expandDataChangeScopes(normalized),
    reason,
    at: Date.now(),
  }
}

export const emitDataChanged = (sourceScope = 'global', reason?: string) => {
  if (typeof window === 'undefined') {
    return
  }

  const payload = createDataChangePayload(sourceScope, reason)
  window.dispatchEvent(new CustomEvent<DataChangePayload>(DATA_CHANGED_EVENT, { detail: payload }))

  if (typeof BroadcastChannel !== 'undefined') {
    sharedChannel ??= new BroadcastChannel(DATA_SYNC_CHANNEL)
    sharedChannel.postMessage(payload)
  }
}

const normalizePath = (url?: string): string => {
  if (!url) {
    return ''
  }

  const raw = url.split('?')[0]?.trim() ?? ''
  if (!raw) {
    return ''
  }

  try {
    const parsed = new URL(raw, typeof window === 'undefined' ? 'http://local' : window.location.origin)
    return parsed.pathname.replace(/^\/api(?=\/)/i, '')
  } catch {
    return raw.replace(/^https?:\/\/[^/]+/i, '').replace(/^\/api(?=\/)/i, '')
  }
}

export const inferDataChangeScope = (method?: string, url?: string): string | null => {
  const normalizedMethod = (method ?? 'get').toLowerCase()
  if (!MUTATION_METHODS.has(normalizedMethod)) {
    return null
  }

  const path = normalizePath(url)
  if (!path || /^\/auth\/login\b/i.test(path) || /^\/auth\/logout\b/i.test(path)) {
    return null
  }

  const matched = PATH_SCOPE_RULES.find(([pattern]) => pattern.test(path))
  return matched?.[1] ?? null
}
