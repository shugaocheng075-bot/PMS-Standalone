<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">报修记录</h2>
        <div class="page-subtitle">项目报修登记、处理进度跟踪与历史记录查询</div>
      </div>
    </div>

    <!-- 顶部状态卡片概览 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '' }" @click="onStatClick('')"><div class="t">全部</div><div class="v">{{ summary.total }}</div></el-card></el-col> 
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '待处理' }" @click="onStatClick('待处理')"><div class="t">待处理</div><div class="v danger">{{ summary.pendingCount }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '处理中' }" @click="onStatClick('处理中')"><div class="t">处理中</div><div class="v warning">{{ summary.inProgressCount }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '已完成' }" @click="onStatClick('已完成')"><div class="t">已完成</div><div class="v success">{{ summary.completedCount }}</div></el-card></el-col>
    </el-row>

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
            </el-select>
          </el-form-item>
          <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
        </el-form>
      </template>

      <template #toolbar>
        <el-button v-if="canCreate" type="primary" @click="onOpenCreate">新增报修</el-button>
        <el-button :loading="exporting" @click="onExport">导出记录</el-button>
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
      <el-table-column prop="description" label="问题描述" min-width="220" show-overflow-tooltip />
      <el-table-column prop="reportedAt" label="报修日期" width="170">    
        <template #default="scope">{{ formatTime(scope.row.reportedAt || '') }}</template>
      </el-table-column>
      <el-table-column prop="actualWorkHours" label="实际工时" width="100" />
      <el-table-column label="操作" width="160" fixed="right">
        <template #default="scope">
          <el-button
            type="primary"
            link
            :loading="detailLoadingId === scope.row.id"
            :disabled="submitLoading || deletingId === scope.row.id"
            @click="onOpenDetail(scope.row.id)"
          >详情</el-button>
          <el-button
            v-if="canEditOrDelete"
            type="primary"
            link
            :disabled="submitLoading || deletingId === scope.row.id"
            @click="onOpenEdit(scope.row)"
          >编辑</el-button>
          <el-button
            v-if="canEditOrDelete"
            type="danger"
            link
            :loading="deletingId === scope.row.id"
            :disabled="submitLoading || deletingId === scope.row.id"
            @click="onDelete(scope.row)"
          >删除</el-button>
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
        <el-form-item label="状态">
          <el-select v-model="form.status" style="width: 100%">
            <el-option label="待处理" value="待处理" />
            <el-option label="处理中" value="处理中" />
            <el-option label="已完成" value="已完成" />
          </el-select>
        </el-form-item>
        <el-form-item label="问题描述"><el-input v-model="form.description" type="textarea" :rows="3" /></el-form-item>
        <el-form-item label="处理结果"><el-input v-model="form.resolution" type="textarea" :rows="3" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading" @click="dialogVisible = false">取消</el-button>
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
        <el-descriptions-item label="状态">{{ detailItem.status || '-' }}</el-descriptions-item>
        <el-descriptions-item label="报修日期">{{ formatTime(detailItem.reportedAt || '') }}</el-descriptions-item>
        <el-descriptions-item label="实际工时">{{ detailItem.actualWorkHours ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="处理结果">{{ detailItem.resolution || '-' }}</el-descriptions-item>
        <el-descriptions-item label="描述" :span="2">{{ detailItem.description || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer><el-button @click="detailVisible = false">关闭</el-button></template>
    </ProDrawer>
  </div>
</template>
<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import {
  fetchRepairRecords,
  fetchRepairSummary,
  fetchRepairRecordById,
  createRepairRecord,
  updateRepairRecord,
  deleteRepairRecord,
  exportRepairRecords,
} from '../../api/modules/repair'
import { fetchDataScope } from '../../api/modules/access'
import { useAccessControl } from '../../composables/useAccessControl'
import type { RepairRecordItem, RepairRecordSummary, RepairRecordUpsert } from '../../types/repair'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { resolveRepairStatusTag } from '../../utils/statusTag'


const access = useAccessControl()
const canCreate = computed(() => {
  if (!access.canPermission('repair.manage')) {
    return false
  }

  return access.isManager() || access.isOperator()
})
const canEditOrDelete = computed(() => access.isManager() && access.canPermission('repair.manage'))

const loading = ref(false)
const exporting = ref(false)
const submitLoading = ref(false)
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
  status: [{ required: true, message: '请选择状态', trigger: 'change' }],
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

const urgencyTag = (urgency: string) => {
  if (urgency === '非常紧急') return 'danger'
  if (urgency === '紧急') return 'warning'
  return 'info'
}

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


