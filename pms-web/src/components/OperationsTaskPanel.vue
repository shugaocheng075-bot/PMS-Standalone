<template>
  <section class="operations-task-panel" v-loading="loading">
    <div class="operations-task-head">
      <div class="operations-task-copy">
        <span class="operations-task-kicker">Operations Task Hub</span>
        <h3 class="operations-task-title">{{ title }}</h3>
        <p class="operations-task-subtitle">{{ subtitle }}</p>
      </div>
      <div class="operations-task-actions">
        <el-tag effect="plain" type="info">共 {{ resolvedSummary.total }} 项</el-tag>
        <el-tag v-if="activeFilterCount > 0" effect="plain" type="primary">已筛选 {{ activeFilterCount }} 项</el-tag>
        <slot name="actions" />
      </div>
    </div>

    <SummaryMetrics :items="summaryCards" :columns="5" @select="onSummaryCardSelect" />

    <AppTableCard>
      <template #header>
        <div class="panel-head">
          <div class="panel-copy">
            <span class="panel-title">任务列表</span>
            <span class="panel-subtitle">支持按级别、来源、负责人、医院和关键字收口，再跳到对应业务页继续推进</span>
          </div>
          <span class="panel-caption">{{ tasks.length }} / {{ resolvedSummary.total }}</span>
        </div>
      </template>

      <div class="task-filter-bar">
        <el-select v-model="draftQuery.level" clearable placeholder="级别" class="task-filter-field" @change="applyFilters">
          <el-option v-for="option in levelOptions" :key="option.value" :label="option.label" :value="option.value" />
        </el-select>
        <el-select v-model="draftQuery.source" clearable placeholder="来源" class="task-filter-field" @change="applyFilters">
          <el-option v-for="option in sourceOptions" :key="option.value" :label="option.label" :value="option.value" />
        </el-select>
        <el-input
          v-model="draftQuery.owner"
          clearable
          placeholder="负责人"
          class="task-filter-field"
          @keyup.enter="applyFilters"
          @clear="applyFilters"
        />
        <el-input
          v-model="draftQuery.hospitalName"
          clearable
          placeholder="医院"
          class="task-filter-field"
          @keyup.enter="applyFilters"
          @clear="applyFilters"
        />
        <el-input
          v-model="draftQuery.keyword"
          clearable
          placeholder="搜索医院、标题、负责人、产品"
          class="task-filter-search"
          @keyup.enter="applyFilters"
          @clear="applyFilters"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
        <div class="task-filter-actions">
          <el-button type="primary" plain icon="Search" @click="applyFilters">筛选</el-button>
          <el-button icon="Refresh" @click="emit('refresh')">刷新</el-button>
          <el-button v-if="activeFilterCount > 0" link type="primary" @click="resetFilters">重置</el-button>
        </div>
      </div>

      <div v-if="activeFilterLabel" class="task-filter-summary">
        <span>当前筛选：</span>
        <strong>{{ activeFilterLabel }}</strong>
      </div>

      <el-table
        :data="tasks"
        stripe
        border
        max-height="520"
        scrollbar-always-on
        :empty-text="emptyText"
        @row-dblclick="onTaskGoto"
      >
        <el-table-column prop="level" label="级别" width="92">
          <template #default="scope">
            <el-tag :type="levelTagType(scope.row.level)" effect="plain">
              {{ scope.row.level || '-' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="source" label="来源" width="120">
          <template #default="scope">
            <el-tag :type="sourceTagType(scope.row.source)" effect="plain">
              {{ formatSourceLabel(scope.row.source) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="owner" label="负责人" width="120" show-overflow-tooltip />
        <el-table-column prop="hospitalName" label="医院" min-width="180" show-overflow-tooltip />
        <el-table-column prop="title" label="标题" min-width="220" show-overflow-tooltip />
        <el-table-column prop="detail" label="说明" min-width="220" show-overflow-tooltip />
        <el-table-column prop="dueAt" label="计划日期" width="116" />
        <el-table-column prop="overdueDays" label="逾期天数" width="110" align="right">
          <template #default="scope">
            <el-tag :type="scope.row.overdueDays > 0 ? 'danger' : 'info'" effect="plain">
              {{ formatOverdueDays(scope.row.overdueDays) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="scope">
            <el-button link type="primary" @click="onTaskGoto(scope.row)" icon="ArrowRight">处理</el-button>
          </template>
        </el-table-column>
      </el-table>
    </AppTableCard>
  </section>
</template>

<script setup lang="ts">
import { Search } from '@element-plus/icons-vue'
import { computed, reactive, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import AppTableCard from './AppTableCard.vue'
import SummaryMetrics from './SummaryMetrics.vue'
import type { OperationsTaskItem, OperationsTaskQuery, OperationsTaskSummary } from '../types/operations'

type SummaryMetricCard = {
  key: string
  title: string
  value: string | number
  context?: string
  note?: string
  color?: string
  active?: boolean
  clickable?: boolean
}

type SummaryMetricSelectItem = {
  key?: string | number
}

type FilterOption = {
  label: string
  value: string
}

const props = withDefaults(defineProps<{
  summary: OperationsTaskSummary
  tasks: OperationsTaskItem[]
  query?: OperationsTaskQuery
  loading?: boolean
  title?: string
  subtitle?: string
  emptyText?: string
}>(), {
  summary: () => ({ total: 0, severe: 0, warning: 0, reminder: 0, overdue: 0 }),
  tasks: () => [],
  query: () => ({}),
  loading: false,
  title: '运维任务聚合',
  subtitle: '汇总待处理任务、风险级别和业务跳转入口，让首页和工作台真正变成作战面板。',
  emptyText: '暂无符合条件的运维任务',
})

const emit = defineEmits<{
  refresh: []
  'query-change': [query: OperationsTaskQuery]
  'reset-query': []
}>()

const router = useRouter()
const draftQuery = reactive<OperationsTaskQuery>({
  level: '',
  source: '',
  owner: '',
  hospitalName: '',
  keyword: '',
})

const levelOptions: FilterOption[] = [
  { label: '严重', value: '严重' },
  { label: '警告', value: '警告' },
  { label: '提醒', value: '提醒' },
]

const sourceOptions: FilterOption[] = [
  { label: '合同风险', value: 'contract' },
  { label: '报修', value: 'repair' },
  { label: '巡检', value: 'inspection' },
  { label: '交接', value: 'handover' },
  { label: '重大需求', value: 'majorDemand' },
  { label: '年报', value: 'annualReport' },
  { label: '月报', value: 'monthlyReport' },
  { label: '数据质量', value: 'dataQuality' },
]

watch(
  () => props.query,
  (query) => {
    draftQuery.level = query?.level ?? ''
    draftQuery.source = query?.source ?? ''
    draftQuery.owner = query?.owner ?? ''
    draftQuery.hospitalName = query?.hospitalName ?? ''
    draftQuery.keyword = query?.keyword ?? ''
  },
  { immediate: true, deep: true },
)

const resolvedSummary = computed(() => ({
  total: props.summary.total ?? props.tasks.length,
  severe: props.summary.severe ?? 0,
  warning: props.summary.warning ?? 0,
  reminder: props.summary.reminder ?? 0,
  overdue: props.summary.overdue ?? 0,
}))

const activeLevel = computed(() => props.query?.level?.trim() ?? '')
const activeFilterCount = computed(() =>
  [props.query?.level, props.query?.source, props.query?.owner, props.query?.hospitalName, props.query?.keyword]
    .filter((value) => typeof value === 'string' && value.trim().length > 0)
    .length,
)

const activeFilterLabel = computed(() => {
  const labels: string[] = []

  if (props.query?.level) {
    labels.push(`级别：${props.query.level}`)
  }

  if (props.query?.source) {
    labels.push(`来源：${formatSourceLabel(props.query.source)}`)
  }

  if (props.query?.owner) {
    labels.push(`负责人：${props.query.owner}`)
  }

  if (props.query?.hospitalName) {
    labels.push(`医院：${props.query.hospitalName}`)
  }

  if (props.query?.keyword) {
    labels.push(`关键字：${props.query.keyword}`)
  }

  return labels.join(' / ')
})

const summaryCards = computed<SummaryMetricCard[]>(() => [
  {
    key: 'all',
    title: '全部任务',
    value: resolvedSummary.value.total,
    context: '任务总览',
    note: '当前筛选口径下的全部运维任务',
    color: '#3f4f63',
    active: !activeLevel.value,
  },
  {
    key: '严重',
    title: '严重',
    value: resolvedSummary.value.severe,
    context: '级别聚合',
    note: '优先处理的高风险任务',
    color: '#c58a87',
    active: activeLevel.value === '严重',
  },
  {
    key: '警告',
    title: '警告',
    value: resolvedSummary.value.warning,
    context: '级别聚合',
    note: '需要尽快推进的任务',
    color: '#c7a06c',
    active: activeLevel.value === '警告',
  },
  {
    key: '提醒',
    title: '提醒',
    value: resolvedSummary.value.reminder,
    context: '级别聚合',
    note: '常规跟进类任务',
    color: '#7c98bc',
    active: activeLevel.value === '提醒',
  },
  {
    key: 'overdue',
    title: '逾期',
    value: resolvedSummary.value.overdue,
    context: '风险聚合',
    note: '已经超过计划完成时间的任务',
    color: '#8db3a8',
    clickable: false,
  },
])

const levelTagType = (level: string) => {
  if (level === '严重') return 'danger'
  if (level === '警告') return 'warning'
  if (level === '提醒') return 'info'
  return 'info'
}

const sourceTagType = (source: string) => {
  if (source === 'contract') return 'warning'
  if (source === 'handover') return 'success'
  if (source === 'inspection') return 'info'
  if (source === 'repair' || source === 'dataQuality') return 'danger'
  return 'info'
}

function formatSourceLabel(source: string) {
  const map: Record<string, string> = {
    contract: '合同风险',
    repair: '报修',
    inspection: '巡检',
    handover: '交接',
    majorDemand: '重大需求',
    annualReport: '年报',
    monthlyReport: '月报',
    dataQuality: '数据质量',
  }

  return map[source] ?? source ?? '-'
}

const formatOverdueDays = (value: number) => {
  const days = Number(value || 0)
  if (!Number.isFinite(days) || days <= 0) {
    return '0 天'
  }

  return `${days} 天`
}

const buildRouteQuery = (relatedQuery: Record<string, string>) =>
  Object.fromEntries(
    Object.entries(relatedQuery || {})
      .filter(([, value]) => typeof value === 'string' && value.trim().length > 0)
      .map(([key, value]) => [key, value.trim()]),
  )

const sanitizeQuery = () => {
  const nextQuery: OperationsTaskQuery = {}

  if (draftQuery.level?.trim()) {
    nextQuery.level = draftQuery.level.trim()
  }

  if (draftQuery.source?.trim()) {
    nextQuery.source = draftQuery.source.trim()
  }

  if (draftQuery.owner?.trim()) {
    nextQuery.owner = draftQuery.owner.trim()
  }

  if (draftQuery.hospitalName?.trim()) {
    nextQuery.hospitalName = draftQuery.hospitalName.trim()
  }

  if (draftQuery.keyword?.trim()) {
    nextQuery.keyword = draftQuery.keyword.trim()
  }

  return nextQuery
}

const applyFilters = () => {
  emit('query-change', sanitizeQuery())
}

const resetFilters = () => {
  draftQuery.level = ''
  draftQuery.source = ''
  draftQuery.owner = ''
  draftQuery.hospitalName = ''
  draftQuery.keyword = ''
  emit('reset-query')
}

const onSummaryCardSelect = (item: SummaryMetricSelectItem) => {
  if (typeof item.key !== 'string' || item.key === 'overdue') {
    return
  }

  draftQuery.level = item.key === 'all' ? '' : item.key
  applyFilters()
}

const onTaskGoto = (row: OperationsTaskItem) => {
  if (!row.relatedPath) {
    ElMessage.warning('该任务未配置关联跳转')
    return
  }

  void router.push({
    path: row.relatedPath,
    query: buildRouteQuery(row.relatedQuery),
  })
}
</script>

<style scoped>
.operations-task-panel {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.operations-task-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.operations-task-copy {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}

.operations-task-kicker {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #7c98bc;
}

.operations-task-title {
  margin: 0;
  font-size: 20px;
  line-height: 1.2;
  font-weight: 700;
  color: #1f2937;
}

.operations-task-subtitle {
  margin: 0;
  max-width: 760px;
  font-size: 13px;
  line-height: 1.7;
  color: #64748b;
}

.operations-task-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.panel-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14px;
}

.panel-copy {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.panel-title {
  font-weight: 700;
  color: #1f2937;
}

.panel-subtitle {
  font-size: 12px;
  line-height: 1.5;
  color: #7b8794;
}

.panel-caption {
  display: inline-flex;
  align-items: center;
  padding: 4px 10px;
  border-radius: 999px;
  background: #f8fafc;
  color: #64748b;
  font-size: 12px;
  font-weight: 600;
}

.task-filter-bar {
  display: grid;
  grid-template-columns: 132px 148px minmax(140px, 0.8fr) minmax(180px, 1fr) minmax(240px, 1.2fr) auto;
  gap: 12px;
  align-items: center;
  margin-bottom: 14px;
}

.task-filter-field,
.task-filter-search {
  width: 100%;
}

.task-filter-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
  flex-wrap: wrap;
}

.task-filter-summary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 12px;
  padding: 6px 10px;
  border-radius: 999px;
  background: #f8fafc;
  color: #64748b;
  font-size: 12px;
  line-height: 1.4;
}

@media (max-width: 1280px) {
  .operations-task-head {
    flex-direction: column;
  }

  .task-filter-bar {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .task-filter-actions {
    justify-content: flex-start;
    grid-column: 1 / -1;
  }
}

@media (max-width: 960px) {
  .task-filter-bar {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 768px) {
  .operations-task-title {
    font-size: 18px;
  }

  .operations-task-actions {
    width: 100%;
  }

  .operations-task-actions :deep(.el-button),
  .operations-task-actions :deep(.el-tag) {
    width: 100%;
    justify-content: center;
  }

  .task-filter-bar {
    grid-template-columns: 1fr;
  }

  .task-filter-actions {
    width: 100%;
  }

  .task-filter-actions :deep(.el-button) {
    flex: 1 1 auto;
  }
}
</style>
