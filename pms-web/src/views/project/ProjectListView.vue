<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">项目台账</h2>
        <div class="page-subtitle">按医院、产品、组别、级别与金额统一管理项目</div>
      </div>
    </div>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <el-form-item label="医院名称">
          <el-input v-model="query.hospitalName" placeholder="请输入医院名称" clearable />
        </el-form-item>
        <el-form-item label="产品">
          <el-select v-model="query.productName" placeholder="全部" clearable style="width: 180px">
            <el-option v-for="product in productOptions" :key="product" :label="product" :value="product" />
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
        <el-form-item label="级别">
          <el-select v-model="query.hospitalLevel" placeholder="全部" clearable style="width: 120px">
            <el-option v-for="level in levelOptions" :key="level" :label="level" :value="level" />
          </el-select>
        </el-form-item>
        <el-form-item label="合同状态">
          <el-select v-model="query.contractStatus" placeholder="全部" clearable style="width: 180px">
            <el-option label="合同已签署" value="合同已签署" />
            <el-option label="超期未签署" value="超期未签署" />
            <el-option label="免费维护期" value="免费维护期" />
            <el-option label="维护超期未签署" value="维护超期未签署" />
            <el-option label="维护合同已签署" value="维护合同已签署" />
            <el-option label="停止维护" value="停止维护" />
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
        <el-table-column prop="hospitalName" label="医院名称" min-width="220" show-overflow-tooltip />
        <el-table-column prop="productName" label="产品" min-width="180" show-overflow-tooltip />
        <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip />
        <el-table-column prop="groupName" label="组别" width="120" show-overflow-tooltip />
        <el-table-column prop="hospitalLevel" label="级别" width="90" />
        <el-table-column prop="contractStatus" label="合同状态" min-width="160" show-overflow-tooltip>
          <template #default="scope">
            <el-tag :type="statusType(scope.row.contractStatus)">{{ scope.row.contractStatus }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="maintenanceAmount" label="维护金额(万)" width="130" align="right" />
        <el-table-column prop="overdueDays" label="超期天数" width="110" align="right">
          <template #default="scope">
            <span :class="{ danger: scope.row.overdueDays > 90 }">{{ scope.row.overdueDays }}</span>
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
import { fetchProjectList } from '../../api/modules/project'
import type { ProjectItem } from '../../types/project'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

const loading = ref(false)
const total = ref(0)
const tableData = ref<ProjectItem[]>([])

const query = reactive({
  hospitalName: '',
  productName: '',
  province: '',
  groupName: '',
  hospitalLevel: '',
  contractStatus: '',
  page: 1,
  size: 10,
})

const provinceOptions = ref<string[]>([...PROVINCE_OPTIONS])
const groupOptionsByProvince = ref<Record<string, string[]>>({})
const productOptions = ref<string[]>([])
const levelOptions = ref<string[]>([])

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
    const res = await fetchProjectList({ page: 1, size: 200 })
    const items = res.data.items

    if (!items.length) {
      return
    }

    const provinces = Array.from(new Set(items.map((item) => item.province).filter(Boolean)))
    if (provinces.length > 0) {
      provinceOptions.value = provinces
    }

    const products = Array.from(new Set(items.map((item) => item.productName).filter(Boolean)))
    if (products.length > 0) {
      productOptions.value = products
    }

    const levels = Array.from(new Set(items.map((item) => item.hospitalLevel).filter(Boolean)))
    if (levels.length > 0) {
      levelOptions.value = levels
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
    if (!productOptions.value.length) {
      productOptions.value = ['住院电子病历V6', '临床路径V6', 'CDSS', '病案归档', 'AI内涵质控']
    }
    if (!levelOptions.value.length) {
      levelOptions.value = ['三级', '二级', '一级', '未评级']
    }
  }
}

const statusType = (status: string) => {
  if (status.includes('超期')) return 'danger'
  if (status.includes('签署')) return 'success'
  if (status.includes('免费')) return 'info'
  return 'warning'
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchProjectList(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载项目列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.hospitalName = ''
  query.productName = ''
  query.province = ''
  query.groupName = ''
  query.hospitalLevel = ''
  query.contractStatus = ''
  query.page = 1
  query.size = 10
  loadData()
}

const refreshLinkedData = async () => {
  await Promise.allSettled([loadFilterOptions(), loadData()])
}

useLinkedRealtimeRefresh({
  refresh: refreshLinkedData,
  scope: 'project',
  intervalMs: 10000,
})

onMounted(async () => {
  await refreshLinkedData()
})
</script>

<style scoped>
.danger {
  color: var(--el-color-danger);
  font-weight: 700;
}
</style>
