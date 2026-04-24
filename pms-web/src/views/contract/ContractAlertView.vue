<template>
  <div class="page-shell">
    <div class="contract-hero">
      <div class="contract-hero-main">
        <div class="contract-hero-kicker-row">
          <span class="contract-hero-kicker">Contract Risk Console</span>
          <span class="contract-hero-badge">{{ activeFilterLabel }}</span>
        </div>
        <h2 class="contract-hero-title">合同预警</h2>
        <div class="contract-hero-subtitle">按超期风险分级管理合同跟进任务，先在首屏判断风险浓度与区域分布，再进入明细列表推进签约与维护闭环。</div>

        <div class="contract-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="contract-signal-card">
            <span class="contract-signal-label">{{ item.label }}</span>
            <strong class="contract-signal-value">{{ item.value }}</strong>
            <span class="contract-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="contract-hero-side">
        <div class="contract-control-card">
          <div class="contract-control-copy">
            <span class="contract-control-title">风险操作</span>
            <span class="contract-control-note">当前共 {{ total }} 条预警记录，可直接筛选、重置或导出。</span>
          </div>
          <div class="contract-control-actions">
            <el-button size="small" @click="onSearch" icon="Search">刷新列表</el-button>
            <el-button size="small" @click="onReset" icon="Refresh">重置筛选</el-button>
            <el-button size="small" :loading="exporting" @click="onExport" icon="Download">导出</el-button>
          </div>
        </div>

        <div class="contract-quick-grid">
          <button v-for="action in quickActions" :key="action.title" type="button" class="contract-quick-action" @click="action.onClick()">
            <span class="contract-quick-title">{{ action.title }}</span>
            <span class="contract-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <SummaryMetrics :items="summaryCards" :columns="4" @select="onSummaryCardSelect" />

    

    <ProTable
      title="明细数据"
      :data="tableData"
      :loading="loading"
      :total="total"
      v-model:page="query.page"
      v-model:size="query.size"
      @refresh="loadData"
      @pagination-change="loadData"
      stripe
      row-key="id"
      empty-text="暂无符合条件的数据"
            
    >
      <template #toolbar>
        <el-button :loading="exporting" @click="onExport" icon="Download">导出Excel</el-button>
      </template>

      <template #search>
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="预警级别">
          <el-select v-model="query.alertLevel" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="level in alertLevelOptions" :key="level" :label="level" :value="level" />
          </el-select>
        </el-form-item>
        <el-form-item label="省份">
          <el-select v-model="query.province" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="province in provinceOptions" :key="province" :label="province" :value="province" />
          </el-select>
        </el-form-item>
        <el-form-item label="组别">
          <el-select v-model="query.groupName" placeholder="全部" clearable style="width: 160px">
            <el-option v-for="group in filteredGroupOptions" :key="group" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="销售">
          <el-input v-model="query.salesName" placeholder="请输入销售" clearable style="width: 160px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
        </el-form-item>
      </el-form>
    </template>

    

        <el-table-column prop="hospitalName" label="医院" min-width="240" show-overflow-tooltip sortable />
        <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip />
        <el-table-column prop="groupName" label="组别" width="140" show-overflow-tooltip />
        <el-table-column prop="salesName" label="销售" width="130" show-overflow-tooltip />
        <el-table-column prop="contractStatus" label="合同状态" min-width="190" show-overflow-tooltip />
        <el-table-column prop="maintenanceAmount" label="维护金额(万)" width="140" align="right" sortable />
        <el-table-column prop="overdueDays" label="超期天数" width="130" align="right" sortable>
          <template #default="scope">
            <span :class="overdueDaysClass(scope.row.overdueDays)">超期{{ scope.row.overdueDays }}天</span>
          </template>
        </el-table-column>
        <el-table-column prop="alertLevel" label="预警级别" width="120" sortable>
          <template #default="scope">
            <el-tag :type="tagType(scope.row.alertLevel)">{{ scope.row.alertLevel }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="132" fixed="right">
          <template #default="scope">
            <el-button link type="primary" @click="onGotoProject(scope.row)" icon="Service">去处理</el-button>
          </template>
        </el-table-column>



    
    </ProTable>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { fetchContractAlerts, fetchContractAlertSummary, exportContractAlerts } from '../../api/modules/contract'
import type { ContractAlertItem, ContractAlertSummary } from '../../types/contract'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import ProTable from '../../components/ProTable.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'

const loading = ref(false)
const exporting = ref(false)
const total = ref(0)
const tableData = ref<ContractAlertItem[]>([])
const summary = ref<ContractAlertSummary>({ reminderCount: 0, warningCount: 0, criticalCount: 0, total: 0 })

const query = reactive({
  alertLevel: '',
  province: '',
  groupName: '',
  salesName: '',
  page: 1,
  size: 15,
})
const route = useRoute()
const router = useRouter()

const readRouteQueryValue = (value: unknown): string => {
  if (typeof value === 'string') {
    return value
  }

  if (Array.isArray(value) && typeof value[0] === 'string') {
    return value[0]
  }

  return ''
}

const updateRouteQuery = async (patch: Record<string, string | undefined>) => {
  const nextQuery = { ...route.query }
  Object.entries(patch).forEach(([key, value]) => {
    if (value) {
      nextQuery[key] = value
      return
    }

    delete nextQuery[key]
  })

  await router.replace({ path: route.path, query: nextQuery })
}

const applyDrillQuery = () => {
  const alertLevel = readRouteQueryValue(route.query.alertLevel)
  const province = readRouteQueryValue(route.query.province)
  const groupName = readRouteQueryValue(route.query.groupName)
  const salesName = readRouteQueryValue(route.query.salesName)
  let applied = false

  if (province) {
    query.province = province
    applied = true
  }

  if (groupName) {
    query.groupName = groupName
    applied = true
  }

  if (salesName) {
    query.salesName = salesName
    applied = true
  }

  if (!alertLevel) {
    if (applied) {
      query.page = 1
    }
    return
  }

  query.province = ''
  query.groupName = ''
  query.salesName = ''
  query.alertLevel = alertLevel
  query.page = 1
}

type ContractFilterState = {
  alertLevel: string
  province: string
  groupName: string
  salesName: string
  page: number
  size: number
}

const alertLevelOptions = ref<string[]>(['提醒', '警告', '严重'])
const provinceOptions = ref<string[]>([...PROVINCE_OPTIONS])
const groupOptionsByProvince = ref<Record<string, string[]>>({})

const allGroupOptions = computed(() => {
  const groups = Object.values(groupOptionsByProvince.value).flat()
  return groups.length > 0 ? Array.from(new Set(groups)) : GROUP_OPTIONS
})

const filteredGroupOptions = computed(() => {
  if (!query.province) {
    return allGroupOptions.value
  }

  const groups = groupOptionsByProvince.value[query.province]
  return groups && groups.length > 0 ? groups : allGroupOptions.value
})

type ContractSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active: boolean
}

const summaryCards = computed<ContractSummaryCard[]>(() => [
  {
    key: '提醒',
    title: '提醒',
    value: summary.value.reminderCount,
    context: '预警级别',
    note: '查看即将到期的合同跟进项',
    color: '#7c98bc',
    active: query.alertLevel === '提醒',
  },
  {
    key: '警告',
    title: '警告',
    value: summary.value.warningCount,
    context: '预警级别',
    note: '聚焦已进入重点跟进窗口的合同',
    color: '#c7a06c',
    active: query.alertLevel === '警告',
  },
  {
    key: '严重',
    title: '严重',
    value: summary.value.criticalCount,
    context: '预警级别',
    note: '优先处理高风险超期合同',
    color: '#c58a87',
    active: query.alertLevel === '严重',
  },
  {
    key: 'all',
    title: '全部',
    value: summary.value.total,
    context: '全量视图',
    note: '返回全部合同预警的总览列表',
    color: '#3f4f63',
    active: query.alertLevel === '',
  },
])

const activeFilterLabel = computed(() => {
  if (query.alertLevel) {
    return `级别：${query.alertLevel}`
  }

  if (query.province) {
    return `省份：${query.province}`
  }

  if (query.groupName) {
    return `组别：${query.groupName}`
  }

  if (query.salesName) {
    return `销售：${query.salesName}`
  }

  return '全量风险视图'
})

const heroSignals = computed(() => {
  const highRisk = summary.value.criticalCount + summary.value.warningCount
  const provinceCount = new Set(tableData.value.map((item) => item.province).filter(Boolean)).size
  const groupCount = new Set(tableData.value.map((item) => item.groupName).filter(Boolean)).size

  return [
    {
      label: '风险总量',
      value: String(summary.value.total),
      note: '当前范围内全部合同预警记录',
    },
    {
      label: '高风险项目',
      value: String(highRisk),
      note: '警告与严重级别的合同风险总数',
    },
    {
      label: '严重风险',
      value: String(summary.value.criticalCount),
      note: '优先推进签约或维护处理的条目',
    },
    {
      label: '覆盖范围',
      value: `${provinceCount}/${groupCount}`,
      note: '当前列表省份数 / 组别数',
    },
  ]
})

const quickActions = computed(() => [
  {
    title: '严重预警',
    note: '直接锁定最高优先级超期合同',
    onClick: () => onStatClick('严重'),
  },
  {
    title: '警告预警',
    note: '聚焦临近阈值的重点跟进项目',
    onClick: () => onStatClick('警告'),
  },
  {
    title: '提醒预警',
    note: '提前处理即将到期的常规合同',
    onClick: () => onStatClick('提醒'),
  },
  {
    title: '全部恢复',
    note: '回到全量合同预警总览',
    onClick: () => onStatClick(''),
  },
])

watch(
  () => query.province,
  () => {
    if (query.groupName && !filteredGroupOptions.value.includes(query.groupName)) {
      query.groupName = ''
    }
  },
)

const loadFilterOptions = async () => {
  try {
    const res = await fetchContractAlerts({ page: 1, size: 100000 })
    const items = res.data.items

    if (!items.length) {
      return
    }

    const provinces = Array.from(new Set(items.map((item) => item.province).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (provinces.length > 0) {
      provinceOptions.value = provinces
    }

    const alertLevels = Array.from(new Set(items.map((item) => item.alertLevel).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (alertLevels.length > 0) {
      alertLevelOptions.value = Array.from(new Set([...alertLevelOptions.value, ...alertLevels]))
    }

    const map: Record<string, string[]> = {}
    for (const item of items) {
      if (!item.province || !item.groupName) continue

      const province = item.province
      const groupName = item.groupName
      const groups = map[province] ?? (map[province] = [])

      if (!groups.includes(groupName)) {
        groups.push(groupName)
      }
    }

    groupOptionsByProvince.value = Object.fromEntries(
      Object.entries(map).map(([province, groups]) => [province, groups.sort((a, b) => a.localeCompare(b, 'zh-CN'))]),
    )
  } catch {
  }
}
const { runInitialLoad } = useResilientLoad()

const tagType = (level: string) => {
  if (level === '严重') return 'danger'
  if (level === '警告') return 'warning'
  return 'info'
}

const overdueDaysClass = (overdueDays: number) => {
  if (overdueDays > 90) {
    return 'text-danger'
  }

  if (overdueDays >= 30) {
    return 'text-warning'
  }

  return 'text-info'
}

const loadSummary = async () => {
  try {
    const res = await fetchContractAlertSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载合同预警汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchContractAlerts(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载合同预警列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onStatClick = (alertLevel: string) => {
  query.province = ''
  query.groupName = ''
  query.salesName = ''
  query.alertLevel = alertLevel
  query.page = 1
  void updateRouteQuery({ alertLevel: undefined, province: undefined, groupName: undefined, salesName: undefined })
  loadData()
}

const onSummaryCardSelect = (card: { key?: string | number }) => {
  if (typeof card.key !== 'string') {
    return
  }

  onStatClick(card.key === 'all' ? '' : card.key)
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.alertLevel = ''
  query.province = ''
  query.groupName = ''
  query.salesName = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  void updateRouteQuery({ alertLevel: undefined, province: undefined, groupName: undefined, salesName: undefined })
  loadData()
}

const onExport = async () => {
  exporting.value = true
  try {
    const blob = await exportContractAlerts({
      alertLevel: query.alertLevel || undefined,
      province: query.province || undefined,
      groupName: query.groupName || undefined,
      salesName: query.salesName || undefined,
    })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `合同预警_${Date.now()}.xlsx`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出合同预警失败，请稍后重试'))
  } finally {
    exporting.value = false
  }
}

const onGotoProject = (row: ContractAlertItem) => {
  void router.push({
    path: '/project/list',
    query: {
      hospitalName: row.hospitalName,
      groupName: row.groupName,
      salesName: row.salesName,
      contractStatus: row.contractStatus,
      action: 'edit',
    },
  })
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<ContractFilterState>({
  key: 'contract-alert',
  getState: () => ({
    alertLevel: query.alertLevel,
    province: query.province,
    groupName: query.groupName,
    salesName: query.salesName,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.alertLevel = state.alertLevel ?? ''
    query.province = state.province ?? ''
    query.groupName = state.groupName ?? ''
    query.salesName = state.salesName ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

const refreshLinkedData = async () => {
  await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
}

useLinkedRealtimeRefresh({
  refresh: refreshLinkedData,
  scope: 'contract',
  intervalMs: 60000,
})

onMounted(async () => {
  restoreFilterState()
  applyDrillQuery()
  await runInitialLoad({
    tasks: [loadSummary, loadFilterOptions, loadData],
    retryChecks: [
      {
        when: () => summary.value.total > 0 && total.value === 0,
        task: loadData,
      },
    ],
  })
})

watch(() => route.fullPath, () => {
  applyDrillQuery()
  loadData()
})
</script>

<style scoped>
.contract-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.18fr) minmax(320px, 0.82fr);
  gap: 16px;
  padding: 24px;
  border-radius: 28px;
  color: #ffffff;
  background:
    radial-gradient(circle at 12% 18%, rgba(255, 194, 168, 0.18), transparent 22%),
    radial-gradient(circle at 100% 0, rgba(255, 233, 170, 0.14), transparent 24%),
    linear-gradient(145deg, #4d3654 0%, #724055 48%, #9a5d43 100%);
  box-shadow: 0 26px 44px rgba(84, 49, 61, 0.18);
}

.contract-hero-main {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-width: 0;
}

.contract-hero-kicker-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.contract-hero-kicker,
.contract-hero-badge {
  display: inline-flex;
  align-items: center;
  padding: 7px 12px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 700;
}

.contract-hero-kicker {
  border: 1px solid rgba(255, 255, 255, 0.18);
  background: rgba(255, 255, 255, 0.08);
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.contract-hero-badge {
  background: rgba(255, 255, 255, 0.14);
  color: #fef8ef;
}

.contract-hero-title {
  margin: 0;
  font-size: 34px;
  line-height: 1.12;
  font-weight: 700;
}

.contract-hero-subtitle {
  max-width: 700px;
  font-size: 14px;
  line-height: 1.85;
  color: rgba(250, 240, 231, 0.84);
}

.contract-hero-signals {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin-top: 4px;
}

.contract-signal-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.11), rgba(255, 255, 255, 0.05));
}

.contract-signal-label {
  font-size: 12px;
  color: rgba(248, 236, 226, 0.76);
}

.contract-signal-value {
  font-size: 28px;
  line-height: 1;
  font-weight: 700;
}

.contract-signal-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(248, 236, 226, 0.76);
}

.contract-hero-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.contract-control-card,
.contract-quick-action {
  border: 1px solid rgba(255, 255, 255, 0.14);
  border-radius: 20px;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.14), rgba(255, 255, 255, 0.08));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

.contract-control-card {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 18px;
}

.contract-control-copy {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.contract-control-title {
  font-size: 16px;
  font-weight: 700;
}

.contract-control-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(248, 236, 226, 0.74);
}

.contract-control-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.contract-control-actions :deep(.el-button) {
  min-height: 40px;
}

.contract-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.contract-quick-action {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  color: #ffffff;
  text-align: left;
  cursor: pointer;
  transition: transform 0.2s ease, border-color 0.2s ease, background 0.2s ease;
}

.contract-quick-action:hover {
  transform: translateY(-1px);
  border-color: rgba(255, 255, 255, 0.24);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.18), rgba(255, 255, 255, 0.09));
}

.contract-quick-title {
  font-size: 14px;
  font-weight: 700;
}

.contract-quick-note {
  font-size: 12px;
  line-height: 1.65;
  color: rgba(249, 239, 230, 0.76);
}

:deep(.el-table__fixed-right .cell) {
  white-space: nowrap;
}

@media (max-width: 1280px) {
  .contract-hero {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .contract-hero {
    padding: 18px;
  }

  .contract-hero-title {
    font-size: 28px;
  }

  .contract-hero-signals,
  .contract-quick-grid {
    grid-template-columns: 1fr;
  }

  .contract-control-actions {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
