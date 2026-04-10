<template>
  <div ref="pageRef" class="page-shell report-page">
    <ProTable
      title="明细数据"
      :data="rows"
      :loading="loading"
      stripe
      row-key="id"
      empty-text="暂无符合条件的数据"
      @row-dblclick="onRowDblClick"
    >
      <template #toolbar>
        <el-date-picker v-model="selectedMonth" type="month" placeholder="选择月份" value-format="YYYY-MM" style="width: 160px" @change="onMonthChange" />
        <el-button :loading="loading" @click="loadReport">刷新</el-button>
        <el-button type="warning" :loading="regenerating" @click="onRegenerate">重新计算工时</el-button>
        <el-button type="success" @click="triggerImport" :loading="importing">导入 Excel</el-button>
        <el-button type="primary" @click="onExportExcel" :disabled="rows.length === 0">导出 Excel</el-button>
        <el-button @click="onPrint" :disabled="rows.length === 0">打印</el-button>
        <el-button @click="onAddRow">新增行</el-button>
        <input ref="fileInputRef" type="file" accept=".xlsx,.xls" style="display:none" @change="onFileSelected" />
      </template>
        <el-table-column prop="opportunityNumber" label="机会号" width="140" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.opportunityNumber" size="small" />
            <span v-else>{{ row.opportunityNumber }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="hospitalName" label="客户名称" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.hospitalName" size="small" />
            <span v-else>{{ row.hospitalName }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="productName" label="产品名称" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.productName" size="small" />
            <span v-else>{{ row.productName }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="implementationStatus" label="实施状态" width="110">
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.implementationStatus" size="small" />
            <span v-else>{{ row.implementationStatus }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="workHoursManDays" label="工时（人天）" width="110" align="right">
          <template #default="{ row }">
            <el-input-number v-if="editingId === row.id" v-model="editForm.workHoursManDays" :min="0" :precision="0" size="small" controls-position="right" style="width:90px" />
            <span v-else>{{ row.workHoursManDays }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="personnelCount" label="实施人员（个数）" width="130" align="right">
          <template #default="{ row }">
            <el-input-number v-if="editingId === row.id" v-model="editForm.personnelCount" :min="0" size="small" controls-position="right" style="width:80px" />
            <span v-else>{{ row.personnelCount }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="personnel1" label="人员1" width="100" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.personnel1" size="small" />
            <span v-else>{{ row.personnel1 }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="personnel2" label="人员2" width="100" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.personnel2" size="small" />
            <span v-else>{{ row.personnel2 }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="personnel3" label="人员3" width="100" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.personnel3" size="small" />
            <span v-else>{{ row.personnel3 }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="personnel4" label="人员4" width="100" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.personnel4" size="small" />
            <span v-else>{{ row.personnel4 }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="personnel5" label="人员5" width="100" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.personnel5" size="small" />
            <span v-else>{{ row.personnel5 }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="maintenanceStartDate" label="维护开始时间" width="130">
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.maintenanceStartDate" size="small" placeholder="yyyy-MM-dd" />
            <span v-else>{{ row.maintenanceStartDate }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="maintenanceEndDate" label="维护结束时间" width="130">
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.maintenanceEndDate" size="small" placeholder="yyyy-MM-dd" />
            <span v-else>{{ row.maintenanceEndDate }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="afterSalesProjectType" label="售后项目类型" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.afterSalesProjectType" size="small" />
            <span v-else>{{ row.afterSalesProjectType }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="remarks" label="备注" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input v-if="editingId === row.id" v-model="editForm.remarks" size="small" />
            <span v-else>{{ row.remarks }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <template v-if="editingId === row.id">
              <el-button type="primary" link size="small" :loading="saving" @click.stop="onSaveRow">保存</el-button>
              <el-button link size="small" @click.stop="onCancelEdit">取消</el-button>
            </template>
            <template v-else>
              <el-button type="primary" link size="small" @click.stop="onEditRow(row)">编辑</el-button>
              <el-button type="danger" link size="small" @click.stop="onDeleteRow(row)">删除</el-button>
            </template>
          </template>
        </el-table-column>
      
      <div class="table-footer">
        <span>共 {{ rows.length }} 条记录</span>
        <span v-if="totalManDays > 0" style="margin-left:16px">总工时：{{ totalManDays }} 人天</span>
      </div>
    </ProTable>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { usePrint } from '../../composables/usePrint'
import {
  fetchWorkHoursReport,
  exportWorkHoursReport,
  updateWorkHoursReportRow,
  deleteWorkHoursReportRow,
  addWorkHoursReportRow,
  importWorkHoursReport,
  regenerateWorkHoursReport,
} from '../../api/modules/report'
import type { WorkHoursReportRow, WorkHoursReportRowUpdatePayload } from '../../types/report'
import { getErrorMessage } from '../../utils/error'
import ProTable from '../../components/ProTable.vue'

const { printArea } = usePrint()
const pageRef = ref<HTMLElement | null>(null)
const loading = ref(false)
const saving = ref(false)
const importing = ref(false)
const regenerating = ref(false)
const rows = ref<WorkHoursReportRow[]>([])
const editingId = ref<number | null>(null)
const fileInputRef = ref<HTMLInputElement | null>(null)

// Default to current month
const now = new Date()
const selectedMonth = ref(`${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`)

const totalManDays = computed(() => rows.value.reduce((sum, r) => sum + (r.workHoursManDays || 0), 0))

const editForm = reactive<WorkHoursReportRowUpdatePayload>({
  opportunityNumber: '',
  hospitalName: '',
  productName: '',
  implementationStatus: '',
  workHoursManDays: 0,
  personnelCount: 0,
  personnel1: '',
  personnel2: '',
  personnel3: '',
  personnel4: '',
  personnel5: '',
  maintenanceStartDate: '',
  maintenanceEndDate: '',
  afterSalesProjectType: '',
  remarks: '',
})

const resetEditForm = () => {
  Object.assign(editForm, {
    opportunityNumber: '',
    hospitalName: '',
    productName: '',
    implementationStatus: '',
    workHoursManDays: 0,
    personnelCount: 0,
    personnel1: '',
    personnel2: '',
    personnel3: '',
    personnel4: '',
    personnel5: '',
    maintenanceStartDate: '',
    maintenanceEndDate: '',
    afterSalesProjectType: '',
    remarks: '',
  })
}

const loadReport = async () => {
  loading.value = true
  try {
    const res = await fetchWorkHoursReport({ reportMonth: selectedMonth.value })
    rows.value = res.data?.rows ?? []
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载工时报表失败'))
  } finally {
    loading.value = false
  }
}

const onMonthChange = () => {
  editingId.value = null
  loadReport()
}

// ── Row editing ──

const onRowDblClick = (row: WorkHoursReportRow) => {
  if (editingId.value === row.id) return
  if (editingId.value !== null) {
    editingId.value = null
  }
  onEditRow(row)
}

const onEditRow = (row: WorkHoursReportRow) => {
  editingId.value = row.id
  Object.assign(editForm, {
    opportunityNumber: row.opportunityNumber,
    hospitalName: row.hospitalName,
    productName: row.productName,
    implementationStatus: row.implementationStatus,
    workHoursManDays: row.workHoursManDays,
    personnelCount: row.personnelCount,
    personnel1: row.personnel1,
    personnel2: row.personnel2,
    personnel3: row.personnel3,
    personnel4: row.personnel4,
    personnel5: row.personnel5,
    maintenanceStartDate: row.maintenanceStartDate,
    maintenanceEndDate: row.maintenanceEndDate,
    afterSalesProjectType: row.afterSalesProjectType,
    remarks: row.remarks,
  })
}

const onCancelEdit = () => {
  editingId.value = null
  resetEditForm()
}

const onSaveRow = async () => {
  if (editingId.value === null) return
  saving.value = true
  try {
    await updateWorkHoursReportRow(editingId.value, { ...editForm }, selectedMonth.value)
    ElMessage.success('保存成功')
    editingId.value = null
    await loadReport()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存失败'))
  } finally {
    saving.value = false
  }
}

const onDeleteRow = async (row: WorkHoursReportRow) => {
  try {
    await ElMessageBox.confirm(`确认删除「${row.hospitalName} - ${row.productName}」？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
  } catch {
    return
  }
  try {
    await deleteWorkHoursReportRow(row.id, selectedMonth.value)
    ElMessage.success('删除成功')
    await loadReport()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '删除失败'))
  }
}

const onAddRow = async () => {
  try {
    await addWorkHoursReportRow({
      opportunityNumber: '',
      hospitalName: '',
      productName: '',
      implementationStatus: '',
      workHoursManDays: 0,
      personnelCount: 0,
      personnel1: '',
      personnel2: '',
      personnel3: '',
      personnel4: '',
      personnel5: '',
      maintenanceStartDate: '',
      maintenanceEndDate: '',
      afterSalesProjectType: '',
      remarks: '',
    }, selectedMonth.value)
    ElMessage.success('已新增空行')
    await loadReport()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '新增行失败'))
  }
}

// ── Import ──

const triggerImport = () => {
  fileInputRef.value?.click()
}

const onFileSelected = async (e: Event) => {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  input.value = '' // reset so same file can be re-selected

  try {
    await ElMessageBox.confirm(
      `将清空 ${selectedMonth.value} 月数据并导入「${file.name}」，是否继续？`,
      '导入确认',
      { type: 'warning', confirmButtonText: '确认导入', cancelButtonText: '取消' },
    )
  } catch {
    return
  }

  importing.value = true
  try {
    const res = await importWorkHoursReport(file, selectedMonth.value, true)
    ElMessage.success(`导入成功，共 ${res.data.total} 条记录`)
    await loadReport()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导入失败'))
  } finally {
    importing.value = false
  }
}

// ── Export ──

const onExportExcel = async () => {
  try {
    const blob = await exportWorkHoursReport({ reportMonth: selectedMonth.value })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `工时报表_${selectedMonth.value}.xlsx`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  }
}

// ── Regenerate ──

const onRegenerate = async () => {
  try {
    await ElMessageBox.confirm(
      `将基于当前项目数据重新计算 ${selectedMonth.value} 月工时，已有工时数据将被覆盖，是否继续？`,
      '重新计算确认',
      { type: 'warning', confirmButtonText: '确认', cancelButtonText: '取消' },
    )
  } catch {
    return
  }
  regenerating.value = true
  try {
    const res = await regenerateWorkHoursReport(selectedMonth.value)
    ElMessage.success(`重新计算完成，共 ${res.data.total} 条`)
    await loadReport()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '重新计算失败'))
  } finally {
    regenerating.value = false
  }
}

const onPrint = () => printArea(pageRef.value, '工时报表')

onMounted(() => { loadReport() })
</script>

<style scoped>
.report-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.head-actions {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.table-footer {
  margin-top: 8px;
  color: #6b7280;
  font-size: 13px;
}
</style>


