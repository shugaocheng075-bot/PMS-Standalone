<template>
  <div class="page-shell workbench-page" v-loading="loading">
    <div class="page-head">
      <div>
        <h2 class="page-title">个人工作台</h2>
        <div class="page-subtitle">待办事项、个人数据统计概览</div>
      </div>
      <el-button size="small" :loading="loading" @click="loadWorkbench" icon="Refresh">刷新数据</el-button>
    </div>

    <div class="metrics-grid metrics-grid--4">
      <button v-for="card in statCards" :key="card.title" type="button" class="metric-card metric-card--action" @click="card.onClick()">
        <div class="metric-card-head">
          <span class="metric-title">{{ card.title }}</span>
          <span class="metric-context">{{ card.context }}</span>
        </div>
        <div class="metric-value">{{ card.value }}</div>
        <div class="metric-note">{{ card.note }}</div>
      </button>
    </div>

    <OperationsTaskPanel
      title="我的运维任务池"
      subtitle="把巡检、报修、交接、重大需求、合同风险和台账问题放进同一屏，先处理最急的。"
      :summary="operationsSummary"
      :tasks="operationsTasks"
      :query="operationsQuery"
      :loading="operationsLoading"
      @refresh="void loadOperationsTasks()"
      @query-change="void applyOperationsTaskQuery($event)"
      @reset-query="void resetOperationsTaskQuery()"
    >
      <template #actions>
        <el-button size="small" :loading="operationsLoading" @click="void loadOperationsTasks()" icon="Refresh">刷新任务</el-button>
      </template>
    </OperationsTaskPanel>

    <div class="workbench-grid">
      <AppTableCard class="todo-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">临期合同</span>
              <el-tag type="warning">{{ data?.expiringContracts.length ?? 0 }}</el-tag>
            </div>
          </template>
          <div v-if="!data?.expiringContracts.length" class="empty-tip">暂无临期合同</div>
          <div
            v-for="item in data?.expiringContracts ?? []"
            :key="item.projectId"
            class="todo-item clickable"
            @click="router.push('/project/list')"
          >
            <div class="todo-main">
              <span class="todo-label">{{ item.hospitalName }}</span>
              <span class="todo-sub">{{ item.productName }}</span>
            </div>
            <el-tag :type="item.daysRemaining <= 7 ? 'danger' : 'warning'" size="small">
              {{ item.daysRemaining }}天后到期
            </el-tag>
          </div>
        </AppTableCard>

      <AppTableCard class="todo-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">待巡检</span>
              <el-tag type="info">{{ data?.pendingInspections.length ?? 0 }}</el-tag>
            </div>
          </template>
          <div v-if="!data?.pendingInspections.length" class="empty-tip">暂无待巡检</div>
          <div
            v-for="item in data?.pendingInspections ?? []"
            :key="item.id"
            class="todo-item clickable"
            @click="router.push('/inspection/plan')"
          >
            <div class="todo-main">
              <span class="todo-label">{{ item.hospitalName }}</span>
              <span class="todo-sub">{{ item.inspector }} · {{ item.planDate }}</span>
            </div>
            <el-tag size="small">{{ item.status }}</el-tag>
          </div>
        </AppTableCard>

      <AppTableCard class="todo-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">未处理报修</span>
              <el-tag type="danger">{{ data?.unresolvedRepairs.length ?? 0 }}</el-tag>
            </div>
          </template>
          <div v-if="!data?.unresolvedRepairs.length" class="empty-tip">暂无未处理报修</div>
          <div
            v-for="item in data?.unresolvedRepairs ?? []"
            :key="item.id"
            class="todo-item clickable"
            @click="router.push('/repair/list')"
          >
            <div class="todo-main">
              <span class="todo-label">{{ item.hospitalName }}</span>
              <span class="todo-sub">{{ item.description }}</span>
            </div>
            <el-tag :type="item.urgency === '非常紧急' ? 'danger' : item.urgency === '紧急' ? 'warning' : 'info'" size="small">
              {{ item.urgency }}
            </el-tag>
          </div>
        </AppTableCard>
    </div>

    <AppTableCard class="quick-actions-card">
      <template #header>
        <div class="panel-head">
          <span class="panel-title">快捷操作</span>
        </div>
      </template>
      <div class="quick-actions">
        <el-button @click="router.push('/workhours/list?action=create')">登记工时</el-button>
        <el-button @click="router.push('/repair/list?action=create')">新建报修</el-button>
        <el-button @click="router.push('/inspection/plan')" icon="View">查看巡检</el-button>
        <el-button @click="router.push('/project/list')">项目台账</el-button>
        <el-button @click="router.push('/major-demand/list')">重大需求</el-button>
        <el-button @click="router.push('/alert/center')">告警中心</el-button>
      </div>
    </AppTableCard>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import AppTableCard from '../../components/AppTableCard.vue'
import OperationsTaskPanel from '../../components/OperationsTaskPanel.vue'
import { fetchWorkbench } from '../../api/modules/dashboard'
import { useOperationsTasks } from '../../composables/useOperationsTasks'
import type { WorkbenchData } from '../../types/workbench'
import { getErrorMessage } from '../../utils/error'

const router = useRouter()
const loading = ref(false)
const data = ref<WorkbenchData | null>(null)
const {
  loading: operationsLoading,
  summary: operationsSummary,
  tasks: operationsTasks,
  query: operationsQuery,
  loadOperationsTasks,
  applyQuery: applyOperationsTaskQuery,
  resetQuery: resetOperationsTaskQuery,
} = useOperationsTasks(8)

const statCards = computed(() => [
  { title: '我的项目', value: String(data.value?.myProjects ?? 0), context: '项目总览', note: '当前账号数据范围内的项目数量', onClick: () => void router.push('/project/list') },
  { title: '待处理报修', value: String(data.value?.pendingRepairCount ?? 0), context: '维修响应', note: '仍待接单或处理完成的报修记录', onClick: () => void router.push('/repair/list') },
  { title: '待巡检', value: String(data.value?.pendingInspectionCount ?? 0), context: '计划执行', note: '待安排或待确认的巡检任务', onClick: () => void router.push('/inspection/plan') },
  { title: '本月工时(人天)', value: String(data.value?.thisMonthWorkHours ?? 0), context: '投入效率', note: '按当月已登记工时折算的人天投入', onClick: () => void router.push('/workhours/list') },
])

async function loadWorkbench() {
  loading.value = true
  try {
    const [res] = await Promise.all([
      fetchWorkbench(),
      loadOperationsTasks(),
    ])
    data.value = res.data
  } catch (err) {
    ElMessage.error(getErrorMessage(err, '加载工作台失败'))
  } finally {
    loading.value = false
  }
}

onMounted(() => loadWorkbench())
</script>

<style scoped>
.workbench-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.quick-actions-card {
  margin-top: 0;
}

@media (max-width: 1280px) {
  .quick-actions {
    grid-template-columns: 1fr 1fr;
  }
}

@media (max-width: 768px) {
  .quick-actions {
    grid-template-columns: 1fr;
  }
}
</style>
