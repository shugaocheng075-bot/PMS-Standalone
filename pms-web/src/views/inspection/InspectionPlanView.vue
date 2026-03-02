<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">巡检计划</h2>
        <div class="page-subtitle">管理巡检排期、执行状态与人员安排</div>
      </div>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">已计划</div><div class="v">{{ summary.plannedCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">执行中</div><div class="v warning">{{ summary.inProgressCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">已完成</div><div class="v success">{{ summary.completedCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">已取消</div><div class="v danger">{{ summary.cancelledCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">本月计划</div><div class="v">{{ summary.thisMonthCount }}</div></el-card></el-col>
      <el-col :span="4"><el-card shadow="never" class="stat-card stats-card"><div class="t">总数</div><div class="v">{{ summary.total }}</div></el-card></el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <el-form-item label="状态">
          <el-select v-model="query.status" clearable style="width: 140px" placeholder="全部">
            <el-option label="已计划" value="已计划" />
            <el-option label="执行中" value="执行中" />
            <el-option label="已完成" value="已完成" />
            <el-option label="已取消" value="已取消" />
          </el-select>
        </el-form-item>
        <el-form-item label="省份">
          <el-select v-model="query.province" clearable style="width: 140px" placeholder="全部">
            <el-option v-for="province in provinceOptions" :key="province" :label="province" :value="province" />
          </el-select>
        </el-form-item>
        <el-form-item label="产品">
          <el-select v-model="query.productName" clearable style="width: 180px" placeholder="全部">
            <el-option v-for="product in productOptions" :key="product" :label="product" :value="product" />
          </el-select>
        </el-form-item>
        <el-form-item label="组别">
          <el-select v-model="query.groupName" clearable style="width: 160px" placeholder="全部">
            <el-option v-for="group in filteredGroupOptions" :key="group" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="巡检人">
          <el-select v-model="query.inspector" clearable style="width: 140px" placeholder="全部">
            <el-option v-for="person in inspectorOptions" :key="person" :label="person" :value="person" />
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
        <el-table-column prop="productName" label="产品" min-width="180" show-overflow-tooltip />
        <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip />
        <el-table-column prop="groupName" label="组别" width="120" show-overflow-tooltip />
        <el-table-column prop="inspector" label="巡检人" width="100" show-overflow-tooltip />
        <el-table-column prop="inspectionType" label="方式" width="90" />
        <el-table-column prop="planDate" label="计划日期" width="120">
          <template #default="scope">{{ formatDate(scope.row.planDate) }}</template>
        </el-table-column>
        <el-table-column prop="actualDate" label="实际日期" width="120">
          <template #default="scope">{{ scope.row.actualDate ? formatDate(scope.row.actualDate) : '-' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="scope">
            <el-tag :type="statusTag(scope.row.status)">{{ scope.row.status }}</el-tag>
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
import { fetchInspections, fetchInspectionSummary } from '../../api/modules/inspection'
import type { InspectionPlanItem, InspectionSummary } from '../../types/inspection'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PERSON_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

const loading = ref(false)
const total = ref(0)
const tableData = ref<InspectionPlanItem[]>([])
const summary = ref<InspectionSummary>({
  plannedCount: 0,
  inProgressCount: 0,
  completedCount: 0,
  cancelledCount: 0,
  thisMonthCount: 0,
  total: 0,
})

const query = reactive({
  status: '',
  province: '',
  productName: '',
  groupName: '',
  inspector: '',
  page: 1,
  size: 10,
})

const provinceOptions = ref<string[]>([...PROVINCE_OPTIONS])
const productOptions = ref<string[]>([])
const groupOptionsByProvince = ref<Record<string, string[]>>({})
const inspectorOptions = ref<string[]>([...PERSON_OPTIONS])

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
    const res = await fetchInspections({ page: 1, size: 200 })
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

    const products = Array.from(new Set(items.map((item) => item.productName).filter(Boolean)))
    if (products.length > 0) {
      productOptions.value = products
    }

    const inspectors = Array.from(new Set(items.map((item) => item.inspector).filter(Boolean)))
    if (inspectors.length > 0) {
      inspectorOptions.value = inspectors
    }
  } catch {
  }
}
const { runInitialLoad } = useResilientLoad()

const formatDate = (value: string) => value.slice(0, 10)

const statusTag = (status: string) => {
  if (status === '已完成') return 'success'
  if (status === '执行中') return 'warning'
  if (status === '已取消') return 'danger'
  return 'info'
}

const loadSummary = async () => {
  try {
    const res = await fetchInspectionSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载巡检汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchInspections(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载巡检列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.status = ''
  query.province = ''
  query.productName = ''
  query.groupName = ''
  query.inspector = ''
  query.page = 1
  query.size = 10
  loadData()
}

const refreshLinkedData = async () => {
  await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
}

useLinkedRealtimeRefresh({
  refresh: refreshLinkedData,
  scope: 'inspection',
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
