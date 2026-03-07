<template>
  <div class="page-shell report-page">
    <div class="page-head">
      <div>
        <h2 class="page-title">工时报表</h2>
        <div class="page-subtitle">自动汇总项目台账生成 Excel 格式工时报表</div>
      </div>
      <div class="head-actions">
        <el-button size="small" :loading="loading" @click="loadReport">刷新</el-button>
        <el-button size="small" type="primary" @click="exportCsv" :disabled="rows.length === 0">导出 CSV</el-button>
      </div>
    </div>

    <el-card shadow="never" class="table-card filter-card">
      <el-form :inline="true" size="small">
        <el-form-item label="组别">
          <el-input v-model="query.groupName" placeholder="全部" clearable style="width: 160px" />
        </el-form-item>
        <el-form-item label="实施状态">
          <el-input v-model="query.implementationStatus" placeholder="全部" clearable style="width: 160px" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" size="small" @click="loadReport">查询</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="rows" stripe border max-height="600" scrollbar-always-on empty-text="暂无数据" v-loading="loading">
        <el-table-column prop="opportunityNumber" label="商机编号" width="140" show-overflow-tooltip />
        <el-table-column prop="hospitalName" label="医院名称" min-width="180" show-overflow-tooltip />
        <el-table-column prop="productName" label="产品" min-width="160" show-overflow-tooltip />
        <el-table-column prop="province" label="省份" width="90" />
        <el-table-column prop="groupName" label="组别" width="110" />
        <el-table-column prop="salesName" label="销售" width="110" show-overflow-tooltip />
        <el-table-column prop="maintenancePersonName" label="运维人员" width="110" show-overflow-tooltip />
        <el-table-column prop="implementationStatus" label="实施状态" width="110" />
        <el-table-column prop="afterSalesProjectType" label="售后项目类型" width="120" show-overflow-tooltip />
        <el-table-column prop="workHoursManDays" label="工时人天" width="100" align="right" />
        <el-table-column prop="personnelCount" label="人员数" width="80" align="right" />
        <el-table-column prop="personnel1" label="人员1" width="100" show-overflow-tooltip />
        <el-table-column prop="personnel2" label="人员2" width="100" show-overflow-tooltip />
        <el-table-column prop="personnel3" label="人员3" width="100" show-overflow-tooltip />
        <el-table-column prop="personnel4" label="人员4" width="100" show-overflow-tooltip />
        <el-table-column prop="personnel5" label="人员5" width="100" show-overflow-tooltip />
      </el-table>
      <div class="table-footer">共 {{ rows.length }} 条记录</div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { fetchWorkHoursReport, exportWorkHoursReport } from '../../api/modules/report'
import type { WorkHoursReportRow } from '../../types/report'
import { getErrorMessage } from '../../utils/error'

const loading = ref(false)
const rows = ref<WorkHoursReportRow[]>([])
const query = reactive({ groupName: '', implementationStatus: '' })

const loadReport = async () => {
  loading.value = true
  try {
    const params: Record<string, string> = {}
    if (query.groupName) params.groupName = query.groupName
    if (query.implementationStatus) params.implementationStatus = query.implementationStatus
    const res = await fetchWorkHoursReport(params)
    rows.value = res.data?.rows ?? []
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载工时报表失败'))
  } finally {
    loading.value = false
  }
}

const exportCsv = async () => {
  try {
    const params: Record<string, string> = {}
    if (query.groupName) params.groupName = query.groupName
    if (query.implementationStatus) params.implementationStatus = query.implementationStatus

    const blob = await exportWorkHoursReport(params)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `工时报表_${new Date().toISOString().slice(0, 10)}.csv`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('工时报表导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出工时报表失败'))
  }
}

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
}

.filter-card {
  padding-bottom: 0;
}

.table-footer {
  margin-top: 8px;
  color: #6b7280;
  font-size: 13px;
}
</style>
