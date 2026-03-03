<template>
  <div class="page-shell dashboard-page">
    <div class="page-head">
      <div>
        <h2 class="page-title">首页</h2>
        <div class="page-subtitle">项目台账、重大需求、告警比例分析与下钻</div>
      </div>
      <el-button size="small" :loading="loading" @click="loadDashboard">刷新数据</el-button>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="6" v-for="card in cards" :key="card.title">
        <el-card shadow="never" class="stat-card stats-card">
          <div class="t">{{ card.title }}</div>
          <div class="v">{{ card.value }}</div>
        </el-card>
      </el-col>
    </el-row>

    <div class="chart-grid">
      <el-card shadow="never" class="table-card chart-card">
        <template #header>
          <div class="panel-head">
            <span class="panel-title">项目台账状态占比</span>
            <el-tag type="info">点击扇区下钻</el-tag>
          </div>
        </template>
        <div ref="projectChartRef" class="chart-box"></div>
      </el-card>

      <el-card shadow="never" class="table-card chart-card">
        <template #header>
          <div class="panel-head">
            <span class="panel-title">重大需求状态占比</span>
            <el-tag type="warning">点击扇区下钻</el-tag>
          </div>
        </template>
        <div ref="demandChartRef" class="chart-box"></div>
      </el-card>

      <el-card shadow="never" class="table-card chart-card">
        <template #header>
          <div class="panel-head">
            <span class="panel-title">告警等级占比</span>
            <el-tag type="danger">点击扇区下钻</el-tag>
          </div>
        </template>
        <div ref="alertChartRef" class="chart-box"></div>
      </el-card>
    </div>

    <el-card shadow="never" class="table-card">
      <template #header>
        <div class="panel-head">
          <span class="panel-title">下钻明细</span>
          <el-button link type="primary" @click="resetDrill">重置筛选</el-button>
        </div>
      </template>

      <el-tabs v-model="activeTab">
        <el-tab-pane label="项目台账" name="project">
          <div class="drill-tip">当前筛选：{{ selectedProjectStatus }}</div>
          <el-table :data="projectDrillRows" stripe border max-height="320" scrollbar-always-on empty-text="暂无项目明细">
            <el-table-column prop="hospitalName" label="医院" min-width="180" show-overflow-tooltip />
            <el-table-column prop="productName" label="产品" min-width="160" show-overflow-tooltip />
            <el-table-column prop="province" label="省份" width="110" />
            <el-table-column prop="contractStatus" label="状态" width="140" />
            <el-table-column prop="overdueDays" label="超期天数" width="110" align="right" />
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="重大需求" name="demand">
          <div class="drill-tip">当前筛选：{{ selectedDemandStatus }}</div>
          <el-table :data="demandDrillRows" stripe border max-height="320" scrollbar-always-on empty-text="暂无重大需求明细">
            <el-table-column prop="title" label="需求" min-width="220" show-overflow-tooltip />
            <el-table-column prop="hospital" label="医院/客户" min-width="180" show-overflow-tooltip />
            <el-table-column prop="owner" label="负责人" width="120" show-overflow-tooltip />
            <el-table-column prop="status" label="状态" width="120" />
            <el-table-column prop="dueDate" label="计划完成" width="140" />
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="告警" name="alert">
          <div class="drill-tip">当前筛选：{{ selectedAlertLevel }}</div>
          <el-table :data="alertDrillRows" stripe border max-height="320" scrollbar-always-on empty-text="暂无告警明细">
            <el-table-column prop="source" label="来源" width="90" />
            <el-table-column prop="level" label="等级" width="90" />
            <el-table-column prop="hospitalName" label="医院" min-width="180" show-overflow-tooltip />
            <el-table-column prop="title" label="标题" min-width="220" show-overflow-tooltip />
            <el-table-column prop="owner" label="责任人" width="110" show-overflow-tooltip />
            <el-table-column prop="overdueDays" label="超期天数" width="110" align="right" />
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import * as echarts from 'echarts'
import { ElMessage } from 'element-plus'
import { fetchProjectList } from '../../api/modules/project'
import { fetchMajorDemands, type MajorDemandSnapshot } from '../../api/modules/majorDemand'
import { fetchAlertCenter, type AlertCenterItem } from '../../api/modules/alertCenter'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

type ChartDatum = { name: string; value: number }
type ChartInstance = any

type DemandDrillItem = {
  rowId: string
  title: string
  hospital: string
  owner: string
  status: string
  dueDate: string
}

const loading = ref(false)
const demandLoading = ref(false)
const activeTab = ref<'project' | 'demand' | 'alert'>('project')
const demandLastLoadedAt = ref(0)
const DEMAND_REFRESH_INTERVAL = 5 * 60 * 1000

const selectedProjectStatus = ref('全部')
const selectedDemandStatus = ref('全部')
const selectedAlertLevel = ref('全部')

const projectItems = ref<any[]>([])
const majorSnapshot = ref<MajorDemandSnapshot | null>(null)
const alertItems = ref<AlertCenterItem[]>([])

const projectChartRef = ref<HTMLDivElement | null>(null)
const demandChartRef = ref<HTMLDivElement | null>(null)
const alertChartRef = ref<HTMLDivElement | null>(null)

const projectChart = ref<ChartInstance | null>(null)
const demandChart = ref<ChartInstance | null>(null)
const alertChart = ref<ChartInstance | null>(null)

const cards = computed(() => {
  const totalAlerts = alertItems.value.length
  const severeCount = alertItems.value.filter((item) => item.level === '严重').length
  const severeRatio = totalAlerts > 0 ? `${((severeCount / totalAlerts) * 100).toFixed(1)}%` : '0%'
  const overdueProjects = projectItems.value.filter((item) => Number(item.overdueDays || 0) > 0).length

  return [
    { title: '项目总数', value: String(projectItems.value.length) },
    { title: '重大需求', value: String((majorSnapshot.value?.workflows ?? []).length) },
    { title: '告警总数', value: String(totalAlerts) },
    { title: '严重告警占比', value: severeRatio },
    { title: '超期项目', value: String(overdueProjects) },
  ]
})

const demandDrillSource = computed<DemandDrillItem[]>(() => {
  const snapshot = majorSnapshot.value
  if (!snapshot) {
    return []
  }

  const rowById = new Map(snapshot.rows.map((row) => [String(row['行ID'] ?? row['rowId'] ?? row['ID'] ?? ''), row]))

  const pickByKeyword = (row: Record<string, string>, keywords: string[]) => {
    for (const [key, value] of Object.entries(row)) {
      if (!value) {
        continue
      }

      if (keywords.some((keyword) => key.toLowerCase().includes(keyword))) {
        return value
      }
    }
    return ''
  }

  return snapshot.workflows.map((workflow) => {
    const row = rowById.get(String(workflow.rowId)) ?? {}
    const title = pickByKeyword(row, ['需求', '标题', '事项']) || `需求-${workflow.rowId}`
    const hospital = pickByKeyword(row, ['医院', '客户']) || '--'
    const owner = workflow.owner || pickByKeyword(row, ['负责人', 'owner']) || '--'

    return {
      rowId: workflow.rowId,
      title,
      hospital,
      owner,
      status: workflow.status || pickByKeyword(row, ['状态']) || '未知',
      dueDate: workflow.dueDate || pickByKeyword(row, ['计划', '截止']) || '--',
    }
  })
})

const projectStatusData = computed<ChartDatum[]>(() => {
  const counter = new Map<string, number>()
  for (const item of projectItems.value) {
    const status = String(item.contractStatus || '未知').trim() || '未知'
    counter.set(status, (counter.get(status) ?? 0) + 1)
  }
  return Array.from(counter.entries()).map(([name, value]) => ({ name, value })).sort((a, b) => b.value - a.value)
})

const demandStatusData = computed<ChartDatum[]>(() => {
  const counter = new Map<string, number>()
  for (const item of demandDrillSource.value) {
    const status = String(item.status || '未知').trim() || '未知'
    counter.set(status, (counter.get(status) ?? 0) + 1)
  }
  return Array.from(counter.entries()).map(([name, value]) => ({ name, value })).sort((a, b) => b.value - a.value)
})

const alertLevelData = computed<ChartDatum[]>(() => {
  const counter = new Map<string, number>()
  for (const item of alertItems.value) {
    const level = String(item.level || '未知').trim() || '未知'
    counter.set(level, (counter.get(level) ?? 0) + 1)
  }
  return Array.from(counter.entries()).map(([name, value]) => ({ name, value })).sort((a, b) => b.value - a.value)
})

const projectDrillRows = computed(() => {
  if (selectedProjectStatus.value === '全部') {
    return projectItems.value.slice(0, 200)
  }
  return projectItems.value.filter((item) => (item.contractStatus || '未知') === selectedProjectStatus.value).slice(0, 200)
})

const demandDrillRows = computed(() => {
  if (selectedDemandStatus.value === '全部') {
    return demandDrillSource.value.slice(0, 200)
  }
  return demandDrillSource.value.filter((item) => item.status === selectedDemandStatus.value).slice(0, 200)
})

const alertDrillRows = computed(() => {
  if (selectedAlertLevel.value === '全部') {
    return alertItems.value.slice(0, 200)
  }
  return alertItems.value.filter((item) => item.level === selectedAlertLevel.value).slice(0, 200)
})

const renderDonut = (
  chart: ChartInstance | null,
  title: string,
  data: ChartDatum[],
  colors: string[],
) => {
  if (!chart) {
    return
  }

  const total = data.reduce((sum, item) => sum + item.value, 0)
  chart.setOption({
    color: colors,
    tooltip: {
      trigger: 'item',
      formatter: (params: any) => {
        const percent = total > 0 ? ((params.value / total) * 100).toFixed(1) : '0.0'
        return `${params.name}<br/>数量：${params.value}<br/>占比：${percent}%`
      },
    },
    legend: {
      bottom: 0,
      type: 'scroll',
    },
    series: [
      {
        name: title,
        type: 'pie',
        radius: ['45%', '70%'],
        center: ['50%', '44%'],
        itemStyle: {
          borderRadius: 8,
          borderColor: '#fff',
          borderWidth: 2,
        },
        label: {
          formatter: '{b}\n{d}%',
          fontSize: 12,
        },
        data,
      },
    ],
  })
}

const updateCharts = () => {
  renderDonut(projectChart.value, '项目台账', projectStatusData.value, ['#3b82f6', '#14b8a6', '#f59e0b', '#ef4444', '#8b5cf6'])
  renderDonut(demandChart.value, '重大需求', demandStatusData.value, ['#0ea5e9', '#22c55e', '#f97316', '#6366f1', '#ec4899'])
  renderDonut(alertChart.value, '告警等级', alertLevelData.value, ['#ef4444', '#f59e0b', '#3b82f6', '#6b7280'])
}

const bindChartEvents = () => {
  projectChart.value?.off('click')
  demandChart.value?.off('click')
  alertChart.value?.off('click')

  projectChart.value?.on('click', (params: any) => {
    selectedProjectStatus.value = params?.name || '全部'
    activeTab.value = 'project'
  })

  demandChart.value?.on('click', (params: any) => {
    selectedDemandStatus.value = params?.name || '全部'
    activeTab.value = 'demand'
  })

  alertChart.value?.on('click', (params: any) => {
    selectedAlertLevel.value = params?.name || '全部'
    activeTab.value = 'alert'
  })
}

const resetDrill = () => {
  selectedProjectStatus.value = '全部'
  selectedDemandStatus.value = '全部'
  selectedAlertLevel.value = '全部'
}

const resizeCharts = () => {
  projectChart.value?.resize()
  demandChart.value?.resize()
  alertChart.value?.resize()
}

const shouldLoadDemand = (force = false) => {
  if (force) {
    return true
  }

  if (!majorSnapshot.value) {
    return true
  }

  return Date.now() - demandLastLoadedAt.value >= DEMAND_REFRESH_INTERVAL
}

const loadDemandSnapshot = async (force = false) => {
  if (demandLoading.value || !shouldLoadDemand(force)) {
    return
  }

  demandLoading.value = true
  try {
    const demandRes = await fetchMajorDemands()
    majorSnapshot.value = demandRes.data
    demandLastLoadedAt.value = Date.now()

    await nextTick()
    updateCharts()
    bindChartEvents()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载重大需求图表失败，请稍后重试'))
  } finally {
    demandLoading.value = false
  }
}

const loadDashboard = async () => {
  if (loading.value) {
    return
  }

  loading.value = true
  try {
    const [projectRes, alertRes] = await Promise.all([
      fetchProjectList({ page: 1, size: 1000 }),
      fetchAlertCenter({ page: 1, size: 1000 }),
    ])

    projectItems.value = projectRes.data.items
    alertItems.value = alertRes.data.items

    await nextTick()
    updateCharts()
    bindChartEvents()

    void loadDemandSnapshot()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载首页图表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

useLinkedRealtimeRefresh({
  refresh: loadDashboard,
  scope: 'global',
  intervalMs: 60000,
  enableAutoRefresh: true,
})

onMounted(async () => {
  await nextTick()

  if (projectChartRef.value) {
    projectChart.value = echarts.init(projectChartRef.value)
  }
  if (demandChartRef.value) {
    demandChart.value = echarts.init(demandChartRef.value)
  }
  if (alertChartRef.value) {
    alertChart.value = echarts.init(alertChartRef.value)
  }

  window.addEventListener('resize', resizeCharts)
  await loadDashboard()
  setTimeout(() => {
    void loadDemandSnapshot()
  }, 0)
})

watch(activeTab, (tab) => {
  if (tab === 'demand') {
    void loadDemandSnapshot(true)
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', resizeCharts)
  projectChart.value?.dispose()
  demandChart.value?.dispose()
  alertChart.value?.dispose()
})
</script>

<style scoped>
.dashboard-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.chart-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
}

.chart-card {
  min-height: 360px;
}

.chart-box {
  height: 280px;
}

.panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.panel-title {
  font-weight: 700;
  color: #1f3f70;
}

.drill-tip {
  margin-bottom: 8px;
  color: #4d6b90;
}

@media (max-width: 1280px) {
  .chart-grid {
    grid-template-columns: 1fr;
  }

  .chart-box {
    height: 300px;
  }
}
</style>
