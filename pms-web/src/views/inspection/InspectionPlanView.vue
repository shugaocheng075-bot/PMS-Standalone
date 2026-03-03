<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">巡检管理</h2>
        <div class="page-subtitle">管理巡检排期、执行状态与实际巡检结果</div>
      </div>
    </div>

    <el-tabs v-model="activeTab" class="main-tabs" @tab-change="onTabChange">
      <!-- ===================== 巡检计划 Tab ===================== -->
      <el-tab-pane label="巡检计划" name="plan">
        <el-row :gutter="16" class="stats-row">
          <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '已计划' }" @click="onStatClick('已计划')"><div class="t">已计划</div><div class="v">{{ summary.plannedCount }}</div></el-card></el-col>
          <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '执行中' }" @click="onStatClick('执行中')"><div class="t">执行中</div><div class="v warning">{{ summary.inProgressCount }}</div></el-card></el-col>
          <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '已完成' }" @click="onStatClick('已完成')"><div class="t">已完成</div><div class="v success">{{ summary.completedCount }}</div></el-card></el-col>
          <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '已取消' }" @click="onStatClick('已取消')"><div class="t">已取消</div><div class="v danger">{{ summary.cancelledCount }}</div></el-card></el-col>
          <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">本月计划</div><div class="v">{{ summary.thisMonthCount }}</div></el-card></el-col>
          <el-col :span="4"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '' }" @click="onStatClick('')"><div class="t">总数</div><div class="v">{{ summary.total }}</div></el-card></el-col>
        </el-row>

        <el-card shadow="never" class="filter-card">
          <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
            <el-form-item label="状态">
              <el-select v-model="query.status" clearable style="width: 140px" placeholder="全部">
                <el-option v-for="status in statusOptions" :key="status" :label="status" :value="status" />
              </el-select>
            </el-form-item>
            <el-form-item label="省份">
              <el-select v-model="query.province" clearable style="width: 140px" placeholder="全部">
                <el-option v-for="province in provinceOptions" :key="province" :label="province" :value="province" />
              </el-select>
            </el-form-item>
            <el-form-item label="产品">
              <el-select
                v-model="query.productName"
                clearable
                filterable
                default-first-option
                style="width: 180px"
                placeholder="全部"
              >
                <el-option v-for="product in productOptions" :key="product" :label="product" :value="product" />
              </el-select>
            </el-form-item>
            <el-form-item label="组别">
              <el-select v-model="query.groupName" clearable style="width: 160px" placeholder="全部">
                <el-option v-for="group in filteredGroupOptions" :key="group" :label="group" :value="group" />
              </el-select>
            </el-form-item>
            <el-form-item label="巡检人">
              <el-select v-model="query.inspector" clearable style="width: 140px" placeholder="全部">
                <el-option v-for="person in inspectorOptions" :key="person" :label="person" :value="person" />
              </el-select>
            </el-form-item>
            <el-form-item class="filter-actions">
              <el-button type="primary" @click="onSearch">查询</el-button>
              <el-button @click="onReset">重置</el-button>
            </el-form-item>
          </el-form>
        </el-card>

        <el-card shadow="never" class="table-card">
          <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无符合条件的数据">
            <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip sortable />
            <el-table-column prop="productName" label="产品" min-width="180" show-overflow-tooltip sortable />
            <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip sortable />
            <el-table-column prop="groupName" label="组别" width="120" show-overflow-tooltip />
            <el-table-column prop="inspector" label="巡检人" width="100" show-overflow-tooltip />
            <el-table-column prop="inspectionType" label="方式" width="90" />
            <el-table-column prop="planDate" label="计划日期" width="120" sortable>
              <template #default="scope">{{ formatDate(scope.row.planDate) }}</template>
            </el-table-column>
            <el-table-column prop="actualDate" label="实际日期" width="120">
              <template #default="scope">{{ scope.row.actualDate ? formatDate(scope.row.actualDate) : '-' }}</template>
            </el-table-column>
            <el-table-column prop="status" label="状态" width="100" sortable>
              <template #default="scope">
                <el-tag :type="statusTag(scope.row.status)">{{ scope.row.status }}</el-tag>
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
      </el-tab-pane>

      <!-- ===================== 巡检结果 Tab ===================== -->
      <el-tab-pane label="巡检结果" name="results">
        <!-- 结果统计概览 -->
        <el-row :gutter="16" class="stats-row">
          <el-col :span="6">
            <el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: resultQuery.healthLevel === '' }" @click="onResultHealthClick('')">
              <div class="t">全部结果</div><div class="v">{{ resultTotal }}</div>
            </el-card>
          </el-col>
          <el-col :span="6">
            <el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: resultQuery.healthLevel === '良好' }" @click="onResultHealthClick('良好')">
              <div class="t">良好</div><div class="v success">{{ resultHealthCounts.good }}</div>
            </el-card>
          </el-col>
          <el-col :span="6">
            <el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: resultQuery.healthLevel === '警告' }" @click="onResultHealthClick('警告')">
              <div class="t">警告</div><div class="v warning">{{ resultHealthCounts.warning }}</div>
            </el-card>
          </el-col>
          <el-col :span="6">
            <el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: resultQuery.healthLevel === '严重' }" @click="onResultHealthClick('严重')">
              <div class="t">严重</div><div class="v danger">{{ resultHealthCounts.critical }}</div>
            </el-card>
          </el-col>
        </el-row>

        <!-- 结果筛选 -->
        <el-card shadow="never" class="filter-card">
          <el-form :model="resultQuery" inline class="filter-form" @submit.prevent="onResultSearch">
            <el-form-item label="医院">
              <el-select v-model="resultQuery.hospitalName" clearable filterable style="width: 200px" placeholder="全部">
                <el-option v-for="h in resultHospitalOptions" :key="h" :label="h" :value="h" />
              </el-select>
            </el-form-item>
            <el-form-item label="产品">
              <el-select v-model="resultQuery.productName" clearable filterable style="width: 180px" placeholder="全部">
                <el-option v-for="p in resultProductOptions" :key="p" :label="p" :value="p" />
              </el-select>
            </el-form-item>
            <el-form-item label="健康等级">
              <el-select v-model="resultQuery.healthLevel" clearable style="width: 120px" placeholder="全部">
                <el-option label="良好" value="良好" />
                <el-option label="警告" value="警告" />
                <el-option label="严重" value="严重" />
              </el-select>
            </el-form-item>
            <el-form-item label="巡检人">
              <el-select v-model="resultQuery.inspector" clearable style="width: 140px" placeholder="全部">
                <el-option v-for="person in resultInspectorOptions" :key="person" :label="person" :value="person" />
              </el-select>
            </el-form-item>
            <el-form-item class="filter-actions">
              <el-button type="primary" @click="onResultSearch">查询</el-button>
              <el-button @click="onResultReset">重置</el-button>
              <el-button type="success" :loading="resultUploadLoading" @click="triggerResultUpload">上传JSON</el-button>
              <input ref="resultUploadInput" type="file" accept=".json,application/json" style="display: none" @change="onResultFileChange" />
            </el-form-item>
          </el-form>
        </el-card>

        <!-- 结果表格 -->
        <el-card shadow="never" class="table-card">
          <el-table :data="resultTableData" v-loading="resultLoading" stripe max-height="520" scrollbar-always-on empty-text="暂无巡检结果数据（SystemAuditTool 推送后将在此显示）">
            <el-table-column prop="hospitalName" label="医院" min-width="200" show-overflow-tooltip sortable />
            <el-table-column prop="productName" label="产品" min-width="160" show-overflow-tooltip sortable />
            <el-table-column prop="inspectedAt" label="巡检时间" width="170" sortable>
              <template #default="scope">{{ formatDateTime(scope.row.inspectedAt) }}</template>
            </el-table-column>
            <el-table-column prop="inspector" label="巡检人" width="100" show-overflow-tooltip />
            <el-table-column prop="healthLevel" label="健康等级" width="110" sortable>
              <template #default="scope">
                <el-tag :type="healthTag(scope.row.healthLevel)" effect="dark">{{ scope.row.healthLevel }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="overallScore" label="评分" width="90" sortable>
              <template #default="scope">
                <span :class="scoreClass(scope.row.overallScore)">{{ scope.row.overallScore }}</span>
              </template>
            </el-table-column>
            <el-table-column label="风险统计" width="150">
              <template #default="scope">
                <span v-if="scope.row.criticalCount > 0" class="risk-badge critical">严重 {{ scope.row.criticalCount }}</span>
                <span v-if="scope.row.warningCount > 0" class="risk-badge warning">警告 {{ scope.row.warningCount }}</span>
                <span v-if="scope.row.criticalCount === 0 && scope.row.warningCount === 0" class="risk-badge good">无风险</span>
              </template>
            </el-table-column>
            <el-table-column prop="databaseVersion" label="数据库版本" width="130" show-overflow-tooltip />
            <el-table-column label="存储" width="90">
              <template #default="scope">
                <span v-if="scope.row.storageUsedPercent != null" :class="storageClass(scope.row.storageUsedPercent)">
                  {{ scope.row.storageUsedPercent }}%
                </span>
                <span v-else>-</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="80" fixed="right">
              <template #default="scope">
                <el-button link type="primary" size="small" @click="showResultDetail(scope.row)">详情</el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pager">
            <el-pagination
              v-model:current-page="resultQuery.page"
              v-model:page-size="resultQuery.size"
              :page-sizes="[15]"
              layout="total, sizes, prev, pager, next"
              :total="resultTotal"
              @size-change="(size: number) => { resultQuery.size = size; resultQuery.page = 1; loadResults() }"
              @current-change="(page: number) => { resultQuery.page = page; loadResults() }"
            />
          </div>
        </el-card>

        <!-- 详情弹窗 -->
        <el-dialog v-model="detailVisible" title="巡检结果详情" width="720px" destroy-on-close>
          <template v-if="detailRow">
            <el-descriptions :column="2" border size="small" class="result-desc">
              <el-descriptions-item label="医院">{{ detailRow.hospitalName }}</el-descriptions-item>
              <el-descriptions-item label="产品">{{ detailRow.productName }}</el-descriptions-item>
              <el-descriptions-item label="巡检时间">{{ formatDateTime(detailRow.inspectedAt) }}</el-descriptions-item>
              <el-descriptions-item label="巡检人">{{ detailRow.inspector }}</el-descriptions-item>
              <el-descriptions-item label="健康等级">
                <el-tag :type="healthTag(detailRow.healthLevel)" effect="dark">{{ detailRow.healthLevel }}</el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="综合评分">
                <span :class="scoreClass(detailRow.overallScore)" style="font-weight: 700; font-size: 16px;">{{ detailRow.overallScore }}</span>
              </el-descriptions-item>
              <el-descriptions-item label="耗时">{{ detailRow.durationSeconds }}秒</el-descriptions-item>
              <el-descriptions-item label="成功">{{ detailRow.success ? '是' : '否' }}</el-descriptions-item>
              <el-descriptions-item label="数据库版本" :span="2">{{ detailRow.databaseVersion ?? '-' }}</el-descriptions-item>
              <el-descriptions-item label="存储使用率">{{ detailRow.storageUsedPercent != null ? detailRow.storageUsedPercent + '%' : '-' }}</el-descriptions-item>
              <el-descriptions-item label="表空间使用率">{{ detailRow.tablespaceUsedPercent != null ? detailRow.tablespaceUsedPercent + '%' : '-' }}</el-descriptions-item>
              <el-descriptions-item label="备份状态">{{ detailRow.backupStatus ?? '-' }}</el-descriptions-item>
              <el-descriptions-item label="预计存储满天数">{{ detailRow.daysToFull != null ? detailRow.daysToFull + '天' : '-' }}</el-descriptions-item>
            </el-descriptions>

            <div v-if="detailRow.errorMessage" style="margin-top: 12px;">
              <el-alert type="error" :closable="false" :title="'错误信息：' + detailRow.errorMessage" />
            </div>

            <div v-if="detailRow.topRisks && detailRow.topRisks.length > 0" style="margin-top: 16px;">
              <h4 style="margin-bottom: 8px;">风险项 ({{ detailRow.topRisks.length }})</h4>
              <el-table :data="detailRow.topRisks" stripe size="small" max-height="300" scrollbar-always-on>
                <el-table-column prop="level" label="级别" width="80">
                  <template #default="scope">
                    <el-tag :type="riskLevelTag(scope.row.level)" size="small">{{ scope.row.level }}</el-tag>
                  </template>
                </el-table-column>
                <el-table-column prop="category" label="分类" width="120" show-overflow-tooltip />
                <el-table-column prop="title" label="标题" min-width="180" show-overflow-tooltip />
                <el-table-column prop="currentValue" label="当前值" width="120" show-overflow-tooltip />
                <el-table-column prop="thresholdValue" label="阈值" width="120" show-overflow-tooltip />
              </el-table>
            </div>
          </template>
        </el-dialog>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute } from 'vue-router'
import {
  fetchInspections,
  fetchInspectionResults,
  fetchInspectionSummary,
  submitInspectionResults,
  type InspectionResultSubmitItem,
} from '../../api/modules/inspection'
import type { InspectionPlanItem, InspectionResult, InspectionSummary } from '../../types/inspection'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PERSON_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

// ---- Tab 切换 ----
const activeTab = ref('plan')

const loading = ref(false)
const total = ref(0)
const tableData = ref<InspectionPlanItem[]>([])
const summary = ref<InspectionSummary>({
  plannedCount: 0,
  inProgressCount: 0,
  completedCount: 0,
  cancelledCount: 0,
  thisMonthCount: 0,
  total: 0,
})

const query = reactive({
  status: '',
  province: '',
  productName: '',
  groupName: '',
  inspector: '',
  page: 1,
  size: 15,
})
const route = useRoute()

const readRouteQueryValue = (value: unknown): string => {
  if (typeof value === 'string') {
    return value
  }

  if (Array.isArray(value) && typeof value[0] === 'string') {
    return value[0]
  }

  return ''
}

const applyDrillQuery = () => {
  const status = readRouteQueryValue(route.query.status)
  if (!status) {
    return
  }

  query.province = ''
  query.productName = ''
  query.groupName = ''
  query.inspector = ''
  query.status = status
  query.page = 1
}

type InspectionFilterState = {
  status: string
  province: string
  productName: string
  groupName: string
  inspector: string
  page: number
  size: number
}

const statusOptions = ref<string[]>(['已计划', '执行中', '已完成', '已取消'])
const provinceOptions = ref<string[]>([...PROVINCE_OPTIONS])
const productOptions = ref<string[]>([])
const groupOptionsByProvince = ref<Record<string, string[]>>({})
const inspectorOptions = ref<string[]>([...PERSON_OPTIONS])

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
    const res = await fetchInspections({ page: 1, size: 1000 })
    const items = res.data.items

    if (!items.length) {
      return
    }

    const provinces = Array.from(new Set(items.map((item) => item.province).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (provinces.length > 0) {
      provinceOptions.value = provinces
    }

    const statuses = Array.from(new Set(items.map((item) => item.status).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (statuses.length > 0) {
      statusOptions.value = Array.from(new Set([...statusOptions.value, ...statuses]))
    }

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

    const products = Array.from(new Set(items.map((item) => item.productName).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (products.length > 0) {
      productOptions.value = products
    }

    const inspectors = Array.from(new Set(items.map((item) => item.inspector).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (inspectors.length > 0) {
      inspectorOptions.value = inspectors
    }
  } catch {
  }
}
const { runInitialLoad } = useResilientLoad()

const formatDate = (value: string) => value.slice(0, 10)

const statusTag = (status: string) => {
  if (status === '已完成') return 'success'
  if (status === '执行中') return 'warning'
  if (status === '已取消') return 'danger'
  return 'info'
}

const loadSummary = async () => {
  try {
    const res = await fetchInspectionSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载巡检汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchInspections(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载巡检列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onStatClick = (status: string) => {
  query.province = ''
  query.productName = ''
  query.groupName = ''
  query.inspector = ''
  query.status = status
  query.page = 1
  loadData()
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.status = ''
  query.province = ''
  query.productName = ''
  query.groupName = ''
  query.inspector = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  loadData()
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<InspectionFilterState>({
  key: 'inspection-plan',
  getState: () => ({
    status: query.status,
    province: query.province,
    productName: query.productName,
    groupName: query.groupName,
    inspector: query.inspector,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.status = state.status ?? ''
    query.province = state.province ?? ''
    query.productName = state.productName ?? ''
    query.groupName = state.groupName ?? ''
    query.inspector = state.inspector ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

const refreshLinkedData = async () => {
  await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
}

useLinkedRealtimeRefresh({
  refresh: refreshLinkedData,
  scope: 'inspection',
  intervalMs: 60000,
})

// ===================== 巡检结果 Tab =====================

const resultLoading = ref(false)
const resultTotal = ref(0)
const resultTableData = ref<InspectionResult[]>([])
const detailVisible = ref(false)
const detailRow = ref<InspectionResult | null>(null)

const resultQuery = reactive({
  hospitalName: '',
  productName: '',
  inspector: '',
  healthLevel: '',
  page: 1,
  size: 15,
})

const resultHospitalOptions = ref<string[]>([])
const resultProductOptions = ref<string[]>([])
const resultInspectorOptions = ref<string[]>([])
const resultUploadLoading = ref(false)
const resultUploadInput = ref<HTMLInputElement | null>(null)

const resultHealthCounts = reactive({ good: 0, warning: 0, critical: 0 })

const loadResults = async () => {
  resultLoading.value = true
  try {
    const res = await fetchInspectionResults(resultQuery)
    resultTableData.value = res.data.items
    resultTotal.value = res.data.total
  } catch (error) {
    resultTableData.value = []
    resultTotal.value = 0
    ElMessage.error(getErrorMessage(error, '加载巡检结果失败'))
  } finally {
    resultLoading.value = false
  }
}

const loadResultFilterOptions = async () => {
  try {
    const res = await fetchInspectionResults({ page: 1, size: 1000 })
    const items = res.data.items
    if (!items.length) return

    resultHospitalOptions.value = Array.from(new Set(items.map((i) => i.hospitalName).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    resultProductOptions.value = Array.from(new Set(items.map((i) => i.productName).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    resultInspectorOptions.value = Array.from(new Set(items.map((i) => i.inspector).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))

    resultHealthCounts.good = items.filter((i) => i.healthLevel === '良好').length
    resultHealthCounts.warning = items.filter((i) => i.healthLevel === '警告').length
    resultHealthCounts.critical = items.filter((i) => i.healthLevel === '严重').length
  } catch {
    // silent
  }
}

let resultsLoaded = false

const onTabChange = (tab: string) => {
  if (tab === 'results' && !resultsLoaded) {
    resultsLoaded = true
    loadResultFilterOptions()
    loadResults()
  }
}

const onResultHealthClick = (level: string) => {
  resultQuery.hospitalName = ''
  resultQuery.productName = ''
  resultQuery.inspector = ''
  resultQuery.healthLevel = level
  resultQuery.page = 1
  loadResults()
}

const onResultSearch = () => {
  resultQuery.page = 1
  loadResults()
}

const onResultReset = () => {
  resultQuery.hospitalName = ''
  resultQuery.productName = ''
  resultQuery.inspector = ''
  resultQuery.healthLevel = ''
  resultQuery.page = 1
  resultQuery.size = 15
  loadResults()
}

const triggerResultUpload = () => {
  if (resultUploadLoading.value) return
  resultUploadInput.value?.click()
}

const onResultFileChange = async (event: Event) => {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  resultUploadLoading.value = true
  try {
    const text = await file.text()
    const parsed = JSON.parse(text)
    const payload = (Array.isArray(parsed) ? parsed : [parsed]) as InspectionResultSubmitItem[]

    if (payload.length === 0) {
      ElMessage.warning('上传文件中没有巡检结果数据')
      return
    }

    const hasRequiredFields = payload.every((item) => item && item.hospitalName && item.productName)
    if (!hasRequiredFields) {
      ElMessage.error('JSON格式不正确：缺少 hospitalName 或 productName 字段')
      return
    }

    const res = await submitInspectionResults(payload)
    ElMessage.success(res.message || `上传成功（${payload.length} 条）`)
    resultQuery.page = 1
    await Promise.all([loadResultFilterOptions(), loadResults()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '上传失败，请检查JSON格式或服务状态'))
  } finally {
    input.value = ''
    resultUploadLoading.value = false
  }
}

const showResultDetail = (row: InspectionResult) => {
  detailRow.value = row
  detailVisible.value = true
}

const formatDateTime = (value: string) => {
  if (!value) return '-'
  return value.replace('T', ' ').slice(0, 19)
}

const healthTag = (level: string) => {
  if (level === '良好') return 'success'
  if (level === '警告') return 'warning'
  if (level === '严重') return 'danger'
  return 'info'
}

const riskLevelTag = (level: string) => {
  if (level === '严重' || level === 'Critical') return 'danger'
  if (level === '警告' || level === 'Warning') return 'warning'
  return 'info'
}

const scoreClass = (score: number) => {
  if (score >= 80) return 'score-good'
  if (score >= 50) return 'score-warning'
  return 'score-danger'
}

const storageClass = (pct: number) => {
  if (pct >= 90) return 'score-danger'
  if (pct >= 70) return 'score-warning'
  return 'score-good'
}

onMounted(async () => {
  restoreFilterState()
  applyDrillQuery()

  // 如果 route 带了 tab=results 参数
  const tabParam = readRouteQueryValue(route.query.tab)
  if (tabParam === 'results') {
    activeTab.value = 'results'
    resultsLoaded = true
    loadResultFilterOptions()
    loadResults()
  }

  await runInitialLoad({
    tasks: [loadSummary, loadFilterOptions, loadData],
    retryChecks: [
      {
        when: () => summary.value.total > 0 && total.value === 0,
        task: loadData,
      },
    ],
  })
})
</script>

<style scoped>
.main-tabs {
  margin-bottom: 0;
}
.main-tabs :deep(.el-tabs__header) {
  margin-bottom: 16px;
}

.risk-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 10px;
  font-size: 12px;
  margin-right: 4px;
  line-height: 18px;
}
.risk-badge.critical {
  background: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
}
.risk-badge.warning {
  background: var(--el-color-warning-light-9);
  color: var(--el-color-warning-dark-2);
}
.risk-badge.good {
  background: var(--el-color-success-light-9);
  color: var(--el-color-success);
}

.score-good { color: var(--el-color-success); font-weight: 600; }
.score-warning { color: var(--el-color-warning-dark-2); font-weight: 600; }
.score-danger { color: var(--el-color-danger); font-weight: 600; }

.result-desc {
  margin-bottom: 8px;
}
</style>
