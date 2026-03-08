<template>
  <div class="page-shell workbench-page" v-loading="loading">
    <div class="page-head">
      <div>
        <h2 class="page-title">区域经理工作台</h2>
        <div class="page-subtitle">区域数据总览、月报审批与区域告警</div>
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

    <!-- 审批 & 告警 -->
    <el-row :gutter="16">
      <el-col :span="8">
        <el-card shadow="never" class="todo-card">
          <template #header>
            <div class="panel-head">
              <span class="panel-title">月报审批</span>
              <el-button link type="primary" @click="router.push('/annual-report/list')">查看全部</el-button>
            </div>
          </template>
          <div class="empty-tip">前往年报/月报列表审批已提交的月报</div>
          <el-button type="primary" plain size="small" @click="router.push('/annual-report/list')">
            去审批
          </el-button>
        </el-card>
      </el-col>

      <el-col :span="8">
        <el-card shadow="never" class="todo-card">
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
        </el-card>
      </el-col>

      <el-col :span="8">
        <el-card shadow="never" class="todo-card">
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
        </el-card>
      </el-col>
    </el-row>

    <!-- 区域巡检 -->
    <el-card shadow="never" class="todo-card" style="margin-top: 16px;">
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
    </el-card>
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
  { title: '区域项目', value: String(data.value?.myProjects ?? 0), onClick: () => void router.push('/project/list') },
  { title: '待处理报修', value: String(data.value?.pendingRepairCount ?? 0), onClick: () => void router.push('/repair/list') },
  { title: '待巡检', value: String(data.value?.pendingInspectionCount ?? 0), onClick: () => void router.push('/inspection/plan') },
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
  font-size: 15px;
  font-weight: 600;
}

.stats-row {
  margin-bottom: 0;
}

.stat-card {
  text-align: center;
  cursor: pointer;
  transition: box-shadow 0.2s;
}

.stat-card:hover {
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.stat-card .t {
  font-size: 13px;
  color: #909399;
  margin-bottom: 8px;
}

.stat-card .v {
  font-size: 28px;
  font-weight: 700;
  color: #303133;
}

.todo-card {
  min-height: 180px;
}

.todo-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid #f0f0f0;
  cursor: pointer;
}

.todo-item:last-child {
  border-bottom: none;
}

.todo-item:hover {
  background-color: #f5f7fa;
}

.todo-main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.todo-label {
  font-size: 14px;
  color: #303133;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.todo-sub {
  font-size: 12px;
  color: #909399;
}

.empty-tip {
  text-align: center;
  color: #909399;
  padding: 24px 0;
  font-size: 14px;
}
</style>
