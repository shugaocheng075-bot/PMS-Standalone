<template>
  <div class="page-shell">
    <div class="project-hero">
      <div class="project-hero-main">
        <div class="project-hero-kicker-row">
          <span class="project-hero-kicker">Project Ledger Desk</span>
          <span class="project-hero-badge">{{ activeProjectFilterLabel }}</span>
        </div>
        <h2 class="project-hero-title">项目台账</h2>
        <div class="project-hero-subtitle">
          在一个入口里统一查看医院、产品、组别、级别与合同状态，把高频筛选、超期识别与批量维护动作前置到首屏，再进入大表格处理明细。
        </div>

        <div class="project-hero-signals">
          <div v-for="item in projectHeroSignals" :key="item.label" class="project-signal-card">
            <span class="project-signal-label">{{ item.label }}</span>
            <strong class="project-signal-value">{{ item.value }}</strong>
            <span class="project-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="project-hero-side">
        <div class="project-control-card">
          <div class="project-control-copy">
            <span class="project-control-title">项目操作</span>
            <span class="project-control-note">当前共 {{ total }} 条记录，支持导出、重置筛选和批量维护。</span>
          </div>
          <div class="project-control-actions">
            <el-button @click="onSearch" icon="Search">刷新列表</el-button>
            <el-button @click="onReset" icon="Refresh">重置筛选</el-button>
            <el-button :loading="exporting" @click="onExport" icon="Download">导出</el-button>
            <el-button
              v-if="canManageProjects"
              type="primary"
              plain
              :disabled="selectedProjectIds.length === 0"
              @click="openBatchEdit"
              icon="Edit"
            >批量编辑</el-button>
          </div>
        </div>

        <div class="project-quick-grid">
          <button type="button" class="project-quick-action" @click="applyContractStatusQuickFilter('超期未签署')">
            <span class="project-quick-title">超期项目</span>
            <span class="project-quick-note">直接查看已超期或高风险合同项目</span>
          </button>
          <button type="button" class="project-quick-action" @click="applyContractStatusQuickFilter('合同已签署')">
            <span class="project-quick-title">已签合同</span>
            <span class="project-quick-note">复查稳定交付中的合同项目</span>
          </button>
          <button type="button" class="project-quick-action" @click="applyLevelQuickFilter('三级')">
            <span class="project-quick-title">三级医院</span>
            <span class="project-quick-note">聚焦高等级医院的交付与维护状态</span>
          </button>
          <button type="button" class="project-quick-action" @click="clearProjectQuickFilters">
            <span class="project-quick-title">清空筛选</span>
            <span class="project-quick-note">回到全量项目视图继续筛查</span>
          </button>
        </div>
      </div>
    </div>

    <section class="project-business-grid" v-loading="overviewLoading">
      <div class="project-business-panel project-business-panel--wide">
        <div class="business-panel-head">
          <div>
            <span class="business-panel-kicker">当前筛选画像</span>
            <h3 class="business-panel-title">{{ businessContextTitle }}</h3>
          </div>
          <el-tag :type="businessHealthTag.type">{{ businessHealthTag.label }}</el-tag>
        </div>
        <div class="business-metric-grid">
          <div v-for="item in businessMetrics" :key="item.label" class="business-metric-card" :class="item.tone">
            <span class="business-metric-label">{{ item.label }}</span>
            <strong class="business-metric-value">{{ item.value }}</strong>
            <span class="business-metric-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="project-business-panel">
        <div class="business-panel-head">
          <div>
            <span class="business-panel-kicker">服务责任</span>
            <h3 class="business-panel-title">人员与产品分布</h3>
          </div>
        </div>
        <div class="business-distribution">
          <div v-for="item in businessDistribution" :key="item.label" class="distribution-row">
            <div class="distribution-copy">
              <span class="distribution-label">{{ item.label }}</span>
              <strong>{{ item.name }}</strong>
            </div>
            <span class="distribution-count">{{ item.count }} 项</span>
          </div>
        </div>
      </div>
    </section>

    <section class="project-focus-section">
      <div class="project-focus-main">
        <div class="business-panel-head">
          <div>
            <span class="business-panel-kicker">重点项目</span>
            <h3 class="business-panel-title">风险与价值优先级</h3>
          </div>
          <el-button link type="primary" @click="applyContractStatusQuickFilter('超期未签署')">查看超期</el-button>
        </div>
        <div class="project-focus-list">
          <button
            v-for="item in focusProjects"
            :key="item.id"
            type="button"
            class="project-focus-item"
            @click="openDetailDrawer(item)"
          >
            <span class="focus-risk-dot" :class="riskClass(item)"></span>
            <span class="focus-main-copy">
              <strong>{{ item.hospitalName || '未填写医院' }}</strong>
              <small>{{ item.productName || '未填写产品' }} · {{ item.maintenancePersonName || '未分配维护人' }}</small>
            </span>
            <span class="focus-meta">
              {{ serviceDaysText(item) }}
            </span>
          </button>
          <div v-if="focusProjects.length === 0" class="empty-state-inline">当前筛选下暂无重点风险项目。</div>
        </div>
      </div>

      <div class="project-focus-side">
        <div class="business-panel-head">
          <div>
            <span class="business-panel-kicker">数据质量</span>
            <h3 class="business-panel-title">维护字段完整度</h3>
          </div>
        </div>
        <div class="quality-list">
          <div v-for="item in dataQualityItems" :key="item.label" class="quality-item">
            <span>{{ item.label }}</span>
            <strong>{{ item.value }}</strong>
          </div>
        </div>
      </div>
    </section>

    <ProTable
      title="项目列表"
      :data="tableData"
      :loading="loading"
      :total="total"
      v-model:page="query.page"
      v-model:size="query.size"
      @refresh="refreshProjectData"
      @pagination-change="loadData"
      stripe
      empty-text="暂无符合条件的数据"
      @selection-change="onSelectionChange"
      @row-dblclick="onRowDoubleClick"
    >
      <template #toolbar>
        <el-button :loading="exporting" @click="onExport" icon="Download">导出CSV</el-button>
        <el-button
            v-if="canManageProjects"
            type="primary"
            plain
            :disabled="selectedProjectIds.length === 0"
            @click="openBatchEdit"
           icon="Edit">批量编辑（{{ selectedProjectIds.length }}）</el-button>
      </template>

      
      <template #search>
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
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
        </el-form-item>
      </el-form>
    </template>
        <el-table-column v-if="canManageProjects" type="selection" width="46" />
        <el-table-column label="项目对象" min-width="300" sortable>
          <template #default="scope">
            <div class="ledger-project-cell">
              <strong>{{ scope.row.hospitalName || '-' }}</strong>
              <span>{{ scope.row.productName || '-' }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="opportunityNumber" label="机会号" width="112" show-overflow-tooltip />
        <el-table-column label="区域" min-width="150" show-overflow-tooltip sortable>
          <template #default="scope">
            {{ [scope.row.serviceArea, scope.row.province, scope.row.city].filter(Boolean).join(' / ') || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="责任人" min-width="178" show-overflow-tooltip>
          <template #default="scope">
            <div class="ledger-owner-cell">
              <strong>{{ scope.row.maintenancePersonName || '未分配' }}</strong>
              <span>{{ scope.row.groupName || '未分组' }} · {{ scope.row.salesName || '未填销售' }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="contractStatus" label="合同状态" min-width="148" show-overflow-tooltip sortable>
          <template #default="scope">
            <el-tag :type="statusType(getDisplayContractStatus(scope.row))">{{ getDisplayContractStatus(scope.row) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="商务价值" width="154" align="right" sortable>
          <template #default="scope">
            <div class="ledger-money-cell">
              <strong>{{ formatMoney(scope.row.salesAmount) }}</strong>
              <span>维护 {{ formatMoney(scope.row.maintenanceAmount) }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="afterSalesEndDate" label="售后结束" width="120" show-overflow-tooltip />
        <el-table-column prop="overdueDays" label="服务状态" width="136" align="right" sortable>
          <template #default="scope">
            <span :class="serviceDaysClass(scope.row)">{{ serviceDaysText(scope.row) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="148" fixed="right">
          <template #default="scope">
            <div class="table-action-group">
              <el-button link type="primary" @click="openDetailDrawer(scope.row)" icon="View">详情</el-button>
              <el-button v-if="canManageProjects" link type="primary" @click="onOpenEdit(scope.row)" icon="Edit">编辑</el-button>
            </div>
          </template>
        </el-table-column>
      

      
    </ProTable>

    <ProDrawer v-model="batchEditVisible" title="批量编辑项目台账" width="560px">
      <el-form ref="batchFormRef" :model="batchEditForm" :rules="batchEditRules" label-width="110px">
        <el-form-item label="合同状态" prop="contractStatus">
          <el-select v-model="batchEditForm.contractStatus" clearable placeholder="不修改" style="width: 100%">
            <el-option v-for="status in contractStatusOptions" :key="`batch-status-${status}`" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item label="组别" prop="groupName">
          <el-input v-model="batchEditForm.groupName" placeholder="不修改" clearable />
        </el-form-item>
        <el-form-item label="销售" prop="salesName">
          <el-input v-model="batchEditForm.salesName" placeholder="不修改" clearable />
        </el-form-item>
        <el-form-item label="维护人员" prop="maintenancePersonName">
          <el-input v-model="batchEditForm.maintenancePersonName" placeholder="不修改" clearable />
        </el-form-item>
        <el-form-item label="医院级别" prop="hospitalLevel">
          <el-select v-model="batchEditForm.hospitalLevel" clearable placeholder="不修改" style="width: 100%">
            <el-option v-for="level in levelOptions" :key="`batch-level-${level}`" :label="level" :value="level" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="batchEditVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="batchUpdating" @click="submitBatchEdit" icon="Check">提交</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="editVisible" title="编辑项目台账" width="560px">
      <el-form ref="editFormRef" :model="editForm" :rules="editFormRules" label-width="110px">
        <el-form-item label="合同状态" prop="contractStatus">
          <el-select v-model="editForm.contractStatus" clearable placeholder="请选择合同状态" style="width: 100%">
            <el-option v-for="status in contractStatusOptions" :key="`edit-status-${status}`" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item label="组别" prop="groupName">
          <el-input v-model="editForm.groupName" clearable />
        </el-form-item>
        <el-form-item label="销售" prop="salesName">
          <el-input v-model="editForm.salesName" clearable />
        </el-form-item>
        <el-form-item label="维护人员" prop="maintenancePersonName">
          <el-input v-model="editForm.maintenancePersonName" clearable />
        </el-form-item>
        <el-form-item label="医院级别" prop="hospitalLevel">
          <el-select v-model="editForm.hospitalLevel" clearable placeholder="请选择医院级别" style="width: 100%">
            <el-option v-for="level in levelOptions" :key="`edit-level-${level}`" :label="level" :value="level" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="editSubmitting" @click="editVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="editSubmitting" @click="submitEdit" icon="Check">保存</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="detailVisible" title="项目业务详情" size-class="lg">
      <div v-if="selectedProject" class="project-detail-desk">
        <div class="detail-hero-card">
          <div>
            <span class="business-panel-kicker">项目 {{ selectedProject.serialNumber || selectedProject.id }}</span>
            <h3>{{ selectedProject.hospitalName || '-' }}</h3>
            <p>{{ selectedProject.productName || '-' }} · {{ selectedProject.serviceArea || '-' }} / {{ selectedProject.province || '-' }} / {{ selectedProject.city || '-' }}</p>
          </div>
          <el-tag :type="statusType(getDisplayContractStatus(selectedProject))">{{ getDisplayContractStatus(selectedProject) }}</el-tag>
        </div>

        <div class="detail-stat-grid">
          <div v-for="item in selectedProjectStats" :key="item.label" class="detail-stat-card">
            <span>{{ item.label }}</span>
            <strong>{{ item.value }}</strong>
          </div>
        </div>

        <div class="detail-section">
          <h4>服务与责任</h4>
          <div class="detail-field-grid">
            <div v-for="item in selectedProjectServiceFields" :key="item.label" class="detail-field">
              <span>{{ item.label }}</span>
              <strong>{{ item.value }}</strong>
            </div>
          </div>
        </div>

        <div class="detail-section">
          <h4>商务与交付</h4>
          <div class="detail-field-grid">
            <div v-for="item in selectedProjectCommercialFields" :key="item.label" class="detail-field">
              <span>{{ item.label }}</span>
              <strong>{{ item.value }}</strong>
            </div>
          </div>
        </div>

        <div class="detail-section" v-if="selectedProject.remarks || selectedProject.points">
          <h4>备注</h4>
          <p class="detail-note">{{ selectedProject.remarks || selectedProject.points }}</p>
        </div>
      </div>
      <template #footer>
        <el-button @click="detailVisible = false" icon="Close">关闭</el-button>
        <el-button v-if="selectedProject && canManageProjects" type="primary" @click="onOpenEdit(selectedProject)" icon="Edit">编辑项目</el-button>
      </template>
    </ProDrawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { batchUpdateProjects, exportProjects, fetchProjectList, updateProject } from '../../api/modules/project'
import type { ProjectItem } from '../../types/project'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import { useResilientLoad } from '../../composables/useResilientLoad'
import ProTable from '../../components/ProTable.vue'
import ProDrawer from '../../components/ProDrawer.vue'


const loading = ref(false)
const exporting = ref(false)
const batchUpdating = ref(false)
const editSubmitting = ref(false)
const total = ref(0)
const tableData = ref<ProjectItem[]>([])
const overviewData = ref<ProjectItem[]>([])
const overviewLoading = ref(false)
const selectedProjectIds = ref<number[]>([])
const batchEditVisible = ref(false)
const editVisible = ref(false)
const detailVisible = ref(false)
const selectedProject = ref<ProjectItem | null>(null)
const editingId = ref<number | null>(null)
const batchFormRef = ref<FormInstance>()
const editFormRef = ref<FormInstance>()

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

const maxTextLengthValidator = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  if (!value) {
    callback()
    return
  }

  if (value.trim().length > 64) {
    callback(new Error('长度不能超过64个字符'))
    return
  }

  callback()
}

const oneOfValidator = (allowed: () => string[]) => (
  _rule: unknown,
  value: string,
  callback: (error?: Error) => void,
) => {
  if (!value) {
    callback()
    return
  }

  const options = allowed()
  if (options.length > 0 && !options.includes(value)) {
    callback(new Error('请选择有效选项'))
    return
  }

  callback()
}

const editFormRules: FormRules<typeof editForm> = {
  contractStatus: [{ validator: oneOfValidator(() => contractStatusOptions.value), trigger: 'change' }],
  groupName: [{ validator: maxTextLengthValidator, trigger: 'blur' }],
  salesName: [{ validator: maxTextLengthValidator, trigger: 'blur' }],
  maintenancePersonName: [{ validator: maxTextLengthValidator, trigger: 'blur' }],
  hospitalLevel: [{ validator: oneOfValidator(() => levelOptions.value), trigger: 'change' }],
}

const batchEditRules: FormRules<typeof batchEditForm> = {
  contractStatus: [{ validator: oneOfValidator(() => contractStatusOptions.value), trigger: 'change' }],
  groupName: [{ validator: maxTextLengthValidator, trigger: 'blur' }],
  salesName: [{ validator: maxTextLengthValidator, trigger: 'blur' }],
  maintenancePersonName: [{ validator: maxTextLengthValidator, trigger: 'blur' }],
  hospitalLevel: [{ validator: oneOfValidator(() => levelOptions.value), trigger: 'change' }],
}

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
    return '未配置'
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

const activeProjectFilterLabel = computed(() => {
  if (query.contractStatus) {
    return `合同：${query.contractStatus}`
  }

  if (query.hospitalLevel) {
    return `级别：${query.hospitalLevel}`
  }

  if (query.province) {
    return `省份：${query.province}`
  }

  if (query.productName) {
    return `产品：${query.productName}`
  }

  if (query.hospitalName) {
    return `医院：${query.hospitalName}`
  }

  return '全量项目视图'
})

const projectHeroSignals = computed(() => {
  const overdueCount = tableData.value.filter((item) => getDisplayOverdueDays(item) > 0).length
  const signedCount = tableData.value.filter((item) => getDisplayContractStatus(item) === '合同已签署').length
  const hospitalCount = new Set(tableData.value.map((item) => item.hospitalName).filter(Boolean)).size
  const productCount = new Set(tableData.value.map((item) => item.productName).filter(Boolean)).size

  return [
    {
      label: '当前页项目',
      value: String(tableData.value.length),
      note: '当前筛选结果中已加载到列表的项目数量',
    },
    {
      label: '超期风险',
      value: String(overdueCount),
      note: '已超期或需要优先关注的项目数量',
    },
    {
      label: '已签合同',
      value: String(signedCount),
      note: '当前结果中合同已签署的稳定项目数量',
    },
    {
      label: '覆盖范围',
      value: `${hospitalCount}/${productCount}`,
      note: '当前页医院数 / 产品数的覆盖密度',
    },
  ]
})

const displayText = (value: string | number | null | undefined, fallback = '-') => {
  const text = String(value ?? '').trim()
  return text || fallback
}

const toNumber = (value: number | null | undefined) => {
  const numberValue = Number(value ?? 0)
  return Number.isFinite(numberValue) ? numberValue : 0
}

const formatInteger = (value: number) => new Intl.NumberFormat('zh-CN').format(value)

const formatMoney = (value: number | null | undefined) => {
  const amount = toNumber(value)
  if (!amount) {
    return '0'
  }

  if (Math.abs(amount) >= 10000) {
    return `${(amount / 10000).toLocaleString('zh-CN', { maximumFractionDigits: 1 })}万`
  }

  return amount.toLocaleString('zh-CN')
}

const businessItems = computed(() => overviewData.value.length > 0 ? overviewData.value : tableData.value)

const countUniqueBy = (items: ProjectItem[], picker: (item: ProjectItem) => string) => {
  return new Set(items.map(picker).map((item) => item.trim()).filter(Boolean)).size
}

const sumBy = (items: ProjectItem[], picker: (item: ProjectItem) => number | null | undefined) => {
  return items.reduce((sum, item) => sum + toNumber(picker(item)), 0)
}

const topBy = (items: ProjectItem[], picker: (item: ProjectItem) => string, fallback = '未填写') => {
  const map = new Map<string, number>()
  for (const item of items) {
    const key = picker(item).trim() || fallback
    map.set(key, (map.get(key) ?? 0) + 1)
  }

  const [name, count] = [...map.entries()].sort((a, b) => b[1] - a[1])[0] ?? [fallback, 0]
  return { name, count }
}

const isDueSoon = (item: ProjectItem) => {
  const dayDiff = getDayDiffFromToday(item.afterSalesEndDate)
  return dayDiff !== null && dayDiff >= 0 && dayDiff <= 30
}

const businessStats = computed(() => {
  const items = businessItems.value
  return {
    total: items.length,
    hospitals: countUniqueBy(items, (item) => item.hospitalName ?? ''),
    products: countUniqueBy(items, (item) => item.productName ?? ''),
    overdue: items.filter((item) => getDisplayOverdueDays(item) > 0).length,
    dueSoon: items.filter(isDueSoon).length,
    signed: items.filter((item) => getDisplayContractStatus(item) === '合同已签署').length,
    onsite: items.filter((item) => String(item.isStationedOnsite ?? '').includes('是')).length,
    maintenanceAmount: sumBy(items, (item) => item.maintenanceAmount),
    salesAmount: sumBy(items, (item) => item.salesAmount),
    annualOutput: sumBy(items, (item) => item.annualOutput),
  }
})

const businessContextTitle = computed(() => {
  if (query.hospitalName && query.productName) {
    return `${query.hospitalName} / ${query.productName}`
  }

  if (query.hospitalName) {
    return `${query.hospitalName} 项目画像`
  }

  if (query.productName) {
    return `${query.productName} 产品画像`
  }

  if (query.groupName) {
    return `${query.groupName} 服务组画像`
  }

  if (query.province) {
    return `${query.province} 区域画像`
  }

  return '全量维护项目业务画像'
})

const businessHealthTag = computed(() => {
  if (businessStats.value.overdue > 0) {
    return { type: 'danger', label: '存在超期风险' }
  }

  if (businessStats.value.dueSoon > 0) {
    return { type: 'warning', label: '临期关注' }
  }

  return { type: 'success', label: '运行稳定' }
})

const businessMetrics = computed(() => {
  const stats = businessStats.value
  return [
    {
      label: '项目数',
      value: formatInteger(stats.total),
      note: `覆盖 ${formatInteger(stats.hospitals)} 家医院 / ${formatInteger(stats.products)} 个产品`,
      tone: 'is-primary',
    },
    {
      label: '风险项目',
      value: `${formatInteger(stats.overdue)} / ${formatInteger(stats.dueSoon)}`,
      note: '已超期 / 30天内到期',
      tone: stats.overdue > 0 ? 'is-danger' : 'is-warning',
    },
    {
      label: '商务规模',
      value: formatMoney(stats.salesAmount),
      note: `维护金额 ${formatMoney(stats.maintenanceAmount)}`,
      tone: 'is-accent',
    },
    {
      label: '年度产出',
      value: formatMoney(stats.annualOutput),
      note: `驻场 ${formatInteger(stats.onsite)} 项 / 已签 ${formatInteger(stats.signed)} 项`,
      tone: 'is-neutral',
    },
  ]
})

const businessDistribution = computed(() => {
  const items = businessItems.value
  return [
    { label: '主力产品', ...topBy(items, (item) => item.productName ?? '') },
    { label: '维护人员', ...topBy(items, (item) => item.maintenancePersonName ?? '', '未分配') },
    { label: '服务组', ...topBy(items, (item) => item.groupName ?? '', '未分组') },
    { label: '合同状态', ...topBy(items, (item) => getDisplayContractStatus(item), '未知') },
  ]
})

const getRiskScore = (item: ProjectItem) => {
  const overdueDays = getDisplayOverdueDays(item)
  const dayDiff = getDayDiffFromToday(item.afterSalesEndDate)
  const missingEndDate = item.afterSalesEndDate ? 0 : 80
  const dueScore = dayDiff !== null && dayDiff >= 0 && dayDiff <= 30 ? 50 : 0
  const amountScore = Math.min(toNumber(item.salesAmount) / 10000, 120)
  return overdueDays * 2 + dueScore + missingEndDate + amountScore
}

const focusProjects = computed(() => {
  return [...businessItems.value]
    .sort((a, b) => getRiskScore(b) - getRiskScore(a))
    .slice(0, 5)
})

const riskClass = (item: ProjectItem) => {
  if (getDisplayOverdueDays(item) > 0) {
    return 'is-danger'
  }

  if (isDueSoon(item) || !item.afterSalesEndDate) {
    return 'is-warning'
  }

  return 'is-success'
}

const dataQualityItems = computed(() => {
  const items = businessItems.value
  return [
    { label: '缺售后结束日期', value: formatInteger(items.filter((item) => !item.afterSalesEndDate).length) },
    { label: '缺维护人员', value: formatInteger(items.filter((item) => !item.maintenancePersonName).length) },
    { label: '维护金额为 0', value: formatInteger(items.filter((item) => toNumber(item.maintenanceAmount) <= 0).length) },
    { label: '缺机会号', value: formatInteger(items.filter((item) => !item.opportunityNumber).length) },
  ]
})

const selectedProjectStats = computed(() => {
  const item = selectedProject.value
  if (!item) {
    return []
  }

  return [
    { label: '服务状态', value: serviceDaysText(item) },
    { label: '合同状态', value: getDisplayContractStatus(item) },
    { label: '销售金额', value: formatMoney(item.salesAmount) },
    { label: '维护金额', value: formatMoney(item.maintenanceAmount) },
    { label: '年度产出', value: formatMoney(item.annualOutput) },
    { label: '驻场', value: displayText(item.isStationedOnsite, '否') },
  ]
})

const selectedProjectServiceFields = computed(() => {
  const item = selectedProject.value
  if (!item) {
    return []
  }

  return [
    { label: '服务区', value: displayText(item.serviceArea) },
    { label: '省市', value: [item.province, item.city].filter(Boolean).join(' / ') || '-' },
    { label: '服务组长/组', value: displayText(item.groupName, '未分组') },
    { label: '维护人员', value: displayText(item.maintenancePersonName, '未分配') },
    { label: '销售', value: displayText(item.salesName, '未填写') },
    { label: '医院级别', value: displayText(item.hospitalLevel, '未评定') },
    { label: '驻场地点', value: displayText(item.stationLocation) },
    { label: '驻场人数', value: displayText(item.stationedCount, '0') },
  ]
})

const selectedProjectCommercialFields = computed(() => {
  const item = selectedProject.value
  if (!item) {
    return []
  }

  return [
    { label: '机会号', value: displayText(item.opportunityNumber) },
    { label: '合同有效性', value: displayText(item.contractValidityStatus, '未维护') },
    { label: '实施状态', value: displayText(item.implementationStatus, '未维护') },
    { label: '验收日期', value: displayText(item.acceptanceDate) },
    { label: '售后开始', value: displayText(item.afterSalesStartDate) },
    { label: '售后结束', value: displayText(item.afterSalesEndDate) },
    { label: '售后项目类型', value: displayText(item.afterSalesProjectType) },
    { label: '工时/人天', value: displayText(item.workHoursManDays) },
  ]
})

const openDetailDrawer = (row: ProjectItem) => {
  selectedProject.value = row
  detailVisible.value = true
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

const loadOverviewData = async () => {
  overviewLoading.value = true
  try {
    const res = await fetchProjectList({ ...query, page: 1, size: 50000 })
    overviewData.value = res.data.items
  } catch {
    overviewData.value = [...tableData.value]
  } finally {
    overviewLoading.value = false
  }
}

const refreshProjectData = async () => {
  await Promise.all([loadData(), loadOverviewData()])
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

  detailVisible.value = false
  openEditDialog(row)
  void updateRouteQuery({ action: 'edit', id: String(row.id) })
}

const onRowDoubleClick = (row: ProjectItem) => {
  openDetailDrawer(row)
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

  const valid = await editFormRef.value?.validate().catch(() => false)
  if (!valid) {
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
    await refreshProjectData()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '更新项目失败，请稍后重试'))
  } finally {
    editSubmitting.value = false
  }
}

const submitBatchEdit = async () => {
  const valid = await batchFormRef.value?.validate().catch(() => false)
  if (!valid) {
    return
  }

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
    await refreshProjectData()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '批量更新失败，请稍后重试'))
  } finally {
    batchUpdating.value = false
  }
}

const onSearch = () => {
  query.page = 1
  void refreshProjectData()
}

const clearProjectQuickFilters = () => {
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
  void refreshProjectData()
}

const applyContractStatusQuickFilter = (status: string) => {
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
  query.contractStatus = status
  query.page = 1
  void refreshProjectData()
}

const applyLevelQuickFilter = (level: string) => {
  query.hospitalName = ''
  query.productName = ''
  query.province = ''
  query.groupName = ''
  query.salesName = ''
  query.maintenancePersonName = ''
  query.afterSalesEndDateFrom = ''
  query.afterSalesEndDateTo = ''
  afterSalesEndDateRange.value = []
  query.contractStatus = ''
  query.hospitalLevel = level
  query.page = 1
  void refreshProjectData()
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
  void refreshProjectData()
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
  await Promise.allSettled([loadFilterOptions(), refreshProjectData()])
}

const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: refreshLinkedData,
  scope: 'project',
  intervalMs: 60000,
})

const { runInitialLoad } = useResilientLoad()

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
  await runInitialLoad({
    tasks: [() => refreshLinkedData()],
    retryChecks: [
      { when: () => total.value === 0 && tableData.value.length === 0, task: refreshProjectData },
    ],
  })
  syncEditDialogFromRoute()
})
</script>

<style scoped>
.project-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.18fr) minmax(320px, 0.82fr);
  gap: 16px;
  padding: 24px;
  border-radius: 28px;
  color: #ffffff;
  background:
    radial-gradient(circle at 14% 18%, rgba(173, 216, 255, 0.18), transparent 22%),
    radial-gradient(circle at 100% 0, rgba(140, 218, 190, 0.15), transparent 24%),
    linear-gradient(145deg, #1d3f62 0%, #2f597b 48%, #3b6d63 100%);
  box-shadow: 0 26px 44px rgba(29, 63, 98, 0.16);
}

.project-hero-main {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-width: 0;
}

.project-hero-kicker-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.project-hero-kicker,
.project-hero-badge {
  display: inline-flex;
  align-items: center;
  padding: 7px 12px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 700;
}

.project-hero-kicker {
  border: 1px solid rgba(255, 255, 255, 0.18);
  background: rgba(255, 255, 255, 0.08);
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.project-hero-badge {
  background: rgba(255, 255, 255, 0.14);
  color: #eff9ff;
}

.project-hero-title {
  margin: 0;
  font-size: 34px;
  line-height: 1.12;
  font-weight: 700;
}

.project-hero-subtitle {
  max-width: 700px;
  font-size: 14px;
  line-height: 1.85;
  color: rgba(233, 244, 248, 0.84);
}

.project-hero-signals {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin-top: 4px;
}

.project-signal-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.11), rgba(255, 255, 255, 0.05));
}

.project-signal-label {
  font-size: 12px;
  color: rgba(227, 244, 247, 0.76);
}

.project-signal-value {
  font-size: 28px;
  line-height: 1;
  font-weight: 700;
}

.project-signal-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(229, 244, 246, 0.76);
}

.project-hero-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.project-control-card,
.project-quick-action {
  border: 1px solid rgba(255, 255, 255, 0.14);
  border-radius: 20px;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.14), rgba(255, 255, 255, 0.08));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

.project-control-card {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 18px;
}

.project-control-copy {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.project-control-title {
  font-size: 16px;
  font-weight: 700;
}

.project-control-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(232, 244, 246, 0.74);
}

.project-control-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.project-control-actions :deep(.el-button) {
  min-height: 40px;
}

.project-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.project-quick-action {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  color: #ffffff;
  text-align: left;
  cursor: pointer;
  transition: transform 0.2s ease, border-color 0.2s ease, background 0.2s ease;
}

.project-quick-action:hover {
  transform: translateY(-1px);
  border-color: rgba(255, 255, 255, 0.24);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.18), rgba(255, 255, 255, 0.09));
}

.project-quick-title {
  font-size: 14px;
  font-weight: 700;
}

.project-quick-note {
  font-size: 12px;
  line-height: 1.65;
  color: rgba(232, 245, 247, 0.76);
}

.table-action-group {
  flex-wrap: nowrap;
}

@media (max-width: 1280px) {
  .project-hero {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .project-hero {
    padding: 18px;
  }

  .project-hero-title {
    font-size: 28px;
  }

  .project-hero-signals,
  .project-quick-grid {
    grid-template-columns: 1fr;
  }

  .project-control-actions {
    flex-direction: column;
    align-items: stretch;
  }
}

.project-hero {
  grid-template-columns: minmax(0, 1fr) minmax(320px, 420px);
  gap: 18px;
  border: 1px solid var(--pms-border);
  border-radius: 8px;
  background: #ffffff;
  color: var(--pms-text);
  box-shadow: var(--pms-shadow-soft);
}

.project-hero-kicker,
.project-hero-badge,
.project-signal-card,
.project-control-card,
.project-quick-action {
  border-radius: 8px;
}

.project-hero-kicker {
  border-color: #d7e3ef;
  background: #f6f9fc;
  color: #477089;
  letter-spacing: 0;
}

.project-hero-badge {
  background: #eef5ff;
  color: #1262b3;
}

.project-hero-title {
  color: #102336;
}

.project-hero-subtitle,
.project-signal-label,
.project-signal-note,
.project-control-note,
.project-quick-note {
  color: #647589;
}

.project-signal-card,
.project-control-card,
.project-quick-action {
  border: 1px solid #dce6ef;
  background: #f8fafc;
  box-shadow: none;
}

.project-signal-value,
.project-control-title,
.project-quick-title {
  color: #102336;
}

.project-quick-action:hover {
  border-color: #9dc4ed;
  background: #f2f7fc;
}

.project-business-grid,
.project-focus-section {
  display: grid;
  gap: 14px;
}

.project-business-grid {
  grid-template-columns: minmax(0, 1.45fr) minmax(320px, 0.55fr);
  margin-top: 14px;
}

.project-focus-section {
  grid-template-columns: minmax(0, 1fr) minmax(300px, 360px);
  margin-top: 14px;
  margin-bottom: 14px;
}

.project-business-panel,
.project-focus-main,
.project-focus-side {
  border: 1px solid #dce6ef;
  border-radius: 8px;
  background: #ffffff;
  box-shadow: var(--pms-shadow-soft);
}

.project-business-panel,
.project-focus-main,
.project-focus-side {
  padding: 18px;
}

.business-panel-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14px;
  margin-bottom: 16px;
}

.business-panel-kicker {
  display: block;
  margin-bottom: 5px;
  font-size: 12px;
  font-weight: 700;
  color: #66798c;
}

.business-panel-title {
  margin: 0;
  color: #102336;
  font-size: 18px;
  line-height: 1.25;
}

.business-metric-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
}

.business-metric-card {
  min-height: 112px;
  border: 1px solid #dde8f1;
  border-left: 3px solid #6f879d;
  border-radius: 8px;
  padding: 14px;
  background: #f9fbfd;
}

.business-metric-card.is-primary {
  border-left-color: #1677d2;
}

.business-metric-card.is-danger {
  border-left-color: #d94b4b;
}

.business-metric-card.is-warning {
  border-left-color: #c8871a;
}

.business-metric-card.is-accent {
  border-left-color: #16846f;
}

.business-metric-label,
.business-metric-note,
.distribution-label,
.distribution-count,
.focus-meta,
.focus-main-copy small,
.quality-item span,
.detail-stat-card span,
.detail-field span {
  color: #68798a;
}

.business-metric-label,
.distribution-label,
.quality-item span,
.detail-stat-card span,
.detail-field span {
  font-size: 12px;
}

.business-metric-value {
  display: block;
  margin: 9px 0 7px;
  color: #102336;
  font-size: 24px;
  line-height: 1.08;
}

.business-metric-note {
  font-size: 12px;
  line-height: 1.55;
}

.business-distribution,
.quality-list,
.project-focus-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.distribution-row,
.quality-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  border: 1px solid #e3ebf2;
  border-radius: 8px;
  padding: 12px;
  background: #f9fbfd;
}

.distribution-copy {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 4px;
}

.distribution-copy strong {
  overflow: hidden;
  color: #102336;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.distribution-count,
.quality-item strong {
  color: #102336;
  font-weight: 700;
  white-space: nowrap;
}

.project-focus-item {
  display: grid;
  grid-template-columns: 10px minmax(0, 1fr) auto;
  align-items: center;
  gap: 12px;
  width: 100%;
  border: 1px solid #e3ebf2;
  border-radius: 8px;
  padding: 13px 14px;
  background: #ffffff;
  text-align: left;
  cursor: pointer;
  transition: border-color 0.18s ease, background 0.18s ease;
}

.project-focus-item:hover {
  border-color: #9dc4ed;
  background: #f6faff;
}

.focus-risk-dot {
  width: 9px;
  height: 32px;
  border-radius: 999px;
  background: #2e9f79;
}

.focus-risk-dot.is-danger {
  background: #d94b4b;
}

.focus-risk-dot.is-warning {
  background: #c8871a;
}

.focus-main-copy {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 5px;
}

.focus-main-copy strong,
.focus-main-copy small {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.focus-main-copy strong {
  color: #102336;
}

.focus-meta {
  font-size: 12px;
  white-space: nowrap;
}

.empty-state-inline {
  border: 1px dashed #d6e1eb;
  border-radius: 8px;
  padding: 18px;
  color: #68798a;
  text-align: center;
}

.ledger-project-cell,
.ledger-owner-cell,
.ledger-money-cell {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 5px;
}

.ledger-project-cell strong,
.ledger-owner-cell strong,
.ledger-money-cell strong {
  overflow: hidden;
  color: #102336;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ledger-project-cell span,
.ledger-owner-cell span,
.ledger-money-cell span {
  overflow: hidden;
  color: #68798a;
  font-size: 12px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ledger-money-cell {
  align-items: flex-end;
}

.project-detail-desk {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.detail-hero-card {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  border: 1px solid #dce6ef;
  border-radius: 8px;
  padding: 18px;
  background: #f8fafc;
}

.detail-hero-card h3 {
  margin: 0;
  color: #102336;
  font-size: 22px;
}

.detail-hero-card p {
  margin: 8px 0 0;
  color: #647589;
}

.detail-stat-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;
}

.detail-stat-card,
.detail-field {
  border: 1px solid #e3ebf2;
  border-radius: 8px;
  padding: 12px;
  background: #ffffff;
}

.detail-stat-card strong,
.detail-field strong {
  display: block;
  margin-top: 6px;
  color: #102336;
  line-height: 1.45;
}

.detail-section {
  border: 1px solid #dce6ef;
  border-radius: 8px;
  padding: 16px;
  background: #ffffff;
}

.detail-section h4 {
  margin: 0 0 12px;
  color: #102336;
  font-size: 15px;
}

.detail-field-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.detail-note {
  margin: 0;
  color: #33475c;
  line-height: 1.75;
}

@media (max-width: 1180px) {
  .project-business-grid,
  .project-focus-section {
    grid-template-columns: 1fr;
  }

  .business-metric-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 768px) {
  .project-hero {
    grid-template-columns: 1fr;
  }

  .business-metric-grid,
  .detail-stat-grid,
  .detail-field-grid {
    grid-template-columns: 1fr;
  }

  .project-focus-item {
    grid-template-columns: 10px minmax(0, 1fr);
  }

  .focus-meta {
    grid-column: 2;
  }

  .detail-hero-card {
    flex-direction: column;
  }
}
</style>
