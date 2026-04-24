<template>
  <div class="page-shell workbench-page" v-loading="loading">
    <div class="page-head">
      <div>
        <h2 class="page-title">区域经理工作台</h2>
        <div class="page-subtitle">区域数据总览、月报审批与区域告警</div>
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
              <span class="panel-title">月报审批</span>
              <el-button link type="primary" @click="router.push('/annual-report/list')" icon="View">查看全部</el-button>
            </div>
          </template>
          <div class="empty-tip">前往年报/月报列表审批已提交的月报</div>
          <el-button type="primary" plain size="small" @click="router.push('/annual-report/list')">
            去审批
          </el-button>
        </AppTableCard>

      <AppTableCard class="todo-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">区域临期合同</span>
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
              <span class="panel-title">区域待处理报修</span>
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
          <span class="panel-title">区域待巡检</span>
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
  { title: '区域项目', value: String(data.value?.myProjects ?? 0), context: '区域总览', note: '当前区域范围内需要持续管理的项目数量', onClick: () => void router.push('/project/list') },
  { title: '待处理报修', value: String(data.value?.pendingRepairCount ?? 0), context: '服务保障', note: '区域内仍待消化的报修与服务事项', onClick: () => void router.push('/repair/list') },
  { title: '待巡检', value: String(data.value?.pendingInspectionCount ?? 0), context: '执行进度', note: '区域巡检计划待执行或待审核数量', onClick: () => void router.push('/inspection/plan') },
  { title: '本月工时(人天)', value: String(data.value?.thisMonthWorkHours ?? 0), context: '资源投入', note: '区域本月工时折算后的人天规模', onClick: () => void router.push('/workhours/list') },
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
