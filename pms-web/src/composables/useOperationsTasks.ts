import { computed, ref } from 'vue'
import { fetchOperationsTaskSummary, fetchOperationsTasks } from '../api/modules/operations'
import type { OperationsTaskItem, OperationsTaskQuery, OperationsTaskSummary } from '../types/operations'

export function useOperationsTasks(defaultSize = 8) {
  const loading = ref(false)
  const summary = ref<OperationsTaskSummary>({ total: 0, severe: 0, warning: 0, reminder: 0, overdue: 0 })
  const tasks = ref<OperationsTaskItem[]>([])
  const query = ref<OperationsTaskQuery>({})

  const normalizeQuery = (nextQuery: OperationsTaskQuery = {}) => {
    const normalized: OperationsTaskQuery = {}

    if (typeof nextQuery.level === 'string' && nextQuery.level.trim()) {
      normalized.level = nextQuery.level.trim()
    }

    if (typeof nextQuery.source === 'string' && nextQuery.source.trim()) {
      normalized.source = nextQuery.source.trim()
    }

    if (typeof nextQuery.owner === 'string' && nextQuery.owner.trim()) {
      normalized.owner = nextQuery.owner.trim()
    }

    if (typeof nextQuery.hospitalName === 'string' && nextQuery.hospitalName.trim()) {
      normalized.hospitalName = nextQuery.hospitalName.trim()
    }

    if (typeof nextQuery.keyword === 'string' && nextQuery.keyword.trim()) {
      normalized.keyword = nextQuery.keyword.trim()
    }

    return normalized
  }

  const loadOperationsTasks = async (nextQuery: OperationsTaskQuery = query.value) => {
    const normalizedQuery = normalizeQuery(nextQuery)
    query.value = normalizedQuery
    loading.value = true
    try {
      const [summaryRes, taskRes] = await Promise.all([
        fetchOperationsTaskSummary(normalizedQuery),
        fetchOperationsTasks({ page: 1, size: defaultSize, ...normalizedQuery }),
      ])

      summary.value = summaryRes.data
      tasks.value = taskRes.data.items ?? []
    } catch (error) {
      console.error('Failed to load operations tasks', error)
      summary.value = { total: 0, severe: 0, warning: 0, reminder: 0, overdue: 0 }
      tasks.value = []
    } finally {
      loading.value = false
    }
  }

  const applyQuery = async (patch: OperationsTaskQuery = {}) => {
    await loadOperationsTasks({
      ...query.value,
      ...patch,
    })
  }

  const resetQuery = async () => {
    await loadOperationsTasks({})
  }

  const activeFilterCount = computed(() =>
    Object.values(query.value).filter((value) => typeof value === 'string' && value.trim().length > 0).length,
  )

  return {
    loading,
    summary,
    tasks,
    query,
    activeFilterCount,
    loadOperationsTasks,
    applyQuery,
    resetQuery,
  }
}
