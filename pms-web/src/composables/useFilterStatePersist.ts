import { watch } from 'vue'

type PersistOptions<T> = {
  key: string
  getState: () => T
  applyState: (state: Partial<T>) => void
}

export const useFilterStatePersist = <T extends Record<string, unknown>>({
  key,
  getState,
  applyState,
}: PersistOptions<T>) => {
  const storageKey = `pms:filter:${key}`

  const restore = () => {
    if (typeof window === 'undefined') {
      return false
    }

    try {
      const raw = window.localStorage.getItem(storageKey)
      if (!raw) {
        return false
      }

      const parsed = JSON.parse(raw) as Partial<T>
      if (!parsed || typeof parsed !== 'object') {
        return false
      }

      applyState(parsed)
      return true
    } catch {
      return false
    }
  }

  const persist = () => {
    if (typeof window === 'undefined') {
      return
    }

    try {
      window.localStorage.setItem(storageKey, JSON.stringify(getState()))
    } catch {
    }
  }

  const clear = () => {
    if (typeof window === 'undefined') {
      return
    }

    window.localStorage.removeItem(storageKey)
  }

  watch(getState, persist, { deep: true })

  return {
    restore,
    persist,
    clear,
  }
}