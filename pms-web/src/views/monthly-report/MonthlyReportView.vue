<template>
  <div class="page-shell">
    <div class="monthly-report-hero">
      <div class="monthly-report-hero-main">
        <div class="monthly-report-hero-kicker-row">
          <span class="monthly-report-hero-kicker">Monthly Report Desk</span>
          <span class="monthly-report-hero-badge">{{ activeMonthlyReportFilterLabel }}</span>
        </div>
        <h2 class="monthly-report-hero-title">月报管理</h2>
        <div class="monthly-report-hero-subtitle">
          统一查看当前筛选范围内的月报编写、提审、驳回和审核分布，直接从第一屏进入待处理记录和审批动作。
        </div>
        <div class="monthly-report-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="monthly-report-signal-card">
            <span class="monthly-report-signal-label">{{ item.label }}</span>
            <strong class="monthly-report-signal-value">{{ item.value }}</strong>
            <span class="monthly-report-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="monthly-report-hero-side">
        <div class="monthly-report-control-card">
          <div class="monthly-report-control-copy">
            <span class="monthly-report-control-title">月报台动作</span>
            <span class="monthly-report-control-note">先锁定医院、月份或状态，再从待处理清单进入编辑、提审或审核动作。</span>
          </div>
          <div class="monthly-report-control-actions">
            <el-button size="small" :loading="loading" @click="refreshDesk" icon="Refresh">刷新</el-button>
            <el-button v-if="canManage" size="small" type="primary" @click="onOpenCreate" icon="Plus">新增月报</el-button>
          </div>
        </div>

        <div class="monthly-report-quick-grid">
          <button
            v-for="action in quickActions"
            :key="action.title"
            type="button"
            class="monthly-report-quick-action"
            @click="action.onClick()"
          >
            <span class="monthly-report-quick-title">{{ action.title }}</span>
            <span class="monthly-report-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <div class="monthly-report-insight-grid">
      <section class="monthly-report-insight-card">
        <div class="monthly-report-insight-head">
          <div>
            <div class="monthly-report-insight-title">待审核与已驳回</div>
            <div class="monthly-report-insight-note">优先处理已提交待审核和已驳回待修改的月报，避免月底堆积。</div>
          </div>
          <el-tag size="small" type="warning" effect="light">{{ reviewQueue.length }} 项</el-tag>
        </div>
        <div v-if="reviewQueue.length" class="monthly-report-queue-list">
          <button
            v-for="item in reviewQueue"
            :key="item.id"
            type="button"
            class="monthly-report-queue-item"
            @click="onOpenEdit(item)"
          >
            <div class="monthly-report-queue-main">
              <strong>{{ item.hospitalName || '未填写医院' }}</strong>
              <span>{{ item.reportMonth || '未填写月份' }} · {{ item.submittedBy || '未填写提交人' }}</span>
            </div>
            <div class="monthly-report-queue-meta">
              <el-tag size="small" :type="statusTag(item.status)">{{ statusLabel(item.status) }}</el-tag>
              <span>{{ formatTime(item.updatedAt) }}</span>
            </div>
          </button>
        </div>
        <el-empty v-else description="当前筛选下没有待审核或驳回记录" :image-size="72" />
      </section>

      <section class="monthly-report-insight-card">
        <div class="monthly-report-insight-head">
          <div>
            <div class="monthly-report-insight-title">医院分布</div>
            <div class="monthly-report-insight-note">查看当前筛选范围内月报最集中的医院，便于追踪覆盖和补齐进度。</div>
          </div>
          <span class="monthly-report-insight-meta">{{ filteredRows.length }} 条记录</span>
        </div>
        <div v-if="topHospitalBuckets.length" class="monthly-report-chip-list">
          <div v-for="item in topHospitalBuckets" :key="item.label" class="monthly-report-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 份</span>
          </div>
        </div>
        <el-empty v-else description="暂无医院分布" :image-size="72" />
      </section>

      <section class="monthly-report-insight-card">
        <div class="monthly-report-insight-head">
          <div>
            <div class="monthly-report-insight-title">提交人分布</div>
            <div class="monthly-report-insight-note">按提交人查看当前月报分布，便于主管核查责任归属和交付节奏。</div>
          </div>
          <span class="monthly-report-insight-meta">{{ activeSubmitterCount }} 人参与</span>
        </div>
        <div v-if="topSubmitterBuckets.length" class="monthly-report-chip-list">
          <div v-for="item in topSubmitterBuckets" :key="item.label" class="monthly-report-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 份</span>
          </div>
        </div>
        <el-empty v-else description="暂无提交人分布" :image-size="72" />
      </section>

      <section class="monthly-report-insight-card">
        <div class="monthly-report-insight-head">
          <div>
            <div class="monthly-report-insight-title">状态结构</div>
            <div class="monthly-report-insight-note">判断当前范围内是草稿堆积、待审核堆积，还是已经基本闭环。</div>
          </div>
          <span class="monthly-report-insight-meta">{{ currentMonthRows.length }} 份当月记录</span>
        </div>
        <div v-if="statusBuckets.length" class="monthly-report-chip-list">
          <div v-for="item in statusBuckets" :key="item.label" class="monthly-report-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 份</span>
          </div>
        </div>
        <el-empty v-else description="暂无状态分布" :image-size="72" />
      </section>
    </div>

    <div class="monthly-report-summary-wrap">
      <SummaryMetrics :items="summaryCards" :columns="5" @select="onSummaryCardSelect" />
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
        <el-form-item label="提交人">
          <el-input v-model="query.submittedBy" clearable @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
        </el-form-item>
      </el-form>
    </AppFilterCard>

    <AppTableCard>
      <template #header>
        <div class="monthly-report-table-head">
          <div>
            <div class="monthly-report-table-title">月报明细</div>
            <div class="monthly-report-table-note">保留原有编辑、提审、审核和驳回链路，首屏先聚焦当前范围内的待处理记录。</div>
          </div>
          <el-tag size="small" effect="light">{{ activeMonthlyReportFilterLabel }}</el-tag>
        </div>
      </template>

      <el-table
        :data="tableData"
        v-loading="loading"
        stripe
        max-height="520"
        scrollbar-always-on
        empty-text="暂无符合条件的数据"
        :row-class-name="rowClassName"
        @row-dblclick="onRowDoubleClick"
      >
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
                icon="Edit"
              >
                {{ canEditReport(scope.row) ? '编辑' : '查看' }}
              </el-button>
              <el-button
                v-if="canSubmitReport(scope.row)"
                type="warning"
                link
                :loading="isWorkflowLoading(scope.row.id, 'submit')"
                :disabled="submitLoading || deletingId === scope.row.id || !!workflowLoadingKey"
                @click="onSubmitForApproval(scope.row)"
                icon="Upload"
              >
                提交
              </el-button>
              <el-button
                v-if="canApproveReport(scope.row)"
                type="success"
                link
                :loading="isWorkflowLoading(scope.row.id, 'approve')"
                :disabled="submitLoading || deletingId === scope.row.id || !!workflowLoadingKey"
                @click="onApproveReport(scope.row)"
                icon="Check"
              >
                通过
              </el-button>
              <el-button
                v-if="canRejectReport(scope.row)"
                type="danger"
                link
                :loading="isWorkflowLoading(scope.row.id, 'reject')"
                :disabled="submitLoading || deletingId === scope.row.id || !!workflowLoadingKey"
                @click="onRejectReport(scope.row)"
                icon="Close"
              >
                驳回
              </el-button>
              <el-button
                v-if="canDeleteReport(scope.row)"
                type="danger"
                link
                :loading="deletingId === scope.row.id"
                :disabled="submitLoading || deletingId === scope.row.id || !!workflowLoadingKey"
                @click="onDelete(scope.row)"
                icon="Delete"
              >
                删除
              </el-button>
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
        <el-button :disabled="submitLoading || !!workflowLoadingKey" @click="dialogVisible = false" icon="Close">
          {{ canManage ? '取消' : '关闭' }}
        </el-button>
        <el-button
          v-if="canSubmitCurrentReport"
          type="warning"
          :loading="isWorkflowLoading(editingId || 0, 'submit')"
          :disabled="submitLoading || !!workflowLoadingKey"
          @click="onSubmitForApproval()"
          icon="Upload"
        >
          保存并提交
        </el-button>
        <el-button
          v-if="canApproveCurrentReport"
          type="success"
          :loading="isWorkflowLoading(editingId || 0, 'approve')"
          :disabled="submitLoading || !!workflowLoadingKey"
          @click="onApproveReport()"
          icon="Check"
        >
          审核通过
        </el-button>
        <el-button
          v-if="canRejectCurrentReport"
          type="danger"
          :loading="isWorkflowLoading(editingId || 0, 'reject')"
          :disabled="submitLoading || !!workflowLoadingKey"
          @click="onRejectReport()"
          icon="Close"
        >
          驳回
        </el-button>
        <el-button
          v-if="canSaveForm"
          type="primary"
          :loading="submitLoading"
          :disabled="submitLoading || !!workflowLoadingKey"
          @click="onSubmit"
        >
          {{ editingId ? '保存修改' : '创建月报' }}
        </el-button>
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
import type { MonthlyReportItem, MonthlyReportQuery, MonthlyReportUpsert } from '../../types/monthly-report'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { resolveMonthlyReportStatusTag } from '../../utils/statusTag'
import AppFilterCard from '../../components/AppFilterCard.vue'
import AppTableCard from '../../components/AppTableCard.vue'
import AppFormDialog from '../../components/AppFormDialog.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'

const access = useAccessControl()
const route = useRoute()
const router = useRouter()
const { runInitialLoad } = useResilientLoad()

const canManage = computed(() => access.canPermission('monthly-report.manage'))
const loading = ref(false)
const submitLoading = ref(false)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const deletingId = ref<number | null>(null)
const workflowLoadingKey = ref('')
const total = ref(0)
const tableData = ref<MonthlyReportItem[]>([])
const allRows = ref<MonthlyReportItem[]>([])
const accessibleHospitals = ref<string[]>([])
const queryMonth = ref('')
const formRef = ref<FormInstance>()

const currentMonth = computed(() => {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
})

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
  if (access.accessProfile.value?.isAdmin) return true
  if (access.accessProfile.value?.dataScope?.scopeType === 'all') return true
  return access.isManager()
})

const currentItem = computed<MonthlyReportItem | null>(() => {
  if (!editingId.value) return null
  return tableData.value.find((item) => item.id === editingId.value) ?? allRows.value.find((item) => item.id === editingId.value) ?? null
})

const dialogTitle = computed(() => {
  if (!editingId.value) return '新增月报'
  return canEditCurrentReport.value ? '编辑月报' : '月报详情'
})

const isDraftOrRejected = (status: string) => status === 'draft' || status === 'rejected'
const canEditReport = (row: MonthlyReportItem) => canManage.value && isDraftOrRejected(row.status)
const canDeleteReport = (row: MonthlyReportItem) => canEditReport(row)
const canSubmitReport = (row: MonthlyReportItem) => canManage.value && isDraftOrRejected(row.status)
const canApproveReport = (row: MonthlyReportItem) => canReview.value && row.status === 'submitted'
const canRejectReport = (row: MonthlyReportItem) => canReview.value && row.status === 'submitted'

const canEditCurrentReport = computed(() => {
  if (!currentItem.value) return canManage.value
  return canEditReport(currentItem.value)
})

const canSubmitCurrentReport = computed(() => Boolean(currentItem.value && canSubmitReport(currentItem.value)))
const canApproveCurrentReport = computed(() => Boolean(currentItem.value && canApproveReport(currentItem.value)))
const canRejectCurrentReport = computed(() => Boolean(currentItem.value && canRejectReport(currentItem.value)))
const canSaveForm = computed(() => canManage.value && (!editingId.value || canEditCurrentReport.value))
const isFormReadOnly = computed(() => (!editingId.value ? !canManage.value : !canEditCurrentReport.value))

type MonthlyReportWorkflowAction = 'submit' | 'approve' | 'reject'
type MonthlyReportSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active: boolean
}
type MonthlyReportFilterState = {
  hospitalName: string
  reportMonth: string
  submittedBy: string
  status: string
  page: number
  size: number
}
type InsightBucket = {
  label: string
  value: number
}

const buildWorkflowKey = (id: number, action: MonthlyReportWorkflowAction) => `${id}:${action}`
const isWorkflowLoading = (id: number, action: MonthlyReportWorkflowAction) => Boolean(id) && workflowLoadingKey.value === buildWorkflowKey(id, action)

const readRouteQueryValue = (value: unknown): string => {
  if (typeof value === 'string') return value
  if (Array.isArray(value) && typeof value[0] === 'string') return value[0]
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
  if (!readRouteQueryValue(route.query.action) && !readRouteQueryValue(route.query.id)) return
  await updateRouteQuery({ action: undefined, id: undefined })
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
    await refreshDesk()
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

const matchesFilteredRow = (item: MonthlyReportItem) => {
  if (query.hospitalName && item.hospitalName !== query.hospitalName) return false
  if (query.reportMonth && item.reportMonth !== query.reportMonth) return false
  if (query.status && item.status !== query.status) return false
  if (query.submittedBy && !String(item.submittedBy || '').includes(query.submittedBy)) return false
  return true
}

const filteredRows = computed(() => allRows.value.filter(matchesFilteredRow))
const currentMonthRows = computed(() => filteredRows.value.filter((item) => item.reportMonth === currentMonth.value))
const reviewQueue = computed(() =>
  filteredRows.value
    .filter((item) => item.status === 'submitted' || item.status === 'rejected')
    .slice()
    .sort((left, right) => {
      if (left.status !== right.status) return left.status === 'submitted' ? -1 : 1
      return String(right.updatedAt).localeCompare(String(left.updatedAt))
    })
    .slice(0, 6),
)

const buildTopBuckets = (items: MonthlyReportItem[], selector: (item: MonthlyReportItem) => string, limit = 6): InsightBucket[] => {
  const counter = new Map<string, number>()
  for (const item of items) {
    const key = selector(item)?.trim() || '未填写'
    counter.set(key, (counter.get(key) ?? 0) + 1)
  }
  return Array.from(counter.entries())
    .map(([label, value]) => ({ label, value }))
    .sort((left, right) => right.value - left.value || left.label.localeCompare(right.label, 'zh-CN'))
    .slice(0, limit)
}

const topHospitalBuckets = computed(() => buildTopBuckets(filteredRows.value, (item) => item.hospitalName || '未填写医院'))
const topSubmitterBuckets = computed(() => buildTopBuckets(filteredRows.value, (item) => item.submittedBy || '未填写提交人'))
const statusBuckets = computed(() => buildTopBuckets(filteredRows.value, (item) => statusLabel(item.status), 4))
const activeSubmitterCount = computed(() => new Set(filteredRows.value.map((item) => item.submittedBy).filter(Boolean)).size)

const activeMonthlyReportFilterLabel = computed(() => {
  const labels: string[] = []
  if (query.hospitalName) labels.push(query.hospitalName)
  if (query.reportMonth) labels.push(query.reportMonth)
  if (query.status) labels.push(statusLabel(query.status))
  if (query.submittedBy) labels.push(`提交:${query.submittedBy}`)
  return labels.length ? labels.join(' / ') : '当前全部范围'
})

const heroSignals = computed(() => [
  {
    label: '当前范围',
    value: filteredRows.value.length,
    note: '当前筛选后纳入工作台的月报总量',
  },
  {
    label: '待审核',
    value: filteredRows.value.filter((item) => item.status === 'submitted').length,
    note: '已经提交等待审核的月报',
  },
  {
    label: '已驳回',
    value: filteredRows.value.filter((item) => item.status === 'rejected').length,
    note: '需要补充修改后重新提交的月报',
  },
  {
    label: '当月记录',
    value: currentMonthRows.value.length,
    note: '当前月份范围内的月报记录',
  },
])

const summaryCards = computed<MonthlyReportSummaryCard[]>(() => [
  {
    key: 'draft',
    title: '草稿',
    value: filteredRows.value.filter((item) => item.status === 'draft').length,
    context: '月报状态',
    note: '查看尚未提交审核的草稿月报',
    color: '#64748b',
    active: query.status === 'draft',
  },
  {
    key: 'submitted',
    title: '已提交',
    value: filteredRows.value.filter((item) => item.status === 'submitted').length,
    context: '月报状态',
    note: '查看等待审核的月报',
    color: '#c7a06c',
    active: query.status === 'submitted',
  },
  {
    key: 'approved',
    title: '已审核',
    value: filteredRows.value.filter((item) => item.status === 'approved').length,
    context: '月报状态',
    note: '查看已经审核通过的月报',
    color: '#7d9f92',
    active: query.status === 'approved',
  },
  {
    key: 'rejected',
    title: '已驳回',
    value: filteredRows.value.filter((item) => item.status === 'rejected').length,
    context: '月报状态',
    note: '查看需要补充修改的月报',
    color: '#c58a87',
    active: query.status === 'rejected',
  },
  {
    key: 'all',
    title: '总数',
    value: filteredRows.value.length,
    context: '全量视图',
    note: '返回当前权限范围内的全部月报',
    color: '#3f4f63',
    active: !query.status,
  },
])

const quickActions = computed(() => [
  {
    title: '待审核',
    note: `${filteredRows.value.filter((item) => item.status === 'submitted').length} 份`,
    onClick: () => onStatClick('submitted'),
  },
  {
    title: '已驳回',
    note: `${filteredRows.value.filter((item) => item.status === 'rejected').length} 份`,
    onClick: () => onStatClick('rejected'),
  },
  {
    title: '当月月报',
    note: `${currentMonthRows.value.length} 份`,
    onClick: () => focusCurrentMonth(),
  },
  {
    title: '全部范围',
    note: `${filteredRows.value.length} 份`,
    onClick: () => onReset(),
  },
])

const loadData = async () => {
  loading.value = true
  try {
    const response = await fetchMonthlyReports(query)
    tableData.value = response.data.items
    total.value = response.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载月报失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const loadOverview = async () => {
  try {
    const response = await fetchMonthlyReports({ page: 1, size: 100000 } as MonthlyReportQuery)
    allRows.value = response.data.items
  } catch (error) {
    allRows.value = []
    ElMessage.error(getErrorMessage(error, '加载月报概览失败，请稍后重试'))
  }
}

const loadScope = async () => {
  try {
    const response = await fetchDataScope()
    accessibleHospitals.value = [...(response.data.accessibleHospitalNames || [])].sort((left, right) => left.localeCompare(right, 'zh-CN'))
  } catch (error) {
    accessibleHospitals.value = []
    ElMessage.error(getErrorMessage(error, '加载可访问医院范围失败，请稍后重试'))
  }
}

const refreshDesk = async () => {
  await Promise.allSettled([loadOverview(), loadData()])
}

const onMonthChange = (value: string | null) => {
  query.reportMonth = value || ''
}

const onSearch = () => {
  query.page = 1
  void refreshDesk()
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
  void refreshDesk()
}

const onStatClick = (status: string) => {
  query.status = status
  query.page = 1
  void refreshDesk()
}

const focusCurrentMonth = () => {
  query.reportMonth = currentMonth.value
  queryMonth.value = currentMonth.value
  query.page = 1
  void refreshDesk()
}

const onSummaryCardSelect = (card: { key?: string | number }) => {
  if (typeof card.key !== 'string') return
  if (card.key === 'all') {
    query.status = ''
  } else {
    query.status = card.key
  }
  query.page = 1
  void refreshDesk()
}

const onPageSizeChange = (size: number) => {
  query.size = size
  query.page = 1
  void loadData()
}

const onCurrentPageChange = (page: number) => {
  query.page = page
  void loadData()
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
  if (!action) return

  if (action === 'create') {
    if (canManage.value && !dialogVisible.value) openCreateDialog()
    return
  }

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) return

  const matched = tableData.value.find((item) => item.id === id) ?? allRows.value.find((item) => item.id === id)
  if (matched) openEditDialog(matched)
}

const syncEditingFormFromCache = () => {
  if (!editingId.value) return
  const matched = tableData.value.find((item) => item.id === editingId.value) ?? allRows.value.find((item) => item.id === editingId.value)
  if (!matched) return
  applyItemToForm(matched)
}

const validateForm = async () => {
  const valid = await formRef.value?.validate().then(() => true).catch(() => false)
  return Boolean(valid)
}

const persistForm = async () => {
  const valid = await validateForm()
  if (!valid) return null

  if (editingId.value) {
    const response = await updateMonthlyReport(editingId.value, { ...form })
    return response.data
  }

  const response = await createMonthlyReport({ ...form })
  return response.data
}

const refreshAfterMutation = async () => {
  notifyDataChanged('monthly-report')
  await refreshDesk()
  syncEditingFormFromCache()
}

const runWorkflowAction = async (id: number, action: MonthlyReportWorkflowAction, rejectionReason = '') => {
  workflowLoadingKey.value = buildWorkflowKey(id, action)
  try {
    if (action === 'submit') {
      await submitMonthlyReport(id)
      ElMessage.success('已提交审核')
    } else if (action === 'approve') {
      await approveMonthlyReport(id)
      ElMessage.success('已审核通过')
    } else {
      await rejectMonthlyReport(id, { rejectionReason })
      ElMessage.success('已驳回月报')
    }

    await refreshAfterMutation()
  } catch (error) {
    const fallback =
      action === 'submit'
        ? '提交审核失败，请稍后重试'
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
    if (!item) return

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
    if (!canSubmitReport(row)) return
    await runWorkflowAction(row.id, 'submit')
    return
  }

  if (!editingId.value || !canSubmitCurrentReport.value) return

  submitLoading.value = true
  try {
    const item = await persistForm()
    if (!item) return
    await runWorkflowAction(item.id, 'submit')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '提交审核失败，请稍后重试'))
  } finally {
    submitLoading.value = false
  }
}

const onApproveReport = async (row?: MonthlyReportItem) => {
  const target = row ?? currentItem.value
  if (!target || !canApproveReport(target)) return
  await runWorkflowAction(target.id, 'approve')
}

const onRejectReport = async (row?: MonthlyReportItem) => {
  const target = row ?? currentItem.value
  if (!target || !canRejectReport(target)) return

  const rejectionReason = await requestRejectReason()
  if (rejectionReason === null) return
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
    if (editingId.value === row.id) {
      dialogVisible.value = false
      editingId.value = null
    }
    await refreshAfterMutation()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '删除失败，请稍后重试'))
  } finally {
    deletingId.value = null
  }
}

const rowClassName = ({ row }: { row: MonthlyReportItem }) => (row.status === 'submitted' ? 'pending-row' : row.status === 'rejected' ? 'rejected-row' : '')

watch(dialogVisible, (visible) => {
  if (!visible) void clearRouteActionQuery()
})

watch(
  () => route.fullPath,
  () => {
    syncDialogFromRoute()
  },
)

onMounted(async () => {
  filterPersist.restore()

  await access.ensureAccessProfileLoaded()
  await runInitialLoad({
    tasks: [loadScope, loadOverview, loadData],
  })

  syncDialogFromRoute()
})
</script>

<style scoped>
.monthly-report-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.65fr) minmax(300px, 0.95fr);
  gap: 18px;
  margin-bottom: 18px;
}

.monthly-report-hero-main,
.monthly-report-control-card,
.monthly-report-insight-card,
.monthly-report-quick-action,
.monthly-report-summary-wrap {
  border: 1px solid var(--pms-border, #e5e7eb);
  border-radius: 8px;
  background: #fff;
  box-shadow: var(--pms-shadow-card, 0 1px 2px rgba(15, 23, 42, 0.04));
}

.monthly-report-hero-main {
  padding: 22px 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.monthly-report-hero-kicker-row,
.monthly-report-control-copy,
.monthly-report-insight-head,
.monthly-report-table-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.monthly-report-hero-kicker {
  font-size: 12px;
  line-height: 18px;
  font-weight: 700;
  color: #2563eb;
  text-transform: uppercase;
}

.monthly-report-hero-badge {
  display: inline-flex;
  align-items: center;
  min-height: 24px;
  padding: 0 10px;
  border-radius: 999px;
  background: #eff6ff;
  color: #1d4ed8;
  font-size: 12px;
  line-height: 18px;
}

.monthly-report-hero-title {
  margin: 0;
  font-size: 30px;
  line-height: 1.2;
  font-weight: 700;
  color: #0f172a;
}

.monthly-report-hero-subtitle {
  font-size: 14px;
  line-height: 1.7;
  color: #475569;
  max-width: 780px;
}

.monthly-report-hero-signals {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.monthly-report-signal-card {
  min-height: 112px;
  padding: 14px 16px;
  border-radius: 8px;
  background: linear-gradient(180deg, #f8fbff 0%, #ffffff 100%);
  border: 1px solid #e2e8f0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.monthly-report-signal-label {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.monthly-report-signal-value {
  font-size: 30px;
  line-height: 1.1;
  font-weight: 700;
  color: #0f172a;
}

.monthly-report-signal-note {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.monthly-report-hero-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.monthly-report-control-card {
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.monthly-report-control-copy {
  flex-direction: column;
  align-items: flex-start;
  justify-content: flex-start;
  gap: 6px;
}

.monthly-report-control-title,
.monthly-report-insight-title,
.monthly-report-table-title {
  font-size: 16px;
  line-height: 24px;
  font-weight: 700;
  color: #0f172a;
}

.monthly-report-control-note,
.monthly-report-insight-note,
.monthly-report-table-note {
  font-size: 13px;
  line-height: 20px;
  color: #64748b;
}

.monthly-report-control-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.monthly-report-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.monthly-report-quick-action {
  appearance: none;
  padding: 16px;
  text-align: left;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}

.monthly-report-quick-action:hover {
  border-color: #bfdbfe;
  box-shadow: 0 8px 20px rgba(37, 99, 235, 0.08);
  transform: translateY(-1px);
}

.monthly-report-quick-title {
  display: block;
  font-size: 14px;
  line-height: 20px;
  font-weight: 700;
  color: #0f172a;
}

.monthly-report-quick-note {
  display: block;
  margin-top: 6px;
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.monthly-report-insight-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 18px;
}

.monthly-report-insight-card {
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 236px;
}

.monthly-report-insight-meta {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
  white-space: nowrap;
}

.monthly-report-queue-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.monthly-report-queue-item {
  appearance: none;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: #f8fafc;
  padding: 12px;
  display: flex;
  justify-content: space-between;
  gap: 12px;
  text-align: left;
  cursor: pointer;
  transition: border-color 0.2s ease, background-color 0.2s ease;
}

.monthly-report-queue-item:hover {
  border-color: #bfdbfe;
  background: #f8fbff;
}

.monthly-report-queue-main,
.monthly-report-queue-meta {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.monthly-report-queue-main strong,
.monthly-report-chip strong {
  font-size: 13px;
  line-height: 20px;
  font-weight: 700;
  color: #0f172a;
}

.monthly-report-queue-main span,
.monthly-report-queue-meta span,
.monthly-report-chip span {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.monthly-report-chip-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.monthly-report-chip {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 12px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  background: #fff;
}

.monthly-report-summary-wrap {
  padding: 16px;
  margin-bottom: 18px;
}

.monthly-report-table-head {
  margin-bottom: 6px;
}

.report-workflow-panel {
  margin-bottom: 18px;
  padding: 16px 18px;
  border-radius: 12px;
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

:deep(.table-action-group) {
  flex-wrap: nowrap;
}

:deep(.pending-row) {
  background-color: #fff7ed !important;
}

:deep(.pending-row:hover > td) {
  background-color: #ffedd5 !important;
}

:deep(.rejected-row) {
  background-color: #fef2f2 !important;
}

:deep(.rejected-row:hover > td) {
  background-color: #fee2e2 !important;
}

@media (max-width: 1440px) {
  .monthly-report-hero {
    grid-template-columns: 1fr;
  }

  .monthly-report-insight-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 992px) {
  .monthly-report-hero-main,
  .monthly-report-control-card,
  .monthly-report-insight-card,
  .monthly-report-summary-wrap {
    padding: 16px;
  }

  .monthly-report-hero-signals,
  .monthly-report-insight-grid {
    grid-template-columns: 1fr;
  }

  .monthly-report-quick-grid {
    grid-template-columns: 1fr;
  }

  .monthly-report-hero-kicker-row,
  .monthly-report-insight-head,
  .monthly-report-table-head,
  .report-workflow-head {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
