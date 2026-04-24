<template>
  <div class="page-shell kpi-page" v-loading="loading">
    <div class="kpi-hero">
      <div class="kpi-hero-main">
        <div class="kpi-hero-kicker-row">
          <span class="kpi-hero-kicker">Performance Control Tower</span>
          <span class="kpi-hero-badge">5 项核心 KPI</span>
        </div>
        <h2 class="kpi-hero-title">交付绩效与执行健康度总览</h2>
        <div class="kpi-hero-subtitle">
          用统一口径观察合同状态、报修处理、巡检执行、医院覆盖和人员负载，把 KPI 从静态图表页拉回到可判断、可跳转、可追踪的经营面板。
        </div>

        <div class="kpi-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="kpi-signal-card">
            <span class="kpi-signal-label">{{ item.label }}</span>
            <strong class="kpi-signal-value">{{ item.value }}</strong>
            <span class="kpi-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="kpi-hero-side">
        <div class="kpi-control-card">
          <div class="kpi-control-copy">
            <span class="kpi-control-title">绩效刷新</span>
            <span class="kpi-control-note">保持当前五项核心指标、图表与占比看板同步刷新。</span>
          </div>
          <el-button size="small" :loading="loading" @click="loadKpi" icon="Refresh">刷新数据</el-button>
        </div>

        <div class="kpi-quick-grid">
          <button v-for="action in quickActions" :key="action.title" type="button" class="kpi-quick-action" @click="action.onClick()">
            <span class="kpi-quick-title">{{ action.title }}</span>
            <span class="kpi-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <div class="metrics-grid metrics-grid--5">
      <button v-for="card in cards" :key="card.title" type="button" class="metric-card metric-card--action" @click="card.onClick()">
        <div class="metric-card-head">
          <span class="metric-title">{{ card.title }}</span>
          <span class="metric-context">{{ card.context }}</span>
        </div>
        <div class="metric-value" :style="{ color: card.color }">{{ card.value }}</div>
        <div class="metric-note">{{ card.note }}</div>
      </button>
    </div>

    <div class="chart-grid">
      <AppTableCard class="chart-card">
        <template #header>
          <div class="panel-head">
            <span class="panel-title">合同状态占比</span>
            <el-tag type="info">{{ kpiData?.contractStatusDistribution.total ?? 0 }} 项目</el-tag>
          </div>
        </template>
        <div ref="contractChartRef" class="chart-box"></div>
      </AppTableCard>

      <AppTableCard class="chart-card">
        <template #header>
          <div class="panel-head">
            <span class="panel-title">报修处理率</span>
            <el-tag :type="repairTagType">{{ kpiData?.repairProcessingRate.rate ?? 0 }}%</el-tag>
          </div>
        </template>
        <div ref="repairChartRef" class="chart-box"></div>
      </AppTableCard>

      <AppTableCard class="chart-card">
        <template #header>
          <div class="panel-head">
            <span class="panel-title">巡检完成率</span>
            <el-tag :type="inspectionTagType">{{ kpiData?.inspectionCompletionRate.rate ?? 0 }}%</el-tag>
          </div>
        </template>
        <div ref="inspectionChartRef" class="chart-box"></div>
      </AppTableCard>
    </div>

    <el-row :gutter="16" class="bottom-row">
      <el-col :span="12">
        <AppTableCard class="chart-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">医院产品覆盖</span>
            </div>
          </template>
          <div ref="coverageChartRef" class="chart-box"></div>
        </AppTableCard>
      </el-col>
      <el-col :span="12">
        <AppTableCard class="chart-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">人员负载率</span>
              <el-tag :type="loadTagType">{{ kpiData?.personnelLoadRate.rate ?? 0 }}%</el-tag>
            </div>
          </template>
          <div ref="loadChartRef" class="chart-box"></div>
        </AppTableCard>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { fetchDashboardV3 } from '../../api/modules/dashboard'
import type { DashboardV3Data } from '../../types/dashboard-kpi'
import { getErrorMessage } from '../../utils/error'
import { basicEcharts, type ECharts } from '../../utils/echarts-basic'
import AppTableCard from '../../components/AppTableCard.vue'

const loading = ref(false)
const kpiData = ref<DashboardV3Data | null>(null)
const router = useRouter()

const contractChartRef = ref<HTMLDivElement | null>(null)
const repairChartRef = ref<HTMLDivElement | null>(null)
const inspectionChartRef = ref<HTMLDivElement | null>(null)
const coverageChartRef = ref<HTMLDivElement | null>(null)
const loadChartRef = ref<HTMLDivElement | null>(null)

let contractChart: ECharts | null = null
let repairChart: ECharts | null = null
let inspectionChart: ECharts | null = null
let coverageChart: ECharts | null = null
let loadChart: ECharts | null = null

const tagTypeForRate = (rate: number) => {
  if (rate >= 80) return 'success'
  if (rate >= 50) return 'warning'
  return 'danger'
}

const repairTagType = computed(() => tagTypeForRate(kpiData.value?.repairProcessingRate.rate ?? 0))
const inspectionTagType = computed(() => tagTypeForRate(kpiData.value?.inspectionCompletionRate.rate ?? 0))
const loadTagType = computed(() => tagTypeForRate(kpiData.value?.personnelLoadRate.rate ?? 0))

const heroSignals = computed(() => {
  const data = kpiData.value
  if (!data) {
    return [
      { label: '项目总数', value: '0', note: '等待 KPI 数据装载' },
      { label: '报修处理', value: '0/0', note: '已完成与全部报修比' },
      { label: '巡检完成', value: '0/0', note: '已完成与全部巡检计划比' },
      { label: '工时活跃', value: '0/0', note: '有工时记录人员与全部人员比' },
    ]
  }

  return [
    {
      label: '项目总数',
      value: String(data.contractStatusDistribution.total),
      note: '当前纳入 KPI 统计口径的项目总量',
    },
    {
      label: '报修处理',
      value: `${data.repairProcessingRate.completed}/${data.repairProcessingRate.total}`,
      note: `处理率 ${data.repairProcessingRate.rate}%`,
    },
    {
      label: '巡检完成',
      value: `${data.inspectionCompletionRate.completed}/${data.inspectionCompletionRate.total}`,
      note: `完成率 ${data.inspectionCompletionRate.rate}%`,
    },
    {
      label: '工时活跃',
      value: `${data.personnelLoadRate.activePersonnel}/${data.personnelLoadRate.totalPersonnel}`,
      note: `负载率 ${data.personnelLoadRate.rate}%`,
    },
  ]
})

const quickActions = computed(() => {
  return [
    {
      title: '合同台账',
      note: '按合同状态继续下钻项目分布',
      onClick: () => void router.push('/project/list'),
    },
    {
      title: '报修处理',
      note: '查看待处理工单和处理效率',
      onClick: () => void router.push('/repair/list'),
    },
    {
      title: '巡检执行',
      note: '切到巡检计划核查完成率与积压',
      onClick: () => void router.push('/inspection/plan'),
    },
    {
      title: '工时报表',
      note: '复查活跃人员与工时填报情况',
      onClick: () => void router.push('/report/workhours'),
    },
  ]
})

const cards = computed(() => {
  const d = kpiData.value
  if (!d) return []
  return [
    { title: '项目总数', value: String(d.contractStatusDistribution.total), color: '#3f4f63', context: '合同视角', note: '纳入 KPI 口径的项目总数量', onClick: () => void router.push('/project/list') },
    { title: '报修处理率', value: `${d.repairProcessingRate.rate}%`, color: d.repairProcessingRate.rate >= 80 ? '#7d9f92' : '#c58a87', context: '服务效率', note: '已完成报修占全部报修的比例', onClick: () => void router.push({ path: '/repair/list', query: { status: '待处理' } }) },
    { title: '人员负载率', value: `${d.personnelLoadRate.rate}%`, color: d.personnelLoadRate.rate >= 50 ? '#7c98bc' : '#c5a272', context: '资源利用', note: '有工时记录人员占全部人员的比例', onClick: () => void router.push('/permission/manage') },
    { title: '巡检完成率', value: `${d.inspectionCompletionRate.rate}%`, color: d.inspectionCompletionRate.rate >= 80 ? '#7d9f92' : '#c58a87', context: '执行质量', note: '已完成巡检占全部计划的比例', onClick: () => void router.push('/inspection/plan') },
    { title: '医院覆盖数', value: String(d.hospitalProductCoverage.hospitalCount), color: '#7c98bc', context: '覆盖面', note: '当前建立项目关联的医院总数量', onClick: () => void router.push('/project/list') },
  ]
})

const renderCharts = () => {
  const d = kpiData.value
  if (!d) return

  // 合同状态占比 — 饼图
  if (contractChart) {
    const data = d.contractStatusDistribution.items.map(i => ({ name: i.status, value: i.count }))
    contractChart.setOption({
      color: ['#7c98bc', '#8db3a8', '#c79b72', '#c58a87', '#9388bf', '#a7b0bd'],
      tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
      legend: { bottom: 0, type: 'scroll' },
      series: [{
        type: 'pie',
        radius: ['45%', '70%'],
        center: ['50%', '44%'],
        itemStyle: { borderRadius: 8, borderColor: '#fff', borderWidth: 2 },
        label: { formatter: '{b}\n{d}%', fontSize: 12 },
        data,
      }],
    })
    contractChart.off('click')
    contractChart.on('click', (params) => {
      const status = typeof params.name === 'string' ? params.name : ''
      void router.push({ path: '/project/list', query: status ? { contractStatus: status } : {} })
    })
  }

  // 报修处理率 — 仪表盘
  if (repairChart) {
    const rate = d.repairProcessingRate.rate
    repairChart.setOption({
      series: [{
        type: 'gauge',
        startAngle: 200,
        endAngle: -20,
        min: 0,
        max: 100,
        axisLine: {
          lineStyle: {
            width: 20,
            color: [[0.5, '#c58a87'], [0.8, '#c7a06c'], [1, '#7d9f92']],
          },
        },
        pointer: { width: 5 },
        axisTick: { show: false },
        splitLine: { length: 10 },
        axisLabel: { distance: 25, fontSize: 11 },
        detail: {
          valueAnimation: true,
          formatter: `{value}%\n已处理 ${d.repairProcessingRate.completed}/${d.repairProcessingRate.total}`,
          fontSize: 14,
          offsetCenter: [0, '70%'],
        },
        data: [{ value: rate }],
      }],
    })
  }

  // 巡检完成率 — 仪表盘
  if (inspectionChart) {
    const rate = d.inspectionCompletionRate.rate
    inspectionChart.setOption({
      series: [{
        type: 'gauge',
        startAngle: 200,
        endAngle: -20,
        min: 0,
        max: 100,
        axisLine: {
          lineStyle: {
            width: 20,
            color: [[0.5, '#c58a87'], [0.8, '#c7a06c'], [1, '#7d9f92']],
          },
        },
        pointer: { width: 5 },
        axisTick: { show: false },
        splitLine: { length: 10 },
        axisLabel: { distance: 25, fontSize: 11 },
        detail: {
          valueAnimation: true,
          formatter: `{value}%\n已完成 ${d.inspectionCompletionRate.completed}/${d.inspectionCompletionRate.total}`,
          fontSize: 14,
          offsetCenter: [0, '70%'],
        },
        data: [{ value: rate }],
      }],
    })
  }

  // 医院产品覆盖 — 柱状图
  if (coverageChart) {
    const cov = d.hospitalProductCoverage
    coverageChart.setOption({
      tooltip: { trigger: 'axis' },
      xAxis: {
        type: 'category',
        data: ['医院数', '产品数', '覆盖组合', '平均产品/医院'],
        axisLabel: { fontSize: 12 },
      },
      yAxis: { type: 'value' },
      series: [{
        type: 'bar',
        barWidth: 40,
        itemStyle: {
          borderRadius: [6, 6, 0, 0],
          color: (params: any) => {
            const colors = ['#7c98bc', '#8db3a8', '#c7a06c', '#9388bf']
            return colors[params.dataIndex % colors.length]
          },
        },
        data: [cov.hospitalCount, cov.productCount, cov.coveragePairs, cov.avgProductsPerHospital],
        label: { show: true, position: 'top', fontSize: 12 },
      }],
    })
  }

  // 人员负载率 — 环形+文字
  if (loadChart) {
    const pLoad = d.personnelLoadRate
    loadChart.setOption({
      tooltip: { trigger: 'item' },
      series: [{
        type: 'pie',
        radius: ['55%', '75%'],
        center: ['50%', '44%'],
        itemStyle: { borderRadius: 8, borderColor: '#fff', borderWidth: 2 },
        label: { show: false },
        data: [
          { value: pLoad.activePersonnel, name: '有工时记录人员', itemStyle: { color: '#7c98bc' } },
          { value: Math.max(pLoad.totalPersonnel - pLoad.activePersonnel, 0), name: '无工时记录人员', itemStyle: { color: '#e5e7eb' } },
        ],
      }],
      graphic: [{
        type: 'text',
        left: 'center',
        top: '38%',
        style: {
          text: `${pLoad.rate}%`,
          fontSize: 28,
          fontWeight: 'bold',
          fill: '#3f4f63',
          textAlign: 'center',
        },
      }, {
        type: 'text',
        left: 'center',
        top: '52%',
        style: {
          text: `${pLoad.activePersonnel} / ${pLoad.totalPersonnel} 人`,
          fontSize: 13,
          fill: '#6b7280',
          textAlign: 'center',
        },
      }],
    })
  }
}

const resizeCharts = () => {
  contractChart?.resize()
  repairChart?.resize()
  inspectionChart?.resize()
  coverageChart?.resize()
  loadChart?.resize()
}

const loadKpi = async () => {
  if (loading.value) return
  loading.value = true
  try {
    const res = await fetchDashboardV3()
    kpiData.value = res.data
    await nextTick()
    renderCharts()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载 KPI 数据失败'))
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await nextTick()
  if (contractChartRef.value) contractChart = basicEcharts.init(contractChartRef.value)
  if (repairChartRef.value) repairChart = basicEcharts.init(repairChartRef.value)
  if (inspectionChartRef.value) inspectionChart = basicEcharts.init(inspectionChartRef.value)
  if (coverageChartRef.value) coverageChart = basicEcharts.init(coverageChartRef.value)
  if (loadChartRef.value) loadChart = basicEcharts.init(loadChartRef.value)

  window.addEventListener('resize', resizeCharts)
  await loadKpi()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', resizeCharts)
  contractChart?.off('click')
  contractChart?.dispose()
  repairChart?.dispose()
  inspectionChart?.dispose()
  coverageChart?.dispose()
  loadChart?.dispose()
})
</script>

<style scoped>
.kpi-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.kpi-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.18fr) minmax(320px, 0.82fr);
  gap: 16px;
  padding: 24px;
  border-radius: 28px;
  color: #ffffff;
  background:
    radial-gradient(circle at 12% 16%, rgba(135, 191, 255, 0.2), transparent 24%),
    radial-gradient(circle at 100% 0, rgba(125, 159, 146, 0.18), transparent 24%),
    linear-gradient(145deg, #123654 0%, #184d7b 52%, #1b6b6b 100%);
  box-shadow: 0 28px 46px rgba(13, 48, 76, 0.18);
}

.kpi-hero-main {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-width: 0;
}

.kpi-hero-kicker-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.kpi-hero-kicker,
.kpi-hero-badge {
  display: inline-flex;
  align-items: center;
  padding: 7px 12px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 700;
}

.kpi-hero-kicker {
  border: 1px solid rgba(255, 255, 255, 0.18);
  background: rgba(255, 255, 255, 0.08);
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.kpi-hero-badge {
  background: rgba(255, 255, 255, 0.14);
  color: #f1f7ff;
}

.kpi-hero-title {
  margin: 0;
  font-size: 34px;
  line-height: 1.12;
  font-weight: 700;
}

.kpi-hero-subtitle {
  max-width: 680px;
  font-size: 14px;
  line-height: 1.85;
  color: rgba(233, 243, 255, 0.84);
}

.kpi-hero-signals {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin-top: 4px;
}

.kpi-signal-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.11), rgba(255, 255, 255, 0.05));
}

.kpi-signal-label {
  font-size: 12px;
  color: rgba(227, 239, 255, 0.74);
}

.kpi-signal-value {
  font-size: 28px;
  line-height: 1;
  font-weight: 700;
}

.kpi-signal-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(232, 242, 255, 0.76);
}

.kpi-hero-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.kpi-control-card,
.kpi-quick-action {
  border: 1px solid rgba(255, 255, 255, 0.14);
  border-radius: 20px;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.14), rgba(255, 255, 255, 0.08));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

.kpi-control-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 18px;
}

.kpi-control-copy {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.kpi-control-title {
  font-size: 16px;
  font-weight: 700;
}

.kpi-control-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(231, 241, 255, 0.74);
}

.kpi-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.kpi-quick-action {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  color: #ffffff;
  text-align: left;
  cursor: pointer;
  transition: transform 0.2s ease, border-color 0.2s ease, background 0.2s ease;
}

.kpi-quick-action:hover {
  transform: translateY(-1px);
  border-color: rgba(255, 255, 255, 0.24);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.18), rgba(255, 255, 255, 0.09));
}

.kpi-quick-title {
  font-size: 14px;
  font-weight: 700;
}

.kpi-quick-note {
  font-size: 12px;
  line-height: 1.65;
  color: rgba(233, 243, 255, 0.76);
}

.chart-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

.bottom-row {
  margin-top: 0;
}

.chart-card {
  min-height: 372px;
}

.chart-box {
  height: 288px;
}

.metric-card--action {
  position: relative;
  overflow: hidden;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98), rgba(246, 249, 253, 0.95));
}

.metric-card--action::before {
  content: '';
  position: absolute;
  inset: 0 auto auto 0;
  width: 100%;
  height: 4px;
  background: linear-gradient(90deg, #184d7b 0%, #4ca591 100%);
  opacity: 0.92;
}

.panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.panel-title {
  font-weight: 700;
  color: #334155;
}

@media (max-width: 1280px) {
  .kpi-hero {
    grid-template-columns: 1fr;
  }

  .chart-grid {
    grid-template-columns: 1fr;
  }

  .chart-box {
    height: 300px;
  }
}

@media (max-width: 768px) {
  .kpi-hero {
    padding: 18px;
  }

  .kpi-hero-title {
    font-size: 28px;
  }

  .kpi-hero-signals,
  .kpi-quick-grid {
    grid-template-columns: 1fr;
  }

  .kpi-control-card {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
