<template>
  <div class="page-shell">
    <div class="workhours-hero">
      <div class="workhours-hero-main">
        <div class="workhours-hero-kicker-row">
          <span class="workhours-hero-kicker">Work Hours Desk</span>
          <span class="workhours-hero-badge">{{ activeWorkHoursFilterLabel }}</span>
        </div>
        <h2 class="workhours-hero-title">工时管理</h2>
        <div class="workhours-hero-subtitle">
          统一查看当前筛选范围内的工时登记、审批状态、人员投入和医院覆盖，直接从第一屏完成录入、核查和审批推进。
        </div>
        <div class="workhours-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="workhours-signal-card">
            <span class="workhours-signal-label">{{ item.label }}</span>
            <strong class="workhours-signal-value">{{ item.value }}</strong>
            <span class="workhours-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>
      <div class="workhours-hero-side">
        <div class="workhours-control-card">
          <div class="workhours-control-copy">
            <span class="workhours-control-title">工时台动作</span>
            <span class="workhours-control-note">先锁定人员、医院或工时类型，再从待确认清单进入详情处理。</span>
          </div>
          <div class="workhours-control-actions">
            <el-button size="small" :loading="loading || overviewLoading" @click="refreshDesk" icon="Refresh">刷新</el-button>
            <el-button v-if="canManage" size="small" type="primary" @click="onOpenCreate" icon="Plus">登记工时</el-button>
            <el-button size="small" :loading="exporting" @click="onExport" icon="Download">导出</el-button>
          </div>
        </div>
        <div class="workhours-quick-grid">
          <button
            v-for="action in quickActions"
            :key="action.title"
            type="button"
            class="workhours-quick-action"
            @click="action.onClick()"
          >
            <span class="workhours-quick-title">{{ action.title }}</span>
            <span class="workhours-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <div class="workhours-insight-grid">
      <section ref="approvalQueueRef" class="workhours-insight-card">
        <div class="workhours-insight-head">
          <div>
            <div class="workhours-insight-title">待确认与退回清单</div>
            <div class="workhours-insight-note">优先处理待确认和已退回记录，避免月底集中补审或重复修改。</div>
          </div>
          <el-tag size="small" type="warning" effect="light">{{ reviewQueue.length }} 项</el-tag>
        </div>
        <div v-if="reviewQueue.length" class="workhours-queue-list">
          <button
            v-for="item in reviewQueue"
            :key="item.id"
            type="button"
            class="workhours-queue-item"
            @click="onOpenDetail(item.id)"
          >
            <div class="workhours-queue-main">
              <strong>{{ item.personnelName || '未填写人员' }}</strong>
              <span>{{ item.hospitalName || '未填写医院' }} · {{ item.workType || '未填写类型' }}</span>
            </div>
            <div class="workhours-queue-meta">
              <el-tag size="small" :type="statusTag(item.status)">{{ statusLabel(item.status) }}</el-tag>
              <span>{{ item.hours }} h</span>
            </div>
          </button>
        </div>
        <el-empty v-else description="当前筛选下没有待确认或退回记录" :image-size="72" />
      </section>

      <section class="workhours-insight-card">
        <div class="workhours-insight-head">
          <div>
            <div class="workhours-insight-title">人员投入排行</div>
            <div class="workhours-insight-note">按工时总量统计当前筛选范围内的主要投入人，方便看负载是否均衡。</div>
          </div>
          <span class="workhours-insight-meta">{{ overviewScopeLabel }}</span>
        </div>
        <div v-if="topPersonnelBuckets.length" class="workhours-chip-list">
          <div v-for="item in topPersonnelBuckets" :key="item.label" class="workhours-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value.toFixed(1) }} h</span>
          </div>
        </div>
        <el-empty v-else description="暂无人员投入分布" :image-size="72" />
      </section>

      <section class="workhours-insight-card">
        <div class="workhours-insight-head">
          <div>
            <div class="workhours-insight-title">医院投入排行</div>
            <div class="workhours-insight-note">看哪些医院当前消耗工时最多，便于复盘支持强度和服务方式。</div>
          </div>
          <span class="workhours-insight-meta">{{ activePersonnelCount }} 人参与</span>
        </div>
        <div v-if="topHospitalBuckets.length" class="workhours-chip-list">
          <div v-for="item in topHospitalBuckets" :key="item.label" class="workhours-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value.toFixed(1) }} h</span>
          </div>
        </div>
        <el-empty v-else description="暂无医院投入分布" :image-size="72" />
      </section>

      <section class="workhours-insight-card">
        <div class="workhours-insight-head">
          <div>
            <div class="workhours-insight-title">工时类型分布</div>
            <div class="workhours-insight-note">从类型结构判断当前工时主要花在现场、远程还是出差任务上。</div>
          </div>
          <span class="workhours-insight-meta" v-if="overviewTruncated">仅统计前 {{ overviewRows.length }} 条</span>
        </div>
        <div v-if="topTypeBuckets.length" class="workhours-chip-list">
          <div v-for="item in topTypeBuckets" :key="item.label" class="workhours-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value.toFixed(1) }} h</span>
          </div>
        </div>
        <el-empty v-else description="暂无工时类型分布" :image-size="72" />
      </section>
    </div>

    <div class="workhours-summary-wrap">
      <SummaryMetrics :items="summaryCards" :columns="5" @select="onSummaryCardSelect" />
    </div>

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
        <el-table-column prop="productName" label="产品名称" min-width="160" show-overflow-tooltip />
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
        <el-descriptions-item label="产品名称">{{ detailItem.productName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="机会号">{{ detailItem.opportunityNumber || '-' }}</el-descriptions-item>
        <el-descriptions-item label="工作日期">{{ detailItem.workDate || '-' }}</el-descriptions-item>
        <el-descriptions-item label="工时(h)">{{ detailItem.hours ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="工作类型">{{ detailItem.workType || '-' }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="statusTag(detailItem.status)">{{ statusLabel(detailItem.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="实施状态">{{ detailItem.implementationStatus || '-' }}</el-descriptions-item>
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
const overviewLoading = ref(false)
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
const overviewRows = ref<WorkHoursItem[]>([])
const overviewTotal = ref(0)
const accessibleHospitals = ref<string[]>([])
const dateRange = ref<[string, string] | null>(null)
const detailItem = ref<WorkHoursItem | null>(null)
const summary = ref<WorkHoursSummary>({ total: 0, totalHours: 0, onsiteCount: 0, remoteCount: 0, travelCount: 0 })
const workTypeOptions = ['驻场', '远程', '出差', '病假', '事假', '其他特殊'] as const
const workflowBusy = computed(() => workflowLoadingKey.value.length > 0)
const approvalQueueRef = ref<HTMLElement | null>(null)

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

type WorkHoursHeroSignal = {
  label: string
  value: string
  note: string
}

type WorkHoursBucket = {
  label: string
  value: number
}

type WorkHoursQuickAction = {
  title: string
  note: string
  onClick: () => void | Promise<void>
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

const OVERVIEW_PAGE_SIZE = 500

const sumHoursBy = (
  items: WorkHoursItem[],
  getLabel: (item: WorkHoursItem) => string,
  limit = 5,
) => {
  const counter = new Map<string, number>()
  items.forEach((item) => {
    const label = getLabel(item).trim() || '未填写'
    const hours = typeof item.hours === 'number' && Number.isFinite(item.hours) ? item.hours : 0
    counter.set(label, (counter.get(label) ?? 0) + hours)
  })

  return [...counter.entries()]
    .map(([label, value]) => ({ label, value }))
    .sort((left, right) => right.value - left.value || left.label.localeCompare(right.label, 'zh-CN'))
    .slice(0, limit)
}

const overviewTruncated = computed(() => overviewTotal.value > overviewRows.value.length)

const totalOverviewHours = computed(() =>
  overviewRows.value.reduce((totalHours, item) => totalHours + (Number.isFinite(item.hours) ? item.hours : 0), 0))

const submittedRows = computed(() => overviewRows.value.filter((item) => item.status === 'submitted'))

const rejectedRows = computed(() => overviewRows.value.filter((item) => item.status === 'rejected'))

const activePersonnelCount = computed(() => new Set(overviewRows.value.map((item) => item.personnelName).filter(Boolean)).size)

const reviewQueue = computed(() =>
  [...overviewRows.value]
    .filter((item) => item.status === 'submitted' || item.status === 'rejected')
    .sort((left, right) => {
      const leftPriority = left.status === 'submitted' ? 0 : 1
      const rightPriority = right.status === 'submitted' ? 0 : 1
      if (leftPriority !== rightPriority) {
        return leftPriority - rightPriority
      }

      const leftTime = new Date(left.workDate || left.createdAt || '').getTime()
      const rightTime = new Date(right.workDate || right.createdAt || '').getTime()
      return (Number.isFinite(rightTime) ? rightTime : 0) - (Number.isFinite(leftTime) ? leftTime : 0)
    })
    .slice(0, 6))

const activeWorkHoursFilterLabel = computed(() => {
  const labels = [
    query.personnelName ? `人员：${query.personnelName}` : '',
    query.hospitalName ? `医院：${query.hospitalName}` : '',
    query.workType ? `类型：${query.workType}` : '',
    dateRange.value?.length ? `${dateRange.value[0]} 至 ${dateRange.value[1]}` : '',
  ].filter(Boolean)

  return labels.length ? labels.join(' / ') : '全部工时'
})

const overviewScopeLabel = computed(() => {
  if (!overviewRows.value.length) {
    return '当前无数据'
  }

  return overviewTruncated.value
    ? `展示 ${overviewRows.value.length} / ${overviewTotal.value}`
    : `共 ${overviewRows.value.length} 条`
})

const heroSignals = computed<WorkHoursHeroSignal[]>(() => [
  {
    label: '当前工时',
    value: totalOverviewHours.value.toFixed(1),
    note: totalOverviewHours.value ? '当前筛选范围内累计登记工时' : '当前筛选下暂无工时登记',
  },
  {
    label: '待确认',
    value: String(submittedRows.value.length),
    note: submittedRows.value.length ? '已提交审批、待主管确认的记录' : '当前没有待确认记录',
  },
  {
    label: '已退回',
    value: String(rejectedRows.value.length),
    note: rejectedRows.value.length ? '需要补充或修改后重新提交' : '当前没有退回记录',
  },
  {
    label: '参与人员',
    value: String(activePersonnelCount.value),
    note: activePersonnelCount.value ? '当前筛选下有工时登记的人员数量' : '当前暂无人员投入',
  },
])

const topPersonnelBuckets = computed<WorkHoursBucket[]>(() =>
  sumHoursBy(overviewRows.value, (item) => item.personnelName || '未填写人员'))

const topHospitalBuckets = computed<WorkHoursBucket[]>(() =>
  sumHoursBy(overviewRows.value, (item) => item.hospitalName || '未填写医院'))

const topTypeBuckets = computed<WorkHoursBucket[]>(() =>
  sumHoursBy(overviewRows.value, (item) => item.workType || '未填写类型'))

const quickActions = computed<WorkHoursQuickAction[]>(() => [
  {
    title: submittedRows.value.length ? '看待确认清单' : rejectedRows.value.length ? '看退回清单' : '看审批清单',
    note: submittedRows.value.length
      ? `${submittedRows.value.length} 条待主管确认`
      : rejectedRows.value.length
        ? `${rejectedRows.value.length} 条已退回待修改`
        : '当前没有待确认或退回记录',
    onClick: () => {
      approvalQueueRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
    },
  },
  {
    title: '只看驻场',
    note: `${summary.value.onsiteCount} 条驻场工时`,
    onClick: () => onTypeClick('驻场'),
  },
  {
    title: '只看远程',
    note: `${summary.value.remoteCount} 条远程工时`,
    onClick: () => onTypeClick('远程'),
  },
  canManage.value
    ? {
        title: '登记今日工时',
        note: '直接录入当天工时，减少月底集中补录',
        onClick: () => onOpenCreate(),
      }
    : {
        title: '回到全部工时',
        note: '清空筛选后回到完整工时台视图',
        onClick: () => onTypeClick(''),
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
    await Promise.allSettled([loadSummary(), loadOverview(), loadData()])
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
    const res = await fetchWorkHoursSummary({
      personnelName: query.personnelName || undefined,
      hospitalName: query.hospitalName || undefined,
      workType: query.workType || undefined,
      workDateFrom: dateRange.value?.[0] || undefined,
      workDateTo: dateRange.value?.[1] || undefined,
    })
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

const loadOverview = async () => {
  overviewLoading.value = true
  try {
    const res = await fetchWorkHours({
      personnelName: query.personnelName || undefined,
      hospitalName: query.hospitalName || undefined,
      workType: query.workType || undefined,
      workDateFrom: dateRange.value?.[0] || undefined,
      workDateTo: dateRange.value?.[1] || undefined,
      page: 1,
      size: OVERVIEW_PAGE_SIZE,
    })
    overviewRows.value = res.data.items
    overviewTotal.value = res.data.total
  } catch (error) {
    overviewRows.value = []
    overviewTotal.value = 0
    ElMessage.error(getErrorMessage(error, '加载工时台概览失败，请稍后重试'))
  } finally {
    overviewLoading.value = false
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

let skipNextRouteRefresh = false

const syncFilterRoute = async () => {
  const nextPersonnelName = query.personnelName || ''
  const nextHospitalName = query.hospitalName || ''
  const nextWorkType = query.workType || ''
  const nextDateFrom = dateRange.value?.[0] || ''
  const nextDateTo = dateRange.value?.[1] || ''
  const routeWillChange = readRouteQueryValue(route.query.personnelName) !== nextPersonnelName
    || readRouteQueryValue(route.query.hospitalName) !== nextHospitalName
    || readRouteQueryValue(route.query.workType) !== nextWorkType
    || readRouteQueryValue(route.query.dateFrom) !== nextDateFrom
    || readRouteQueryValue(route.query.dateTo) !== nextDateTo
    || Boolean(readRouteQueryValue(route.query.action))
    || Boolean(readRouteQueryValue(route.query.id))

  if (routeWillChange) {
    skipNextRouteRefresh = true
  }

  await updateRouteQuery({
    personnelName: nextPersonnelName || undefined,
    hospitalName: nextHospitalName || undefined,
    workType: nextWorkType || undefined,
    dateFrom: nextDateFrom || undefined,
    dateTo: nextDateTo || undefined,
    action: undefined,
    id: undefined,
  })
}

const loadDeskData = async () => {
  await Promise.allSettled([loadSummary(), loadOverview(), loadData()])
}

const refreshDesk = async () => {
  await loadDeskData()
}

const onSearch = async () => {
  query.page = 1
  await syncFilterRoute()
  await loadDeskData()
}

const onReset = async () => {
  query.personnelName = ''
  query.hospitalName = ''
  query.workType = ''
  dateRange.value = null
  query.page = 1
  query.size = 15
  filterPersist.clear()
  await syncFilterRoute()
  await loadDeskData()
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

const onTypeClick = async (workType: string) => {
  query.workType = query.workType === workType ? '' : workType
  query.page = 1
  await syncFilterRoute()
  await loadDeskData()
}

const onSummaryCardSelect = (card: { key?: string | number; clickable?: boolean }) => {
  if (card.clickable === false || typeof card.key !== 'string') {
    return
  }

  void onTypeClick(card.key)
}

const applyRouteFilters = () => {
  const personnelName = readRouteQueryValue(route.query.personnelName)
  const hospitalName = readRouteQueryValue(route.query.hospitalName)
  const workType = readRouteQueryValue(route.query.workType)
  const dateFrom = readRouteQueryValue(route.query.dateFrom)
  const dateTo = readRouteQueryValue(route.query.dateTo)
  const nextWorkType = workTypeOptions.includes(workType as (typeof workTypeOptions)[number]) ? workType : ''
  const nextDateRange = dateFrom && dateTo ? [dateFrom, dateTo] as [string, string] : null
  const changed = query.personnelName !== personnelName
    || query.hospitalName !== hospitalName
    || query.workType !== nextWorkType
    || (dateRange.value?.[0] ?? '') !== (nextDateRange?.[0] ?? '')
    || (dateRange.value?.[1] ?? '') !== (nextDateRange?.[1] ?? '')

  query.personnelName = personnelName
  query.hospitalName = hospitalName
  query.workType = nextWorkType
  dateRange.value = nextDateRange

  if (changed) {
    query.page = 1
  }

  return changed
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
    if (action === 'detail' && !detailVisible.value) {
      const id = Number(readRouteQueryValue(route.query.id))
      if (Number.isFinite(id) && id > 0) {
        await onOpenDetail(id, false)
      }
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
    return
  }

  try {
    const res = await fetchWorkHoursById(id)
    openEditDialog(res.data)
  } catch {
  }
}

const onOpenDetail = async (id: number, syncRoute = true) => {
  detailLoadingId.value = id
  try {
    const res = await fetchWorkHoursById(id)
    detailItem.value = res.data
    detailVisible.value = true
    if (syncRoute) {
      void updateRouteQuery({ action: 'detail', id: String(id) })
    }
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
    await Promise.all([loadData(), loadSummary(), loadOverview()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存失败，请稍后重试'))
  } finally {
    submitLoading.value = false
  }
}

const refreshAfterMutation = async () => {
  notifyDataChanged('workhours')
  await Promise.all([loadData(), loadSummary(), loadOverview()])
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
    await Promise.all([loadData(), loadSummary(), loadOverview()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '删除失败，请稍后重试'))
  } finally {
    deletingId.value = null
  }
}

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
  const filtersChanged = applyRouteFilters()

  if (skipNextRouteRefresh) {
    skipNextRouteRefresh = false
    void syncDialogFromRoute()
    return
  }

  if (filtersChanged) {
    void loadDeskData()
  }

  void syncDialogFromRoute()
})

onMounted(async () => {
  const restored = filterPersist.restore()
  applyRouteFilters()

  await runInitialLoad({
    tasks: [loadScope, loadSummary, loadOverview, loadData],
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
.workhours-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.85fr) minmax(300px, 0.95fr);
  gap: 20px;
  margin-bottom: 20px;
}

.workhours-hero-main,
.workhours-hero-side,
.workhours-insight-card,
.workhours-summary-wrap {
  border: 1px solid #e5ebf3;
  border-radius: 20px;
  background: linear-gradient(180deg, #ffffff 0%, #f8fbff 100%);
  box-shadow: 0 16px 34px rgba(15, 23, 42, 0.05);
}

.workhours-hero-main {
  padding: 28px 30px;
}

.workhours-hero-kicker-row,
.workhours-insight-head,
.workhours-control-card {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.workhours-hero-kicker {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #5d6b7a;
}

.workhours-hero-badge,
.workhours-insight-meta {
  display: inline-flex;
  align-items: center;
  min-height: 28px;
  padding: 0 12px;
  border-radius: 999px;
  background: #edf4ff;
  color: #335d98;
  font-size: 12px;
  font-weight: 600;
}

.workhours-hero-title {
  margin: 14px 0 8px;
  font-size: 30px;
  line-height: 1.1;
  color: #172233;
}

.workhours-hero-subtitle {
  max-width: 760px;
  font-size: 14px;
  line-height: 1.7;
  color: #5d6b7a;
}

.workhours-hero-signals {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
  margin-top: 22px;
}

.workhours-signal-card {
  display: flex;
  min-height: 116px;
  flex-direction: column;
  justify-content: space-between;
  padding: 16px 18px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.88);
  border: 1px solid #e7edf5;
}

.workhours-signal-label,
.workhours-control-title,
.workhours-insight-title {
  font-size: 13px;
  font-weight: 600;
  color: #506072;
}

.workhours-signal-value {
  font-size: 28px;
  line-height: 1;
  color: #172233;
}

.workhours-signal-note,
.workhours-control-note,
.workhours-insight-note,
.workhours-quick-note,
.workhours-queue-main span,
.workhours-chip span,
.workhours-queue-meta span {
  font-size: 12px;
  line-height: 1.6;
  color: #6f7d8c;
}

.workhours-hero-side {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 22px;
}

.workhours-control-card {
  padding: 18px;
  border-radius: 16px;
  background: #f5f8fd;
  border: 1px solid #e3ebf5;
}

.workhours-control-copy {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.workhours-control-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 10px;
}

.workhours-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.workhours-quick-action {
  display: flex;
  min-height: 112px;
  flex-direction: column;
  justify-content: space-between;
  gap: 12px;
  padding: 16px;
  border: 1px solid #e6edf6;
  border-radius: 16px;
  background: #ffffff;
  text-align: left;
  cursor: pointer;
  transition: transform 0.18s ease, box-shadow 0.18s ease, border-color 0.18s ease;
}

.workhours-quick-action:hover {
  transform: translateY(-1px);
  border-color: #cdd9e7;
  box-shadow: 0 12px 24px rgba(15, 23, 42, 0.08);
}

.workhours-quick-title,
.workhours-queue-main strong,
.workhours-chip strong {
  font-size: 14px;
  font-weight: 600;
  color: #172233;
}

.workhours-insight-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 18px;
}

.workhours-insight-card {
  display: flex;
  min-height: 248px;
  flex-direction: column;
  gap: 16px;
  padding: 20px;
}

.workhours-queue-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.workhours-queue-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px;
  border-radius: 14px;
  border: 1px solid #e7edf5;
  background: #ffffff;
  cursor: pointer;
  text-align: left;
  transition: border-color 0.18s ease, box-shadow 0.18s ease;
}

.workhours-queue-item:hover {
  border-color: #d5e0ed;
  box-shadow: 0 10px 22px rgba(15, 23, 42, 0.06);
}

.workhours-queue-main {
  display: flex;
  min-width: 0;
  flex: 1;
  flex-direction: column;
  gap: 4px;
}

.workhours-queue-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
}

.workhours-chip-list {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.workhours-chip {
  display: inline-flex;
  min-width: 132px;
  flex: 1 1 0;
  flex-direction: column;
  gap: 4px;
  padding: 14px 16px;
  border-radius: 14px;
  border: 1px solid #e7edf5;
  background: #ffffff;
}

.workhours-summary-wrap {
  padding: 16px;
  margin-bottom: 18px;
}

.table-action-group {
  flex-wrap: nowrap;
}

@media (max-width: 1360px) {
  .workhours-hero {
    grid-template-columns: 1fr;
  }

  .workhours-insight-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .workhours-hero-signals {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 900px) {
  .workhours-insight-grid,
  .workhours-hero-signals,
  .workhours-quick-grid {
    grid-template-columns: 1fr;
  }

  .workhours-hero-main,
  .workhours-hero-side,
  .workhours-insight-card,
  .workhours-summary-wrap {
    border-radius: 16px;
  }

  .workhours-hero-main {
    padding: 22px;
  }

  .workhours-hero-title {
    font-size: 24px;
  }

  .workhours-hero-kicker-row,
  .workhours-insight-head,
  .workhours-control-card,
  .workhours-queue-item {
    flex-direction: column;
  }

  .workhours-control-actions,
  .workhours-queue-meta {
    justify-content: flex-start;
  }
}
</style>

