<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">统一预警中心</h2>
        <div class="page-subtitle">聚合合同、交接、巡检待办，支持一键跳转处理</div>
      </div>
    </div>

    <el-row :gutter="12" class="stats-row">
      <el-col :xs="24" :sm="8" :md="4">
        <el-card shadow="never" class="stat-card" @click="filterByLevel('严重')">
          <div class="t">严重</div>
          <div class="v danger">{{ summary.severe }}</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="8" :md="4">
        <el-card shadow="never" class="stat-card" @click="filterByLevel('警告')">
          <div class="t">警告</div>
          <div class="v warning">{{ summary.warning }}</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="8" :md="4">
        <el-card shadow="never" class="stat-card" @click="filterByLevel('提醒')">
          <div class="t">提醒</div>
          <div class="v info">{{ summary.reminder }}</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="8" :md="4">
        <el-card shadow="never" class="stat-card" @click="filterBySource('合同')">
          <div class="t">合同</div>
          <div class="v">{{ summary.contract }}</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="8" :md="4">
        <el-card shadow="never" class="stat-card" @click="filterBySource('交接')">
          <div class="t">交接</div>
          <div class="v">{{ summary.handover }}</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="8" :md="4">
        <el-card shadow="never" class="stat-card" @click="filterBySource('巡检')">
          <div class="t">巡检</div>
          <div class="v">{{ summary.inspection }}</div>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="来源">
          <el-select v-model="query.source" clearable placeholder="全部" style="width: 130px">
            <el-option label="合同" value="合同" />
            <el-option label="交接" value="交接" />
            <el-option label="巡检" value="巡检" />
          </el-select>
        </el-form-item>
        <el-form-item label="级别">
          <el-select v-model="query.level" clearable placeholder="全部" style="width: 130px">
            <el-option label="严重" value="严重" />
            <el-option label="警告" value="警告" />
            <el-option label="提醒" value="提醒" />
          </el-select>
        </el-form-item>
        <el-form-item label="关键字">
          <el-input v-model="query.keyword" clearable placeholder="医院/负责人/内容" style="width: 220px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe max-height="520" scrollbar-always-on empty-text="暂无待办预警" @row-dblclick="onGoto">
        <el-table-column prop="source" label="来源" width="90" />
        <el-table-column prop="level" label="级别" width="90">
          <template #default="scope">
            <el-tag :type="levelTagType(scope.row.level)">{{ scope.row.level }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="hospitalName" label="医院" min-width="180" show-overflow-tooltip />
        <el-table-column prop="title" label="标题" min-width="220" show-overflow-tooltip />
        <el-table-column prop="detail" label="详情" min-width="260" show-overflow-tooltip />
        <el-table-column prop="owner" label="责任人" width="120" show-overflow-tooltip />
        <el-table-column prop="overdueDays" label="超期天数" width="100" align="right" />
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="scope">
            <el-button link type="primary" @click="onGoto(scope.row)">去处理</el-button>
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
import { onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { fetchAlertCenter, type AlertCenterItem } from '../../api/modules/alertCenter'
import { getErrorMessage } from '../../utils/error'

const router = useRouter()
const route = useRoute()

const loading = ref(false)
const tableData = ref<AlertCenterItem[]>([])
const total = ref(0)
const summary = reactive({
  total: 0,
  severe: 0,
  warning: 0,
  reminder: 0,
  contract: 0,
  handover: 0,
  inspection: 0,
})

const query = reactive({
  source: '',
  level: '',
  keyword: '',
  page: 1,
  size: 15,
})

const readRouteQueryValue = (value: unknown): string => {
  if (typeof value === 'string') {
    return value
  }

  if (Array.isArray(value) && typeof value[0] === 'string') {
    return value[0]
  }

  return ''
}

const applyRouteFilters = () => {
  const source = readRouteQueryValue(route.query.source)
  const level = readRouteQueryValue(route.query.level)
  const keyword = readRouteQueryValue(route.query.keyword)
  let applied = false

  if (source) {
    query.source = source
    applied = true
  }

  if (level) {
    query.level = level
    applied = true
  }

  if (keyword) {
    query.keyword = keyword
    applied = true
  }

  if (applied) {
    query.page = 1
  }
}

const levelTagType = (level: string) => {
  if (level === '严重') return 'danger'
  if (level === '警告') return 'warning'
  return 'info'
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchAlertCenter(query)
    tableData.value = res.data.items
    total.value = res.data.total
    summary.total = res.data.summary.total
    summary.severe = res.data.summary.severe
    summary.warning = res.data.summary.warning
    summary.reminder = res.data.summary.reminder
    summary.contract = res.data.summary.contract
    summary.handover = res.data.summary.handover
    summary.inspection = res.data.summary.inspection
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载统一预警中心失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.source = ''
  query.level = ''
  query.keyword = ''
  query.page = 1
  query.size = 15
  loadData()
}

const filterBySource = (source: string) => {
  query.source = source
  query.level = ''
  query.keyword = ''
  query.page = 1
  loadData()
}

const filterByLevel = (level: string) => {
  query.source = ''
  query.level = level
  query.keyword = ''
  query.page = 1
  loadData()
}

const onGoto = (row: AlertCenterItem) => {
  router.push({
    path: row.relatedPath,
    query: row.relatedQuery,
  })
}

onMounted(() => {
  applyRouteFilters()
  loadData()
})

watch(() => route.fullPath, () => {
  applyRouteFilters()
  loadData()
})
</script>

<style scoped>
</style>
