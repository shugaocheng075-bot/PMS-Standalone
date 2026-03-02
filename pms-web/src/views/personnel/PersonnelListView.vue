<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">权限管理</h2>
        <div class="page-subtitle">人员信息、权限模板与角色权限统一管理</div>
      </div>
      <el-button v-if="canManagePersonnel" type="primary" @click="onOpenCreate">新增人员</el-button>
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
      <el-table :data="tableData" v-loading="loading" stripe empty-text="暂无符合条件的数据">
        <el-table-column prop="name" label="姓名" width="100" show-overflow-tooltip sortable />
        <el-table-column prop="department" label="部门" width="140" show-overflow-tooltip sortable />
        <el-table-column prop="groupName" label="组别" width="140" show-overflow-tooltip sortable />
        <el-table-column prop="roleType" label="角色" width="100" sortable />
        <el-table-column prop="phone" label="联系电话" width="150" show-overflow-tooltip />
        <el-table-column label="驻场" width="90">
          <template #default="scope">
            <el-tag :type="scope.row.isOnsite ? 'warning' : 'info'">{{ scope.row.isOnsite ? '是' : '否' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="projectCount" label="负责项目" width="100" align="right" sortable />
        <el-table-column prop="overdueCount" label="超期项目" width="100" align="right" sortable />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="scope">
            <el-button
              type="primary"
              link
              :loading="detailLoadingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenDetail(scope.row.id)"
            >详情</el-button>
            <el-button
              v-if="canManagePersonnel"
              type="primary"
              link
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenEdit(scope.row)"
            >编辑</el-button>
            <el-button
              v-if="canManagePersonnel"
              type="danger"
              link
              :loading="deletingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onDelete(scope.row)"
            >删除</el-button>
            <el-button
              v-if="canManagePermissions"
              type="primary"
              link
              :loading="permissionLoadingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenPermission(scope.row)"
            >权限配置</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pager">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.size"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          :total="total"
          @size-change="loadData"
          @current-change="loadData"
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
        <el-form-item label="负责项目" prop="projectCount"><el-input-number v-model="editForm.projectCount" :min="0" /></el-form-item>
        <el-form-item label="超期项目" prop="overdueCount"><el-input-number v-model="editForm.overdueCount" :min="0" /></el-form-item>
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
        <el-descriptions-item label="负责项目">{{ detailItem.projectCount }}</el-descriptions-item>
        <el-descriptions-item label="超期项目">{{ detailItem.overdueCount }}</el-descriptions-item>
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
              <el-form-item label="快捷模板">
                <el-space wrap>
                  <el-button size="small" @click="onApplyPermissionTemplate('service')">服务模板</el-button>
                  <el-button size="small" @click="onApplyPermissionTemplate('implementation')">实施模板</el-button>
                  <el-button size="small" type="primary" @click="onApplyPermissionTemplate('admin')">管理员模板</el-button>
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
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useRoute } from 'vue-router'
import {
  createPersonnel,
  deletePersonnel,
  fetchPersonnel,
  fetchPersonnelById,
  fetchPersonnelSummary,
  updatePersonnel,
} from '../../api/modules/personnel'
import {
  fetchPermissionCatalog,
  fetchUserAccessProfile,
  updateUserPermissions,
} from '../../api/modules/access'
import type { PersonnelItem, PersonnelSummary, PersonnelUpsert } from '../../types/personnel'
import type { PermissionDefinition } from '../../types/access'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { DEPARTMENT_OPTIONS, GROUP_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'

const loading = ref(false)
const total = ref(0)
const tableData = ref<PersonnelItem[]>([])
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
  size: 10,
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

const departmentOptions = ref<string[]>([...DEPARTMENT_OPTIONS])
const groupOptions = ref<string[]>([...GROUP_OPTIONS])
const roleTypeOptions = ref<string[]>(['服务', '实施'])

const onsiteValue = ref('')

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
const activeStatFilter = ref('')
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
  permissionKeys: [] as string[],
})

const permissionTemplates = {
  service: [
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
  ],
  implementation: [
    'dashboard.view',
    'alert-center.view',
    'project.view',
    'handover.view',
    'inspection.view',
    'annual-report.view',
    'hospital.view',
    'personnel.view',
    'product.view',
  ],
  admin: [
    'dashboard.view',
    'alert-center.view',
    'project.view',
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
const canManagePermissions = computed(() => access.canPermission('permission.manage'))

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
    await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
  },
  scope: 'personnel',
  intervalMs: 10000,
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

const loadFilterOptions = async () => {
  try {
    const res = await fetchPersonnel({ page: 1, size: 5000 })
    const items = res.data.items

    if (!items.length) {
      return
    }

    const departments = Array.from(new Set(items.map((item) => item.department).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (departments.length > 0) {
      departmentOptions.value = departments
    }

    const groups = Array.from(new Set(items.map((item) => item.groupName).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (groups.length > 0) {
      groupOptions.value = groups
    }

    const roleTypes = Array.from(new Set(items.map((item) => item.roleType).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (roleTypes.length > 0) {
      roleTypeOptions.value = Array.from(new Set([...roleTypeOptions.value, ...roleTypes]))
    }
  } catch {
  }
}

const loadSummary = async () => {
  try {
    const res = await fetchPersonnelSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载人员汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    syncOnsiteQuery()
    const res = await fetchPersonnel(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载人员列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onStatClick = (key: string) => {
  // Clear filter state
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
  query.size = 10
  clearFilterState()
  loadData()
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
    query.size = typeof state.size === 'number' ? state.size : 10
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

const onOpenEdit = (row: PersonnelItem) => {
  if (!canManagePersonnel.value) {
    ElMessage.warning('当前账号无编辑人员权限')
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
    await Promise.all([loadSummary(), loadData()])
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

const onDelete = async (row: PersonnelItem) => {
  if (!canManagePersonnel.value) {
    ElMessage.warning('当前账号无删除人员权限')
    return
  }

  if (submitLoading.value || deletingId.value === row.id) return
  deletingId.value = row.id
  try {
    await ElMessageBox.confirm(`确认删除人员“${row.name}”吗？`, '提示', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })

    await deletePersonnel(row.id)
    ElMessage.success('删除成功')
    await Promise.all([loadSummary(), loadData()])
    notifyDataChanged('global')
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      ElMessage.info('已取消删除')
    } else {
      ElMessage.error(getErrorMessage(error, '删除人员失败，请稍后重试'))
    }
  } finally {
    deletingId.value = null
  }
}

const ensurePermissionCatalog = async () => {
  if (permissionCatalog.value.length > 0) {
    return
  }

  const res = await fetchPermissionCatalog()
  permissionCatalog.value = res.data
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
    const res = await fetchUserAccessProfile(row.id)
    permissionForm.isAdmin = res.data.isAdmin
    permissionForm.permissionKeys = [...res.data.permissions]
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

  if (template === 'admin') {
    permissionForm.isAdmin = true
    permissionForm.permissionKeys = permissionTemplates.admin.filter((key) => validPermissionKeys.has(key))
    ElMessage.success('已应用管理员模板')
    return
  }

  permissionForm.isAdmin = false
  permissionForm.permissionKeys = permissionTemplates[template].filter((key) => validPermissionKeys.has(key))
  ElMessage.success(template === 'service' ? '已应用服务模板' : '已应用实施模板')
}

onMounted(async () => {
  await access.ensureAccessProfileLoaded()
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
})
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
</style>
