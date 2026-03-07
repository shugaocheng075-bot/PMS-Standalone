<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">年度报告</h2>
        <div class="page-subtitle">跟踪年度服务报告进度与提交情况</div>
      </div>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '未开始' }" @click="onStatClick('未开始')"><div class="t">未开始</div><div class="v">{{ summary.notStartedCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '编写中' }" @click="onStatClick('编写中')"><div class="t">编写中</div><div class="v warning">{{ summary.writingCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '已提交' }" @click="onStatClick('已提交')"><div class="t">已提交</div><div class="v info">{{ summary.submittedCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '已完成' }" @click="onStatClick('已完成')"><div class="t">已完成</div><div class="v success">{{ summary.completedCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">本年度</div><div class="v">{{ summary.thisYearCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '' }" @click="onStatClick('')"><div class="t">总数</div><div class="v">{{ summary.total }}</div></el-card></el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="状态">
          <el-select v-model="query.status" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="status in statusOptions" :key="status" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item label="年度">
          <el-input-number v-model="query.reportYear" :min="2020" :max="2035" controls-position="right" />
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
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无符合条件的数据" @row-dblclick="onRowDoubleClick">
        <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip sortable />
        <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip sortable />
        <el-table-column prop="groupName" label="组别" width="120" show-overflow-tooltip />
        <el-table-column prop="servicePerson" label="服务人员" width="110" show-overflow-tooltip />
        <el-table-column prop="reportYear" label="年度" width="90" sortable />
        <el-table-column prop="status" label="状态" width="100" sortable>
          <template #default="scope">
            <el-tag :type="statusTag(scope.row.status)">{{ scope.row.status }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="submitDate" label="提交日期" width="120">
          <template #default="scope">{{ scope.row.submitDate ? formatDate(scope.row.submitDate) : '-' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="scope">
            <el-button v-if="canManageAnnualReport" link type="primary" @click="onOpenEdit(scope.row)">编辑</el-button>
            <el-button link type="primary" @click="onOpenDetail(scope.row)">详情</el-button>
            <el-button link @click="goToProjectPage(scope.row)">项目页</el-button>
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
          @size-change="(size: number) => { query.size = size; query.page = 1; loadData() }"
          @current-change="(page: number) => { query.page = page; loadData() }"
        />
      </div>
    </el-card>

    <el-dialog v-model="detailVisible" title="年度报告详情" width="680px">
      <template v-if="detailItem">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="医院">{{ detailItem.hospitalName }}</el-descriptions-item>
          <el-descriptions-item label="省份">{{ detailItem.province }}</el-descriptions-item>
          <el-descriptions-item label="组别">{{ detailItem.groupName }}</el-descriptions-item>
          <el-descriptions-item label="服务人员">{{ detailItem.servicePerson }}</el-descriptions-item>
          <el-descriptions-item label="年度">{{ detailItem.reportYear }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="statusTag(detailItem.status)">{{ detailItem.status }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="提交日期">{{ detailItem.submitDate ? formatDate(detailItem.submitDate) : '-' }}</el-descriptions-item>
        </el-descriptions>

        <div class="detail-actions">
          <el-button v-if="canManageAnnualReport" plain type="primary" @click="onOpenEdit(detailItem)">编辑报告</el-button>
          <el-button plain @click="goToProjectPage(detailItem)">去项目台账</el-button>
          <el-button plain @click="goToPersonnelPage(detailItem)">去人员权限页</el-button>
        </div>
      </template>
    </el-dialog>

    <el-dialog v-model="editVisible" title="编辑年度报告" width="620px">
      <el-form label-width="100px">
        <el-form-item label="组别">
          <el-select v-model="editForm.groupName" clearable filterable style="width: 100%">
            <el-option v-for="group in filteredGroupOptions" :key="`edit-group-${group}`" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="服务人员">
          <el-select v-model="editForm.servicePerson" clearable filterable style="width: 100%">
            <el-option v-for="person in filteredServicePersonOptions" :key="`edit-person-${person}`" :label="person" :value="person" />
          </el-select>
        </el-form-item>
        <el-form-item label="年度">
          <el-input-number v-model="editForm.reportYear" :min="2020" :max="2035" controls-position="right" style="width: 100%" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="editForm.status" style="width: 100%">
            <el-option v-for="status in statusOptions" :key="`edit-status-${status}`" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item label="提交日期">
          <el-date-picker v-model="editForm.submitDate" type="date" value-format="YYYY-MM-DD" clearable style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="submitEdit">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { fetchAnnualReportList, fetchAnnualReportSummary, updateAnnualReport } from '../../api/modules/annual-report'
import type { AnnualReportItem, AnnualReportSummary, AnnualReportUpsert } from '../../types/annual-report'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PERSON_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'

const loading = ref(false)
const total = ref(0)
const tableData = ref<AnnualReportItem[]>([])
const allRows = ref<AnnualReportItem[]>([])
const summary = ref<AnnualReportSummary>({
  notStartedCount: 0,
  writingCount: 0,
  submittedCount: 0,
  completedCount: 0,
  thisYearCount: 0,
  total: 0,
})

const query = reactive({
  status: '',
  reportYear: new Date().getFullYear(),
  groupName: '',
  servicePerson: '',
  page: 1,
  size: 15,
})
const route = useRoute()
const router = useRouter()
const access = useAccessControl()
const canManageAnnualReport = computed(() => access.isManager() && access.canPermission('annual-report.view'))
const detailVisible = ref(false)
const detailItem = ref<AnnualReportItem | null>(null)
const editVisible = ref(false)
const submitting = ref(false)
const editingId = ref<number | null>(null)
const editForm = reactive<AnnualReportUpsert>({
  groupName: '',
  servicePerson: '',
  reportYear: new Date().getFullYear(),
  status: '未开始',
  submitDate: null,
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

const applyDrillQuery = () => {
  const status = readRouteQueryValue(route.query.status)
  const groupName = readRouteQueryValue(route.query.groupName)
  const servicePerson = readRouteQueryValue(route.query.servicePerson)
  const hospitalName = readRouteQueryValue(route.query.hospitalName)
  const reportYear = Number(readRouteQueryValue(route.query.reportYear))

  if (status) {
    query.status = status
  }

  if (groupName) {
    query.groupName = groupName
  }

  if (servicePerson) {
    query.servicePerson = servicePerson
  }

  if (hospitalName) {
    query.page = 1
  }

  if (Number.isFinite(reportYear) && reportYear > 0) {
    query.reportYear = reportYear
  }

  if (status || groupName || servicePerson || hospitalName || Number.isFinite(reportYear)) {
    query.page = 1
  }
}

const getRouteHospitalName = () => readRouteQueryValue(route.query.hospitalName)

type AnnualReportFilterState = {
  status: string
  reportYear: number
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
      if (query.status && item.status !== query.status) return false
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
      if (query.status && item.status !== query.status) return false
      if (query.reportYear && item.reportYear !== query.reportYear) return false
      if (query.groupName && item.groupName !== query.groupName) return false
      return true
    })
    .map((item) => item.servicePerson)
    .filter(Boolean)

  const unique = Array.from(new Set(people)).sort((a, b) => a.localeCompare(b, 'zh-CN'))
  return unique.length > 0 ? unique : servicePersonOptions.value
})

watch(
  () => query.groupName,
  () => {
    if (query.servicePerson && !filteredServicePersonOptions.value.includes(query.servicePerson)) {
      query.servicePerson = ''
    }
  },
)

watch(
  () => query.servicePerson,
  () => {
    if (query.groupName && !filteredGroupOptions.value.includes(query.groupName)) {
      query.groupName = ''
    }
  },
)

const formatDate = (value: string) => value.slice(0, 10)

const statusTag = (status: string) => {
  if (status === '已完成') return 'success'
  if (status === '编写中') return 'warning'
  if (status === '已提交') return 'info'
  return 'danger'
}

const goToProjectPage = (row: AnnualReportItem) => {
  void router.push({
    path: '/project/list',
    query: {
      groupName: row.groupName,
      maintenancePersonName: row.servicePerson,
      action: 'edit',
    },
  })
}

const goToPersonnelPage = (row: AnnualReportItem) => {
  void router.push({
    path: '/permission/manage',
    query: {
      name: row.servicePerson,
      groupName: row.groupName,
    },
  })
}

const onOpenDetail = (row: AnnualReportItem, syncRoute = true) => {
  detailItem.value = row
  detailVisible.value = true
  if (syncRoute) {
    void updateRouteQuery({ action: 'detail', id: String(row.id) })
  }
}

const onOpenEdit = (row: AnnualReportItem, syncRoute = true) => {
  editingId.value = row.id
  editForm.groupName = row.groupName
  editForm.servicePerson = row.servicePerson
  editForm.reportYear = row.reportYear
  editForm.status = row.status
  editForm.submitDate = row.submitDate ?? null
  editVisible.value = true
  if (syncRoute) {
    void updateRouteQuery({ action: 'edit', id: String(row.id) })
  }
}

const onRowDoubleClick = (row: AnnualReportItem) => {
  if (canManageAnnualReport.value) {
    onOpenEdit(row)
    return
  }

  onOpenDetail(row)
}

const submitEdit = async () => {
  if (!editingId.value) {
    return
  }

  submitting.value = true
  try {
    const res = await updateAnnualReport(editingId.value, editForm)
    ElMessage.success('年度报告更新成功')
    detailItem.value = res.data
    editVisible.value = false
    await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '更新年度报告失败'))
  } finally {
    submitting.value = false
  }
}

const syncDetailFromRoute = () => {
  const action = readRouteQueryValue(route.query.action)
  if (action !== 'detail' && action !== 'edit') {
    return
  }

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) {
    return
  }

  const matched = tableData.value.find((item) => item.id === id) ?? allRows.value.find((item) => item.id === id)
  if (matched) {
    if (action === 'edit' && canManageAnnualReport.value) {
      onOpenEdit(matched, false)
      return
    }

    onOpenDetail(matched, false)
  }
}

const syncSingleActionFromFilters = () => {
  const action = readRouteQueryValue(route.query.action)
  const id = Number(readRouteQueryValue(route.query.id))
  if ((action !== 'detail' && action !== 'edit') || (Number.isFinite(id) && id > 0) || detailVisible.value || editVisible.value) {
    return
  }

  const filtered = allRows.value.filter((item) => {
    if (getRouteHospitalName() && item.hospitalName !== getRouteHospitalName()) {
      return false
    }

    if (query.status && item.status !== query.status) {
      return false
    }

    if (query.reportYear && item.reportYear !== query.reportYear) {
      return false
    }

    if (query.groupName && item.groupName !== query.groupName) {
      return false
    }

    if (query.servicePerson && item.servicePerson !== query.servicePerson) {
      return false
    }

    return true
  })

  if (filtered.length !== 1) {
    return
  }

  const matched = filtered[0]
  if (!matched) {
    return
  }

  if (action === 'edit' && canManageAnnualReport.value) {
    onOpenEdit(matched, false)
    return
  }

  onOpenDetail(matched, false)
}

const loadSummary = async () => {
  try {
    const res = await fetchAnnualReportSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载年报汇总失败，请稍后重试'))
  }
}

const loadFilterOptions = async () => {
  try {
    const res = await fetchAnnualReportList({ page: 1, size: 1000 })
    const items = res.data.items
    allRows.value = items

    if (!items.length) {
      return
    }

    const groups = Array.from(new Set(items.map((item) => item.groupName).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (groups.length > 0) {
      groupOptions.value = groups
    }

    const statuses = Array.from(new Set(items.map((item) => item.status).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (statuses.length > 0) {
      statusOptions.value = Array.from(new Set([...statusOptions.value, ...statuses]))
    }

    const servicePeople = Array.from(new Set(items.map((item) => item.servicePerson).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (servicePeople.length > 0) {
      servicePersonOptions.value = servicePeople
    }
  } catch {
  }
}

const loadData = async () => {
  if (allRows.value.length === 0) {
    loading.value = true
    try {
      const res = await fetchAnnualReportList({ page: 1, size: 1000 })
      allRows.value = res.data.items
    } catch (error) {
      tableData.value = []
      total.value = 0
      ElMessage.error(getErrorMessage(error, '加载年报列表失败，请稍后重试'))
      loading.value = false
      return
    } finally {
      loading.value = false
    }
  }

  const filtered = allRows.value.filter((item) => {
    if (getRouteHospitalName() && item.hospitalName !== getRouteHospitalName()) {
      return false
    }

    if (query.status && item.status !== query.status) {
      return false
    }

    if (query.reportYear && item.reportYear !== query.reportYear) {
      return false
    }

    if (query.groupName && item.groupName !== query.groupName) {
      return false
    }

    if (query.servicePerson && item.servicePerson !== query.servicePerson) {
      return false
    }

    return true
  })

  total.value = filtered.length
  const maxPage = Math.max(1, Math.ceil(filtered.length / (query.size <= 0 ? 15 : query.size)))
  if (query.page > maxPage) {
    query.page = maxPage
  }

  const page = query.page < 1 ? 1 : query.page
  const size = query.size <= 0 ? 15 : query.size
  const start = (page - 1) * size
  tableData.value = filtered.slice(start, start + size)
}

const onStatClick = (status: string) => {
  query.status = status
  query.groupName = ''
  query.servicePerson = ''
  query.page = 1
  loadData()
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.status = ''
  query.reportYear = new Date().getFullYear()
  query.groupName = ''
  query.servicePerson = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  void updateRouteQuery({ hospitalName: undefined, action: undefined, id: undefined })
  loadData()
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<AnnualReportFilterState>({
  key: 'annual-report',
  getState: () => ({
    status: query.status,
    reportYear: query.reportYear,
    groupName: query.groupName,
    servicePerson: query.servicePerson,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.status = state.status ?? ''
    query.reportYear = typeof state.reportYear === 'number' ? state.reportYear : new Date().getFullYear()
    query.groupName = state.groupName ?? ''
    query.servicePerson = state.servicePerson ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

watch(
  () => [query.status, query.reportYear],
  () => {
    if (query.groupName && !filteredGroupOptions.value.includes(query.groupName)) {
      query.groupName = ''
    }
    if (query.servicePerson && !filteredServicePersonOptions.value.includes(query.servicePerson)) {
      query.servicePerson = ''
    }
  },
)

const refreshLinkedData = async () => {
  await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
}

useLinkedRealtimeRefresh({
  refresh: refreshLinkedData,
  scope: 'annual-report',
  intervalMs: 60000,
})

watch(detailVisible, (visible) => {
  if (!visible) {
    void clearRouteActionQuery()
  }
})

watch(editVisible, (visible) => {
  if (!visible && !detailVisible.value) {
    void clearRouteActionQuery()
  }
})

watch(() => route.fullPath, async () => {
  applyDrillQuery()
  await loadData()
  syncDetailFromRoute()
  syncSingleActionFromFilters()
})

onMounted(async () => {
  restoreFilterState()
  applyDrillQuery()
  await runInitialLoad({
    tasks: [loadSummary, loadFilterOptions, loadData],
    retryChecks: [
      {
        when: () => summary.value.total > 0 && total.value === 0,
        task: loadData,
      },
    ],
  })
  syncDetailFromRoute()
  syncSingleActionFromFilters()
})
</script>

<style scoped>
.detail-actions {
  margin-top: 16px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
</style>
