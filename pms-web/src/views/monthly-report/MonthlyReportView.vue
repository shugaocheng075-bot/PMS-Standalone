<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">月度报告</h2>
        <div class="page-subtitle">按医院与月份管理服务月报</div>
      </div>
      <el-button v-if="canManage" type="primary" @click="onOpenCreate">新增月报</el-button>
    </div>

    <el-card shadow="never" class="filter-card">
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
          </el-select>
        </el-form-item>
        <el-form-item label="提交人"><el-input v-model="query.submittedBy" clearable @keyup.enter="onSearch" /></el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无符合条件的数据">
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
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="scope">
            <el-button
              type="primary"
              link
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenEdit(scope.row)"
            >{{ canManage ? '编辑' : '查看' }}</el-button>
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
          :page-sizes="[15]"
          layout="total, sizes, prev, pager, next"
          :total="total"
          @size-change="onPageSizeChange"
          @current-change="onCurrentPageChange"
        />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑月报' : '新增月报'" width="640px" destroy-on-close>
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="90px" :disabled="!canManage && !!editingId">
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
        <el-form-item label="状态">
          <el-select v-model="form.status" style="width: 160px">
            <el-option label="草稿" value="draft" />
            <el-option label="已提交" value="submitted" />
            <el-option label="已审核" value="approved" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading" @click="dialogVisible = false">{{ canManage ? '取消' : '关闭' }}</el-button>
        <el-button v-if="canManage" type="primary" :loading="submitLoading" :disabled="submitLoading" @click="onSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import {
  fetchMonthlyReports,
  createMonthlyReport,
  updateMonthlyReport,
  deleteMonthlyReport,
} from '../../api/modules/monthly-report'
import { fetchDataScope } from '../../api/modules/access'
import { useAccessControl } from '../../composables/useAccessControl'
import type { MonthlyReportItem, MonthlyReportUpsert } from '../../types/monthly-report'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

const access = useAccessControl()
const canManage = computed(() => access.canPermission('monthly-report.manage'))

const loading = ref(false)
const submitLoading = ref(false)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const deletingId = ref<number | null>(null)
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
  status: 'draft',
})

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

const statusTag = (status: string) => {
  if (status === 'draft') return 'info'
  if (status === 'submitted') return 'warning'
  return 'success'
}

const statusLabel = (status: string) => {
  if (status === 'draft') return '草稿'
  if (status === 'submitted') return '已提交'
  if (status === 'approved') return '已审核'
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
    status: 'draft',
  })
}

const onOpenCreate = () => {
  editingId.value = null
  resetForm()
  dialogVisible.value = true
}

const onOpenEdit = (row: MonthlyReportItem) => {
  editingId.value = row.id
  Object.assign(form, {
    hospitalName: row.hospitalName,
    reportMonth: row.reportMonth,
    title: row.title,
    content: row.content,
    attachments: [...row.attachments],
    status: row.status,
  })
  dialogVisible.value = true
}

const onSubmit = async () => {
  if (!canManage.value) {
    dialogVisible.value = false
    return
  }

  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) {
    return
  }

  submitLoading.value = true
  try {
    if (editingId.value) {
      await updateMonthlyReport(editingId.value, { ...form })
      ElMessage.success('更新成功')
    } else {
      await createMonthlyReport({ ...form })
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    notifyDataChanged('monthly-report')
    await loadData()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存失败，请稍后重试'))
  } finally {
    submitLoading.value = false
  }
}

const onDelete = async (row: MonthlyReportItem) => {
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

onMounted(async () => {
  filterPersist.restore()

  await runInitialLoad({
    tasks: [loadScope, loadData],
  })
})
</script>

<style scoped>
</style>