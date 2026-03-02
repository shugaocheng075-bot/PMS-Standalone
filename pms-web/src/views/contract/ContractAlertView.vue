<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">合同预警</h2>
        <div class="page-subtitle">按超期风险分级管理合同跟进任务</div>
      </div>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="6">
        <el-card shadow="never" class="stat-card stats-card"><div class="t">提醒</div><div class="v">{{ summary.reminderCount }}</div></el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never" class="stat-card stats-card"><div class="t">警告</div><div class="v warning">{{ summary.warningCount }}</div></el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never" class="stat-card stats-card"><div class="t">严重</div><div class="v danger">{{ summary.criticalCount }}</div></el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never" class="stat-card stats-card"><div class="t">全部</div><div class="v">{{ summary.total }}</div></el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <el-form-item label="预警级别">
          <el-select v-model="query.alertLevel" placeholder="全部" clearable style="width: 140px">
            <el-option label="提醒" value="提醒" />
            <el-option label="警告" value="警告" />
            <el-option label="严重" value="严重" />
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
        <el-form-item>
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe>
        <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip />
        <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip />
        <el-table-column prop="groupName" label="组别" width="120" show-overflow-tooltip />
        <el-table-column prop="contractStatus" label="合同状态" min-width="170" show-overflow-tooltip />
        <el-table-column prop="maintenanceAmount" label="维护金额(万)" width="130" align="right" />
        <el-table-column prop="overdueDays" label="超期天数" width="110" align="right" />
        <el-table-column prop="alertLevel" label="预警级别" width="120">
          <template #default="scope">
            <el-tag :type="tagType(scope.row.alertLevel)">{{ scope.row.alertLevel }}</el-tag>
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
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { fetchContractAlerts, fetchContractAlertSummary } from '../../api/modules/contract'
import type { ContractAlertItem, ContractAlertSummary } from '../../types/contract'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

const loading = ref(false)
const total = ref(0)
const tableData = ref<ContractAlertItem[]>([])
const summary = ref<ContractAlertSummary>({ reminderCount: 0, warningCount: 0, criticalCount: 0, total: 0 })

const query = reactive({
  alertLevel: '',
  province: '',
  groupName: '',
  page: 1,
  size: 10,
})

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
    const res = await fetchContractAlerts({ page: 1, size: 200 })
    const items = res.data.items

    if (!items.length) {
      return
    }

    const provinces = Array.from(new Set(items.map((item) => item.province).filter(Boolean)))
    if (provinces.length > 0) {
      provinceOptions.value = provinces
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

    groupOptionsByProvince.value = map
  } catch {
  }
}
const { runInitialLoad } = useResilientLoad()

const tagType = (level: string) => {
  if (level === '严重') return 'danger'
  if (level === '警告') return 'warning'
  return 'info'
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

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.alertLevel = ''
  query.province = ''
  query.groupName = ''
  query.page = 1
  query.size = 10
  loadData()
}

const refreshLinkedData = async () => {
  await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
}

useLinkedRealtimeRefresh({
  refresh: refreshLinkedData,
  scope: 'contract',
  intervalMs: 10000,
})

onMounted(async () => {
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
</script>

<style scoped>
</style>
