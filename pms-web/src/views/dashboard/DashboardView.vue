<template>
  <SupervisorWorkbench v-if="dashboardRole === 'supervisor'" />
  <RegionalManagerWorkbench v-else-if="dashboardRole === 'regional_manager'" />
  <PersonalWorkbench v-else-if="dashboardRole === 'operator'" />
  <div v-else class="page-shell dashboard-page" v-loading="loading">
    <div class="dashboard-hero">
      <div class="dashboard-hero-main">
        <div class="dashboard-hero-kicker-row">
          <span class="dashboard-hero-kicker">Manager Command Deck</span>
          <span class="dashboard-hero-role">{{ currentScopeLabel }}</span>
        </div>
        <h2 class="dashboard-hero-title">欢迎回来，{{ heroDisplayName }}</h2>
        <div class="dashboard-hero-subtitle">
          以管理视角总览项目、重大需求、预警和年度报告健康度，并从这里直接切入高频执行模块，减少在菜单与图表之间反复切换。
        </div>

        <div class="dashboard-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="hero-signal">
            <span class="hero-signal-label">{{ item.label }}</span>
            <strong class="hero-signal-value">{{ item.value }}</strong>
            <span class="hero-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="dashboard-hero-side">
        <div class="hero-control-card">
          <div class="hero-control-copy">
            <span class="hero-control-title">统计窗口</span>
            <span class="hero-control-note">按时间窗口刷新当前首页的趋势与分布图表</span>
          </div>
          <div class="hero-control-actions">
            <el-select v-model="timeRange" size="small" style="width: 140px" @change="loadDashboard">
              <el-option :value="1" label="最近1个月" />
              <el-option :value="3" label="最近3个月" />
              <el-option :value="6" label="最近6个月" />
              <el-option :value="12" label="最近12个月" />
            </el-select>
            <el-button size="small" :loading="loading" @click="loadDashboard" icon="Refresh">刷新数据</el-button>
          </div>
        </div>

        <div class="hero-quick-grid">
          <button v-for="action in quickActions" :key="action.title" type="button" class="hero-quick-action" @click="action.onClick()">
            <span class="hero-quick-title">{{ action.title }}</span>
            <span class="hero-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <div class="dashboard-metrics">
      <button v-for="card in cards" :key="card.title" type="button" class="metric-card" @click="card.onClick()">
        <div class="metric-card-head">
          <span class="metric-title">{{ card.title }}</span>
          <span class="metric-context">{{ card.context }}</span>
        </div>
        <div class="metric-value">{{ card.value }}</div>
        <div class="metric-note">{{ card.note }}</div>
      </button>
    </div>

    <div class="chart-grid">
      <AppTableCard class="chart-card">
        <template #header>
          <div class="panel-head">
            <div class="panel-copy">
              <span class="panel-title">项目台账状态占比</span>
              <span class="panel-subtitle">按合同与维护状态聚合，支持直接下钻</span>
            </div>
            <span class="panel-caption">点击图形</span>
          </div>
        </template>
        <div ref="projectChartRef" class="chart-box"></div>
      </AppTableCard>

      <AppTableCard class="chart-card">
        <template #header>
          <div class="panel-head">
            <div class="panel-copy">
              <span class="panel-title">重大需求状态占比</span>
              <span class="panel-subtitle">聚焦需求流程进度与跟进压力</span>
            </div>
            <span class="panel-caption">点击图形</span>
          </div>
        </template>
        <div ref="demandChartRef" class="chart-box"></div>
      </AppTableCard>

      <AppTableCard class="chart-card">
        <template #header>
          <div class="panel-head">
            <div class="panel-copy">
              <span class="panel-title">告警等级占比</span>
              <span class="panel-subtitle">观察风险集中度与严重程度分布</span>
            </div>
            <span class="panel-caption">点击图形</span>
          </div>
        </template>
        <div ref="alertChartRef" class="chart-box"></div>
      </AppTableCard>

      <AppTableCard class="chart-card">
        <template #header>
          <div class="panel-head">
            <div class="panel-copy">
              <span class="panel-title">年度报告状态</span>
              <span class="panel-subtitle">编写、提交与完成节点一屏总览</span>
            </div>
            <span class="panel-caption">点击跳转</span>
          </div>
        </template>
        <div ref="annualChartRef" class="chart-box"></div>
      </AppTableCard>
    </div>

    <div class="analysis-grid">
      <AppTableCard class="chart-card">
        <template #header>
          <div class="panel-head">
            <div class="panel-copy">
              <span class="panel-title">告警月度趋势</span>
              <span class="panel-subtitle">近 {{ timeRange }} 个月风险变化走势</span>
            </div>
            <span class="panel-caption">趋势分析</span>
          </div>
        </template>
        <div ref="trendChartRef" class="chart-box"></div>
      </AppTableCard>

      <AppTableCard class="chart-card">
        <template #header>
          <div class="panel-head">
            <div class="panel-copy">
              <span class="panel-title">责任人工作量 TOP12</span>
              <span class="panel-subtitle">识别忙闲分布与负载集中情况</span>
            </div>
            <span class="panel-caption">堆叠柱状</span>
          </div>
        </template>
        <div ref="workloadChartRef" class="chart-box"></div>
      </AppTableCard>
    </div>

    <AppTableCard>
      <template #header>
        <div class="panel-head panel-head-compact">
          <div class="panel-copy">
            <span class="panel-title">下钻明细</span>
            <span class="panel-subtitle">图表筛选与列表明细保持联动</span>
          </div>
          <el-button link type="primary" @click="resetDrill" icon="Refresh">重置筛选</el-button>
        </div>
      </template>

      <el-tabs v-model="activeTab">
        <el-tab-pane label="项目台账" name="project">
          <div class="drill-tip">当前筛选：{{ selectedProjectStatus }}</div>
          <el-table :data="projectDrillRows" stripe border max-height="320" scrollbar-always-on empty-text="暂无项目明细" @row-dblclick="onProjectDrillGoto">
            <el-table-column prop="hospitalName" label="医院" min-width="180" show-overflow-tooltip />
            <el-table-column prop="productName" label="产品" min-width="160" show-overflow-tooltip />
            <el-table-column prop="province" label="省份" width="110" />
            <el-table-column prop="contractStatus" label="状态" width="140" />
            <el-table-column prop="overdueDays" label="超期天数" width="110" align="right" />
            <el-table-column label="操作" width="120" fixed="right">
              <template #default="scope">
                <el-button link type="primary" @click="onProjectDrillGoto(scope.row)" icon="Service">去处理</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="重大需求" name="demand">
          <div class="drill-tip">当前筛选：{{ selectedDemandStatus }}</div>
          <el-table :data="demandDrillRows" stripe border max-height="320" scrollbar-always-on empty-text="暂无重大需求明细" @row-dblclick="onDemandDrillGoto">
            <el-table-column prop="title" label="需求" min-width="220" show-overflow-tooltip />
            <el-table-column prop="hospital" label="医院/客户" min-width="180" show-overflow-tooltip />
            <el-table-column prop="owner" label="负责人" width="120" show-overflow-tooltip />
            <el-table-column prop="status" label="状态" width="120" />
            <el-table-column prop="dueDate" label="计划完成" width="140" />
            <el-table-column label="操作" width="120" fixed="right">
              <template #default="scope">
                <el-button link type="primary" @click="onDemandDrillGoto(scope.row)" icon="Service">去处理</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="告警" name="alert">
          <div class="drill-tip">当前筛选：{{ selectedAlertLevel }}</div>
          <el-table :data="alertDrillRows" stripe border max-height="320" scrollbar-always-on empty-text="暂无告警明细" @row-dblclick="onAlertDrillGoto">
            <el-table-column prop="source" label="来源" width="90" />
            <el-table-column prop="level" label="等级" width="90" />
            <el-table-column prop="hospitalName" label="医院" min-width="180" show-overflow-tooltip />
            <el-table-column prop="title" label="标题" min-width="220" show-overflow-tooltip />
            <el-table-column prop="owner" label="责任人" width="110" show-overflow-tooltip />
            <el-table-column prop="overdueDays" label="超期天数" width="110" align="right" />
            <el-table-column label="操作" width="120" fixed="right">
              <template #default="scope">
                <el-button link type="primary" @click="onAlertDrillGoto(scope.row)" icon="Service">去处理</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </AppTableCard>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { fetchProjectList } from '../../api/modules/project'
import { fetchMajorDemands, type MajorDemandSnapshot } from '../../api/modules/majorDemand'
import { fetchAlertCenter, type AlertCenterItem } from '../../api/modules/alertCenter'
import { fetchAnnualReportSummary } from '../../api/modules/annual-report'
import { fetchDashboardV2, type DashboardV2TrendItem, type DashboardV2OwnerItem } from '../../api/modules/dashboard'
import type { AnnualReportSummary } from '../../types/annual-report'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import PersonalWorkbench from './PersonalWorkbench.vue'
import SupervisorWorkbench from './SupervisorWorkbench.vue'
import RegionalManagerWorkbench from './RegionalManagerWorkbench.vue'
import AppTableCard from '../../components/AppTableCard.vue'
import { basicEcharts } from '../../utils/echarts-basic'

const access = useAccessControl()
const dashboardRole = computed(() => {
  if (access.isSupervisor()) return 'supervisor'
  if (access.isRegionalManager()) return 'regional_manager'
  if (access.isManager()) return 'manager'
  return 'operator'
})

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
const router = useRouter()
const activeTab = ref<'project' | 'demand' | 'alert'>('project')
const demandLastLoadedAt = ref(0)
const DEMAND_REFRESH_INTERVAL = 5 * 60 * 1000
const timeRange = ref(6)

const selectedProjectStatus = ref('全部')
const selectedDemandStatus = ref('全部')
const selectedAlertLevel = ref('全部')

const projectItems = ref<any[]>([])
const majorSnapshot = ref<MajorDemandSnapshot | null>(null)
const alertItems = ref<AlertCenterItem[]>([])

const projectChartRef = ref<HTMLDivElement | null>(null)
const demandChartRef = ref<HTMLDivElement | null>(null)
const alertChartRef = ref<HTMLDivElement | null>(null)
const annualChartRef = ref<HTMLDivElement | null>(null)

const projectChart = ref<ChartInstance | null>(null)
const demandChart = ref<ChartInstance | null>(null)
const alertChart = ref<ChartInstance | null>(null)
const annualChart = ref<ChartInstance | null>(null)

const annualSummary = ref<AnnualReportSummary | null>(null)

const trendData = ref<DashboardV2TrendItem[]>([])
const ownerWorkloadData = ref<DashboardV2OwnerItem[]>([])

const trendChartRef = ref<HTMLDivElement | null>(null)
const workloadChartRef = ref<HTMLDivElement | null>(null)
const trendChart = ref<ChartInstance | null>(null)
const workloadChart = ref<ChartInstance | null>(null)

const projectDonutColors = ['#6c88a7', '#7ca8a1', '#c7a06c', '#c28787', '#8c88b9', '#a7b6c7']
const demandDonutColors = ['#7c98bc', '#8db3a8', '#c79b72', '#9589c5', '#c39ab0']
const alertDonutColors = ['#c68587', '#cbab73', '#7f9ac4', '#a7b0bd']
const annualDonutColors = ['#c58a87', '#ccb07e', '#85a3c4', '#8eb5a3']
const severityColors = {
  severe: '#c58686',
  warning: '#c8a368',
  reminder: '#7f99c2',
}

const cards = computed(() => {
  const totalAlerts = alertItems.value.length
  const severeCount = alertItems.value.filter((item) => item.level === '严重').length
  const severeRatio = totalAlerts > 0 ? `${((severeCount / totalAlerts) * 100).toFixed(1)}%` : '0%'
  const overdueProjects = projectItems.value.filter((item) => Number(item.overdueDays || 0) > 0).length

  return [
    {
      title: '项目总数',
      value: String(projectItems.value.length),
      context: '项目总览',
      note: '当前数据范围内可见的项目台账总量',
      onClick: () => void router.push('/project/list'),
    },
    {
      title: '重大需求',
      value: String((majorSnapshot.value?.workflows ?? []).length),
      context: '需求跟踪',
      note: '需要持续推进的需求流程与协同事项',
      onClick: () => void router.push('/major-demand/list'),
    },
    {
      title: '告警总数',
      value: String(totalAlerts),
      context: '风险监控',
      note: '当前告警中心汇总的全部风险事件',
      onClick: () => void router.push('/alert/center'),
    },
    {
      title: '严重告警占比',
      value: severeRatio,
      context: '风险浓度',
      note: '高优先级风险在全部告警中的占比',
      onClick: () => void router.push({ path: '/alert/center', query: { level: '严重' } }),
    },
    {
      title: '超期项目',
      value: String(overdueProjects),
      context: '交付压力',
      note: '已进入超期或即将超期的项目数量',
      onClick: () => void router.push({ path: '/project/list', query: { contractStatus: '超期未签署' } }),
    },
  ]
})

const heroDisplayName = computed(() => {
  const profile = access.accessProfile.value
  if (!profile) {
    return '管理员'
  }

  if (profile.isAdmin) {
    return 'admin'
  }

  return profile.personnelName || '管理员'
})

const currentScopeLabel = computed(() => {
  const profile = access.accessProfile.value
  if (!profile) {
    return '企业工作台'
  }

  if (profile.isAdmin) {
    return '系统全局视角'
  }

  switch (profile.systemRole) {
    case 'manager':
      return '管理视角'
    case 'regional_manager':
      return '区域经理视角'
    case 'supervisor':
      return '运维主管视角'
    default:
      return '执行视角'
  }
})

const heroSignals = computed(() => {
  const totalAlerts = alertItems.value.length
  const severeCount = alertItems.value.filter((item) => item.level === '严重').length
  const overdueProjects = projectItems.value.filter((item) => Number(item.overdueDays || 0) > 0).length
  const annualPending = annualSummary.value
    ? annualSummary.value.notStartedCount + annualSummary.value.writingCount + annualSummary.value.submittedCount
    : 0

  return [
    {
      label: '可见项目',
      value: String(projectItems.value.length),
      note: '当前数据范围内可直接下钻的项目台账数量',
    },
    {
      label: '高优预警',
      value: `${severeCount}/${totalAlerts}`,
      note: '严重告警占当前全部告警的核心风险密度',
    },
    {
      label: '待收口报告',
      value: String(annualPending),
      note: '仍处于未开始、编写中或已提交状态的年度报告',
    },
    {
      label: '交付压力',
      value: String(overdueProjects),
      note: '已进入超期或逼近风险边界的项目数量',
    },
  ]
})

const quickActions = computed(() => {
  return [
    {
      title: '统一预警中心',
      note: '直接查看严重与警告级风险事件',
      onClick: () => void router.push({ path: '/alert/center', query: { level: '严重' } }),
    },
    {
      title: '合同预警',
      note: '优先收口超期和临界交付项目',
      onClick: () => void router.push('/contract/alerts'),
    },
    {
      title: '年度报告',
      note: '切到编写、提交和完成节点管理',
      onClick: () => void router.push('/annual-report/list'),
    },
    {
      title: '待办列表',
      note: '进入跨模块执行面板继续推进事项',
      onClick: () => void router.push('/complex-model/ms010001-013-001'),
    },
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

const annualReportStatusData = computed<ChartDatum[]>(() => {
  const s = annualSummary.value
  if (!s) return []
  return [
    { name: '未开始', value: s.notStartedCount },
    { name: '编写中', value: s.writingCount },
    { name: '已提交', value: s.submittedCount },
    { name: '已完成', value: s.completedCount },
  ].filter(d => d.value > 0)
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
      backgroundColor: 'rgba(15, 23, 42, 0.9)',
      borderWidth: 0,
      textStyle: { color: '#f8fafc' },
      formatter: (params: any) => {
        const percent = total > 0 ? ((params.value / total) * 100).toFixed(1) : '0.0'
        return `${params.name}<br/>数量：${params.value}<br/>占比：${percent}%`
      },
    },
    legend: {
      bottom: 0,
      type: 'scroll',
      textStyle: {
        color: '#64748b',
        fontSize: 12,
      },
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
        emphasis: {
          scale: false,
        },
        label: {
          formatter: '{b}\n{d}%',
          fontSize: 12,
          color: '#526071',
        },
        labelLine: {
          lineStyle: {
            color: '#b8c4d4',
          },
        },
        data,
      },
    ],
  })
}

const renderTrendChart = () => {
  if (!trendChart.value || !trendData.value.length) return
  const months = trendData.value.map(d => d.month)
  trendChart.value.setOption({
    color: [severityColors.severe, severityColors.warning, severityColors.reminder],
    tooltip: { trigger: 'axis', axisPointer: { type: 'cross' } },
    legend: { bottom: 0, textStyle: { color: '#64748b' } },
    grid: { left: 50, right: 20, top: 30, bottom: 50 },
    xAxis: {
      type: 'category',
      data: months,
      boundaryGap: false,
      axisLabel: { color: '#64748b' },
      axisLine: { lineStyle: { color: '#d9e2ec' } },
      axisTick: { show: false },
    },
    yAxis: {
      type: 'value',
      minInterval: 1,
      axisLabel: { color: '#64748b' },
      splitLine: { lineStyle: { color: '#edf2f6' } },
    },
    series: [
      { name: '严重', type: 'line', stack: 'total', areaStyle: { opacity: 0.18 }, smooth: true, symbolSize: 6, lineStyle: { width: 2 }, data: trendData.value.map(d => d.severe), itemStyle: { color: severityColors.severe } },
      { name: '警告', type: 'line', stack: 'total', areaStyle: { opacity: 0.18 }, smooth: true, symbolSize: 6, lineStyle: { width: 2 }, data: trendData.value.map(d => d.warning), itemStyle: { color: severityColors.warning } },
      { name: '提醒', type: 'line', stack: 'total', areaStyle: { opacity: 0.18 }, smooth: true, symbolSize: 6, lineStyle: { width: 2 }, data: trendData.value.map(d => d.reminder), itemStyle: { color: severityColors.reminder } },
    ],
  })
}

const renderWorkloadChart = () => {
  if (!workloadChart.value || !ownerWorkloadData.value.length) return
  const owners = ownerWorkloadData.value.map(d => d.owner)
  workloadChart.value.setOption({
    color: [severityColors.severe, severityColors.warning, severityColors.reminder],
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    legend: { bottom: 0, textStyle: { color: '#64748b' } },
    grid: { left: 80, right: 20, top: 20, bottom: 50 },
    yAxis: {
      type: 'category',
      data: owners,
      inverse: true,
      axisLabel: { width: 60, overflow: 'truncate', color: '#64748b' },
      axisTick: { show: false },
      axisLine: { show: false },
    },
    xAxis: {
      type: 'value',
      minInterval: 1,
      axisLabel: { color: '#64748b' },
      splitLine: { lineStyle: { color: '#edf2f6' } },
    },
    series: [
      { name: '严重', type: 'bar', stack: 'total', barMaxWidth: 18, data: ownerWorkloadData.value.map(d => d.severe), itemStyle: { color: severityColors.severe, borderRadius: [0, 6, 6, 0] } },
      { name: '警告', type: 'bar', stack: 'total', barMaxWidth: 18, data: ownerWorkloadData.value.map(d => d.warning), itemStyle: { color: severityColors.warning, borderRadius: [0, 6, 6, 0] } },
      { name: '提醒', type: 'bar', stack: 'total', barMaxWidth: 18, data: ownerWorkloadData.value.map(d => d.reminder), itemStyle: { color: severityColors.reminder, borderRadius: [0, 6, 6, 0] } },
    ],
  })
}

const updateCharts = () => {
  renderDonut(projectChart.value, '项目台账', projectStatusData.value, projectDonutColors)
  renderDonut(demandChart.value, '重大需求', demandStatusData.value, demandDonutColors)
  renderDonut(alertChart.value, '告警等级', alertLevelData.value, alertDonutColors)
  renderDonut(annualChart.value, '年度报告', annualReportStatusData.value, annualDonutColors)
  renderTrendChart()
  renderWorkloadChart()
}

const bindChartEvents = () => {
  projectChart.value?.off('click')
  demandChart.value?.off('click')
  alertChart.value?.off('click')
  annualChart.value?.off('click')

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

  annualChart.value?.on('click', (params: any) => {
    const status = params?.name || ''
    void router.push({ path: '/annual-report/list', query: status ? { status } : {} })
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
  annualChart.value?.resize()
  trendChart.value?.resize()
  workloadChart.value?.resize()
}

const onProjectDrillGoto = (row: any) => {
  void router.push({
    path: '/project/list',
    query: {
      hospitalName: row.hospitalName,
      productName: row.productName,
      action: 'edit',
    },
  })
}

const onDemandDrillGoto = (row: DemandDrillItem) => {
  void router.push({
    path: '/major-demand/list',
    query: {
      action: 'detail',
      rowId: row.rowId,
      status: row.status,
    },
  })
}

const onAlertDrillGoto = (row: AlertCenterItem) => {
  void router.push({
    path: row.relatedPath,
    query: row.relatedQuery,
  })
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
    const [projectRes, alertRes, annualRes, v2Res] = await Promise.all([
      fetchProjectList({ page: 1, size: 100000 }),
      fetchAlertCenter({ page: 1, size: 100000 }),
      fetchAnnualReportSummary(),
      fetchDashboardV2({ months: timeRange.value }),
    ])

    projectItems.value = projectRes.data.items
    alertItems.value = alertRes.data.items
    annualSummary.value = annualRes.data
    trendData.value = v2Res.data.trend
    ownerWorkloadData.value = v2Res.data.ownerWorkload

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
    projectChart.value = basicEcharts.init(projectChartRef.value)
  }
  if (demandChartRef.value) {
    demandChart.value = basicEcharts.init(demandChartRef.value)
  }
  if (alertChartRef.value) {
    alertChart.value = basicEcharts.init(alertChartRef.value)
  }
  if (annualChartRef.value) {
    annualChart.value = basicEcharts.init(annualChartRef.value)
  }
  if (trendChartRef.value) {
    trendChart.value = basicEcharts.init(trendChartRef.value)
  }
  if (workloadChartRef.value) {
    workloadChart.value = basicEcharts.init(workloadChartRef.value)
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
  annualChart.value?.dispose()
  trendChart.value?.dispose()
  workloadChart.value?.dispose()
})
</script>

<style scoped>
.dashboard-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.dashboard-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.2fr) minmax(320px, 0.8fr);
  gap: 16px;
  padding: 24px;
  border-radius: 28px;
  color: #ffffff;
  background:
    radial-gradient(circle at 14% 18%, rgba(145, 214, 255, 0.22), transparent 20%),
    radial-gradient(circle at 100% 0, rgba(90, 236, 208, 0.18), transparent 24%),
    linear-gradient(145deg, #0f365d 0%, #0a5b99 48%, #0b7f81 100%);
  box-shadow: 0 28px 48px rgba(12, 53, 93, 0.2);
}

.dashboard-hero-main {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-width: 0;
}

.dashboard-hero-kicker-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.dashboard-hero-kicker {
  display: inline-flex;
  align-items: center;
  padding: 7px 12px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.18);
  background: rgba(255, 255, 255, 0.08);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.dashboard-hero-role {
  display: inline-flex;
  align-items: center;
  padding: 6px 11px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.12);
  font-size: 12px;
  font-weight: 700;
  color: #f2f7ff;
}

.dashboard-hero-title {
  margin: 0;
  font-size: 34px;
  line-height: 1.12;
  font-weight: 700;
}

.dashboard-hero-subtitle {
  max-width: 680px;
  font-size: 14px;
  line-height: 1.85;
  color: rgba(232, 243, 255, 0.84);
}

.dashboard-hero-signals {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin-top: 4px;
}

.hero-signal {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.11), rgba(255, 255, 255, 0.05));
}

.hero-signal-label {
  font-size: 12px;
  color: rgba(227, 239, 255, 0.74);
}

.hero-signal-value {
  font-size: 28px;
  line-height: 1;
  font-weight: 700;
}

.hero-signal-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(232, 242, 255, 0.76);
}

.dashboard-hero-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.hero-control-card,
.hero-quick-action {
  border: 1px solid rgba(255, 255, 255, 0.14);
  border-radius: 20px;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.14), rgba(255, 255, 255, 0.08));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

.hero-control-card {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 18px;
}

.hero-control-copy {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.hero-control-title {
  font-size: 16px;
  font-weight: 700;
}

.hero-control-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(231, 241, 255, 0.74);
}

.hero-control-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.hero-control-actions :deep(.el-select__wrapper),
.hero-control-actions :deep(.el-button) {
  min-height: 40px;
}

.hero-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.hero-quick-action {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  color: #ffffff;
  text-align: left;
  cursor: pointer;
  transition: transform 0.2s ease, border-color 0.2s ease, background 0.2s ease;
}

.hero-quick-action:hover {
  transform: translateY(-1px);
  border-color: rgba(255, 255, 255, 0.24);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.18), rgba(255, 255, 255, 0.09));
}

.hero-quick-title {
  font-size: 14px;
  font-weight: 700;
}

.hero-quick-note {
  font-size: 12px;
  line-height: 1.65;
  color: rgba(233, 243, 255, 0.76);
}

.page-head-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.dashboard-metrics {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 14px;
}

.metric-card {
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 18px;
  border: 1px solid #e7ebf0;
  border-radius: 18px;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98), rgba(247, 250, 254, 0.94));
  box-shadow: 0 12px 26px rgba(15, 23, 42, 0.04);
  text-align: left;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}

.metric-card::before {
  content: '';
  position: absolute;
  inset: 0 auto auto 0;
  width: 100%;
  height: 4px;
  background: linear-gradient(90deg, #0a5b99 0%, #4ca591 100%);
  opacity: 0.9;
}

.metric-card:hover {
  border-color: #d7e4f7;
  box-shadow: 0 16px 32px rgba(15, 23, 42, 0.06);
  transform: translateY(-1px);
}

.metric-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.metric-title {
  font-size: 13px;
  font-weight: 600;
  color: #526071;
}

.metric-context {
  display: inline-flex;
  align-items: center;
  padding: 4px 8px;
  border-radius: 999px;
  background: #f8fafc;
  color: #64748b;
  font-size: 11px;
  font-weight: 600;
}

.metric-value {
  font-size: 32px;
  line-height: 1;
  font-weight: 700;
  letter-spacing: -0.03em;
  color: #0f172a;
}

.metric-note {
  font-size: 12px;
  line-height: 1.6;
  color: #7b8794;
}

.chart-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
}

.analysis-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
}

.chart-card {
  min-height: 372px;
}

.chart-box {
  height: 296px;
}

.panel-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14px;
}

.panel-head-compact {
  align-items: center;
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
  color: #7b8794;
  line-height: 1.5;
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

.drill-tip {
  display: inline-flex;
  align-items: center;
  margin-bottom: 10px;
  padding: 6px 10px;
  border-radius: 999px;
  background: #f8fafc;
  color: #64748b;
  font-size: 12px;
}

@media (max-width: 1280px) {
  .dashboard-hero {
    grid-template-columns: 1fr;
  }

  .hero-quick-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .dashboard-metrics {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .chart-grid,
  .analysis-grid {
    grid-template-columns: 1fr;
  }

  .chart-box {
    height: 300px;
  }
}

@media (max-width: 768px) {
  .dashboard-hero {
    padding: 18px;
  }

  .dashboard-hero-title {
    font-size: 28px;
  }

  .dashboard-hero-signals,
  .hero-quick-grid,
  .dashboard-metrics {
    grid-template-columns: 1fr;
  }

  .page-head-actions {
    width: 100%;
    justify-content: space-between;
  }

  .hero-control-actions {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
