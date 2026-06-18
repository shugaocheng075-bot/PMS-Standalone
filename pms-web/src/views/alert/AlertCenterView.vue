<template>
  <div class="page-shell">
    <div class="alert-hero">
      <div class="alert-hero-main">
        <div class="alert-hero-kicker-row">
          <span class="alert-hero-kicker">Risk Command Center</span>
          <span class="alert-hero-badge">{{ activeFilterLabel }}</span>
        </div>
        <h2 class="alert-hero-title">统一预警中心</h2>
        <div class="alert-hero-subtitle">
          聚合合同、交接、巡检三类待办风险，先从这里判断风险浓度和来源分布，再继续下钻到具体执行模块处理，不必在多个列表之间反复切换。
        </div>

        <div class="alert-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="alert-signal-card">
            <span class="alert-signal-label">{{ item.label }}</span>
            <strong class="alert-signal-value">{{ item.value }}</strong>
            <span class="alert-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="alert-hero-side">
        <div class="alert-control-card">
          <div class="alert-control-copy">
            <span class="alert-control-title">风险操作</span>
            <span class="alert-control-note">当前列表共 {{ total }} 条风险项，可继续筛选、重置或导出。</span>
          </div>
          <div class="alert-control-actions">
            <el-button size="small" @click="onSearch" icon="Search">刷新列表</el-button>
            <el-button size="small" @click="onReset" icon="Refresh">重置筛选</el-button>
            <el-button size="small" :loading="exporting" @click="onExport" icon="Download">导出</el-button>
          </div>
        </div>

        <div class="alert-quick-grid">
          <button v-for="action in quickActions" :key="action.title" type="button" class="alert-quick-action" @click="action.onClick()">
            <span class="alert-quick-title">{{ action.title }}</span>
            <span class="alert-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <SummaryMetrics :items="summaryCards" :columns="6" @select="onSummaryCardSelect" />

    <div class="alert-insight-grid">
      <section class="alert-insight-card">
        <div class="alert-insight-head">
          <div>
            <div class="alert-insight-title">优先处理清单</div>
            <div class="alert-insight-note">按级别和逾期天数排序，优先下钻最急迫的预警项。</div>
          </div>
          <el-tag size="small" type="danger" effect="light">{{ priorityQueue.length }} 项</el-tag>
        </div>
        <div v-if="priorityQueue.length" class="alert-queue-list">
          <button
            v-for="item in priorityQueue"
            :key="item.id"
            type="button"
            class="alert-queue-item"
            @click="onGoto(item)"
          >
            <div class="alert-queue-main">
              <strong>{{ item.hospitalName || '未填写医院' }}</strong>
              <span>{{ item.title }}</span>
            </div>
            <div class="alert-queue-meta">
              <el-tag size="small" :type="levelTagType(item.level)">{{ item.level }}</el-tag>
              <span>{{ item.owner || '未分配责任人' }} · {{ item.overdueDays }} 天</span>
            </div>
          </button>
        </div>
        <el-empty v-else description="当前筛选下没有需要优先推进的预警项" :image-size="72" />
      </section>

      <section class="alert-insight-card">
        <div class="alert-insight-head">
          <div>
            <div class="alert-insight-title">医院关注</div>
            <div class="alert-insight-note">看预警最集中的医院，便于主管按医院专题推进处理。</div>
          </div>
          <span class="alert-insight-meta">{{ overviewRows.length }} 项预警</span>
        </div>
        <div v-if="topHospitalBuckets.length" class="alert-chip-list">
          <div v-for="item in topHospitalBuckets" :key="item.label" class="alert-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 项</span>
          </div>
        </div>
        <el-empty v-else description="暂无医院分布" :image-size="72" />
      </section>

      <section class="alert-insight-card">
        <div class="alert-insight-head">
          <div>
            <div class="alert-insight-title">责任人负载</div>
            <div class="alert-insight-note">按当前预警责任人分布查看负载，便于及时分流和跟催。</div>
          </div>
          <span class="alert-insight-meta">{{ activeOwnerCount }} 个责任人口径</span>
        </div>
        <div v-if="topOwnerBuckets.length" class="alert-chip-list">
          <div v-for="item in topOwnerBuckets" :key="item.label" class="alert-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 项</span>
          </div>
        </div>
        <el-empty v-else description="暂无责任人分布" :image-size="72" />
      </section>

      <section class="alert-insight-card">
        <div class="alert-insight-head">
          <div>
            <div class="alert-insight-title">来源结构</div>
            <div class="alert-insight-note">快速判断当前是合同风险、交接待办还是巡检异常更集中。</div>
          </div>
          <span class="alert-insight-meta">{{ activeFilterLabel }}</span>
        </div>
        <div v-if="sourceBuckets.length" class="alert-chip-list">
          <div v-for="item in sourceBuckets" :key="item.label" class="alert-chip alert-chip--soft">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 项</span>
          </div>
        </div>
        <el-empty v-else description="暂无来源结构" :image-size="72" />
      </section>
    </div>

    <AppFilterCard>
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="来源">
          <el-select v-model="query.source" clearable placeholder="全部" style="width: 130px">
            <el-option label="合同" value="合同" />
            <el-option label="交接" value="交接" />
            <el-option label="巡检" value="巡检" />
          </el-select>
        </el-form-item>
        <el-form-item label="级别">
          <el-select v-model="query.level" clearable placeholder="全部" style="width: 130px">
            <el-option label="严重" value="严重" />
            <el-option label="警告" value="警告" />
            <el-option label="提醒" value="提醒" />
          </el-select>
        </el-form-item>
        <el-form-item label="关键字">
          <el-input v-model="query.keyword" clearable placeholder="医院/负责人/内容" style="width: 220px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
          <el-button :loading="exporting" @click="onExport" icon="Download">导出CSV</el-button>
        </el-form-item>
      </el-form>
    </AppFilterCard>

    <AppTableCard>
      <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无待办预警" @row-dblclick="onGoto">
        <el-table-column prop="source" label="来源" width="90" />
        <el-table-column prop="level" label="级别" width="90">
          <template #default="scope">
            <el-tag :type="levelTagType(scope.row.level)">{{ scope.row.level }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="hospitalName" label="医院" min-width="180" show-overflow-tooltip />
        <el-table-column prop="title" label="标题" min-width="220" show-overflow-tooltip />
        <el-table-column prop="detail" label="详情" min-width="260" show-overflow-tooltip />
        <el-table-column prop="owner" label="责任人" width="120" show-overflow-tooltip />
        <el-table-column prop="overdueDays" label="超期天数" width="100" align="right" />
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="scope">
            <el-button link type="primary" @click="onGoto(scope.row)" icon="Service">去处理</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pager">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.size"
          :page-sizes="[15, 30, 50, 100]"
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
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { fetchAlertCenter, exportAlertCenter, type AlertCenterItem } from '../../api/modules/alertCenter'
import { getErrorMessage } from '../../utils/error'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useResilientLoad } from '../../composables/useResilientLoad'
import AppFilterCard from '../../components/AppFilterCard.vue'
import AppTableCard from '../../components/AppTableCard.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'

const router = useRouter()
const route = useRoute()

const loading = ref(false)
const exporting = ref(false)
const tableData = ref<AlertCenterItem[]>([])
const overviewRows = ref<AlertCenterItem[]>([])
const total = ref(0)
const summary = reactive({
  total: 0,
  severe: 0,
  warning: 0,
  reminder: 0,
  contract: 0,
  handover: 0,
  inspection: 0,
})

const query = reactive({
  source: '',
  level: '',
  keyword: '',
  page: 1,
  size: 15,
})

type AlertSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active: boolean
}

type AlertBucket = {
  label: string
  value: number
}

const buildBuckets = (items: AlertCenterItem[], resolveLabel: (item: AlertCenterItem) => string, limit = 5): AlertBucket[] => {
  const counts = new Map<string, number>()
  items.forEach((item) => {
    const label = resolveLabel(item).trim() || '未设置'
    counts.set(label, (counts.get(label) ?? 0) + 1)
  })

  return Array.from(counts.entries())
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => (b.value - a.value) || a.label.localeCompare(b.label, 'zh-CN'))
    .slice(0, limit)
}

const priorityQueue = computed(() => overviewRows.value
  .slice()
  .sort((left, right) => {
    const priorityDiff = right.priority - left.priority
    if (priorityDiff !== 0) {
      return priorityDiff
    }

    return right.overdueDays - left.overdueDays
  })
  .slice(0, 5))

const topHospitalBuckets = computed(() => buildBuckets(overviewRows.value, (item) => item.hospitalName || '未填写医院'))
const topOwnerBuckets = computed(() => buildBuckets(overviewRows.value, (item) => item.owner || '未分配责任人'))
const sourceBuckets = computed(() => buildBuckets(overviewRows.value, (item) => item.source || '未设置来源', 6))
const activeOwnerCount = computed(() => new Set(overviewRows.value.map((item) => (item.owner || '未分配责任人').trim() || '未分配责任人')).size)

const rawSummaryCards = computed(() => [
  {
    title: '严重',
    context: '级别筛选',
    note: '优先处理高风险待办项',
    value: summary.severe,
    color: '#c58a87',
    active: query.level === '严重',
    onClick: () => filterByLevel('严重'),
  },
  {
    title: '警告',
    context: '级别筛选',
    note: '跟进临近阈值的风险项',
    value: summary.warning,
    color: '#c7a06c',
    active: query.level === '警告',
    onClick: () => filterByLevel('警告'),
  },
  {
    title: '提醒',
    context: '级别筛选',
    note: '常规待办与预警提醒汇总',
    value: summary.reminder,
    color: '#7c98bc',
    active: query.level === '提醒',
    onClick: () => filterByLevel('提醒'),
  },
  {
    title: '合同',
    context: '来源筛选',
    note: '查看合同临期与到期提醒',
    value: summary.contract,
    color: '#7c98bc',
    active: query.source === '合同',
    onClick: () => filterBySource('合同'),
  },
  {
    title: '交接',
    context: '来源筛选',
    note: '聚焦交接阶段待推进事项',
    value: summary.handover,
    color: '#8db3a8',
    active: query.source === '交接',
    onClick: () => filterBySource('交接'),
  },
  {
    title: '巡检',
    context: '来源筛选',
    note: '查看巡检任务与健康等级异常',
    value: summary.inspection,
    color: '#9388bf',
    active: query.source === '巡检',
    onClick: () => filterBySource('巡检'),
  },
])

const activeFilterLabel = computed(() => {
  if (query.level) {
    return `级别：${query.level}`
  }

  if (query.source) {
    return `来源：${query.source}`
  }

  if (query.keyword) {
    return `检索：${query.keyword}`
  }

  return '全量风险视图'
})

const heroSignals = computed(() => [
  {
    label: '风险总量',
    value: String(summary.total),
    note: '当前数据范围内聚合的全部预警条目',
  },
  {
    label: '严重风险',
    value: String(summary.severe),
    note: '需要优先处理的高风险待办项',
  },
  {
    label: '合同来源',
    value: String(summary.contract),
    note: '合同临期与到期类风险数量',
  },
  {
    label: '交接 + 巡检',
    value: String(summary.handover + summary.inspection),
    note: '执行链路中的交接与巡检积压总量',
  },
])

const summaryCardKeys = ['severe', 'warning', 'reminder', 'contract', 'handover', 'inspection'] as const
const summaryCards = computed<AlertSummaryCard[]>(() => rawSummaryCards.value.map((card, index) => ({
  ...card,
  key: summaryCardKeys[index],
})))

type AlertFilterState = {
  source: string
  level: string
  keyword: string
  page: number
  size: number
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<AlertFilterState>({
  key: 'alert-center',
  getState: () => ({ source: query.source, level: query.level, keyword: query.keyword, page: query.page, size: query.size }),
  applyState: (state) => {
    query.source = state.source ?? ''
    query.level = state.level ?? ''
    query.keyword = state.keyword ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

useLinkedRealtimeRefresh({
  refresh: async () => { await loadData() },
  scope: 'alert',
  intervalMs: 60000,
})

const { runInitialLoad } = useResilientLoad()

const readRouteQueryValue = (value: unknown): string => {
  if (typeof value === 'string') {
    return value
  }

  if (Array.isArray(value) && typeof value[0] === 'string') {
    return value[0]
  }

  return ''
}

const applyRouteFilters = () => {
  const source = readRouteQueryValue(route.query.source)
  const level = readRouteQueryValue(route.query.level)
  const keyword = readRouteQueryValue(route.query.keyword)
  let applied = false

  if (source) {
    query.source = source
    applied = true
  }

  if (level) {
    query.level = level
    applied = true
  }

  if (keyword) {
    query.keyword = keyword
    applied = true
  }

  if (applied) {
    query.page = 1
  }
}

const levelTagType = (level: string) => {
  if (level === '严重') return 'danger'
  if (level === '警告') return 'warning'
  return 'info'
}

const loadOverview = async () => {
  const res = await fetchAlertCenter({
    source: query.source,
    level: query.level,
    keyword: query.keyword,
    page: 1,
    size: 5000,
  })
  overviewRows.value = res.data.items
}

const loadData = async () => {
  loading.value = true
  try {
    const [pageRes] = await Promise.all([
      fetchAlertCenter(query),
      loadOverview(),
    ])
    const res = pageRes
    tableData.value = res.data.items
    total.value = res.data.total
    summary.total = res.data.summary.total
    summary.severe = res.data.summary.severe
    summary.warning = res.data.summary.warning
    summary.reminder = res.data.summary.reminder
    summary.contract = res.data.summary.contract
    summary.handover = res.data.summary.handover
    summary.inspection = res.data.summary.inspection
  } catch (error) {
    tableData.value = []
    overviewRows.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载统一预警中心失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.source = ''
  query.level = ''
  query.keyword = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  loadData()
}

const onExport = async () => {
  exporting.value = true
  try {
    const blob = await exportAlertCenter(query)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `预警中心-${Date.now()}.csv`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  } finally {
    exporting.value = false
  }
}

const filterBySource = (source: string) => {
  query.source = source
  query.level = ''
  query.keyword = ''
  query.page = 1
  loadData()
}

const filterByLevel = (level: string) => {
  query.source = ''
  query.level = level
  query.keyword = ''
  query.page = 1
  loadData()
}

const onSummaryCardSelect = (card: { key?: string | number }) => {
  if (typeof card.key !== 'string') {
    return
  }

  if (card.key === 'severe') {
    filterByLevel('涓ラ噸')
    return
  }

  if (card.key === 'warning') {
    filterByLevel('璀﹀憡')
    return
  }

  if (card.key === 'reminder') {
    filterByLevel('鎻愰啋')
    return
  }

  if (card.key === 'contract') {
    filterBySource('鍚堝悓')
    return
  }

  if (card.key === 'handover') {
    filterBySource('浜ゆ帴')
    return
  }

  if (card.key === 'inspection') {
    filterBySource('宸℃')
    return
  }
}

const quickActions = computed(() => [
  {
    title: '严重风险',
    note: '只看高优先级风险待办',
    onClick: () => filterByLevel('严重'),
  },
  {
    title: '合同预警',
    note: '切到合同临期与到期提醒',
    onClick: () => filterBySource('合同'),
  },
  {
    title: '交接待办',
    note: '聚焦交接阶段未完成事项',
    onClick: () => filterBySource('交接'),
  },
  {
    title: '巡检异常',
    note: '查看巡检执行与健康等级异常',
    onClick: () => filterBySource('巡检'),
  },
])

const onGoto = (row: AlertCenterItem) => {
  router.push({
    path: row.relatedPath,
    query: row.relatedQuery,
  })
}

onMounted(async () => {
  restoreFilterState()
  applyRouteFilters()
  await runInitialLoad({
    tasks: [loadData],
    retryChecks: [
      { when: () => summary.total > 0 && total.value === 0, task: loadData },
    ],
  })
})

watch(() => route.fullPath, () => {
  applyRouteFilters()
  loadData()
})
</script>

<style scoped>
.alert-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.18fr) minmax(320px, 0.82fr);
  gap: 16px;
  padding: 24px;
  border-radius: 28px;
  color: #ffffff;
  background:
    radial-gradient(circle at 12% 18%, rgba(255, 184, 184, 0.18), transparent 22%),
    radial-gradient(circle at 100% 0, rgba(236, 214, 122, 0.14), transparent 24%),
    linear-gradient(145deg, #3f2d52 0%, #6b3b59 48%, #8b5737 100%);
  box-shadow: 0 26px 44px rgba(80, 43, 53, 0.18);
}

.alert-hero-main {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-width: 0;
}

.alert-hero-kicker-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.alert-hero-kicker,
.alert-hero-badge {
  display: inline-flex;
  align-items: center;
  padding: 7px 12px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 700;
}

.alert-hero-kicker {
  border: 1px solid rgba(255, 255, 255, 0.18);
  background: rgba(255, 255, 255, 0.08);
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.alert-hero-badge {
  background: rgba(255, 255, 255, 0.14);
  color: #f9f4ef;
}

.alert-hero-title {
  margin: 0;
  font-size: 34px;
  line-height: 1.12;
  font-weight: 700;
}

.alert-hero-subtitle {
  max-width: 700px;
  font-size: 14px;
  line-height: 1.85;
  color: rgba(249, 241, 233, 0.84);
}

.alert-hero-signals {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin-top: 4px;
}

.alert-signal-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.11), rgba(255, 255, 255, 0.05));
}

.alert-signal-label {
  font-size: 12px;
  color: rgba(248, 235, 228, 0.76);
}

.alert-signal-value {
  font-size: 28px;
  line-height: 1;
  font-weight: 700;
}

.alert-signal-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(246, 236, 230, 0.76);
}

.alert-hero-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.alert-control-card,
.alert-quick-action {
  border: 1px solid rgba(255, 255, 255, 0.14);
  border-radius: 20px;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.14), rgba(255, 255, 255, 0.08));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

.alert-control-card {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 18px;
}

.alert-control-copy {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.alert-control-title {
  font-size: 16px;
  font-weight: 700;
}

.alert-control-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(245, 234, 226, 0.74);
}

.alert-control-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.alert-control-actions :deep(.el-button) {
  min-height: 40px;
}

.alert-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.alert-quick-action {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  color: #ffffff;
  text-align: left;
  cursor: pointer;
  transition: transform 0.2s ease, border-color 0.2s ease, background 0.2s ease;
}

.alert-quick-action:hover {
  transform: translateY(-1px);
  border-color: rgba(255, 255, 255, 0.24);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.18), rgba(255, 255, 255, 0.09));
}

.alert-quick-title {
  font-size: 14px;
  font-weight: 700;
}

.alert-quick-note {
  font-size: 12px;
  line-height: 1.65;
  color: rgba(247, 236, 229, 0.76);
}

.alert-insight-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
  margin-top: 16px;
}

.alert-insight-card {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 18px 20px;
  border-radius: 20px;
  background: #ffffff;
  border: 1px solid rgba(148, 163, 184, 0.16);
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.06);
}

.alert-insight-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.alert-insight-title {
  font-size: 15px;
  font-weight: 700;
  color: #111827;
}

.alert-insight-note {
  margin-top: 4px;
  font-size: 12px;
  line-height: 1.6;
  color: #64748b;
}

.alert-insight-meta {
  font-size: 12px;
  font-weight: 600;
  color: #475569;
  white-space: nowrap;
}

.alert-queue-list,
.alert-chip-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.alert-queue-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  padding: 12px 14px;
  border-radius: 16px;
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: #f8fafc;
  cursor: pointer;
  text-align: left;
  transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
}

.alert-queue-item:hover {
  transform: translateY(-1px);
  border-color: rgba(59, 130, 246, 0.22);
  box-shadow: 0 12px 24px rgba(15, 23, 42, 0.08);
}

.alert-queue-main,
.alert-queue-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.alert-queue-main strong,
.alert-chip strong {
  font-size: 13px;
  font-weight: 700;
  color: #111827;
}

.alert-queue-main span,
.alert-queue-meta span,
.alert-chip span {
  font-size: 12px;
  line-height: 1.5;
  color: #64748b;
}

.alert-queue-meta {
  align-items: flex-end;
}

.alert-chip-list {
  gap: 8px;
}

.alert-chip {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 11px 14px;
  border-radius: 14px;
  background: #f8fafc;
  border: 1px solid rgba(148, 163, 184, 0.14);
}

.alert-chip--soft {
  background: #f5f7fb;
}

.metric-card--action {
  position: relative;
  overflow: hidden;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98), rgba(248, 245, 243, 0.96));
}

.metric-card--action::before {
  content: '';
  position: absolute;
  inset: 0 auto auto 0;
  width: 100%;
  height: 4px;
  background: linear-gradient(90deg, #8b5737 0%, #c58a87 100%);
  opacity: 0.92;
}

@media (max-width: 1280px) {
  .alert-hero {
    grid-template-columns: 1fr;
  }

  .alert-insight-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .alert-hero {
    padding: 18px;
  }

  .alert-hero-title {
    font-size: 28px;
  }

  .alert-hero-signals,
  .alert-quick-grid {
    grid-template-columns: 1fr;
  }

  .alert-control-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .alert-queue-item,
  .alert-chip,
  .alert-insight-head {
    align-items: flex-start;
    flex-direction: column;
  }

  .alert-queue-meta {
    align-items: flex-start;
  }
}
</style>
