<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">合同预警</h2>
        <div class="page-subtitle">按超期风险分级管理合同跟进任务</div>
      </div>
      <el-button :loading="exporting" @click="onExport">导出Excel</el-button>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="6">
        <el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.alertLevel === '' }" @click="onStatClick('')"><div class="t">提醒</div><div class="v">{{ summary.reminderCount }}</div></el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.alertLevel === '警告' }" @click="onStatClick('警告')"><div class="t">警告</div><div class="v warning">{{ summary.warningCount }}</div></el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.alertLevel === '严重' }" @click="onStatClick('严重')"><div class="t">严重</div><div class="v danger">{{ summary.criticalCount }}</div></el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never" class="stat-card stats-card"><div class="t">全部</div><div class="v">{{ summary.total }}</div></el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="预警级别">
          <el-select v-model="query.alertLevel" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="level in alertLevelOptions" :key="level" :label="level" :value="level" />
          </el-select>
        </el-form-item>
        <el-form-item label="省份">
          <el-select v-model="query.province" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="province in provinceOptions" :key="province" :label="province" :value="province" />
          </el-select>
        </el-form-item>
        <el-form-item label="组别">
          <el-select v-model="query.groupName" placeholder="全部" clearable style="width: 160px">
            <el-option v-for="group in filteredGroupOptions" :key="group" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="销售">
          <el-input v-model="query.salesName" placeholder="请输入销售" clearable style="width: 160px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无符合条件的数据" @row-dblclick="onGotoProject">
        <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip sortable />
        <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip />
        <el-table-column prop="groupName" label="组别" width="120" show-overflow-tooltip />
        <el-table-column prop="salesName" label="销售" width="120" show-overflow-tooltip />
        <el-table-column prop="contractStatus" label="合同状态" min-width="170" show-overflow-tooltip />
        <el-table-column prop="maintenanceAmount" label="维护金额(万)" width="130" align="right" sortable />
        <el-table-column prop="overdueDays" label="超期天数" width="120" align="right" sortable>
          <template #default="scope">
            <span :class="overdueDaysClass(scope.row.overdueDays)">超期{{ scope.row.overdueDays }}天</span>
          </template>
        </el-table-column>
        <el-table-column prop="alertLevel" label="预警级别" width="120" sortable>
          <template #default="scope">
            <el-tag :type="tagType(scope.row.alertLevel)">{{ scope.row.alertLevel }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="scope">
            <el-button link type="primary" @click="onGotoProject(scope.row)">去处理</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pager">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.size"
          :page-sizes="[15, 30, 50, 100]"
          layout="total, sizes, prev, pager, next"
          :total="total"
          @size-change="(size: number) => { query.size = size; query.page = 1; loadData() }"
          @current-change="(page: number) => { query.page = page; loadData() }"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { fetchContractAlerts, fetchContractAlertSummary, exportContractAlerts } from '../../api/modules/contract'
import type { ContractAlertItem, ContractAlertSummary } from '../../types/contract'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

const loading = ref(false)
const exporting = ref(false)
const total = ref(0)
const tableData = ref<ContractAlertItem[]>([])
const summary = ref<ContractAlertSummary>({ reminderCount: 0, warningCount: 0, criticalCount: 0, total: 0 })

const query = reactive({
  alertLevel: '',
  province: '',
  groupName: '',
  salesName: '',
  page: 1,
  size: 15,
})
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

const applyDrillQuery = () => {
  const alertLevel = readRouteQueryValue(route.query.alertLevel)
  const province = readRouteQueryValue(route.query.province)
  const groupName = readRouteQueryValue(route.query.groupName)
  const salesName = readRouteQueryValue(route.query.salesName)
  let applied = false

  if (province) {
    query.province = province
    applied = true
  }

  if (groupName) {
    query.groupName = groupName
    applied = true
  }

  if (salesName) {
    query.salesName = salesName
    applied = true
  }

  if (!alertLevel) {
    if (applied) {
      query.page = 1
    }
    return
  }

  query.province = ''
  query.groupName = ''
  query.salesName = ''
  query.alertLevel = alertLevel
  query.page = 1
}

type ContractFilterState = {
  alertLevel: string
  province: string
  groupName: string
  salesName: string
  page: number
  size: number
}

const alertLevelOptions = ref<string[]>(['提醒', '警告', '严重'])
const provinceOptions = ref<string[]>([...PROVINCE_OPTIONS])
const groupOptionsByProvince = ref<Record<string, string[]>>({})

const allGroupOptions = computed(() => {
  const groups = Object.values(groupOptionsByProvince.value).flat()
  return groups.length > 0 ? Array.from(new Set(groups)) : GROUP_OPTIONS
})

const filteredGroupOptions = computed(() => {
  if (!query.province) {
    return allGroupOptions.value
  }

  const groups = groupOptionsByProvince.value[query.province]
  return groups && groups.length > 0 ? groups : allGroupOptions.value
})

watch(
  () => query.province,
  () => {
    if (query.groupName && !filteredGroupOptions.value.includes(query.groupName)) {
      query.groupName = ''
    }
  },
)

const loadFilterOptions = async () => {
  try {
    const res = await fetchContractAlerts({ page: 1, size: 100000 })
    const items = res.data.items

    if (!items.length) {
      return
    }

    const provinces = Array.from(new Set(items.map((item) => item.province).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (provinces.length > 0) {
      provinceOptions.value = provinces
    }

    const alertLevels = Array.from(new Set(items.map((item) => item.alertLevel).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (alertLevels.length > 0) {
      alertLevelOptions.value = Array.from(new Set([...alertLevelOptions.value, ...alertLevels]))
    }

    const map: Record<string, string[]> = {}
    for (const item of items) {
      if (!item.province || !item.groupName) continue

      const province = item.province
      const groupName = item.groupName
      const groups = map[province] ?? (map[province] = [])

      if (!groups.includes(groupName)) {
        groups.push(groupName)
      }
    }

    groupOptionsByProvince.value = Object.fromEntries(
      Object.entries(map).map(([province, groups]) => [province, groups.sort((a, b) => a.localeCompare(b, 'zh-CN'))]),
    )
  } catch {
  }
}
const { runInitialLoad } = useResilientLoad()

const tagType = (level: string) => {
  if (level === '严重') return 'danger'
  if (level === '警告') return 'warning'
  return 'info'
}

const overdueDaysClass = (overdueDays: number) => {
  if (overdueDays > 90) {
    return 'text-danger'
  }

  if (overdueDays >= 30) {
    return 'text-warning'
  }

  return 'text-info'
}

const loadSummary = async () => {
  try {
    const res = await fetchContractAlertSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载合同预警汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchContractAlerts(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载合同预警列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onStatClick = (alertLevel: string) => {
  query.province = ''
  query.groupName = ''
  query.salesName = ''
  query.alertLevel = alertLevel
  query.page = 1
  loadData()
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.alertLevel = ''
  query.province = ''
  query.groupName = ''
  query.salesName = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  loadData()
}

const onExport = async () => {
  exporting.value = true
  try {
    const blob = await exportContractAlerts({
      alertLevel: query.alertLevel || undefined,
      province: query.province || undefined,
      groupName: query.groupName || undefined,
      salesName: query.salesName || undefined,
    })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `合同预警_${Date.now()}.xlsx`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出合同预警失败，请稍后重试'))
  } finally {
    exporting.value = false
  }
}

const onGotoProject = (row: ContractAlertItem) => {
  void router.push({
    path: '/project/list',
    query: {
      hospitalName: row.hospitalName,
      groupName: row.groupName,
      salesName: row.salesName,
      contractStatus: row.contractStatus,
      action: 'edit',
    },
  })
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<ContractFilterState>({
  key: 'contract-alert',
  getState: () => ({
    alertLevel: query.alertLevel,
    province: query.province,
    groupName: query.groupName,
    salesName: query.salesName,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.alertLevel = state.alertLevel ?? ''
    query.province = state.province ?? ''
    query.groupName = state.groupName ?? ''
    query.salesName = state.salesName ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

const refreshLinkedData = async () => {
  await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
}

useLinkedRealtimeRefresh({
  refresh: refreshLinkedData,
  scope: 'contract',
  intervalMs: 60000,
})

onMounted(async () => {
  restoreFilterState()
  applyDrillQuery()
  await runInitialLoad({
    tasks: [loadSummary, loadFilterOptions, loadData],
    retryChecks: [
      {
        when: () => summary.value.total > 0 && total.value === 0,
        task: loadData,
      },
    ],
  })
})

watch(() => route.fullPath, () => {
  applyDrillQuery()
  loadData()
})
</script>

<style scoped>
</style>
