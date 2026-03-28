<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">数据维护中心</h2>
        <div class="page-subtitle">导入、清洗、归属审计与手工调整</div>
      </div>
    </div>

    <AppFilterCard>
      <el-space wrap>
        <el-button v-if="canManageMaintenance" type="primary" :loading="loading.importing" @click="onAutoImport">自动重导入</el-button>
        <el-button v-if="canManageMaintenance" type="warning" :loading="loading.cleaning" @click="onCleanup">执行清洗</el-button>
        <el-button :loading="loading.audit" @click="loadAudit">刷新审计列表</el-button>
      </el-space>
      <el-space wrap style="margin-top: 8px;">
        <el-button size="small" @click="onDownloadTemplate('project-ledger')">下载项目台账模板</el-button>
        <el-button size="small" @click="onDownloadTemplate('major-demand')">下载重大需求模板</el-button>
        <el-button size="small" @click="onDownloadTemplate('workhours')">下载工时报表模板</el-button>
      </el-space>
      <div v-if="lastResult" style="margin-top: 12px; color: var(--el-text-color-secondary)">
        {{ lastResult }}
      </div>
    </AppFilterCard>

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
            <el-button type="primary" :loading="loading.uploadProject" :disabled="!projectFile" @click="onUploadProject">导入</el-button>
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
            <el-button type="primary" :loading="loading.uploadDemand" :disabled="!demandFile" @click="onUploadDemand">导入</el-button>
          </div>
          <div v-if="uploadDemandResult" style="margin-top: 8px; color: var(--el-color-success); font-size: 13px;">
            {{ uploadDemandResult }}
          </div>
        </el-card>
      </el-col>
    </el-row>

    <AppFilterCard>
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
    </AppFilterCard>

    <AppTableCard>
      <el-table :data="auditRows" v-loading="loading.audit" stripe max-height="520" scrollbar-always-on empty-text="暂无可审计数据">
        <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip sortable />
        <el-table-column prop="productName" label="产品" min-width="180" show-overflow-tooltip sortable />
        <el-table-column prop="groupName" label="归属人/组别" width="130" show-overflow-tooltip sortable />
        <el-table-column prop="hospitalLevel" label="级别" width="90" sortable />
        <el-table-column prop="province" label="省份" width="100" sortable />
        <el-table-column prop="amount" label="金额" width="110" align="right" sortable />
      </el-table>

      <div class="pager" style="margin-top: 12px; color: var(--el-text-color-secondary)">
        共 {{ auditRows.length }} 条医院+产品归属记录
      </div>
    </AppTableCard>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Upload } from '@element-plus/icons-vue'
import { fetchOwnershipAudit, reassignOwnership, runAutoImport, runCleanup, uploadProjectLedger, uploadMajorDemand, downloadImportTemplate, validateProjectLedger } from '../../api/modules/maintenance'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import type { UploadFile } from 'element-plus'
import AppFilterCard from '../../components/AppFilterCard.vue'
import AppTableCard from '../../components/AppTableCard.vue'

const auditRows = ref<Array<{ hospitalName: string; productName: string; groupName: string; hospitalLevel: string; province: string; amount: number }>>([])
const lastResult = ref('')
const loading = reactive({
  importing: false,
  cleaning: false,
  audit: false,
  reassign: false,
  uploadProject: false,
  uploadDemand: false,
  validateProject: false,
})

// Upload state
const projectFile = ref<File | null>(null)
const projectFileList = ref<UploadFile[]>([])
const projectSheetName = ref('')
const uploadProjectResult = ref('')
const validateProjectResult = ref<{ valid: boolean; totalRows: number; errors: string[]; warnings: string[] } | null>(null)

const demandFile = ref<File | null>(null)
const demandFileList = ref<UploadFile[]>([])
const demandSheetName = ref('')
const uploadDemandResult = ref('')

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
    const nameMap = { 'project-ledger': '维护项目明细_导入模板', 'major-demand': '重大需求明细_导入模板', 'workhours': '工时报表_导入模板' }
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

const form = reactive({
  hospitalName: '',
  productName: '',
  groupName: '',
})

const access = useAccessControl()
const canManageMaintenance = computed(() => access.canPermission('maintenance.manage'))

const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await loadAudit()
  },
  scope: 'maintenance',
  intervalMs: 60000,
})

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

onMounted(async () => {
  await access.ensureAccessProfileLoaded()
  await loadAudit()
})
</script>

<style scoped>
</style>
