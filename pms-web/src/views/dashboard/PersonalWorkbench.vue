<template>
  <div class="page-shell workbench-page">
    <div class="page-head">
      <div>
        <h2 class="page-title">个人工作台</h2>
        <div class="page-subtitle">待办事项、个人数据统计概览</div>
      </div>
      <el-button size="small" :loading="loading" @click="loadWorkbench">刷新数据</el-button>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :span="6" v-for="card in statCards" :key="card.title">
        <el-card shadow="never" class="stat-card stats-card clickable" @click="card.onClick()">
          <div class="t">{{ card.title }}</div>
          <div class="v">{{ card.value }}</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 待办列表 -->
    <el-row :gutter="16">
      <el-col :span="8">
        <el-card shadow="never" class="todo-card">
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
        </el-card>
      </el-col>

      <el-col :span="8">
        <el-card shadow="never" class="todo-card">
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
            @click="router.push('/inspection/list')"
          >
            <div class="todo-main">
              <span class="todo-label">{{ item.hospitalName }}</span>
              <span class="todo-sub">{{ item.inspector }} · {{ item.planDate }}</span>
            </div>
            <el-tag size="small">{{ item.status }}</el-tag>
          </div>
        </el-card>
      </el-col>

      <el-col :span="8">
        <el-card shadow="never" class="todo-card">
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
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { fetchWorkbench } from '../../api/modules/dashboard'
import type { WorkbenchData } from '../../types/workbench'
import { getErrorMessage } from '../../utils/error'

const router = useRouter()
const loading = ref(false)
const data = ref<WorkbenchData | null>(null)

const statCards = computed(() => [
  { title: '我的项目', value: String(data.value?.myProjects ?? 0), onClick: () => void router.push('/project/list') },
  { title: '待处理报修', value: String(data.value?.pendingRepairCount ?? 0), onClick: () => void router.push('/repair/list') },
  { title: '待巡检', value: String(data.value?.pendingInspectionCount ?? 0), onClick: () => void router.push('/inspection/list') },
  { title: '本月工时(人天)', value: String(data.value?.thisMonthWorkHours ?? 0), onClick: () => void router.push('/workhours/list') },
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
  gap: 16px;
}

.panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.panel-title {
  font-weight: 700;
  color: #1f3f70;
}

.todo-card {
  min-height: 320px;
}

.todo-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.todo-item:last-child {
  border-bottom: none;
}

.todo-main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
  flex: 1;
  margin-right: 8px;
}

.todo-label {
  font-size: 14px;
  color: var(--el-text-color-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.todo-sub {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.empty-tip {
  color: var(--el-text-color-placeholder);
  text-align: center;
  padding: 40px 0;
}

.clickable {
  cursor: pointer;
}

@media (max-width: 1280px) {
  .el-col-8 {
    max-width: 100%;
    flex: 0 0 100%;
  }
}
</style>
