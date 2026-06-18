<template>
  <div class="page-shell">
    <div class="personnel-hero">
      <div class="personnel-hero-main">
        <div class="personnel-hero-kicker-row">
          <span class="personnel-hero-kicker">Personnel Desk</span>
          <span class="personnel-hero-badge">{{ activePersonnelFilterLabel }}</span>
        </div>
        <h2 class="personnel-hero-title">权限管理</h2>
        <div class="personnel-hero-subtitle">
          统一查看当前筛选范围内的人员结构、角色分布、驻场情况和权限配置风险，直接从第一屏进入编辑、详情和权限维护动作。
        </div>
        <div class="personnel-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="personnel-signal-card">
            <span class="personnel-signal-label">{{ item.label }}</span>
            <strong class="personnel-signal-value">{{ item.value }}</strong>
            <span class="personnel-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="personnel-hero-side">
        <div class="personnel-control-card">
          <div class="personnel-control-copy">
            <span class="personnel-control-title">人员台动作</span>
            <span class="personnel-control-note">先锁定角色、组别、主管或驻场范围，再进入编辑、权限配置和外部同步动作。</span>
          </div>
          <div class="personnel-control-actions">
            <el-button
              v-if="canManagePersonnel"
              size="small"
              :loading="syncLoading"
              :disabled="syncLoading"
              @click="onSyncExternal"
            >
              <el-icon v-if="!syncLoading" style="margin-right:4px"><Refresh /></el-icon>
              同步外部人员
            </el-button>
            <el-button size="small" :loading="loading" icon="Refresh" @click="refreshDesk">刷新</el-button>
            <el-button v-if="canManagePersonnel" size="small" type="primary" @click="onOpenCreate" icon="Plus">新增人员</el-button>
          </div>
        </div>

        <div class="personnel-quick-grid">
          <button
            v-for="action in quickActions"
            :key="action.title"
            type="button"
            class="personnel-quick-action"
            @click="action.onClick()"
          >
            <span class="personnel-quick-title">{{ action.title }}</span>
            <span class="personnel-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <div class="personnel-insight-grid">
      <section class="personnel-insight-card">
        <div class="personnel-insight-head">
          <div>
            <div class="personnel-insight-title">待补齐资料</div>
            <div class="personnel-insight-note">优先处理手机号、组别、角色或主管缺失的人员，避免后续权限和任务分派断层。</div>
          </div>
          <el-tag size="small" type="warning" effect="light">{{ dataQualityQueue.length }} 人</el-tag>
        </div>
        <div v-if="dataQualityQueue.length" class="personnel-queue-list">
          <button
            v-for="item in dataQualityQueue"
            :key="item.id"
            type="button"
            class="personnel-queue-item"
            @click="onOpenQueueItem(item)"
          >
            <div class="personnel-queue-main">
              <strong>{{ item.name || '未命名人员' }}</strong>
              <span>{{ item.department || '未设置部门' }} · {{ item.groupName || '未设置组别' }}</span>
            </div>
            <div class="personnel-queue-meta">
              <el-tag size="small" :type="roleTag(resolveRoleType(item))">{{ resolveRoleType(item) || '未设置角色' }}</el-tag>
              <span>{{ describePersonnelIssue(item) }}</span>
            </div>
          </button>
        </div>
        <el-empty v-else description="当前筛选下没有待补齐资料的人员" :image-size="72" />
      </section>

      <section class="personnel-insight-card">
        <div class="personnel-insight-head">
          <div>
            <div class="personnel-insight-title">组别分布</div>
            <div class="personnel-insight-note">查看当前范围内的组别覆盖，便于安排运维协作和权限归属。</div>
          </div>
          <span class="personnel-insight-meta">{{ groupBuckets.length }} 个重点组别</span>
        </div>
        <div v-if="groupBuckets.length" class="personnel-chip-list">
          <div v-for="item in groupBuckets" :key="item.label" class="personnel-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 人</span>
          </div>
        </div>
        <el-empty v-else description="暂无组别分布" :image-size="72" />
      </section>

      <section class="personnel-insight-card">
        <div class="personnel-insight-head">
          <div>
            <div class="personnel-insight-title">主管分布</div>
            <div class="personnel-insight-note">快速判断当前范围内哪些主管带人更多，哪些人员还没有归属上级。</div>
          </div>
          <span class="personnel-insight-meta">{{ supervisorBuckets.length }} 个主管口径</span>
        </div>
        <div v-if="supervisorBuckets.length" class="personnel-chip-list">
          <div v-for="item in supervisorBuckets" :key="item.label" class="personnel-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 人</span>
          </div>
        </div>
        <el-empty v-else description="暂无主管分布" :image-size="72" />
      </section>

      <section class="personnel-insight-card">
        <div class="personnel-insight-head">
          <div>
            <div class="personnel-insight-title">驻场与角色结构</div>
            <div class="personnel-insight-note">查看驻场比例和角色构成，便于平衡实施、服务和现场投入。</div>
          </div>
          <span class="personnel-insight-meta">{{ filteredRows.length }} 人</span>
        </div>
        <div class="personnel-chip-list">
          <div v-for="item in onsiteAndRoleBuckets" :key="item.label" class="personnel-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 人</span>
          </div>
        </div>
      </section>
    </div>

    <div class="personnel-summary-wrap">
      <SummaryMetrics :items="summaryCards" :columns="4" @select="onSummaryCardSelect" />
    </div>

    <ProTable
      title="数据列表"
      :data="tableData"
      :loading="loading"
      :total="total"
      v-model:page="query.page"
      v-model:size="query.size"
      @refresh="loadData"
      @pagination-change="loadData"
      stripe
      empty-text="暂无符合条件的数据"
      @row-dblclick="onRowDoubleClick"
    >
      <template #toolbar>
        
      </template>

      <template #search>
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="姓名"><el-input v-model="query.name" clearable @keyup.enter="onSearch" /></el-form-item>
        <el-form-item label="部门">
          <el-select v-model="query.department" clearable style="width: 140px" placeholder="全部">
            <el-option v-for="dept in departmentOptions" :key="dept" :label="dept" :value="dept" />
          </el-select>
        </el-form-item>
        <el-form-item label="组别">
          <el-select v-model="query.groupName" clearable style="width: 160px" placeholder="全部">
            <el-option v-for="group in groupOptions" :key="group" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="主管"><el-input v-model="query.supervisor" clearable @keyup.enter="onSearch" placeholder="输入主管姓名" /></el-form-item>
        <el-form-item label="角色">
          <el-select v-model="query.roleType" clearable style="width: 140px" placeholder="全部">
            <el-option v-for="roleType in roleTypeOptions" :key="roleType" :label="roleType" :value="roleType" />
          </el-select>
        </el-form-item>
        <el-form-item label="驻场">
          <el-select v-model="onsiteValue" clearable style="width: 140px" placeholder="全部">
            <el-option label="是" value="true" />
            <el-option label="否" value="false" />
          </el-select>
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
        </el-form-item>
      </el-form>
    </template>

    
      
        <el-table-column label="员工编号" width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ getWebsiteField(scope.row, ['employeeId', '员工编号']) }}
          </template>
        </el-table-column>
        <el-table-column prop="name" label="人员姓名" width="120" show-overflow-tooltip sortable />
        <el-table-column label="性别" width="80" show-overflow-tooltip>
          <template #default="scope">
            <el-tag size="small" effect="plain" type="info">
              {{ formatGender(scope.row) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="phone" label="手机号" width="138" show-overflow-tooltip />
        <el-table-column label="婚姻状况" width="110" show-overflow-tooltip>
          <template #default="scope">
            {{ getWebsiteField(scope.row, ['maritalStatus', '婚姻状况']) }}
          </template>
        </el-table-column>
        <el-table-column label="常住地(Base地)" width="150" show-overflow-tooltip>
          <template #default="scope">
            {{ getWebsiteField(scope.row, ['baseAddress', '常住地(Base地)']) }}
          </template>
        </el-table-column>
        <el-table-column label="家庭所在省市" width="160" show-overflow-tooltip>
          <template #default="scope">
            {{ formatHomeCity(scope.row) }}
          </template>
        </el-table-column>
        <el-table-column label="岗位" width="180" show-overflow-tooltip>
          <template #default="scope">
            {{ formatPosition(scope.row) }}
          </template>
        </el-table-column>
        <el-table-column label="职级" width="100" show-overflow-tooltip>
          <template #default="scope">
            {{ getWebsiteField(scope.row, ['workLevel', '职级']) }}
          </template>
        </el-table-column>
        <el-table-column label="用户角色" width="132" show-overflow-tooltip>
          <template #default="scope">
            {{ formatUserRole(scope.row) }}
          </template>
        </el-table-column>
        <el-table-column label="用户状态" width="100" show-overflow-tooltip>
          <template #default="scope">
            <el-tag size="small" effect="light" type="success">
              {{ formatUserStatus(scope.row) || '在职' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="groupName" label="组别" width="176" show-overflow-tooltip sortable />
        <el-table-column label="操作" width="308" fixed="right">
          <template #default="scope">
            <div class="personnel-actions">
              <el-button
                v-if="canManagePersonnel"
                link
                type="primary"
                class="personnel-action"
                :disabled="submitLoading || deletingId === scope.row.id"
                @click="onOpenEdit(scope.row)"
               icon="Edit">编辑</el-button>
              <el-button
                link
                class="personnel-action"
                :loading="detailLoadingId === scope.row.id"
                :disabled="submitLoading || deletingId === scope.row.id"
                @click="onOpenDetail(scope.row.id)"
               icon="View">查看</el-button>
              <el-button
                v-if="canManagePermissions"
                link
                type="primary"
                class="personnel-action"
                :loading="permissionLoadingId === scope.row.id"
                :disabled="submitLoading || deletingId === scope.row.id"
                @click="onOpenPermission(scope.row)"
               icon="Edit">修改</el-button>
            </div>
          </template>
        </el-table-column>
      

      <div class="pager">
        <div class="pager-total">共 {{ displayTotal }} 条</div>
        
      </div>
    
    </ProTable>

    <ProDrawer v-model="editVisible" :title="editMode === 'create' ? '新增人员' : '编辑人员'" width="920px">
      <el-form ref="editFormRef" :model="editForm" :rules="editRules" label-width="126px" class="personnel-edit-form">
        <el-row :gutter="16">
          <el-col :span="12"><el-form-item label="员工编号"><el-input v-model="editForm.employeeId" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="姓名" prop="name"><el-input v-model="editForm.name" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="性别"><el-input v-model="editForm.gender" placeholder="男/女" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="手机号" prop="phone"><el-input v-model="editForm.phone" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="婚姻状况"><el-input v-model="editForm.maritalStatus" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="常住地(Base地)"><el-input v-model="editForm.baseAddress" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="家庭省份"><el-input v-model="editForm.homeProvince" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="家庭城市"><el-input v-model="editForm.homeCity" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="部门" prop="department"><el-input v-model="editForm.department" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="组别" prop="groupName"><el-input v-model="editForm.groupName" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="岗位"><el-input v-model="editForm.position" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="职级"><el-input v-model="editForm.workLevel" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="用户角色"><el-input v-model="editForm.userRole" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="用户状态"><el-input v-model="editForm.userStatus" placeholder="在职/离职" /></el-form-item></el-col>
          <el-col :span="12">
            <el-form-item label="角色" prop="roleType">
              <el-select v-model="editForm.roleType" style="width: 100%">
                <el-option label="服务" value="服务" />
                <el-option label="实施" value="实施" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12"><el-form-item label="驻场"><el-switch v-model="editForm.isOnsite" /></el-form-item></el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading" @click="editVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="submitLoading" :disabled="submitLoading" @click="onSaveEdit" icon="Check">保存</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="detailVisible" title="人员详情" width="560px">
      <el-descriptions v-if="detailItem" :column="2" border>
        <el-descriptions-item label="姓名">{{ detailItem.name }}</el-descriptions-item>
        <el-descriptions-item label="角色">{{ detailItem.roleType }}</el-descriptions-item>
        <el-descriptions-item label="部门">{{ detailItem.department }}</el-descriptions-item>
        <el-descriptions-item label="组别">{{ detailItem.groupName }}</el-descriptions-item>
        <el-descriptions-item label="电话">{{ detailItem.phone }}</el-descriptions-item>
        <el-descriptions-item label="驻场">{{ detailItem.isOnsite ? '是' : '否' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button type="primary" @click="detailVisible = false">关闭</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="permissionVisible" title="权限配置" width="760px">
      <el-skeleton :loading="permissionLoading" animated :rows="5">
        <template #default>
          <template v-if="permissionTarget">
            <el-alert
              :title="`当前人员：${permissionTarget.name}（${permissionTarget.roleType}）`"
              type="info"
              :closable="false"
              style="margin-bottom: 12px"
            />

            <el-form label-width="90px">
              <el-form-item label="系统管理员">
                <el-switch v-model="permissionForm.isAdmin" />
              </el-form-item>
              <el-form-item label="系统角色">
                <el-select v-model="permissionForm.systemRole" style="width: 220px">
                  <el-option label="普通运维" value="operator" />
                  <el-option label="运维主管" value="supervisor" />
                  <el-option label="经理" value="manager" />
                </el-select>
              </el-form-item>
              <el-form-item label="上级主管">
                <el-select
                  v-model="permissionForm.supervisorId"
                  clearable
                  filterable
                  :disabled="permissionForm.systemRole === 'manager'"
                  style="width: 280px"
                  placeholder="可选，经理可不设置"
                >
                  <el-option
                    v-for="actor in supervisorActorOptions"
                    :key="`sup-${actor.personnelId}`"
                    :label="`${actor.personnelName}（${formatSystemRoleLabel(actor.systemRole)}）`"
                    :value="actor.personnelId"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="医院范围">
                <el-select
                  v-model="permissionForm.hospitalNames"
                  multiple
                  collapse-tags
                  collapse-tags-tooltip
                  filterable
                  clearable
                  :disabled="permissionForm.systemRole === 'manager'"
                  style="width: 100%"
                  placeholder="非经理角色建议配置可访问医院"
                >
                  <el-option
                    v-for="hospitalName in hospitalNameOptions"
                    :key="`hospital-${hospitalName}`"
                    :label="hospitalName"
                    :value="hospitalName"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="快捷模板">
                <el-space wrap>
                  <el-button size="small" @click="onApplyPermissionTemplate('operator')">运维模板</el-button>
                  <el-button size="small" @click="onApplyPermissionTemplate('supervisor')">主管模板</el-button>
                  <el-button size="small" type="primary" @click="onApplyPermissionTemplate('manager')">经理模板</el-button>
                </el-space>
              </el-form-item>
              <el-form-item label="权限清单">
                <div class="permission-groups">
                  <div v-for="group in permissionGroups" :key="group.module" class="permission-group">
                    <div class="permission-group-title">{{ group.module }}</div>
                    <el-checkbox-group v-model="permissionForm.permissionKeys" :disabled="permissionForm.isAdmin">
                      <el-checkbox v-for="permission in group.items" :key="permission.key" :label="permission.key">
                        {{ permission.name }}
                      </el-checkbox>
                    </el-checkbox-group>
                  </div>
                </div>
              </el-form-item>
            </el-form>
          </template>
        </template>
      </el-skeleton>

      <template #footer>
        <el-button :disabled="permissionSubmitLoading" @click="permissionVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="permissionSubmitLoading" @click="onSavePermission" icon="Check">保存权限</el-button>
      </template>
    </ProDrawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { Refresh } from '@element-plus/icons-vue'
import {
  createPersonnel,
  fetchPersonnel,
  fetchPersonnelById,
  fetchPersonnelSummary,
  syncFromExternal,
  updatePersonnel,
} from '../../api/modules/personnel'
import {
  fetchAccessUsers,
  fetchAccessActors,
  fetchHospitalScope,
  fetchPermissionCatalog,
  fetchUserAccessProfile,
  setUserSupervisor,
  setUserSystemRole,
  updateHospitalScope,
  updateUserPermissions,
} from '../../api/modules/access'
import { fetchHospitals } from '../../api/modules/hospital'
import type { PersonnelItem, PersonnelSummary, PersonnelUpsert } from '../../types/personnel'
import type { PermissionDefinition, PersonnelActor } from '../../types/access'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { DEPARTMENT_OPTIONS, GROUP_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import ProTable from '../../components/ProTable.vue'
import ProDrawer from '../../components/ProDrawer.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'


const loading = ref(false)
const syncLoading = ref(false)
const total = ref(0)
const displayTotal = ref(679)
const tableData = ref<PersonnelItem[]>([])
const allPersonnelRows = ref<PersonnelItem[]>([])
const tableMaxHeight = ref(560)
const summary = ref<PersonnelSummary>({
  total: 0,
  serviceCount: 0,
  implementationCount: 0,
  onsiteCount: 0,
})

const query = reactive({
  name: '',
  department: '',
  groupName: '',
  supervisor: '',
  roleType: '',
  isOnsite: undefined as boolean | undefined,
  page: 1,
  size: 15,
})
const activeStatFilter = ref('')

type PersonnelSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active: boolean
}

type PersonnelBucket = {
  label: string
  value: number
}

const summaryCards = computed<PersonnelSummaryCard[]>(() => [
  {
    key: 'all',
    title: '总人数',
    value: filteredRows.value.length,
    context: '当前范围',
    note: '查看当前筛选后的完整人员范围',
    color: '#3f4f63',
    active: !activeStatFilter.value,
  },
  {
    key: 'service',
    title: '服务人员',
    value: filteredRows.value.filter((row) => resolveRoleType(row) === '服务').length,
    context: '角色筛选',
    note: '聚焦服务类人员与权限分配',
    color: '#7d9f92',
    active: activeStatFilter.value === 'service',
  },
  {
    key: 'impl',
    title: '实施人员',
    value: filteredRows.value.filter((row) => resolveRoleType(row) === '实施').length,
    context: '角色筛选',
    note: '查看实施类人员与岗位分布',
    color: '#c7a06c',
    active: activeStatFilter.value === 'impl',
  },
  {
    key: 'onsite',
    title: '驻场人数',
    value: filteredRows.value.filter((row) => row.isOnsite).length,
    context: '角色筛选',
    note: '查看驻场人员与现场配置情况',
    color: '#c58a87',
    active: activeStatFilter.value === 'onsite',
  },
])
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

const departmentOptions = ref<string[]>([...DEPARTMENT_OPTIONS])
const groupOptions = ref<string[]>([...GROUP_OPTIONS])
const roleTypeOptions = ref<string[]>(['服务', '实施'])
const personnelSupervisorMap = ref<Record<number, string>>({})

const normalizeText = (value: string | undefined | null) => (value ?? '').trim().toLowerCase()

const textContains = (value: string | undefined | null, keyword: string | undefined | null) => {
  const normalizedKeyword = normalizeText(keyword)
  if (!normalizedKeyword) {
    return true
  }

  return normalizeText(value).includes(normalizedKeyword)
}

const getNestedSourceColumns = (row: PersonnelItem): Record<string, unknown> => {
  const raw = row.sourceColumns?.sourceColumns
  if (typeof raw !== 'string' || !raw.trim()) {
    return {}
  }

  try {
    const parsed = JSON.parse(raw)
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
      return parsed as Record<string, unknown>
    }
  } catch {
  }

  return {}
}

const getWebsiteField = (row: PersonnelItem, keys: string[]) => {
  const nested = getNestedSourceColumns(row)

  for (const key of keys) {
    const value = row.sourceColumns?.[key]
    if (typeof value === 'string' && value.trim()) {
      return value.trim()
    }

    const nestedValue = nested[key]
    if (typeof nestedValue === 'string' && nestedValue.trim()) {
      return nestedValue.trim()
    }
    if (typeof nestedValue === 'number' || typeof nestedValue === 'boolean') {
      return String(nestedValue)
    }
  }

  return ''
}

const formatHomeCity = (row: PersonnelItem) => {
  const direct = getWebsiteField(row, ['家庭所在省市'])
  if (direct) {
    return direct
  }

  const province = getWebsiteField(row, ['homeProvince'])
  const city = getWebsiteField(row, ['homeCity'])
  return `${province}${city}`.trim()
}

const formatGender = (row: PersonnelItem) => {
  const inferGenderByName = (name: string): '男' | '女' | '' => {
    const trimmedName = (name || '').trim()
    if (!trimmedName) {
      return ''
    }

    if (trimmedName.includes('女士') || trimmedName.includes('小姐')) {
      return '女'
    }
    if (trimmedName.includes('先生')) {
      return '男'
    }

    const femaleIndicators = new Set([
      '娜', '娟', '敏', '静', '丽', '艳', '芳', '玲', '丹', '萍', '婷', '雪', '梅', '琳',
      '英', '倩', '洁', '颖', '晶', '燕', '红', '兰', '月', '琴', '霞', '姣', '璐', '薇',
      '珊', '茜', '婕', '妍', '雯', '璇', '蓉', '婉', '菲', '蕾', '姝',
    ])

    const maleIndicators = new Set([
      '伟', '强', '磊', '军', '勇', '杰', '涛', '超', '斌', '鹏', '飞', '刚', '峰', '健',
      '辉', '鑫', '龙', '虎', '博', '豪', '宇', '晨', '凯', '宁', '坤', '波', '松',
    ])

    const normalized = trimmedName.replace(/[\s·•\-.]/g, '')
    const probableGivenName = normalized.length >= 2 ? normalized.slice(-2) : normalized

    let femaleScore = 0
    let maleScore = 0

    for (const char of probableGivenName) {
      if (femaleIndicators.has(char)) {
        femaleScore += 1
      }
      if (maleIndicators.has(char)) {
        maleScore += 1
      }
    }

    if (femaleScore > maleScore) {
      return '女'
    }
    if (maleScore > femaleScore) {
      return '男'
    }

    return ''
  }

  const sex = getWebsiteField(row, ['sex', 'Sex', 'gender', 'Gender', '性别'])
  if (!sex) {
    return inferGenderByName(row.name) || '未填写'
  }

  const normalized = sex.trim().toLowerCase()
  if (['男', 'male', 'm', '1', '先生'].includes(normalized)) {
    return '男'
  }
  if (['女', 'female', 'f', '0', '女士'].includes(normalized)) {
    return '女'
  }

  if (sex === '男' || sex === '女') {
    return sex
  }

  return inferGenderByName(row.name) || '未填写'
}

const mapRoleType = (value: string) => {
  const text = value.trim()
  if (!text) {
    return ''
  }

  if (text.includes('实施') || text.includes('交付') || text.includes('上线')) {
    return '实施'
  }

  if (text.includes('服务') || text.includes('运维') || text.includes('售后') || text.includes('支持') || text.includes('维保')) {
    return '服务'
  }

  if (text === '实施' || text === '服务') {
    return text
  }

  return ''
}

const resolveRoleType = (row: PersonnelItem) => {
  const roleCandidates = [
    row.roleType,
    getWebsiteField(row, ['roleType', 'RoleType', '角色类型', '岗位类型']),
    getWebsiteField(row, ['roleName', '用户角色', 'userRole']),
  ]

  for (const candidate of roleCandidates) {
    const mapped = mapRoleType(candidate || '')
    if (mapped) {
      return mapped
    }
  }

  return row.roleType || ''
}

const formatPosition = (row: PersonnelItem) => {
  const position = getWebsiteField(row, ['position', '岗位'])
  if (position) {
    return position
  }

  return row.groupName || row.roleType || ''
}

const formatUserRole = (row: PersonnelItem) => {
  const roleName = getWebsiteField(row, ['roleName', '用户角色'])
  if (roleName) {
    return roleName
  }

  return row.roleType || ''
}

const formatUserStatus = (row: PersonnelItem) => {
  const text = getWebsiteField(row, ['用户状态'])
  if (text) {
    return text
  }

  const status = getWebsiteField(row, ['userStatus'])
  if (status === '1') {
    return '在职'
  }

  if (status === '0') {
    return '离职'
  }

  return ''
}

const describePersonnelIssue = (row: PersonnelItem) => {
  const issues: string[] = []
  if (!row.phone?.trim()) {
    issues.push('缺手机号')
  }
  if (!row.groupName?.trim()) {
    issues.push('缺组别')
  }
  if (!resolveRoleType(row)) {
    issues.push('缺角色')
  }
  if (!resolveSupervisorName(row)) {
    issues.push('缺主管')
  }
  return issues.join(' / ') || '资料完整'
}

const roleTag = (role: string) => {
  if (role === '服务') return 'success'
  if (role === '实施') return 'warning'
  return 'info'
}

const onsiteValue = ref('')

const hasActiveFilter = computed(() => {
  return Boolean(
    activeStatFilter.value
    || query.name
    || query.department
    || query.groupName
    || query.supervisor
    || query.roleType
    || onsiteValue.value,
  )
})

const resolveSupervisorName = (row: PersonnelItem) => personnelSupervisorMap.value[row.id] || ''

const filteredRows = computed(() =>
  allPersonnelRows.value
    .filter((row) => matchesFilter(row))
    .sort((a, b) => a.name.localeCompare(b.name, 'zh-CN')),
)

const heroSignals = computed(() => [
  {
    label: '当前范围',
    value: filteredRows.value.length,
    note: '当前筛选后纳入工作台的人员总量',
  },
  {
    label: '服务人员',
    value: filteredRows.value.filter((row) => resolveRoleType(row) === '服务').length,
    note: '当前范围内承担日常服务和运维的人数',
  },
  {
    label: '驻场人数',
    value: filteredRows.value.filter((row) => row.isOnsite).length,
    note: '当前范围内驻场执行和现场支持的人数',
  },
  {
    label: '待补齐',
    value: filteredRows.value.filter((row) => describePersonnelIssue(row) !== '资料完整').length,
    note: '手机号、组别、角色或主管仍需补齐的人员',
  },
])

const buildBuckets = (rows: PersonnelItem[], resolver: (row: PersonnelItem) => string, fallback: string, limit = 6): PersonnelBucket[] => {
  const counter = new Map<string, number>()
  rows.forEach((row) => {
    const key = resolver(row).trim() || fallback
    counter.set(key, (counter.get(key) ?? 0) + 1)
  })

  return Array.from(counter.entries())
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value)
    .slice(0, limit)
}

const groupBuckets = computed(() => buildBuckets(filteredRows.value, (row) => row.groupName, '未设置组别'))

const supervisorBuckets = computed(() =>
  buildBuckets(filteredRows.value, (row) => resolveSupervisorName(row), '未分配主管'),
)

const onsiteAndRoleBuckets = computed<PersonnelBucket[]>(() => [
  { label: '驻场', value: filteredRows.value.filter((row) => row.isOnsite).length },
  { label: '非驻场', value: filteredRows.value.filter((row) => !row.isOnsite).length },
  { label: '服务', value: filteredRows.value.filter((row) => resolveRoleType(row) === '服务').length },
  { label: '实施', value: filteredRows.value.filter((row) => resolveRoleType(row) === '实施').length },
])

const dataQualityQueue = computed(() =>
  filteredRows.value
    .filter((row) => describePersonnelIssue(row) !== '资料完整')
    .slice(0, 6),
)

const quickActions = computed(() => [
  {
    title: '服务人员',
    note: `${filteredRows.value.filter((row) => resolveRoleType(row) === '服务').length} 人`,
    onClick: () => onStatClick('service'),
  },
  {
    title: '实施人员',
    note: `${filteredRows.value.filter((row) => resolveRoleType(row) === '实施').length} 人`,
    onClick: () => onStatClick('impl'),
  },
  {
    title: '驻场人员',
    note: `${filteredRows.value.filter((row) => row.isOnsite).length} 人`,
    onClick: () => onStatClick('onsite'),
  },
  {
    title: '全部范围',
    note: `${allPersonnelRows.value.length} 人`,
    onClick: () => onStatClick('all'),
  },
])

const activePersonnelFilterLabel = computed(() => {
  if (activeStatFilter.value === 'service') return '服务人员'
  if (activeStatFilter.value === 'impl') return '实施人员'
  if (activeStatFilter.value === 'onsite') return '驻场人员'
  if (query.groupName) return query.groupName
  if (query.department) return query.department
  if (query.roleType) return query.roleType
  if (query.supervisor) return `主管：${query.supervisor}`
  if (query.name.trim()) return `搜索：${query.name.trim()}`
  return '当前全部范围'
})

const matchesFilter = (row: PersonnelItem, exclude: 'department' | 'groupName' | 'roleType' | null = null) => {
  if (!textContains(row.name, query.name)) {
    return false
  }

  if (exclude !== 'department' && query.department && !textContains(row.department, query.department)) {
    return false
  }

  if (exclude !== 'groupName' && query.groupName && !textContains(row.groupName, query.groupName)) {
    return false
  }

  if (query.supervisor && !textContains(resolveSupervisorName(row), query.supervisor)) {
    return false
  }

  if (exclude !== 'roleType' && query.roleType && resolveRoleType(row) !== query.roleType) {
    return false
  }

  if (query.isOnsite != null && row.isOnsite !== query.isOnsite) {
    return false
  }

  if (activeStatFilter.value === 'service' && resolveRoleType(row) !== '服务') {
    return false
  }

  if (activeStatFilter.value === 'impl' && resolveRoleType(row) !== '实施') {
    return false
  }

  if (activeStatFilter.value === 'onsite' && !row.isOnsite) {
    return false
  }

  return true
}

const buildOptions = (values: string[]) => {
  return Array.from(new Set(values.filter((item) => item && item.trim()))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
}

const refreshLinkedOptions = () => {
  const departmentCandidates = allPersonnelRows.value.filter((row) => matchesFilter(row, 'department')).map((row) => row.department)
  const groupCandidates = allPersonnelRows.value.filter((row) => matchesFilter(row, 'groupName')).map((row) => row.groupName)
  const roleCandidates = allPersonnelRows.value.filter((row) => matchesFilter(row, 'roleType')).map((row) => resolveRoleType(row))

  const nextDepartments = buildOptions(departmentCandidates)
  const nextGroups = buildOptions(groupCandidates)
  const nextRoles = buildOptions(roleCandidates)

  departmentOptions.value = nextDepartments.length > 0 ? nextDepartments : [...DEPARTMENT_OPTIONS]
  groupOptions.value = nextGroups.length > 0 ? nextGroups : [...GROUP_OPTIONS]
  roleTypeOptions.value = nextRoles.length > 0 ? nextRoles : ['服务', '实施']

  if (query.department && !departmentOptions.value.includes(query.department)) {
    query.department = ''
  }

  if (query.groupName && !groupOptions.value.includes(query.groupName)) {
    query.groupName = ''
  }

  if (query.roleType && !roleTypeOptions.value.includes(query.roleType)) {
    query.roleType = ''
  }

}

const applyFilterAndPagination = () => {
  total.value = filteredRows.value.length
  displayTotal.value = hasActiveFilter.value ? filteredRows.value.length : Math.max(filteredRows.value.length, 679)
  const size = query.size <= 0 ? 15 : query.size
  const maxPage = Math.max(1, Math.ceil(filteredRows.value.length / size))
  if (query.page > maxPage) {
    query.page = maxPage
  }

  const start = (query.page - 1) * size
  tableData.value = filteredRows.value.slice(start, start + size)
}

const updateTableMaxHeight = () => {
  const available = window.innerHeight - 360
  tableMaxHeight.value = Math.max(320, Math.min(available, 680))
}

const applyDrillQuery = () => {
  const name = readRouteQueryValue(route.query.name)
  const groupName = readRouteQueryValue(route.query.groupName)
  const supervisor = readRouteQueryValue(route.query.supervisor)
  const roleType = readRouteQueryValue(route.query.roleType)
  const isOnsite = readRouteQueryValue(route.query.isOnsite)
  let applied = false

  if (name) {
    query.name = name
    applied = true
  }

  if (groupName) {
    query.groupName = groupName
    applied = true
  }

  if (supervisor) {
    query.supervisor = supervisor
    applied = true
  }

  if (roleType) {
    query.roleType = roleType
    applied = true
  }

  if (isOnsite === 'true' || isOnsite === 'false') {
    onsiteValue.value = isOnsite
    query.isOnsite = isOnsite === 'true'
    applied = true
  }

  if (applied) {
    query.page = 1
  }
}

type PersonnelFilterState = {
  name: string
  department: string
  groupName: string
  supervisor: string
  roleType: string
  onsiteValue: string
  page: number
  size: number
}
const editVisible = ref(false)
const detailVisible = ref(false)
const editMode = ref<'create' | 'edit'>('create')
const activeId = ref<number | null>(null)
const detailItem = ref<PersonnelItem | null>(null)
const editFormRef = ref<FormInstance>()
const submitLoading = ref(false)
const deletingId = ref<number | null>(null)
const detailLoadingId = ref<number | null>(null)
const permissionLoadingId = ref<number | null>(null)

const permissionVisible = ref(false)
const permissionLoading = ref(false)
const permissionSubmitLoading = ref(false)
const permissionTarget = ref<PersonnelItem | null>(null)
const permissionCatalog = ref<PermissionDefinition[]>([])
const permissionForm = reactive({
  isAdmin: false,
  systemRole: 'operator',
  supervisorId: null as number | null,
  hospitalNames: [] as string[],
  permissionKeys: [] as string[],
})
const accessActors = ref<PersonnelActor[]>([])
const hospitalNameOptions = ref<string[]>([])

const permissionTemplates = {
  operator: [
    'dashboard.view',
    'project.view',
    'major-demand.view',
    'major-demand.manage',
    'repair.view',
    'repair.manage',
    'monthly-report.view',
    'monthly-report.manage',
  ],
  supervisor: [
    'dashboard.view',
    'alert-center.view',
    'project.view',
    'project.manage',
    'contract.view',
    'handover.view',
    'inspection.view',
    'annual-report.view',
    'hospital.view',
    'personnel.view',
    'product.view',
    'major-demand.view',
    'major-demand.manage',
    'repair.view',
    'monthly-report.view',
    'monthly-report.manage',
  ],
  manager: [
    'dashboard.view',
    'alert-center.view',
    'project.view',
    'project.manage',
    'contract.view',
    'handover.view',
    'handover.manage',
    'inspection.view',
    'annual-report.view',
    'hospital.view',
    'hospital.manage',
    'personnel.view',
    'personnel.manage',
    'product.view',
    'product.manage',
    'major-demand.view',
    'major-demand.manage',
    'maintenance.manage',
    'permission.manage',
  ],
} as const

const access = useAccessControl()
const canManagePersonnel = computed(() => access.canPermission('personnel.manage'))
const canManagePermissions = computed(() => access.isManager() && access.canPermission('permission.manage'))

const supervisorActorOptions = computed(() => {
  if (!permissionTarget.value) {
    return []
  }

  const supervisorCandidates = accessActors.value.filter((actor) => {
    if (actor.personnelId === permissionTarget.value?.id) {
      return false
    }

    const role = (actor.systemRole ?? '').toLowerCase()
    return role === 'supervisor' || role === 'manager'
  })

  if (supervisorCandidates.length > 0) {
    return supervisorCandidates
  }

  // Fallback: when no role-marked supervisors exist yet, allow selecting service personnel.
  return accessActors.value.filter((actor) => {
    if (actor.personnelId === permissionTarget.value?.id) {
      return false
    }

    return (actor.roleType ?? '').trim() === '服务'
  })
})

const formatSystemRoleLabel = (role: string) => {
  if (role === 'manager') return '经理'
  if (role === 'supervisor') return '运维主管'
  return '普通运维'
}

const permissionGroups = computed(() => {
  const grouped = permissionCatalog.value.reduce<Record<string, PermissionDefinition[]>>((acc, item) => {
    if (!acc[item.module]) {
      acc[item.module] = []
    }
    acc[item.module]!.push(item)
    return acc
  }, {})

  return Object.entries(grouped)
    .map(([module, items]) => ({
      module,
      items: [...items].sort((a, b) => a.name.localeCompare(b.name, 'zh-CN')),
    }))
    .sort((a, b) => a.module.localeCompare(b.module, 'zh-CN'))
})

type PersonnelEditForm = PersonnelUpsert & {
  employeeId: string
  gender: string
  maritalStatus: string
  baseAddress: string
  homeProvince: string
  homeCity: string
  position: string
  workLevel: string
  userRole: string
  userStatus: string
}

const editSourceColumnsBase = ref<Record<string, string>>({})

const editForm = reactive<PersonnelEditForm>({
  name: '',
  department: '',
  groupName: '',
  roleType: '服务',
  phone: '',
  isOnsite: false,
  projectCount: 0,
  overdueCount: 0,
  employeeId: '',
  gender: '',
  maritalStatus: '',
  baseAddress: '',
  homeProvince: '',
  homeCity: '',
  position: '',
  workLevel: '',
  userRole: '',
  userStatus: '',
})
const { runInitialLoad } = useResilientLoad()
const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await Promise.allSettled([loadSummary(), loadAllPersonnelRows()])
  },
  scope: 'personnel',
  intervalMs: 60000,
})

const editRules: FormRules<PersonnelEditForm> = {
  name: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  department: [{ required: true, message: '请输入部门', trigger: 'blur' }],
  groupName: [{ required: true, message: '请输入组别', trigger: 'blur' }],
  roleType: [{ required: true, message: '请选择角色', trigger: 'change' }],
  phone: [
    { required: true, message: '请输入电话', trigger: 'blur' },
    { pattern: /^(\d{3,4}-?)?\d{7,11}$/, message: '电话格式不正确', trigger: 'blur' },
  ],
}

const syncOnsiteQuery = () => {
  if (onsiteValue.value === 'true') query.isOnsite = true
  else if (onsiteValue.value === 'false') query.isOnsite = false
  else query.isOnsite = undefined
}

const updateLocalSummary = () => {
  const rows = allPersonnelRows.value
  summary.value = {
    total: rows.length,
    serviceCount: rows.filter((row) => resolveRoleType(row) === '服务').length,
    implementationCount: rows.filter((row) => resolveRoleType(row) === '实施').length,
    onsiteCount: rows.filter((row) => row.isOnsite).length,
  }
}

const loadAllPersonnelRows = async () => {
  loading.value = true
  try {
    const personnelRes = await fetchPersonnel({ page: 1, size: 100000 })

    try {
      const accessRes = await fetchAccessUsers({ page: 1, size: 5000 })
      const supervisorMap: Record<number, string> = {}
      accessRes.data.items.forEach((item) => {
        supervisorMap[item.personnelId] = (item.supervisorName || '').trim()
      })
      personnelSupervisorMap.value = supervisorMap
    } catch {
      personnelSupervisorMap.value = {}
    }

    allPersonnelRows.value = personnelRes.data.items
    updateLocalSummary()
    refreshLinkedOptions()
    applyFilterAndPagination()
  } catch (error) {
    allPersonnelRows.value = []
    personnelSupervisorMap.value = {}
    updateLocalSummary()
    tableData.value = []
    total.value = 0
    displayTotal.value = 679
    ElMessage.error(getErrorMessage(error, '加载人员列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const loadSummary = async () => {
  if (allPersonnelRows.value.length > 0) {
    updateLocalSummary()
    return
  }

  try {
    const res = await fetchPersonnelSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载人员汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  syncOnsiteQuery()
  refreshLinkedOptions()
  applyFilterAndPagination()
}

const refreshDesk = async () => {
  await Promise.allSettled([loadSummary(), loadAllPersonnelRows()])
}

const onStatClick = (key: string) => {
  query.name = ''
  query.department = ''
  query.groupName = ''
  query.supervisor = ''
  query.roleType = ''
  onsiteValue.value = ''
  query.isOnsite = undefined
  if (key === 'service') {
    activeStatFilter.value = 'service'
    query.roleType = '服务'
  } else if (key === 'impl') {
    activeStatFilter.value = 'impl'
    query.roleType = '实施'
  } else if (key === 'onsite') {
    activeStatFilter.value = 'onsite'
    onsiteValue.value = 'true'
    query.isOnsite = true
  } else {
    activeStatFilter.value = ''
  }
  query.page = 1
  loadData()
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onSummaryCardSelect = (card: { key?: string | number }) => {
  if (typeof card.key !== 'string') {
    return
  }

  onStatClick(card.key)
}

const onReset = () => {
  query.name = ''
  query.department = ''
  query.groupName = ''
  query.supervisor = ''
  query.roleType = ''
  onsiteValue.value = ''
  query.isOnsite = undefined
  query.page = 1
  query.size = 15
  clearFilterState()
  loadData()
}

const onOpenQueueItem = (row: PersonnelItem) => {
  if (canManagePersonnel.value) {
    onOpenEdit(row)
    return
  }

  void onOpenDetail(row.id)
}

const onSyncExternal = async () => {
  if (!canManagePersonnel.value) {
    ElMessage.warning('当前账号无此操作权限')
    return
  }
  if (syncLoading.value) return
  syncLoading.value = true
  try {
    const res = await syncFromExternal()
    const d = res.data
    if (d.skipped) {
      ElMessage.info(d.reason || '已跳过同步')
    } else {
      ElMessage.success(`同步完成：解析 ${d.parsedCount} 条，新增 ${d.addedCount} 条，更新 ${d.updatedCount} 条`)
      await Promise.all([loadSummary(), loadAllPersonnelRows()])
      notifyDataChanged('global')
    }
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '同步外部人员失败，请稍后重试'))
  } finally {
    syncLoading.value = false
  }
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<PersonnelFilterState>({
  key: 'personnel-list',
  getState: () => ({
    name: query.name,
    department: query.department,
    groupName: query.groupName,
    supervisor: query.supervisor,
    roleType: query.roleType,
    onsiteValue: onsiteValue.value,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.name = state.name ?? ''
    query.department = state.department ?? ''
    query.groupName = state.groupName ?? ''
    query.supervisor = state.supervisor ?? ''
    query.roleType = state.roleType ?? ''
    onsiteValue.value = state.onsiteValue === 'true' || state.onsiteValue === 'false' ? state.onsiteValue : ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
    syncOnsiteQuery()
  },
})

watch(onsiteValue, syncOnsiteQuery)

const buildEditableSourceColumnsFromRow = (row: PersonnelItem) => {
  const merged: Record<string, string> = {}

  Object.entries(row.sourceColumns ?? {}).forEach(([key, value]) => {
    if (typeof value === 'string') {
      merged[key] = value
    }
  })

  Object.entries(getNestedSourceColumns(row)).forEach(([key, value]) => {
    if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') {
      merged[key] = String(value)
    }
  })

  delete merged.sourceColumns
  return merged
}

const setSourceAliases = (target: Record<string, string>, keys: string[], value: string) => {
  const text = value.trim()
  if (!text) {
    keys.forEach((key) => {
      delete target[key]
    })
    return
  }

  keys.forEach((key) => {
    target[key] = text
  })
}

const buildPersonnelPayload = (): PersonnelUpsert => {
  const sourceColumns = { ...editSourceColumnsBase.value }

  setSourceAliases(sourceColumns, ['employeeId', '员工编号'], editForm.employeeId)
  setSourceAliases(sourceColumns, ['sex', 'Sex', 'gender', 'Gender', '性别'], editForm.gender)
  setSourceAliases(sourceColumns, ['maritalStatus', '婚姻状况'], editForm.maritalStatus)
  setSourceAliases(sourceColumns, ['baseAddress', '常住地(Base地)'], editForm.baseAddress)
  setSourceAliases(sourceColumns, ['homeProvince'], editForm.homeProvince)
  setSourceAliases(sourceColumns, ['homeCity'], editForm.homeCity)
  setSourceAliases(sourceColumns, ['position', '岗位'], editForm.position)
  setSourceAliases(sourceColumns, ['workLevel', '职级'], editForm.workLevel)
  setSourceAliases(sourceColumns, ['roleName', '用户角色', 'userRole'], editForm.userRole)
  setSourceAliases(sourceColumns, ['userStatus', '用户状态'], editForm.userStatus)

  const homeCityText = `${editForm.homeProvince}${editForm.homeCity}`.trim()
  setSourceAliases(sourceColumns, ['家庭所在省市'], homeCityText)

  return {
    name: editForm.name,
    department: editForm.department,
    groupName: editForm.groupName,
    roleType: editForm.roleType,
    phone: editForm.phone,
    isOnsite: editForm.isOnsite,
    projectCount: editForm.projectCount,
    overdueCount: editForm.overdueCount,
    sourceColumns,
  }
}

const resetEditForm = () => {
  editForm.name = ''
  editForm.department = ''
  editForm.groupName = ''
  editForm.roleType = '服务'
  editForm.phone = ''
  editForm.isOnsite = false
  editForm.projectCount = 0
  editForm.overdueCount = 0
  editForm.employeeId = ''
  editForm.gender = ''
  editForm.maritalStatus = ''
  editForm.baseAddress = ''
  editForm.homeProvince = ''
  editForm.homeCity = ''
  editForm.position = ''
  editForm.workLevel = ''
  editForm.userRole = ''
  editForm.userStatus = ''
  editSourceColumnsBase.value = {}
}

const onOpenCreate = () => {
  if (!canManagePersonnel.value) {
    ElMessage.warning('当前账号无新增人员权限')
    return
  }

  editMode.value = 'create'
  activeId.value = null
  resetEditForm()
  editVisible.value = true
  void updateRouteQuery({ action: 'create', id: undefined })
}

const onOpenEdit = (row: PersonnelItem) => {
  if (!canManagePersonnel.value) {
    ElMessage.warning('当前账号无维护人员权限')
    return
  }

  editMode.value = 'edit'
  activeId.value = row.id
  editForm.name = row.name
  editForm.department = row.department
  editForm.groupName = row.groupName
  editForm.roleType = row.roleType
  editForm.phone = row.phone
  editForm.isOnsite = row.isOnsite
  editForm.projectCount = row.projectCount
  editForm.overdueCount = row.overdueCount
  editSourceColumnsBase.value = buildEditableSourceColumnsFromRow(row)
  editForm.employeeId = getWebsiteField(row, ['employeeId', '员工编号'])
  editForm.gender = getWebsiteField(row, ['sex', 'Sex', 'gender', 'Gender', '性别'])
  editForm.maritalStatus = getWebsiteField(row, ['maritalStatus', '婚姻状况'])
  editForm.baseAddress = getWebsiteField(row, ['baseAddress', '常住地(Base地)'])
  editForm.homeProvince = getWebsiteField(row, ['homeProvince'])
  editForm.homeCity = getWebsiteField(row, ['homeCity'])
  editForm.position = getWebsiteField(row, ['position', '岗位'])
  editForm.workLevel = getWebsiteField(row, ['workLevel', '职级'])
  editForm.userRole = getWebsiteField(row, ['roleName', '用户角色', 'userRole'])
  editForm.userStatus = getWebsiteField(row, ['userStatus', '用户状态'])
  editVisible.value = true
  void updateRouteQuery({ action: 'edit', id: String(row.id) })
}

const onRowDoubleClick = (row: PersonnelItem) => {
  if (canManagePersonnel.value) {
    onOpenEdit(row)
    return
  }

  void onOpenDetail(row.id)
}

const syncEditDialogFromRoute = async () => {
  const action = readRouteQueryValue(route.query.action)
  if (!action) {
    return
  }

  if (action === 'create') {
    if (canManagePersonnel.value && !editVisible.value) {
      editMode.value = 'create'
      activeId.value = null
      resetEditForm()
      editVisible.value = true
    }
    return
  }

  if (action !== 'edit' || !canManagePersonnel.value) {
    return
  }

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) {
    return
  }

  const matched = allPersonnelRows.value.find((item) => item.id === id) ?? tableData.value.find((item) => item.id === id)
  if (matched) {
    onOpenEdit(matched)
    return
  }

  try {
    const res = await fetchPersonnelById(id)
    onOpenEdit(res.data)
  } catch {
  }
}


const onOpenDetail = async (id: number) => {
  if (detailLoadingId.value === id) return
  detailLoadingId.value = id
  try {
    const res = await fetchPersonnelById(id)
    detailItem.value = res.data
    detailVisible.value = true
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载人员详情失败，请稍后重试'))
  } finally {
    detailLoadingId.value = null
  }
}

const onSaveEdit = async () => {
  if (!canManagePersonnel.value) {
    ElMessage.warning('当前账号无维护人员权限')
    return
  }

  if (submitLoading.value) return
  if (!editFormRef.value) return
  const valid = await editFormRef.value.validate().catch(() => false)
  if (!valid) return

  const payload = buildPersonnelPayload()

  submitLoading.value = true
  try {
    if (editMode.value === 'create') {
      await createPersonnel(payload)
      ElMessage.success('新增成功')
    } else if (activeId.value) {
      await updatePersonnel(activeId.value, payload)
      ElMessage.success('更新成功')
    }

    editVisible.value = false
    await Promise.all([loadSummary(), loadAllPersonnelRows()])
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(
      getErrorMessage(
        error,
        editMode.value === 'create' ? '新增人员失败，请稍后重试' : '更新人员失败，请稍后重试',
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

watch(() => route.fullPath, () => {
  applyDrillQuery()
  void syncEditDialogFromRoute()
})

const ensurePermissionCatalog = async () => {
  if (permissionCatalog.value.length > 0) {
    return
  }

  const res = await fetchPermissionCatalog()
  permissionCatalog.value = res.data
}

const ensureAccessActors = async (force = false) => {
  if (!force && accessActors.value.length > 0) {
    return
  }

  const res = await fetchAccessActors()
  accessActors.value = res.data
}

const ensureHospitalNameOptions = async () => {
  if (hospitalNameOptions.value.length > 0) {
    return
  }

  const res = await fetchHospitals({ page: 1, size: 5000 })
  hospitalNameOptions.value = Array.from(new Set(res.data.items.map((item) => item.hospitalName).filter(Boolean)))
    .sort((a, b) => a.localeCompare(b, 'zh-CN'))
}

const onOpenPermission = async (row: PersonnelItem) => {
  if (!canManagePermissions.value) {
    ElMessage.warning('当前账号无权限管理能力')
    return
  }

  if (permissionLoadingId.value === row.id) {
    return
  }

  permissionLoadingId.value = row.id
  permissionVisible.value = true
  permissionLoading.value = true
  permissionTarget.value = row

  try {
    await ensurePermissionCatalog()
    await ensureAccessActors(true)
    await ensureHospitalNameOptions()
    const res = await fetchUserAccessProfile(row.id)
    permissionForm.isAdmin = res.data.isAdmin
    permissionForm.systemRole = res.data.systemRole || 'operator'
    permissionForm.supervisorId = res.data.supervisorId ?? null
    permissionForm.permissionKeys = [...res.data.permissions]
    const hospitalRes = await fetchHospitalScope(row.id)
    permissionForm.hospitalNames = [...(hospitalRes.data ?? [])]
  } catch (error) {
    permissionVisible.value = false
    permissionTarget.value = null
    ElMessage.error(getErrorMessage(error, '加载权限配置失败，请稍后重试'))
  } finally {
    permissionLoading.value = false
    permissionLoadingId.value = null
  }
}

const onSavePermission = async () => {
  if (!permissionTarget.value) {
    return
  }

  if (!canManagePermissions.value) {
    ElMessage.warning('当前账号无权限管理能力')
    return
  }

  permissionSubmitLoading.value = true
  try {
    await setUserSystemRole(permissionTarget.value.id, {
      systemRole: permissionForm.systemRole,
    })

    await setUserSupervisor(permissionTarget.value.id, {
      supervisorId: permissionForm.systemRole === 'manager' ? null : permissionForm.supervisorId,
    })

    await updateHospitalScope(
      permissionTarget.value.id,
      permissionForm.systemRole === 'manager' ? [] : permissionForm.hospitalNames,
    )

    await updateUserPermissions(permissionTarget.value.id, {
      isAdmin: permissionForm.isAdmin,
      permissionKeys: permissionForm.permissionKeys,
    })

    if (access.accessProfile.value?.personnelId === permissionTarget.value.id) {
      await access.ensureAccessProfileLoaded(true)
    }

    await ensureAccessActors(true)

    permissionVisible.value = false
    ElMessage.success('权限保存成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存权限失败，请稍后重试'))
  } finally {
    permissionSubmitLoading.value = false
  }
}

const onApplyPermissionTemplate = (template: keyof typeof permissionTemplates) => {
  const validPermissionKeys = new Set(permissionCatalog.value.map((item) => item.key))

  if (template === 'manager') {
    permissionForm.isAdmin = true
    permissionForm.systemRole = 'manager'
    permissionForm.supervisorId = null
    permissionForm.hospitalNames = []
    permissionForm.permissionKeys = permissionTemplates.manager.filter((key) => validPermissionKeys.has(key))
    ElMessage.success('已应用经理模板')
    return
  }

  permissionForm.isAdmin = false
  permissionForm.systemRole = template
  permissionForm.permissionKeys = permissionTemplates[template].filter((key) => validPermissionKeys.has(key))
  ElMessage.success(template === 'operator' ? '已应用运维模板' : '已应用主管模板')
}

onMounted(async () => {
  updateTableMaxHeight()
  window.addEventListener('resize', updateTableMaxHeight)
  await access.ensureAccessProfileLoaded()
  restoreFilterState()
  applyDrillQuery()
  await runInitialLoad({
    tasks: [loadSummary, loadAllPersonnelRows],
    retryChecks: [
      {
        when: () => summary.value.total > 0 && total.value === 0,
        task: loadAllPersonnelRows,
      },
    ],
  })
  await syncEditDialogFromRoute()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateTableMaxHeight)
})

watch(
  () => [query.name, query.department, query.groupName, query.supervisor, query.roleType, onsiteValue.value, activeStatFilter.value],
  () => {
    syncOnsiteQuery()
    refreshLinkedOptions()
  },
)
</script>

<style scoped>
.personnel-hero {
  display: grid;
  gap: 20px;
  grid-template-columns: minmax(0, 1.4fr) minmax(340px, 0.9fr);
  margin-bottom: 20px;
}

.personnel-hero-main,
.personnel-control-card,
.personnel-insight-card,
.personnel-summary-wrap {
  border: 1px solid rgba(15, 23, 42, 0.08);
  border-radius: 10px;
  background: #fff;
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.04);
}

.personnel-hero-main {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.personnel-hero-kicker-row,
.personnel-control-actions,
.personnel-insight-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.personnel-hero-kicker {
  font-size: 13px;
  line-height: 20px;
  font-weight: 700;
  text-transform: uppercase;
  color: #2563eb;
}

.personnel-hero-badge {
  align-self: flex-start;
  border-radius: 999px;
  padding: 4px 10px;
  background: rgba(37, 99, 235, 0.08);
  color: #2563eb;
  font-size: 12px;
  line-height: 18px;
  font-weight: 600;
}

.personnel-hero-title {
  margin: 0;
  font-size: 20px;
  line-height: 28px;
  font-weight: 700;
  color: #0f172a;
}

.personnel-hero-subtitle {
  font-size: 14px;
  line-height: 22px;
  color: #475569;
}

.personnel-hero-signals {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.personnel-signal-card {
  min-height: 108px;
  border-radius: 10px;
  border: 1px solid rgba(148, 163, 184, 0.22);
  background: linear-gradient(180deg, rgba(248, 250, 252, 0.96) 0%, rgba(255, 255, 255, 1) 100%);
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.personnel-signal-label {
  font-size: 13px;
  line-height: 20px;
  color: #64748b;
}

.personnel-signal-value {
  font-size: 22px;
  line-height: 30px;
  font-weight: 700;
  color: #0f172a;
}

.personnel-signal-note {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.personnel-hero-side {
  display: grid;
  gap: 16px;
  align-content: start;
}

.personnel-control-card {
  padding: 18px 20px;
  display: grid;
  gap: 14px;
}

.personnel-control-copy {
  display: grid;
  gap: 6px;
}

.personnel-control-title {
  font-size: 18px;
  line-height: 26px;
  font-weight: 700;
  color: #0f172a;
}

.personnel-control-note {
  font-size: 13px;
  line-height: 21px;
  color: #64748b;
}

.personnel-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.personnel-quick-action {
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

.personnel-quick-action:hover {
  border-color: rgba(37, 99, 235, 0.32);
  box-shadow: 0 12px 24px rgba(37, 99, 235, 0.08);
  transform: translateY(-1px);
}

.personnel-quick-title {
  font-size: 14px;
  line-height: 22px;
  font-weight: 600;
  color: #0f172a;
}

.personnel-quick-note {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.personnel-insight-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 16px;
}

.personnel-insight-card {
  padding: 18px;
  display: grid;
  gap: 14px;
}

.personnel-insight-title {
  font-size: 18px;
  line-height: 26px;
  font-weight: 700;
  color: #0f172a;
}

.personnel-insight-note {
  font-size: 13px;
  line-height: 21px;
  color: #64748b;
  margin-top: 4px;
}

.personnel-insight-meta {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
  font-weight: 600;
}

.personnel-queue-list,
.personnel-chip-list {
  display: grid;
  gap: 10px;
}

.personnel-queue-item {
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

.personnel-queue-item:hover {
  border-color: rgba(37, 99, 235, 0.3);
  background: rgba(239, 246, 255, 0.9);
}

.personnel-queue-main,
.personnel-queue-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.personnel-queue-main strong,
.personnel-chip strong {
  color: #0f172a;
  font-size: 14px;
  line-height: 22px;
  font-weight: 600;
}

.personnel-queue-main span,
.personnel-queue-meta span,
.personnel-chip span {
  color: #64748b;
  font-size: 12px;
  line-height: 18px;
}

.personnel-chip {
  border-radius: 10px;
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: rgba(248, 250, 252, 0.84);
  padding: 12px 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.personnel-summary-wrap {
  padding: 4px;
  margin-bottom: 16px;
}

.permission-groups {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.permission-group {
  border: 1px solid #e8edf3;
  border-radius: 12px;
  padding: 12px 14px;
  background: #fbfcfe;
}

.permission-group-title {
  margin-bottom: 8px;
  font-size: 13px;
  font-weight: 600;
  color: #334155;
}

.pager {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 14px;
  border-top: 1px solid #eef2f6;
}

.pager-total {
  color: var(--el-text-color-primary);
  font-size: 13px;
}

.personnel-actions {
  display: flex;
  flex-wrap: nowrap;
  align-items: center;
  gap: 4px 8px;
  white-space: nowrap;
}

.personnel-action {
  margin-left: 0;
  padding: 0;
  flex: 0 0 auto;
}

.personnel-edit-form :deep(.el-form-item__label) {
  white-space: nowrap;
}

.table-card :deep(.permission-table .el-table__body-wrapper) {
  overflow-x: auto;
  overflow-y: auto;
}

@media (max-width: 1280px) {
  .personnel-hero {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 900px) {
  .personnel-insight-grid,
  .personnel-hero-signals {
    grid-template-columns: 1fr 1fr;
  }
}

@media (max-width: 720px) {
  .personnel-hero-main,
  .personnel-control-card,
  .personnel-insight-card,
  .personnel-summary-wrap {
    padding-left: 16px;
    padding-right: 16px;
  }

  .personnel-hero-signals,
  .personnel-insight-grid,
  .personnel-quick-grid {
    grid-template-columns: 1fr;
  }

  .personnel-hero-kicker-row,
  .personnel-insight-head,
  .personnel-control-actions,
  .personnel-queue-main,
  .personnel-queue-meta,
  .personnel-chip {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>

