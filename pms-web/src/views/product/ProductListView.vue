<template>
  <div class="page-shell">
    <div class="product-hero">
      <div class="product-hero-main">
        <div class="product-hero-kicker-row">
          <span class="product-hero-kicker">Product Desk</span>
          <span class="product-hero-badge">{{ activeProductFilterLabel }}</span>
        </div>
        <h2 class="product-hero-title">产品管理</h2>
        <div class="product-hero-subtitle">
          统一查看当前筛选范围内的产品结构、部署规模、运行状态和主数据完整度，直接从第一屏进入详情、编辑和批量维护动作。
        </div>
        <div class="product-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="product-signal-card">
            <span class="product-signal-label">{{ item.label }}</span>
            <strong class="product-signal-value">{{ item.value }}</strong>
            <span class="product-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="product-hero-side">
        <div class="product-control-card">
          <div class="product-control-copy">
            <span class="product-control-title">产品台动作</span>
            <span class="product-control-note">先锁定状态、分类或部署重点，再进入编辑、详情和批量清理动作。</span>
          </div>
          <div class="product-control-actions">
            <el-button size="small" :loading="loading" icon="Refresh" @click="refreshDesk">刷新</el-button>
            <el-button v-if="canManageProduct" size="small" type="primary" icon="Plus" @click="onOpenCreate">新增产品</el-button>
            <el-button size="small" :loading="exporting" icon="Download" @click="onExport">导出</el-button>
          </div>
        </div>

        <div class="product-quick-grid">
          <button
            v-for="action in quickActions"
            :key="action.title"
            type="button"
            class="product-quick-action"
            @click="action.onClick()"
          >
            <span class="product-quick-title">{{ action.title }}</span>
            <span class="product-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <div class="product-insight-grid">
      <section class="product-insight-card">
        <div class="product-insight-head">
          <div>
            <div class="product-insight-title">部署重点</div>
            <div class="product-insight-note">优先关注部署医院数最高的产品，便于安排版本跟进、巡检和重大需求联动。</div>
          </div>
          <el-tag size="small" type="primary" effect="light">{{ deploymentRanking.length }} 个重点产品</el-tag>
        </div>
        <div v-if="deploymentRanking.length" class="product-ranking-list">
          <button
            v-for="item in deploymentRanking"
            :key="item.id"
            type="button"
            class="product-ranking-item"
            @click="onOpenQueueItem(item)"
          >
            <div class="product-ranking-main">
              <strong>{{ item.productName || '未命名产品' }}</strong>
              <span>{{ item.category || '未分类' }} · {{ item.version || '未标记版本' }}</span>
            </div>
            <div class="product-ranking-meta">
              <el-tag size="small" :type="statusTag(item.status)">{{ item.status || '未设置状态' }}</el-tag>
              <span>{{ item.deployHospitalCount }} 家部署医院</span>
            </div>
          </button>
        </div>
        <el-empty v-else description="当前筛选下暂无重点部署产品" :image-size="72" />
      </section>

      <section class="product-insight-card">
        <div class="product-insight-head">
          <div>
            <div class="product-insight-title">分类分布</div>
            <div class="product-insight-note">查看当前范围内的产品线分布，便于规划版本、培训和服务资源。</div>
          </div>
          <span class="product-insight-meta">{{ categoryBuckets.length }} 个分类</span>
        </div>
        <div v-if="categoryBuckets.length" class="product-chip-list">
          <div v-for="item in categoryBuckets" :key="item.label" class="product-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 个</span>
          </div>
        </div>
        <el-empty v-else description="暂无分类分布" :image-size="72" />
      </section>

      <section class="product-insight-card">
        <div class="product-insight-head">
          <div>
            <div class="product-insight-title">状态结构</div>
            <div class="product-insight-note">快速判断当前范围内是运行中产品主导，还是试运行、已停用产品更集中。</div>
          </div>
          <span class="product-insight-meta">{{ filteredRows.length }} 个产品</span>
        </div>
        <div v-if="statusBuckets.length" class="product-chip-list">
          <div v-for="item in statusBuckets" :key="item.label" class="product-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 个</span>
          </div>
        </div>
        <el-empty v-else description="暂无状态结构" :image-size="72" />
      </section>

      <section class="product-insight-card">
        <div class="product-insight-head">
          <div>
            <div class="product-insight-title">待补齐资料</div>
            <div class="product-insight-note">版本、分类或状态缺失的产品会影响交付、项目台账和运维统计口径。</div>
          </div>
          <span class="product-insight-meta">{{ dataQualityQueue.length }} 个待补齐</span>
        </div>
        <div v-if="dataQualityQueue.length" class="product-chip-list">
          <div v-for="item in dataQualityQueue" :key="item.id" class="product-chip product-chip--soft">
            <strong>{{ item.productName || '未命名产品' }}</strong>
            <span>{{ describeProductIssue(item) }}</span>
          </div>
        </div>
        <el-empty v-else description="当前范围内没有待补齐资料的产品" :image-size="72" />
      </section>
    </div>

    <div class="product-summary-wrap">
      <SummaryMetrics :items="summaryCards" :columns="4" @select="onSummaryCardSelect" />
    </div>

    <ProTable
      title="产品明细"
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
      :row-class-name="rowClassName"
      @selection-change="onSelectionChange"
      @row-dblclick="onRowDoubleClick"
    >
      <template #toolbar>
        <el-button v-if="canManageProduct" type="primary" icon="Plus" @click="onOpenCreate">新增产品</el-button>
        <el-button
          v-if="canManageProduct"
          type="danger"
          plain
          icon="Delete"
          :disabled="selectedProductIds.length === 0"
          :loading="batchDeleting"
          @click="onBatchDelete"
        >
          批量删除（{{ selectedProductIds.length }}）
        </el-button>
        <el-button :loading="exporting" icon="Download" @click="onExport">导出 CSV</el-button>
      </template>

      <template #search>
        <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
          <el-form-item label="产品名称">
            <el-input v-model="query.productName" clearable @keyup.enter="onSearch" />
          </el-form-item>
          <el-form-item label="分类">
            <el-select v-model="query.category" clearable style="width: 150px" placeholder="全部">
              <el-option v-for="category in categoryOptions" :key="category" :label="category" :value="category" />
            </el-select>
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="query.status" clearable style="width: 150px" placeholder="全部">
              <el-option v-for="status in statusOptions" :key="status" :label="status" :value="status" />
            </el-select>
          </el-form-item>
          <el-form-item class="filter-actions">
            <el-button type="primary" icon="Search" @click="onSearch">查询</el-button>
            <el-button icon="Refresh" @click="onReset">重置</el-button>
          </el-form-item>
        </el-form>
      </template>

      <el-table-column v-if="canManageProduct" type="selection" width="46" />
      <el-table-column prop="productName" label="产品名称" min-width="220" show-overflow-tooltip sortable />
      <el-table-column prop="version" label="版本" min-width="140" show-overflow-tooltip sortable />
      <el-table-column prop="category" label="分类" width="130" show-overflow-tooltip sortable />
      <el-table-column prop="status" label="状态" width="110" sortable>
        <template #default="scope">
          <el-tag :type="statusTag(scope.row.status)">{{ scope.row.status || '-' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="deployHospitalCount" label="部署医院数" width="120" align="right" sortable />
      <el-table-column prop="createdAt" label="创建时间" width="180" show-overflow-tooltip sortable />
      <el-table-column label="操作" width="220" fixed="right">
        <template #default="scope">
          <div class="table-action-group">
            <el-button
              type="primary"
              link
              icon="Document"
              :loading="detailLoadingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenDetail(scope.row.id)"
            >
              详情
            </el-button>
            <el-button
              v-if="canManageProduct"
              type="primary"
              link
              icon="Edit"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenEdit(scope.row)"
            >
              编辑
            </el-button>
            <el-button
              v-if="canManageProduct"
              type="danger"
              link
              icon="Delete"
              :loading="deletingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onDelete(scope.row)"
            >
              删除
            </el-button>
          </div>
        </template>
      </el-table-column>
    </ProTable>

    <ProDrawer v-model="editVisible" :title="editMode === 'create' ? '新增产品' : '编辑产品'" width="560px">
      <el-form ref="editFormRef" :model="editForm" :rules="editRules" label-width="90px">
        <el-form-item label="产品名称" prop="productName"><el-input v-model="editForm.productName" /></el-form-item>
        <el-form-item label="版本" prop="version"><el-input v-model="editForm.version" /></el-form-item>
        <el-form-item label="分类" prop="category"><el-input v-model="editForm.category" /></el-form-item>
        <el-form-item label="状态" prop="status">
          <el-select v-model="editForm.status" style="width: 160px">
            <el-option label="运行中" value="运行中" />
            <el-option label="试运行" value="试运行" />
            <el-option label="已停用" value="已停用" />
          </el-select>
        </el-form-item>
        <el-form-item label="部署医院" prop="deployHospitalCount">
          <el-input-number v-model="editForm.deployHospitalCount" :min="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button icon="Close" :disabled="submitLoading" @click="editVisible = false">取消</el-button>
        <el-button type="primary" icon="Check" :loading="submitLoading" :disabled="submitLoading" @click="onSaveEdit">保存</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="detailVisible" title="产品详情" width="520px">
      <el-descriptions v-if="detailItem" :column="2" border>
        <el-descriptions-item label="产品名称">{{ detailItem.productName }}</el-descriptions-item>
        <el-descriptions-item label="版本">{{ detailItem.version }}</el-descriptions-item>
        <el-descriptions-item label="分类">{{ detailItem.category }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ detailItem.status }}</el-descriptions-item>
        <el-descriptions-item label="部署医院数">{{ detailItem.deployHospitalCount }}</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ detailItem.createdAt || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button type="primary" @click="detailVisible = false">关闭</el-button>
      </template>
    </ProDrawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import {
  batchDeleteProducts,
  createProduct,
  deleteProduct,
  fetchProductById,
  fetchProducts,
  updateProduct,
} from '../../api/modules/product'
import type { ProductItem, ProductUpsert } from '../../types/product'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { resolveProductStatusTag } from '../../utils/statusTag'
import ProTable from '../../components/ProTable.vue'
import ProDrawer from '../../components/ProDrawer.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'

type ProductSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active: boolean
}

type ProductBucket = {
  label: string
  value: number
}

const loading = ref(false)
const total = ref(0)
const tableData = ref<ProductItem[]>([])
const allRows = ref<ProductItem[]>([])
const exporting = ref(false)
const focusMode = ref<'all' | 'highDeployment'>('all')

const query = reactive({
  productName: '',
  category: '',
  status: '',
  page: 1,
  size: 15,
})

const categoryOptions = ref<string[]>(['EMR', '临床辅助', '管理', 'AI', '移动'])
const statusOptions = ref<string[]>(['运行中', '试运行', '已停用'])
const route = useRoute()
const router = useRouter()

type ProductFilterState = {
  productName: string
  category: string
  status: string
  page: number
  size: number
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<ProductFilterState>({
  key: 'product-list',
  getState: () => ({
    productName: query.productName,
    category: query.category,
    status: query.status,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.productName = state.productName ?? ''
    query.category = state.category ?? ''
    query.status = state.status ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

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

const applyDrillQuery = () => {
  const status = readRouteQueryValue(route.query.status)
  if (!status) return

  query.productName = ''
  query.category = ''
  query.status = status
  query.page = 1
}

const editVisible = ref(false)
const detailVisible = ref(false)
const editMode = ref<'create' | 'edit'>('create')
const activeId = ref<number | null>(null)
const detailItem = ref<ProductItem | null>(null)
const editFormRef = ref<FormInstance>()
const submitLoading = ref(false)
const deletingId = ref<number | null>(null)
const detailLoadingId = ref<number | null>(null)
const selectedProductIds = ref<number[]>([])
const batchDeleting = ref(false)

const access = useAccessControl()
const canManageProduct = computed(() => access.canPermission('product.manage'))

const editForm = reactive<ProductUpsert>({
  productName: '',
  version: '',
  category: '',
  status: '运行中',
  deployHospitalCount: 0,
})

const { runInitialLoad } = useResilientLoad()

const filteredRows = computed(() => {
  const keyword = query.productName.trim().toLowerCase()

  return allRows.value.filter((item) => {
    const matchesName = !keyword || item.productName.toLowerCase().includes(keyword)
    const matchesCategory = !query.category || item.category === query.category
    const matchesStatus = !query.status || item.status === query.status
    const matchesFocus = focusMode.value !== 'highDeployment' || Number(item.deployHospitalCount || 0) >= 10
    return matchesName && matchesCategory && matchesStatus && matchesFocus
  })
})

const heroSignals = computed(() => [
  {
    label: '当前范围',
    value: filteredRows.value.length,
    note: '当前筛选后纳入工作台的产品总量',
  },
  {
    label: '运行中',
    value: filteredRows.value.filter((item) => item.status === '运行中').length,
    note: '当前范围内稳定运行中的产品',
  },
  {
    label: '试运行',
    value: filteredRows.value.filter((item) => item.status === '试运行').length,
    note: '仍处于试运行阶段的产品',
  },
  {
    label: '部署医院合计',
    value: filteredRows.value.reduce((sum, item) => sum + Number(item.deployHospitalCount || 0), 0),
    note: '当前范围内全部产品覆盖的部署医院数',
  },
])

const summaryCards = computed<ProductSummaryCard[]>(() => [
  {
    key: 'all',
    title: '产品总数',
    value: filteredRows.value.length,
    context: '当前范围',
    note: '查看当前筛选后的完整产品范围',
    color: '#3f4f63',
    active: !query.status,
  },
  {
    key: '运行中',
    title: '运行中',
    value: filteredRows.value.filter((item) => item.status === '运行中').length,
    context: '状态筛选',
    note: '聚焦当前稳定运行中的产品',
    color: '#7d9f92',
    active: query.status === '运行中',
  },
  {
    key: '试运行',
    title: '试运行',
    value: filteredRows.value.filter((item) => item.status === '试运行').length,
    context: '状态筛选',
    note: '跟进仍处于试运行阶段的产品',
    color: '#c7a06c',
    active: query.status === '试运行',
  },
  {
    key: '已停用',
    title: '已停用',
    value: filteredRows.value.filter((item) => item.status === '已停用').length,
    context: '状态筛选',
    note: '查看已停用产品与历史部署情况',
    color: '#c58a87',
    active: query.status === '已停用',
  },
])

const categoryBuckets = computed<ProductBucket[]>(() => {
  const counter = new Map<string, number>()
  filteredRows.value.forEach((item) => {
    const key = item.category?.trim() || '未分类'
    counter.set(key, (counter.get(key) ?? 0) + 1)
  })

  return Array.from(counter.entries())
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value)
    .slice(0, 6)
})

const statusBuckets = computed<ProductBucket[]>(() => {
  const counter = new Map<string, number>()
  filteredRows.value.forEach((item) => {
    const key = item.status?.trim() || '未设置状态'
    counter.set(key, (counter.get(key) ?? 0) + 1)
  })

  return Array.from(counter.entries())
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value)
})

const deploymentRanking = computed(() =>
  [...filteredRows.value]
    .sort((a, b) => Number(b.deployHospitalCount || 0) - Number(a.deployHospitalCount || 0))
    .slice(0, 6),
)

const dataQualityQueue = computed(() =>
  filteredRows.value
    .filter((item) => !item.version?.trim() || !item.category?.trim() || !item.status?.trim())
    .slice(0, 6),
)

const quickActions = computed(() => [
  {
    title: '运行中',
    note: `${filteredRows.value.filter((item) => item.status === '运行中').length} 个`,
    onClick: () => applyStatusFilter('运行中'),
  },
  {
    title: '试运行',
    note: `${filteredRows.value.filter((item) => item.status === '试运行').length} 个`,
    onClick: () => applyStatusFilter('试运行'),
  },
  {
    title: '高部署产品',
    note: `${filteredRows.value.filter((item) => Number(item.deployHospitalCount || 0) >= 10).length} 个`,
    onClick: () => applyHighDeploymentFilter(),
  },
  {
    title: '全部范围',
    note: `${allRows.value.length} 个`,
    onClick: () => resetFocusFilter(),
  },
])

const activeProductFilterLabel = computed(() => {
  if (focusMode.value === 'highDeployment') return '高部署产品'
  if (query.status) return query.status
  if (query.category) return query.category
  if (query.productName.trim()) return `搜索：${query.productName.trim()}`
  return '当前全部范围'
})

const editRules: FormRules<ProductUpsert> = {
  productName: [{ required: true, message: '请输入产品名称', trigger: 'blur' }],
  version: [{ required: true, message: '请输入版本', trigger: 'blur' }],
  category: [{ required: true, message: '请输入分类', trigger: 'blur' }],
  status: [{ required: true, message: '请选择状态', trigger: 'change' }],
}

const statusTag = (status: string) => resolveProductStatusTag(status)

const describeProductIssue = (item: ProductItem) => {
  const issues: string[] = []
  if (!item.version?.trim()) issues.push('缺版本')
  if (!item.category?.trim()) issues.push('缺分类')
  if (!item.status?.trim()) issues.push('缺状态')
  return issues.join(' / ') || '资料完整'
}

const rowClassName = ({ row }: { row: ProductItem }) => {
  if (!row.version?.trim() || !row.category?.trim() || !row.status?.trim()) {
    return 'focus-row'
  }
  return ''
}

const loadOverview = async () => {
  const res = await fetchProducts({ page: 1, size: 100000 })
  allRows.value = res.data.items

  const categories = Array.from(new Set(res.data.items.map((item) => item.category).filter(Boolean))).sort((a, b) =>
    a.localeCompare(b, 'zh-CN'),
  )
  categoryOptions.value = Array.from(new Set([...categoryOptions.value, ...categories]))

  const statuses = Array.from(new Set(res.data.items.map((item) => item.status).filter(Boolean))).sort((a, b) =>
    a.localeCompare(b, 'zh-CN'),
  )
  statusOptions.value = Array.from(new Set([...statusOptions.value, ...statuses]))
}

const loadData = async () => {
  loading.value = true
  try {
    const start = (query.page - 1) * query.size
    const end = start + query.size
    total.value = filteredRows.value.length
    tableData.value = filteredRows.value.slice(start, end)
    selectedProductIds.value = selectedProductIds.value.filter((id) => filteredRows.value.some((item) => item.id === id))
  } finally {
    loading.value = false
  }
}

const applyStatusFilter = (status: string) => {
  focusMode.value = 'all'
  query.productName = ''
  query.category = ''
  query.status = status
  query.page = 1
  void updateRouteQuery({ status: undefined })
  void loadData()
}

const applyHighDeploymentFilter = () => {
  focusMode.value = 'highDeployment'
  query.productName = ''
  query.category = ''
  query.status = ''
  query.page = 1
  void updateRouteQuery({ status: undefined })
  void loadData()
}

const resetFocusFilter = () => {
  focusMode.value = 'all'
  query.productName = ''
  query.category = ''
  query.status = ''
  query.page = 1
  void updateRouteQuery({ status: undefined })
  void loadData()
}

const onSummaryCardSelect = (card: { key?: string | number }) => {
  if (typeof card.key !== 'string') return
  if (card.key === 'all') {
    resetFocusFilter()
    return
  }
  applyStatusFilter(card.key)
}

const onSearch = () => {
  query.page = 1
  void loadData()
}

const onReset = () => {
  focusMode.value = 'all'
  query.productName = ''
  query.category = ''
  query.status = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  void updateRouteQuery({ status: undefined, action: undefined, id: undefined })
  void loadData()
}

const buildCsvCell = (value: string | number | undefined | null) => `"${String(value ?? '').replace(/"/g, '""')}"`

const downloadCsv = (content: string, filename: string) => {
  const blob = new Blob([`\uFEFF${content}`], { type: 'text/csv;charset=utf-8;' })
  const objectUrl = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = objectUrl
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(objectUrl)
}

const onExport = async () => {
  if (exporting.value) return
  exporting.value = true
  try {
    const rows = filteredRows.value.map((item) =>
      [
        buildCsvCell(item.productName),
        buildCsvCell(item.version),
        buildCsvCell(item.category),
        buildCsvCell(item.status),
        buildCsvCell(item.deployHospitalCount),
        buildCsvCell(item.createdAt),
      ].join(','),
    )
    const content = [['产品名称', '版本', '分类', '状态', '部署医院数', '创建时间'].join(','), ...rows].join('\n')
    downloadCsv(content, `products-${Date.now()}.csv`)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出产品失败，请稍后重试'))
  } finally {
    exporting.value = false
  }
}

const resetEditForm = () => {
  editForm.productName = ''
  editForm.version = ''
  editForm.category = ''
  editForm.status = '运行中'
  editForm.deployHospitalCount = 0
}

const openCreateDialog = () => {
  editMode.value = 'create'
  activeId.value = null
  resetEditForm()
  editVisible.value = true
}

const openEditDialog = (row: ProductItem) => {
  editMode.value = 'edit'
  activeId.value = row.id
  editForm.productName = row.productName
  editForm.version = row.version
  editForm.category = row.category
  editForm.status = row.status
  editForm.deployHospitalCount = row.deployHospitalCount
  editVisible.value = true
}

const onOpenCreate = () => {
  if (!canManageProduct.value) {
    ElMessage.warning('当前账号无新增产品权限')
    return
  }

  openCreateDialog()
  void updateRouteQuery({ action: 'create', id: undefined })
}

const onOpenEdit = (row: ProductItem) => {
  if (!canManageProduct.value) {
    ElMessage.warning('当前账号无编辑产品权限')
    return
  }

  openEditDialog(row)
  void updateRouteQuery({ action: 'edit', id: String(row.id) })
}

const onOpenQueueItem = (row: ProductItem) => {
  if (canManageProduct.value) {
    onOpenEdit(row)
    return
  }
  void onOpenDetail(row.id)
}

const onRowDoubleClick = (row: ProductItem) => {
  if (canManageProduct.value) {
    onOpenEdit(row)
    return
  }

  void onOpenDetail(row.id)
}

const syncEditDialogFromRoute = async () => {
  const action = readRouteQueryValue(route.query.action)
  if (!action) return

  if (action === 'create') {
    if (canManageProduct.value && !editVisible.value) openCreateDialog()
    return
  }

  if (action !== 'edit' || !canManageProduct.value) return

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) return

  const matched = tableData.value.find((item) => item.id === id) ?? allRows.value.find((item) => item.id === id)
  if (matched) {
    openEditDialog(matched)
    return
  }

  try {
    const res = await fetchProductById(id)
    openEditDialog(res.data)
  } catch {
    // ignore route sync failure
  }
}

const onOpenDetail = async (id: number) => {
  if (detailLoadingId.value === id) return
  detailLoadingId.value = id
  try {
    const res = await fetchProductById(id)
    detailItem.value = res.data
    detailVisible.value = true
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载产品详情失败，请稍后重试'))
  } finally {
    detailLoadingId.value = null
  }
}

const refreshDesk = async () => {
  try {
    await loadOverview()
    await loadData()
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载产品列表失败，请稍后重试'))
  }
}

const onSaveEdit = async () => {
  if (!canManageProduct.value) {
    ElMessage.warning('当前账号无产品维护权限')
    return
  }

  if (submitLoading.value || !editFormRef.value) return
  const valid = await editFormRef.value.validate().catch(() => false)
  if (!valid) return

  submitLoading.value = true
  try {
    if (editMode.value === 'create') {
      await createProduct(editForm)
      ElMessage.success('新增成功')
    } else if (activeId.value) {
      await updateProduct(activeId.value, editForm)
      ElMessage.success('更新成功')
    }

    editVisible.value = false
    await refreshDesk()
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, editMode.value === 'create' ? '新增产品失败，请稍后重试' : '更新产品失败，请稍后重试'))
  } finally {
    submitLoading.value = false
  }
}

watch(editVisible, (visible) => {
  if (!visible) {
    void clearRouteActionQuery()
  }
})

watch(
  () => route.fullPath,
  () => {
    applyDrillQuery()
    void syncEditDialogFromRoute()
  },
)

const onDelete = async (row: ProductItem) => {
  if (!canManageProduct.value) {
    ElMessage.warning('当前账号无删除产品权限')
    return
  }

  if (submitLoading.value || deletingId.value === row.id) return
  deletingId.value = row.id
  try {
    await ElMessageBox.confirm(`确认删除产品“${row.productName}”吗？`, '提示', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })

    await deleteProduct(row.id)
    ElMessage.success('删除成功')
    await refreshDesk()
    notifyDataChanged('global')
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      ElMessage.info('已取消删除')
    } else {
      ElMessage.error(getErrorMessage(error, '删除产品失败，请稍后重试'))
    }
  } finally {
    deletingId.value = null
  }
}

const onSelectionChange = (selection: ProductItem[]) => {
  selectedProductIds.value = selection.map((item) => item.id)
}

const onBatchDelete = async () => {
  if (!canManageProduct.value || selectedProductIds.value.length === 0 || batchDeleting.value) return

  try {
    await ElMessageBox.confirm(`确认删除选中的 ${selectedProductIds.value.length} 个产品吗？此操作不可恢复。`, '批量删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
  } catch {
    return
  }

  batchDeleting.value = true
  try {
    await batchDeleteProducts(selectedProductIds.value)
    ElMessage.success('批量删除成功')
    selectedProductIds.value = []
    await refreshDesk()
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '批量删除失败，请稍后重试'))
  } finally {
    batchDeleting.value = false
  }
}

const notifyDataChanged = useLinkedRealtimeRefresh({
  refresh: refreshDesk,
  scope: 'product',
  intervalMs: 60000,
}).notifyDataChanged

onMounted(async () => {
  await access.ensureAccessProfileLoaded()
  restoreFilterState()
  applyDrillQuery()
  await runInitialLoad({
    tasks: [refreshDesk],
  })
  await syncEditDialogFromRoute()
})
</script>

<style scoped>
.product-hero {
  display: grid;
  gap: 20px;
  grid-template-columns: minmax(0, 1.4fr) minmax(340px, 0.9fr);
  margin-bottom: 20px;
}

.product-hero-main,
.product-control-card,
.product-insight-card,
.product-summary-wrap {
  border: 1px solid rgba(15, 23, 42, 0.08);
  border-radius: 10px;
  background: #fff;
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.04);
}

.product-hero-main {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.product-hero-kicker-row,
.product-control-actions,
.product-insight-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.product-hero-kicker {
  font-size: 13px;
  line-height: 20px;
  font-weight: 700;
  text-transform: uppercase;
  color: #2563eb;
}

.product-hero-badge {
  align-self: flex-start;
  border-radius: 999px;
  padding: 4px 10px;
  background: rgba(37, 99, 235, 0.08);
  color: #2563eb;
  font-size: 12px;
  line-height: 18px;
  font-weight: 600;
}

.product-hero-title {
  margin: 0;
  font-size: 20px;
  line-height: 28px;
  font-weight: 700;
  color: #0f172a;
}

.product-hero-subtitle {
  font-size: 14px;
  line-height: 22px;
  color: #475569;
}

.product-hero-signals {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.product-signal-card {
  min-height: 108px;
  border-radius: 10px;
  border: 1px solid rgba(148, 163, 184, 0.22);
  background: linear-gradient(180deg, rgba(248, 250, 252, 0.96) 0%, rgba(255, 255, 255, 1) 100%);
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.product-signal-label {
  font-size: 13px;
  line-height: 20px;
  color: #64748b;
}

.product-signal-value {
  font-size: 22px;
  line-height: 30px;
  font-weight: 700;
  color: #0f172a;
}

.product-signal-note {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.product-hero-side {
  display: grid;
  gap: 16px;
  align-content: start;
}

.product-control-card {
  padding: 18px 20px;
  display: grid;
  gap: 14px;
}

.product-control-copy {
  display: grid;
  gap: 6px;
}

.product-control-title {
  font-size: 18px;
  line-height: 26px;
  font-weight: 700;
  color: #0f172a;
}

.product-control-note {
  font-size: 13px;
  line-height: 21px;
  color: #64748b;
}

.product-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.product-quick-action {
  border: 1px solid rgba(148, 163, 184, 0.18);
  background: #fff;
  border-radius: 10px;
  padding: 14px 16px;
  text-align: left;
  display: grid;
  gap: 4px;
  cursor: pointer;
  transition: border-color 0.2s ease, transform 0.2s ease, box-shadow 0.2s ease;
}

.product-quick-action:hover {
  border-color: rgba(37, 99, 235, 0.32);
  box-shadow: 0 12px 24px rgba(37, 99, 235, 0.08);
  transform: translateY(-1px);
}

.product-quick-title {
  font-size: 14px;
  line-height: 22px;
  font-weight: 600;
  color: #0f172a;
}

.product-quick-note {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.product-insight-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 16px;
}

.product-insight-card {
  padding: 18px;
  display: grid;
  gap: 14px;
}

.product-insight-title {
  font-size: 18px;
  line-height: 26px;
  font-weight: 700;
  color: #0f172a;
}

.product-insight-note {
  font-size: 13px;
  line-height: 21px;
  color: #64748b;
  margin-top: 4px;
}

.product-insight-meta {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
  font-weight: 600;
}

.product-ranking-list,
.product-chip-list {
  display: grid;
  gap: 10px;
}

.product-ranking-item {
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: rgba(248, 250, 252, 0.9);
  border-radius: 10px;
  padding: 14px 16px;
  text-align: left;
  display: grid;
  gap: 8px;
  cursor: pointer;
  transition: border-color 0.2s ease, background-color 0.2s ease;
}

.product-ranking-item:hover {
  border-color: rgba(37, 99, 235, 0.3);
  background: rgba(239, 246, 255, 0.9);
}

.product-ranking-main,
.product-ranking-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.product-ranking-main strong,
.product-chip strong {
  color: #0f172a;
  font-size: 14px;
  line-height: 22px;
  font-weight: 600;
}

.product-ranking-main span,
.product-ranking-meta span,
.product-chip span {
  color: #64748b;
  font-size: 12px;
  line-height: 18px;
}

.product-chip {
  border-radius: 10px;
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: rgba(248, 250, 252, 0.84);
  padding: 12px 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.product-chip--soft {
  align-items: flex-start;
  flex-direction: column;
}

.product-summary-wrap {
  padding: 4px;
  margin-bottom: 16px;
}

:deep(.focus-row) {
  --el-table-tr-bg-color: rgba(251, 191, 36, 0.08);
}

@media (max-width: 1280px) {
  .product-hero {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 900px) {
  .product-insight-grid,
  .product-hero-signals {
    grid-template-columns: 1fr 1fr;
  }
}

@media (max-width: 720px) {
  .product-hero-main,
  .product-control-card,
  .product-insight-card,
  .product-summary-wrap {
    padding-left: 16px;
    padding-right: 16px;
  }

  .product-hero-signals,
  .product-insight-grid,
  .product-quick-grid {
    grid-template-columns: 1fr;
  }

  .product-hero-kicker-row,
  .product-insight-head,
  .product-control-actions,
  .product-ranking-main,
  .product-ranking-meta,
  .product-chip {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
