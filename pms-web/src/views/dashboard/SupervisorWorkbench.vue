<template>
  <div class="page-shell workbench-page" v-loading="loading">
    <div class="page-head">
      <div>
        <h2 class="page-title">主管工作台</h2>
        <div class="page-subtitle">团队待办、审批队列与团队数据概览</div>
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

    <div class="workbench-grid">
      <AppTableCard class="todo-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">待确认工时</span>
              <el-button link type="primary" @click="router.push('/workhours/list')" icon="View">查看全部</el-button>
            </div>
          </template>
          <div class="empty-tip">前往工时列表确认提交的工时记录</div>
          <el-button type="primary" plain size="small" @click="router.push('/workhours/list')" icon="Service">
            去处理
          </el-button>
        </AppTableCard>

      <AppTableCard class="todo-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">待审核巡检</span>
              <el-button link type="primary" @click="router.push('/inspection/plan')" icon="View">查看全部</el-button>
            </div>
          </template>
          <div v-if="!data?.pendingInspections.length" class="empty-tip">暂无待审核巡检</div>
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
              <span class="panel-title">团队未处理报修</span>
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

    <AppTableCard class="todo-card">
      <template #header>
        <div class="panel-head">
          <span class="panel-title">团队临期合同</span>
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
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import AppTableCard from '../../components/AppTableCard.vue'
import { fetchWorkbench } from '../../api/modules/dashboard'
import type { WorkbenchData } from '../../types/workbench'
import { getErrorMessage } from '../../utils/error'

const router = useRouter()
const loading = ref(false)
const data = ref<WorkbenchData | null>(null)

const statCards = computed(() => [
  { title: '团队项目', value: String(data.value?.myProjects ?? 0), context: '项目范围', note: '当前团队口径下需要跟进的项目总量', onClick: () => void router.push('/project/list') },
  { title: '待处理报修', value: String(data.value?.pendingRepairCount ?? 0), context: '服务响应', note: '团队当前仍需处理的报修任务数量', onClick: () => void router.push('/repair/list') },
  { title: '待审核巡检', value: String(data.value?.pendingInspectionCount ?? 0), context: '质控审批', note: '等待主管确认结果的巡检计划', onClick: () => void router.push('/inspection/plan') },
  { title: '本月工时(人天)', value: String(data.value?.thisMonthWorkHours ?? 0), context: '团队投入', note: '本月团队工时登记折算后的人天值', onClick: () => void router.push('/workhours/list') },
])

async function loadWorkbench() {
  loading.value = true
  try {
    const res = await fetchWorkbench()
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
</style>
