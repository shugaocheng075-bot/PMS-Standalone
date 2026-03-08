<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">医院 360° 视图</h2>
        <div class="page-subtitle">聚合查看单家医院的全部关联数据</div>
      </div>
      <el-select
        v-model="selectedHospital"
        filterable
        placeholder="选择或搜索医院"
        style="width: 320px"
        @change="loadHospitalData"
      >
        <el-option v-for="name in hospitalNames" :key="name" :label="name" :value="name" />
      </el-select>
    </div>

    <template v-if="selectedHospital">
      <!-- 统计卡片 -->
      <el-row :gutter="16" class="stats-row">
        <el-col :span="6">
          <el-card shadow="never" class="stat-card stats-card"><div class="t">关联项目</div><div class="v">{{ projects.length }}</div></el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="never" class="stat-card stats-card"><div class="t">告警条数</div><div class="v warning">{{ alerts.length }}</div></el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="never" class="stat-card stats-card"><div class="t">工时记录</div><div class="v info">{{ workHoursItems.length }}</div></el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="never" class="stat-card stats-card"><div class="t">报修记录</div><div class="v danger">{{ repairs.length }}</div></el-card>
        </el-col>
      </el-row>

      <el-tabs v-model="activeTab">
        <el-tab-pane label="项目台账" name="project">
          <el-table :data="projects" stripe border max-height="400" v-loading="loading" empty-text="暂无数据">
            <el-table-column prop="hospitalName" label="医院" min-width="180" show-overflow-tooltip />
            <el-table-column prop="productName" label="产品" min-width="160" show-overflow-tooltip />
            <el-table-column prop="contractStatus" label="合同状态" width="140" />
            <el-table-column prop="province" label="省份" width="100" />
            <el-table-column prop="groupName" label="组别" width="120" />
            <el-table-column prop="overdueDays" label="超期天数" width="110" align="right" />
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="告警" name="alert">
          <el-table :data="alerts" stripe border max-height="400" v-loading="loading" empty-text="暂无数据">
            <el-table-column prop="source" label="来源" width="90" />
            <el-table-column prop="level" label="等级" width="90" />
            <el-table-column prop="title" label="标题" min-width="220" show-overflow-tooltip />
            <el-table-column prop="owner" label="责任人" width="120" />
            <el-table-column prop="overdueDays" label="超期天数" width="110" align="right" />
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="工时" name="workhours">
          <el-table :data="workHoursItems" stripe border max-height="400" v-loading="loading" empty-text="暂无数据">
            <el-table-column prop="personnelName" label="人员" width="120" />
            <el-table-column prop="productName" label="产品" min-width="160" show-overflow-tooltip />
            <el-table-column prop="workDate" label="日期" width="120" />
            <el-table-column prop="hours" label="工时(h)" width="100" align="right" />
            <el-table-column prop="workType" label="类型" width="100" />
            <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="报修" name="repair">
          <el-table :data="repairs" stripe border max-height="400" v-loading="loading" empty-text="暂无数据">
            <el-table-column prop="productName" label="产品" min-width="160" show-overflow-tooltip />
            <el-table-column prop="description" label="描述" min-width="220" show-overflow-tooltip />
            <el-table-column prop="severity" label="严重等级" width="100" />
            <el-table-column prop="urgency" label="紧急度" width="100" />
            <el-table-column prop="status" label="状态" width="100" />
            <el-table-column prop="reporterName" label="报修人" width="120" />
            <el-table-column prop="reportedAt" label="报修时间" width="160" />
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </template>

    <el-empty v-else description="请从上方选择医院查看详情" />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { fetchDataScope } from '../../api/modules/access'
import { fetchProjectList } from '../../api/modules/project'
import { fetchAlertCenter, type AlertCenterItem } from '../../api/modules/alertCenter'
import { fetchWorkHours } from '../../api/modules/workhours'
import { fetchRepairRecords } from '../../api/modules/repair'
import type { RepairRecordItem } from '../../types/repair'
import type { ProjectItem } from '../../types/project'
import type { WorkHoursItem } from '../../types/workhours'
import { getErrorMessage } from '../../utils/error'

const route = useRoute()
const loading = ref(false)
const selectedHospital = ref('')
const hospitalNames = ref<string[]>([])
const activeTab = ref('project')

const projects = ref<ProjectItem[]>([])
const alerts = ref<AlertCenterItem[]>([])
const workHoursItems = ref<WorkHoursItem[]>([])
const repairs = ref<RepairRecordItem[]>([])

const loadHospitalNames = async () => {
  try {
    const res = await fetchDataScope()
    hospitalNames.value = [...(res.data.accessibleHospitalNames || [])].sort((a, b) => a.localeCompare(b, 'zh-CN'))
  } catch {
    hospitalNames.value = []
  }
}

const loadHospitalData = async () => {
  if (!selectedHospital.value) return
  loading.value = true
  try {
    const [projRes, alertRes, whRes, repairRes] = await Promise.all([
      fetchProjectList({ hospitalName: selectedHospital.value, page: 1, size: 1000 }),
      fetchAlertCenter({ keyword: selectedHospital.value, page: 1, size: 1000 }),
      fetchWorkHours({ hospitalName: selectedHospital.value, page: 1, size: 1000 }),
      fetchRepairRecords({ hospitalName: selectedHospital.value, page: 1, size: 1000 }),
    ])
    projects.value = projRes.data.items
    alerts.value = alertRes.data.items
    workHoursItems.value = whRes.data.items
    repairs.value = repairRes.data.items
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载医院数据失败'))
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadHospitalNames()
  const hospitalFromQuery = route.query.hospitalName
  if (typeof hospitalFromQuery === 'string' && hospitalFromQuery) {
    selectedHospital.value = hospitalFromQuery
    await loadHospitalData()
  }
})
</script>
