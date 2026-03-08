<template>
  <div ref="pageRef" class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">年度报告</h2>
        <div class="page-subtitle">跟踪年度服务报告进度与提交情况（点击单元格即可编辑）</div>
      </div>
      <div class="head-actions no-print">
        <el-button size="small" @click="onPrint">打印</el-button>
      </div>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: isStatusActive('未开始') }" @click="onStatClick('未开始')"><div class="t">未开始</div><div class="v">{{ summary.notStartedCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: isStatusActive('编写中') }" @click="onStatClick('编写中')"><div class="t">编写中</div><div class="v warning">{{ summary.writingCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: isStatusActive('已提交') }" @click="onStatClick('已提交')"><div class="t">已提交</div><div class="v info">{{ summary.submittedCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: isStatusActive('已完成') }" @click="onStatClick('已完成')"><div class="t">已完成</div><div class="v success">{{ summary.completedCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: isOverdueActive }" @click="onOverdueClick"><div class="t">逾期待处理</div><div class="v" :class="{ danger: summary.overdueCount > 0 }">{{ summary.overdueCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: !normalizeStatusText(query.status) && !isOverdueActive }" @click="onStatClick('')"><div class="t">总数</div><div class="v">{{ summary.total }}</div></el-card></el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="状态">
          <el-select v-model="query.status" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="status in statusOptions" :key="status" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item label="到期月份">
          <el-date-picker v-model="dueMonthPicker" type="month" placeholder="全部" clearable value-format="YYYY-MM" style="width: 160px" @change="onDueMonthChange" />
        </el-form-item>
        <el-form-item label="组别">
          <el-select v-model="query.groupName" clearable placeholder="全部" style="width: 160px">
            <el-option v-for="group in filteredGroupOptions" :key="group" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="服务人员">
          <el-select v-model="query.servicePerson" clearable placeholder="全部" style="width: 140px">
            <el-option v-for="person in filteredServicePersonOptions" :key="person" :label="person" :value="person" />
          </el-select>
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
          <el-button :loading="exporting" @click="onExport">导出CSV</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无符合条件的数据" :row-class-name="rowClassName">
        <!-- 机会号 -->
        <el-table-column prop="opportunityNumber" label="机会号" min-width="150" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="isEditing(row.id, 'opportunityNumber')" v-model="row.opportunityNumber" size="small" @blur="onCellSave(row, 'opportunityNumber')" @keyup.enter="onCellSave(row, 'opportunityNumber')" v-focus />
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'opportunityNumber')">{{ row.opportunityNumber || '-' }}</span>
          </template>
        </el-table-column>
        <!-- 医院 -->
        <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip sortable>
          <template #default="{ row }">
            <el-input v-if="isEditing(row.id, 'hospitalName')" v-model="row.hospitalName" size="small" @blur="onCellSave(row, 'hospitalName')" @keyup.enter="onCellSave(row, 'hospitalName')" v-focus />
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'hospitalName')">{{ row.hospitalName || '-' }}</span>
          </template>
        </el-table-column>
        <!-- 产品 -->
        <el-table-column prop="productName" label="产品" min-width="150" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="isEditing(row.id, 'productName')" v-model="row.productName" size="small" @blur="onCellSave(row, 'productName')" @keyup.enter="onCellSave(row, 'productName')" v-focus />
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'productName')">{{ row.productName || '-' }}</span>
          </template>
        </el-table-column>
        <!-- 省份 -->
        <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip sortable>
          <template #default="{ row }">
            <el-input v-if="isEditing(row.id, 'province')" v-model="row.province" size="small" @blur="onCellSave(row, 'province')" @keyup.enter="onCellSave(row, 'province')" v-focus />
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'province')">{{ row.province || '-' }}</span>
          </template>
        </el-table-column>
        <!-- 组别 -->
        <el-table-column prop="groupName" label="组别" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            <el-select v-if="isEditing(row.id, 'groupName')" v-model="row.groupName" size="small" clearable filterable @change="onCellSave(row, 'groupName')" @blur="onCellSave(row, 'groupName')" v-focus>
              <el-option v-for="g in groupOptions" :key="g" :label="g" :value="g" />
            </el-select>
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'groupName')">{{ row.groupName || '-' }}</span>
          </template>
        </el-table-column>
        <!-- 服务人员 -->
        <el-table-column prop="servicePerson" label="服务人员" width="110" show-overflow-tooltip>
          <template #default="{ row }">
            <el-select v-if="isEditing(row.id, 'servicePerson')" v-model="row.servicePerson" size="small" clearable filterable @change="onCellSave(row, 'servicePerson')" @blur="onCellSave(row, 'servicePerson')" v-focus>
              <el-option v-for="p in servicePersonOptions" :key="p" :label="p" :value="p" />
            </el-select>
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'servicePerson')">{{ row.servicePerson || '-' }}</span>
          </template>
        </el-table-column>
        <!-- 实施状态 -->
        <el-table-column prop="implementationStatus" label="实施状态" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="isEditing(row.id, 'implementationStatus')" v-model="row.implementationStatus" size="small" @blur="onCellSave(row, 'implementationStatus')" @keyup.enter="onCellSave(row, 'implementationStatus')" v-focus />
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'implementationStatus')">{{ row.implementationStatus || '-' }}</span>
          </template>
        </el-table-column>
        <!-- 维护开始 -->
        <el-table-column prop="maintenanceStartDate" label="维护开始" width="130">
          <template #default="{ row }">
            <el-date-picker v-if="isEditing(row.id, 'maintenanceStartDate')" v-model="row.maintenanceStartDate" type="date" size="small" value-format="YYYY-MM-DD" style="width:100%" @change="onCellSave(row, 'maintenanceStartDate')" @blur="onCellSave(row, 'maintenanceStartDate')" v-focus />
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'maintenanceStartDate')">{{ row.maintenanceStartDate ? row.maintenanceStartDate.slice(0, 10) : '-' }}</span>
          </template>
        </el-table-column>
        <!-- 维护结束 -->
        <el-table-column prop="maintenanceEndDate" label="维护结束" width="130">
          <template #default="{ row }">
            <el-date-picker v-if="isEditing(row.id, 'maintenanceEndDate')" v-model="row.maintenanceEndDate" type="date" size="small" value-format="YYYY-MM-DD" style="width:100%" @change="onCellSave(row, 'maintenanceEndDate')" @blur="onCellSave(row, 'maintenanceEndDate')" v-focus />
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'maintenanceEndDate')">{{ row.maintenanceEndDate ? row.maintenanceEndDate.slice(0, 10) : '-' }}</span>
          </template>
        </el-table-column>
        <!-- 到期月份 -->
        <el-table-column prop="dueMonth" label="到期月份" width="110" sortable>
          <template #default="{ row }">
            <el-tag v-if="row.dueMonth === currentMonth" type="warning" size="small">{{ row.dueMonth }}</el-tag>
            <span v-else>{{ row.dueMonth || '-' }}</span>
          </template>
        </el-table-column>
        <!-- 报告年度 -->
        <el-table-column prop="reportYear" label="报告年度" width="110" sortable>
          <template #default="{ row }">
            <el-input-number v-if="isEditing(row.id, 'reportYear')" v-model="row.reportYear" size="small" :min="2020" :max="2035" controls-position="right" style="width:100%" @change="onCellSave(row, 'reportYear')" @blur="onCellSave(row, 'reportYear')" v-focus />
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'reportYear')">{{ row.reportYear }}</span>
          </template>
        </el-table-column>
        <!-- 状态 -->
        <el-table-column prop="status" label="状态" width="110" sortable>
          <template #default="{ row }">
            <el-select v-if="isEditing(row.id, 'status')" v-model="row.status" size="small" @change="onCellSave(row, 'status')" v-focus>
              <el-option v-for="s in statusOptions" :key="s" :label="s" :value="s" />
            </el-select>
            <el-tag v-else :type="statusTag(row.status)" class="editable-cell" @click="onCellEdit(row.id, 'status')">{{ row.status }}</el-tag>
          </template>
        </el-table-column>
        <!-- 提交日期 -->
        <el-table-column prop="submitDate" label="提交日期" width="130">
          <template #default="{ row }">
            <el-date-picker v-if="isEditing(row.id, 'submitDate')" v-model="row.submitDate" type="date" size="small" value-format="YYYY-MM-DD" clearable style="width:100%" @change="onCellSave(row, 'submitDate')" @blur="onCellSave(row, 'submitDate')" v-focus />
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'submitDate')">{{ row.submitDate ? row.submitDate.slice(0, 10) : '-' }}</span>
          </template>
        </el-table-column>
        <!-- 备注 -->
        <el-table-column prop="remarks" label="备注" min-width="150" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="isEditing(row.id, 'remarks')" v-model="row.remarks" size="small" @blur="onCellSave(row, 'remarks')" @keyup.enter="onCellSave(row, 'remarks')" v-focus />
            <span v-else class="editable-cell" @click="onCellEdit(row.id, 'remarks')">{{ row.remarks || '-' }}</span>
          </template>
        </el-table-column>
        <!-- 操作 -->
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="onOpenDetail(row)">详情</el-button>
            <el-button link @click="goToProjectPage(row)">项目页</el-button>
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
          @size-change="(size: number) => { query.size = size; query.page = 1; loadData() }"
          @current-change="(page: number) => { query.page = page; loadData() }"
        />
      </div>
    </el-card>

    <el-dialog v-model="detailVisible" title="年度报告详情" width="720px" destroy-on-close>
      <template v-if="detailItem">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="机会号">{{ detailItem.opportunityNumber }}</el-descriptions-item>
          <el-descriptions-item label="医院">{{ detailItem.hospitalName }}</el-descriptions-item>
          <el-descriptions-item label="产品">{{ detailItem.productName }}</el-descriptions-item>
          <el-descriptions-item label="省份">{{ detailItem.province }}</el-descriptions-item>
          <el-descriptions-item label="组别">{{ detailItem.groupName }}</el-descriptions-item>
          <el-descriptions-item label="服务人员">{{ detailItem.servicePerson }}</el-descriptions-item>
          <el-descriptions-item label="实施状态">{{ detailItem.implementationStatus }}</el-descriptions-item>
          <el-descriptions-item label="维护开始">{{ detailItem.maintenanceStartDate }}</el-descriptions-item>
          <el-descriptions-item label="维护结束">{{ detailItem.maintenanceEndDate }}</el-descriptions-item>
          <el-descriptions-item label="到期月份">
            <el-tag v-if="detailItem.dueMonth === currentMonth" type="warning">{{ detailItem.dueMonth }}</el-tag>
            <span v-else>{{ detailItem.dueMonth || '-' }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="报告年度">{{ detailItem.reportYear }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="statusTag(detailItem.status)">{{ detailItem.status }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="提交日期">{{ detailItem.submitDate ? detailItem.submitDate.slice(0, 10) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="备注">{{ detailItem.remarks || '-' }}</el-descriptions-item>
        </el-descriptions>

        <div class="detail-actions">
          <el-button plain @click="goToProjectPage(detailItem)">去项目台账</el-button>
          <el-button plain @click="goToPersonnelPage(detailItem)">去人员权限页</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { usePrint } from '../../composables/usePrint'
import { fetchAnnualReportList, fetchAnnualReportSummary, updateAnnualReport, exportAnnualReports } from '../../api/modules/annual-report'
import type { AnnualReportItem, AnnualReportSummary } from '../../types/annual-report'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PERSON_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import { normalizeStatusText, resolveAnnualReportStatusTag } from '../../utils/statusTag'

const { printArea } = usePrint()
const pageRef = ref<HTMLElement | null>(null)

/* ---- 自定义指令：自动聚焦 ---- */
const vFocus = { mounted: (el: HTMLElement) => { const input = el.querySelector('input') || el.querySelector('textarea'); input?.focus() } }

const loading = ref(false)
const exporting = ref(false)
const total = ref(0)
const tableData = ref<AnnualReportItem[]>([])
const allRows = ref<AnnualReportItem[]>([])
const summary = ref<AnnualReportSummary>({
  notStartedCount: 0,
  writingCount: 0,
  submittedCount: 0,
  completedCount: 0,
  thisYearCount: 0,
  dueThisMonthCount: 0,
  overdueCount: 0,
  total: 0,
})

const currentMonth = computed(() => {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
})

const query = reactive({
  status: '',
  reportYear: undefined as number | undefined,
  dueMonth: '',
  groupName: '',
  servicePerson: '',
  page: 1,
  size: 15,
})

const dueMonthPicker = ref<string>('')
const isOverdueActive = ref(false)

const route = useRoute()
const router = useRouter()
const access = useAccessControl()
const canManageAnnualReport = computed(() => access.isManager() && access.canPermission('annual-report.view'))

const detailVisible = ref(false)
const detailItem = ref<AnnualReportItem | null>(null)

/* ---- 内联编辑状态 ---- */
const editingCell = ref<{ rowId: number; col: string } | null>(null)
const savingCell = ref(false)

const isEditing = (rowId: number, col: string) => {
  if (!canManageAnnualReport.value) return false
  return editingCell.value?.rowId === rowId && editingCell.value?.col === col
}

const onCellEdit = (rowId: number, col: string) => {
  if (!canManageAnnualReport.value) return
  editingCell.value = { rowId, col }
}

const onCellSave = async (row: AnnualReportItem, col: string) => {
  if (savingCell.value) return
  editingCell.value = null

  savingCell.value = true
  try {
    const payload: Record<string, unknown> = {}
    payload[col] = (row as unknown as Record<string, unknown>)[col] ?? null
    const res = await updateAnnualReport(row.id, payload)
    Object.assign(row, res.data)
    // 同步 allRows 缓存
    const cached = allRows.value.find(r => r.id === row.id)
    if (cached && cached !== row) Object.assign(cached, res.data)
    ElMessage.success('已保存')
    // 后台刷新汇总
    loadSummary()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存失败'))
    // 刷新恢复原数据
    await loadFilterOptions()
    loadData()
  } finally {
    savingCell.value = false
  }
}

const readRouteQueryValue = (value: unknown): string => {
  if (typeof value === 'string') return value
  if (Array.isArray(value) && typeof value[0] === 'string') return value[0]
  return ''
}

const updateRouteQuery = async (patch: Record<string, string | undefined>) => {
  const nextQuery = { ...route.query }
  Object.entries(patch).forEach(([key, value]) => {
    if (value) { nextQuery[key] = value; return }
    delete nextQuery[key]
  })
  await router.replace({ path: route.path, query: nextQuery })
}

const clearRouteActionQuery = async () => {
  if (!readRouteQueryValue(route.query.action) && !readRouteQueryValue(route.query.id)) return
  await updateRouteQuery({ action: undefined, id: undefined })
}

const applyDrillQuery = () => {
  const status = readRouteQueryValue(route.query.status)
  const groupName = readRouteQueryValue(route.query.groupName)
  const servicePerson = readRouteQueryValue(route.query.servicePerson)
  const hospitalName = readRouteQueryValue(route.query.hospitalName)
  const reportYear = Number(readRouteQueryValue(route.query.reportYear))
  const dueMonth = readRouteQueryValue(route.query.dueMonth)

  if (status) query.status = normalizeStatusText(status)
  if (groupName) query.groupName = groupName
  if (servicePerson) query.servicePerson = servicePerson
  if (dueMonth) { query.dueMonth = dueMonth; dueMonthPicker.value = dueMonth }
  if (Number.isFinite(reportYear) && reportYear > 0) query.reportYear = reportYear
  if (status || groupName || servicePerson || hospitalName || dueMonth || Number.isFinite(reportYear)) query.page = 1
}

const getRouteHospitalName = () => readRouteQueryValue(route.query.hospitalName)

type AnnualReportFilterState = {
  status: string
  reportYear: number | undefined
  dueMonth: string
  groupName: string
  servicePerson: string
  page: number
  size: number
}

const statusOptions = ref<string[]>(['未开始', '编写中', '已提交', '已完成'])
const groupOptions = ref<string[]>([...GROUP_OPTIONS])
const servicePersonOptions = ref<string[]>([...PERSON_OPTIONS])
const { runInitialLoad } = useResilientLoad()

const filteredGroupOptions = computed(() => {
  const groups = allRows.value
    .filter((item) => {
      if (getRouteHospitalName() && item.hospitalName !== getRouteHospitalName()) return false
      if (query.status && !sameStatus(item.status, query.status)) return false
      if (query.dueMonth && item.dueMonth !== query.dueMonth) return false
      if (query.reportYear && item.reportYear !== query.reportYear) return false
      if (query.servicePerson && item.servicePerson !== query.servicePerson) return false
      return true
    })
    .map((item) => item.groupName)
    .filter(Boolean)
  const unique = Array.from(new Set(groups)).sort((a, b) => a.localeCompare(b, 'zh-CN'))
  return unique.length > 0 ? unique : groupOptions.value
})

const filteredServicePersonOptions = computed(() => {
  const people = allRows.value
    .filter((item) => {
      if (getRouteHospitalName() && item.hospitalName !== getRouteHospitalName()) return false
      if (query.status && !sameStatus(item.status, query.status)) return false
      if (query.dueMonth && item.dueMonth !== query.dueMonth) return false
      if (query.reportYear && item.reportYear !== query.reportYear) return false
      if (query.groupName && item.groupName !== query.groupName) return false
      return true
    })
    .map((item) => item.servicePerson)
    .filter(Boolean)
  const unique = Array.from(new Set(people)).sort((a, b) => a.localeCompare(b, 'zh-CN'))
  return unique.length > 0 ? unique : servicePersonOptions.value
})

watch(() => query.groupName, () => {
  if (query.servicePerson && !filteredServicePersonOptions.value.includes(query.servicePerson)) query.servicePerson = ''
})
watch(() => query.servicePerson, () => {
  if (query.groupName && !filteredGroupOptions.value.includes(query.groupName)) query.groupName = ''
})

const statusTag = (status: string) => resolveAnnualReportStatusTag(status)
const sameStatus = (left: string, right: string) => normalizeStatusText(left) === normalizeStatusText(right)
const isStatusActive = (status: string) => sameStatus(query.status, status)

const rowClassName = ({ row }: { row: AnnualReportItem }) => {
  if (row.dueMonth <= currentMonth.value && !sameStatus(row.status, '已完成') && !sameStatus(row.status, '已提交')) return 'due-row'
  return ''
}

const onDueMonthChange = (val: string | null) => {
  isOverdueActive.value = false
  query.dueMonth = val || ''
  dueMonthPicker.value = val || ''
  query.page = 1
  loadData()
}

const onOverdueClick = () => {
  if (isOverdueActive.value) {
    isOverdueActive.value = false
  } else {
    isOverdueActive.value = true
    query.status = ''
    query.dueMonth = ''
    dueMonthPicker.value = ''
    query.groupName = ''
    query.servicePerson = ''
  }
  query.page = 1
  loadData()
}

const goToProjectPage = (row: AnnualReportItem) => {
  void router.push({
    path: '/project/list',
    query: { groupName: row.groupName, maintenancePersonName: row.servicePerson, action: 'edit' },
  })
}

const goToPersonnelPage = (row: AnnualReportItem) => {
  void router.push({
    path: '/permission/manage',
    query: { name: row.servicePerson, groupName: row.groupName },
  })
}

const onOpenDetail = (row: AnnualReportItem, syncRoute = true) => {
  detailItem.value = row
  detailVisible.value = true
  if (syncRoute) void updateRouteQuery({ action: 'detail', id: String(row.id) })
}

const syncDetailFromRoute = () => {
  const action = readRouteQueryValue(route.query.action)
  if (action !== 'detail') return
  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) return
  const matched = tableData.value.find((item) => item.id === id) ?? allRows.value.find((item) => item.id === id)
  if (matched) onOpenDetail(matched, false)
}

const loadSummary = async () => {
  try {
    const res = await fetchAnnualReportSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载年报汇总失败'))
  }
}

const loadFilterOptions = async () => {
  try {
    const res = await fetchAnnualReportList({ page: 1, size: 100000 })
    const items = res.data.items
    allRows.value = items
    if (!items.length) return
    const groups = Array.from(new Set(items.map((item) => item.groupName).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (groups.length > 0) groupOptions.value = groups
    const statuses = Array.from(new Set(items.map((item) => normalizeStatusText(item.status)).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (statuses.length > 0) statusOptions.value = Array.from(new Set([...statusOptions.value, ...statuses]))
    const servicePeople = Array.from(new Set(items.map((item) => item.servicePerson).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (servicePeople.length > 0) servicePersonOptions.value = servicePeople
  } catch { /* swallow */ }
}

const loadData = async () => {
  loading.value = true
  try {
    const hospitalName = getRouteHospitalName()

    if (isOverdueActive.value || hospitalName) {
      // 逾期 / 医院钻取：需要客户端过滤（后端不支持这两个组合条件）
      if (allRows.value.length === 0) {
        const res = await fetchAnnualReportList({ page: 1, size: 100000 })
        allRows.value = res.data.items
      }

      const filtered = allRows.value.filter((item) => {
        if (isOverdueActive.value) {
          if (item.dueMonth > currentMonth.value || sameStatus(item.status, '已完成')) return false
        }
        if (hospitalName && item.hospitalName !== hospitalName) return false
        if (query.status && !sameStatus(item.status, query.status)) return false
        if (query.dueMonth && item.dueMonth !== query.dueMonth) return false
        if (query.reportYear && item.reportYear !== query.reportYear) return false
        if (query.groupName && item.groupName !== query.groupName) return false
        if (query.servicePerson && item.servicePerson !== query.servicePerson) return false
        return true
      })

      total.value = filtered.length
      const maxPage = Math.max(1, Math.ceil(filtered.length / (query.size <= 0 ? 15 : query.size)))
      if (query.page > maxPage) query.page = maxPage
      const page = query.page < 1 ? 1 : query.page
      const size = query.size <= 0 ? 15 : query.size
      const start = (page - 1) * size
      tableData.value = filtered.slice(start, start + size)
    } else {
      // 常规场景：服务端过滤 + 分页
      const res = await fetchAnnualReportList({
        page: query.page < 1 ? 1 : query.page,
        size: query.size <= 0 ? 15 : query.size,
        ...(query.status ? { status: query.status } : {}),
        ...(query.reportYear ? { reportYear: query.reportYear } : {}),
        ...(query.dueMonth ? { dueMonth: query.dueMonth } : {}),
        ...(query.groupName ? { groupName: query.groupName } : {}),
        ...(query.servicePerson ? { servicePerson: query.servicePerson } : {}),
      })
      tableData.value = res.data.items
      total.value = res.data.total
    }
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载年报列表失败'))
  } finally {
    loading.value = false
  }
}

const onStatClick = (status: string) => {
  isOverdueActive.value = false
  query.status = normalizeStatusText(status)
  query.dueMonth = ''
  dueMonthPicker.value = ''
  query.groupName = ''
  query.servicePerson = ''
  query.page = 1
  loadData()
}

const onSearch = () => { query.page = 1; loadData() }

const onReset = () => {
  isOverdueActive.value = false
  query.status = ''
  query.reportYear = undefined
  query.dueMonth = ''
  dueMonthPicker.value = ''
  query.groupName = ''
  query.servicePerson = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  void updateRouteQuery({ hospitalName: undefined, action: undefined, id: undefined, dueMonth: undefined })
  loadData()
}

const onExport = async () => {
  exporting.value = true
  try {
    const blob = await exportAnnualReports(query)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `年度报告-${Date.now()}.csv`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  } finally {
    exporting.value = false
  }
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<AnnualReportFilterState>({
  key: 'annual-report',
  getState: () => ({
    status: query.status,
    reportYear: query.reportYear,
    dueMonth: query.dueMonth,
    groupName: query.groupName,
    servicePerson: query.servicePerson,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.status = state.status ?? ''
    query.reportYear = typeof state.reportYear === 'number' ? state.reportYear : undefined
    query.dueMonth = state.dueMonth ?? ''
    dueMonthPicker.value = state.dueMonth ?? ''
    query.groupName = state.groupName ?? ''
    query.servicePerson = state.servicePerson ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

watch(() => [query.status, query.reportYear, query.dueMonth], () => {
  if (query.groupName && !filteredGroupOptions.value.includes(query.groupName)) query.groupName = ''
  if (query.servicePerson && !filteredServicePersonOptions.value.includes(query.servicePerson)) query.servicePerson = ''
})

const refreshLinkedData = async () => {
  await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
}

useLinkedRealtimeRefresh({ refresh: refreshLinkedData, scope: 'annual-report', intervalMs: 60000 })

watch(detailVisible, (visible) => { if (!visible) void clearRouteActionQuery() })

watch(() => route.fullPath, async () => {
  applyDrillQuery()
  await loadData()
  syncDetailFromRoute()
})

const onPrint = () => printArea(pageRef.value, '年度报告')

onMounted(async () => {
  restoreFilterState()
  applyDrillQuery()
  await runInitialLoad({
    tasks: [loadSummary, loadFilterOptions, loadData],
    retryChecks: [{ when: () => summary.value.total > 0 && total.value === 0, task: loadData }],
  })
  syncDetailFromRoute()
})
</script>

<style scoped>
.editable-cell {
  cursor: pointer;
  padding: 2px 4px;
  border-radius: 3px;
  min-height: 22px;
  display: inline-block;
  min-width: 30px;
  transition: background-color 0.2s;
}
.editable-cell:hover {
  background-color: var(--el-fill-color-light);
}
.detail-actions {
  margin-top: 16px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
:deep(.due-row) {
  background-color: #fdf6ec !important;
}
:deep(.due-row:hover > td) {
  background-color: #faecd8 !important;
}
</style>