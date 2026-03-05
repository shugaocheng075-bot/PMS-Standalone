import { computed, ref } from 'vue'
import { fetchCurrentAccessProfile } from '../api/modules/access'
import { CURRENT_PERSONNEL_STORAGE_KEY, ROUTE_PERMISSION_ENTRIES, resolveRoutePermission } from '../constants/access'
import type { AccessProfile } from '../types/access'

const parseInitialPersonnelId = (): number => {
  const raw = typeof window !== 'undefined' ? window.localStorage.getItem(CURRENT_PERSONNEL_STORAGE_KEY) : null
  const parsed = raw ? Number(raw) : 1
  return Number.isInteger(parsed) && parsed > 0 ? parsed : 1
}

const currentPersonnelId = ref<number>(parseInitialPersonnelId())
const accessProfile = ref<AccessProfile | null>(null)
const initialized = ref(false)
let pendingLoad: Promise<void> | null = null

const permissionSet = computed(() => new Set(accessProfile.value?.permissions ?? []))

const getSystemRole = (): string => {
  return (accessProfile.value?.systemRole ?? '').trim().toLowerCase()
}

const canPermission = (permission?: string): boolean => {
  if (!permission) {
    return true
  }

  if (accessProfile.value?.isAdmin) {
    return true
  }

  return permissionSet.value.has(permission)
}

const ensureAccessProfileLoaded = async (force = false): Promise<void> => {
  if (!force && initialized.value) {
    return
  }

  if (!force && pendingLoad) {
    await pendingLoad
    return
  }

  pendingLoad = (async () => {
    const response = await fetchCurrentAccessProfile()
    accessProfile.value = response.data
    initialized.value = true
  })()

  try {
    await pendingLoad
  } finally {
    pendingLoad = null
  }
}

const setCurrentPersonnelId = (personnelId: number): void => {
  if (!Number.isInteger(personnelId) || personnelId <= 0) {
    return
  }

  currentPersonnelId.value = personnelId
  if (typeof window !== 'undefined') {
    window.localStorage.setItem(CURRENT_PERSONNEL_STORAGE_KEY, String(personnelId))
  }
  initialized.value = false
}

const canAccessPath = (path: string): boolean => {
  const permission = resolveRoutePermission(path)
  return canPermission(permission)
}

const isManager = (): boolean => {
  if (accessProfile.value?.isAdmin) {
    return true
  }

  return getSystemRole() === 'manager'
}

const isSupervisor = (): boolean => getSystemRole() === 'supervisor'

const isOperator = (): boolean => getSystemRole() === 'operator'

const getFirstAccessiblePath = (): string => {
  const first = ROUTE_PERMISSION_ENTRIES.find((entry) => canPermission(entry.permission))
  return first?.path ?? '/dashboard'
}

export const useAccessControl = () => {
  return {
    currentPersonnelId,
    accessProfile,
    initialized,
    ensureAccessProfileLoaded,
    setCurrentPersonnelId,
    canPermission,
    canAccessPath,
    getFirstAccessiblePath,
    getSystemRole,
    isManager,
    isSupervisor,
    isOperator,
  }
}