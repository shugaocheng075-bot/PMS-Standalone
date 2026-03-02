<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">驾驶舱</h2>
        <div class="page-subtitle">平台总览与关键指标入口</div>
      </div>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="6" v-for="card in cards" :key="card.title">
        <el-card shadow="never" class="stat-card stats-card">
          <div class="t">{{ card.title }}</div>
          <div class="v">{{ card.value }}</div>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="table-card">
      <template #header>
        <div class="panel-title">开发起点</div>
      </template>
      <p>项目台账、合同预警、交接管理、巡检计划、年度报告、医院/人员/产品模块已完成联动。</p>
      <p>当前驾驶舱指标来自实时接口汇总，可作为运营总览入口。</p>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { fetchProjectList } from '../../api/modules/project'
import { fetchContractAlertSummary } from '../../api/modules/contract'
import { fetchHandoverSummary } from '../../api/modules/handover'
import { fetchInspectionSummary } from '../../api/modules/inspection'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

const cards = ref([
  { title: '项目总数', value: '0' },
  { title: '超期合同', value: '0' },
  { title: '进行中交接', value: '0' },
  { title: '本月巡检', value: '0' },
])

const loadDashboard = async () => {
  try {
    const [projectRes, contractRes, handoverRes, inspectionRes] = await Promise.all([
      fetchProjectList({ page: 1, size: 1 }),
      fetchContractAlertSummary(),
      fetchHandoverSummary(),
      fetchInspectionSummary(),
    ])

    cards.value = [
      { title: '项目总数', value: String(projectRes.data.total) },
      { title: '超期合同', value: String(contractRes.data.total) },
      { title: '进行中交接', value: String(handoverRes.data.inProgressCount) },
      { title: '本月巡检', value: String(inspectionRes.data.thisMonthCount) },
    ]
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载驾驶舱数据失败，请稍后重试'))
  }
}

useLinkedRealtimeRefresh({
  refresh: loadDashboard,
  scope: 'global',
  intervalMs: 10000,
})
</script>

<style scoped>
.panel-title {
  font-weight: 600;
}
</style>
