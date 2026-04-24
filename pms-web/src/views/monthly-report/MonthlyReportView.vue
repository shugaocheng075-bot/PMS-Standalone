<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">月度报告</h2>
        <div class="page-subtitle">按医院与月份管理服务月报</div>
      </div>
      <el-button v-if="canManage" type="primary" @click="onOpenCreate" icon="Plus">新增月报</el-button>
    </div>

    <AppFilterCard>
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="医院名称">
          <el-select v-model="query.hospitalName" clearable filterable placeholder="全部" style="width: 220px">
            <el-option v-for="name in accessibleHospitals" :key="name" :label="name" :value="name" />
          </el-select>
        </el-form-item>
        <el-form-item label="报告月份">
          <el-date-picker
            v-model="queryMonth"
            type="month"
            placeholder="选择月份"
            format="YYYY-MM"
            value-format="YYYY-MM"
            clearable
            style="width: 160px"
            @change="onMonthChange"
          />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="query.status" clearable style="width: 140px" placeholder="全部">
            <el-option label="草稿" value="draft" />
            <el-option label="已提交" value="submitted" />
            <el-option label="已审核" value="approved" />
            <el-option label="已驳回" value="rejected" />
          </el-select>
        </el-form-item>
        <el-form-item label="提交人"><el-input v-model="query.submittedBy" clearable @keyup.enter="onSearch" /></el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
        </el-form-item>
      </el-form>
    </AppFilterCard>

    <AppTableCard>
      <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无符合条件的数据" @row-dblclick="onRowDoubleClick">
        <el-table-column prop="id" label="ID" width="70" />
        <el-table-column prop="hospitalName" label="医院名称" min-width="180" show-overflow-tooltip />
        <el-table-column prop="reportMonth" label="报告月份" width="120" />
        <el-table-column prop="title" label="标题" min-width="220" show-overflow-tooltip />
        <el-table-column prop="submittedBy" label="提交人" width="120" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="scope">
            <el-tag :type="statusTag(scope.row.status)">{{ statusLabel(scope.row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="updatedAt" label="更新时间" width="170">
          <template #default="scope">{{ formatTime(scope.row.updatedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="280" fixed="right">
          <template #default="scope">
            <div class="table-action-group">
              <el-button
                type="primary"
                link
                :disabled="submitLoading || deletingId === scope.row.id || !!workflowLoadingKey"
                @click="onOpenEdit(scope.row)"
               icon="Edit">{{ canEditReport(scope.row) ? '编辑' : '查看' }}</el-button>
              <el-button
                v-if="canSubmitReport(scope.row)"
                type="warning"
                link
                :loading="isWorkflowLoading(scope.row.id, 'submit')"
                :disabled="submitLoading || deletingId === scope.row.id || !!workflowLoadingKey"
                @click="onSubmitForApproval(scope.row)"
               icon="Upload">提交</el-button>
              <el-button
                v-if="canApproveReport(scope.row)"
                type="success"
                link
                :loading="isWorkflowLoading(scope.row.id, 'approve')"
                :disabled="submitLoading || deletingId === scope.row.id || !!workflowLoadingKey"
                @click="onApproveReport(scope.row)"
               icon="Check">通过</el-button>
              <el-button
                v-if="canRejectReport(scope.row)"
                type="danger"
                link
                :loading="isWorkflowLoading(scope.row.id, 'reject')"
                :disabled="submitLoading || deletingId === scope.row.id || !!workflowLoadingKey"
                @click="onRejectReport(scope.row)"
               icon="Close">驳回</el-button>
              <el-button
                v-if="canDeleteReport(scope.row)"
                type="danger"
                link
                :loading="deletingId === scope.row.id"
                :disabled="submitLoading || deletingId === scope.row.id || !!workflowLoadingKey"
                @click="onDelete(scope.row)"
               icon="Delete">删除</el-button>
            </div>
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
    </AppTableCard>

    <AppFormDialog v-model="dialogVisible" :title="dialogTitle" width="640px">
      <div v-if="currentItem" class="report-workflow-panel">
        <div class="report-workflow-head">
          <div>
            <div class="report-workflow-title">流程信息</div>
            <div class="report-workflow-subtitle">月报状态切换改为 submit / approve / reject 动作流，普通编辑不再直接改状态。</div>
          </div>
          <el-tag :type="statusTag(currentItem.status)">{{ statusLabel(currentItem.status) }}</el-tag>
        </div>
        <div class="report-workflow-grid">
          <div class="report-workflow-item">
            <span class="report-workflow-label">提交人</span>
            <span class="report-workflow-value">{{ currentItem.submittedBy || '-' }}</span>
          </div>
          <div class="report-workflow-item">
            <span class="report-workflow-label">审核人</span>
            <span class="report-workflow-value">{{ currentItem.approvedBy || '-' }}</span>
          </div>
          <div class="report-workflow-item">
            <span class="report-workflow-label">审核时间</span>
            <span class="report-workflow-value">{{ formatTime(currentItem.approvedAt || '') }}</span>
          </div>
          <div class="report-workflow-item">
            <span class="report-workflow-label">驳回原因</span>
            <span class="report-workflow-value">{{ currentItem.rejectionReason || '-' }}</span>
          </div>
        </div>
      </div>

      <el-form ref="formRef" :model="form" :rules="formRules" label-width="90px" :disabled="isFormReadOnly">
        <el-form-item label="医院名称" required>
          <el-select v-model="form.hospitalName" filterable placeholder="请选择医院" style="width: 100%">
            <el-option v-for="name in accessibleHospitals" :key="name" :label="name" :value="name" />
          </el-select>
        </el-form-item>
        <el-form-item label="报告月份" required>
          <el-date-picker v-model="form.reportMonth" type="month" placeholder="选择月份" format="YYYY-MM" value-format="YYYY-MM" style="width: 100%" />
        </el-form-item>
        <el-form-item label="标题" required>
          <el-input v-model="form.title" placeholder="请输入标题" />
        </el-form-item>
        <el-form-item label="内容">
          <el-input v-model="form.content" type="textarea" :rows="7" placeholder="请输入月报内容" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading || !!workflowLoadingKey" @click="dialogVisible = false" icon="Close">{{ canManage ? '取消' : '关闭' }}</el-button>
        <el-button
          v-if="canSubmitCurrentReport"
          type="warning"
          :loading="isWorkflowLoading(editingId || 0, 'submit')"
          :disabled="submitLoading || !!workflowLoadingKey"
          @click="onSubmitForApproval()"
         icon="Upload">保存并提交</el-button>
        <el-button
          v-if="canApproveCurrentReport"
          type="success"
          :loading="isWorkflowLoading(editingId || 0, 'approve')"
          :disabled="submitLoading || !!workflowLoadingKey"
          @click="onApproveReport()"
         icon="Check">审核通过</el-button>
        <el-button
          v-if="canRejectCurrentReport"
          type="danger"
          :loading="isWorkflowLoading(editingId || 0, 'reject')"
          :disabled="submitLoading || !!workflowLoadingKey"
          @click="onRejectReport()"
         icon="Close">驳回</el-button>
        <el-button v-if="canSaveForm" type="primary" :loading="submitLoading" :disabled="submitLoading || !!workflowLoadingKey" @click="onSubmit">{{ editingId ? '保存修改' : '创建月报' }}</el-button>
      </template>
    </AppFormDialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import {
  fetchMonthlyReports,
  createMonthlyReport,
  updateMonthlyReport,
  submitMonthlyReport,
  approveMonthlyReport,
  rejectMonthlyReport,
  deleteMonthlyReport,
} from '../../api/modules/monthly-report'
import { fetchDataScope } from '../../api/modules/access'
import { useAccessControl } from '../../composables/useAccessControl'
import type { MonthlyReportItem, MonthlyReportUpsert } from '../../types/monthly-report'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { resolveMonthlyReportStatusTag } from '../../utils/statusTag'
import AppFilterCard from '../../components/AppFilterCard.vue'
import AppTableCard from '../../components/AppTableCard.vue'
import AppFormDialog from '../../components/AppFormDialog.vue'

const access = useAccessControl()
const canManage = computed(() => access.canPermission('monthly-report.manage'))
const route = useRoute()
const router = useRouter()

const loading = ref(false)
const submitLoading = ref(false)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const deletingId = ref<number | null>(null)
const workflowLoadingKey = ref('')
const total = ref(0)
const tableData = ref<MonthlyReportItem[]>([])
const accessibleHospitals = ref<string[]>([])
const queryMonth = ref('')
const formRef = ref<FormInstance>()
const { runInitialLoad } = useResilientLoad()

const query = reactive({
  hospitalName: '',
  reportMonth: '',
  submittedBy: '',
  status: '',
  page: 1,
  size: 15,
})

const form = reactive<MonthlyReportUpsert>({
  hospitalName: '',
  reportMonth: '',
  title: '',
  content: '',
  attachments: [],
})

const canReview = computed(() => {
  if (access.accessProfile.value?.isAdmin) {
    return true
  }

  if (access.accessProfile.value?.dataScope?.scopeType === 'all') {
    return true
  }

  return access.isManager()
})

const currentItem = computed<MonthlyReportItem | null>(() => {
  if (!editingId.value) {
    return null
  }

  return tableData.value.find((item) => item.id === editingId.value) ?? null
})

const dialogTitle = computed(() => {
  if (!editingId.value) {
    return '新增月报'
  }

  return canEditCurrentReport.value ? '编辑月报' : '月报详情'
})

const isDraftOrRejected = (status: string) => status === 'draft' || status === 'rejected'

const canEditReport = (row: MonthlyReportItem) => canManage.value && isDraftOrRejected(row.status)

const canDeleteReport = (row: MonthlyReportItem) => canEditReport(row)

const canSubmitReport = (row: MonthlyReportItem) => canManage.value && isDraftOrRejected(row.status)

const canApproveReport = (row: MonthlyReportItem) => canReview.value && row.status === 'submitted'

const canRejectReport = (row: MonthlyReportItem) => canReview.value && row.status === 'submitted'

const canEditCurrentReport = computed(() => {
  if (!currentItem.value) {
    return canManage.value
  }

  return canEditReport(currentItem.value)
})

const canSubmitCurrentReport = computed(() => {
  if (!currentItem.value) {
    return false
  }

  return canSubmitReport(currentItem.value)
})

const canApproveCurrentReport = computed(() => {
  if (!currentItem.value) {
    return false
  }

  return canApproveReport(currentItem.value)
})

const canRejectCurrentReport = computed(() => {
  if (!currentItem.value) {
    return false
  }

  return canRejectReport(currentItem.value)
})

const canSaveForm = computed(() => canManage.value && (!editingId.value || canEditCurrentReport.value))

const isFormReadOnly = computed(() => {
  if (!editingId.value) {
    return !canManage.value
  }

  return !canEditCurrentReport.value
})

type MonthlyReportWorkflowAction = 'submit' | 'approve' | 'reject'

const buildWorkflowKey = (id: number, action: MonthlyReportWorkflowAction) => `${id}:${action}`

const isWorkflowLoading = (id: number, action: MonthlyReportWorkflowAction) => {
  if (!id) {
    return false
  }

  return workflowLoadingKey.value === buildWorkflowKey(id, action)
}

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

type MonthlyReportFilterState = {
  hospitalName: string
  reportMonth: string
  submittedBy: string
  status: string
  page: number
  size: number
}

const filterPersist = useFilterStatePersist<MonthlyReportFilterState>({
  key: 'monthly-reports',
  getState: () => ({ ...query }),
  applyState: (state) => {
    query.hospitalName = typeof state.hospitalName === 'string' ? state.hospitalName : ''
    query.reportMonth = typeof state.reportMonth === 'string' ? state.reportMonth : ''
    query.submittedBy = typeof state.submittedBy === 'string' ? state.submittedBy : ''
    query.status = typeof state.status === 'string' ? state.status : ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
    queryMonth.value = query.reportMonth
  },
})

const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await loadData()
  },
  scope: 'monthly-report',
  intervalMs: 60000,
})

const formRules: FormRules<MonthlyReportUpsert> = {
  hospitalName: [{ required: true, message: '请选择医院名称', trigger: 'change' }],
  reportMonth: [{ required: true, message: '请选择报告月份', trigger: 'change' }],
  title: [{ required: true, message: '请输入标题', trigger: 'blur' }],
}

const statusTag = (status: string) => resolveMonthlyReportStatusTag(status)

const statusLabel = (status: string) => {
  if (status === 'draft') return '草稿'
  if (status === 'submitted') return '已提交'
  if (status === 'approved') return '已审核'
  if (status === 'rejected') return '已驳回'
  return status
}

const formatTime = (iso: string) => {
  if (!iso) return '-'
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString('zh-CN', { dateStyle: 'short', timeStyle: 'short' })
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchMonthlyReports(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载月报失败，请稍后重试'))
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

const onMonthChange = (value: string | null) => {
  query.reportMonth = value || ''
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.hospitalName = ''
  query.reportMonth = ''
  query.submittedBy = ''
  query.status = ''
  query.page = 1
  query.size = 15
  queryMonth.value = ''
  filterPersist.clear()
  loadData()
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
    hospitalName: '',
    reportMonth: '',
    title: '',
    content: '',
    attachments: [],
  })
}

const applyItemToForm = (row: MonthlyReportItem) => {
  editingId.value = row.id
  Object.assign(form, {
    hospitalName: row.hospitalName,
    reportMonth: row.reportMonth,
    title: row.title,
    content: row.content,
    attachments: [...row.attachments],
  })
}

const openCreateDialog = () => {
  editingId.value = null
  resetForm()
  dialogVisible.value = true
}

const openEditDialog = (row: MonthlyReportItem) => {
  applyItemToForm(row)
  dialogVisible.value = true
}

const onOpenCreate = () => {
  openCreateDialog()
  void updateRouteQuery({ action: 'create', id: undefined })
}

const onOpenEdit = (row: MonthlyReportItem) => {
  openEditDialog(row)
  void updateRouteQuery({ action: 'edit', id: String(row.id) })
}

const onRowDoubleClick = (row: MonthlyReportItem) => {
  onOpenEdit(row)
}

const syncDialogFromRoute = () => {
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

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) {
    return
  }

  const matched = tableData.value.find((item) => item.id === id)
  if (matched) {
    openEditDialog(matched)
  }
}

const syncEditingFormFromTable = () => {
  if (!editingId.value) {
    return
  }

  const matched = tableData.value.find((item) => item.id === editingId.value)
  if (!matched) {
    return
  }

  applyItemToForm(matched)
}

const validateForm = async () => {
  const valid = await formRef.value?.validate().then(() => true).catch(() => false)
  return Boolean(valid)
}

const persistForm = async () => {
  const valid = await validateForm()
  if (!valid) {
    return null
  }

  if (editingId.value) {
    const res = await updateMonthlyReport(editingId.value, { ...form })
    return res.data
  }

  const res = await createMonthlyReport({ ...form })
  return res.data
}

const refreshAfterMutation = async () => {
  notifyDataChanged('monthly-report')
  await loadData()
  syncEditingFormFromTable()
}

const runWorkflowAction = async (id: number, action: MonthlyReportWorkflowAction, rejectionReason = '') => {
  workflowLoadingKey.value = buildWorkflowKey(id, action)
  try {
    if (action === 'submit') {
      await submitMonthlyReport(id)
      ElMessage.success('已提交审批')
    } else if (action === 'approve') {
      await approveMonthlyReport(id)
      ElMessage.success('已审核通过')
    } else {
      await rejectMonthlyReport(id, { rejectionReason })
      ElMessage.success('已驳回月报')
    }

    await refreshAfterMutation()
  } catch (error) {
    const fallback = action === 'submit'
      ? '提交审批失败，请稍后重试'
      : action === 'approve'
        ? '审核通过失败，请稍后重试'
        : '驳回月报失败，请稍后重试'
    ElMessage.error(getErrorMessage(error, fallback))
  } finally {
    workflowLoadingKey.value = ''
  }
}

const requestRejectReason = async () => {
  try {
    const { value } = await ElMessageBox.prompt('请输入驳回原因（可选）', '驳回月报', {
      confirmButtonText: '确认驳回',
      cancelButtonText: '取消',
      inputType: 'textarea',
      inputPlaceholder: '补充修改建议，便于提交人修订后重新提交',
      inputValue: currentItem.value?.rejectionReason ?? '',
    })

    return value.trim()
  } catch {
    return null
  }
}

const onSubmit = async () => {
  if (!canManage.value) {
    dialogVisible.value = false
    return
  }

  const isEditing = Boolean(editingId.value)
  submitLoading.value = true
  try {
    const item = await persistForm()
    if (!item) {
      return
    }

    ElMessage.success(isEditing ? '更新成功' : '创建成功')
    dialogVisible.value = false
    await refreshAfterMutation()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存失败，请稍后重试'))
  } finally {
    submitLoading.value = false
  }
}

const onSubmitForApproval = async (row?: MonthlyReportItem) => {
  if (row) {
    if (!canSubmitReport(row)) {
      return
    }

    await runWorkflowAction(row.id, 'submit')
    return
  }

  if (!editingId.value || !canSubmitCurrentReport.value) {
    return
  }

  submitLoading.value = true
  try {
    const item = await persistForm()
    if (!item) {
      return
    }

    await runWorkflowAction(item.id, 'submit')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '提交审批失败，请稍后重试'))
  } finally {
    submitLoading.value = false
  }
}

const onApproveReport = async (row?: MonthlyReportItem) => {
  const target = row ?? currentItem.value
  if (!target || !canApproveReport(target)) {
    return
  }

  await runWorkflowAction(target.id, 'approve')
}

const onRejectReport = async (row?: MonthlyReportItem) => {
  const target = row ?? currentItem.value
  if (!target || !canRejectReport(target)) {
    return
  }

  const rejectionReason = await requestRejectReason()
  if (rejectionReason === null) {
    return
  }

  await runWorkflowAction(target.id, 'reject', rejectionReason)
}

const onDelete = async (row: MonthlyReportItem) => {
  if (!canDeleteReport(row)) {
    ElMessage.warning('仅草稿或已驳回的月报可以删除')
    return
  }

  try {
    await ElMessageBox.confirm(`确认删除《${row.title}》吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
  } catch {
    return
  }

  deletingId.value = row.id
  try {
    await deleteMonthlyReport(row.id)
    ElMessage.success('删除成功')
    notifyDataChanged('monthly-report')
    await loadData()
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
  syncDialogFromRoute()
})

onMounted(async () => {
  filterPersist.restore()

  await access.ensureAccessProfileLoaded()

  await runInitialLoad({
    tasks: [loadScope, loadData],
  })

  syncDialogFromRoute()
})
</script>

<style scoped>
.report-workflow-panel {
  margin-bottom: 18px;
  padding: 16px 18px;
  border-radius: 16px;
  background: var(--el-fill-color-light);
  border: 1px solid rgba(15, 23, 42, 0.06);
}

.report-workflow-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 14px;
}

.report-workflow-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.report-workflow-subtitle {
  margin-top: 4px;
  font-size: 12px;
  line-height: 1.6;
  color: var(--el-text-color-secondary);
}

.report-workflow-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 12px;
}

.report-workflow-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.report-workflow-label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.report-workflow-value {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  word-break: break-word;
}
</style>