<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">首页</h2>
        <div class="page-subtitle">平台总览与关键指标入口</div>
      </div>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="6" v-for="card in cards" :key="card.title">
        <el-card shadow="never" class="stat-card stats-card">
          <div class="t">{{ card.title }}</div>
          <div class="v">{{ card.value }}</div>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="table-card share-card">
      <template #header>
        <div class="panel-title">页面占比（细分）</div>
      </template>
      <div class="share-grid">
        <div v-for="item in pageShareItems" :key="item.key" class="share-item">
          <div class="share-main">
            <div class="share-left">
              <div class="share-icon">{{ item.icon }}</div>
              <div>
                <div class="share-name share-link" @click="navigateByItem(item.key)">{{ item.title }}</div>
                <div class="share-detail">总量 {{ item.count }}，占总盘子 {{ item.rateText }}</div>
              </div>
            </div>
            <div class="share-right">
              <div class="share-count">{{ item.count }}</div>
              <div class="share-rate">{{ item.rateText }}</div>
            </div>
          </div>
          <div class="share-bar">
            <span :style="{ width: `${item.ratePercent}%` }"></span>
          </div>
          <div class="share-sub-grid">
            <div
              v-for="segment in item.segments"
              :key="`${item.key}-${segment.label}`"
              class="share-sub-item share-link"
              @click="navigateBySegment(item.key, segment.label)"
            >
              <span class="sub-name">{{ segment.label }}</span>
              <span class="sub-value">{{ segment.count }}</span>
              <span class="sub-rate" :class="`tone-${segment.tone}`">{{ segment.percentText }}</span>
            </div>
          </div>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useRouter } from 'vue-router'
import { fetchProjectList } from '../../api/modules/project'
import { fetchContractAlertSummary } from '../../api/modules/contract'
import { fetchHandoverSummary } from '../../api/modules/handover'
import { fetchInspectionSummary } from '../../api/modules/inspection'
import { fetchAnnualReportSummary } from '../../api/modules/annual-report'
import { fetchHospitalSummary } from '../../api/modules/hospital'
import { fetchPersonnelSummary } from '../../api/modules/personnel'
import { fetchProductSummary } from '../../api/modules/product'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

const cards = ref([
  { title: '项目总数', value: '0' },
  { title: '超期合同', value: '0' },
  { title: '进行中交接', value: '0' },
  { title: '本月巡检', value: '0' },
])

type PageShareItem = {
  key: string
  title: string
  icon: string
  count: number
  ratePercent: number
  rateText: string
  segments: Array<{
    label: string
    count: number
    percentText: string
    tone: 'danger' | 'warning' | 'success' | 'info'
  }>
}

const pageShareItems = ref<PageShareItem[]>([])
const router = useRouter()

const navigateByItem = async (key: string) => {
  const routeNameMap: Record<string, string> = {
    project: 'project-list',
    contract: 'contract-alerts',
    handover: 'handover-list',
    inspection: 'inspection-plan',
    annual: 'annual-report-list',
    hospital: 'hospital-list',
    personnel: 'personnel-list',
    product: 'product-list',
  }

  const name = routeNameMap[key]
  if (!name) return
  await router.push({ name })
}

const navigateBySegment = async (key: string, label: string) => {
  const drillQueryMap: Record<string, Record<string, Record<string, string>>> = {
    project: {
      签署: { contractStatus: '合同已签署' },
      超期: { contractStatus: '超期未签署' },
      免费: { contractStatus: '免费维护期' },
      停止维护: { contractStatus: '停止维护' },
      未知: { contractStatus: '未知' },
    },
    contract: {
      提醒: { alertLevel: '提醒' },
      警告: { alertLevel: '警告' },
      严重: { alertLevel: '严重' },
    },
    handover: {
      待交接: { stage: '未发' },
      已发邮件: { stage: '已发邮件' },
      进行中: { stage: '交接中' },
      已完成: { stage: '已交接' },
    },
    inspection: {
      已计划: { status: '已计划' },
      执行中: { status: '执行中' },
      已完成: { status: '已完成' },
      已取消: { status: '已取消' },
    },
    annual: {
      未开始: { status: '未开始' },
      编写中: { status: '编写中' },
      已提交: { status: '已提交' },
      已完成: { status: '已完成' },
    },
    hospital: {
      三级: { tier: '三级' },
      二级: { tier: '二级' },
      一级: { tier: '一级' },
    },
    personnel: {
      服务: { roleType: '服务' },
      实施: { roleType: '实施' },
      驻场: { isOnsite: 'true' },
    },
    product: {
      在用: { status: '运行中' },
      试点: { status: '试运行' },
      退役: { status: '已停用' },
    },
  }

  const routeNameMap: Record<string, string> = {
    project: 'project-list',
    contract: 'contract-alerts',
    handover: 'handover-list',
    inspection: 'inspection-plan',
    annual: 'annual-report-list',
    hospital: 'hospital-list',
    personnel: 'personnel-list',
    product: 'product-list',
  }

  const name = routeNameMap[key]
  if (!name) return

  const query = drillQueryMap[key]?.[label]
  await router.push({ name, query })
}

const toPercentText = (count: number, total: number): string => {
  if (total <= 0) {
    return '0.0%'
  }

  return `${((count / total) * 100).toFixed(1)}%`
}

const parseDateText = (text?: string): Date | null => {
  if (!text) {
    return null
  }

  const matched = text.match(/^(\d{4})-(\d{1,2})-(\d{1,2})$/)
  if (!matched) {
    return null
  }

  const year = Number(matched[1])
  const month = Number(matched[2])
  const day = Number(matched[3])
  const date = new Date(year, month - 1, day)
  return Number.isNaN(date.getTime()) ? null : date
}

const isProjectOverdue = (item: { overdueDays: number; afterSalesEndDate?: string }) => {
  if (item.overdueDays > 0) {
    return true
  }

  const target = parseDateText(item.afterSalesEndDate)
  if (!target) {
    return false
  }

  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  return target.getTime() < today.getTime()
}

const buildSegments = (
  total: number,
  rows: Array<{ label: string; count: number; tone: 'danger' | 'warning' | 'success' | 'info' }>,
) => {
  return rows.map((row) => ({
    ...row,
    percentText: toPercentText(row.count, total),
  }))
}

const loadDashboard = async () => {
  try {
    const [projectRes, contractRes, handoverRes, inspectionRes, annualRes, hospitalRes, personnelRes, productRes] = await Promise.all([
      fetchProjectList({ page: 1, size: 5000 }),
      fetchContractAlertSummary(),
      fetchHandoverSummary(),
      fetchInspectionSummary(),
      fetchAnnualReportSummary(),
      fetchHospitalSummary(),
      fetchPersonnelSummary(),
      fetchProductSummary(),
    ])

    cards.value = [
      { title: '项目总数', value: String(projectRes.data.total) },
      { title: '超期合同', value: String(contractRes.data.total) },
      { title: '进行中交接', value: String(handoverRes.data.inProgressCount) },
      { title: '本月巡检', value: String(inspectionRes.data.thisMonthCount) },
    ]

    const projectItems = projectRes.data.items
    const projectStatus = {
      signed: 0,
      overdue: 0,
      free: 0,
      stopped: 0,
      unknown: 0,
    }

    for (const item of projectItems) {
      const status = item.contractStatus ?? ''
      if (status.includes('超期')) {
        projectStatus.overdue++
      } else if (status.includes('免费')) {
        projectStatus.free++
      } else if (status.includes('停止')) {
        projectStatus.stopped++
      } else if (status.includes('签署')) {
        projectStatus.signed++
      } else {
        if (isProjectOverdue(item)) {
          projectStatus.overdue++
        } else {
          projectStatus.unknown++
        }
      }
    }

    const rawItems = [
      {
        key: 'project',
        title: '项目台账',
        icon: '📒',
        count: projectRes.data.total,
        segments: buildSegments(projectRes.data.total, [
          { label: '签署', count: projectStatus.signed, tone: 'success' },
          { label: '超期', count: projectStatus.overdue, tone: 'danger' },
          { label: '免费', count: projectStatus.free, tone: 'info' },
          { label: '停止维护', count: projectStatus.stopped, tone: 'warning' },
          { label: '未知', count: projectStatus.unknown, tone: 'info' },
        ]),
      },
      {
        key: 'contract',
        title: '合同预警',
        icon: '⚠️',
        count: contractRes.data.total,
        segments: buildSegments(contractRes.data.total, [
          { label: '提醒', count: contractRes.data.reminderCount, tone: 'info' },
          { label: '警告', count: contractRes.data.warningCount, tone: 'warning' },
          { label: '严重', count: contractRes.data.criticalCount, tone: 'danger' },
        ]),
      },
      {
        key: 'handover',
        title: '交接管理',
        icon: '🔁',
        count: handoverRes.data.total,
        segments: buildSegments(handoverRes.data.total, [
          { label: '待交接', count: handoverRes.data.pendingCount, tone: 'warning' },
          { label: '已发邮件', count: handoverRes.data.emailSentCount, tone: 'info' },
          { label: '进行中', count: handoverRes.data.inProgressCount, tone: 'warning' },
          { label: '已完成', count: handoverRes.data.completedCount, tone: 'success' },
        ]),
      },
      {
        key: 'inspection',
        title: '巡检计划',
        icon: '🗓️',
        count: inspectionRes.data.total,
        segments: buildSegments(inspectionRes.data.total, [
          { label: '已计划', count: inspectionRes.data.plannedCount, tone: 'info' },
          { label: '执行中', count: inspectionRes.data.inProgressCount, tone: 'warning' },
          { label: '已完成', count: inspectionRes.data.completedCount, tone: 'success' },
          { label: '已取消', count: inspectionRes.data.cancelledCount, tone: 'danger' },
        ]),
      },
      {
        key: 'annual',
        title: '年度报告',
        icon: '📝',
        count: annualRes.data.total,
        segments: buildSegments(annualRes.data.total, [
          { label: '未开始', count: annualRes.data.notStartedCount, tone: 'warning' },
          { label: '编写中', count: annualRes.data.writingCount, tone: 'info' },
          { label: '已提交', count: annualRes.data.submittedCount, tone: 'success' },
          { label: '已完成', count: annualRes.data.completedCount, tone: 'success' },
        ]),
      },
      {
        key: 'hospital',
        title: '医院管理',
        icon: '🏥',
        count: hospitalRes.data.total,
        segments: buildSegments(hospitalRes.data.total, [
          { label: '三级', count: hospitalRes.data.threeTierCount, tone: 'success' },
          { label: '二级', count: hospitalRes.data.twoTierCount, tone: 'info' },
          { label: '一级', count: hospitalRes.data.oneTierCount, tone: 'warning' },
        ]),
      },
      {
        key: 'personnel',
        title: '人员管理',
        icon: '👥',
        count: personnelRes.data.total,
        segments: buildSegments(personnelRes.data.total, [
          { label: '服务', count: personnelRes.data.serviceCount, tone: 'info' },
          { label: '实施', count: personnelRes.data.implementationCount, tone: 'info' },
          { label: '驻场', count: personnelRes.data.onsiteCount, tone: 'success' },
        ]),
      },
      {
        key: 'product',
        title: '产品管理',
        icon: '📦',
        count: productRes.data.total,
        segments: buildSegments(productRes.data.total, [
          { label: '在用', count: productRes.data.activeCount, tone: 'success' },
          { label: '试点', count: productRes.data.pilotCount, tone: 'warning' },
          { label: '退役', count: productRes.data.retiredCount, tone: 'danger' },
        ]),
      },
    ]

    const total = rawItems.reduce((sum, item) => sum + item.count, 0)
    pageShareItems.value = rawItems.map((item) => {
      const rate = total > 0 ? (item.count / total) * 100 : 0
      return {
        ...item,
        ratePercent: Number(rate.toFixed(1)),
        rateText: `${rate.toFixed(1)}%`,
      }
    })
  } catch (error) {
    pageShareItems.value = []
    ElMessage.error(getErrorMessage(error, '加载首页数据失败，请稍后重试'))
  }
}

useLinkedRealtimeRefresh({
  refresh: loadDashboard,
  scope: 'global',
  intervalMs: 10000,
})

onMounted(() => {
  void loadDashboard()
})
</script>

<style scoped>
.panel-title {
  font-weight: 600;
}

.share-card {
  margin-top: 0;
}

.share-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.share-item {
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 10px 12px;
  background: var(--el-bg-color);
}

.share-main {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.share-left {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.share-icon {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--el-fill-color-light);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
}

.share-name {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.share-detail {
  margin-top: 2px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 280px;
}

.share-right {
  text-align: right;
  flex-shrink: 0;
}

.share-count {
  font-size: 16px;
  font-weight: 700;
  color: var(--el-text-color-primary);
}

.share-rate {
  font-size: 12px;
  color: var(--el-color-primary);
}

.share-bar {
  margin-top: 8px;
  height: 6px;
  border-radius: 999px;
  background: var(--el-fill-color-lighter);
  overflow: hidden;
}

.share-bar > span {
  display: block;
  height: 100%;
  border-radius: 999px;
  background: var(--el-color-primary);
}

.share-sub-grid {
  margin-top: 8px;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 6px 10px;
}

.share-sub-item {
  display: grid;
  grid-template-columns: 1fr auto auto;
  align-items: center;
  gap: 6px;
  font-size: 12px;
}

.share-link {
  cursor: pointer;
}

.share-link:hover {
  opacity: 0.85;
}

.sub-name {
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sub-value {
  color: var(--el-text-color-regular);
  font-weight: 600;
}

.sub-rate {
  font-variant-numeric: tabular-nums;
}

.sub-rate.tone-danger {
  color: var(--el-color-danger);
}

.sub-rate.tone-warning {
  color: var(--el-color-warning);
}

.sub-rate.tone-success {
  color: var(--el-color-success);
}

.sub-rate.tone-info {
  color: var(--el-color-primary);
}

@media (max-width: 1200px) {
  .share-grid {
    grid-template-columns: 1fr;
  }

  .share-detail {
    max-width: 100%;
  }

  .share-sub-grid {
    grid-template-columns: 1fr;
  }
}
</style>
