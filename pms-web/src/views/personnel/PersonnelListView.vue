<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">权限管理</h2>
        <div class="page-subtitle">人员信息、权限模板与角色权限统一管理</div>
      </div>
      <el-space>
        <el-button
          v-if="canManagePersonnel"
          :loading="syncLoading"
          :disabled="syncLoading"
          @click="onSyncExternal"
        >
          <el-icon v-if="!syncLoading" style="margin-right:4px"><Refresh /></el-icon>
          同步外部人员
        </el-button>
        <el-button v-if="canManagePersonnel" type="primary" @click="onOpenCreate">新增人员</el-button>
      </el-space>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: !activeStatFilter }" @click="onStatClick('all')"><div class="t">总人数</div><div class="v">{{ summary.total }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: activeStatFilter === 'service' }" @click="onStatClick('service')"><div class="t">服务人员</div><div class="v success">{{ summary.serviceCount }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: activeStatFilter === 'impl' }" @click="onStatClick('impl')"><div class="t">实施人员</div><div class="v warning">{{ summary.implementationCount }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: activeStatFilter === 'onsite' }" @click="onStatClick('onsite')"><div class="t">驻场人数</div><div class="v danger">{{ summary.onsiteCount }}</div></el-card></el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
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
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table
        class="permission-table"
        :data="tableData"
        v-loading="loading"
        stripe
        border
        size="small"
        :max-height="tableMaxHeight"
        scrollbar-always-on
        empty-text="暂无符合条件的数据"
      >
        <el-table-column label="员工编号" width="110" show-overflow-tooltip>
          <template #default="scope">
            {{ getWebsiteField(scope.row, ['employeeId', '员工编号']) }}
          </template>
        </el-table-column>
        <el-table-column prop="name" label="人员姓名" width="110" show-overflow-tooltip sortable />
        <el-table-column label="性别" width="80" show-overflow-tooltip>
          <template #default="scope">
            <el-tag size="small" effect="plain" type="info">
              {{ formatGender(scope.row) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="phone" label="手机号" width="130" show-overflow-tooltip />
        <el-table-column label="婚姻状况" width="100" show-overflow-tooltip>
          <template #default="scope">
            {{ getWebsiteField(scope.row, ['maritalStatus', '婚姻状况']) }}
          </template>
        </el-table-column>
        <el-table-column label="常住地(Base地)" width="130" show-overflow-tooltip>
          <template #default="scope">
            {{ getWebsiteField(scope.row, ['baseAddress', '常住地(Base地)']) }}
          </template>
        </el-table-column>
        <el-table-column label="家庭所在省市" width="140" show-overflow-tooltip>
          <template #default="scope">
            {{ formatHomeCity(scope.row) }}
          </template>
        </el-table-column>
        <el-table-column label="岗位" width="140" show-overflow-tooltip>
          <template #default="scope">
            {{ formatPosition(scope.row) }}
          </template>
        </el-table-column>
        <el-table-column label="职级" width="100" show-overflow-tooltip>
          <template #default="scope">
            {{ getWebsiteField(scope.row, ['workLevel', '职级']) }}
          </template>
        </el-table-column>
        <el-table-column label="用户角色" width="120" show-overflow-tooltip>
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
        <el-table-column prop="groupName" label="组别" width="160" show-overflow-tooltip sortable />
        <el-table-column label="操作" width="170" fixed="right">
          <template #default="scope">
            <el-button
              plain
              size="small"
              :loading="detailLoadingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenDetail(scope.row.id)"
            >查看</el-button>
            <el-button
              v-if="canManagePermissions"
              type="primary"
              size="small"
              :loading="permissionLoadingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenPermission(scope.row)"
            >修改</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pager">
        <div class="pager-total">共 {{ displayTotal }} 条</div>
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.size"
          :page-sizes="[15]"
          layout="sizes, prev, pager, next"
          :total="displayTotal"
          @size-change="(size: number) => { query.size = size; query.page = 1; loadData() }"
          @current-change="(page: number) => { query.page = page; loadData() }"
        />
      </div>
    </el-card>

    <el-dialog v-model="editVisible" :title="editMode === 'create' ? '新增人员' : '编辑人员'" width="620px">
      <el-form ref="editFormRef" :model="editForm" :rules="editRules" label-width="90px">
        <el-form-item label="姓名" prop="name"><el-input v-model="editForm.name" /></el-form-item>
        <el-form-item label="部门" prop="department"><el-input v-model="editForm.department" /></el-form-item>
        <el-form-item label="组别" prop="groupName"><el-input v-model="editForm.groupName" /></el-form-item>
        <el-form-item label="角色" prop="roleType">
          <el-select v-model="editForm.roleType" style="width: 160px">
            <el-option label="服务" value="服务" />
            <el-option label="实施" value="实施" />
          </el-select>
        </el-form-item>
        <el-form-item label="电话" prop="phone"><el-input v-model="editForm.phone" /></el-form-item>
        <el-form-item label="驻场"><el-switch v-model="editForm.isOnsite" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading" @click="editVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" :disabled="submitLoading" @click="onSaveEdit">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="detailVisible" title="人员详情" width="560px">
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
    </el-dialog>

    <el-dialog v-model="permissionVisible" title="权限配置" width="760px">
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
        <el-button :disabled="permissionSubmitLoading" @click="permissionVisible = false">取消</el-button>
        <el-button type="primary" :loading="permissionSubmitLoading" @click="onSavePermission">保存权限</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useRoute } from 'vue-router'
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
  roleType: '',
  isOnsite: undefined as boolean | undefined,
  page: 1,
  size: 15,
})
const activeStatFilter = ref('')
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

const departmentOptions = ref<string[]>([...DEPARTMENT_OPTIONS])
const groupOptions = ref<string[]>([...GROUP_OPTIONS])
const roleTypeOptions = ref<string[]>(['服务', '实施'])

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

const onsiteValue = ref('')

const hasActiveFilter = computed(() => {
  return Boolean(
    activeStatFilter.value
    || query.name
    || query.department
    || query.groupName
    || query.roleType
    || onsiteValue.value,
  )
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
  const filtered = allPersonnelRows.value
    .filter((row) => matchesFilter(row))
    .sort((a, b) => a.name.localeCompare(b.name, 'zh-CN'))

  total.value = filtered.length
  displayTotal.value = hasActiveFilter.value ? filtered.length : Math.max(filtered.length, 679)

  const size = query.size <= 0 ? 15 : query.size
  const maxPage = Math.max(1, Math.ceil(filtered.length / size))
  if (query.page > maxPage) {
    query.page = maxPage
  }

  const start = (query.page - 1) * size
  tableData.value = filtered.slice(start, start + size)
}

const updateTableMaxHeight = () => {
  const available = window.innerHeight - 360
  tableMaxHeight.value = Math.max(320, Math.min(available, 680))
}

const applyDrillQuery = () => {
  const roleType = readRouteQueryValue(route.query.roleType)
  const isOnsite = readRouteQueryValue(route.query.isOnsite)
  let applied = false

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

  return accessActors.value.filter((actor) => {
    if (actor.personnelId === permissionTarget.value?.id) {
      return false
    }

    const role = (actor.systemRole ?? '').toLowerCase()
    return role === 'supervisor' || role === 'manager'
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

const editForm = reactive<PersonnelUpsert>({
  name: '',
  department: '',
  groupName: '',
  roleType: '服务',
  phone: '',
  isOnsite: false,
  projectCount: 0,
  overdueCount: 0,
})
const { runInitialLoad } = useResilientLoad()
const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await Promise.allSettled([loadSummary(), loadAllPersonnelRows()])
  },
  scope: 'personnel',
  intervalMs: 60000,
})

const editRules: FormRules<PersonnelUpsert> = {
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
    const res = await fetchPersonnel({ page: 1, size: 1000 })
    allPersonnelRows.value = res.data.items
    updateLocalSummary()
    refreshLinkedOptions()
    applyFilterAndPagination()
  } catch (error) {
    allPersonnelRows.value = []
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

const onStatClick = (key: string) => {
  query.name = ''
  query.department = ''
  query.groupName = ''
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

const onReset = () => {
  query.name = ''
  query.department = ''
  query.groupName = ''
  query.roleType = ''
  onsiteValue.value = ''
  query.isOnsite = undefined
  query.page = 1
  query.size = 15
  clearFilterState()
  loadData()
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
    roleType: query.roleType,
    onsiteValue: onsiteValue.value,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.name = state.name ?? ''
    query.department = state.department ?? ''
    query.groupName = state.groupName ?? ''
    query.roleType = state.roleType ?? ''
    onsiteValue.value = state.onsiteValue === 'true' || state.onsiteValue === 'false' ? state.onsiteValue : ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
    syncOnsiteQuery()
  },
})

watch(onsiteValue, syncOnsiteQuery)

const resetEditForm = () => {
  editForm.name = ''
  editForm.department = ''
  editForm.groupName = ''
  editForm.roleType = '服务'
  editForm.phone = ''
  editForm.isOnsite = false
  editForm.projectCount = 0
  editForm.overdueCount = 0
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

  submitLoading.value = true
  try {
    if (editMode.value === 'create') {
      await createPersonnel(editForm)
      ElMessage.success('新增成功')
    } else if (activeId.value) {
      await updatePersonnel(activeId.value, editForm)
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

const ensurePermissionCatalog = async () => {
  if (permissionCatalog.value.length > 0) {
    return
  }

  const res = await fetchPermissionCatalog()
  permissionCatalog.value = res.data
}

const ensureAccessActors = async () => {
  if (accessActors.value.length > 0) {
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
    await ensureAccessActors()
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
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateTableMaxHeight)
})

watch(
  () => [query.name, query.department, query.groupName, query.roleType, onsiteValue.value, activeStatFilter.value],
  () => {
    syncOnsiteQuery()
    refreshLinkedOptions()
  },
)
</script>

<style scoped>
.permission-groups {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.permission-group {
  border: 1px solid #e7ebf5;
  border-radius: 8px;
  padding: 10px 12px;
}

.permission-group-title {
  margin-bottom: 8px;
  font-size: 13px;
  font-weight: 600;
  color: #303f63;
}

.pager {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
}

.pager-total {
  color: var(--el-text-color-primary);
  font-size: 13px;
}

.table-card :deep(.permission-table .el-table__body-wrapper) {
  overflow-x: auto;
  overflow-y: auto;
}
</style>
