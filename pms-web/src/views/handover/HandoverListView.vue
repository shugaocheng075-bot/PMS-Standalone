<template>
  <div class="page-shell">
    <div class="handover-hero">
      <div class="handover-hero-main">
        <div class="handover-hero-kicker-row">
          <span class="handover-hero-kicker">Handover Management</span>
          <span class="handover-hero-badge">{{ handoverFilterLabel }}</span>
        </div>
        <h2 class="handover-hero-title">交接管理</h2>
        <div class="handover-hero-subtitle">跟踪交接批次、阶段流转与看板进度</div>
        <div class="handover-hero-signals">
          <div v-for="item in handoverHeroSignals" :key="item.label" class="handover-signal-card">
            <span class="handover-signal-label">{{ item.label }}</span>
            <strong class="handover-signal-value">{{ item.value }}</strong>
            <span class="handover-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>
      <div class="handover-hero-side">
        <div class="handover-control-card">
          <div class="handover-control-title">交接推进台</div>
          <div class="handover-control-note">按阶段快速定位与推进交接任务</div>
          <div class="handover-control-actions">
            <el-button size="small" :loading="exporting" @click="onExport" icon="Download">导出CSV</el-button>
          </div>
        </div>
        <div class="handover-quick-grid">
          <button v-for="action in handoverQuickActions" :key="action.title" class="handover-quick-action" @click="action.onClick()">
            <span class="handover-quick-title">{{ action.title }}</span>
            <span class="handover-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <SummaryMetrics :items="summaryCards" :columns="5" @select="onSummaryCardSelect" />

    

    <ProTable
      title="明细数据列表"
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
            @row-dblclick="onRowDoubleClick"
    >
      <template #toolbar>
        
        <el-button :loading="exporting" @click="onExport" icon="Download">导出CSV</el-button>
      </template>

      <template #search>
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="阶段">
          <el-select v-model="query.stage" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="stage in stageOptions" :key="stage" :label="stage" :value="stage" />
          </el-select>
        </el-form-item>
        <el-form-item label="批次">
          <el-select v-model="query.batch" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="batch in batchOptions" :key="batch" :label="batch" :value="batch" />
          </el-select>
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="query.type" placeholder="全部" clearable style="width: 160px">
            <el-option v-for="type in typeOptions" :key="type" :label="type" :value="type" />
          </el-select>
        </el-form-item>
        <el-form-item label="原组别">
          <el-select v-model="query.fromGroup" placeholder="全部" clearable style="width: 160px">
            <el-option v-for="group in fromGroupOptions" :key="group" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="对接人">
          <el-select v-model="query.toOwner" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="owner in toOwnerOptions" :key="owner" :label="owner" :value="owner" />
          </el-select>
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
        </el-form-item>
      </el-form>
    </template>

    
      
        <el-table-column prop="handoverNo" label="交接单号" width="130" sortable />
        <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip sortable />
        <el-table-column prop="productName" label="产品" min-width="170" show-overflow-tooltip sortable />
        <el-table-column prop="type" label="类型" width="130" />
        <el-table-column prop="fromGroup" label="原组别" width="150" show-overflow-tooltip />
        <el-table-column prop="fromOwner" label="提出人" width="110" />
        <el-table-column prop="toOwner" label="对接人" width="110" />
        <el-table-column prop="batch" label="批次" width="110" sortable />
        <el-table-column prop="stage" label="阶段" width="120" sortable>
          <template #default="scope">
            <el-tag :type="stageTag(scope.row.stage)">{{ scope.row.stage }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="emailSentDate" label="邮件日期" width="150">
          <template #default="scope">
            {{ scope.row.emailSentDate ? formatDate(scope.row.emailSentDate) : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="340" fixed="right">
          <template #default="scope">
            <div class="table-action-group">
              <el-button type="primary" link @click="onOpenDetail(scope.row)" icon="Document">
                详情
              </el-button>
              <el-button
                v-if="canManageHandover && resolvePrimaryAction(scope.row)"
                :type="resolvePrimaryAction(scope.row)?.type ?? 'primary'"
                link
                :loading="isWorkflowLoading(scope.row.id, resolvePrimaryAction(scope.row)?.key as HandoverWorkflowAction)"
                :disabled="workflowBusy"
                @click="onPrimaryWorkflowAction(scope.row)"
              >
                {{ resolvePrimaryAction(scope.row)?.label }}
              </el-button>
              <el-button
                v-if="canRollbackHandover(scope.row)"
                type="warning"
                link
                :loading="isWorkflowLoading(scope.row.id, 'rollback')"
                :disabled="workflowBusy"
                @click="onRollbackWorkflow(scope.row)"
              >
                {{ rollbackActionLabel(scope.row) }}
              </el-button>
              <span v-if="!resolvePrimaryAction(scope.row) && !canRollbackHandover(scope.row)">-</span>
            </div>
          </template>
        </el-table-column>
      

      
    
    </ProTable>

    <ProDrawer v-model="detailVisible" title="交接详情" width="680px">
      <template v-if="detailItem">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="交接单号">{{ detailItem.handoverNo }}</el-descriptions-item>
          <el-descriptions-item label="阶段">
            <el-tag :type="stageTag(detailItem.stage)">{{ detailItem.stage }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="医院">{{ detailItem.hospitalName }}</el-descriptions-item>
          <el-descriptions-item label="产品">{{ detailItem.productName }}</el-descriptions-item>
          <el-descriptions-item label="类型">{{ detailItem.type }}</el-descriptions-item>
          <el-descriptions-item label="批次">{{ detailItem.batch || '-' }}</el-descriptions-item>
          <el-descriptions-item label="原组别">{{ detailItem.fromGroup || '-' }}</el-descriptions-item>
          <el-descriptions-item label="提出人">{{ detailItem.fromOwner || '-' }}</el-descriptions-item>
          <el-descriptions-item label="对接人">{{ detailItem.toOwner || '-' }}</el-descriptions-item>
          <el-descriptions-item label="当前责任节点">{{ responsibilityNode(detailItem) }}</el-descriptions-item>
          <el-descriptions-item label="邮件发送时间">{{ detailItem.emailSentDate ? formatDateTime(detailItem.emailSentDate) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="开始交接时间">{{ formatDateTime(detailItem.startedAt) }}</el-descriptions-item>
          <el-descriptions-item label="完成时间">{{ formatDateTime(detailItem.completedAt) }}</el-descriptions-item>
        </el-descriptions>

        <div class="detail-actions">
          <el-button plain @click="goToProjectPage(detailItem)">去项目台账</el-button>
          <el-button plain @click="goToInspectionPage(detailItem)">去巡检计划</el-button>
          <el-button
            v-if="canManageHandover && resolvePrimaryAction(detailItem)"
            :type="resolvePrimaryAction(detailItem)?.type ?? 'primary'"
            :loading="isWorkflowLoading(detailItem.id, resolvePrimaryAction(detailItem)?.key as HandoverWorkflowAction)"
            :disabled="workflowBusy"
            @click="onPrimaryWorkflowAction(detailItem)"
          >
            {{ resolvePrimaryAction(detailItem)?.label }}
          </el-button>
          <el-button
            v-if="canRollbackHandover(detailItem)"
            type="warning"
            :loading="isWorkflowLoading(detailItem.id, 'rollback')"
            :disabled="workflowBusy"
            @click="onRollbackWorkflow(detailItem)"
          >
            {{ rollbackActionLabel(detailItem) }}
          </el-button>
        </div>
      </template>
    </ProDrawer>

    <AppTableCard>
      <template #header>
        <div class="kanban-title">交接看板</div>
      </template>

      <el-row :gutter="12" class="kanban-row">
        <el-col :span="6" v-for="column in kanbanColumns" :key="column.stage">
          <div class="kanban-col">
            <div class="kanban-col-header">{{ column.stage }}（{{ column.count }}）</div>
            <div class="kanban-items">
              <div class="kanban-item clickable" v-for="item in column.items" :key="item.id" @click="onOpenDetail(item)">
                <div class="kanban-item-name">{{ item.hospitalName }}</div>
                <div class="kanban-item-meta">{{ item.handoverNo }} · {{ item.productName }} · {{ item.toOwner }}</div>
              </div>
              <div v-if="column.count === 0" class="kanban-empty">暂无数据</div>
            </div>
          </div>
        </el-col>
      </el-row>
    </AppTableCard>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import {
  fetchHandovers,
  fetchHandoverById,
  fetchHandoverKanban,
  fetchHandoverSummary,
  sendHandoverEmail,
  startHandover,
  completeHandover,
  rollbackHandover,
  exportHandovers,
} from '../../api/modules/handover'
import type { HandoverItem, HandoverKanbanColumn, HandoverSummary } from '../../types/handover'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { HANDOVER_GROUP_OPTIONS, HANDOVER_OWNER_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import ProTable from '../../components/ProTable.vue'
import ProDrawer from '../../components/ProDrawer.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'


const loading = ref(false)
const exporting = ref(false)
const total = ref(0)
const tableData = ref<HandoverItem[]>([])
const allRows = ref<HandoverItem[]>([])
const kanbanColumns = ref<HandoverKanbanColumn[]>([])
const summary = ref<HandoverSummary>({
  pendingCount: 0,
  emailSentCount: 0,
  inProgressCount: 0,
  completedCount: 0,
  total: 0,
})

const query = reactive({
  stage: '',
  batch: '',
  type: '',
  fromGroup: '',
  toOwner: '',
  page: 1,
  size: 15,
})

type HandoverSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active: boolean
}

const summaryCards = computed<HandoverSummaryCard[]>(() => [
  {
    key: '未发',
    title: '未发',
    value: summary.value.pendingCount,
    context: '交接阶段',
    note: '查看尚未发起邮件的交接任务',
    color: '#7c98bc',
    active: query.stage === '未发',
  },
  {
    key: '已发邮件',
    title: '已发邮件',
    value: summary.value.emailSentCount,
    context: '交接阶段',
    note: '查看已发起邮件等待承接的任务',
    color: '#8db3a8',
    active: query.stage === '已发邮件',
  },
  {
    key: '交接中',
    title: '交接中',
    value: summary.value.inProgressCount,
    context: '交接阶段',
    note: '聚焦仍在推进中的交接批次',
    color: '#c7a06c',
    active: query.stage === '交接中',
  },
  {
    key: '已交接',
    title: '已交接',
    value: summary.value.completedCount,
    context: '交接阶段',
    note: '查看已完成交接的历史记录',
    color: '#7d9f92',
    active: query.stage === '已交接',
  },
  {
    key: 'all',
    title: '总数',
    value: summary.value.total,
    context: '全量视图',
    note: '返回全部交接记录与阶段分布',
    color: '#3f4f63',
    active: query.stage === '',
  },
])
const route = useRoute()
const router = useRouter()
const detailVisible = ref(false)
const detailItem = ref<HandoverItem | null>(null)

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

const clearRouteActionQuery = async () => {
  if (!readRouteQueryValue(route.query.action) && !readRouteQueryValue(route.query.id)) {
    return
  }

  await updateRouteQuery({ action: undefined, id: undefined })
}

const applyDrillQuery = () => {
  const stage = readRouteQueryValue(route.query.stage)
  const fromGroup = readRouteQueryValue(route.query.fromGroup)
  const toOwner = readRouteQueryValue(route.query.toOwner)
  const batch = readRouteQueryValue(route.query.batch)
  const type = readRouteQueryValue(route.query.type)
  const hospitalName = readRouteQueryValue(route.query.hospitalName)
  const productName = readRouteQueryValue(route.query.productName)
  let applied = false

  if (stage) {
    query.stage = stage
    applied = true
  }

  if (fromGroup) {
    query.fromGroup = fromGroup
    applied = true
  }

  if (toOwner) {
    query.toOwner = toOwner
    applied = true
  }

  if (batch) {
    query.batch = batch
    applied = true
  }

  if (type) {
    query.type = type
    applied = true
  }

  if (hospitalName || productName) {
    applied = true
  }

  if (applied) {
    query.page = 1
  }
}

const getRouteHospitalName = () => readRouteQueryValue(route.query.hospitalName)

const getRouteProductName = () => readRouteQueryValue(route.query.productName)

const buildFilteredRows = (rows: HandoverItem[]) => {
  const hospitalName = getRouteHospitalName()
  const productName = getRouteProductName()

  return rows.filter((item) => {
    if (hospitalName && item.hospitalName !== hospitalName) {
      return false
    }

    if (productName && item.productName !== productName) {
      return false
    }

    return true
  })
}

type HandoverFilterState = {
  stage: string
  batch: string
  type: string
  fromGroup: string
  toOwner: string
  page: number
  size: number
}

const stageOptions = ref<string[]>(['未发', '已发邮件', '交接中', '已交接'])
const batchOptions = ref<string[]>(['第一批', '第二批', '第三批', '第四批'])
const typeOptions = ref<string[]>(['实施→运维', '区域/组间'])
const fromGroupOptions = ref<string[]>([...HANDOVER_GROUP_OPTIONS])
const toOwnerOptions = ref<string[]>([...HANDOVER_OWNER_OPTIONS])
const workflowLoadingKey = ref('')
const access = useAccessControl()
const canManageHandover = computed(() => access.canPermission('handover.manage'))
const workflowBusy = computed(() => workflowLoadingKey.value.length > 0)
const { runInitialLoad } = useResilientLoad()
const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData(), loadKanban()])
  },
  scope: 'handover',
  intervalMs: 60000,
})

const stageTag = (stage: string) => {
  if (stage === '已交接') return 'success'
  if (stage === '交接中') return 'warning'
  if (stage === '已发邮件') return 'info'
  return 'danger'
}

const formatDate = (value: string) => value.slice(0, 10)

const formatDateTime = (value?: string | null) => {
  if (!value) {
    return '-'
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleString('zh-CN', { dateStyle: 'short', timeStyle: 'short' })
}

const resolvePrimaryAction = (item: HandoverItem): {
  key: 'send-email' | 'start' | 'complete'
  label: string
  type: 'primary' | 'success'
} | null => {
  if (item.stage === '未发') {
    return { key: 'send-email', label: '发交接邮件', type: 'primary' as const }
  }

  if (item.stage === '已发邮件') {
    return { key: 'start', label: '开始交接', type: 'primary' as const }
  }

  if (item.stage === '交接中') {
    return { key: 'complete', label: '完成交接', type: 'success' as const }
  }

  return null
}

const canRollbackHandover = (item: HandoverItem) => canManageHandover.value && item.stage !== '未发'

const rollbackActionLabel = (item: HandoverItem) => item.stage === '已交接' ? '重开交接' : '回退一步'

const responsibilityNode = (item: HandoverItem) => {
  if (item.stage === '未发') {
    return `${item.fromGroup || '原组别'}待发起交接`
  }

  if (item.stage === '已发邮件') {
    return `等待 ${item.toOwner || '对接人'} 承接`
  }

  if (item.stage === '交接中') {
    return `${item.toOwner || '对接人'} 推进交接`
  }

  return '已完成交接归档'
}

type HandoverWorkflowAction = 'send-email' | 'start' | 'complete' | 'rollback'

const buildWorkflowKey = (id: number, action: HandoverWorkflowAction) => `${id}:${action}`

const isWorkflowLoading = (id: number, action: HandoverWorkflowAction) => workflowLoadingKey.value === buildWorkflowKey(id, action)

const goToProjectPage = (item: HandoverItem) => {
  void router.push({
    path: '/project/list',
    query: {
      hospitalName: item.hospitalName,
      productName: item.productName,
      groupName: item.fromGroup,
      action: 'edit',
    },
  })
}

const goToInspectionPage = (item: HandoverItem) => {
  void router.push({
    path: '/inspection/plan',
    query: {
      groupName: item.fromGroup,
      hospitalName: item.hospitalName,
      productName: item.productName,
      action: 'detail',
    },
  })
}

const onOpenDetail = (item: HandoverItem, syncRoute = true) => {
  detailItem.value = item
  detailVisible.value = true
  if (syncRoute) {
    void updateRouteQuery({ action: 'detail', id: String(item.id) })
  }
}

const onRowDoubleClick = (row: HandoverItem) => {
  onOpenDetail(row)
}

const syncDetailFromRoute = async () => {
  const action = readRouteQueryValue(route.query.action)
  if (action !== 'detail') {
    return
  }

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) {
    return
  }

  const matched = tableData.value.find((item) => item.id === id)
    ?? kanbanColumns.value.flatMap((column) => column.items).find((item) => item.id === id)
  if (matched) {
    onOpenDetail(matched, false)
    return
  }

  try {
    const res = await fetchHandoverById(id)
    onOpenDetail(res.data, false)
  } catch {
  }
}

const syncSingleDetailFromFilters = () => {
  const action = readRouteQueryValue(route.query.action)
  const id = Number(readRouteQueryValue(route.query.id))
  if (action !== 'detail' || (Number.isFinite(id) && id > 0) || detailVisible.value) {
    return
  }

  const filtered = buildFilteredRows(allRows.value)
    .filter((item) => !query.stage || item.stage === query.stage)
    .filter((item) => !query.batch || item.batch === query.batch)
    .filter((item) => !query.type || item.type === query.type)
    .filter((item) => !query.fromGroup || item.fromGroup === query.fromGroup)
    .filter((item) => !query.toOwner || item.toOwner === query.toOwner)

  const matched = filtered[0]
  if (filtered.length === 1 && matched) {
    onOpenDetail(matched, false)
  }
}

const loadSummary = async () => {
  try {
    const res = await fetchHandoverSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载交接汇总失败，请稍后重试'))
  }
}

const loadKanban = async () => {
  try {
    const res = await fetchHandoverKanban()
    kanbanColumns.value = res.data
  } catch (error) {
    kanbanColumns.value = []
    ElMessage.error(getErrorMessage(error, '加载交接看板失败，请稍后重试'))
  }
}

const loadFilterOptions = async () => {
  try {
    const res = await fetchHandovers({ page: 1, size: 100000 })
    const items = res.data.items
    allRows.value = items

    if (!items.length) {
      return
    }

    const stages = Array.from(new Set(items.map((item) => item.stage).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (stages.length > 0) {
      stageOptions.value = Array.from(new Set([...stageOptions.value, ...stages]))
    }

    const batches = Array.from(new Set(items.map((item) => item.batch).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (batches.length > 0) {
      batchOptions.value = Array.from(new Set([...batchOptions.value, ...batches]))
    }

    const types = Array.from(new Set(items.map((item) => item.type).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (types.length > 0) {
      typeOptions.value = Array.from(new Set([...typeOptions.value, ...types]))
    }

    const fromGroups = Array.from(new Set(items.map((item) => item.fromGroup).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (fromGroups.length > 0) {
      fromGroupOptions.value = fromGroups
    }

    const toOwners = Array.from(new Set(items.map((item) => item.toOwner).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (toOwners.length > 0) {
      toOwnerOptions.value = toOwners
    }
  } catch {
  }
}

const loadData = async () => {
  loading.value = true
  try {
    if (getRouteHospitalName() || getRouteProductName()) {
      const source = allRows.value.length > 0 ? allRows.value : (await fetchHandovers({ page: 1, size: 100000 })).data.items
      if (allRows.value.length === 0) {
        allRows.value = source
      }

      const filtered = buildFilteredRows(source)
        .filter((item) => !query.stage || item.stage === query.stage)
        .filter((item) => !query.batch || item.batch === query.batch)
        .filter((item) => !query.type || item.type === query.type)
        .filter((item) => !query.fromGroup || item.fromGroup === query.fromGroup)
        .filter((item) => !query.toOwner || item.toOwner === query.toOwner)

      total.value = filtered.length
      const start = (query.page - 1) * query.size
      tableData.value = filtered.slice(start, start + query.size)
      return
    }

    const res = await fetchHandovers(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载交接列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onStatClick = (stage: string) => {
  query.batch = ''
  query.type = ''
  query.fromGroup = ''
  query.toOwner = ''
  query.stage = stage
  query.page = 1
  void updateRouteQuery({
    stage: undefined,
    fromGroup: undefined,
    toOwner: undefined,
    batch: undefined,
    type: undefined,
    hospitalName: undefined,
    productName: undefined,
    action: undefined,
    id: undefined,
  })
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
  query.stage = ''
  query.batch = ''
  query.type = ''
  query.fromGroup = ''
  query.toOwner = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  void updateRouteQuery({
    stage: undefined,
    fromGroup: undefined,
    toOwner: undefined,
    batch: undefined,
    type: undefined,
    hospitalName: undefined,
    productName: undefined,
    action: undefined,
    id: undefined,
  })
  loadData()
}

const onExport = async () => {
  exporting.value = true
  try {
    const blob = await exportHandovers(query)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `交接管理-${Date.now()}.csv`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  } finally {
    exporting.value = false
  }
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<HandoverFilterState>({
  key: 'handover-list',
  getState: () => ({
    stage: query.stage,
    batch: query.batch,
    type: query.type,
    fromGroup: query.fromGroup,
    toOwner: query.toOwner,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.stage = state.stage ?? ''
    query.batch = state.batch ?? ''
    query.type = state.type ?? ''
    query.fromGroup = state.fromGroup ?? ''
    query.toOwner = state.toOwner ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

const refreshAfterWorkflow = async (updatedItem: HandoverItem) => {
  await Promise.all([loadSummary(), loadFilterOptions(), loadData(), loadKanban()])
  notifyDataChanged('handover')

  const matched = tableData.value.find((item) => item.id === updatedItem.id)
    ?? kanbanColumns.value.flatMap((column) => column.items).find((item) => item.id === updatedItem.id)

  detailItem.value = matched ?? updatedItem
}

const runWorkflowAction = async (
  item: HandoverItem,
  action: HandoverWorkflowAction,
  confirmText: string,
  successText: string,
  fallbackText: string,
) => {
  if (!canManageHandover.value) {
    ElMessage.warning('当前账号无交接推进权限')
    return
  }

  try {
    await ElMessageBox.confirm(confirmText, '请确认操作', {
      confirmButtonText: '确认',
      cancelButtonText: '取消',
      type: 'warning',
    })
  } catch {
    return
  }

  workflowLoadingKey.value = buildWorkflowKey(item.id, action)
  try {
    let updated: HandoverItem
    if (action === 'send-email') {
      const res = await sendHandoverEmail(item.id)
      updated = res.data
    } else if (action === 'start') {
      const res = await startHandover(item.id)
      updated = res.data
    } else if (action === 'complete') {
      const res = await completeHandover(item.id)
      updated = res.data
    } else {
      const res = await rollbackHandover(item.id)
      updated = res.data
    }

    ElMessage.success(successText)
    await refreshAfterWorkflow(updated)
  } catch (error) {
    ElMessage.error(getErrorMessage(error, fallbackText))
  } finally {
    workflowLoadingKey.value = ''
  }
}

const onPrimaryWorkflowAction = async (item: HandoverItem) => {
  const action = resolvePrimaryAction(item)
  if (!action) {
    return
  }

  const confirmText = action.key === 'send-email'
    ? `确认向「${item.toOwner || '对接人'}」发送「${item.hospitalName}」的交接邮件吗？`
    : action.key === 'start'
      ? `确认将「${item.hospitalName}」正式推进为“交接中”吗？`
      : `确认将「${item.hospitalName}」标记为“已交接”吗？`
  const successText = action.key === 'send-email'
    ? '交接邮件已发送'
    : action.key === 'start'
      ? '交接已开始'
      : '交接已完成'
  const fallbackText = action.key === 'send-email'
    ? '发送交接邮件失败，请稍后重试'
    : action.key === 'start'
      ? '开始交接失败，请稍后重试'
      : '完成交接失败，请稍后重试'

  await runWorkflowAction(item, action.key, confirmText, successText, fallbackText)
}

const onRollbackWorkflow = async (item: HandoverItem) => {
  if (!canRollbackHandover(item)) {
    return
  }

  await runWorkflowAction(
    item,
    'rollback',
    item.stage === '已交接'
      ? `确认重开「${item.hospitalName}」的交接任务吗？`
      : `确认将「${item.hospitalName}」从「${item.stage}」回退一步吗？`,
    item.stage === '已交接' ? '交接已重开' : '交接阶段已回退',
    item.stage === '已交接' ? '重开交接失败，请稍后重试' : '回退交接阶段失败，请稍后重试',
  )
}

watch(detailVisible, (visible) => {
  if (!visible) {
    void clearRouteActionQuery()
  }
})

watch(() => route.fullPath, async () => {
  applyDrillQuery()
  await Promise.allSettled([loadData(), loadKanban()])
  await syncDetailFromRoute()
  syncSingleDetailFromFilters()
})

const handoverFilterLabel = computed(() => {
  if (query.stage) return `阶段：${query.stage}`
  if (query.batch) return `批次：${query.batch}`
  if (query.fromGroup) return `来源：${query.fromGroup}`
  return '全部交接记录'
})

const handoverHeroSignals = computed(() => [
  { label: '未发邮件', value: summary.value.pendingCount, note: '待推进' },
  { label: '进行中', value: summary.value.inProgressCount, note: '交接中' },
  { label: '已发邮件', value: summary.value.emailSentCount, note: '等待承接' },
  { label: '已完成', value: summary.value.completedCount, note: `共 ${summary.value.total} 条` },
])

const handoverQuickActions = computed(() => [
  { title: '未发邮件', note: `${summary.value.pendingCount} 项待推进`, onClick: () => onStatClick('未发') },
  { title: '已发邮件', note: `${summary.value.emailSentCount} 项等待承接`, onClick: () => onStatClick('已发邮件') },
  { title: '交接中', note: `${summary.value.inProgressCount} 项进行中`, onClick: () => onStatClick('交接中') },
  { title: '全部重置', note: '恢复全量视图', onClick: () => onStatClick('') },
])

onMounted(async () => {
  await access.ensureAccessProfileLoaded()
  restoreFilterState()
  applyDrillQuery()
  await runInitialLoad({
    tasks: [loadSummary, loadFilterOptions, loadData, loadKanban],
    retryChecks: [
      {
        when: () => summary.value.total > 0 && total.value === 0,
        task: loadData,
      },
      {
        when: () => summary.value.total > 0 && kanbanColumns.value.length === 0,
        task: loadKanban,
      },
    ],
  })
  await syncDetailFromRoute()
  syncSingleDetailFromFilters()
})
</script>

<style scoped>
.detail-actions {
  margin-top: 16px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.kanban-title {
  font-weight: 600;
}

.kanban-col {
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  min-height: 280px;
  background: var(--el-fill-color-lighter);
  transition: box-shadow 0.2s ease;
}

.kanban-col:hover {
  box-shadow: 0 2px 8px rgba(47, 58, 150, 0.08);
}

.kanban-col-header {
  padding: 10px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  font-weight: 600;
  background: var(--el-bg-color);
}

.kanban-items {
  padding: 10px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.kanban-item {
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  padding: 8px;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.kanban-item:hover {
  transform: translateY(-1px);
  box-shadow: 0 2px 6px rgba(47, 58, 150, 0.08);
}

.kanban-item-name {
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.kanban-item-meta {
  margin-top: 2px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.kanban-empty {
  color: var(--el-text-color-placeholder);
  text-align: center;
  padding: 24px 0;
}

.detail-actions {
  flex-wrap: nowrap;
  overflow-x: auto;
}

:deep(.table-action-group) {
  flex-wrap: nowrap;
  white-space: nowrap;
}

.clickable {
  cursor: pointer;
}

/* ===== Handover Hero ===== */
.handover-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.18fr) minmax(320px, 0.82fr);
  gap: 16px;
  padding: 24px;
  border-radius: 28px;
  color: #ffffff;
  background:
    radial-gradient(ellipse 80% 60% at 18% 30%, rgba(91, 112, 200, 0.22) 0%, transparent 70%),
    linear-gradient(135deg, #1d2e52 0%, #2f4585 48%, #2b5f8a 100%);
  box-shadow: 0 26px 44px rgba(29, 46, 82, 0.36), inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

.handover-hero-kicker-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 10px;
}

.handover-hero-kicker {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  opacity: 0.65;
}

.handover-hero-badge {
  padding: 2px 10px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.14);
  font-size: 11px;
  font-weight: 600;
  opacity: 0.9;
}

.handover-hero-title {
  margin: 0 0 6px;
  font-size: 28px;
  font-weight: 800;
  line-height: 1.2;
  letter-spacing: -0.5px;
}

.handover-hero-subtitle {
  font-size: 13px;
  opacity: 0.7;
  margin-bottom: 18px;
}

.handover-hero-signals {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
}

.handover-signal-card {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 10px 12px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(4px);
  border: 1px solid rgba(255, 255, 255, 0.12);
  transition: background 0.2s;
}

.handover-signal-card:hover {
  background: rgba(255, 255, 255, 0.16);
}

.handover-signal-label {
  font-size: 11px;
  font-weight: 600;
  opacity: 0.7;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.handover-signal-value {
  font-size: 22px;
  font-weight: 800;
  line-height: 1.15;
}

.handover-signal-note {
  font-size: 11px;
  opacity: 0.6;
}

.handover-hero-side {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.handover-control-card {
  padding: 16px 18px;
  border-radius: 20px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(255, 255, 255, 0.14);
}

.handover-control-title {
  font-size: 15px;
  font-weight: 700;
  margin-bottom: 4px;
}

.handover-control-note {
  font-size: 12px;
  opacity: 0.65;
  margin-bottom: 12px;
}

.handover-control-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.handover-control-actions :deep(.el-button) {
  background: rgba(255, 255, 255, 0.14);
  border-color: rgba(255, 255, 255, 0.25);
  color: #fff;
}

.handover-control-actions :deep(.el-button:hover) {
  background: rgba(255, 255, 255, 0.24);
}

.handover-quick-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  flex: 1;
}

.handover-quick-action {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 10px 12px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.14);
  cursor: pointer;
  text-align: left;
  color: #fff;
  transition: background 0.2s, transform 0.15s;
}

.handover-quick-action:hover {
  background: rgba(255, 255, 255, 0.18);
  transform: translateY(-1px);
}

.handover-quick-title {
  font-size: 13px;
  font-weight: 700;
}

.handover-quick-note {
  font-size: 11px;
  opacity: 0.65;
}

@media (max-width: 1280px) {
  .handover-hero {
    grid-template-columns: 1fr;
  }

  .handover-hero-signals {
    grid-template-columns: repeat(4, 1fr);
  }
}

@media (max-width: 768px) {
  .handover-hero {
    padding: 16px;
  }

  .handover-hero-signals {
    grid-template-columns: repeat(2, 1fr);
  }

  .handover-quick-grid {
    grid-template-columns: repeat(4, 1fr);
  }
}
</style>
