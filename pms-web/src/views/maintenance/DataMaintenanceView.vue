<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">数据维护中心</h2>
        <div class="page-subtitle">导入、清洗、归属审计与手工调整</div>
      </div>
    </div>

    <el-alert
      v-if="hasTaskFocus"
      type="warning"
      :closable="false"
      show-icon
      class="task-focus-alert"
    >
      <template #title>
        来自运维任务的定位：{{ taskFocusLabel }}
      </template>
      <template #default>
        审计列表已经按当前任务自动聚焦，可以直接核对归属并发起调整。
        <el-space wrap style="margin-left: 12px;">
          <el-button size="small" @click="applyTaskFocus">重新定位</el-button>
          <el-button size="small" text @click="clearTaskFocus">清除定位</el-button>
        </el-space>
      </template>
    </el-alert>

    <el-card shadow="never" style="margin-bottom: 16px;">
      <el-space wrap>
        <el-button v-if="canManageMaintenance" type="primary" :loading="loading.importing" @click="onAutoImport" icon="Upload">自动重导入</el-button>
        <el-button v-if="canManageMaintenance" type="warning" :loading="loading.cleaning" @click="onCleanup">执行清洗</el-button>
        <el-button :loading="loading.audit" @click="loadAudit" icon="Refresh">刷新审计列表</el-button>
      </el-space>
      <el-space wrap style="margin-top: 8px;">
        <el-button size="small" @click="onDownloadTemplate('project-ledger')" icon="Download">下载项目台账模板</el-button>
        <el-button size="small" @click="onDownloadTemplate('major-demand')" icon="Download">下载重大需求模板</el-button>
        <el-button size="small" @click="onDownloadTemplate('workhours')" icon="Download">下载工时报表模板</el-button>
      </el-space>
      <div v-if="lastResult" style="margin-top: 12px; color: var(--el-text-color-secondary)">
        {{ lastResult }}
      </div>
    </el-card>

    <el-row v-if="canManageMaintenance" :gutter="16" style="margin-bottom: 16px;">
      <el-col :xs="24" :sm="12">
        <el-card shadow="never">
          <template #header>
            <div style="display: flex; align-items: center; justify-content: space-between;">
              <span>上传项目台账 Excel</span>
              <el-tag type="info" size="small">支持 .xlsx / .xls</el-tag>
            </div>
          </template>
          <el-upload
            drag
            :auto-upload="false"
            :limit="1"
            accept=".xlsx,.xls,.xlsm"
            :on-change="onProjectFileChange"
            :on-remove="() => projectFile = null"
            :file-list="projectFileList"
          >
            <el-icon style="font-size: 40px; color: var(--el-text-color-placeholder);"><Upload /></el-icon>
            <div style="margin-top: 8px;">将 Excel 文件拖到此处，或 <em>点击选择</em></div>
          </el-upload>
          <div style="margin-top: 12px; display: flex; align-items: center; gap: 8px;">
            <el-input v-model="projectSheetName" placeholder="工作表名（默认：维护项目明细）" clearable style="flex: 1;" />
            <el-button size="small" :loading="loading.validateProject" :disabled="!projectFile" @click="onValidateProject">验证</el-button>
            <el-button type="primary" :loading="loading.uploadProject" :disabled="!projectFile" @click="onUploadProject" icon="Upload">全量导入</el-button>
            <el-button type="success" :loading="loading.syncProject" :disabled="!projectFile" @click="onSyncProject" icon="RefreshRight">增量同步</el-button>
          </div>
          <div style="margin-top: 8px;">
            <el-input v-model="syncGroupName" placeholder="新增项目组别（默认：舒高成）" clearable />
          </div>
          <div v-if="validateProjectResult" style="margin-top: 8px; font-size: 13px;">
            <div v-if="validateProjectResult.errors.length" style="color: var(--el-color-danger)">
              <div v-for="e in validateProjectResult.errors" :key="e">❌ {{ e }}</div>
            </div>
            <div v-if="validateProjectResult.warnings.length" style="color: var(--el-color-warning)">
              <div v-for="w in validateProjectResult.warnings" :key="w">⚠️ {{ w }}</div>
            </div>
            <div v-if="validateProjectResult.valid" style="color: var(--el-color-success)">
              ✅ 验证通过，共 {{ validateProjectResult.totalRows }} 行数据
            </div>
          </div>
          <div v-if="uploadProjectResult" style="margin-top: 8px; color: var(--el-color-success); font-size: 13px;">
            {{ uploadProjectResult }}
          </div>
          <div v-if="syncProjectResult" style="margin-top: 8px; color: var(--el-color-success); font-size: 13px; line-height: 1.6;">
            {{ syncProjectResult }}
          </div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12">
        <el-card shadow="never">
          <template #header>
            <div style="display: flex; align-items: center; justify-content: space-between;">
              <span>上传重大需求 Excel</span>
              <el-tag type="info" size="small">支持 .xlsx / .xls</el-tag>
            </div>
          </template>
          <el-upload
            drag
            :auto-upload="false"
            :limit="1"
            accept=".xlsx,.xls,.xlsm"
            :on-change="onDemandFileChange"
            :on-remove="() => demandFile = null"
            :file-list="demandFileList"
          >
            <el-icon style="font-size: 40px; color: var(--el-text-color-placeholder);"><Upload /></el-icon>
            <div style="margin-top: 8px;">将 Excel 文件拖到此处，或 <em>点击选择</em></div>
          </el-upload>
          <div style="margin-top: 12px; display: flex; align-items: center; gap: 8px;">
            <el-input v-model="demandSheetName" placeholder="工作表名（默认：重大需求明细）" clearable style="flex: 1;" />
            <el-button type="primary" :loading="loading.uploadDemand" :disabled="!demandFile" @click="onUploadDemand" icon="Upload">导入</el-button>
          </div>
          <div v-if="uploadDemandResult" style="margin-top: 8px; color: var(--el-color-success); font-size: 13px;">
            {{ uploadDemandResult }}
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" style="margin-bottom: 16px;">
      <el-form :model="form" inline class="filter-form" @submit.prevent="onReassign">
        <el-form-item label="医院">
          <el-input v-model="form.hospitalName" clearable style="width: 220px" @keyup.enter="onReassign" />
        </el-form-item>
        <el-form-item label="产品">
          <el-input v-model="form.productName" clearable style="width: 220px" @keyup.enter="onReassign" />
        </el-form-item>
        <el-form-item label="归属人/组别">
          <el-input v-model="form.groupName" clearable style="width: 180px" @keyup.enter="onReassign" />
        </el-form-item>
        <el-form-item v-if="canManageMaintenance">
          <el-button type="primary" :loading="loading.reassign" @click="onReassign">调整归属</el-button>
        </el-form-item>
      </el-form>
      <div v-if="hasTaskFocus" class="focus-hint">
        已按任务定位预填医院和产品，补充归属人/组别后即可完成调整。
      </div>
    </el-card>

    <el-card shadow="never" class="table-card">
      <div class="audit-toolbar">
        <el-form :model="auditFilters" inline class="filter-form" @submit.prevent>
          <el-form-item label="医院筛选">
            <el-input v-model="auditFilters.hospitalName" clearable style="width: 220px" placeholder="按医院关键字过滤" />
          </el-form-item>
          <el-form-item label="产品筛选">
            <el-input v-model="auditFilters.productName" clearable style="width: 220px" placeholder="按产品关键字过滤" />
          </el-form-item>
          <el-form-item label="归属筛选">
            <el-input v-model="auditFilters.groupName" clearable style="width: 180px" placeholder="按归属人/组别过滤" />
          </el-form-item>
          <el-form-item>
            <el-button @click="resetAuditFilters">重置筛选</el-button>
          </el-form-item>
        </el-form>
        <div class="audit-toolbar-meta">
          <span v-if="hasActiveAuditFilters">当前显示 {{ filteredAuditRows.length }} / {{ auditRows.length }} 条</span>
          <span v-else>共 {{ auditRows.length }} 条医院+产品归属记录</span>
        </div>
      </div>

      <el-table
        :data="filteredAuditRows"
        v-loading="loading.audit"
        stripe
        max-height="520"
        scrollbar-always-on
        empty-text="暂无可审计数据"
        @row-click="onAuditRowClick"
      >
        <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip sortable />
        <el-table-column prop="productName" label="产品" min-width="180" show-overflow-tooltip sortable />
        <el-table-column prop="groupName" label="归属人/组别" width="130" show-overflow-tooltip sortable />
        <el-table-column prop="hospitalLevel" label="级别" width="90" sortable />
        <el-table-column prop="province" label="省份" width="100" sortable />
        <el-table-column prop="amount" label="金额" width="110" align="right" sortable />
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button text type="primary" @click.stop="applyAuditRow(row)">填入调整</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pager" style="margin-top: 12px; color: var(--el-text-color-secondary)">
        <span v-if="hasActiveAuditFilters">已聚焦 {{ filteredAuditRows.length }} 条记录，点击行可快速填入归属调整。</span>
        <span v-else>点击任一行可将医院、产品、当前归属带入上方调整表单。</span>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Upload } from '@element-plus/icons-vue'
import { fetchOwnershipAudit, reassignOwnership, runAutoImport, runCleanup, uploadProjectLedger, syncProjectLedger, uploadMajorDemand, downloadImportTemplate, validateProjectLedger } from '../../api/modules/maintenance'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import type { UploadFile } from 'element-plus'

type OwnershipAuditRow = {
  hospitalName: string
  productName: string
  groupName: string
  hospitalLevel: string
  province: string
  amount: number
}

const route = useRoute()
const router = useRouter()

const auditRows = ref<OwnershipAuditRow[]>([])
const lastResult = ref('')
const loading = reactive({
  importing: false,
  cleaning: false,
  audit: false,
  reassign: false,
  uploadProject: false,
  syncProject: false,
  uploadDemand: false,
  validateProject: false,
})

const projectFile = ref<File | null>(null)
const projectFileList = ref<UploadFile[]>([])
const projectSheetName = ref('')
const uploadProjectResult = ref('')
const syncGroupName = ref('舒高成')
const syncProjectResult = ref('')
const validateProjectResult = ref<{ valid: boolean; totalRows: number; errors: string[]; warnings: string[] } | null>(null)

const demandFile = ref<File | null>(null)
const demandFileList = ref<UploadFile[]>([])
const demandSheetName = ref('')
const uploadDemandResult = ref('')

const form = reactive({
  hospitalName: '',
  productName: '',
  groupName: '',
})

const auditFilters = reactive({
  hospitalName: '',
  productName: '',
  groupName: '',
})

const readQueryText = (value: unknown) => {
  if (Array.isArray(value)) {
    return (value[0] ?? '').trim()
  }

  return typeof value === 'string'
    ? value.trim()
    : ''
}

const taskFocus = computed(() => ({
  hospitalName: readQueryText(route.query.hospitalName),
  productName: readQueryText(route.query.productName),
}))

const hasTaskFocus = computed(() => Boolean(taskFocus.value.hospitalName || taskFocus.value.productName))

const taskFocusLabel = computed(() => {
  const parts = [taskFocus.value.hospitalName, taskFocus.value.productName].filter(Boolean)
  return parts.join(' / ')
})

const normalizeKeyword = (value: string) => value.trim().toLowerCase()

const filteredAuditRows = computed(() => {
  const hospitalKeyword = normalizeKeyword(auditFilters.hospitalName)
  const productKeyword = normalizeKeyword(auditFilters.productName)
  const groupKeyword = normalizeKeyword(auditFilters.groupName)

  return auditRows.value.filter((row) => {
    if (hospitalKeyword && !row.hospitalName.toLowerCase().includes(hospitalKeyword)) {
      return false
    }

    if (productKeyword && !row.productName.toLowerCase().includes(productKeyword)) {
      return false
    }

    if (groupKeyword && !row.groupName.toLowerCase().includes(groupKeyword)) {
      return false
    }

    return true
  })
})

const hasActiveAuditFilters = computed(() => Boolean(
  auditFilters.hospitalName.trim()
  || auditFilters.productName.trim()
  || auditFilters.groupName.trim(),
))

const access = useAccessControl()
const canManageMaintenance = computed(() => access.canPermission('maintenance.manage'))

const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await loadAudit()
  },
  scope: 'maintenance',
  intervalMs: 60000,
})

const applyTaskFocus = () => {
  auditFilters.hospitalName = taskFocus.value.hospitalName
  auditFilters.productName = taskFocus.value.productName

  if (taskFocus.value.hospitalName) {
    form.hospitalName = taskFocus.value.hospitalName
  }

  if (taskFocus.value.productName) {
    form.productName = taskFocus.value.productName
  }
}

const resetAuditFilters = () => {
  auditFilters.hospitalName = ''
  auditFilters.productName = ''
  auditFilters.groupName = ''
}

const clearTaskFocus = async () => {
  resetAuditFilters()
  await router.replace({ path: route.path })
}

const applyAuditRow = (row: OwnershipAuditRow) => {
  form.hospitalName = row.hospitalName
  form.productName = row.productName
  form.groupName = row.groupName
  auditFilters.hospitalName = row.hospitalName
  auditFilters.productName = row.productName
  ElMessage.success('已将当前记录带入归属调整表单')
}

const onAuditRowClick = (row: OwnershipAuditRow) => {
  applyAuditRow(row)
}

const onProjectFileChange = (uploadFile: UploadFile) => {
  projectFile.value = uploadFile.raw ?? null
}

const onDemandFileChange = (uploadFile: UploadFile) => {
  demandFile.value = uploadFile.raw ?? null
}

const onUploadProject = async () => {
  if (!projectFile.value) return
  loading.uploadProject = true
  uploadProjectResult.value = ''
  syncProjectResult.value = ''
  try {
    const res = await uploadProjectLedger(projectFile.value, projectSheetName.value || undefined)
    uploadProjectResult.value = `导入成功：${res.data.importedRowCount} 行 — ${res.data.message}`
    ElMessage.success('项目台账导入成功')
    projectFile.value = null
    projectFileList.value = []
    await loadAudit()
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '项目台账上传导入失败'))
  } finally {
    loading.uploadProject = false
  }
}

const onSyncProject = async () => {
  if (!projectFile.value) return
  loading.syncProject = true
  uploadProjectResult.value = ''
  syncProjectResult.value = ''
  try {
    const res = await syncProjectLedger(
      projectFile.value,
      projectSheetName.value || undefined,
      syncGroupName.value || undefined,
    )
    syncProjectResult.value = [
      `同步成功：匹配 ${res.data.matchedProjectCount}，更新 ${res.data.updatedProjectCount}，新增 ${res.data.addedProjectCount}，保留未匹配 ${res.data.preservedUnmatchedProjectCount}。`,
      `${res.data.message}`,
    ].join(' ')
    ElMessage.success('项目台账增量同步成功')
    projectFile.value = null
    projectFileList.value = []
    await loadAudit()
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '项目台账增量同步失败'))
  } finally {
    loading.syncProject = false
  }
}

const onUploadDemand = async () => {
  if (!demandFile.value) return
  loading.uploadDemand = true
  uploadDemandResult.value = ''
  try {
    const res = await uploadMajorDemand(demandFile.value, demandSheetName.value || undefined)
    uploadDemandResult.value = `导入成功：${res.data.importedRowCount} 行（${res.data.importedColumnCount} 列） — ${res.data.message}`
    ElMessage.success('重大需求导入成功')
    demandFile.value = null
    demandFileList.value = []
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '重大需求上传导入失败'))
  } finally {
    loading.uploadDemand = false
  }
}

const onDownloadTemplate = async (type: 'project-ledger' | 'major-demand' | 'workhours') => {
  try {
    const blob = await downloadImportTemplate(type)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    const nameMap = { 'project-ledger': '维护项目明细_导入模板', 'major-demand': '重大需求明细_导入模板', workhours: '工时报表_导入模板' }
    a.download = `${nameMap[type]}.xlsx`
    a.click()
    URL.revokeObjectURL(url)
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '下载模板失败'))
  }
}

const onValidateProject = async () => {
  if (!projectFile.value) return
  loading.validateProject = true
  validateProjectResult.value = null
  try {
    const res = await validateProjectLedger(projectFile.value, projectSheetName.value || undefined)
    validateProjectResult.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '验证失败'))
  } finally {
    loading.validateProject = false
  }
}

const loadAudit = async () => {
  loading.audit = true
  try {
    const res = await fetchOwnershipAudit()
    auditRows.value = res.data.items
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载审计数据失败，请稍后重试'))
  } finally {
    loading.audit = false
  }
}

const onAutoImport = async () => {
  if (!canManageMaintenance.value) {
    ElMessage.warning('当前账号无数据重导入权限')
    return
  }

  try {
    await ElMessageBox.confirm(
      '该操作将触发自动重导入并覆盖当前数据结果，确认继续吗？',
      '高风险操作确认',
      {
        type: 'warning',
        confirmButtonText: '确认执行',
        cancelButtonText: '取消',
      },
    )
  } catch {
    return
  }

  loading.importing = true
  try {
    const res = await runAutoImport()
    lastResult.value = `重导入完成：导入${res.data.importedRowCount}，保留${res.data.keptProjectCount}`
    ElMessage.success('自动重导入成功')
    await loadAudit()
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '自动重导入失败，请稍后重试'))
  } finally {
    loading.importing = false
  }
}

const onCleanup = async () => {
  if (!canManageMaintenance.value) {
    ElMessage.warning('当前账号无数据清洗权限')
    return
  }

  try {
    await ElMessageBox.confirm(
      '该操作会对当前数据进行清洗并可能移除无效记录，确认执行吗？',
      '高风险操作确认',
      {
        type: 'warning',
        confirmButtonText: '确认清洗',
        cancelButtonText: '取消',
      },
    )
  } catch {
    return
  }

  loading.cleaning = true
  try {
    const res = await runCleanup()
    lastResult.value = `清洗完成：清洗前${res.data.beforeCount}，清洗后${res.data.afterCount}`
    ElMessage.success('清洗完成')
    await loadAudit()
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '执行清洗失败，请稍后重试'))
  } finally {
    loading.cleaning = false
  }
}

const onReassign = async () => {
  if (!canManageMaintenance.value) {
    ElMessage.warning('当前账号无归属调整权限')
    return
  }

  if (!form.hospitalName || !form.productName || !form.groupName) {
    ElMessage.warning('请填写医院、产品和归属人')
    return
  }

  try {
    await ElMessageBox.confirm(
      `确认将“${form.hospitalName}/${form.productName}”归属调整为“${form.groupName}”吗？`,
      '归属调整确认',
      {
        type: 'warning',
        confirmButtonText: '确认调整',
        cancelButtonText: '取消',
      },
    )
  } catch {
    return
  }

  loading.reassign = true
  try {
    await reassignOwnership({
      hospitalName: form.hospitalName,
      productName: form.productName,
      groupName: form.groupName,
    })
    ElMessage.success('归属调整成功')
    await loadAudit()
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '归属调整失败，请稍后重试'))
  } finally {
    loading.reassign = false
  }
}

watch(
  () => [route.query.hospitalName, route.query.productName],
  () => {
    if (!hasTaskFocus.value) {
      return
    }

    applyTaskFocus()
  },
)

onMounted(async () => {
  await access.ensureAccessProfileLoaded()
  if (hasTaskFocus.value) {
    applyTaskFocus()
  }
  await loadAudit()
})
</script>

<style scoped>
.task-focus-alert {
  margin-bottom: 16px;
}

.focus-hint {
  margin-top: 8px;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.audit-toolbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.audit-toolbar-meta {
  color: var(--el-text-color-secondary);
  font-size: 13px;
  padding-top: 6px;
}
</style>
