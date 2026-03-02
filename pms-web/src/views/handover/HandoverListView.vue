<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">交接管理</h2>
        <div class="page-subtitle">跟踪交接批次、阶段流转与看板进度</div>
      </div>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">未发</div><div class="v">{{ summary.pendingCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">已发邮件</div><div class="v">{{ summary.emailSentCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">交接中</div><div class="v warning">{{ summary.inProgressCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">已交接</div><div class="v success">{{ summary.completedCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">总数</div><div class="v">{{ summary.total }}</div></el-card></el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <el-form-item label="阶段">
          <el-select v-model="query.stage" placeholder="全部" clearable style="width: 140px">
            <el-option label="未发" value="未发" />
            <el-option label="已发邮件" value="已发邮件" />
            <el-option label="交接中" value="交接中" />
            <el-option label="已交接" value="已交接" />
          </el-select>
        </el-form-item>
        <el-form-item label="批次">
          <el-select v-model="query.batch" placeholder="全部" clearable style="width: 140px">
            <el-option label="第一批" value="第一批" />
            <el-option label="第二批" value="第二批" />
            <el-option label="第三批" value="第三批" />
            <el-option label="第四批" value="第四批" />
          </el-select>
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="query.type" placeholder="全部" clearable style="width: 160px">
            <el-option label="实施→运维" value="实施→运维" />
            <el-option label="区域/组间" value="区域/组间" />
          </el-select>
        </el-form-item>
        <el-form-item label="原组别">
          <el-select v-model="query.fromGroup" placeholder="全部" clearable style="width: 160px">
            <el-option v-for="group in fromGroupOptions" :key="group" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="对接人">
          <el-select v-model="query.toOwner" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="owner in toOwnerOptions" :key="owner" :label="owner" :value="owner" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe>
        <el-table-column prop="handoverNo" label="交接单号" width="130" />
        <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip />
        <el-table-column prop="type" label="类型" width="120" />
        <el-table-column prop="fromGroup" label="原组别" width="130" show-overflow-tooltip />
        <el-table-column prop="fromOwner" label="提出人" width="100" />
        <el-table-column prop="toOwner" label="对接人" width="100" />
        <el-table-column prop="batch" label="批次" width="100" />
        <el-table-column prop="stage" label="阶段" width="110">
          <template #default="scope">
            <el-tag :type="stageTag(scope.row.stage)">{{ scope.row.stage }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="emailSentDate" label="邮件日期" width="130">
          <template #default="scope">
            {{ scope.row.emailSentDate ? formatDate(scope.row.emailSentDate) : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="scope">
            <el-button
              v-if="nextStage(scope.row.stage)"
              type="primary"
              link
              :loading="advancingId === scope.row.id"
              :disabled="advancingId === scope.row.id"
              @click="onAdvanceStage(scope.row)"
            >
              推进到{{ nextStage(scope.row.stage) }}
            </el-button>
            <span v-else>-</span>
          </template>
        </el-table-column>
      </el-table>

      <div class="pager">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.size"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          :total="total"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </el-card>

    <el-card shadow="never" class="table-card">
      <template #header>
        <div class="kanban-title">交接看板</div>
      </template>

      <el-row :gutter="12" class="kanban-row">
        <el-col :span="6" v-for="column in kanbanColumns" :key="column.stage">
          <div class="kanban-col">
            <div class="kanban-col-header">{{ column.stage }}（{{ column.count }}）</div>
            <div class="kanban-items">
              <div class="kanban-item" v-for="item in column.items" :key="item.id">
                <div class="kanban-item-name">{{ item.hospitalName }}</div>
                <div class="kanban-item-meta">{{ item.handoverNo }} · {{ item.toOwner }}</div>
              </div>
              <div v-if="column.count === 0" class="kanban-empty">暂无数据</div>
            </div>
          </div>
        </el-col>
      </el-row>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  fetchHandovers,
  fetchHandoverKanban,
  fetchHandoverSummary,
  updateHandoverStage,
} from '../../api/modules/handover'
import type { HandoverItem, HandoverKanbanColumn, HandoverSummary } from '../../types/handover'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { HANDOVER_GROUP_OPTIONS, HANDOVER_OWNER_OPTIONS } from '../../constants/filterOptions'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

const loading = ref(false)
const total = ref(0)
const tableData = ref<HandoverItem[]>([])
const kanbanColumns = ref<HandoverKanbanColumn[]>([])
const summary = ref<HandoverSummary>({
  pendingCount: 0,
  emailSentCount: 0,
  inProgressCount: 0,
  completedCount: 0,
  total: 0,
})

const query = reactive({
  stage: '',
  batch: '',
  type: '',
  fromGroup: '',
  toOwner: '',
  page: 1,
  size: 10,
})

const fromGroupOptions = ref<string[]>([...HANDOVER_GROUP_OPTIONS])
const toOwnerOptions = ref<string[]>([...HANDOVER_OWNER_OPTIONS])
const advancingId = ref<number | null>(null)
const { runInitialLoad } = useResilientLoad()
const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData(), loadKanban()])
  },
  scope: 'handover',
  intervalMs: 10000,
})

const stageTag = (stage: string) => {
  if (stage === '已交接') return 'success'
  if (stage === '交接中') return 'warning'
  if (stage === '已发邮件') return 'info'
  return 'danger'
}

const formatDate = (value: string) => value.slice(0, 10)

const nextStage = (stage: string) => {
  if (stage === '未发') return '已发邮件'
  if (stage === '已发邮件') return '交接中'
  if (stage === '交接中') return '已交接'
  return ''
}

const loadSummary = async () => {
  try {
    const res = await fetchHandoverSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载交接汇总失败，请稍后重试'))
  }
}

const loadKanban = async () => {
  try {
    const res = await fetchHandoverKanban()
    kanbanColumns.value = res.data
  } catch (error) {
    kanbanColumns.value = []
    ElMessage.error(getErrorMessage(error, '加载交接看板失败，请稍后重试'))
  }
}

const loadFilterOptions = async () => {
  try {
    const res = await fetchHandovers({ page: 1, size: 200 })
    const items = res.data.items

    if (!items.length) {
      return
    }

    const fromGroups = Array.from(new Set(items.map((item) => item.fromGroup).filter(Boolean)))
    if (fromGroups.length > 0) {
      fromGroupOptions.value = fromGroups
    }

    const toOwners = Array.from(new Set(items.map((item) => item.toOwner).filter(Boolean)))
    if (toOwners.length > 0) {
      toOwnerOptions.value = toOwners
    }
  } catch {
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchHandovers(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载交接列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.stage = ''
  query.batch = ''
  query.type = ''
  query.fromGroup = ''
  query.toOwner = ''
  query.page = 1
  query.size = 10
  loadData()
}

const onAdvanceStage = async (item: HandoverItem) => {
  if (advancingId.value === item.id) return

  const targetStage = nextStage(item.stage)
  if (!targetStage) return

  advancingId.value = item.id
  try {
    await updateHandoverStage(item.id, { targetStage })
    ElMessage.success(`已推进到${targetStage}`)
    await Promise.all([loadSummary(), loadData(), loadKanban()])
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '推进阶段失败，请稍后重试'))
  } finally {
    advancingId.value = null
  }
}

onMounted(async () => {
  await runInitialLoad({
    tasks: [loadSummary, loadFilterOptions, loadData, loadKanban],
    retryChecks: [
      {
        when: () => summary.value.total > 0 && total.value === 0,
        task: loadData,
      },
      {
        when: () => summary.value.total > 0 && kanbanColumns.value.length === 0,
        task: loadKanban,
      },
    ],
  })
})
</script>

<style scoped>
.kanban-title {
  font-weight: 600;
}

.kanban-col {
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  min-height: 280px;
  background: var(--el-fill-color-lighter);
}

.kanban-col-header {
  padding: 10px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  font-weight: 600;
  background: var(--el-bg-color);
}

.kanban-items {
  padding: 10px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.kanban-item {
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  padding: 8px;
}

.kanban-item-name {
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.kanban-item-meta {
  margin-top: 2px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.kanban-empty {
  color: var(--el-text-color-placeholder);
  text-align: center;
  padding: 24px 0;
}
</style>
