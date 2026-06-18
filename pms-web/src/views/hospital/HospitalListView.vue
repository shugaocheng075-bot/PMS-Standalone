<template>
  <div class="page-shell">
    <div class="hospital-hero">
      <div class="hospital-hero-main">
        <div class="hospital-hero-kicker-row">
          <span class="hospital-hero-kicker">Hospital Desk</span>
          <span class="hospital-hero-badge">{{ activeHospitalFilterLabel }}</span>
        </div>
        <h2 class="hospital-hero-title">医院管理</h2>
        <div class="hospital-hero-subtitle">
          统一查看当前筛选范围内的医院分布、等级结构、评级覆盖和资料完整度，直接从第一屏进入详情、编辑和评级动作。
        </div>
        <div class="hospital-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="hospital-signal-card">
            <span class="hospital-signal-label">{{ item.label }}</span>
            <strong class="hospital-signal-value">{{ item.value }}</strong>
            <span class="hospital-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="hospital-hero-side">
        <div class="hospital-control-card">
          <div class="hospital-control-copy">
            <span class="hospital-control-title">医院台动作</span>
            <span class="hospital-control-note">先锁定等级、省市或重点问题，再从待补齐清单进入编辑、评级和详情动作。</span>
          </div>
          <div class="hospital-control-actions">
            <el-button size="small" :loading="loading" @click="refreshDesk" icon="Refresh">刷新</el-button>
            <el-button v-if="canManageHospital" size="small" type="primary" @click="onOpenCreate" icon="Plus">新增医院</el-button>
            <el-button :loading="exporting" size="small" @click="onExport" icon="Download">导出</el-button>
          </div>
        </div>

        <div class="hospital-quick-grid">
          <button
            v-for="action in quickActions"
            :key="action.title"
            type="button"
            class="hospital-quick-action"
            @click="action.onClick()"
          >
            <span class="hospital-quick-title">{{ action.title }}</span>
            <span class="hospital-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <div class="hospital-insight-grid">
      <section class="hospital-insight-card">
        <div class="hospital-insight-head">
          <div>
            <div class="hospital-insight-title">资料待补齐</div>
            <div class="hospital-insight-note">优先处理联系人、电话、地址或评级缺失的医院，减少后续模块联动中的信息断层。</div>
          </div>
          <el-tag size="small" type="warning" effect="light">{{ dataQualityQueue.length }} 家</el-tag>
        </div>
        <div v-if="dataQualityQueue.length" class="hospital-queue-list">
          <button
            v-for="item in dataQualityQueue"
            :key="item.id"
            type="button"
            class="hospital-queue-item"
            @click="onOpenQueueItem(item)"
          >
            <div class="hospital-queue-main">
              <strong>{{ item.hospitalName || '未命名医院' }}</strong>
              <span>{{ item.province || '未知省份' }} · {{ item.city || '未知城市' }}</span>
            </div>
            <div class="hospital-queue-meta">
              <el-tag size="small" :type="tierTag(item.tier)">{{ item.tier || '未分级' }}</el-tag>
              <span>{{ describeHospitalIssue(item) }}</span>
            </div>
          </button>
        </div>
        <el-empty v-else description="当前筛选下没有待补齐资料的医院" :image-size="72" />
      </section>

      <section class="hospital-insight-card">
        <div class="hospital-insight-head">
          <div>
            <div class="hospital-insight-title">省份分布</div>
            <div class="hospital-insight-note">查看当前筛选范围内的省份覆盖，便于按区域规划服务和维护策略。</div>
          </div>
          <span class="hospital-insight-meta">{{ topProvinceBuckets.length }} 个重点省份</span>
        </div>
        <div v-if="topProvinceBuckets.length" class="hospital-chip-list">
          <div v-for="item in topProvinceBuckets" :key="item.label" class="hospital-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 家</span>
          </div>
        </div>
        <el-empty v-else description="暂无省份分布" :image-size="72" />
      </section>

      <section class="hospital-insight-card">
        <div class="hospital-insight-head">
          <div>
            <div class="hospital-insight-title">等级结构</div>
            <div class="hospital-insight-note">快速判断当前范围内是三级医院主导，还是二级、一级医院更集中。</div>
          </div>
          <span class="hospital-insight-meta">{{ filteredRows.length }} 家医院</span>
        </div>
        <div v-if="tierBuckets.length" class="hospital-chip-list">
          <div v-for="item in tierBuckets" :key="item.label" class="hospital-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 家</span>
          </div>
        </div>
        <el-empty v-else description="暂无等级结构" :image-size="72" />
      </section>

      <section class="hospital-insight-card">
        <div class="hospital-insight-head">
          <div>
            <div class="hospital-insight-title">评级覆盖</div>
            <div class="hospital-insight-note">查看 EMR 和互联互通评级覆盖情况，便于后续项目交付和巡检场景调用。</div>
          </div>
          <span class="hospital-insight-meta">{{ ratedHospitalCount }} 家已评级</span>
        </div>
        <div class="hospital-chip-list">
          <div v-for="item in ratingCoverageBuckets" :key="item.label" class="hospital-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 家</span>
          </div>
        </div>
      </section>
    </div>

    <div class="hospital-summary-wrap">
      <SummaryMetrics :items="summaryCards" :columns="4" @select="onSummaryCardSelect" />
    </div>

    <ProTable
      title="医院明细"
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
      @row-dblclick="onRowDoubleClick"
    >
      <template #toolbar>
        <el-button v-if="canManageHospital" type="primary" @click="onOpenCreate" icon="Plus">新增医院</el-button>
        <el-button :loading="exporting" @click="onExport" icon="Download">导出 CSV</el-button>
      </template>

      <template #search>
        <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
          <el-form-item label="医院名称">
            <el-input v-model="query.hospitalName" clearable @keyup.enter="onSearch" />
          </el-form-item>
          <el-form-item label="医院等级">
            <el-select v-model="query.tier" clearable style="width: 140px" placeholder="全部">
              <el-option v-for="tier in tierOptions" :key="tier" :label="tier" :value="tier" />
            </el-select>
          </el-form-item>
          <el-form-item label="省份">
            <el-select v-model="query.province" clearable style="width: 140px" placeholder="全部">
              <el-option v-for="province in provinceOptions" :key="province" :label="province" :value="province" />
            </el-select>
          </el-form-item>
          <el-form-item label="城市">
            <el-select v-model="query.city" clearable style="width: 140px" placeholder="全部">
              <el-option v-for="city in filteredCityOptions" :key="city" :label="city" :value="city" />
            </el-select>
          </el-form-item>
          <el-form-item class="filter-actions">
            <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
            <el-button @click="onReset" icon="Refresh">重置</el-button>
          </el-form-item>
        </el-form>
      </template>

      <el-table-column prop="hospitalName" label="医院名称" min-width="240" show-overflow-tooltip sortable />
      <el-table-column prop="tier" label="等级" width="100" sortable>
        <template #default="scope">
          <el-tag :type="tierTag(scope.row.tier)">{{ scope.row.tier }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip sortable />
      <el-table-column prop="city" label="城市" width="100" show-overflow-tooltip sortable />
      <el-table-column prop="address" label="地址" min-width="240" show-overflow-tooltip />
      <el-table-column prop="contactPerson" label="联系人" width="110" show-overflow-tooltip />
      <el-table-column prop="contactPhone" label="联系电话" width="150" show-overflow-tooltip />
      <el-table-column prop="departmentCount" label="科室数量" width="110" align="right" sortable />
      <el-table-column label="评级" min-width="220">
        <template #default="scope">
          <div class="rating-inline">
            <span>EMR: {{ scope.row.emrRatingLevel || '-' }}</span>
            <span>互联互通: {{ scope.row.interopRatingLevel || '-' }}</span>
          </div>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="250" fixed="right">
        <template #default="scope">
          <div class="hospital-action-group">
            <el-button
              type="primary"
              link
              :loading="detailLoadingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id || ratingLoading"
              @click="onOpenDetail(scope.row.id)"
              icon="Document"
            >
              详情
            </el-button>
            <el-button
              v-if="canManageHospital"
              type="primary"
              link
              :disabled="submitLoading || deletingId === scope.row.id || ratingLoading"
              @click="onOpenEdit(scope.row)"
              icon="Edit"
            >
              编辑
            </el-button>
            <el-button
              v-if="canManageHospital"
              type="primary"
              link
              :disabled="submitLoading || deletingId === scope.row.id || ratingLoading"
              @click="onOpenRating(scope.row)"
            >
              评级
            </el-button>
            <el-button
              v-if="canManageHospital"
              type="danger"
              link
              :loading="deletingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id || ratingLoading"
              @click="onDelete(scope.row)"
              icon="Delete"
            >
              删除
            </el-button>
          </div>
        </template>
      </el-table-column>
    </ProTable>

    <ProDrawer v-model="editVisible" :title="editMode === 'create' ? '新增医院' : '编辑医院'" width="620px">
      <el-form ref="editFormRef" :model="editForm" :rules="editRules" label-width="90px">
        <el-form-item label="医院名称" prop="hospitalName"><el-input v-model="editForm.hospitalName" /></el-form-item>
        <el-form-item label="医院等级">
          <el-select v-model="editForm.tier" style="width: 160px">
            <el-option label="三级" value="三级" />
            <el-option label="二级" value="二级" />
            <el-option label="一级" value="一级" />
          </el-select>
        </el-form-item>
        <el-form-item label="省份" prop="province"><el-input v-model="editForm.province" /></el-form-item>
        <el-form-item label="城市" prop="city"><el-input v-model="editForm.city" /></el-form-item>
        <el-form-item label="地址" prop="address"><el-input v-model="editForm.address" /></el-form-item>
        <el-form-item label="联系人" prop="contactPerson"><el-input v-model="editForm.contactPerson" /></el-form-item>
        <el-form-item label="联系电话" prop="contactPhone"><el-input v-model="editForm.contactPhone" /></el-form-item>
        <el-form-item label="科室数量" prop="departmentCount"><el-input v-model="editForm.departmentCount" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading" @click="editVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="submitLoading" :disabled="submitLoading" @click="onSaveEdit" icon="Check">保存</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="ratingVisible" title="更新评级" width="460px">
      <el-form :model="ratingForm" label-width="95px">
        <el-form-item label="EMR评级"><el-input v-model="ratingForm.emrRatingLevel" /></el-form-item>
        <el-form-item label="互联互通评级"><el-input v-model="ratingForm.interopRatingLevel" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="ratingLoading" @click="ratingVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="ratingLoading" :disabled="ratingLoading" @click="onSaveRating" icon="Check">保存</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="detailVisible" title="医院详情" width="620px">
      <el-descriptions v-if="detailItem" :column="2" border>
        <el-descriptions-item label="医院名称">{{ detailItem.hospitalName }}</el-descriptions-item>
        <el-descriptions-item label="等级">{{ detailItem.tier }}</el-descriptions-item>
        <el-descriptions-item label="省份">{{ detailItem.province }}</el-descriptions-item>
        <el-descriptions-item label="城市">{{ detailItem.city }}</el-descriptions-item>
        <el-descriptions-item label="地址" :span="2">{{ detailItem.address }}</el-descriptions-item>
        <el-descriptions-item label="联系人">{{ detailItem.contactPerson }}</el-descriptions-item>
        <el-descriptions-item label="联系电话">{{ detailItem.contactPhone }}</el-descriptions-item>
        <el-descriptions-item label="科室数量">{{ detailItem.departmentCount }}</el-descriptions-item>
        <el-descriptions-item label="EMR评级">{{ detailItem.emrRatingLevel || '-' }}</el-descriptions-item>
        <el-descriptions-item label="互联互通评级">{{ detailItem.interopRatingLevel || '-' }}</el-descriptions-item>
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
  createHospital,
  deleteHospital,
  fetchHospitalById,
  fetchHospitals,
  updateHospital,
  updateHospitalRating,
} from '../../api/modules/hospital'
import type { HospitalItem, HospitalQuery, HospitalRating, HospitalUpsert } from '../../types/hospital'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { CITY_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import ProTable from '../../components/ProTable.vue'
import ProDrawer from '../../components/ProDrawer.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'

const loading = ref(false)
const exporting = ref(false)
const total = ref(0)
const tableData = ref<HospitalItem[]>([])
const allRows = ref<HospitalItem[]>([])

const query = reactive({
  hospitalName: '',
  tier: '',
  province: '',
  city: '',
  page: 1,
  size: 15,
})

type HospitalSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active: boolean
}

type HospitalFilterState = {
  hospitalName: string
  tier: string
  province: string
  city: string
  page: number
  size: number
}

type InsightBucket = {
  label: string
  value: number
}

type FocusMode = '' | 'missingInfo' | 'unrated'

const focusMode = ref<FocusMode>('')
const route = useRoute()
const router = useRouter()

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

const tierOptions = ref<string[]>(['三级', '二级', '一级'])
const applyDrillQuery = () => {
  const tier = readRouteQueryValue(route.query.tier)
  if (!tier) return
  focusMode.value = ''
  query.hospitalName = ''
  query.province = ''
  query.city = ''
  query.tier = tier
  query.page = 1
}

const provinceOptions = ref<string[]>([...PROVINCE_OPTIONS])
const cityOptionsByProvince = ref<Record<string, string[]>>({})

const allCityOptions = computed(() => {
  const cities = Object.values(cityOptionsByProvince.value).flat()
  return cities.length > 0 ? Array.from(new Set(cities)) : CITY_OPTIONS
})

const filteredCityOptions = computed(() => {
  if (!query.province) return allCityOptions.value
  const cities = cityOptionsByProvince.value[query.province]
  return cities && cities.length > 0 ? cities : allCityOptions.value
})

watch(
  () => query.province,
  () => {
    if (query.city && !filteredCityOptions.value.includes(query.city)) {
      query.city = ''
    }
  },
)

const editVisible = ref(false)
const ratingVisible = ref(false)
const detailVisible = ref(false)
const editMode = ref<'create' | 'edit'>('create')
const activeId = ref<number | null>(null)
const detailItem = ref<HospitalItem | null>(null)
const editFormRef = ref<FormInstance>()
const submitLoading = ref(false)
const ratingLoading = ref(false)
const deletingId = ref<number | null>(null)
const detailLoadingId = ref<number | null>(null)

const access = useAccessControl()
const canManageHospital = computed(() => access.isManager() && access.canPermission('hospital.manage'))

const editForm = reactive<HospitalUpsert>({
  hospitalName: '',
  tier: '三级',
  province: '',
  city: '',
  address: '',
  contactPerson: '',
  contactPhone: '',
  departmentCount: '',
})

const ratingForm = reactive<HospitalRating>({
  emrRatingLevel: '',
  interopRatingLevel: '',
})

const { runInitialLoad } = useResilientLoad()
const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await refreshDesk()
  },
  scope: 'hospital',
  intervalMs: 60000,
})

const editRules: FormRules<HospitalUpsert> = {
  hospitalName: [{ required: true, message: '请输入医院名称', trigger: 'blur' }],
  province: [{ required: true, message: '请输入省份', trigger: 'blur' }],
  city: [{ required: true, message: '请输入城市', trigger: 'blur' }],
  address: [{ required: true, message: '请输入地址', trigger: 'blur' }],
  contactPerson: [{ required: true, message: '请输入联系人', trigger: 'blur' }],
  contactPhone: [
    { required: true, message: '请输入联系电话', trigger: 'blur' },
    { pattern: /^(\d{3,4}-?)?\d{7,11}$/, message: '联系电话格式不正确', trigger: 'blur' },
  ],
  departmentCount: [{ required: true, message: '请输入科室数量', trigger: 'blur' }],
}

const tierTag = (tier: string) => {
  if (tier === '三级') return 'success'
  if (tier === '二级') return 'warning'
  return 'info'
}

const parseDepartmentCount = (value: string) => {
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : 0
}

const hasMissingContact = (item: HospitalItem) =>
  !item.contactPerson?.trim() || !item.contactPhone?.trim() || !item.address?.trim()

const hasRatingGap = (item: HospitalItem) =>
  !item.emrRatingLevel?.trim() || !item.interopRatingLevel?.trim()

const matchesFilteredRow = (item: HospitalItem) => {
  if (focusMode.value === 'missingInfo' && !hasMissingContact(item)) return false
  if (focusMode.value === 'unrated' && !hasRatingGap(item)) return false
  if (query.hospitalName && !item.hospitalName?.includes(query.hospitalName)) return false
  if (query.tier && item.tier !== query.tier) return false
  if (query.province && item.province !== query.province) return false
  if (query.city && item.city !== query.city) return false
  return true
}

const filteredRows = computed(() => allRows.value.filter(matchesFilteredRow))

const buildTopBuckets = (items: HospitalItem[], selector: (item: HospitalItem) => string, limit = 6): InsightBucket[] => {
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

const topProvinceBuckets = computed(() => buildTopBuckets(filteredRows.value, (item) => item.province || '未填写省份'))
const tierBuckets = computed(() => buildTopBuckets(filteredRows.value, (item) => item.tier || '未分级', 4))

const ratingCoverageBuckets = computed<InsightBucket[]>(() => {
  const fullyRated = filteredRows.value.filter((item) => item.emrRatingLevel?.trim() && item.interopRatingLevel?.trim()).length
  const partialRated = filteredRows.value.filter((item) => {
    const emr = Boolean(item.emrRatingLevel?.trim())
    const interop = Boolean(item.interopRatingLevel?.trim())
    return (emr || interop) && !(emr && interop)
  }).length
  const unrated = filteredRows.value.filter((item) => !item.emrRatingLevel?.trim() && !item.interopRatingLevel?.trim()).length
  return [
    { label: '已完整评级', value: fullyRated },
    { label: '部分缺失', value: partialRated },
    { label: '未评级', value: unrated },
  ]
})

const ratedHospitalCount = computed(() => ratingCoverageBuckets.value[0]?.value ?? 0)
const missingInfoCount = computed(() => filteredRows.value.filter(hasMissingContact).length)
const unratedCount = computed(() => filteredRows.value.filter(hasRatingGap).length)
const departmentTotal = computed(() => filteredRows.value.reduce((sum, item) => sum + parseDepartmentCount(item.departmentCount), 0))

const hospitalIssueScore = (item: HospitalItem) => {
  let score = 0
  if (hasMissingContact(item)) score += 2
  if (hasRatingGap(item)) score += 2
  if (!item.departmentCount?.trim()) score += 1
  return score
}

const describeHospitalIssue = (item: HospitalItem) => {
  const issues: string[] = []
  if (!item.contactPerson?.trim()) issues.push('缺联系人')
  if (!item.contactPhone?.trim()) issues.push('缺电话')
  if (!item.address?.trim()) issues.push('缺地址')
  if (!item.emrRatingLevel?.trim()) issues.push('缺 EMR 评级')
  if (!item.interopRatingLevel?.trim()) issues.push('缺互联互通评级')
  return issues.length ? issues.join(' / ') : '资料完整'
}

const dataQualityQueue = computed(() =>
  filteredRows.value
    .filter((item) => hasMissingContact(item) || hasRatingGap(item))
    .slice()
    .sort((left, right) => {
      const scoreGap = hospitalIssueScore(right) - hospitalIssueScore(left)
      if (scoreGap !== 0) return scoreGap
      return left.hospitalName.localeCompare(right.hospitalName, 'zh-CN')
    })
    .slice(0, 6),
)

const activeHospitalFilterLabel = computed(() => {
  const labels: string[] = []
  if (focusMode.value === 'missingInfo') labels.push('资料待补齐')
  if (focusMode.value === 'unrated') labels.push('未完成评级')
  if (query.tier) labels.push(query.tier)
  if (query.province) labels.push(query.province)
  if (query.city) labels.push(query.city)
  if (query.hospitalName) labels.push(query.hospitalName)
  return labels.length ? labels.join(' / ') : '当前全部范围'
})

const heroSignals = computed(() => [
  {
    label: '当前范围',
    value: filteredRows.value.length,
    note: '当前筛选后纳入工作台的医院总量',
  },
  {
    label: '资料待补齐',
    value: missingInfoCount.value,
    note: '联系人、电话或地址仍需补齐的医院',
  },
  {
    label: '未完成评级',
    value: unratedCount.value,
    note: 'EMR 或互联互通评级仍未补齐的医院',
  },
  {
    label: '科室总量',
    value: departmentTotal.value,
    note: '当前范围内医院科室数量合计',
  },
])

const summaryCards = computed<HospitalSummaryCard[]>(() => [
  {
    key: 'all',
    title: '全部医院',
    value: filteredRows.value.length,
    context: '医院层级',
    note: '查看当前范围内全部医院分布',
    color: '#3f4f63',
    active: query.tier === '',
  },
  {
    key: '三级',
    title: '三级医院',
    value: filteredRows.value.filter((item) => item.tier === '三级').length,
    context: '医院层级',
    note: '聚焦三级医院客户与评级覆盖',
    color: '#7d9f92',
    active: query.tier === '三级',
  },
  {
    key: '二级',
    title: '二级医院',
    value: filteredRows.value.filter((item) => item.tier === '二级').length,
    context: '医院层级',
    note: '查看二级医院客户与联系资料',
    color: '#c7a06c',
    active: query.tier === '二级',
  },
  {
    key: '一级',
    title: '一级医院',
    value: filteredRows.value.filter((item) => item.tier === '一级').length,
    context: '医院层级',
    note: '查看一级医院客户与区域分布',
    color: '#c58a87',
    active: query.tier === '一级',
  },
])

const quickActions = computed(() => [
  {
    title: '资料待补齐',
    note: `${missingInfoCount.value} 家`,
    onClick: () => focusMissingInfo(),
  },
  {
    title: '未完成评级',
    note: `${unratedCount.value} 家`,
    onClick: () => focusUnrated(),
  },
  {
    title: '三级医院',
    note: `${filteredRows.value.filter((item) => item.tier === '三级').length} 家`,
    onClick: () => onStatClick('三级'),
  },
  {
    title: '全部范围',
    note: `${filteredRows.value.length} 家`,
    onClick: () => onReset(),
  },
])

const loadOverview = async () => {
  try {
    const res = await fetchHospitals({ page: 1, size: 100000 } as HospitalQuery)
    const items = res.data.items
    allRows.value = items

    if (!items.length) return

    const provinces = Array.from(new Set(items.map((item) => item.province).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (provinces.length > 0) {
      provinceOptions.value = provinces
    }

    const tiers = Array.from(new Set(items.map((item) => item.tier).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (tiers.length > 0) {
      tierOptions.value = Array.from(new Set([...tierOptions.value, ...tiers]))
    }

    const cityMap: Record<string, string[]> = {}
    for (const item of items) {
      if (!item.province || !item.city) continue
      const cities = cityMap[item.province] ?? (cityMap[item.province] = [])
      if (!cities.includes(item.city)) cities.push(item.city)
    }

    cityOptionsByProvince.value = Object.fromEntries(
      Object.entries(cityMap).map(([province, cities]) => [province, cities.sort((a, b) => a.localeCompare(b, 'zh-CN'))]),
    )
  } catch (error) {
    allRows.value = []
    ElMessage.error(getErrorMessage(error, '加载医院概览失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const filtered = filteredRows.value
    total.value = filtered.length
    const size = query.size <= 0 ? 15 : query.size
    const maxPage = Math.max(1, Math.ceil(filtered.length / size))
    if (query.page > maxPage) query.page = maxPage
    const page = query.page < 1 ? 1 : query.page
    const start = (page - 1) * size
    tableData.value = filtered.slice(start, start + size)
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载医院列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onStatClick = (tier: string) => {
  focusMode.value = ''
  query.hospitalName = ''
  query.province = ''
  query.city = ''
  query.tier = tier
  query.page = 1
  void updateRouteQuery({ tier: undefined })
  void loadData()
}

const focusMissingInfo = () => {
  focusMode.value = 'missingInfo'
  query.page = 1
  void loadData()
}

const focusUnrated = () => {
  focusMode.value = 'unrated'
  query.page = 1
  void loadData()
}

const onSummaryCardSelect = (card: { key?: string | number }) => {
  if (typeof card.key !== 'string') return
  onStatClick(card.key === 'all' ? '' : card.key)
}

const onSearch = () => {
  focusMode.value = ''
  query.page = 1
  void loadData()
}

const onReset = () => {
  focusMode.value = ''
  query.hospitalName = ''
  query.tier = ''
  query.province = ''
  query.city = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  void updateRouteQuery({ tier: undefined, action: undefined, id: undefined })
  void loadData()
}

const buildCsvCell = (value: string | number | undefined | null) => `"${String(value ?? '').replace(/"/g, '""')}"`

const onExport = async () => {
  exporting.value = true
  try {
    const lines = [
      ['医院名称', '等级', '省份', '城市', '地址', '联系人', '联系电话', '科室数量', 'EMR评级', '互联互通评级'].join(','),
      ...filteredRows.value.map((item) =>
        [
          buildCsvCell(item.hospitalName),
          buildCsvCell(item.tier),
          buildCsvCell(item.province),
          buildCsvCell(item.city),
          buildCsvCell(item.address),
          buildCsvCell(item.contactPerson),
          buildCsvCell(item.contactPhone),
          buildCsvCell(item.departmentCount),
          buildCsvCell(item.emrRatingLevel),
          buildCsvCell(item.interopRatingLevel),
        ].join(','),
      ),
    ]

    const blob = new Blob(['\uFEFF' + lines.join('\n')], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `医院信息-${Date.now()}.csv`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  } finally {
    exporting.value = false
  }
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<HospitalFilterState>({
  key: 'hospital-list',
  getState: () => ({
    hospitalName: query.hospitalName,
    tier: query.tier,
    province: query.province,
    city: query.city,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.hospitalName = state.hospitalName ?? ''
    query.tier = state.tier ?? ''
    query.province = state.province ?? ''
    query.city = state.city ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

const resetEditForm = () => {
  editForm.hospitalName = ''
  editForm.tier = '三级'
  editForm.province = ''
  editForm.city = ''
  editForm.address = ''
  editForm.contactPerson = ''
  editForm.contactPhone = ''
  editForm.departmentCount = ''
}

const openCreateDialog = () => {
  editMode.value = 'create'
  activeId.value = null
  resetEditForm()
  editVisible.value = true
}

const openEditDialog = (row: HospitalItem) => {
  editMode.value = 'edit'
  activeId.value = row.id
  editForm.hospitalName = row.hospitalName
  editForm.tier = row.tier
  editForm.province = row.province
  editForm.city = row.city
  editForm.address = row.address
  editForm.contactPerson = row.contactPerson
  editForm.contactPhone = row.contactPhone
  editForm.departmentCount = row.departmentCount
  editVisible.value = true
}

const onOpenCreate = () => {
  if (!canManageHospital.value) {
    ElMessage.warning('当前账号无新增医院权限')
    return
  }
  openCreateDialog()
  void updateRouteQuery({ action: 'create', id: undefined })
}

const onOpenEdit = (row: HospitalItem) => {
  if (!canManageHospital.value) {
    ElMessage.warning('当前账号无编辑医院权限')
    return
  }
  openEditDialog(row)
  void updateRouteQuery({ action: 'edit', id: String(row.id) })
}

const onRowDoubleClick = (row: HospitalItem) => {
  if (canManageHospital.value) {
    onOpenEdit(row)
  } else {
    void onOpenDetail(row.id)
  }
}

const syncEditDialogFromRoute = async () => {
  const action = readRouteQueryValue(route.query.action)
  if (!action) return

  if (action === 'create') {
    if (canManageHospital.value && !editVisible.value) openCreateDialog()
    return
  }

  if (action !== 'edit' || !canManageHospital.value) return

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) return

  const matched = tableData.value.find((item) => item.id === id) ?? allRows.value.find((item) => item.id === id)
  if (matched) {
    openEditDialog(matched)
    return
  }

  try {
    const res = await fetchHospitalById(id)
    openEditDialog(res.data)
  } catch {
    // ignore route sync failure
  }
}

const onOpenQueueItem = (row: HospitalItem) => {
  if (canManageHospital.value) {
    onOpenEdit(row)
  } else {
    void onOpenDetail(row.id)
  }
}

const onOpenRating = (row: HospitalItem) => {
  if (!canManageHospital.value) {
    ElMessage.warning('当前账号无医院评级权限')
    return
  }
  activeId.value = row.id
  ratingForm.emrRatingLevel = row.emrRatingLevel || ''
  ratingForm.interopRatingLevel = row.interopRatingLevel || ''
  ratingVisible.value = true
}

const onOpenDetail = async (id: number) => {
  if (detailLoadingId.value === id) return
  detailLoadingId.value = id
  try {
    const res = await fetchHospitalById(id)
    detailItem.value = res.data
    detailVisible.value = true
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载医院详情失败，请稍后重试'))
  } finally {
    detailLoadingId.value = null
  }
}

const refreshDesk = async () => {
  await Promise.allSettled([loadOverview(), loadData()])
}

const onSaveEdit = async () => {
  if (!canManageHospital.value) {
    ElMessage.warning('当前账号无医院维护权限')
    return
  }
  if (submitLoading.value || !editFormRef.value) return

  const valid = await editFormRef.value.validate().catch(() => false)
  if (!valid) return

  submitLoading.value = true
  try {
    if (editMode.value === 'create') {
      await createHospital(editForm)
      ElMessage.success('新增成功')
    } else if (activeId.value) {
      await updateHospital(activeId.value, editForm)
      ElMessage.success('更新成功')
    }

    editVisible.value = false
    await refreshDesk()
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(
      getErrorMessage(
        error,
        editMode.value === 'create' ? '新增医院失败，请稍后重试' : '更新医院失败，请稍后重试',
      ),
    )
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
    void loadData()
    void syncEditDialogFromRoute()
  },
)

const onDelete = async (row: HospitalItem) => {
  if (!canManageHospital.value) {
    ElMessage.warning('当前账号无删除医院权限')
    return
  }
  if (submitLoading.value || ratingLoading.value || deletingId.value === row.id) return

  deletingId.value = row.id
  try {
    await ElMessageBox.confirm(`确认删除医院“${row.hospitalName}”吗？`, '提示', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })

    await deleteHospital(row.id)
    ElMessage.success('删除成功')
    await refreshDesk()
    notifyDataChanged('global')
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      ElMessage.info('已取消删除')
    } else {
      ElMessage.error(getErrorMessage(error, '删除医院失败，请稍后重试'))
    }
  } finally {
    deletingId.value = null
  }
}

const onSaveRating = async () => {
  if (!canManageHospital.value) {
    ElMessage.warning('当前账号无医院评级权限')
    return
  }
  if (ratingLoading.value || !activeId.value) return

  ratingLoading.value = true
  try {
    await updateHospitalRating(activeId.value, ratingForm)
    ElMessage.success('评级已更新')
    if (detailItem.value?.id === activeId.value) {
      detailItem.value = {
        ...detailItem.value,
        emrRatingLevel: ratingForm.emrRatingLevel,
        interopRatingLevel: ratingForm.interopRatingLevel,
      }
    }
    ratingVisible.value = false
    await refreshDesk()
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '更新评级失败，请稍后重试'))
  } finally {
    ratingLoading.value = false
  }
}

const rowClassName = ({ row }: { row: HospitalItem }) => {
  if (hasMissingContact(row) || hasRatingGap(row)) return 'focus-row'
  return ''
}

onMounted(async () => {
  await access.ensureAccessProfileLoaded()
  restoreFilterState()
  applyDrillQuery()
  await runInitialLoad({
    tasks: [loadOverview, loadData],
    retryChecks: [
      {
        when: () => filteredRows.value.length > 0 && total.value === 0,
        task: loadData,
      },
    ],
  })
  await syncEditDialogFromRoute()
})
</script>

<style scoped>
.hospital-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.65fr) minmax(300px, 0.95fr);
  gap: 18px;
  margin-bottom: 18px;
}

.hospital-hero-main,
.hospital-control-card,
.hospital-insight-card,
.hospital-quick-action,
.hospital-summary-wrap {
  border: 1px solid var(--pms-border, #e5e7eb);
  border-radius: 8px;
  background: #fff;
  box-shadow: var(--pms-shadow-card, 0 1px 2px rgba(15, 23, 42, 0.04));
}

.hospital-hero-main {
  padding: 22px 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.hospital-hero-kicker-row,
.hospital-control-copy,
.hospital-insight-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.hospital-hero-kicker {
  font-size: 12px;
  line-height: 18px;
  font-weight: 700;
  color: #2563eb;
  text-transform: uppercase;
}

.hospital-hero-badge {
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

.hospital-hero-title {
  margin: 0;
  font-size: 30px;
  line-height: 1.2;
  font-weight: 700;
  color: #0f172a;
}

.hospital-hero-subtitle {
  font-size: 14px;
  line-height: 1.7;
  color: #475569;
  max-width: 780px;
}

.hospital-hero-signals {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.hospital-signal-card {
  min-height: 112px;
  padding: 14px 16px;
  border-radius: 8px;
  background: linear-gradient(180deg, #f8fbff 0%, #ffffff 100%);
  border: 1px solid #e2e8f0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.hospital-signal-label {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.hospital-signal-value {
  font-size: 30px;
  line-height: 1.1;
  font-weight: 700;
  color: #0f172a;
}

.hospital-signal-note {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.hospital-hero-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.hospital-control-card {
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.hospital-control-copy {
  flex-direction: column;
  align-items: flex-start;
  justify-content: flex-start;
  gap: 6px;
}

.hospital-control-title,
.hospital-insight-title {
  font-size: 16px;
  line-height: 24px;
  font-weight: 700;
  color: #0f172a;
}

.hospital-control-note,
.hospital-insight-note {
  font-size: 13px;
  line-height: 20px;
  color: #64748b;
}

.hospital-control-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.hospital-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.hospital-quick-action {
  appearance: none;
  padding: 16px;
  text-align: left;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}

.hospital-quick-action:hover {
  border-color: #bfdbfe;
  box-shadow: 0 8px 20px rgba(37, 99, 235, 0.08);
  transform: translateY(-1px);
}

.hospital-quick-title {
  display: block;
  font-size: 14px;
  line-height: 20px;
  font-weight: 700;
  color: #0f172a;
}

.hospital-quick-note {
  display: block;
  margin-top: 6px;
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.hospital-insight-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 18px;
}

.hospital-insight-card {
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 236px;
}

.hospital-insight-meta {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
  white-space: nowrap;
}

.hospital-queue-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.hospital-queue-item {
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

.hospital-queue-item:hover {
  border-color: #bfdbfe;
  background: #f8fbff;
}

.hospital-queue-main,
.hospital-queue-meta {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.hospital-queue-main strong,
.hospital-chip strong {
  font-size: 13px;
  line-height: 20px;
  font-weight: 700;
  color: #0f172a;
}

.hospital-queue-main span,
.hospital-queue-meta span,
.hospital-chip span {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.hospital-chip-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.hospital-chip {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 12px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  background: #fff;
}

.hospital-summary-wrap {
  padding: 16px;
  margin-bottom: 18px;
}

.rating-inline {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  white-space: nowrap;
}

.hospital-action-group {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: nowrap;
  white-space: nowrap;
}

:deep(.focus-row) {
  background-color: #fff7ed !important;
}

:deep(.focus-row:hover > td) {
  background-color: #ffedd5 !important;
}

@media (max-width: 1440px) {
  .hospital-hero {
    grid-template-columns: 1fr;
  }

  .hospital-insight-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 992px) {
  .hospital-hero-main,
  .hospital-control-card,
  .hospital-insight-card,
  .hospital-summary-wrap {
    padding: 16px;
  }

  .hospital-hero-signals,
  .hospital-insight-grid {
    grid-template-columns: 1fr;
  }

  .hospital-quick-grid {
    grid-template-columns: 1fr;
  }

  .hospital-hero-kicker-row,
  .hospital-insight-head {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
