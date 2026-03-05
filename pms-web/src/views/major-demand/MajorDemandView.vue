<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">重大需求</h2>
        <div class="page-subtitle">支持状态流转、批量操作、评论日志与导出</div>
      </div>
    </div>

    <el-card shadow="never" class="filter-card">
      <el-space wrap>
        <el-input v-model="query.keyword" placeholder="搜索关键字" clearable style="width: 220px" />
        <el-select v-model="query.status" placeholder="状态" clearable style="width: 150px">
          <el-option v-for="item in statusOptions" :key="item" :label="item" :value="item" />
        </el-select>
        <el-select v-model="query.owner" placeholder="负责人" clearable style="width: 150px">
          <el-option v-for="item in ownerOptions" :key="item" :label="item" :value="item" />
        </el-select>

        <el-button type="primary" @click="onSearch">查询</el-button>
        <el-button @click="onReset">重置</el-button>
        <el-button :loading="loading.fetching" @click="loadData">刷新</el-button>
        <el-button v-if="canManageMaintenance" type="primary" :loading="loading.importing" @click="onImport">从桌面导入重大需求</el-button>
        <el-button :loading="loading.exporting" @click="onExport">导出CSV</el-button>
      </el-space>

      <el-space v-if="canManageMajorDemand" wrap style="margin-top: 10px">
        <el-select v-model="batchForm.status" placeholder="批量状态" style="width: 150px">
          <el-option v-for="item in statusOptions" :key="`batch-status-${item}`" :label="item" :value="item" />
        </el-select>
        <el-button :disabled="!selectedRowIds.length || !batchForm.status" @click="onBatchStatus">批量更新状态</el-button>

        <el-input v-model="batchForm.owner" placeholder="批量负责人" style="width: 150px" />
        <el-button :disabled="!selectedRowIds.length" @click="onBatchOwner">批量分配负责人</el-button>

        <el-date-picker
          v-model="batchForm.dueDate"
          type="date"
          value-format="YYYY-MM-DD"
          placeholder="批量截止日期"
          style="width: 170px"
        />
        <el-button :disabled="!selectedRowIds.length" @click="onBatchDueDate">批量设置截止日期</el-button>
      </el-space>

      <div v-if="summaryText" style="margin-top: 12px; color: var(--el-text-color-secondary)">
        {{ summaryText }}
      </div>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table
        :data="displayRows"
        v-loading="loading.fetching"
        stripe
        max-height="520"
        scrollbar-always-on
        empty-text="暂无重大需求数据"
        @selection-change="onSelectionChange"
      >
        <el-table-column type="selection" width="46" />
        <el-table-column prop="_status" label="状态" min-width="120">
          <template #default="scope">
            <el-tag :type="statusTagType(scope.row._status)">{{ scope.row._status || '待评估' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="_owner" label="负责人" min-width="130" show-overflow-tooltip />
        <el-table-column prop="_dueDate" label="截止日期" min-width="120" show-overflow-tooltip />
        <el-table-column
          v-for="column in columns"
          :key="column"
          :prop="column"
          :label="column"
          min-width="180"
          show-overflow-tooltip
        >
          <template #default="scope">
            <el-progress
              v-if="isProgressColumn(column) && resolveProgressPercent(scope.row[column]) !== null"
              :percentage="resolveProgressPercent(scope.row[column])!"
              :stroke-width="14"
              status="success"
              :text-inside="true"
            />
            <span v-else>{{ scope.row[column] }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" fixed="right" width="220">
          <template #default="scope">
            <el-space>
              <el-button link type="primary" @click="openDetail(scope.row)">详情</el-button>
              <el-button
                link
                type="primary"
                :disabled="!canCommentMajorDemand"
                @click="openComment(scope.row)"
              >评论</el-button>
            </el-space>
          </template>
        </el-table-column>
      </el-table>

      <div class="pager" style="margin-top: 12px; color: var(--el-text-color-secondary)">
        共 {{ displayRows.length }} 条（已选 {{ selectedRowIds.length }} 条），字段 {{ columns.length }} 列
      </div>
    </el-card>

    <el-drawer v-model="detailVisible" title="重大需求详情" size="48%">
      <template v-if="activeRow">
        <el-descriptions :column="1" border>
          <el-descriptions-item label="状态">{{ activeWorkflow?.status || '待评估' }}</el-descriptions-item>
          <el-descriptions-item label="负责人">{{ activeWorkflow?.owner || '-' }}</el-descriptions-item>
          <el-descriptions-item label="截止日期">{{ activeWorkflow?.dueDate || '-' }}</el-descriptions-item>
          <el-descriptions-item v-for="column in columns" :key="`desc-${column}`" :label="column">
            {{ activeRow[column] || '-' }}
          </el-descriptions-item>
        </el-descriptions>

        <h4 style="margin: 16px 0 8px">评论</h4>
        <el-timeline v-if="(activeWorkflow?.comments.length ?? 0) > 0">
          <el-timeline-item
            v-for="comment in activeWorkflow?.comments"
            :key="comment.id"
            :timestamp="formatTime(comment.createdAt)"
          >
            <div style="font-weight: 600">{{ comment.createdBy }}</div>
            <div>{{ comment.content }}</div>
          </el-timeline-item>
        </el-timeline>
        <el-empty v-else description="暂无评论" :image-size="60" />

        <h4 style="margin: 16px 0 8px">操作日志</h4>
        <el-timeline v-if="(activeWorkflow?.logs.length ?? 0) > 0">
          <el-timeline-item
            v-for="log in activeWorkflow?.logs"
            :key="log.id"
            :timestamp="formatTime(log.createdAt)"
          >
            <div style="font-weight: 600">{{ log.action }}（{{ log.createdBy }}）</div>
            <div>{{ log.detail }}</div>
          </el-timeline-item>
        </el-timeline>
        <el-empty v-else description="暂无日志" :image-size="60" />
      </template>
    </el-drawer>

    <el-dialog v-model="commentVisible" title="新增评论" width="520px">
      <el-input v-model="commentContent" type="textarea" :rows="4" maxlength="500" show-word-limit />
      <template #footer>
        <el-button @click="commentVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading.commenting" @click="submitComment">提交</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  addMajorDemandComment,
  batchAssignMajorDemandOwner,
  batchUpdateMajorDemandDueDate,
  batchUpdateMajorDemandStatus,
  exportMajorDemandCsv,
  fetchMajorDemands,
  importMajorDemandsFromDesktop,
  type MajorDemandWorkflow,
} from '../../api/modules/majorDemand'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'

const columns = ref<string[]>([])
const rows = ref<Array<Record<string, string>>>([])
const workflows = ref<MajorDemandWorkflow[]>([])
const sourceFilePath = ref('')
const importedAt = ref('')
const selectedRowIds = ref<string[]>([])
const detailVisible = ref(false)
const activeRowId = ref('')
const commentVisible = ref(false)
const commentTargetRowId = ref('')
const commentContent = ref('')

const query = reactive({
  keyword: '',
  status: '',
  owner: '',
})

const batchForm = reactive({
  status: '处理中',
  owner: '',
  dueDate: '',
})

const loading = reactive({
  fetching: false,
  importing: false,
  exporting: false,
  commenting: false,
})

const access = useAccessControl()
const canManageMaintenance = computed(() => access.canPermission('maintenance.manage'))
const canManageMajorDemand = computed(() => {
  if (!access.isManager()) {
    return false
  }

  return access.canPermission('major-demand.manage') || access.canPermission('maintenance.manage')
})
const canCommentMajorDemand = computed(() => {
  if (!access.canPermission('major-demand.manage')) {
    return false
  }

  return access.isManager() || access.isOperator()
})

const statusOptions = ['待评估', '处理中', '待验证', '已完成', '已关闭']

const workflowMap = computed(() => {
  const map = new Map<string, MajorDemandWorkflow>()
  for (const item of workflows.value) {
    map.set(item.rowId, item)
  }
  return map
})

const ownerOptions = computed(() => {
  const owners = workflows.value.map((item) => item.owner).filter((x) => !!x)
  return Array.from(new Set(owners))
})

const displayRows = computed(() => {
  const keyword = query.keyword.trim().toLowerCase()
  return rows.value
    .filter((row) => {
      const rowId = row._RowId ?? ''
      const workflow = workflowMap.value.get(rowId)
      if (query.status && workflow?.status !== query.status) return false
      if (query.owner && workflow?.owner !== query.owner) return false
      if (!keyword) return true

      const inColumns = columns.value.some((column) => String(row[column] ?? '').toLowerCase().includes(keyword))
      const inWorkflow = [workflow?.status, workflow?.owner, workflow?.dueDate].some((x) => String(x ?? '').toLowerCase().includes(keyword))
      return inColumns || inWorkflow
    })
    .map((row) => {
      const rowId = row._RowId ?? ''
      const workflow = workflowMap.value.get(rowId)
      return {
        ...row,
        _rowId: rowId,
        _status: workflow?.status ?? '待评估',
        _owner: workflow?.owner ?? '',
        _dueDate: workflow?.dueDate ?? '',
      }
    })
})

const activeRow = computed<Record<string, string> | null>(() => rows.value.find((item) => item._RowId === activeRowId.value) ?? null)
const activeWorkflow = computed(() => workflowMap.value.get(activeRowId.value) ?? null)

const summaryText = computed(() => {
  if (!sourceFilePath.value) {
    return ''
  }

  const importedText = importedAt.value ? `，导入时间：${importedAt.value}` : ''
  return `来源文件：${sourceFilePath.value}${importedText}`
})

const isProgressColumn = (columnName: string): boolean => {
  const value = columnName.trim().toLowerCase()
  return value.includes('进度') || value.includes('progress') || value.includes('完成率')
}

const resolveProgressPercent = (rawValue: unknown): number | null => {
  if (rawValue === null || rawValue === undefined) {
    return null
  }

  const text = String(rawValue).trim().replace('%', '')
  if (!text) {
    return null
  }

  const parsed = Number(text)
  if (!Number.isFinite(parsed)) {
    return null
  }

  const percent = parsed <= 1 ? parsed * 100 : parsed
  const normalized = Math.max(0, Math.min(100, percent))
  return Math.round(normalized * 100) / 100
}

const statusTagType = (status: string) => {
  if (status === '已完成') return 'success'
  if (status === '已关闭') return 'info'
  if (status === '待验证') return 'warning'
  if (status === '处理中') return 'primary'
  return 'danger'
}

const formatTime = (value: string) => {
  if (!value) return '-'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
}

const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await loadData()
  },
  scope: 'major-demand',
  intervalMs: 60000,
})

const loadData = async () => {
  loading.fetching = true
  try {
    const res = await fetchMajorDemands()
    columns.value = (res.data.columns ?? []).filter((column) => column !== '_RowId')
    rows.value = res.data.rows ?? []
    workflows.value = res.data.workflows ?? []
    sourceFilePath.value = res.data.sourceFilePath ?? ''
    importedAt.value = res.data.importedAt ? String(res.data.importedAt) : ''
    selectedRowIds.value = selectedRowIds.value.filter((id) => rows.value.some((item) => item._RowId === id))
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载重大需求数据失败，请稍后重试'))
  } finally {
    loading.fetching = false
  }
}

const onSearch = () => {
}

const onReset = () => {
  query.keyword = ''
  query.status = ''
  query.owner = ''
}

const onSelectionChange = (selection: Array<Record<string, string>>) => {
  selectedRowIds.value = selection.map((item) => item._rowId ?? '').filter(Boolean)
}

const onBatchStatus = async () => {
  if (!canManageMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求批量管理权限')
    return
  }

  if (!selectedRowIds.value.length || !batchForm.status) {
    return
  }

  try {
    await batchUpdateMajorDemandStatus({ rowIds: selectedRowIds.value, status: batchForm.status })
    ElMessage.success('批量状态更新成功')
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '批量状态更新失败'))
  }
}

const onBatchOwner = async () => {
  if (!canManageMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求批量管理权限')
    return
  }

  if (!selectedRowIds.value.length) {
    return
  }

  try {
    await batchAssignMajorDemandOwner({ rowIds: selectedRowIds.value, owner: batchForm.owner.trim() })
    ElMessage.success('批量负责人更新成功')
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '批量负责人更新失败'))
  }
}

const onBatchDueDate = async () => {
  if (!canManageMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求批量管理权限')
    return
  }

  if (!selectedRowIds.value.length) {
    return
  }

  try {
    await batchUpdateMajorDemandDueDate({ rowIds: selectedRowIds.value, dueDate: batchForm.dueDate })
    ElMessage.success('批量截止日期更新成功')
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '批量截止日期更新失败'))
  }
}

const openDetail = (row: Record<string, string>) => {
  activeRowId.value = row._rowId ?? ''
  detailVisible.value = true
}

const openComment = (row: Record<string, string>) => {
  if (!canCommentMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求进度维护权限')
    return
  }

  commentTargetRowId.value = row._rowId ?? ''
  commentContent.value = ''
  commentVisible.value = true
}

const submitComment = async () => {
  if (!canCommentMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求进度维护权限')
    return
  }

  if (!commentTargetRowId.value || !commentContent.value.trim()) {
    ElMessage.warning('请输入评论内容')
    return
  }

  loading.commenting = true
  try {
    await addMajorDemandComment(commentTargetRowId.value, commentContent.value.trim())
    ElMessage.success('评论提交成功')
    commentVisible.value = false
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '评论提交失败'))
  } finally {
    loading.commenting = false
  }
}

const onExport = async () => {
  loading.exporting = true
  try {
    const blob = await exportMajorDemandCsv()
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `major-demands-${Date.now()}.csv`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  } finally {
    loading.exporting = false
  }
}

const onImport = async () => {
  if (!canManageMaintenance.value) {
    ElMessage.warning('当前账号无导入权限')
    return
  }

  try {
    await ElMessageBox.confirm(
      '将从桌面文件 C:\\Users\\R9000P\\Desktop\\项目明细.xlsx 的“重大需求”工作表导入，仅更新重大需求模块，确认继续吗？',
      '导入确认',
      {
        type: 'warning',
        confirmButtonText: '确认导入',
        cancelButtonText: '取消',
      },
    )
  } catch {
    return
  }

  loading.importing = true
  try {
    const res = await importMajorDemandsFromDesktop()
    ElMessage.success(res.data.message ?? '重大需求导入成功')
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导入重大需求失败，请稍后重试'))
  } finally {
    loading.importing = false
  }
}

onMounted(async () => {
  await access.ensureAccessProfileLoaded()
  await loadData()
})
</script>

<style scoped>
</style>