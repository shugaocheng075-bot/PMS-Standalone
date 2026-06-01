import { onMounted, onUnmounted } from 'vue'
import {
  DATA_CHANGED_EVENT,
  DATA_SYNC_CHANNEL,
  emitDataChanged,
  shouldRefreshForScopes,
  type DataChangePayload,
} from '../utils/dataSync'

const DEFAULT_INTERVAL_MS = 60000
const MIN_INTERVAL_MS = 30000
const REFRESH_DEBOUNCE_MS = 180

type LinkedRefreshOptions = {
  refresh: () => Promise<void> | void
  scope?: string | string[]
  intervalMs?: number
  immediate?: boolean
  enableAutoRefresh?: boolean
  refreshOnFocus?: boolean
}

export function useLinkedRealtimeRefresh(options: LinkedRefreshOptions) {
  const scopes = (Array.isArray(options.scope) ? options.scope : [options.scope ?? 'global'])
    .map((scope) => scope.trim().toLowerCase())
    .filter(Boolean)
  const intervalMs = Math.max(options.intervalMs ?? DEFAULT_INTERVAL_MS, MIN_INTERVAL_MS)
  const immediate = options.immediate ?? false
  const enableAutoRefresh = options.enableAutoRefresh ?? false
  const refreshOnFocus = options.refreshOnFocus ?? enableAutoRefresh
  let timer: ReturnType<typeof setInterval> | null = null
  let debounceTimer: ReturnType<typeof setTimeout> | null = null
  let channel: BroadcastChannel | null = null
  let refreshing = false
  let pending = false

  const runRefresh = async () => {
    if (typeof document !== 'undefined' && document.hidden) {
      return
    }

    if (refreshing) {
      pending = true
      return
    }

    refreshing = true
    try {
      await Promise.resolve(options.refresh())
    } finally {
      refreshing = false
      if (pending) {
        pending = false
        scheduleRefresh()
      }
    }
  }

  const scheduleRefresh = () => {
    if (debounceTimer) {
      clearTimeout(debounceTimer)
    }

    debounceTimer = setTimeout(() => {
      debounceTimer = null
      void runRefresh()
    }, REFRESH_DEBOUNCE_MS)
  }

  const onChanged = (event: Event) => {
    const customEvent = event as CustomEvent<DataChangePayload>
    if (!shouldRefreshForScopes(scopes, customEvent.detail?.scopes ?? ['global'])) {
      return
    }

    scheduleRefresh()
  }

  const onFocus = () => {
    scheduleRefresh()
  }

  const onVisibility = () => {
    if (!document.hidden) {
      scheduleRefresh()
    }
  }

  const onChannelMessage = (message: MessageEvent<DataChangePayload>) => {
    if (!shouldRefreshForScopes(scopes, message.data?.scopes ?? ['global'])) {
      return
    }

    scheduleRefresh()
  }

  const notifyDataChanged = (targetScope = 'global') => {
    emitDataChanged(targetScope, 'manual')
  }

  onMounted(() => {
    if (typeof window === 'undefined') {
      return
    }

    window.addEventListener(DATA_CHANGED_EVENT, onChanged as EventListener)

    if (refreshOnFocus) {
      window.addEventListener('focus', onFocus)
      document.addEventListener('visibilitychange', onVisibility)
    }

    if (typeof BroadcastChannel !== 'undefined') {
      channel = new BroadcastChannel(DATA_SYNC_CHANNEL)
      channel.addEventListener('message', onChannelMessage)
    }

    if (enableAutoRefresh) {
      timer = setInterval(() => {
        scheduleRefresh()
      }, intervalMs)
    }

    if (immediate) {
      scheduleRefresh()
    }
  })

  onUnmounted(() => {
    if (typeof window !== 'undefined') {
      window.removeEventListener(DATA_CHANGED_EVENT, onChanged as EventListener)
      window.removeEventListener('focus', onFocus)
      document.removeEventListener('visibilitychange', onVisibility)
    }

    if (debounceTimer) {
      clearTimeout(debounceTimer)
      debounceTimer = null
    }

    if (timer) {
      clearInterval(timer)
      timer = null
    }

    if (channel) {
      channel.removeEventListener('message', onChannelMessage)
      channel.close()
      channel = null
    }
  })

  return {
    notifyDataChanged,
  }
}
