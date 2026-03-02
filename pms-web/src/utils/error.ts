import axios from 'axios'

const NETWORK_ERROR_CODES = new Set(['ERR_NETWORK', 'ECONNABORTED', 'ETIMEDOUT'])

export function getErrorMessage(error: unknown, fallbackMessage: string) {
  if (axios.isAxiosError(error)) {
    const responseData = error.response?.data as { message?: string; error?: string } | undefined

    if (typeof responseData?.message === 'string' && responseData.message.trim()) {
      return responseData.message
    }

    if (typeof responseData?.error === 'string' && responseData.error.trim()) {
      return responseData.error
    }

    if (error.code && NETWORK_ERROR_CODES.has(error.code)) {
      return '网络连接异常，请检查服务是否启动或稍后重试'
    }

    if (error.response?.status === 404) {
      return '请求资源不存在，请刷新后重试'
    }

    if (error.response?.status && error.response.status >= 500) {
      return '服务端异常，请稍后重试'
    }
  }

  if (error instanceof Error && error.message.trim()) {
    return error.message
  }

  return fallbackMessage
}
