<template>
  <div class="page-shell">
    <div class="major-demand-hero">
      <div class="major-demand-hero-main">
        <div class="major-demand-hero-kicker-row">
          <span class="major-demand-hero-kicker">Major Demand Desk</span>
          <span class="major-demand-hero-badge">{{ activeDemandFilterLabel }}</span>
        </div>
        <h2 class="major-demand-hero-title">重大需求</h2>
        <div class="major-demand-hero-subtitle">
          围绕当前筛选范围统一查看需求受理、推进、待验证和到期压力，先看重点队列，再进入详情、评论和状态流转动作。
        </div>
        <div class="major-demand-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="major-demand-signal-card">
            <span class="major-demand-signal-label">{{ item.label }}</span>
            <strong class="major-demand-signal-value">{{ item.value }}</strong>
            <span class="major-demand-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="major-demand-hero-side">
        <div class="major-demand-control-card">
          <div class="major-demand-control-copy">
            <span class="major-demand-control-title">需求台动作</span>
            <span class="major-demand-control-note">先锁定状态、负责人或关键字，再从重点队列进入处理。</span>
          </div>
          <div class="major-demand-control-actions">
            <el-button size="small" :loading="loading.fetching" @click="refreshDesk" icon="Refresh">刷新</el-button>
            <el-button v-if="canManageMajorDemand" size="small" type="primary" @click="onAddRow" icon="Plus">新增需求</el-button>
            <el-button size="small" :loading="loading.exporting" @click="onExport" icon="Download">导出</el-button>
          </div>
        </div>

        <div class="major-demand-quick-grid">
          <button
            v-for="action in quickActions"
            :key="action.title"
            type="button"
            class="major-demand-quick-action"
            @click="action.onClick()"
          >
            <span class="major-demand-quick-title">{{ action.title }}</span>
            <span class="major-demand-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <div class="major-demand-insight-grid">
      <section class="major-demand-insight-card">
        <div class="major-demand-insight-head">
          <div>
            <div class="major-demand-insight-title">优先处理清单</div>
            <div class="major-demand-insight-note">按逾期、待处理和待验证优先排序，先推进最容易卡住交付的需求。</div>
          </div>
          <el-tag size="small" type="danger" effect="light">{{ priorityQueue.length }} 项</el-tag>
        </div>
        <div v-if="priorityQueue.length" class="major-demand-queue-list">
          <button
            v-for="item in priorityQueue"
            :key="item._rowId"
            type="button"
            class="major-demand-queue-item"
            @click="openDetail(item)"
          >
            <div class="major-demand-queue-main">
              <strong>{{ resolveHospitalName(item) || '未填写医院' }}</strong>
              <span>{{ resolveDemandNo(item) || '未填写编号' }} · {{ item._owner || '未分配负责人' }}</span>
            </div>
            <div class="major-demand-queue-meta">
              <el-tag size="small" :type="statusTagType(item._status)">{{ item._status || '待评估' }}</el-tag>
              <el-tag size="small" :type="demandDeadlineTagType(item._dueDate)">{{ formatDemandDeadline(item._dueDate) }}</el-tag>
            </div>
          </button>
        </div>
        <el-empty v-else description="当前筛选下没有需要优先推进的重大需求" :image-size="72" />
      </section>

      <section class="major-demand-insight-card">
        <div class="major-demand-insight-head">
          <div>
            <div class="major-demand-insight-title">医院分布</div>
            <div class="major-demand-insight-note">查看当前需求最集中的医院，便于主管判断是否需要专题跟进。</div>
          </div>
          <span class="major-demand-insight-meta">{{ displayRows.length }} 项需求</span>
        </div>
        <div v-if="topHospitalBuckets.length" class="major-demand-chip-list">
          <div v-for="item in topHospitalBuckets" :key="item.label" class="major-demand-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 项</span>
          </div>
        </div>
        <el-empty v-else description="暂无医院分布" :image-size="72" />
      </section>

      <section class="major-demand-insight-card">
        <div class="major-demand-insight-head">
          <div>
            <div class="major-demand-insight-title">负责人负载</div>
            <div class="major-demand-insight-note">按未闭环需求统计当前负责人负载，帮助主管及时分流。</div>
          </div>
          <span class="major-demand-insight-meta">{{ openDemandCount }} 项未闭环</span>
        </div>
        <div v-if="topOwnerBuckets.length" class="major-demand-chip-list">
          <div v-for="item in topOwnerBuckets" :key="item.label" class="major-demand-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 项</span>
          </div>
        </div>
        <el-empty v-else description="暂无负责人负载分布" :image-size="72" />
      </section>

      <section class="major-demand-insight-card">
        <div class="major-demand-insight-head">
          <div>
            <div class="major-demand-insight-title">状态结构</div>
            <div class="major-demand-insight-note">快速判断当前范围内是待处理积压、处理中积压，还是需求已经基本闭环。</div>
          </div>
          <span class="major-demand-insight-meta">{{ overdueDemandCount }} 项逾期</span>
        </div>
        <div v-if="statusBuckets.length" class="major-demand-chip-list">
          <div v-for="item in statusBuckets" :key="item.label" class="major-demand-chip major-demand-chip--soft">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 项</span>
          </div>
        </div>
        <el-empty v-else description="暂无状态结构" :image-size="72" />
      </section>
    </div>

    <div class="major-demand-summary-wrap">
      <SummaryMetrics :items="summaryCards" :columns="5" @select="onSummaryCardSelect" />
    </div>

    <AppFilterCard>
      <el-form class="filter-form" @submit.prevent>
        <el-form-item label="关键字">
          <el-input v-model="query.keyword" placeholder="搜索名称或描述" clearable style="width: 220px" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="query.status" placeholder="全部状态" clearable style="width: 150px">
            <el-option v-for="item in statusOptions" :key="item" :label="item" :value="item" />
          </el-select>
        </el-form-item>
        <el-form-item label="负责人">
          <el-select v-model="query.owner" placeholder="全部负责人" clearable style="width: 150px">
            <el-option v-for="item in ownerOptions" :key="item" :label="item" :value="item" />
          </el-select>
        </el-form-item>

        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
        </el-form-item>
      </el-form>

      <div v-if="canManageMajorDemand" style="margin-top: 16px; padding-top: 16px; border-top: 1px solid var(--el-border-color-light); display: flex; gap: 12px; align-items: center;">
        <span style="font-size: 13px; color: var(--el-text-color-regular); font-weight: 500;">批量操作:</span>
        <el-select v-model="batchForm.status" placeholder="批量状态" style="width: 150px">
          <el-option v-for="item in statusOptions" :key="`batch-status-${item}`" :label="item" :value="item" />
        </el-select>
        <el-button :disabled="!selectedRowIds.length || !batchForm.status" @click="onBatchStatus">批量更新状态</el-button>

        <el-input v-model="batchForm.owner" placeholder="批量负责人" style="width: 150px" />
        <el-button :disabled="!selectedRowIds.length" @click="onBatchOwner" icon="User">批量分配负责人</el-button>

        <el-date-picker
          v-model="batchForm.dueDate"
          type="date"
          value-format="YYYY-MM-DD"
          placeholder="批量截止日期"
          style="width: 170px"
        />
        <el-button :disabled="!selectedRowIds.length" @click="onBatchDueDate">批量设置截止日期</el-button>
      </div>
    </AppFilterCard>

    <ProTable
      title="需求列表"
      :data="pagedRows"
      :loading="loading.fetching"
      :total="displayRows.length"
      v-model:page="currentPage"
      v-model:size="pageSize"
      @refresh="loadData"
      stripe
      row-key="_rowId"
      empty-text="暂无重大需求数据"
      @selection-change="onSelectionChange"
      @row-dblclick="onRowDoubleClick"
    >
      <template #toolbar>
        <el-button v-if="canManageMajorDemand" type="success" @click="onAddRow" icon="Plus">新增</el-button>
        <el-button :loading="loading.exporting" @click="onExport" icon="Download">导出 Excel</el-button>
      </template>

      
      
        <el-table-column type="selection" width="46" />
        <el-table-column v-if="hospitalColumn" :prop="hospitalColumn" :label="hospitalColumn" min-width="180" show-overflow-tooltip />
        <el-table-column v-if="demandNoColumn" :prop="demandNoColumn" :label="demandNoColumn" min-width="150" show-overflow-tooltip />
        <el-table-column v-if="progressColumn" :prop="progressColumn" :label="progressColumn" min-width="160">
          <template #default="scope">
            <el-progress
              v-if="progressColumn && resolveProgressPercent(scope.row[progressColumn]) !== null"
              :percentage="resolveProgressPercent(scope.row[progressColumn]) ?? 0"
              :stroke-width="14"
              :status="(resolveProgressPercent(scope.row[progressColumn]) ?? 0) >= 100 ? 'success' : ''"
              :text-inside="true"
            />
            <span v-else>{{ progressColumn ? scope.row[progressColumn] : '' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="_status" label="状态" min-width="100">
          <template #default="scope">
            <el-tag :type="statusTagType(scope.row._status)">{{ scope.row._status || '待评估' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="_owner" label="负责人" min-width="100" show-overflow-tooltip />
        <el-table-column prop="_dueDate" label="截止日期" min-width="170">
          <template #default="scope">
            <div class="deadline-cell">
              <span>{{ scope.row._dueDate || '-' }}</span>
              <el-tag v-if="scope.row._dueDate" size="small" :type="demandDeadlineTagType(scope.row._dueDate)">
                {{ formatDemandDeadline(scope.row._dueDate) }}
              </el-tag>
            </div>
          </template>
        </el-table-column>
        <el-table-column
          v-for="column in remainingColumns"
          :key="column"
          :prop="column"
          :label="column"
          min-width="180"
          show-overflow-tooltip
        />
        <el-table-column label="操作" fixed="right" width="420">
          <template #default="scope">
            <el-space>
              <el-button
                v-if="canManageMajorDemand && canAcceptDemand(scope.row._status)"
                link
                type="primary"
                :loading="isActionLoading(scope.row._rowId, 'accept')"
                :disabled="isRowBusy(scope.row._rowId)"
                @click="onAccept(scope.row)"
              >受理</el-button>
              <el-button
                v-if="canManageMajorDemand && canCompleteDemand(scope.row._status)"
                link
                type="success"
                :loading="isActionLoading(scope.row._rowId, 'complete')"
                :disabled="isRowBusy(scope.row._rowId)"
                @click="onComplete(scope.row)"
              >完成</el-button>
              <el-button
                v-if="canManageMajorDemand && canReopenDemand(scope.row._status)"
                link
                type="warning"
                :loading="isActionLoading(scope.row._rowId, 'reopen')"
                :disabled="isRowBusy(scope.row._rowId)"
                @click="onReopen(scope.row)"
              >重开</el-button>
              <el-button v-if="canManageMajorDemand" link type="primary" @click="openEdit(scope.row)" icon="Edit">编辑</el-button>
              <el-button link type="primary" @click="openDetail(scope.row)" icon="Document">详情</el-button>
              <el-button
                link
                type="primary"
                :disabled="!canCommentMajorDemand"
                @click="openComment(scope.row)"
              >评论</el-button>
            </el-space>
          </template>
        </el-table-column>
      

      <div class="pager" style="margin-top: 12px; display: flex; justify-content: space-between; align-items: center;">
        <span style="color: var(--el-text-color-secondary)">
          共 {{ displayRows.length }} 条（已选 {{ selectedRowIds.length }} 条）
        </span>
        <el-pagination
          background
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[15, 30, 50, 100]"
          :total="displayRows.length"
          layout="total, sizes, prev, pager, next"
        />
      </div>
    
    </ProTable>

    <ProDrawer v-model="detailVisible" title="重大需求详情" size-class="xl">
      <template v-if="activeRow">
        <el-descriptions :column="1" border>
          <el-descriptions-item label="状态">{{ activeWorkflow?.status || '待评估' }}</el-descriptions-item>
          <el-descriptions-item label="负责人">{{ activeWorkflow?.owner || '-' }}</el-descriptions-item>
          <el-descriptions-item label="截止日期">{{ activeWorkflow?.dueDate || '-' }}</el-descriptions-item>
          <el-descriptions-item label="受理时间">{{ formatTime(activeWorkflow?.acceptedAt || '') }}</el-descriptions-item>
          <el-descriptions-item label="完成时间">{{ formatTime(activeWorkflow?.completedAt || '') }}</el-descriptions-item>
          <el-descriptions-item v-for="column in columns" :key="`desc-${column}`" :label="column">
            {{ activeRow[column] || '-' }}
          </el-descriptions-item>
        </el-descriptions>

        <div v-if="canManageMajorDemand" class="detail-actions">
          <el-button v-if="canAcceptDemand(activeWorkflow?.status || '')" plain type="primary" :loading="isActionLoading(activeRowId, 'accept')" :disabled="isRowBusy(activeRowId)" @click="onAccept(activeRow)">受理</el-button>
          <el-button v-if="canCompleteDemand(activeWorkflow?.status || '')" plain type="success" :loading="isActionLoading(activeRowId, 'complete')" :disabled="isRowBusy(activeRowId)" @click="onComplete(activeRow)">完成</el-button>
          <el-button v-if="canReopenDemand(activeWorkflow?.status || '')" plain type="warning" :loading="isActionLoading(activeRowId, 'reopen')" :disabled="isRowBusy(activeRowId)" @click="onReopen(activeRow)">重开</el-button>
        </div>

        <h4 style="margin: 16px 0 8px">评论</h4>
        <el-timeline v-if="(activeWorkflow?.comments.length ?? 0) > 0">
          <el-timeline-item
            v-for="comment in activeWorkflow?.comments"
            :key="comment.id"
            :timestamp="formatTime(comment.createdAt)"
          >
            <div style="font-weight: 600">{{ comment.createdBy }}</div>
            <div>{{ comment.content }}</div>
          </el-timeline-item>
        </el-timeline>
        <el-empty v-else description="暂无评论" :image-size="60" />

        <h4 style="margin: 16px 0 8px">操作日志</h4>
        <el-timeline v-if="(activeWorkflow?.logs.length ?? 0) > 0">
          <el-timeline-item
            v-for="log in activeWorkflow?.logs"
            :key="log.id"
            :timestamp="formatTime(log.createdAt)"
          >
            <div style="font-weight: 600">{{ log.action }}（{{ log.createdBy }}）</div>
            <div>{{ log.detail }}</div>
          </el-timeline-item>
        </el-timeline>
        <el-empty v-else description="暂无日志" :image-size="60" />
      </template>
    </ProDrawer>

    <ProDrawer v-model="commentVisible" title="新增评论" width="520px">
      <el-input v-model="commentContent" type="textarea" :rows="4" maxlength="500" show-word-limit />
      <template #footer>
        <el-button @click="commentVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="loading.commenting" @click="submitComment" icon="Check">提交</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="editVisible" title="编辑重大需求" width="680px">
      <el-form v-if="editForm" label-width="120px" label-position="right">
        <el-form-item label="状态">
          <el-select v-model="editForm._status" style="width: 100%">
            <el-option v-for="item in statusOptions" :key="`edit-s-${item}`" :label="item" :value="item" />
          </el-select>
        </el-form-item>
        <el-form-item label="负责人">
          <el-input v-model="editForm._owner" />
        </el-form-item>
        <el-form-item label="截止日期">
          <el-date-picker v-model="editForm._dueDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
        <el-form-item v-for="column in columns" :key="`edit-${column}`" :label="column">
          <el-input v-model="editForm[column]" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="loading.saving" @click="submitEdit" icon="Check">保存</el-button>
      </template>
    </ProDrawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import {
  acceptMajorDemand,
  addMajorDemandComment,
  addMajorDemandRow,
  batchAssignMajorDemandOwner,
  batchUpdateMajorDemandDueDate,
  batchUpdateMajorDemandStatus,
  completeMajorDemand,
  exportMajorDemandExcel,
  fetchMajorDemands,
  reopenMajorDemand,
  updateMajorDemandCell,
  type MajorDemandWorkflow,
} from '../../api/modules/majorDemand'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { normalizeStatusText, resolveMajorDemandStatusTag } from '../../utils/statusTag'
import {
  normalizeMajorDemandValue,
  resolveMajorDemandDueDate,
  resolveMajorDemandOwner,
  resolveMajorDemandRowId,
  resolveMajorDemandStatus,
} from '../../utils/majorDemandFields'
import ProTable from '../../components/ProTable.vue'
import ProDrawer from '../../components/ProDrawer.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'


const columns = ref<string[]>([])
const rows = ref<Array<Record<string, string>>>([])
const workflows = ref<MajorDemandWorkflow[]>([])
const sourceFilePath = ref('')
const importedAt = ref('')
const selectedRowIds = ref<string[]>([])
const detailVisible = ref(false)
const activeRowId = ref('')
const commentVisible = ref(false)
const commentTargetRowId = ref('')
const commentContent = ref('')
const currentPage = ref(1)
const pageSize = ref(15)
const editVisible = ref(false)
const editRowId = ref('')
const editForm = ref<Record<string, string>>({})
const actionLoadingKey = ref('')
const nowTick = ref(Date.now())
let nowTimer: number | null = null
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
  if (!readRouteQueryValue(route.query.action) && !readRouteQueryValue(route.query.rowId)) {
    return
  }

  await updateRouteQuery({ action: undefined, rowId: undefined })
}

const query = reactive({
  keyword: '',
  status: '',
  owner: '',
})

const batchForm = reactive({
  status: '处理中',
  owner: '',
  dueDate: '',
})

const loading = reactive({
  fetching: false,
  exporting: false,
  commenting: false,
  saving: false,
})

const access = useAccessControl()

type MajorDemandFilterState = {
  keyword: string
  status: string
  owner: string
}

type MajorDemandDisplayRow = Record<string, string> & {
  _rowId: string
  _status: string
  _owner: string
  _dueDate: string
  _acceptedAt: string
  _completedAt: string
}

type MajorDemandSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active: boolean
}

type MajorDemandBucket = {
  label: string
  value: number
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<MajorDemandFilterState>({
  key: 'major-demand',
  getState: () => ({ keyword: query.keyword, status: query.status, owner: query.owner }),
  applyState: (state) => {
    query.keyword = typeof state.keyword === 'string' ? state.keyword : ''
    query.status = typeof state.status === 'string' ? state.status : ''
    query.owner = typeof state.owner === 'string' ? state.owner : ''
  },
})

const canManageMajorDemand = computed(() => {
  return access.canPermission('major-demand.manage') || access.canPermission('maintenance.manage')
})
const canCommentMajorDemand = computed(() => {
  return canManageMajorDemand.value
})

const statusOptions = ['待评估', '待处理', '处理中', '待验证', '已完成', '已关闭']

const workflowMap = computed(() => {
  const map = new Map<string, MajorDemandWorkflow>()
  for (const item of workflows.value) {
    map.set(item.rowId, item)
  }
  return map
})

const ownerOptions = computed(() => {
  const owners = rows.value
    .map((row, index) => {
      const rowId = resolveMajorDemandRowId(row, index + 1)
      const workflow = workflowMap.value.get(rowId)
      return normalizeMajorDemandValue(workflow?.owner) || resolveMajorDemandOwner(row)
    })
    .filter((x) => !!x)
  return Array.from(new Set(owners))
})

const hospitalColumn = computed(() => columns.value.find((c) => c.includes('医院')))
const demandNoColumn = computed(() => columns.value.find((c) => c.includes('编号')))
const progressColumn = computed(() => columns.value.find((c) => isProgressColumn(c)))
const priorityColumnSet = computed(() => new Set([hospitalColumn.value, demandNoColumn.value, progressColumn.value].filter(Boolean)))
const remainingColumns = computed(() => columns.value.filter((c) => !priorityColumnSet.value.has(c)))

const displayRows = computed<MajorDemandDisplayRow[]>(() => {
  const keyword = query.keyword.trim().toLowerCase()
  return rows.value
    .filter((row, index) => {
      const rowId = resolveMajorDemandRowId(row, index + 1)
      const workflow = workflowMap.value.get(rowId)
      const status = normalizeMajorDemandValue(workflow?.status) || resolveMajorDemandStatus(row) || '待评估'
      const owner = normalizeMajorDemandValue(workflow?.owner) || resolveMajorDemandOwner(row)
      const dueDate = normalizeMajorDemandValue(workflow?.dueDate) || resolveMajorDemandDueDate(row)
      if (query.status && normalizeStatusText(status) !== normalizeStatusText(query.status)) return false
      if (query.owner && owner !== query.owner) return false
      if (!keyword) return true

      const inColumns = columns.value.some((column) => String(row[column] ?? '').toLowerCase().includes(keyword))
      const inWorkflow = [status, owner, dueDate].some((x) => String(x ?? '').toLowerCase().includes(keyword))
      return inColumns || inWorkflow
    })
    .map((row, index) => {
      const rowId = resolveMajorDemandRowId(row, index + 1)
      const workflow = workflowMap.value.get(rowId)
      const status = normalizeMajorDemandValue(workflow?.status) || resolveMajorDemandStatus(row) || '待评估'
      const owner = normalizeMajorDemandValue(workflow?.owner) || resolveMajorDemandOwner(row)
      const dueDate = normalizeMajorDemandValue(workflow?.dueDate) || resolveMajorDemandDueDate(row)
      return {
        ...row,
        _rowId: rowId,
        _status: status,
        _owner: owner,
        _dueDate: dueDate,
        _acceptedAt: workflow?.acceptedAt ?? '',
        _completedAt: workflow?.completedAt ?? '',
      }
    })
})

const buildBuckets = (items: MajorDemandDisplayRow[], resolveLabel: (item: MajorDemandDisplayRow) => string, limit = 5): MajorDemandBucket[] => {
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

const resolveHospitalName = (row: Record<string, string>) => hospitalColumn.value ? String(row[hospitalColumn.value] ?? '').trim() : ''
const resolveDemandNo = (row: Record<string, string>) => demandNoColumn.value ? String(row[demandNoColumn.value] ?? '').trim() : ''

const isClosedDemandStatus = (status: string) => {
  const normalized = normalizeStatusText(status)
  return normalized === '已完成' || normalized === '已关闭'
}

const isOverdueDemand = (row: MajorDemandDisplayRow) => {
  const dueAt = parseDemandDeadline(row._dueDate)
  return Boolean(dueAt) && dueAt!.getTime() <= nowTick.value && !isClosedDemandStatus(row._status)
}

const demandStatusPriority = (status: string) => {
  const normalized = normalizeStatusText(status)
  if (normalized === '待处理' || normalized === '待评估') return 0
  if (normalized === '待验证') return 1
  if (normalized === '处理中') return 2
  if (normalized === '已完成') return 3
  if (normalized === '已关闭') return 4
  return 5
}

const overdueDemandCount = computed(() => displayRows.value.filter((item) => isOverdueDemand(item)).length)
const openDemandCount = computed(() => displayRows.value.filter((item) => !isClosedDemandStatus(item._status)).length)
const inProgressDemandCount = computed(() => displayRows.value.filter((item) => normalizeStatusText(item._status) === '处理中').length)
const pendingDemandCount = computed(() => displayRows.value.filter((item) => {
  const normalized = normalizeStatusText(item._status)
  return normalized === '待处理' || normalized === '待评估'
}).length)
const verificationDemandCount = computed(() => displayRows.value.filter((item) => normalizeStatusText(item._status) === '待验证').length)
const completedDemandCount = computed(() => displayRows.value.filter((item) => normalizeStatusText(item._status) === '已完成').length)

const priorityQueue = computed(() => displayRows.value
  .filter((item) => !isClosedDemandStatus(item._status))
  .slice()
  .sort((left, right) => {
    const overdueDiff = Number(isOverdueDemand(right)) - Number(isOverdueDemand(left))
    if (overdueDiff !== 0) {
      return overdueDiff
    }

    const statusDiff = demandStatusPriority(left._status) - demandStatusPriority(right._status)
    if (statusDiff !== 0) {
      return statusDiff
    }

    return String(left._dueDate || '').localeCompare(String(right._dueDate || ''), 'zh-CN')
  })
  .slice(0, 5))

const topHospitalBuckets = computed(() => buildBuckets(displayRows.value, (item) => resolveHospitalName(item) || '未填写医院'))
const topOwnerBuckets = computed(() => buildBuckets(
  displayRows.value.filter((item) => !isClosedDemandStatus(item._status)),
  (item) => item._owner || '未分配负责人',
))
const statusBuckets = computed(() => buildBuckets(displayRows.value, (item) => item._status || '待评估', 6))

const activeDemandFilterLabel = computed(() => {
  if (query.status) return `状态：${query.status}`
  if (query.owner) return `负责人：${query.owner}`
  if (query.keyword.trim()) return `关键字：${query.keyword.trim()}`
  return '全部重大需求'
})

const heroSignals = computed(() => [
  { label: '待处理', value: pendingDemandCount.value, note: '待评估与待处理' },
  { label: '处理中', value: inProgressDemandCount.value, note: '当前推进中' },
  { label: '待验证', value: verificationDemandCount.value, note: '待确认结果' },
  { label: '逾期', value: overdueDemandCount.value, note: `共 ${displayRows.value.length} 项` },
])

const summaryCards = computed<MajorDemandSummaryCard[]>(() => [
  {
    key: '待处理',
    title: '待处理',
    value: pendingDemandCount.value,
    context: '需求状态',
    note: '查看待评估与待处理需求',
    color: '#7c98bc',
    active: normalizeStatusText(query.status) === '待处理' || normalizeStatusText(query.status) === '待评估',
  },
  {
    key: '处理中',
    title: '处理中',
    value: inProgressDemandCount.value,
    context: '需求状态',
    note: '查看当前正在推进的需求',
    color: '#c7a06c',
    active: normalizeStatusText(query.status) === '处理中',
  },
  {
    key: '待验证',
    title: '待验证',
    value: verificationDemandCount.value,
    context: '需求状态',
    note: '查看待验证和待回执需求',
    color: '#8db3a8',
    active: normalizeStatusText(query.status) === '待验证',
  },
  {
    key: '已完成',
    title: '已完成',
    value: completedDemandCount.value,
    context: '需求状态',
    note: '查看已完成的闭环记录',
    color: '#7d9f92',
    active: normalizeStatusText(query.status) === '已完成',
  },
  {
    key: 'all',
    title: '总数',
    value: displayRows.value.length,
    context: '全量视图',
    note: '返回当前筛选范围的全部需求',
    color: '#3f4f63',
    active: !query.status,
  },
])

const quickActions = computed(() => [
  { title: '待处理', note: `${pendingDemandCount.value} 项待推进`, onClick: () => applyStatusFilter('待处理') },
  { title: '处理中', note: `${inProgressDemandCount.value} 项进行中`, onClick: () => applyStatusFilter('处理中') },
  { title: '待验证', note: `${verificationDemandCount.value} 项待确认`, onClick: () => applyStatusFilter('待验证') },
  { title: '全部重置', note: '恢复全量视图', onClick: () => onReset() },
])

const pagedRows = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return displayRows.value.slice(start, start + pageSize.value)
})

const activeRow = computed<Record<string, string> | null>(() => {
  return rows.value.find((item, index) => resolveMajorDemandRowId(item, index + 1) === activeRowId.value) ?? null
})
const activeWorkflow = computed(() => workflowMap.value.get(activeRowId.value) ?? null)

const applyRouteFilters = () => {
  const status = readRouteQueryValue(route.query.status)
  const owner = readRouteQueryValue(route.query.owner)
  const keyword = readRouteQueryValue(route.query.keyword)

  if (status) {
    query.status = status
  }

  if (owner) {
    query.owner = owner
  }

  if (keyword) {
    query.keyword = keyword
  }
}

const isProgressColumn = (columnName: string): boolean => {
  const value = columnName.trim().toLowerCase()
  return value.includes('进度') || value.includes('progress') || value.includes('完成率')
}

const resolveProgressPercent = (rawValue: unknown): number | null => {
  if (rawValue === null || rawValue === undefined) {
    return null
  }

  const text = String(rawValue).trim().replace('%', '')
  if (!text) {
    return null
  }

  const parsed = Number(text)
  if (!Number.isFinite(parsed)) {
    return null
  }

  const percent = parsed <= 1 ? parsed * 100 : parsed
  const normalized = Math.max(0, Math.min(100, percent))
  return Math.round(normalized * 100) / 100
}

const statusTagType = (status: string) => resolveMajorDemandStatusTag(status)

const parseDemandDeadline = (value: string) => {
  if (!value) {
    return null
  }

  const trimmed = value.trim()
  const normalized = /^\d{4}-\d{2}-\d{2}$/.test(trimmed) ? `${trimmed}T23:59:59` : trimmed
  const date = new Date(normalized)
  return Number.isNaN(date.getTime()) ? null : date
}

const formatDemandDeadline = (value: string) => {
  const dueAt = parseDemandDeadline(value)
  if (!dueAt) {
    return '未设置'
  }

  const diff = dueAt.getTime() - nowTick.value
  const hours = Math.round(diff / 3600000)
  if (hours < 0) {
    return `逾期 ${Math.abs(hours)}h`
  }
  if (hours < 24) {
    return `${hours}h 内`
  }

  return `${Math.ceil(hours / 24)} 天内`
}

const demandDeadlineTagType = (value: string) => {
  const dueAt = parseDemandDeadline(value)
  if (!dueAt) {
    return 'info'
  }

  const diff = dueAt.getTime() - nowTick.value
  if (diff <= 0) {
    return 'danger'
  }
  if (diff <= 24 * 3600000) {
    return 'warning'
  }
  return 'success'
}

const canAcceptDemand = (status: string) => {
  const normalized = normalizeStatusText(status)
  return ['待评估', '待处理', '处理中', '待验证'].includes(normalized)
}

const canCompleteDemand = (status: string) => {
  const normalized = normalizeStatusText(status)
  return normalized === '处理中' || normalized === '待验证'
}

const canReopenDemand = (status: string) => {
  const normalized = normalizeStatusText(status)
  return normalized === '已完成' || normalized === '已关闭'
}

const workflowActionKey = (rowId: string, action: string) => `${rowId}:${action}`
const isActionLoading = (rowId: string, action: string) => actionLoadingKey.value === workflowActionKey(rowId, action)
const isRowBusy = (rowId: string) => actionLoadingKey.value.startsWith(`${rowId}:`)

const formatTime = (value: string) => {
  if (!value) return '-'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
}

const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await loadData()
  },
  scope: 'major-demand',
  intervalMs: 60000,
})

const { runInitialLoad } = useResilientLoad()

const loadData = async () => {
  loading.fetching = true
  try {
    const res = await fetchMajorDemands()
    columns.value = (res.data.columns ?? []).filter((column) => column !== '_RowId')
    rows.value = res.data.rows ?? []
    workflows.value = res.data.workflows ?? []
    sourceFilePath.value = res.data.sourceFilePath ?? ''
    importedAt.value = res.data.importedAt ? String(res.data.importedAt) : ''
    selectedRowIds.value = selectedRowIds.value.filter((id) => rows.value.some((item) => item._RowId === id))
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载重大需求数据失败，请稍后重试'))
  } finally {
    loading.fetching = false
  }
}

const refreshDesk = async () => {
  await loadData()
}

const applyStatusFilter = (status: string) => {
  query.status = status
  currentPage.value = 1
}

const onSummaryCardSelect = (card: { key?: string | number }) => {
  if (typeof card.key !== 'string') {
    return
  }

  if (card.key === 'all') {
    query.status = ''
    currentPage.value = 1
    return
  }

  applyStatusFilter(card.key)
}

const runWorkflowAction = async (rowId: string, action: string, runner: () => Promise<void>, message: string) => {
  actionLoadingKey.value = workflowActionKey(rowId, action)
  try {
    await runner()
    ElMessage.success(message)
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, `${message}失败`))
  } finally {
    actionLoadingKey.value = ''
  }
}

const onSearch = () => {
  currentPage.value = 1
}

const onReset = () => {
  query.keyword = ''
  query.status = ''
  query.owner = ''
  currentPage.value = 1
  clearFilterState()
}

const onSelectionChange = (selection: Array<Record<string, string>>) => {
  selectedRowIds.value = selection.map((item) => item._rowId ?? '').filter(Boolean)
}

const onBatchStatus = async () => {
  if (!canManageMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求批量管理权限')
    return
  }

  if (!selectedRowIds.value.length || !batchForm.status) {
    return
  }

  try {
    await batchUpdateMajorDemandStatus({ rowIds: selectedRowIds.value, status: batchForm.status })
    ElMessage.success('批量状态更新成功')
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '批量状态更新失败'))
  }
}

const onBatchOwner = async () => {
  if (!canManageMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求批量管理权限')
    return
  }

  if (!selectedRowIds.value.length) {
    return
  }

  try {
    await batchAssignMajorDemandOwner({ rowIds: selectedRowIds.value, owner: batchForm.owner.trim() })
    ElMessage.success('批量负责人更新成功')
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '批量负责人更新失败'))
  }
}

const onBatchDueDate = async () => {
  if (!canManageMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求批量管理权限')
    return
  }

  if (!selectedRowIds.value.length) {
    return
  }

  try {
    await batchUpdateMajorDemandDueDate({ rowIds: selectedRowIds.value, dueDate: batchForm.dueDate })
    ElMessage.success('批量截止日期更新成功')
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '批量截止日期更新失败'))
  }
}

const openDetail = (row: Record<string, string>) => {
  activeRowId.value = row._rowId ?? ''
  detailVisible.value = true
  void updateRouteQuery({ action: 'detail', rowId: activeRowId.value || undefined })
}

const onRowDoubleClick = (row: Record<string, string>) => {
  openDetail(row)
}

const openComment = (row: Record<string, string>) => {
  if (!canCommentMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求进度维护权限')
    return
  }

  commentTargetRowId.value = row._rowId ?? ''
  commentContent.value = ''
  commentVisible.value = true
  void updateRouteQuery({ action: 'comment', rowId: commentTargetRowId.value || undefined })
}

const syncPanelFromRoute = () => {
  const action = readRouteQueryValue(route.query.action)
  const rowId = readRouteQueryValue(route.query.rowId)
  if (!action || !rowId) {
    return
  }

  const matched = displayRows.value.find((item) => item._rowId === rowId)
  if (!matched) {
    return
  }

  if (action === 'detail' && !detailVisible.value) {
    openDetail(matched)
    return
  }

  if (action === 'comment' && !commentVisible.value) {
    openComment(matched)
  }
}

const submitComment = async () => {
  if (!canCommentMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求进度维护权限')
    return
  }

  if (!commentTargetRowId.value || !commentContent.value.trim()) {
    ElMessage.warning('请输入评论内容')
    return
  }

  loading.commenting = true
  try {
    await addMajorDemandComment(commentTargetRowId.value, commentContent.value.trim())
    ElMessage.success('评论提交成功')
    commentVisible.value = false
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '评论提交失败'))
  } finally {
    loading.commenting = false
  }
}

const onAccept = async (row: Record<string, string>) => {
  if (!canManageMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求管理权限')
    return
  }

  const rowId = row._rowId ?? ''
  if (!rowId) {
    return
  }

  try {
    const { value } = await ElMessageBox.prompt('可选填写负责人，留空则沿用当前负责人或操作人。', '受理重大需求', {
      inputValue: row._owner ?? '',
      inputPlaceholder: '负责人',
      confirmButtonText: '受理',
      cancelButtonText: '取消',
    })

    await runWorkflowAction(rowId, 'accept', async () => {
      await acceptMajorDemand(rowId, value?.trim() ? { owner: value.trim() } : undefined)
    }, '需求已受理')
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      return
    }
    ElMessage.error(getErrorMessage(error, '需求受理失败'))
  }
}

const onComplete = async (row: Record<string, string>) => {
  if (!canManageMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求管理权限')
    return
  }

  const rowId = row._rowId ?? ''
  if (!rowId) {
    return
  }

  try {
    const { value } = await ElMessageBox.prompt('请填写完成说明。', '完成重大需求', {
      inputPlaceholder: '例如：已完成系统上线与现场培训',
      confirmButtonText: '完成',
      cancelButtonText: '取消',
      inputValidator: (inputValue) => inputValue.trim().length > 0 || '请输入完成说明',
    })

    await runWorkflowAction(rowId, 'complete', async () => {
      await completeMajorDemand(rowId, { note: value.trim() })
    }, '需求已完成')
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      return
    }
    ElMessage.error(getErrorMessage(error, '完成重大需求失败'))
  }
}

const onReopen = async (row: Record<string, string>) => {
  if (!canManageMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求管理权限')
    return
  }

  const rowId = row._rowId ?? ''
  if (!rowId) {
    return
  }

  try {
    const { value } = await ElMessageBox.prompt('可选填写重开原因。', '重开重大需求', {
      inputPlaceholder: '重开原因',
      confirmButtonText: '重开',
      cancelButtonText: '取消',
    })

    await runWorkflowAction(rowId, 'reopen', async () => {
      await reopenMajorDemand(rowId, value?.trim() ? { reason: value.trim() } : undefined)
    }, '需求已重开')
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      return
    }
    ElMessage.error(getErrorMessage(error, '重开重大需求失败'))
  }
}

const openEdit = (row: Record<string, string>) => {
  editRowId.value = row._rowId ?? ''
  editForm.value = { ...row }
  editVisible.value = true
}

const submitEdit = async () => {
  if (!editRowId.value) return
  loading.saving = true
  try {
    const origRow = displayRows.value.find((r) => r._rowId === editRowId.value)
    if (origRow) {
      if (editForm.value._status !== origRow._status) {
        await batchUpdateMajorDemandStatus({ rowIds: [editRowId.value], status: editForm.value._status ?? '' })
      }
      if (editForm.value._owner !== origRow._owner) {
        await batchAssignMajorDemandOwner({ rowIds: [editRowId.value], owner: editForm.value._owner ?? '' })
      }
      if (editForm.value._dueDate !== origRow._dueDate) {
        await batchUpdateMajorDemandDueDate({ rowIds: [editRowId.value], dueDate: editForm.value._dueDate ?? '' })
      }
      for (const column of columns.value) {
        if (editForm.value[column] !== (origRow as Record<string, string>)[column]) {
          await updateMajorDemandCell(editRowId.value, column, editForm.value[column] ?? '')
        }
      }
    }
    ElMessage.success('保存成功')
    editVisible.value = false
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存失败'))
  } finally {
    loading.saving = false
  }
}

const onAddRow = async () => {
  if (!canManageMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求管理权限')
    return
  }

  try {
    await addMajorDemandRow()
    ElMessage.success('新增成功')
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '新增失败'))
  }
}

const onExport = async () => {
  loading.exporting = true
  try {
    const blob = await exportMajorDemandExcel()
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `major-demands-${Date.now()}.xlsx`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  } finally {
    loading.exporting = false
  }
}

onMounted(async () => {
  nowTimer = window.setInterval(() => {
    nowTick.value = Date.now()
  }, 60000)

  await access.ensureAccessProfileLoaded()
  restoreFilterState()
  applyRouteFilters()
  await runInitialLoad({
    tasks: [loadData],
  })
  syncPanelFromRoute()
})

onBeforeUnmount(() => {
  if (nowTimer !== null) {
    window.clearInterval(nowTimer)
    nowTimer = null
  }
})

watch(detailVisible, (visible) => {
  if (!visible && !commentVisible.value) {
    void clearRouteActionQuery()
  }
})

watch(commentVisible, (visible) => {
  if (!visible && !detailVisible.value) {
    void clearRouteActionQuery()
  }
})

watch(() => route.fullPath, () => {
  applyRouteFilters()
  syncPanelFromRoute()
})
</script>

<style scoped>
.major-demand-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.18fr) minmax(320px, 0.82fr);
  gap: 16px;
  padding: 24px;
  border-radius: 28px;
  color: #ffffff;
  background:
    radial-gradient(ellipse 80% 60% at 18% 30%, rgba(104, 122, 220, 0.22) 0%, transparent 70%),
    linear-gradient(135deg, #24355c 0%, #2e4a88 48%, #2f6f91 100%);
  box-shadow: 0 26px 44px rgba(29, 46, 82, 0.3), inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

.major-demand-hero-kicker-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 10px;
}

.major-demand-hero-kicker {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  opacity: 0.65;
}

.major-demand-hero-badge {
  padding: 2px 10px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.14);
  font-size: 11px;
  font-weight: 600;
  opacity: 0.9;
}

.major-demand-hero-title {
  margin: 0 0 6px;
  font-size: 28px;
  font-weight: 800;
  line-height: 1.2;
  letter-spacing: -0.5px;
}

.major-demand-hero-subtitle {
  margin-bottom: 18px;
  font-size: 13px;
  opacity: 0.72;
}

.major-demand-hero-signals {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
}

.major-demand-signal-card {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 10px 12px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(4px);
  border: 1px solid rgba(255, 255, 255, 0.12);
}

.major-demand-signal-label {
  font-size: 11px;
  font-weight: 600;
  opacity: 0.72;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.major-demand-signal-value {
  font-size: 22px;
  font-weight: 800;
  line-height: 1.15;
}

.major-demand-signal-note {
  font-size: 11px;
  opacity: 0.62;
}

.major-demand-hero-side {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.major-demand-control-card {
  padding: 16px 18px;
  border-radius: 20px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(255, 255, 255, 0.14);
}

.major-demand-control-copy {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-bottom: 12px;
}

.major-demand-control-title {
  font-size: 15px;
  font-weight: 700;
}

.major-demand-control-note {
  font-size: 12px;
  opacity: 0.68;
}

.major-demand-control-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.major-demand-control-actions :deep(.el-button) {
  background: rgba(255, 255, 255, 0.14);
  border-color: rgba(255, 255, 255, 0.25);
  color: #fff;
}

.major-demand-control-actions :deep(.el-button:hover) {
  background: rgba(255, 255, 255, 0.22);
}

.major-demand-quick-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  flex: 1;
}

.major-demand-quick-action {
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

.major-demand-quick-action:hover {
  background: rgba(255, 255, 255, 0.18);
  transform: translateY(-1px);
}

.major-demand-quick-title {
  font-size: 13px;
  font-weight: 700;
}

.major-demand-quick-note {
  font-size: 11px;
  opacity: 0.65;
}

.major-demand-insight-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
  margin-top: 16px;
}

.major-demand-insight-card,
.major-demand-summary-wrap {
  border-radius: 20px;
}

.major-demand-insight-card {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 18px 20px;
  background: #ffffff;
  border: 1px solid rgba(148, 163, 184, 0.16);
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.06);
}

.major-demand-insight-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.major-demand-insight-title {
  font-size: 15px;
  font-weight: 700;
  color: #111827;
}

.major-demand-insight-note {
  margin-top: 4px;
  font-size: 12px;
  line-height: 1.6;
  color: #64748b;
}

.major-demand-insight-meta {
  font-size: 12px;
  font-weight: 600;
  color: #475569;
  white-space: nowrap;
}

.major-demand-queue-list,
.major-demand-chip-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.major-demand-queue-item {
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

.major-demand-queue-item:hover {
  transform: translateY(-1px);
  border-color: rgba(59, 130, 246, 0.22);
  box-shadow: 0 12px 24px rgba(15, 23, 42, 0.08);
}

.major-demand-queue-main,
.major-demand-queue-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.major-demand-queue-main strong,
.major-demand-chip strong {
  font-size: 13px;
  font-weight: 700;
  color: #111827;
}

.major-demand-queue-main span,
.major-demand-queue-meta span,
.major-demand-chip span {
  font-size: 12px;
  line-height: 1.5;
  color: #64748b;
}

.major-demand-queue-meta {
  align-items: flex-end;
}

.major-demand-chip-list {
  gap: 8px;
}

.major-demand-chip {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 11px 14px;
  border-radius: 14px;
  background: #f8fafc;
  border: 1px solid rgba(148, 163, 184, 0.14);
}

.major-demand-chip--soft {
  background: #f5f7fb;
}

.deadline-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.detail-actions {
  margin-top: 16px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

@media (max-width: 1280px) {
  .major-demand-hero,
  .major-demand-insight-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .major-demand-hero {
    padding: 16px;
  }

  .major-demand-hero-signals {
    grid-template-columns: repeat(2, 1fr);
  }

  .major-demand-quick-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .major-demand-queue-item,
  .major-demand-chip,
  .major-demand-insight-head {
    align-items: flex-start;
    flex-direction: column;
  }

  .major-demand-queue-meta {
    align-items: flex-start;
  }
}
</style>
