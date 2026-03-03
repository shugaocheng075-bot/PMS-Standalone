import { onMounted, onUnmounted } from 'vue'

const EVENT_NAME = 'pms:data-changed'
const CHANNEL_NAME = 'pms-realtime-sync'
const DEFAULT_INTERVAL_MS = 60000
const MIN_INTERVAL_MS = 30000

type LinkedRefreshOptions = {
  refresh: () => Promise<void> | void
  scope?: string
  intervalMs?: number
  immediate?: boolean
  enableAutoRefresh?: boolean
}

export function useLinkedRealtimeRefresh(options: LinkedRefreshOptions) {
  const scope = options.scope ?? 'global'
  const intervalMs = Math.max(options.intervalMs ?? DEFAULT_INTERVAL_MS, MIN_INTERVAL_MS)
  const immediate = options.immediate ?? true
  const enableAutoRefresh = options.enableAutoRefresh ?? false
  let timer: ReturnType<typeof setInterval> | null = null
  let channel: BroadcastChannel | null = null

  const triggerRefresh = async () => {
    if (typeof document !== 'undefined' && document.hidden) {
      return
    }

    await Promise.resolve(options.refresh())
  }

  const shouldRefresh = (targetScope?: string) => {
    if (!targetScope || targetScope === 'global') {
      return true
    }

    return scope === 'global' || scope === targetScope
  }

  const onChanged = (event: Event) => {
    const customEvent = event as CustomEvent<{ scope?: string }>
    if (!shouldRefresh(customEvent.detail?.scope)) {
      return
    }

    void triggerRefresh()
  }

  const onFocus = () => {
    void triggerRefresh()
  }

  const onVisibility = () => {
    if (!document.hidden) {
      void triggerRefresh()
    }
  }

  const onChannelMessage = (message: MessageEvent<{ scope?: string }>) => {
    if (!shouldRefresh(message.data?.scope)) {
      return
    }

    void triggerRefresh()
  }

  const notifyDataChanged = (targetScope = 'global') => {
    if (typeof window !== 'undefined') {
      window.dispatchEvent(new CustomEvent(EVENT_NAME, { detail: { scope: targetScope } }))
    }

    if (channel) {
      channel.postMessage({ scope: targetScope })
    }
  }

  onMounted(() => {
    if (typeof window === 'undefined') {
      return
    }

    if (!enableAutoRefresh) {
      return
    }

    window.addEventListener(EVENT_NAME, onChanged as EventListener)
    window.addEventListener('focus', onFocus)
    document.addEventListener('visibilitychange', onVisibility)

    if (typeof BroadcastChannel !== 'undefined') {
      channel = new BroadcastChannel(CHANNEL_NAME)
      channel.addEventListener('message', onChannelMessage)
    }

    timer = setInterval(() => {
      void triggerRefresh()
    }, intervalMs)

    if (immediate) {
      void triggerRefresh()
    }
  })

  onUnmounted(() => {
    if (typeof window !== 'undefined') {
      window.removeEventListener(EVENT_NAME, onChanged as EventListener)
      window.removeEventListener('focus', onFocus)
      document.removeEventListener('visibilitychange', onVisibility)
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
