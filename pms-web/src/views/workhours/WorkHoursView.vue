<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">工时管理</h2>
        <div class="page-subtitle">项目工时登记、统计与查询</div>
      </div>


    </div>

    <SummaryMetrics :items="summaryCards" :columns="5" @select="onSummaryCardSelect" />

    

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
            @row-dblclick="onRowDoubleClick"
    >
      <template #toolbar>
        <el-button v-if="canManage" type="primary" @click="onOpenCreate" icon="Plus">新增工时</el-button>
        <el-button :loading="exporting" @click="onExport" icon="Download">导出Excel</el-button>
      </template>

      <template #search>
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="人员"><el-input v-model="query.personnelName" clearable @keyup.enter="onSearch" /></el-form-item>
        <el-form-item label="医院名称">
          <el-select v-model="query.hospitalName" clearable filterable placeholder="全部" style="width: 220px">
            <el-option v-for="name in accessibleHospitals" :key="name" :label="name" :value="name" />
          </el-select>
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="query.workType" clearable style="width: 120px" placeholder="全部">
            <el-option label="驻场" value="驻场" />
            <el-option label="远程" value="远程" />
            <el-option label="出差" value="出差" />
            <el-option label="病假" value="病假" />
            <el-option label="事假" value="事假" />
            <el-option label="其他特殊" value="其他特殊" />
          </el-select>
        </el-form-item>
        <el-form-item label="日期范围">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            style="width: 260px"
          />
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
        </el-form-item>
      </el-form>
    </template>

    

        <el-table-column prop="id" label="ID" width="70" />
        <el-table-column prop="personnelName" label="人员" width="100" />
        <el-table-column prop="hospitalName" label="医院名称" min-width="220" show-overflow-tooltip />
        <el-table-column prop="workDate" label="工作日期" width="120" />
        <el-table-column prop="hours" label="工时(h)" width="90" />
        <el-table-column prop="workType" label="类型" width="90">
          <template #default="scope">
            <el-tag :type="typeTag(scope.row.workType)">{{ scope.row.workType }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="scope">
            <el-tag :type="statusTag(scope.row.status)">{{ statusLabel(scope.row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="工作内容" min-width="320" show-overflow-tooltip />
        <el-table-column prop="createdAt" label="创建时间" width="178">
          <template #default="scope">{{ formatTime(scope.row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="360" fixed="right">
          <template #default="scope">
            <div class="table-action-group">
              <el-button
                type="primary"
                link
                :loading="detailLoadingId === scope.row.id"
                :disabled="submitLoading || deletingId === scope.row.id || workflowBusy"
                @click="onOpenDetail(scope.row.id)"
               icon="Document">详情</el-button>
              <el-button
                v-if="canEditWorkHours(scope.row)"
                type="primary"
                link
                :disabled="submitLoading || deletingId === scope.row.id || workflowBusy"
                @click="onOpenEdit(scope.row)"
               icon="Edit">编辑</el-button>
              <el-button
                v-if="canSubmitWorkHours(scope.row)"
                type="warning"
                link
                :loading="isWorkflowLoading(scope.row.id, 'submit')"
                :disabled="submitLoading || deletingId === scope.row.id || workflowBusy"
                @click="onSubmitForApproval(scope.row)"
               icon="Upload">提交</el-button>
              <el-button
                v-if="canConfirmWorkHours(scope.row)"
                type="success"
                link
                :loading="isWorkflowLoading(scope.row.id, 'confirm')"
                :disabled="submitLoading || deletingId === scope.row.id || workflowBusy"
                @click="onConfirmWorkHours(scope.row)"
               icon="Check">确认</el-button>
              <el-button
                v-if="canRejectWorkHours(scope.row)"
                type="danger"
                link
                :loading="isWorkflowLoading(scope.row.id, 'reject')"
                :disabled="submitLoading || deletingId === scope.row.id || workflowBusy"
                @click="onRejectWorkHours(scope.row)"
               icon="RefreshLeft">退回</el-button>
              <el-button
                v-if="canDeleteWorkHours(scope.row)"
                type="danger"
                link
                :loading="deletingId === scope.row.id"
                :disabled="submitLoading || deletingId === scope.row.id || workflowBusy"
                @click="onDelete(scope.row)"
               icon="Delete">删除</el-button>
            </div>
          </template>
        </el-table-column>



    
    </ProTable>

    <ProDrawer v-model="dialogVisible" :title="editingId ? '编辑工时' : '新增工时'" width="600px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="90px">
        <el-form-item label="医院名称" prop="hospitalName" required>
          <el-select v-model="form.hospitalName" filterable placeholder="请选择医院" style="width: 100%">
            <el-option v-for="name in accessibleHospitals" :key="name" :label="name" :value="name" />
          </el-select>
        </el-form-item>
        <el-form-item label="人员姓名"><el-input v-model="personnelName" /></el-form-item>
        <el-form-item label="工作日期" prop="workDate">
          <el-date-picker
            v-model="form.workDate"
            type="date"
            value-format="YYYY-MM-DD"
            :disabled-date="disableFutureDates"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="工时(h)" prop="hours"><el-input-number v-model="form.hours" :min="0.5" :max="24" :step="0.5" style="width: 100%" /></el-form-item>
        <el-form-item label="工作类型" prop="workType">
          <el-select v-model="form.workType" style="width: 100%">
            <el-option label="驻场" value="驻场" />
            <el-option label="远程" value="远程" />
            <el-option label="出差" value="出差" />
            <el-option label="病假" value="病假" />
            <el-option label="事假" value="事假" />
            <el-option label="其他特殊" value="其他特殊" />
          </el-select>
        </el-form-item>
        <el-form-item label="工作内容" prop="description"><el-input v-model="form.description" type="textarea" :rows="3" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading" @click="dialogVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="submitLoading" :disabled="submitLoading" @click="onSubmit">确定</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="detailVisible" title="工时详情" width="620px">
      <el-descriptions v-if="detailItem" :column="2" border>
        <el-descriptions-item label="人员">{{ detailItem.personnelName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="医院名称">{{ detailItem.hospitalName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="工作日期">{{ detailItem.workDate || '-' }}</el-descriptions-item>
        <el-descriptions-item label="工时(h)">{{ detailItem.hours ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="工作类型">{{ detailItem.workType || '-' }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="statusTag(detailItem.status)">{{ statusLabel(detailItem.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ formatTime(detailItem.createdAt) }}</el-descriptions-item>
        <el-descriptions-item label="确认人">{{ detailItem.confirmedBy || '-' }}</el-descriptions-item>
        <el-descriptions-item label="确认时间">{{ formatTime(detailItem.confirmedAt || '') }}</el-descriptions-item>
        <el-descriptions-item label="工作内容" :span="2">{{ detailItem.description || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button
          v-if="detailItem && canSubmitWorkHours(detailItem)"
          type="warning"
          :loading="isWorkflowLoading(detailItem.id, 'submit')"
          :disabled="workflowBusy"
          @click="onSubmitForApproval(detailItem)"
         icon="Upload">提交审批</el-button>
        <el-button
          v-if="detailItem && canConfirmWorkHours(detailItem)"
          type="success"
          :loading="isWorkflowLoading(detailItem.id, 'confirm')"
          :disabled="workflowBusy"
          @click="onConfirmWorkHours(detailItem)"
         icon="Check">确认通过</el-button>
        <el-button
          v-if="detailItem && canRejectWorkHours(detailItem)"
          type="danger"
          :loading="isWorkflowLoading(detailItem.id, 'reject')"
          :disabled="workflowBusy"
          @click="onRejectWorkHours(detailItem)"
         icon="RefreshLeft">退回修改</el-button>
        <el-button type="primary" @click="detailVisible = false">关闭</el-button>
      </template>
    </ProDrawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import {
  fetchWorkHours,
  fetchWorkHoursSummary,
  fetchWorkHoursById,
  createWorkHours,
  updateWorkHours,
  submitWorkHours,
  confirmWorkHours,
  rejectWorkHours,
  deleteWorkHours,
  exportWorkHours,
} from '../../api/modules/workhours'
import { fetchDataScope } from '../../api/modules/access'
import { useAccessControl } from '../../composables/useAccessControl'
import type { WorkHoursItem, WorkHoursSummary, WorkHoursUpsert } from '../../types/workhours'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import ProTable from '../../components/ProTable.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'


const access = useAccessControl()
const canManage = computed(() => access.isManager() && access.canPermission('workhours.manage'))
const route = useRoute()
const router = useRouter()

const loading = ref(false)
const exporting = ref(false)
const submitLoading = ref(false)
const workflowLoadingKey = ref('')
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const deletingId = ref<number | null>(null)
const detailVisible = ref(false)
const detailLoadingId = ref<number | null>(null)
const total = ref(0)
const tableData = ref<WorkHoursItem[]>([])
const accessibleHospitals = ref<string[]>([])
const dateRange = ref<[string, string] | null>(null)
const detailItem = ref<WorkHoursItem | null>(null)
const summary = ref<WorkHoursSummary>({ total: 0, totalHours: 0, onsiteCount: 0, remoteCount: 0, travelCount: 0 })
const workTypeOptions = ['驻场', '远程', '出差', '病假', '事假', '其他特殊'] as const
const workflowBusy = computed(() => workflowLoadingKey.value.length > 0)

const query = reactive({
  personnelName: '',
  hospitalName: '',
  workType: '',
  page: 1,
  size: 15,
})

type WorkHoursSummaryCard = {
  key: string
  title: string
  value: number | string
  context: string
  note: string
  color: string
  active?: boolean
  clickable?: boolean
}

const summaryCards = computed<WorkHoursSummaryCard[]>(() => [
  {
    key: 'total',
    title: '记录总数',
    value: summary.value.total,
    context: '工作量概览',
    note: '当前筛选口径下的工时记录条数',
    color: '#3f4f63',
    clickable: false,
  },
  {
    key: 'hours',
    title: '总工时(h)',
    value: summary.value.totalHours,
    context: '工作量概览',
    note: '当前筛选范围内累计录入工时',
    color: '#7c98bc',
    clickable: false,
  },
  {
    key: '驻场',
    title: '驻场',
    value: summary.value.onsiteCount,
    context: '类型筛选',
    note: '查看现场服务的工时记录',
    color: '#7d9f92',
    active: query.workType === '驻场',
  },
  {
    key: '远程',
    title: '远程',
    value: summary.value.remoteCount,
    context: '类型筛选',
    note: '查看远程支持与远程处理记录',
    color: '#c7a06c',
    active: query.workType === '远程',
  },
  {
    key: '出差',
    title: '出差',
    value: summary.value.travelCount,
    context: '类型筛选',
    note: '查看出差任务对应的工时填报',
    color: '#c58a87',
    active: query.workType === '出差',
  },
])

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

const form = reactive<WorkHoursUpsert>({
  projectId: 0,
  hospitalName: '',
  workDate: '',
  hours: 8,
  workType: '驻场',
  description: '',
})

const personnelName = ref('')
const formRef = ref<FormInstance>()
const { runInitialLoad } = useResilientLoad()

type WorkHoursFilterState = {
  personnelName: string
  hospitalName: string
  workType: string
  page: number
  size: number
  dateFrom: string
  dateTo: string
}

const filterPersist = useFilterStatePersist<WorkHoursFilterState>({
  key: 'workhours',
  getState: () => ({
    ...query,
    dateFrom: dateRange.value?.[0] ?? '',
    dateTo: dateRange.value?.[1] ?? '',
  }),
  applyState: (state) => {
    query.personnelName = typeof state.personnelName === 'string' ? state.personnelName : ''
    query.hospitalName = typeof state.hospitalName === 'string' ? state.hospitalName : ''
    query.workType = typeof state.workType === 'string' ? state.workType : ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
    const dateFrom = typeof state.dateFrom === 'string' ? state.dateFrom : ''
    const dateTo = typeof state.dateTo === 'string' ? state.dateTo : ''
    dateRange.value = dateFrom && dateTo ? [dateFrom, dateTo] : null
  },
})

const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await Promise.allSettled([loadSummary(), loadData()])
  },
  scope: 'workhours',
  intervalMs: 60000,
})

const formRules: FormRules<WorkHoursUpsert> = {
  hospitalName: [{ required: true, message: '请选择医院名称', trigger: 'change' }],
  workDate: [{ required: true, message: '请选择工作日期', trigger: 'change' }],
  hours: [
    { required: true, message: '请填写工时', trigger: 'change' },
    {
      validator: (_rule, value: number, callback) => {
        if (typeof value !== 'number' || Number.isNaN(value)) {
          callback(new Error('请填写有效工时'))
          return
        }

        if (value < 0.5 || value > 24) {
          callback(new Error('工时需在0.5到24之间'))
          return
        }

        callback()
      },
      trigger: 'change',
    },
  ],
  workType: [{ required: true, message: '请选择工作类型', trigger: 'change' }],
  description: [
    { required: true, message: '请填写工作内容', trigger: 'blur' },
    { min: 2, max: 500, message: '工作内容长度需在2到500个字符之间', trigger: 'blur' },
  ],
}

const disableFutureDates = (date: Date) => {
  const today = new Date()
  today.setHours(23, 59, 59, 999)
  return date.getTime() > today.getTime()
}

const typeTag = (type: string) => {
  if (type === '驻场') return 'success'
  if (type === '远程') return 'warning'
  if (type === '病假') return 'danger'
  if (type === '事假' || type === '其他特殊') return 'info'
  return 'danger'
}

const statusTag = (status: string) => {
  if (status === 'confirmed') return 'success'
  if (status === 'submitted') return 'warning'
  if (status === 'rejected') return 'danger'
  return 'info'
}

const statusLabel = (status: string) => {
  if (status === 'draft') return '草稿'
  if (status === 'submitted') return '已提交'
  if (status === 'confirmed') return '已确认'
  if (status === 'rejected') return '已退回'
  return status || '-'
}

const canEditWorkHours = (row: WorkHoursItem) => canManage.value && (row.status === 'draft' || row.status === 'rejected')

const canDeleteWorkHours = (row: WorkHoursItem) => canManage.value && row.status !== 'submitted'

const canSubmitWorkHours = (row: WorkHoursItem) => canManage.value && (row.status === 'draft' || row.status === 'rejected')

const canConfirmWorkHours = (row: WorkHoursItem) => canManage.value && row.status === 'submitted'

const canRejectWorkHours = (row: WorkHoursItem) => canManage.value && row.status === 'submitted'

type WorkHoursWorkflowAction = 'submit' | 'confirm' | 'reject'

const buildWorkflowKey = (id: number, action: WorkHoursWorkflowAction) => `${id}:${action}`

const isWorkflowLoading = (id: number, action: WorkHoursWorkflowAction) => {
  if (!id) {
    return false
  }

  return workflowLoadingKey.value === buildWorkflowKey(id, action)
}

const formatTime = (iso: string) => {
  if (!iso) return '-'
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString('zh-CN', { dateStyle: 'short', timeStyle: 'short' })
}

const loadSummary = async () => {
  try {
    const res = await fetchWorkHoursSummary()
    summary.value = res.data
  } catch (error) {
    summary.value = { total: 0, totalHours: 0, onsiteCount: 0, remoteCount: 0, travelCount: 0 }
    ElMessage.error(getErrorMessage(error, '加载工时汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchWorkHours({
      personnelName: query.personnelName || undefined,
      hospitalName: query.hospitalName || undefined,
      workType: query.workType || undefined,
      workDateFrom: dateRange.value?.[0] || undefined,
      workDateTo: dateRange.value?.[1] || undefined,
      page: query.page,
      size: query.size,
    })
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载工时记录失败，请稍后重试'))
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
  query.personnelName = ''
  query.hospitalName = ''
  query.workType = ''
  dateRange.value = null
  query.page = 1
  query.size = 15
  filterPersist.clear()
  void updateRouteQuery({ personnelName: undefined, hospitalName: undefined, workType: undefined, action: undefined, id: undefined })
  loadData()
}

const onExport = async () => {
  exporting.value = true
  try {
    const blob = await exportWorkHours({
      personnelName: query.personnelName || undefined,
      hospitalName: query.hospitalName || undefined,
      workType: query.workType || undefined,
      workDateFrom: dateRange.value?.[0] || undefined,
      workDateTo: dateRange.value?.[1] || undefined,
    })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `工时明细_${Date.now()}.xlsx`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出工时数据失败，请稍后重试'))
  } finally {
    exporting.value = false
  }
}

const onTypeClick = (workType: string) => {
  query.workType = query.workType === workType ? '' : workType
  query.page = 1
  void updateRouteQuery({ personnelName: undefined, hospitalName: undefined, workType: undefined, action: undefined, id: undefined })
  loadData()
}

const onSummaryCardSelect = (card: { key?: string | number; clickable?: boolean }) => {
  if (card.clickable === false || typeof card.key !== 'string') {
    return
  }

  onTypeClick(card.key)
}

const applyRouteFilters = () => {
  const personnelName = readRouteQueryValue(route.query.personnelName)
  const hospitalName = readRouteQueryValue(route.query.hospitalName)
  const workType = readRouteQueryValue(route.query.workType)

  if (personnelName) {
    query.personnelName = personnelName
  }

  if (hospitalName) {
    query.hospitalName = hospitalName
  }

  if (!workType || !workTypeOptions.includes(workType as (typeof workTypeOptions)[number])) {
    if (personnelName || hospitalName) {
      query.page = 1
    }
    return
  }

  query.workType = workType
  query.page = 1
}





const resetForm = () => {
  Object.assign(form, {
    projectId: 0,
    hospitalName: '',
    workDate: '',
    hours: 8,
    workType: '驻场',
    description: '',
  })
  personnelName.value = ''
}

const fillCreateWorkType = () => {
  const workType = readRouteQueryValue(route.query.workType)
  if (workType && workTypeOptions.includes(workType as (typeof workTypeOptions)[number])) {
    form.workType = workType
  }

  const routePersonnelName = readRouteQueryValue(route.query.personnelName)
  const routeHospitalName = readRouteQueryValue(route.query.hospitalName)
  if (routePersonnelName) {
    personnelName.value = routePersonnelName
  }

  if (routeHospitalName) {
    form.hospitalName = routeHospitalName
  }
}

const openCreateDialog = () => {
  editingId.value = null
  resetForm()
  fillCreateWorkType()
  dialogVisible.value = true
}

const openEditDialog = (row: WorkHoursItem) => {
  editingId.value = row.id
  Object.assign(form, {
    projectId: row.projectId,
    hospitalName: row.hospitalName,
    workDate: row.workDate,
    hours: row.hours,
    workType: row.workType,
    description: row.description,
  })
  personnelName.value = row.personnelName
  dialogVisible.value = true
}

const onOpenCreate = () => {
  if (!canManage.value) {
    ElMessage.warning('当前账号无工时管理权限')
    return
  }

  openCreateDialog()
  void updateRouteQuery({ action: 'create', id: undefined })
}

const onOpenEdit = (row: WorkHoursItem) => {
  if (!canManage.value) {
    ElMessage.warning('当前账号无工时管理权限')
    return
  }

  if (!canEditWorkHours(row)) {
    ElMessage.warning('仅草稿或已退回的工时记录可以编辑')
    return
  }

  openEditDialog(row)
  void updateRouteQuery({ action: 'edit', id: String(row.id) })
}

const onRowDoubleClick = (row: WorkHoursItem) => {
  if (!canManage.value) {
    return
  }

  if (!canEditWorkHours(row)) {
    void onOpenDetail(row.id)
    return
  }

  onOpenEdit(row)
}

const syncDialogFromRoute = async () => {
  const action = readRouteQueryValue(route.query.action)
  if (!action) {
    return
  }

  if (action === 'create') {
    if (canManage.value && !dialogVisible.value) {
      openCreateDialog()
    }
    return
  }

  if (action !== 'edit' || !canManage.value) {
    return
  }

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) {
    return
  }

  const matched = tableData.value.find((item) => item.id === id)
  if (matched) {
    openEditDialog(matched)
    return
  }

  try {
    const res = await fetchWorkHoursById(id)
    openEditDialog(res.data)
  } catch {
  }
}

const onOpenDetail = async (id: number) => {
  detailLoadingId.value = id
  try {
    const res = await fetchWorkHoursById(id)
    detailItem.value = res.data
    detailVisible.value = true
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载工时详情失败，请稍后重试'))
  } finally {
    detailLoadingId.value = null
  }
}

const onSubmit = async () => {
  if (!canManage.value) {
    ElMessage.warning('当前账号无工时管理权限')
    return
  }

  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) {
    return
  }

  submitLoading.value = true
  try {
    if (editingId.value) {
      await updateWorkHours(editingId.value, { ...form })
      ElMessage.success('更新成功')
    } else {
      await createWorkHours({ ...form })
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    notifyDataChanged('workhours')
    await Promise.all([loadData(), loadSummary()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存失败，请稍后重试'))
  } finally {
    submitLoading.value = false
  }
}

const refreshAfterMutation = async () => {
  notifyDataChanged('workhours')
  await Promise.all([loadData(), loadSummary()])
  if (detailItem.value) {
    const matched = tableData.value.find((item) => item.id === detailItem.value?.id)
    if (matched) {
      detailItem.value = matched
    }
  }
}

const runWorkflowAction = async (row: WorkHoursItem, action: WorkHoursWorkflowAction) => {
  workflowLoadingKey.value = buildWorkflowKey(row.id, action)
  try {
    let item: WorkHoursItem
    if (action === 'submit') {
      const res = await submitWorkHours(row.id)
      item = res.data
      ElMessage.success('工时已提交审批')
    } else if (action === 'confirm') {
      const res = await confirmWorkHours(row.id)
      item = res.data
      ElMessage.success('工时已确认')
    } else {
      const res = await rejectWorkHours(row.id)
      item = res.data
      ElMessage.success('工时已退回')
    }

    if (detailItem.value?.id === item.id) {
      detailItem.value = item
    }

    await refreshAfterMutation()
  } catch (error) {
    const fallback = action === 'submit'
      ? '提交工时审批失败，请稍后重试'
      : action === 'confirm'
        ? '确认工时失败，请稍后重试'
        : '退回工时失败，请稍后重试'
    ElMessage.error(getErrorMessage(error, fallback))
  } finally {
    workflowLoadingKey.value = ''
  }
}

const onSubmitForApproval = async (row: WorkHoursItem) => {
  if (!canSubmitWorkHours(row)) {
    return
  }

  await runWorkflowAction(row, 'submit')
}

const onConfirmWorkHours = async (row: WorkHoursItem) => {
  if (!canConfirmWorkHours(row)) {
    return
  }

  await runWorkflowAction(row, 'confirm')
}

const onRejectWorkHours = async (row: WorkHoursItem) => {
  if (!canRejectWorkHours(row)) {
    return
  }

  await runWorkflowAction(row, 'reject')
}

const onDelete = async (row: WorkHoursItem) => {
  if (!canManage.value) {
    ElMessage.warning('当前账号无工时管理权限')
    return
  }

  if (!canDeleteWorkHours(row)) {
    ElMessage.warning('已提交待确认的工时记录不可删除')
    return
  }

  try {
    await ElMessageBox.confirm(`确认删除工时记录 #${row.id} 吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
  } catch {
    return
  }

  deletingId.value = row.id
  try {
    await deleteWorkHours(row.id)
    ElMessage.success('删除成功')
    notifyDataChanged('workhours')
    await Promise.all([loadData(), loadSummary()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '删除失败，请稍后重试'))
  } finally {
    deletingId.value = null
  }
}

watch(dialogVisible, (visible) => {
  if (!visible) {
    void clearRouteActionQuery()
  }
})

watch(() => route.fullPath, () => {
  applyRouteFilters()
  void syncDialogFromRoute()
})

onMounted(async () => {
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
</script>

<style scoped>
.table-action-group {
  flex-wrap: nowrap;
}
</style>

