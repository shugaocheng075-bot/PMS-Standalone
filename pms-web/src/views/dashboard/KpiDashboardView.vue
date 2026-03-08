<template>
  <div class="page-shell kpi-page" v-loading="loading">
    <div class="page-head">
      <div>
        <h2 class="page-title">KPI 概览</h2>
        <div class="page-subtitle">合同状态、医院产品覆盖、报修处理、人员负载、巡检完成五大指标</div>
      </div>
      <el-button size="small" :loading="loading" @click="loadKpi">刷新数据</el-button>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="6" v-for="card in cards" :key="card.title">
        <el-card shadow="never" class="stat-card stats-card clickable" @click="card.onClick()">
          <div class="t">{{ card.title }}</div>
          <div class="v" :style="{ color: card.color }">{{ card.value }}</div>
        </el-card>
      </el-col>
    </el-row>

    <div class="chart-grid">
      <el-card shadow="never" class="table-card chart-card">
        <template #header>
          <div class="panel-head">
            <span class="panel-title">合同状态占比</span>
            <el-tag type="info">{{ kpiData?.contractStatusDistribution.total ?? 0 }} 项目</el-tag>
          </div>
        </template>
        <div ref="contractChartRef" class="chart-box"></div>
      </el-card>

      <el-card shadow="never" class="table-card chart-card">
        <template #header>
          <div class="panel-head">
            <span class="panel-title">报修处理率</span>
            <el-tag :type="repairTagType">{{ kpiData?.repairProcessingRate.rate ?? 0 }}%</el-tag>
          </div>
        </template>
        <div ref="repairChartRef" class="chart-box"></div>
      </el-card>

      <el-card shadow="never" class="table-card chart-card">
        <template #header>
          <div class="panel-head">
            <span class="panel-title">巡检完成率</span>
            <el-tag :type="inspectionTagType">{{ kpiData?.inspectionCompletionRate.rate ?? 0 }}%</el-tag>
          </div>
        </template>
        <div ref="inspectionChartRef" class="chart-box"></div>
      </el-card>
    </div>

    <el-row :gutter="16" class="bottom-row">
      <el-col :span="12">
        <el-card shadow="never" class="table-card chart-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">医院产品覆盖</span>
            </div>
          </template>
          <div ref="coverageChartRef" class="chart-box"></div>
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card shadow="never" class="table-card chart-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">人员负载率</span>
              <el-tag :type="loadTagType">{{ kpiData?.personnelLoadRate.rate ?? 0 }}%</el-tag>
            </div>
          </template>
          <div ref="loadChartRef" class="chart-box"></div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import * as echarts from 'echarts'
import { ElMessage } from 'element-plus'
import { fetchDashboardV3 } from '../../api/modules/dashboard'
import type { DashboardV3Data } from '../../types/dashboard-kpi'
import { getErrorMessage } from '../../utils/error'

const loading = ref(false)
const kpiData = ref<DashboardV3Data | null>(null)
const router = useRouter()

const contractChartRef = ref<HTMLDivElement | null>(null)
const repairChartRef = ref<HTMLDivElement | null>(null)
const inspectionChartRef = ref<HTMLDivElement | null>(null)
const coverageChartRef = ref<HTMLDivElement | null>(null)
const loadChartRef = ref<HTMLDivElement | null>(null)

let contractChart: echarts.ECharts | null = null
let repairChart: echarts.ECharts | null = null
let inspectionChart: echarts.ECharts | null = null
let coverageChart: echarts.ECharts | null = null
let loadChart: echarts.ECharts | null = null

const tagTypeForRate = (rate: number) => {
  if (rate >= 80) return 'success'
  if (rate >= 50) return 'warning'
  return 'danger'
}

const repairTagType = computed(() => tagTypeForRate(kpiData.value?.repairProcessingRate.rate ?? 0))
const inspectionTagType = computed(() => tagTypeForRate(kpiData.value?.inspectionCompletionRate.rate ?? 0))
const loadTagType = computed(() => tagTypeForRate(kpiData.value?.personnelLoadRate.rate ?? 0))

const cards = computed(() => {
  const d = kpiData.value
  if (!d) return []
  return [
    { title: '项目总数', value: String(d.contractStatusDistribution.total), color: '#1f3f70', onClick: () => void router.push('/project/list') },
    { title: '报修处理率', value: `${d.repairProcessingRate.rate}%`, color: d.repairProcessingRate.rate >= 80 ? '#22c55e' : '#ef4444', onClick: () => void router.push({ path: '/repair/list', query: { status: '待处理' } }) },
    { title: '人员负载率', value: `${d.personnelLoadRate.rate}%`, color: d.personnelLoadRate.rate >= 50 ? '#22c55e' : '#f59e0b', onClick: () => void router.push('/permission/manage') },
    { title: '巡检完成率', value: `${d.inspectionCompletionRate.rate}%`, color: d.inspectionCompletionRate.rate >= 80 ? '#22c55e' : '#ef4444', onClick: () => void router.push('/inspection/plan') },
    { title: '医院覆盖数', value: String(d.hospitalProductCoverage.hospitalCount), color: '#3b82f6', onClick: () => void router.push('/project/list') },
  ]
})

const renderCharts = () => {
  const d = kpiData.value
  if (!d) return

  // 合同状态占比 — 饼图
  if (contractChart) {
    const data = d.contractStatusDistribution.items.map(i => ({ name: i.status, value: i.count }))
    contractChart.setOption({
      color: ['#3b82f6', '#14b8a6', '#f59e0b', '#ef4444', '#8b5cf6', '#6b7280'],
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
            color: [[0.5, '#ef4444'], [0.8, '#f59e0b'], [1, '#22c55e']],
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
            color: [[0.5, '#ef4444'], [0.8, '#f59e0b'], [1, '#22c55e']],
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
            const colors = ['#3b82f6', '#14b8a6', '#f59e0b', '#8b5cf6']
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
          { value: pLoad.activePersonnel, name: '有工时记录人员', itemStyle: { color: '#3b82f6' } },
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
          fill: '#1f3f70',
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
  if (contractChartRef.value) contractChart = echarts.init(contractChartRef.value)
  if (repairChartRef.value) repairChart = echarts.init(repairChartRef.value)
  if (inspectionChartRef.value) inspectionChart = echarts.init(inspectionChartRef.value)
  if (coverageChartRef.value) coverageChart = echarts.init(coverageChartRef.value)
  if (loadChartRef.value) loadChart = echarts.init(loadChartRef.value)

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
  gap: 12px;
}

.chart-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
}

.bottom-row {
  margin-top: 0;
}

.chart-card {
  min-height: 360px;
}

.clickable {
  cursor: pointer;
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

@media (max-width: 1280px) {
  .chart-grid {
    grid-template-columns: 1fr;
  }

  .chart-box {
    height: 300px;
  }
}
</style>
