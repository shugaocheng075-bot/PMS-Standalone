<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">项目台账</h2>
        <div class="page-subtitle">按医院、产品、组别、级别与金额统一管理项目</div>
      </div>
    </div>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="医院名称">
          <el-input v-model="query.hospitalName" placeholder="请输入医院名称" clearable @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item label="产品">
          <el-select v-model="query.productName" placeholder="全部" clearable style="width: 180px">
            <el-option v-for="product in productOptions" :key="product" :label="product" :value="product" />
          </el-select>
        </el-form-item>
        <el-form-item label="省份">
          <el-select v-model="query.province" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="province in provinceOptions" :key="province" :label="province" :value="province" />
          </el-select>
        </el-form-item>
        <el-form-item label="组别">
          <el-select v-model="query.groupName" placeholder="全部" clearable style="width: 160px">
            <el-option v-for="group in filteredGroupOptions" :key="group" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="销售">
          <el-input v-model="query.salesName" placeholder="请输入销售" clearable style="width: 160px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item label="维护人员">
          <el-input v-model="query.maintenancePersonName" placeholder="请输入维护人员" clearable style="width: 160px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item label="售后结束">
          <el-date-picker
            v-model="afterSalesEndDateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            clearable
            style="width: 280px"
          />
        </el-form-item>
        <el-form-item label="级别">
          <el-select v-model="query.hospitalLevel" placeholder="全部" clearable style="width: 120px">
            <el-option v-for="level in levelOptions" :key="level" :label="level" :value="level" />
          </el-select>
        </el-form-item>
        <el-form-item label="合同状态">
          <el-select v-model="query.contractStatus" placeholder="全部" clearable style="width: 180px">
            <el-option v-for="status in contractStatusOptions" :key="status" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
          <el-button :loading="exporting" @click="onExport">导出CSV</el-button>
          <el-button
            v-if="canManageProjects"
            type="primary"
            plain
            :disabled="selectedProjectIds.length === 0"
            @click="openBatchEdit"
          >批量编辑（{{ selectedProjectIds.length }}）</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无符合条件的数据" @selection-change="onSelectionChange" @row-dblclick="onRowDoubleClick">
        <el-table-column v-if="canManageProjects" type="selection" width="46" />
        <el-table-column prop="hospitalName" label="医院名称" min-width="220" show-overflow-tooltip sortable />
        <el-table-column prop="productName" label="产品" min-width="180" show-overflow-tooltip sortable />
        <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip sortable />
        <el-table-column prop="groupName" label="组别" width="120" show-overflow-tooltip sortable />
        <el-table-column prop="salesName" label="销售" width="120" show-overflow-tooltip />
        <el-table-column prop="maintenancePersonName" label="维护人员" width="130" show-overflow-tooltip />
        <el-table-column prop="afterSalesStartDate" label="售后开始" width="120" show-overflow-tooltip />
        <el-table-column prop="afterSalesEndDate" label="售后结束" width="120" show-overflow-tooltip />
        <el-table-column prop="hospitalLevel" label="级别" width="90" sortable />
        <el-table-column prop="contractStatus" label="合同状态" min-width="160" show-overflow-tooltip sortable>
          <template #default="scope">
            <el-tag :type="statusType(getDisplayContractStatus(scope.row))">{{ getDisplayContractStatus(scope.row) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="maintenanceAmount" label="维护金额(万)" width="130" align="right" sortable />
        <el-table-column prop="overdueDays" label="超期/剩余天数" width="130" align="right" sortable>
          <template #default="scope">
            <span :class="serviceDaysClass(scope.row)">{{ serviceDaysText(scope.row) }}</span>
          </template>
        </el-table-column>
        <el-table-column v-if="canManageProjects" label="操作" width="100" fixed="right">
          <template #default="scope">
            <el-button link type="primary" @click="onOpenEdit(scope.row)">编辑</el-button>
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

    <el-dialog v-model="batchEditVisible" title="批量编辑项目台账" width="560px" destroy-on-close>
      <el-form label-width="110px">
        <el-form-item label="合同状态">
          <el-select v-model="batchEditForm.contractStatus" clearable placeholder="不修改" style="width: 100%">
            <el-option v-for="status in contractStatusOptions" :key="`batch-status-${status}`" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item label="组别">
          <el-input v-model="batchEditForm.groupName" placeholder="不修改" clearable />
        </el-form-item>
        <el-form-item label="销售">
          <el-input v-model="batchEditForm.salesName" placeholder="不修改" clearable />
        </el-form-item>
        <el-form-item label="维护人员">
          <el-input v-model="batchEditForm.maintenancePersonName" placeholder="不修改" clearable />
        </el-form-item>
        <el-form-item label="医院级别">
          <el-select v-model="batchEditForm.hospitalLevel" clearable placeholder="不修改" style="width: 100%">
            <el-option v-for="level in levelOptions" :key="`batch-level-${level}`" :label="level" :value="level" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="batchEditVisible = false">取消</el-button>
        <el-button type="primary" :loading="batchUpdating" @click="submitBatchEdit">提交</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="editVisible" title="编辑项目台账" width="560px" destroy-on-close>
      <el-form label-width="110px">
        <el-form-item label="合同状态">
          <el-select v-model="editForm.contractStatus" clearable placeholder="请选择合同状态" style="width: 100%">
            <el-option v-for="status in contractStatusOptions" :key="`edit-status-${status}`" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item label="组别">
          <el-input v-model="editForm.groupName" clearable />
        </el-form-item>
        <el-form-item label="销售">
          <el-input v-model="editForm.salesName" clearable />
        </el-form-item>
        <el-form-item label="维护人员">
          <el-input v-model="editForm.maintenancePersonName" clearable />
        </el-form-item>
        <el-form-item label="医院级别">
          <el-select v-model="editForm.hospitalLevel" clearable placeholder="请选择医院级别" style="width: 100%">
            <el-option v-for="level in levelOptions" :key="`edit-level-${level}`" :label="level" :value="level" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="editSubmitting" @click="editVisible = false">取消</el-button>
        <el-button type="primary" :loading="editSubmitting" @click="submitEdit">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { batchUpdateProjects, exportProjects, fetchProjectList, updateProject } from '../../api/modules/project'
import type { ProjectItem } from '../../types/project'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'

const loading = ref(false)
const exporting = ref(false)
const batchUpdating = ref(false)
const editSubmitting = ref(false)
const total = ref(0)
const tableData = ref<ProjectItem[]>([])
const selectedProjectIds = ref<number[]>([])
const batchEditVisible = ref(false)
const editVisible = ref(false)
const editingId = ref<number | null>(null)

const batchEditForm = reactive({
  contractStatus: '',
  groupName: '',
  salesName: '',
  maintenancePersonName: '',
  hospitalLevel: '',
})

const editForm = reactive({
  contractStatus: '',
  groupName: '',
  salesName: '',
  maintenancePersonName: '',
  hospitalLevel: '',
})

const access = useAccessControl()
const canManageProjects = computed(() => {
  if (!access.canPermission('project.manage')) {
    return false
  }

  return access.isManager() || access.isSupervisor()
})

const query = reactive({
  hospitalName: '',
  productName: '',
  province: '',
  groupName: '',
  salesName: '',
  maintenancePersonName: '',
  afterSalesEndDateFrom: '',
  afterSalesEndDateTo: '',
  hospitalLevel: '',
  contractStatus: '',
  page: 1,
  size: 15,
})
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

const afterSalesEndDateRange = ref<string[]>([])

type ProjectFilterState = {
  hospitalName: string
  productName: string
  province: string
  groupName: string
  salesName: string
  maintenancePersonName: string
  afterSalesEndDateFrom: string
  afterSalesEndDateTo: string
  afterSalesEndDateRange: string[]
  hospitalLevel: string
  contractStatus: string
  page: number
  size: number
}

const applyDrillQuery = () => {
  const contractStatus = readRouteQueryValue(route.query.contractStatus)
  const groupName = readRouteQueryValue(route.query.groupName)
  const hospitalName = readRouteQueryValue(route.query.hospitalName)
  const productName = readRouteQueryValue(route.query.productName)
  const salesName = readRouteQueryValue(route.query.salesName)
  const maintenancePersonName = readRouteQueryValue(route.query.maintenancePersonName)
  let applied = false

  if (hospitalName) {
    query.hospitalName = hospitalName
    applied = true
  }

  if (groupName) {
    query.groupName = groupName
    applied = true
  }

  if (productName) {
    query.productName = productName
    applied = true
  }

  if (salesName) {
    query.salesName = salesName
    applied = true
  }

  if (maintenancePersonName) {
    query.maintenancePersonName = maintenancePersonName
    applied = true
  }

  if (!contractStatus) {
    if (applied) {
      query.page = 1
    }
    return
  }

  query.hospitalName = ''
  query.productName = ''
  query.province = ''
  query.groupName = ''
  query.salesName = ''
  query.maintenancePersonName = ''
  query.afterSalesEndDateFrom = ''
  query.afterSalesEndDateTo = ''
  afterSalesEndDateRange.value = []
  query.hospitalLevel = ''
  query.contractStatus = contractStatus
  query.page = 1
}

watch(
  afterSalesEndDateRange,
  (value) => {
    query.afterSalesEndDateFrom = value?.[0] ?? ''
    query.afterSalesEndDateTo = value?.[1] ?? ''
  },
  { deep: true },
)

const provinceOptions = ref<string[]>([...PROVINCE_OPTIONS])
const groupOptionsByProvince = ref<Record<string, string[]>>({})
const productOptions = ref<string[]>([])
const levelOptions = ref<string[]>([])
const contractStatusOptions = ref<string[]>([
  '合同已签署',
  '超期未签署',
  '免费维护期',
  '停止维护',
  '未知',
])

const normalizeContractStatus = (status: string, overdueDays = 0): string => {
  const normalized = status.trim()
  if (!normalized || normalized === '未知') {
    return overdueDays > 0 ? '超期未签署' : '未知'
  }

  if (normalized.includes('停止')) return '停止维护'
  if (normalized.includes('免费')) return '免费维护期'
  if (normalized.includes('超期') || normalized.includes('到期') || normalized.includes('停保') || normalized.includes('脱保')) return '超期未签署'
  if (normalized.includes('签署') || normalized.includes('签订')) return '合同已签署'
  return normalized
}

const allGroupOptions = computed(() => {
  const groups = Object.values(groupOptionsByProvince.value).flat()
  return groups.length > 0 ? Array.from(new Set(groups)) : GROUP_OPTIONS
})

const filteredGroupOptions = computed(() => {
  if (!query.province) {
    return allGroupOptions.value
  }

  const groups = groupOptionsByProvince.value[query.province]
  return groups && groups.length > 0 ? groups : allGroupOptions.value
})

watch(
  () => query.province,
  () => {
    if (query.groupName && !filteredGroupOptions.value.includes(query.groupName)) {
      query.groupName = ''
    }
  },
)

const loadFilterOptions = async () => {
  try {
    const res = await fetchProjectList({ page: 1, size: 100000 })
    const items = res.data.items

    if (!items.length) {
      return
    }

    const provinces = Array.from(new Set(items.map((item) => item.province).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (provinces.length > 0) {
      provinceOptions.value = provinces
    }

    const products = Array.from(new Set(items.map((item) => item.productName).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (products.length > 0) {
      productOptions.value = products
    }

    const levels = Array.from(new Set(items.map((item) => item.hospitalLevel).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (levels.length > 0) {
      levelOptions.value = levels
    }

    const statuses = Array.from(new Set(items
      .map((item) => normalizeContractStatus(item.contractStatus ?? '', item.overdueDays))
      .filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    contractStatusOptions.value = Array.from(new Set([...contractStatusOptions.value, ...statuses]))

    const map: Record<string, string[]> = {}
    for (const item of items) {
      if (!item.province || !item.groupName) continue

      const province = item.province
      const groupName = item.groupName
      const groups = map[province] ?? (map[province] = [])

      if (!groups.includes(groupName)) {
        groups.push(groupName)
      }
    }

    groupOptionsByProvince.value = Object.fromEntries(
      Object.entries(map).map(([province, groups]) => [province, groups.sort((a, b) => a.localeCompare(b, 'zh-CN'))]),
    )
  } catch {
    if (!productOptions.value.length) {
      productOptions.value = ['住院电子病历V6', '临床路径V6', 'CDSS', '病案归档', 'AI内涵质控']
    }
    if (!levelOptions.value.length) {
      levelOptions.value = ['三级', '二级', '一级', '未评级']
    }
    if (!contractStatusOptions.value.length) {
      contractStatusOptions.value = ['合同已签署', '超期未签署', '免费维护期', '维护超期未签署', '维护合同已签署', '停止维护', '未知']
    }
  }
}

const statusType = (status: string) => {
  if (status.includes('超期')) return 'danger'
  if (status.includes('签署')) return 'success'
  if (status.includes('免费')) return 'info'
  return 'warning'
}

const parseDateText = (text: string): Date | null => {
  if (!text) {
    return null
  }

  const matched = text.match(/^(\d{4})-(\d{1,2})-(\d{1,2})$/)
  if (!matched) {
    return null
  }

  const year = Number(matched[1])
  const month = Number(matched[2])
  const day = Number(matched[3])
  const date = new Date(year, month - 1, day)
  return Number.isNaN(date.getTime()) ? null : date
}

const getDayDiffFromToday = (dateText: string): number | null => {
  const target = parseDateText(dateText)
  if (!target) {
    return null
  }

  const today = new Date()
  const current = new Date(today.getFullYear(), today.getMonth(), today.getDate())
  const diff = target.getTime() - current.getTime()
  return Math.floor(diff / (24 * 60 * 60 * 1000))
}

const getDisplayOverdueDays = (item: ProjectItem): number => {
  if (item.overdueDays > 0) {
    return item.overdueDays
  }

  const dayDiff = getDayDiffFromToday(item.afterSalesEndDate)
  if (dayDiff === null || dayDiff >= 0) {
    return 0
  }

  return Math.abs(dayDiff)
}

const getDisplayContractStatus = (item: ProjectItem): string => {
  return normalizeContractStatus(item.contractStatus ?? '', getDisplayOverdueDays(item))
}

const serviceDaysText = (item: ProjectItem): string => {
  const displayOverdueDays = getDisplayOverdueDays(item)
  if (displayOverdueDays > 0) {
    return `超期${displayOverdueDays}天`
  }

  const dayDiff = getDayDiffFromToday(item.afterSalesEndDate)
  if (dayDiff === null) {
    return '0'
  }

  return `剩余${dayDiff}天`
}

const serviceDaysClass = (item: ProjectItem): string => {
  const displayOverdueDays = getDisplayOverdueDays(item)
  if (displayOverdueDays > 90) {
    return 'text-danger'
  }

  if (displayOverdueDays > 0) {
    return 'text-warning'
  }

  const dayDiff = getDayDiffFromToday(item.afterSalesEndDate)
  if (dayDiff !== null && dayDiff <= 30) {
    return 'text-warning'
  }

  return 'text-success'
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchProjectList(query)
    tableData.value = res.data.items
    total.value = res.data.total
    selectedProjectIds.value = selectedProjectIds.value.filter((id) => tableData.value.some((item) => item.id === id))
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载项目列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onSelectionChange = (selection: ProjectItem[]) => {
  selectedProjectIds.value = selection.map((item) => item.id)
}

const onExport = async () => {
  exporting.value = true
  try {
    const blob = await exportProjects(query)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `projects-${Date.now()}.csv`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出项目台账失败，请稍后重试'))
  } finally {
    exporting.value = false
  }
}

const openBatchEdit = () => {
  batchEditForm.contractStatus = ''
  batchEditForm.groupName = ''
  batchEditForm.salesName = ''
  batchEditForm.maintenancePersonName = ''
  batchEditForm.hospitalLevel = ''
  batchEditVisible.value = true
}

const openEditDialog = (row: ProjectItem) => {
  editingId.value = row.id
  editForm.contractStatus = getDisplayContractStatus(row)
  editForm.groupName = row.groupName ?? ''
  editForm.salesName = row.salesName ?? ''
  editForm.maintenancePersonName = row.maintenancePersonName ?? ''
  editForm.hospitalLevel = row.hospitalLevel ?? ''
  editVisible.value = true
}

const onOpenEdit = (row: ProjectItem) => {
  if (!canManageProjects.value) {
    return
  }

  openEditDialog(row)
  void updateRouteQuery({ action: 'edit', id: String(row.id) })
}

const onRowDoubleClick = (row: ProjectItem) => {
  if (!canManageProjects.value) {
    return
  }

  onOpenEdit(row)
}

const syncEditDialogFromRoute = () => {
  if (!canManageProjects.value) {
    return
  }

  const action = readRouteQueryValue(route.query.action)
  if (action !== 'edit') {
    return
  }

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) {
    const singleRow = tableData.value.length === 1 ? tableData.value[0] : null
    if (singleRow) {
      openEditDialog(singleRow)
    }
    return
  }

  const matched = tableData.value.find((item) => item.id === id)
  if (matched) {
    openEditDialog(matched)
  }
}

const submitEdit = async () => {
  if (!editingId.value) {
    return
  }

  const payload = {
    contractStatus: editForm.contractStatus.trim() || undefined,
    groupName: editForm.groupName.trim() || undefined,
    salesName: editForm.salesName.trim() || undefined,
    maintenancePersonName: editForm.maintenancePersonName.trim() || undefined,
    hospitalLevel: editForm.hospitalLevel.trim() || undefined,
  }

  if (!payload.contractStatus && !payload.groupName && !payload.salesName && !payload.maintenancePersonName && !payload.hospitalLevel) {
    ElMessage.warning('请至少填写一个需要修改的字段')
    return
  }

  editSubmitting.value = true
  try {
    await updateProject(editingId.value, payload)
    ElMessage.success('更新成功')
    editVisible.value = false
    notifyDataChanged('project')
    await loadData()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '更新项目失败，请稍后重试'))
  } finally {
    editSubmitting.value = false
  }
}

const submitBatchEdit = async () => {
  const payload = {
    projectIds: selectedProjectIds.value,
    contractStatus: batchEditForm.contractStatus.trim() || undefined,
    groupName: batchEditForm.groupName.trim() || undefined,
    salesName: batchEditForm.salesName.trim() || undefined,
    maintenancePersonName: batchEditForm.maintenancePersonName.trim() || undefined,
    hospitalLevel: batchEditForm.hospitalLevel.trim() || undefined,
  }

  if (!payload.contractStatus && !payload.groupName && !payload.salesName && !payload.maintenancePersonName && !payload.hospitalLevel) {
    ElMessage.warning('请至少填写一个需要批量修改的字段')
    return
  }

  batchUpdating.value = true
  try {
    const res = await batchUpdateProjects(payload)
    ElMessage.success(res.data.message ?? '批量更新成功')
    batchEditVisible.value = false
    notifyDataChanged('project')
    await loadData()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '批量更新失败，请稍后重试'))
  } finally {
    batchUpdating.value = false
  }
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.hospitalName = ''
  query.productName = ''
  query.province = ''
  query.groupName = ''
  query.salesName = ''
  query.maintenancePersonName = ''
  query.afterSalesEndDateFrom = ''
  query.afterSalesEndDateTo = ''
  afterSalesEndDateRange.value = []
  query.hospitalLevel = ''
  query.contractStatus = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  loadData()
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<ProjectFilterState>({
  key: 'project-list',
  getState: () => ({
    hospitalName: query.hospitalName,
    productName: query.productName,
    province: query.province,
    groupName: query.groupName,
    salesName: query.salesName,
    maintenancePersonName: query.maintenancePersonName,
    afterSalesEndDateFrom: query.afterSalesEndDateFrom,
    afterSalesEndDateTo: query.afterSalesEndDateTo,
    afterSalesEndDateRange: afterSalesEndDateRange.value,
    hospitalLevel: query.hospitalLevel,
    contractStatus: query.contractStatus,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.hospitalName = state.hospitalName ?? ''
    query.productName = state.productName ?? ''
    query.province = state.province ?? ''
    query.groupName = state.groupName ?? ''
    query.salesName = state.salesName ?? ''
    query.maintenancePersonName = state.maintenancePersonName ?? ''
    query.afterSalesEndDateFrom = state.afterSalesEndDateFrom ?? ''
    query.afterSalesEndDateTo = state.afterSalesEndDateTo ?? ''
    query.hospitalLevel = state.hospitalLevel ?? ''
    query.contractStatus = state.contractStatus ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15

    if (Array.isArray(state.afterSalesEndDateRange)) {
      afterSalesEndDateRange.value = state.afterSalesEndDateRange.filter((item): item is string => typeof item === 'string').slice(0, 2)
    } else if (query.afterSalesEndDateFrom && query.afterSalesEndDateTo) {
      afterSalesEndDateRange.value = [query.afterSalesEndDateFrom, query.afterSalesEndDateTo]
    }
  },
})

const refreshLinkedData = async () => {
  await Promise.allSettled([loadFilterOptions(), loadData()])
}

const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: refreshLinkedData,
  scope: 'project',
  intervalMs: 60000,
})

watch(editVisible, (visible) => {
  if (!visible) {
    void clearRouteActionQuery()
  }
})

watch(() => route.fullPath, () => {
  applyDrillQuery()
  syncEditDialogFromRoute()
})

onMounted(async () => {
  restoreFilterState()
  applyDrillQuery()
  await refreshLinkedData()
  syncEditDialogFromRoute()
})
</script>

<style scoped>
</style>
