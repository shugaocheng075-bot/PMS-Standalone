<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">数据维护中心</h2>
        <div class="page-subtitle">导入、清洗、归属审计与手工调整</div>
      </div>
    </div>

    <el-card shadow="never" class="filter-card">
      <el-space wrap>
        <el-button type="primary" :loading="loading.importing" @click="onAutoImport">自动重导入</el-button>
        <el-button type="warning" :loading="loading.cleaning" @click="onCleanup">执行清洗</el-button>
        <el-button :loading="loading.audit" @click="loadAudit">刷新审计列表</el-button>
      </el-space>
      <div v-if="lastResult" style="margin-top: 12px; color: var(--el-text-color-secondary)">
        {{ lastResult }}
      </div>
    </el-card>

    <el-card shadow="never" class="filter-card">
      <el-form :model="form" inline>
        <el-form-item label="医院">
          <el-input v-model="form.hospitalName" clearable style="width: 220px" />
        </el-form-item>
        <el-form-item label="产品">
          <el-input v-model="form.productName" clearable style="width: 220px" />
        </el-form-item>
        <el-form-item label="归属人/组别">
          <el-input v-model="form.groupName" clearable style="width: 180px" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading.reassign" @click="onReassign">调整归属</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="auditRows" v-loading="loading.audit" stripe>
        <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip />
        <el-table-column prop="productName" label="产品" min-width="180" show-overflow-tooltip />
        <el-table-column prop="groupName" label="归属人/组别" width="130" show-overflow-tooltip />
        <el-table-column prop="hospitalLevel" label="级别" width="90" />
        <el-table-column prop="province" label="省份" width="100" />
        <el-table-column prop="amount" label="金额" width="110" align="right" />
      </el-table>

      <div class="pager" style="margin-top: 12px; color: var(--el-text-color-secondary)">
        共 {{ auditRows.length }} 条医院+产品归属记录
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { fetchOwnershipAudit, reassignOwnership, runAutoImport, runCleanup } from '../../api/modules/maintenance'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

const auditRows = ref<Array<{ hospitalName: string; productName: string; groupName: string; hospitalLevel: string; province: string; amount: number }>>([])
const lastResult = ref('')
const loading = reactive({
  importing: false,
  cleaning: false,
  audit: false,
  reassign: false,
})

const form = reactive({
  hospitalName: '',
  productName: '',
  groupName: '',
})

const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await loadAudit()
  },
  scope: 'maintenance',
  intervalMs: 10000,
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
  if (!form.hospitalName || !form.productName || !form.groupName) {
    ElMessage.warning('请填写医院、产品和归属人')
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

onMounted(loadAudit)
</script>

<style scoped>
</style>
