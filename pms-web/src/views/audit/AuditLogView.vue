<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">操作日志</h2>
        <div class="page-subtitle">记录系统中的关键操作，支持按模块、操作类型、操作人和时间范围筛选</div>
      </div>
    </div>

    <el-row :gutter="12" class="stats-row">
      <el-col :xs="24" :sm="8" :md="4">
        <el-card shadow="never" class="stat-card">
          <div class="t">总记录</div>
          <div class="v">{{ summary.total }}</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="8" :md="4">
        <el-card shadow="never" class="stat-card">
          <div class="t">今日操作</div>
          <div class="v primary">{{ summary.todayCount }}</div>
        </el-card>
      </el-col>
      <el-col v-for="(count, action) in summary.actionCounts" :key="action" :xs="24" :sm="8" :md="4">
        <el-card shadow="never" class="stat-card" @click="filterByAction(action as string)">
          <div class="t">{{ actionLabel(action as string) }}</div>
          <div class="v">{{ count }}</div>
        </el-card>
      </el-col>
    </el-row>

    <AppFilterCard>
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="操作类型">
          <el-select v-model="query.action" clearable placeholder="全部" style="width: 130px">
            <el-option v-for="opt in actionOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="模块">
          <el-select v-model="query.module" clearable placeholder="全部" style="width: 130px">
            <el-option v-for="opt in moduleOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="操作人">
          <el-input v-model="query.operatorName" clearable placeholder="姓名" style="width: 150px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item label="时间范围">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            style="width: 260px"
            @change="onDateRangeChange"
          />
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>
    </AppFilterCard>

    <AppTableCard>
      <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无操作日志">
        <el-table-column prop="createdAt" label="时间" width="170">
          <template #default="scope">
            {{ formatDateTime(scope.row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column prop="operator" label="操作人" width="120" show-overflow-tooltip />
        <el-table-column prop="action" label="操作类型" width="110">
          <template #default="scope">
            <el-tag :type="actionTagType(scope.row.action)" size="small">{{ actionLabel(scope.row.action) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="module" label="模块" width="110">
          <template #default="scope">
            {{ moduleLabel(scope.row.module) }}
          </template>
        </el-table-column>
        <el-table-column prop="target" label="操作目标" min-width="200" show-overflow-tooltip />
        <el-table-column prop="detail" label="详情" min-width="260" show-overflow-tooltip />
        <el-table-column prop="ipAddress" label="IP地址" width="140" show-overflow-tooltip />
      </el-table>

      <div class="pager">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.size"
          :page-sizes="PAGE_SIZES"
          layout="total, sizes, prev, pager, next"
          :total="total"
          @size-change="(size: number) => { query.size = size; query.page = 1; loadData() }"
          @current-change="(page: number) => { query.page = page; loadData() }"
        />
      </div>
    </AppTableCard>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { fetchAuditLogs, fetchAuditLogSummary } from '../../api/modules/auditLog'
import type { AuditLogItem, AuditLogSummary } from '../../types/auditLog'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { DEFAULT_PAGE_SIZE, PAGE_SIZES } from '../../constants/tableConfig'
import AppFilterCard from '../../components/AppFilterCard.vue'
import AppTableCard from '../../components/AppTableCard.vue'

const loading = ref(false)
const tableData = ref<AuditLogItem[]>([])
const total = ref(0)
const dateRange = ref<[string, string] | null>(null)

const summary = reactive<AuditLogSummary>({
  total: 0,
  todayCount: 0,
  actionCounts: {}
})

const query = reactive({
  action: '',
  module: '',
  operatorName: '',
  startDate: '',
  endDate: '',
  page: 1,
  size: DEFAULT_PAGE_SIZE
})

type AuditLogFilterState = Pick<typeof query, 'action' | 'module' | 'operatorName' | 'startDate' | 'endDate'>

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<AuditLogFilterState>({
  key: 'audit-log',
  getState: () => ({
    action: query.action,
    module: query.module,
    operatorName: query.operatorName,
    startDate: query.startDate,
    endDate: query.endDate
  }),
  applyState: (s) => {
    query.action = s.action ?? ''
    query.module = s.module ?? ''
    query.operatorName = s.operatorName ?? ''
    query.startDate = s.startDate ?? ''
    query.endDate = s.endDate ?? ''
    if (query.startDate && query.endDate) {
      dateRange.value = [query.startDate, query.endDate]
    }
  }
})

const { runInitialLoad } = useResilientLoad()

const actionOptions = [
  { label: '新增', value: 'create' },
  { label: '修改', value: 'update' },
  { label: '删除', value: 'delete' },
  { label: '导出', value: 'export' },
  { label: '导入', value: 'import' },
  { label: '登录', value: 'login' },
  { label: '批量删除', value: 'batch-delete' },
]

const moduleOptions = [
  { label: '项目', value: 'project' },
  { label: '产品', value: 'product' },
  { label: '医院', value: 'hospital' },
  { label: '报修', value: 'repair' },
  { label: '工时', value: 'workhours' },
  { label: '交接', value: 'handover' },
  { label: '巡检', value: 'inspection' },
  { label: '年度报告', value: 'annual-report' },
  { label: '重大需求', value: 'major-demand' },
  { label: '月度报告', value: 'monthly-report' },
  { label: '权限', value: 'permission' },
  { label: '系统', value: 'system' },
]

const actionLabelMap: Record<string, string> = {
  create: '新增',
  update: '修改',
  delete: '删除',
  export: '导出',
  import: '导入',
  login: '登录',
  'batch-delete': '批量删除',
}

const moduleLabelMap: Record<string, string> = Object.fromEntries(moduleOptions.map(o => [o.value, o.label]))

const actionLabel = (action: string) => actionLabelMap[action] ?? action
const moduleLabel = (mod: string) => moduleLabelMap[mod] ?? mod

const actionTagType = (action: string): '' | 'success' | 'info' | 'warning' | 'danger' => {
  switch (action) {
    case 'create': return 'success'
    case 'update': return 'warning'
    case 'delete':
    case 'batch-delete': return 'danger'
    case 'export': return 'info'
    case 'import': return ''
    case 'login': return 'info'
    default: return 'info'
  }
}

const formatDateTime = (dateStr: string) => {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

const onDateRangeChange = (val: [string, string] | null) => {
  if (val) {
    query.startDate = val[0]
    query.endDate = val[1]
  } else {
    query.startDate = ''
    query.endDate = ''
  }
}

const filterByAction = (action: string) => {
  query.action = action
  query.page = 1
  loadData()
}

const loadData = async () => {
  loading.value = true
  try {
    const params: Record<string, any> = {
      page: query.page,
      size: query.size
    }
    if (query.action) params.action = query.action
    if (query.module) params.module = query.module
    if (query.operatorName) params.operatorName = query.operatorName
    if (query.startDate) params.startDate = query.startDate
    if (query.endDate) params.endDate = query.endDate

    const res = await fetchAuditLogs(params)
    tableData.value = res.data?.items ?? []
    total.value = res.data?.total ?? 0
  } catch {
    tableData.value = []
    total.value = 0
  } finally {
    loading.value = false
  }
}

const loadSummary = async () => {
  try {
    const res = await fetchAuditLogSummary()
    if (res.data) {
      summary.total = res.data.total
      summary.todayCount = res.data.todayCount
      summary.actionCounts = res.data.actionCounts ?? {}
    }
  } catch {
    // silent
  }
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.action = ''
  query.module = ''
  query.operatorName = ''
  query.startDate = ''
  query.endDate = ''
  query.page = 1
  dateRange.value = null
  clearFilterState()
  loadData()
}

onMounted(async () => {
  restoreFilterState()
  await runInitialLoad({
    tasks: [loadData, loadSummary]
  })
})
</script>

<style scoped>
.stats-row { margin-bottom: 12px; }
.stat-card { cursor: pointer; text-align: center; }
.stat-card .t { font-size: 13px; color: #888; }
.stat-card .v { font-size: 22px; font-weight: 700; margin-top: 4px; }
.stat-card .v.primary { color: var(--el-color-primary); }
.stat-card .v.danger { color: var(--el-color-danger); }
.pager { margin-top: 12px; display: flex; justify-content: flex-end; }
</style>
