<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">工时管理</h2>
        <div class="page-subtitle">项目工时登记、统计与查询</div>
      </div>
      <el-button v-if="canManage" type="primary" @click="onOpenCreate">新增工时</el-button>
      <el-button :loading="exporting" @click="onExport">导出Excel</el-button>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="5"><el-card shadow="never" class="stat-card stats-card"><div class="t">记录总数</div><div class="v">{{ summary.total }}</div></el-card></el-col>
      <el-col :span="5"><el-card shadow="never" class="stat-card stats-card"><div class="t">总工时(h)</div><div class="v info">{{ summary.totalHours }}</div></el-card></el-col>
      <el-col :span="5"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.workType === '驻场' }" @click="onTypeClick('驻场')"><div class="t">驻场</div><div class="v success">{{ summary.onsiteCount }}</div></el-card></el-col>
      <el-col :span="5"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.workType === '远程' }" @click="onTypeClick('远程')"><div class="t">远程</div><div class="v warning">{{ summary.remoteCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.workType === '出差' }" @click="onTypeClick('出差')"><div class="t">出差</div><div class="v danger">{{ summary.travelCount }}</div></el-card></el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
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
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无符合条件的数据" @row-dblclick="onRowDoubleClick">
        <el-table-column prop="id" label="ID" width="70" />
        <el-table-column prop="personnelName" label="人员" width="100" />
        <el-table-column prop="hospitalName" label="医院名称" min-width="180" show-overflow-tooltip />
        <el-table-column prop="workDate" label="工作日期" width="120" />
        <el-table-column prop="hours" label="工时(h)" width="90" />
        <el-table-column prop="workType" label="类型" width="90">
          <template #default="scope">
            <el-tag :type="typeTag(scope.row.workType)">{{ scope.row.workType }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="工作内容" min-width="260" show-overflow-tooltip />
        <el-table-column prop="createdAt" label="创建时间" width="170">
          <template #default="scope">{{ formatTime(scope.row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="scope">
            <el-button
              type="primary"
              link
              :loading="detailLoadingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenDetail(scope.row.id)"
            >详情</el-button>
            <el-button
              v-if="canManage"
              type="primary"
              link
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenEdit(scope.row)"
            >编辑</el-button>
            <el-button
              v-if="canManage"
              type="danger"
              link
              :loading="deletingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onDelete(scope.row)"
            >删除</el-button>
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
          @size-change="onPageSizeChange"
          @current-change="onCurrentPageChange"
        />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑工时' : '新增工时'" width="600px" destroy-on-close>
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="90px">
        <el-form-item label="医院名称" required>
          <el-select v-model="form.hospitalName" filterable placeholder="请选择医院" style="width: 100%">
            <el-option v-for="name in accessibleHospitals" :key="name" :label="name" :value="name" />
          </el-select>
        </el-form-item>
        <el-form-item label="人员姓名"><el-input v-model="personnelName" /></el-form-item>
        <el-form-item label="工作日期"><el-date-picker v-model="form.workDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" /></el-form-item>
        <el-form-item label="工时(h)"><el-input-number v-model="form.hours" :min="0.5" :max="24" :step="0.5" style="width: 100%" /></el-form-item>
        <el-form-item label="工作类型">
          <el-select v-model="form.workType" style="width: 100%">
            <el-option label="驻场" value="驻场" />
            <el-option label="远程" value="远程" />
            <el-option label="出差" value="出差" />
            <el-option label="病假" value="病假" />
            <el-option label="事假" value="事假" />
            <el-option label="其他特殊" value="其他特殊" />
          </el-select>
        </el-form-item>
        <el-form-item label="工作内容"><el-input v-model="form.description" type="textarea" :rows="3" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading" @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" :disabled="submitLoading" @click="onSubmit">确定</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="detailVisible" title="工时详情" width="620px" destroy-on-close>
      <el-descriptions v-if="detailItem" :column="2" border>
        <el-descriptions-item label="人员">{{ detailItem.personnelName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="医院名称">{{ detailItem.hospitalName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="工作日期">{{ detailItem.workDate || '-' }}</el-descriptions-item>
        <el-descriptions-item label="工时(h)">{{ detailItem.hours ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="工作类型">{{ detailItem.workType || '-' }}</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ formatTime(detailItem.createdAt) }}</el-descriptions-item>
        <el-descriptions-item label="工作内容" :span="2">{{ detailItem.description || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button type="primary" @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
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

const access = useAccessControl()
const canManage = computed(() => access.isManager() && access.canPermission('workhours.manage'))
const route = useRoute()
const router = useRouter()

const loading = ref(false)
const exporting = ref(false)
const submitLoading = ref(false)
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

const query = reactive({
  personnelName: '',
  hospitalName: '',
  workType: '',
  page: 1,
  size: 15,
})

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
  hours: [{ required: true, message: '请填写工时', trigger: 'change' }],
  workType: [{ required: true, message: '请选择工作类型', trigger: 'change' }],
}

const typeTag = (type: string) => {
  if (type === '驻场') return 'success'
  if (type === '远程') return 'warning'
  if (type === '病假') return 'danger'
  if (type === '事假' || type === '其他特殊') return 'info'
  return 'danger'
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
  loadData()
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

const onPageSizeChange = (size: number) => {
  query.size = size
  query.page = 1
  loadData()
}

const onCurrentPageChange = (page: number) => {
  query.page = page
  loadData()
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

  openEditDialog(row)
  void updateRouteQuery({ action: 'edit', id: String(row.id) })
}

const onRowDoubleClick = (row: WorkHoursItem) => {
  if (!canManage.value) {
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

const onDelete = async (row: WorkHoursItem) => {
  if (!canManage.value) {
    ElMessage.warning('当前账号无工时管理权限')
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
</style>