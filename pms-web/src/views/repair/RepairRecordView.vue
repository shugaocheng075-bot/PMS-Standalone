<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">报修记录</h2>
        <div class="page-subtitle">项目报修登记、处理进度跟踪与历史记录查询</div>
      </div>
    </div>

    <SummaryMetrics :items="summaryCards" :columns="4" @select="onSummaryCardSelect" />

    <ProTable
      title="报修明细"
      :data="tableData"
      :loading="loading"
      :total="total"
      v-model:page="query.page"
      v-model:size="query.size"
      @refresh="loadData"
      @pagination-change="loadData"
      stripe
      empty-text="暂无符合条件的数据"
      @row-dblclick="onRowDoubleClick"
    >
      <template #search>
        <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
          <el-form-item label="医院名称">
            <el-select v-model="query.hospitalName" clearable filterable placeholder="全部" style="width: 220px">
              <el-option v-for="name in accessibleHospitals" :key="name" :label="name" :value="name" />
            </el-select>
          </el-form-item>
          <el-form-item label="报修人"><el-input v-model="query.reporterName" placeholder="输入姓名按回车搜索" clearable @keyup.enter="onSearch" /></el-form-item>
          <el-form-item label="状态">
            <el-select v-model="query.status" clearable style="width: 140px" placeholder="全部">
              <el-option label="待处理" value="待处理" />
              <el-option label="处理中" value="处理中" />
              <el-option label="已完成" value="已完成" />
              <el-option label="已关闭" value="已关闭" />
            </el-select>
          </el-form-item>
          <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
        </el-form-item>
        </el-form>
      </template>

      <template #toolbar>
        <el-button v-if="canCreate" type="primary" @click="onOpenCreate" icon="Plus">新增报修</el-button>
        <el-button :loading="exporting" @click="onExport" icon="Download">导出记录</el-button>
      </template>

      <!-- Table Columns -->
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="hospitalName" label="医院名称" min-width="180" show-overflow-tooltip />
      <el-table-column prop="productName" label="产品名称" min-width="140" show-overflow-tooltip />
      <el-table-column prop="projectName" label="项目名称" min-width="180" show-overflow-tooltip />
      <el-table-column prop="reporterName" label="报修人" width="100" />    
      <el-table-column prop="severity" label="严重程度" width="100" show-overflow-tooltip />
      <el-table-column prop="urgency" label="紧迫程度" width="110">
        <template #default="scope">
          <el-tag :type="urgencyTag(scope.row.urgency)">{{ scope.row.urgency }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="status" label="状态" width="100">
        <template #default="scope">
          <el-tag :type="statusTag(scope.row.status)">{{ scope.row.status }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="处理SLA" width="150">
        <template #default="scope">
          <el-tag :type="slaTagType(scope.row)">{{ formatSlaText(scope.row) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="description" label="问题描述" min-width="220" show-overflow-tooltip />
      <el-table-column prop="reportedAt" label="报修日期" width="170">    
        <template #default="scope">{{ formatTime(scope.row.reportedAt || '') }}</template>
      </el-table-column>
      <el-table-column prop="actualWorkHours" label="实际工时" width="100" />
      <el-table-column label="操作" width="380" fixed="right">
        <template #default="scope">
          <div class="table-action-group">
            <el-button
              type="primary"
              link
              :loading="detailLoadingId === scope.row.id"
              :disabled="isRowBusy(scope.row.id)"
              @click="onOpenDetail(scope.row.id)"
             icon="Document">详情</el-button>
            <el-button
              v-if="canOperateWorkflow && scope.row.status === '待处理'"
              type="success"
              link
              :loading="isActionLoading('accept', scope.row.id)"
              :disabled="isRowBusy(scope.row.id)"
              @click="onAccept(scope.row)"
             icon="Select">签收</el-button>
            <el-button
              v-if="canOperateWorkflow && scope.row.status === '处理中'"
              type="success"
              link
              :loading="isActionLoading('resolve', scope.row.id)"
              :disabled="isRowBusy(scope.row.id)"
              @click="onResolve(scope.row)"
             icon="CircleCheck">完成</el-button>
            <el-button
              v-if="canOperateWorkflow && (scope.row.status === '已完成' || scope.row.status === '已关闭')"
              type="warning"
              link
              :loading="isActionLoading('reopen', scope.row.id)"
              :disabled="isRowBusy(scope.row.id)"
              @click="onReopen(scope.row)"
             icon="RefreshLeft">重开</el-button>
            <el-button
              v-if="canEditOrDelete"
              type="primary"
              link
              :disabled="isRowBusy(scope.row.id)"
              @click="onOpenEdit(scope.row)"
             icon="Edit">编辑</el-button>
            <el-button
              v-if="canEditOrDelete"
              type="danger"
              link
              :loading="deletingId === scope.row.id"
              :disabled="isRowBusy(scope.row.id)"
              @click="onDelete(scope.row)"
             icon="Delete">删除</el-button>
          </div>
        </template>
      </el-table-column>
    </ProTable>

    <ProDrawer v-model="dialogVisible" :title="editingId ? '编辑报修' : '新增报修'" width="620px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="90px">
        <el-form-item label="医院名称" required>
          <el-select v-model="form.hospitalName" filterable placeholder="请选择医院" style="width: 100%">
            <el-option v-for="name in accessibleHospitals" :key="name" :label="name" :value="name" />
          </el-select>
        </el-form-item>
        <el-form-item label="产品名称"><el-input v-model="form.productName" /></el-form-item>
        <el-form-item label="项目名称"><el-input v-model="form.projectName" /></el-form-item>
        <el-form-item label="报修人"><el-input v-model="form.reporterName" /></el-form-item>
        <el-form-item label="严重程度"><el-input v-model="form.severity" /></el-form-item>
        <el-form-item label="紧急程度">
          <el-select v-model="form.urgency" style="width: 100%">
            <el-option label="普通" value="普通" />
            <el-option label="紧急" value="紧急" />
            <el-option label="非常紧急" value="非常紧急" />
          </el-select>
        </el-form-item>
        <el-form-item label="问题描述"><el-input v-model="form.description" type="textarea" :rows="3" /></el-form-item>
        <el-form-item label="处理结果"><el-input v-model="form.resolution" type="textarea" :rows="3" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading" @click="dialogVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="submitLoading" :disabled="submitLoading" @click="onSubmit">确定</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="detailVisible" title="报修详情" width="720px">  
      <el-descriptions v-if="detailItem" :column="2" border>
        <el-descriptions-item label="医院名称">{{ detailItem.hospitalName }}</el-descriptions-item>
        <el-descriptions-item label="报修人">{{ detailItem.reporterName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="产品名称">{{ detailItem.productName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="项目名称">{{ detailItem.projectName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="严重程度">{{ detailItem.severity || '-' }}</el-descriptions-item>
        <el-descriptions-item label="紧迫程度">{{ detailItem.urgency || '-' }}</el-descriptions-item>
        <el-descriptions-item label="处理人">{{ detailItem.assigneeName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="受理时间">{{ formatTime(detailItem.acceptedAt || '') }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ detailItem.status || '-' }}</el-descriptions-item>
        <el-descriptions-item label="报修日期">{{ formatTime(detailItem.reportedAt || '') }}</el-descriptions-item>
        <el-descriptions-item label="SLA 截止">{{ formatTime(detailItem.slaDueAt || '') }}</el-descriptions-item>
        <el-descriptions-item label="完成时间">{{ formatTime(detailItem.completedAt || '') }}</el-descriptions-item>
        <el-descriptions-item label="实际工时">{{ detailItem.actualWorkHours ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="处理结果">{{ detailItem.resolution || '-' }}</el-descriptions-item>
        <el-descriptions-item label="描述" :span="2">{{ detailItem.description || '-' }}</el-descriptions-item>
      </el-descriptions>
      <div v-if="detailTimeline.length > 0" class="repair-detail-timeline">
        <div class="repair-detail-timeline__title">处理节点</div>
        <el-timeline>
          <el-timeline-item
            v-for="item in detailTimeline"
            :key="item.key"
            :timestamp="item.time"
            placement="top"
          >
            {{ item.title }}
          </el-timeline-item>
        </el-timeline>
      </div>
      <template #footer><el-button @click="detailVisible = false">关闭</el-button></template>
    </ProDrawer>
  </div>
</template>
<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import {
  acceptRepairRecord,
  fetchRepairRecords,
  fetchRepairSummary,
  fetchRepairRecordById,
  createRepairRecord,
  updateRepairRecord,
  deleteRepairRecord,
  exportRepairRecords,
  reopenRepairRecord,
  resolveRepairRecord,
} from '../../api/modules/repair'
import { fetchDataScope } from '../../api/modules/access'
import { useAccessControl } from '../../composables/useAccessControl'
import type { RepairRecordItem, RepairRecordSummary, RepairRecordUpsert } from '../../types/repair'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { resolveRepairStatusTag } from '../../utils/statusTag'
import SummaryMetrics from '../../components/SummaryMetrics.vue'


const access = useAccessControl()
const canCreate = computed(() => {
  if (!access.canPermission('repair.manage')) {
    return false
  }

  return access.isManager() || access.isOperator()
})
const canOperateWorkflow = computed(() => access.canPermission('repair.manage'))
const canEditOrDelete = computed(() => access.isManager() && access.canPermission('repair.manage'))

const loading = ref(false)
const exporting = ref(false)
const submitLoading = ref(false)
const actionLoadingKey = ref('')
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const deletingId = ref<number | null>(null)
const detailVisible = ref(false)
const detailLoadingId = ref<number | null>(null)
const total = ref(0)
const tableData = ref<RepairRecordItem[]>([])
const accessibleHospitals = ref<string[]>([])
const detailItem = ref<RepairRecordItem | null>(null)
const summary = ref<RepairRecordSummary>({ total: 0, pendingCount: 0, inProgressCount: 0, completedCount: 0 })
const route = useRoute()
const router = useRouter()
const nowTick = ref(Date.now())
let slaTimer: number | null = null

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

const query = reactive({
  hospitalName: '',
  reporterName: '',
  status: '',
  page: 1,
  size: 15,
})

type RepairSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active: boolean
}

const summaryCards = computed<RepairSummaryCard[]>(() => [
  {
    key: 'all',
    title: '全部',
    value: summary.value.total,
    context: '状态总览',
    note: '查看全部报修记录与处理进度',
    color: '#3f4f63',
    active: query.status === '',
  },
  {
    key: '待处理',
    title: '待处理',
    value: summary.value.pendingCount,
    context: '优先处理',
    note: '需要立即分派或开始处理的报修',
    color: '#c58a87',
    active: query.status === '待处理',
  },
  {
    key: '处理中',
    title: '处理中',
    value: summary.value.inProgressCount,
    context: '进度跟踪',
    note: '查看当前正在推进中的工单',
    color: '#c7a06c',
    active: query.status === '处理中',
  },
  {
    key: '已完成',
    title: '已完成',
    value: summary.value.completedCount,
    context: '闭环情况',
    note: '核查已完成报修的处理闭环',
    color: '#7d9f92',
    active: query.status === '已完成',
  },
])

const form = reactive<RepairRecordUpsert>({
  projectId: 0,
  hospitalName: '',
  productName: '',
  projectName: '',
  productCategory: '',
  issueCategory: '',
  reporterName: '',
  severity: '',
  functionModule: '',
  description: '',
  reportedAt: '',
  actualWorkHours: undefined,
  content: '',
  resolution: '',
  attachmentImages: '',
  registrationStatus: '',
  status: '待处理',
  urgency: '普通',
})
const formRef = ref<FormInstance>()
const { runInitialLoad } = useResilientLoad()

type RepairFilterState = {
  hospitalName: string
  reporterName: string
  status: string
  page: number
  size: number
}

const filterPersist = useFilterStatePersist<RepairFilterState>({
  key: 'repair-records',
  getState: () => ({ ...query }),
  applyState: (state) => {
    query.hospitalName = typeof state.hospitalName === 'string' ? state.hospitalName : ''
    query.reporterName = typeof state.reporterName === 'string' ? state.reporterName : ''
    query.status = typeof state.status === 'string' ? state.status : ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await Promise.allSettled([loadSummary(), loadData()])
  },
  scope: 'repair',
  intervalMs: 60000,
})

const formRules: FormRules<RepairRecordUpsert> = {
  hospitalName: [{ required: true, message: '请选择医院名称', trigger: 'change' }],
  description: [{ required: true, message: '请输入问题描述', trigger: 'blur' }],
}

const applyRouteFilters = () => {
  const status = readRouteQueryValue(route.query.status)
  const hospitalName = readRouteQueryValue(route.query.hospitalName)
  const reporterName = readRouteQueryValue(route.query.reporterName)
  let applied = false

  if (status) {
    query.status = status
    applied = true
  }

  if (hospitalName) {
    query.hospitalName = hospitalName
    applied = true
  }

  if (reporterName) {
    query.reporterName = reporterName
    applied = true
  }

  if (applied) {
    query.page = 1
  }
}

const statusTag = (status: string) => resolveRepairStatusTag(status)

const isActionLoading = (action: string, id: number) => actionLoadingKey.value === `${action}-${id}`

const isRowBusy = (id: number) => {
  return submitLoading.value || deletingId.value === id || detailLoadingId.value === id || actionLoadingKey.value.endsWith(`-${id}`)
}

const urgencyTag = (urgency: string) => {
  if (urgency === '非常紧急') return 'danger'
  if (urgency === '紧急') return 'warning'
  return 'info'
}

const formatDuration = (milliseconds: number) => {
  const totalMinutes = Math.max(1, Math.floor(milliseconds / 60000))
  const days = Math.floor(totalMinutes / (60 * 24))
  const hours = Math.floor((totalMinutes % (60 * 24)) / 60)
  const minutes = totalMinutes % 60

  if (days > 0) {
    return `${days}天${hours}小时`
  }

  if (hours > 0) {
    return `${hours}小时${minutes}分`
  }

  return `${minutes}分`
}

const resolveSlaMeta = (row: RepairRecordItem) => {
  if (!row.slaDueAt) {
    return { label: '未设置', type: 'info' as const }
  }

  if (row.status === '已完成' || row.status === '已关闭') {
    return { label: '已闭环', type: 'success' as const }
  }

  const dueTime = new Date(row.slaDueAt).getTime()
  if (Number.isNaN(dueTime)) {
    return { label: '未设置', type: 'info' as const }
  }

  const diff = dueTime - nowTick.value
  if (diff <= 0) {
    return { label: `超时 ${formatDuration(Math.abs(diff))}`, type: 'danger' as const }
  }

  if (diff <= 2 * 60 * 60 * 1000) {
    return { label: `剩余 ${formatDuration(diff)}`, type: 'warning' as const }
  }

  return { label: `剩余 ${formatDuration(diff)}`, type: 'success' as const }
}

const formatSlaText = (row: RepairRecordItem) => resolveSlaMeta(row).label

const slaTagType = (row: RepairRecordItem) => resolveSlaMeta(row).type

const formatTime = (iso: string) => {
  if (!iso) return '-'
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString('zh-CN', { dateStyle: 'short', timeStyle: 'short' })
}

const loadSummary = async () => {
  try {
    const res = await fetchRepairSummary()
    summary.value = res.data
  } catch (error) {
    summary.value = { total: 0, pendingCount: 0, inProgressCount: 0, completedCount: 0 }
    ElMessage.error(getErrorMessage(error, '加载报修汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchRepairRecords({
      hospitalName: query.hospitalName || undefined,
      reporterName: query.reporterName || undefined,
      status: query.status || undefined,
      page: query.page,
      size: query.size,
    })
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载报修记录失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const loadScope = async () => {
  try {
    const res = await fetchDataScope()
    accessibleHospitals.value = [...(res.data.accessibleHospitalNames || [])].sort((a, b) =>
      a.localeCompare(b, 'zh-CN'),
    )
  } catch (error) {
    accessibleHospitals.value = []
    ElMessage.error(getErrorMessage(error, '加载可访问医院范围失败，请稍后重试'))
  }
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.hospitalName = ''
  query.reporterName = ''
  query.status = ''
  query.page = 1
  query.size = 15
  filterPersist.clear()
  void updateRouteQuery({ status: undefined, hospitalName: undefined, reporterName: undefined, action: undefined, id: undefined })
  loadData()
}

const onExport = async () => {
  exporting.value = true
  try {
    const blob = await exportRepairRecords(query)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `报修记录-${Date.now()}.csv`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  } finally {
    exporting.value = false
  }
}

const onStatClick = (status: string) => {
  query.hospitalName = ''
  query.reporterName = ''
  query.status = status
  query.page = 1
  void updateRouteQuery({ status: undefined, hospitalName: undefined, reporterName: undefined, action: undefined, id: undefined })
  loadData()
}

const onSummaryCardSelect = (card: { key?: string | number }) => {
  if (typeof card.key !== 'string') {
    return
  }

  onStatClick(card.key === 'all' ? '' : card.key)
}

const detailTimeline = computed(() => {
  if (!detailItem.value) {
    return [] as Array<{ key: string; title: string; time: string }>
  }

  const items = [
    detailItem.value.createdAt ? { key: 'created', title: '工单创建', time: formatTime(detailItem.value.createdAt) } : null,
    detailItem.value.acceptedAt ? { key: 'accepted', title: `工单签收${detailItem.value.assigneeName ? ` · ${detailItem.value.assigneeName}` : ''}`, time: formatTime(detailItem.value.acceptedAt) } : null,
    detailItem.value.completedAt ? { key: 'completed', title: '工单完成', time: formatTime(detailItem.value.completedAt) } : null,
  ]

  return items.filter((item): item is { key: string; title: string; time: string } => Boolean(item))
})



const resetForm = () => {
  Object.assign(form, {
    projectId: 0,
    hospitalName: '',
    productName: '',
    projectName: '',
    productCategory: '',
    issueCategory: '',
    reporterName: '',
    severity: '',
    functionModule: '',
    description: '',
    reportedAt: '',
    actualWorkHours: undefined,
    content: '',
    resolution: '',
    attachmentImages: '',
    registrationStatus: '',
    status: '待处理',
    urgency: '普通',
  })
}

const onOpenCreate = () => {
  if (!canCreate.value) {
    ElMessage.warning('当前账号无报修新增权限')
    return
  }

  editingId.value = null
  resetForm()
  dialogVisible.value = true
  void updateRouteQuery({ action: 'create', id: undefined })
}

const onOpenEdit = (row: RepairRecordItem) => {
  if (!canEditOrDelete.value) {
    ElMessage.warning('当前账号无报修编辑权限')
    return
  }

  editingId.value = row.id
  Object.assign(form, {
    projectId: row.projectId,
    hospitalName: row.hospitalName,
    productName: row.productName,
    projectName: row.projectName,
    productCategory: row.productCategory,
    issueCategory: row.issueCategory,
    reporterName: row.reporterName,
    severity: row.severity,
    functionModule: row.functionModule,
    description: row.description,
    reportedAt: row.reportedAt || '',
    actualWorkHours: row.actualWorkHours,
    content: row.content,
    resolution: row.resolution,
    attachmentImages: row.attachmentImages,
    registrationStatus: row.registrationStatus,
    status: row.status,
    urgency: row.urgency,
  })
  dialogVisible.value = true
  void updateRouteQuery({ action: 'edit', id: String(row.id) })
}

const onRowDoubleClick = (row: RepairRecordItem) => {
  if (canEditOrDelete.value) {
    onOpenEdit(row)
    return
  }

  void onOpenDetail(row.id)
}

const onOpenDetail = async (id: number) => {
  detailLoadingId.value = id
  try {
    const res = await fetchRepairRecordById(id)
    detailItem.value = res.data
    detailVisible.value = true
    void updateRouteQuery({ action: 'detail', id: String(id) })
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载报修详情失败，请稍后重试'))
  } finally {
    detailLoadingId.value = null
  }
}

const refreshDetailIfOpen = async (id: number) => {
  if (!detailVisible.value || detailItem.value?.id !== id) {
    return
  }

  try {
    const res = await fetchRepairRecordById(id)
    detailItem.value = res.data
  } catch {
  }
}

const runWorkflowAction = async (action: 'accept' | 'resolve' | 'reopen', id: number, runner: () => Promise<void>) => {
  actionLoadingKey.value = `${action}-${id}`
  try {
    await runner()
    notifyDataChanged('repair')
    await Promise.all([loadData(), loadSummary(), refreshDetailIfOpen(id)])
  } finally {
    actionLoadingKey.value = ''
  }
}

const onAccept = async (row: RepairRecordItem) => {
  const assigneeName = access.accessProfile.value?.personnelName || row.assigneeName || row.reporterName
  try {
    await runWorkflowAction('accept', row.id, async () => {
      await acceptRepairRecord(row.id, { assigneeName })
      ElMessage.success('已签收，工单进入处理中')
    })
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '签收失败，请稍后重试'))
  }
}

const onResolve = async (row: RepairRecordItem) => {
  let resolution = ''
  try {
    const result = await ElMessageBox.prompt('请填写处理结果，系统会记录为工单闭环内容。', '完成报修', {
      confirmButtonText: '完成工单',
      cancelButtonText: '取消',
      inputPlaceholder: '例如：已重启服务并清理缓存，现场验证恢复正常',
      inputType: 'textarea',
      inputValue: row.resolution || '',
      inputValidator: (value) => (value.trim() ? true : '请填写处理结果'),
    })
    resolution = result.value.trim()
  } catch {
    return
  }

  try {
    await runWorkflowAction('resolve', row.id, async () => {
      await resolveRepairRecord(row.id, { resolution })
      ElMessage.success('工单已完成闭环')
    })
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '完成报修失败，请稍后重试'))
  }
}

const onReopen = async (row: RepairRecordItem) => {
  let reason = ''
  try {
    const result = await ElMessageBox.prompt('可填写重开原因，系统会写入审计日志并重新进入待处理。', '重开报修', {
      confirmButtonText: '重开工单',
      cancelButtonText: '取消',
      inputPlaceholder: '例如：现场复测仍有复现，需要继续跟进',
      inputType: 'textarea',
      inputValue: '',
    })
    reason = result.value.trim()
  } catch {
    return
  }

  try {
    await runWorkflowAction('reopen', row.id, async () => {
      await reopenRepairRecord(row.id, { reason: reason || undefined })
      ElMessage.success('工单已重开并回到待处理')
    })
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '重开报修失败，请稍后重试'))
  }
}

const syncDialogFromRoute = async () => {
  const action = readRouteQueryValue(route.query.action)
  if (!action) {
    return
  }

  if (action === 'create') {
    if (canCreate.value && !dialogVisible.value) {
      editingId.value = null
      resetForm()
      dialogVisible.value = true
    }
    return
  }

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) {
    return
  }

  if (action === 'edit' && canEditOrDelete.value) {
    const matched = tableData.value.find((item) => item.id === id)
    if (matched) {
      onOpenEdit(matched)
      return
    }

    try {
      const res = await fetchRepairRecordById(id)
      onOpenEdit(res.data)
    } catch {
    }
    return
  }

  if (action === 'detail' && !detailVisible.value) {
    await onOpenDetail(id)
  }
}

const onSubmit = async () => {
  if (!canCreate.value) {
    ElMessage.warning('当前账号无报修新增权限')
    return
  }

  if (editingId.value && !canEditOrDelete.value) {
    ElMessage.warning('当前账号无报修编辑权限')
    return
  }

  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) {
    return
  }

  submitLoading.value = true
  try {
    if (editingId.value) {
      await updateRepairRecord(editingId.value, { ...form })
      ElMessage.success('更新成功')
    } else {
      await createRepairRecord({ ...form })
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    notifyDataChanged('repair')
    await Promise.all([loadData(), loadSummary()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存失败，请稍后重试'))
  } finally {
    submitLoading.value = false
  }
}

const onDelete = async (row: RepairRecordItem) => {
  if (!canEditOrDelete.value) {
    ElMessage.warning('当前账号无报修删除权限')
    return
  }

  try {
    await ElMessageBox.confirm(`确认删除报修记录 #${row.id} 吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
  } catch {
    return
  }

  deletingId.value = row.id
  try {
    await deleteRepairRecord(row.id)
    ElMessage.success('删除成功')
    notifyDataChanged('repair')
    await Promise.all([loadData(), loadSummary()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '删除失败，请稍后重试'))
  } finally {
    deletingId.value = null
  }
}

onMounted(async () => {
  if (typeof window !== 'undefined') {
    slaTimer = window.setInterval(() => {
      nowTick.value = Date.now()
    }, 60000)
  }

  const restored = filterPersist.restore()
  applyRouteFilters()

  await runInitialLoad({
    tasks: [loadScope, loadSummary, loadData],
    retryChecks: [
      {
        when: () => restored && query.page > 1 && tableData.value.length === 0 && total.value > 0,
        task: async () => {
          query.page = 1
          await loadData()
        },
      },
    ],
  })

  await syncDialogFromRoute()
})

onUnmounted(() => {
  if (slaTimer !== null && typeof window !== 'undefined') {
    window.clearInterval(slaTimer)
  }
})

watch(dialogVisible, (visible) => {
  if (!visible && !detailVisible.value) {
    void clearRouteActionQuery()
  }
})

watch(detailVisible, (visible) => {
  if (!visible && !dialogVisible.value) {
    void clearRouteActionQuery()
  }
})

watch(() => route.fullPath, () => {
  applyRouteFilters()
  void syncDialogFromRoute()
})
</script>

<style scoped>
.repair-detail-timeline {
  margin-top: 20px;
}

.repair-detail-timeline__title {
  margin-bottom: 12px;
  font-size: 13px;
  font-weight: 600;
  color: #4a5a6a;
}
</style>


