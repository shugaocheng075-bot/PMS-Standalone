<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">重大需求</h2>
        <div class="page-subtitle">支持状态流转、批量操作、评论日志与导出</div>
      </div>
    </div>

    <AppFilterCard>
      <el-form class="filter-form" @submit.prevent>
        <el-form-item label="关键字">
          <el-input v-model="query.keyword" placeholder="搜索名称或描述" clearable style="width: 220px" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="query.status" placeholder="全部状态" clearable style="width: 150px">
            <el-option v-for="item in statusOptions" :key="item" :label="item" :value="item" />
          </el-select>
        </el-form-item>
        <el-form-item label="负责人">
          <el-select v-model="query.owner" placeholder="全部负责人" clearable style="width: 150px">
            <el-option v-for="item in ownerOptions" :key="item" :label="item" :value="item" />
          </el-select>
        </el-form-item>

        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>

      <div v-if="canManageMajorDemand" style="margin-top: 16px; padding-top: 16px; border-top: 1px solid var(--el-border-color-light); display: flex; gap: 12px; align-items: center;">
        <span style="font-size: 13px; color: var(--el-text-color-regular); font-weight: 500;">批量操作:</span>
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
      </div>
    </AppFilterCard>

    <ProTable
      title="需求列表"
      :data="pagedRows"
      :loading="loading.fetching"
      :total="displayRows.length"
      v-model:page="currentPage"
      v-model:size="pageSize"
      @refresh="loadData"
      stripe
      row-key="_rowId"
      empty-text="暂无重大需求数据"
      @selection-change="onSelectionChange"
      @row-dblclick="onRowDoubleClick"
    >
      <template #toolbar>
        <el-button type="success" @click="onAddRow">新增</el-button>
        <el-button :loading="loading.exporting" @click="onExport">导出 Excel</el-button>
      </template>

      
      
        <el-table-column type="selection" width="46" />
        <el-table-column v-if="hospitalColumn" :prop="hospitalColumn" :label="hospitalColumn" min-width="180" show-overflow-tooltip />
        <el-table-column v-if="demandNoColumn" :prop="demandNoColumn" :label="demandNoColumn" min-width="150" show-overflow-tooltip />
        <el-table-column v-if="progressColumn" :prop="progressColumn" :label="progressColumn" min-width="160">
          <template #default="scope">
            <el-progress
              v-if="progressColumn && resolveProgressPercent(scope.row[progressColumn]) !== null"
              :percentage="resolveProgressPercent(scope.row[progressColumn]) ?? 0"
              :stroke-width="14"
              :status="(resolveProgressPercent(scope.row[progressColumn]) ?? 0) >= 100 ? 'success' : ''"
              :text-inside="true"
            />
            <span v-else>{{ progressColumn ? scope.row[progressColumn] : '' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="_status" label="状态" min-width="100">
          <template #default="scope">
            <el-tag :type="statusTagType(scope.row._status)">{{ scope.row._status || '待评估' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="_owner" label="负责人" min-width="100" show-overflow-tooltip />
        <el-table-column prop="_dueDate" label="截止日期" min-width="120" show-overflow-tooltip />
        <el-table-column
          v-for="column in remainingColumns"
          :key="column"
          :prop="column"
          :label="column"
          min-width="180"
          show-overflow-tooltip
        />
        <el-table-column label="操作" fixed="right" width="260">
          <template #default="scope">
            <el-space>
              <el-button link type="primary" @click="openEdit(scope.row)">编辑</el-button>
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
      

      <div class="pager" style="margin-top: 12px; display: flex; justify-content: space-between; align-items: center;">
        <span style="color: var(--el-text-color-secondary)">
          共 {{ displayRows.length }} 条（已选 {{ selectedRowIds.length }} 条）
        </span>
        <el-pagination
          background
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[15, 30, 50, 100]"
          :total="displayRows.length"
          layout="total, sizes, prev, pager, next"
        />
      </div>
    
    </ProTable>

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

    <ProDrawer v-model="commentVisible" title="新增评论" width="520px">
      <el-input v-model="commentContent" type="textarea" :rows="4" maxlength="500" show-word-limit />
      <template #footer>
        <el-button @click="commentVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading.commenting" @click="submitComment">提交</el-button>
      </template>
    </ProDrawer>

    <ProDrawer v-model="editVisible" title="编辑重大需求" width="680px">
      <el-form v-if="editForm" label-width="120px" label-position="right">
        <el-form-item label="状态">
          <el-select v-model="editForm._status" style="width: 100%">
            <el-option v-for="item in statusOptions" :key="`edit-s-${item}`" :label="item" :value="item" />
          </el-select>
        </el-form-item>
        <el-form-item label="负责人">
          <el-input v-model="editForm._owner" />
        </el-form-item>
        <el-form-item label="截止日期">
          <el-date-picker v-model="editForm._dueDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
        <el-form-item v-for="column in columns" :key="`edit-${column}`" :label="column">
          <el-input v-model="editForm[column]" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading.saving" @click="submitEdit">保存</el-button>
      </template>
    </ProDrawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import {
  addMajorDemandComment,
  addMajorDemandRow,
  batchAssignMajorDemandOwner,
  batchUpdateMajorDemandDueDate,
  batchUpdateMajorDemandStatus,
  exportMajorDemandExcel,
  fetchMajorDemands,
  updateMajorDemandCell,
  type MajorDemandWorkflow,
} from '../../api/modules/majorDemand'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { normalizeStatusText, resolveMajorDemandStatusTag } from '../../utils/statusTag'
import ProTable from '../../components/ProTable.vue'
import ProDrawer from '../../components/ProDrawer.vue'


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
const currentPage = ref(1)
const pageSize = ref(15)
const editVisible = ref(false)
const editRowId = ref('')
const editForm = ref<Record<string, string>>({})
const route = useRoute()
const router = useRouter()

const readRouteQueryValue = (value: unknown): string => {
  if (typeof value === 'string') {
    return value
  }

  if (Array.isArray(value) && typeof value[0] === 'string') {
    return value[0]
  }

  return ''
}

const updateRouteQuery = async (patch: Record<string, string | undefined>) => {
  const nextQuery = { ...route.query }
  Object.entries(patch).forEach(([key, value]) => {
    if (value) {
      nextQuery[key] = value
      return
    }

    delete nextQuery[key]
  })

  await router.replace({ path: route.path, query: nextQuery })
}

const clearRouteActionQuery = async () => {
  if (!readRouteQueryValue(route.query.action) && !readRouteQueryValue(route.query.rowId)) {
    return
  }

  await updateRouteQuery({ action: undefined, rowId: undefined })
}

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
  exporting: false,
  commenting: false,
  saving: false,
})

const access = useAccessControl()

type MajorDemandFilterState = {
  keyword: string
  status: string
  owner: string
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<MajorDemandFilterState>({
  key: 'major-demand',
  getState: () => ({ keyword: query.keyword, status: query.status, owner: query.owner }),
  applyState: (state) => {
    query.keyword = typeof state.keyword === 'string' ? state.keyword : ''
    query.status = typeof state.status === 'string' ? state.status : ''
    query.owner = typeof state.owner === 'string' ? state.owner : ''
  },
})

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

const statusOptions = ['待评估', '待处理', '处理中', '待验证', '已完成', '已关闭']

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

const hospitalColumn = computed(() => columns.value.find((c) => c.includes('医院')))
const demandNoColumn = computed(() => columns.value.find((c) => c.includes('编号')))
const progressColumn = computed(() => columns.value.find((c) => isProgressColumn(c)))
const priorityColumnSet = computed(() => new Set([hospitalColumn.value, demandNoColumn.value, progressColumn.value].filter(Boolean)))
const remainingColumns = computed(() => columns.value.filter((c) => !priorityColumnSet.value.has(c)))

const displayRows = computed(() => {
  const keyword = query.keyword.trim().toLowerCase()
  return rows.value
    .filter((row) => {
      const rowId = row._RowId ?? ''
      const workflow = workflowMap.value.get(rowId)
      if (query.status && normalizeStatusText(workflow?.status) !== normalizeStatusText(query.status)) return false
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

const pagedRows = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return displayRows.value.slice(start, start + pageSize.value)
})

const activeRow = computed<Record<string, string> | null>(() => rows.value.find((item) => item._RowId === activeRowId.value) ?? null)
const activeWorkflow = computed(() => workflowMap.value.get(activeRowId.value) ?? null)

const applyRouteFilters = () => {
  const status = readRouteQueryValue(route.query.status)
  const owner = readRouteQueryValue(route.query.owner)
  const keyword = readRouteQueryValue(route.query.keyword)

  if (status) {
    query.status = status
  }

  if (owner) {
    query.owner = owner
  }

  if (keyword) {
    query.keyword = keyword
  }
}

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

const statusTagType = (status: string) => resolveMajorDemandStatusTag(status)

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

const { runInitialLoad } = useResilientLoad()

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
    currentPage.value = 1
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载重大需求数据失败，请稍后重试'))
  } finally {
    loading.fetching = false
  }
}

const onSearch = () => {
  currentPage.value = 1
}

const onReset = () => {
  query.keyword = ''
  query.status = ''
  query.owner = ''
  currentPage.value = 1
  clearFilterState()
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
  void updateRouteQuery({ action: 'detail', rowId: activeRowId.value || undefined })
}

const onRowDoubleClick = (row: Record<string, string>) => {
  openDetail(row)
}

const openComment = (row: Record<string, string>) => {
  if (!canCommentMajorDemand.value) {
    ElMessage.warning('当前账号无重大需求进度维护权限')
    return
  }

  commentTargetRowId.value = row._rowId ?? ''
  commentContent.value = ''
  commentVisible.value = true
  void updateRouteQuery({ action: 'comment', rowId: commentTargetRowId.value || undefined })
}

const syncPanelFromRoute = () => {
  const action = readRouteQueryValue(route.query.action)
  const rowId = readRouteQueryValue(route.query.rowId)
  if (!action || !rowId) {
    return
  }

  const matched = displayRows.value.find((item) => item._rowId === rowId)
  if (!matched) {
    return
  }

  if (action === 'detail' && !detailVisible.value) {
    openDetail(matched)
    return
  }

  if (action === 'comment' && !commentVisible.value) {
    openComment(matched)
  }
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

const openEdit = (row: Record<string, string>) => {
  editRowId.value = row._rowId ?? ''
  editForm.value = { ...row }
  editVisible.value = true
}

const submitEdit = async () => {
  if (!editRowId.value) return
  loading.saving = true
  try {
    const origRow = displayRows.value.find((r) => r._rowId === editRowId.value)
    if (origRow) {
      if (editForm.value._status !== origRow._status) {
        await batchUpdateMajorDemandStatus({ rowIds: [editRowId.value], status: editForm.value._status ?? '' })
      }
      if (editForm.value._owner !== origRow._owner) {
        await batchAssignMajorDemandOwner({ rowIds: [editRowId.value], owner: editForm.value._owner ?? '' })
      }
      if (editForm.value._dueDate !== origRow._dueDate) {
        await batchUpdateMajorDemandDueDate({ rowIds: [editRowId.value], dueDate: editForm.value._dueDate ?? '' })
      }
      for (const column of columns.value) {
        if (editForm.value[column] !== (origRow as Record<string, string>)[column]) {
          await updateMajorDemandCell(editRowId.value, column, editForm.value[column] ?? '')
        }
      }
    }
    ElMessage.success('保存成功')
    editVisible.value = false
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存失败'))
  } finally {
    loading.saving = false
  }
}

const onAddRow = async () => {
  try {
    await addMajorDemandRow()
    ElMessage.success('新增成功')
    await loadData()
    notifyDataChanged('major-demand')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '新增失败'))
  }
}

const onExport = async () => {
  loading.exporting = true
  try {
    const blob = await exportMajorDemandExcel()
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `major-demands-${Date.now()}.xlsx`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  } finally {
    loading.exporting = false
  }
}

onMounted(async () => {
  await access.ensureAccessProfileLoaded()
  restoreFilterState()
  applyRouteFilters()
  await runInitialLoad({
    tasks: [loadData],
  })
  syncPanelFromRoute()
})

watch(detailVisible, (visible) => {
  if (!visible && !commentVisible.value) {
    void clearRouteActionQuery()
  }
})

watch(commentVisible, (visible) => {
  if (!visible && !detailVisible.value) {
    void clearRouteActionQuery()
  }
})

watch(() => route.fullPath, () => {
  applyRouteFilters()
  syncPanelFromRoute()
})
</script>

<style scoped>
</style>
