<template>
  <div class="page-shell hospital-360-view">
    <div class="page-head hospital-360-head">
      <div class="hospital-360-head__content">
        <div>
          <h2 class="page-title">医院 360° 视图</h2>
          <div class="page-subtitle">聚合查看单家医院的全部关联数据</div>
        </div>
        <div v-if="selectedHospital" class="hospital-360-head__meta">
          <span class="hospital-360-head__meta-label">当前医院</span>
          <strong>{{ selectedHospital }}</strong>
          <span>共 {{ totalLinkedRecords }} 条关联记录</span>
        </div>
      </div>

      <div class="hospital-360-head__actions">
        <span class="hospital-360-head__hint">快速切换医院</span>
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
    </div>

    <template v-if="selectedHospital">
      <div class="hospital-360-overview">
        <div v-for="item in overviewMetrics" :key="item.key" class="hospital-360-overview__item">
          <span class="hospital-360-overview__label">{{ item.label }}</span>
          <strong class="hospital-360-overview__value">{{ item.value }}</strong>
          <span class="hospital-360-overview__note">{{ item.note }}</span>
        </div>
      </div>

      <SummaryMetrics :items="summaryCards" :columns="4" @select="handleSummarySelect" />

      <AppTableCard class="hospital-360-panel">
        <div class="hospital-360-panel__intro">
          <div class="hospital-360-panel__copy">
            <div class="hospital-360-panel__eyebrow">关联数据面板</div>
            <div class="hospital-360-panel__title-row">
              <h3 class="hospital-360-panel__title">{{ activePanel.label }}</h3>
              <el-tag size="small" effect="plain">{{ selectedHospital }}</el-tag>
            </div>
            <p class="hospital-360-panel__description">{{ activePanel.description }}</p>
          </div>

          <div class="hospital-360-panel__aside">
            <div class="hospital-360-panel__stat">
              <span class="hospital-360-panel__stat-value">{{ activePanel.count }}</span>
              <span class="hospital-360-panel__stat-label">条记录</span>
            </div>

            <el-button class="hospital-360-panel__jump" type="primary" plain @click="navigateToPanelPage(activePanel.name)">
              进入{{ activePanel.actionLabel }}
            </el-button>
          </div>
        </div>

        <el-tabs v-model="activeTab" class="hospital-360-tabs">
          <el-tab-pane v-for="panel in tabPanels" :key="panel.name" :label="panel.label" :name="panel.name">
            <template #label>
              <div class="hospital-360-tabs__label">
                <span>{{ panel.label }}</span>
                <em>{{ panel.count }}</em>
              </div>
            </template>

            <div class="hospital-360-tabs__meta">
              <div class="hospital-360-tabs__meta-copy">
                <span>{{ panel.description }}</span>
                <span class="hospital-360-tabs__meta-secondary">{{ panel.secondary }}</span>
              </div>

              <div class="hospital-360-tabs__highlights">
                <div v-for="highlight in panel.highlights" :key="`${panel.name}-${highlight.label}`" class="hospital-360-tabs__highlight">
                  <strong>{{ highlight.value }}</strong>
                  <span>{{ highlight.label }}</span>
                </div>
              </div>
            </div>

            <el-table
              v-if="panel.name === 'project'"
              :data="sortedProjects"
              stripe
              max-height="440"
              scrollbar-always-on
              v-loading="loading"
              :empty-text="panel.emptyText"
            >
              <el-table-column prop="hospitalName" label="医院" min-width="180" show-overflow-tooltip />
              <el-table-column prop="productName" label="产品" min-width="160" show-overflow-tooltip />
              <el-table-column prop="contractStatus" label="合同状态" width="140">
                <template #default="scope">
                  <el-tag :type="contractStatusTagType(scope.row.contractStatus)" effect="plain">{{ scope.row.contractStatus || '-' }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="province" label="省份" width="100" />
              <el-table-column prop="groupName" label="组别" width="120" />
              <el-table-column prop="overdueDays" label="超期天数" width="110" align="right" />
              <el-table-column label="联动" width="110" align="center">
                <template #default="scope">
                  <div class="hospital-360-table__actions">
                    <el-button type="primary" link @click="openProjectListPage(scope.row)">查看页面</el-button>
                  </div>
                </template>
              </el-table-column>
            </el-table>

            <el-table
              v-else-if="panel.name === 'alert'"
              :data="sortedAlerts"
              stripe
              max-height="440"
              scrollbar-always-on
              v-loading="loading"
              :empty-text="panel.emptyText"
            >
              <el-table-column prop="source" label="来源" width="90" />
              <el-table-column prop="level" label="等级" width="90">
                <template #default="scope">
                  <el-tag :type="alertLevelTagType(scope.row.level)" effect="plain">{{ scope.row.level || '-' }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="title" label="标题" min-width="220" show-overflow-tooltip />
              <el-table-column prop="owner" label="责任人" width="120" />
              <el-table-column prop="overdueDays" label="超期天数" width="110" align="right" />
              <el-table-column label="联动" width="110" align="center">
                <template #default="scope">
                  <div class="hospital-360-table__actions">
                    <el-button type="primary" link @click="openAlertCenterPage(scope.row)">查看页面</el-button>
                  </div>
                </template>
              </el-table-column>
            </el-table>

            <el-table
              v-else-if="panel.name === 'workhours'"
              :data="sortedWorkHoursItems"
              stripe
              max-height="440"
              scrollbar-always-on
              v-loading="loading"
              :empty-text="panel.emptyText"
            >
              <el-table-column prop="personnelName" label="人员" width="120" />
              <el-table-column prop="productName" label="产品" min-width="160" show-overflow-tooltip />
              <el-table-column prop="workDate" label="日期" width="120" />
              <el-table-column prop="hours" label="工时(h)" width="100" align="right" />
              <el-table-column prop="workType" label="类型" width="100">
                <template #default="scope">
                  <el-tag type="info" effect="plain">{{ scope.row.workType || '-' }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
              <el-table-column label="联动" width="110" align="center">
                <template #default="scope">
                  <div class="hospital-360-table__actions">
                    <el-button type="primary" link @click="openWorkHoursPage(scope.row)">查看页面</el-button>
                  </div>
                </template>
              </el-table-column>
            </el-table>

            <el-table
              v-else
              :data="sortedRepairs"
              stripe
              max-height="440"
              scrollbar-always-on
              v-loading="loading"
              :empty-text="panel.emptyText"
            >
              <el-table-column prop="productName" label="产品" min-width="160" show-overflow-tooltip />
              <el-table-column prop="description" label="描述" min-width="220" show-overflow-tooltip />
              <el-table-column prop="severity" label="严重等级" width="100">
                <template #default="scope">
                  <el-tag :type="repairSeverityTagType(scope.row.severity)" effect="plain">{{ scope.row.severity || '-' }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="urgency" label="紧急度" width="100" />
              <el-table-column prop="status" label="状态" width="100">
                <template #default="scope">
                  <el-tag :type="repairStatusTagType(scope.row.status)" effect="plain">{{ scope.row.status || '-' }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="reporterName" label="报修人" width="120" />
              <el-table-column prop="reportedAt" label="报修时间" width="160" />
              <el-table-column label="联动" width="110" align="center">
                <template #default="scope">
                  <div class="hospital-360-table__actions">
                    <el-button type="primary" link @click="openRepairPage(scope.row)">打开工单</el-button>
                  </div>
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>
        </el-tabs>
      </AppTableCard>
    </template>

    <AppTableCard v-else class="hospital-360-empty">
      <el-empty description="请从上方选择医院查看详情" />
    </AppTableCard>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter, type RouteLocationRaw } from 'vue-router'
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
import AppTableCard from '../../components/AppTableCard.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'

type Hospital360TabName = 'project' | 'alert' | 'workhours' | 'repair'
type Hospital360Highlight = {
  label: string
  value: string | number
}

type Hospital360Panel = {
  name: Hospital360TabName
  label: string
  actionLabel: string
  count: number
  description: string
  secondary: string
  emptyText: string
  highlights: Hospital360Highlight[]
}

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const selectedHospital = ref('')
const hospitalNames = ref<string[]>([])
const activeTab = ref<Hospital360TabName>('project')

const projects = ref<ProjectItem[]>([])
const alerts = ref<AlertCenterItem[]>([])
const workHoursItems = ref<WorkHoursItem[]>([])
const repairs = ref<RepairRecordItem[]>([])

const defaultPanel: Hospital360Panel = {
  name: 'project',
  label: '项目台账',
  actionLabel: '项目台账',
  count: 0,
  description: '查看当前医院名下的项目、合同、区域与组别归属。',
  secondary: '适合快速确认合同状态与超期风险。',
  emptyText: '暂无项目台账数据',
  highlights: [],
}

const toNumber = (value?: number | string | null) => {
  const numericValue = Number(value)
  return Number.isFinite(numericValue) ? numericValue : 0
}

const parseDateValue = (value?: string) => {
  if (!value) return Number.NEGATIVE_INFINITY
  const parsedValue = Date.parse(value.trim().replace(/\./g, '-'))
  return Number.isNaN(parsedValue) ? Number.NEGATIVE_INFINITY : parsedValue
}

const compareNumberDesc = (left: number, right: number) => {
  if (left === right) return 0
  return right > left ? 1 : -1
}

const compareDateDesc = (left?: string, right?: string) => compareNumberDesc(parseDateValue(left), parseDateValue(right))

const compareTextAsc = (left?: string, right?: string) => (left ?? '').localeCompare(right ?? '', 'zh-CN')

const formatHours = (value: number) => (Number.isInteger(value) ? `${value}` : value.toFixed(1))

const formatShortDate = (value?: string) => {
  const timestamp = parseDateValue(value)
  if (timestamp === Number.NEGATIVE_INFINITY) return '-'
  const date = new Date(timestamp)
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

const contractRiskRank = (value?: string) => {
  if (!value) return 0
  if (value.includes('超期') || value.includes('到期')) return 3
  if (value.includes('待签')) return 2
  if (value.includes('维护') || value.includes('正常')) return 1
  return 0
}

const alertLevelRank = (value?: string) => {
  if (value === '严重') return 3
  if (value === '警告') return 2
  if (value === '提醒') return 1
  return 0
}

const repairSeverityRank = (value?: string) => {
  if (!value) return 0
  if (value.includes('高') || value.includes('严重') || value.includes('紧急')) return 3
  if (value.includes('中')) return 2
  return 1
}

const repairStatusRank = (value?: string) => {
  if (!value) return 1
  if (value.includes('处理中') || value.includes('待处理') || value.includes('跟进')) return 3
  if (value.includes('完成') || value.includes('关闭') || value.includes('已解决')) return 0
  return 2
}

const repairIsOpen = (value?: string) => repairStatusRank(value) > 0

const projectProductCount = computed(() => new Set(projects.value.map((item) => item.productName).filter(Boolean)).size)

const projectGroupCount = computed(() => new Set(projects.value.map((item) => item.groupName).filter(Boolean)).size)

const projectRiskCount = computed(() =>
  projects.value.filter((item) => contractRiskRank(item.contractStatus) >= 2 || toNumber(item.overdueDays) > 0).length,
)

const severeAlertCount = computed(() => alerts.value.filter((item) => alertLevelRank(item.level) === 3).length)

const followUpAlertCount = computed(() => alerts.value.filter((item) => alertLevelRank(item.level) >= 2 || toNumber(item.overdueDays) > 0).length)

const topAlertSource = computed(() => {
  const sourceCounter = new Map<string, number>()
  alerts.value.forEach((item) => {
    if (!item.source) return
    sourceCounter.set(item.source, (sourceCounter.get(item.source) ?? 0) + 1)
  })

  let topSource = '-'
  let topCount = -1
  sourceCounter.forEach((count, source) => {
    if (count > topCount) {
      topSource = source
      topCount = count
    }
  })
  return topSource
})

const totalWorkHours = computed(() => workHoursItems.value.reduce((sum, item) => sum + toNumber(item.hours), 0))

const workHoursPersonnelCount = computed(() => new Set(workHoursItems.value.map((item) => item.personnelName).filter(Boolean)).size)

const openRepairCount = computed(() => repairs.value.filter((item) => repairIsOpen(item.status)).length)

const urgentRepairCount = computed(() =>
  repairs.value.filter(
    (item) =>
      repairSeverityRank(item.severity) >= 3 || `${item.urgency ?? ''}`.includes('高') || `${item.urgency ?? ''}`.includes('紧急'),
  ).length,
)

const sortedProjects = computed(() =>
  [...projects.value].sort(
    (left, right) =>
      compareNumberDesc(toNumber(left.overdueDays), toNumber(right.overdueDays)) ||
      compareNumberDesc(contractRiskRank(left.contractStatus), contractRiskRank(right.contractStatus)) ||
      compareTextAsc(left.productName, right.productName),
  ),
)

const sortedAlerts = computed(() =>
  [...alerts.value].sort(
    (left, right) =>
      compareNumberDesc(alertLevelRank(left.level), alertLevelRank(right.level)) ||
      compareNumberDesc(toNumber(left.overdueDays), toNumber(right.overdueDays)) ||
      compareTextAsc(left.title, right.title),
  ),
)

const sortedWorkHoursItems = computed(() =>
  [...workHoursItems.value].sort(
    (left, right) =>
      compareDateDesc(left.workDate, right.workDate) ||
      compareNumberDesc(toNumber(left.hours), toNumber(right.hours)) ||
      compareTextAsc(left.personnelName, right.personnelName),
  ),
)

const sortedRepairs = computed(() =>
  [...repairs.value].sort(
    (left, right) =>
      compareNumberDesc(repairStatusRank(left.status), repairStatusRank(right.status)) ||
      compareNumberDesc(repairSeverityRank(left.severity), repairSeverityRank(right.severity)) ||
      compareDateDesc(left.reportedAt, right.reportedAt),
  ),
)

const latestWorkDate = computed(() => sortedWorkHoursItems.value[0]?.workDate)

const latestRepairDate = computed(() => sortedRepairs.value[0]?.reportedAt)

const overviewMetrics = computed(() => [
  {
    key: 'project-risk',
    label: '合同风险',
    value: projectRiskCount.value,
    note: projectRiskCount.value ? '超期 / 到期项目需优先核查' : '当前没有超期或待签风险',
  },
  {
    key: 'alert-focus',
    label: '高优告警',
    value: followUpAlertCount.value,
    note: alerts.value.length ? `主要来源 ${topAlertSource.value}` : '当前没有统一预警',
  },
  {
    key: 'workhours-total',
    label: '累计工时',
    value: `${formatHours(totalWorkHours.value)}h`,
    note: workHoursPersonnelCount.value ? `${workHoursPersonnelCount.value} 名人员参与投入` : '当前没有工时填报',
  },
  {
    key: 'repair-open',
    label: '开放工单',
    value: openRepairCount.value,
    note: urgentRepairCount.value ? `其中 ${urgentRepairCount.value} 条高优先级` : '当前没有待处理报修',
  },
])

const buildProjectListRoute = (project?: ProjectItem): RouteLocationRaw => ({
  name: 'project-list',
  query: {
    hospitalName: project?.hospitalName || selectedHospital.value || undefined,
    productName: project?.productName || undefined,
    groupName: project?.groupName || undefined,
  },
})

const buildAlertCenterRoute = (alert?: AlertCenterItem): RouteLocationRaw => ({
  name: 'alert-center',
  query: {
    source: alert?.source || undefined,
    level: alert?.level || undefined,
    keyword: alert?.title || selectedHospital.value || undefined,
  },
})

const buildWorkHoursRoute = (item?: WorkHoursItem): RouteLocationRaw => ({
  name: 'workhours-list',
  query: {
    hospitalName: item?.hospitalName || selectedHospital.value || undefined,
    personnelName: item?.personnelName || undefined,
    workType: item?.workType || undefined,
  },
})

const buildRepairRoute = (repair?: RepairRecordItem): RouteLocationRaw => ({
  name: 'repair-list',
  query: {
    hospitalName: repair?.hospitalName || selectedHospital.value || undefined,
    reporterName: repair?.reporterName || undefined,
    status: repair?.status || undefined,
    action: repair?.id ? 'detail' : undefined,
    id: repair?.id ? String(repair.id) : undefined,
  },
})

const navigateToPanelPage = (panelName: Hospital360TabName) => {
  switch (panelName) {
    case 'project':
      void router.push(buildProjectListRoute())
      break
    case 'alert':
      void router.push(buildAlertCenterRoute())
      break
    case 'workhours':
      void router.push(buildWorkHoursRoute())
      break
    case 'repair':
      void router.push(buildRepairRoute())
      break
    default:
      break
  }
}

const openProjectListPage = (project: ProjectItem) => {
  void router.push(buildProjectListRoute(project))
}

const openAlertCenterPage = (alert: AlertCenterItem) => {
  void router.push(buildAlertCenterRoute(alert))
}

const openWorkHoursPage = (item: WorkHoursItem) => {
  void router.push(buildWorkHoursRoute(item))
}

const openRepairPage = (repair: RepairRecordItem) => {
  void router.push(buildRepairRoute(repair))
}

const tabPanels = computed<Hospital360Panel[]>(() => [
  {
    name: 'project',
    label: '项目台账',
    actionLabel: '项目台账',
    count: projects.value.length,
    description: '查看当前医院名下的项目、合同、区域与组别归属。',
    secondary: '适合快速确认合同状态与超期风险。',
    emptyText: `${selectedHospital.value} 暂无项目台账数据`,
    highlights: [
      { label: '超期 / 到期', value: projectRiskCount.value },
      { label: '覆盖产品', value: projectProductCount.value },
      { label: '关联组别', value: projectGroupCount.value },
    ],
  },
  {
    name: 'alert' as Hospital360TabName,
    label: '告警',
    actionLabel: '预警中心',
    count: alerts.value.length,
    description: '聚合该医院相关的合同、交接、巡检等统一预警。',
    secondary: '便于识别当前最需要处理的风险项。',
    emptyText: `${selectedHospital.value} 暂无关联告警`,
    highlights: [
      { label: '严重告警', value: severeAlertCount.value },
      { label: '需跟进', value: followUpAlertCount.value },
      { label: '主要来源', value: topAlertSource.value },
    ],
  },
  {
    name: 'workhours' as Hospital360TabName,
    label: '工时',
    actionLabel: '工时页面',
    count: workHoursItems.value.length,
    description: '展示该医院对应的运维人员工时填报与工作内容。',
    secondary: '适合核对投入强度与工作类型分布。',
    emptyText: `${selectedHospital.value} 暂无工时记录`,
    highlights: [
      { label: '累计工时', value: `${formatHours(totalWorkHours.value)}h` },
      { label: '投入人员', value: workHoursPersonnelCount.value },
      { label: '最近填报', value: formatShortDate(latestWorkDate.value) },
    ],
  },
  {
    name: 'repair' as Hospital360TabName,
    label: '报修',
    actionLabel: '报修工单',
    count: repairs.value.length,
    description: '查看该医院相关报修工单的等级、状态与处理信息。',
    secondary: '可快速判断当前故障压力与处理进度。',
    emptyText: `${selectedHospital.value} 暂无报修记录`,
    highlights: [
      { label: '待处理 / 处理中', value: openRepairCount.value },
      { label: '高优先级', value: urgentRepairCount.value },
      { label: '最近报修', value: formatShortDate(latestRepairDate.value) },
    ],
  },
])

const activePanel = computed<Hospital360Panel>(() => tabPanels.value.find((panel) => panel.name === activeTab.value) ?? defaultPanel)

const totalLinkedRecords = computed(() => tabPanels.value.reduce((total, panel) => total + panel.count, 0))

const summaryCards = computed(() => [
  {
    key: 'projects',
    title: '关联项目',
    value: projects.value.length,
    context: '项目面板',
    note: '当前医院名下关联的项目数量',
    color: '#3f4f63',
    active: activeTab.value === 'project',
  },
  {
    key: 'alerts',
    title: '告警条数',
    value: alerts.value.length,
    context: '告警面板',
    note: '该医院当前关联的统一预警数量',
    color: '#c7a06c',
    active: activeTab.value === 'alert',
  },
  {
    key: 'workhours',
    title: '工时记录',
    value: workHoursItems.value.length,
    context: '工时面板',
    note: '该医院对应的工时填报记录总数',
    color: '#7c98bc',
    active: activeTab.value === 'workhours',
  },
  {
    key: 'repairs',
    title: '报修记录',
    value: repairs.value.length,
    context: '工单面板',
    note: '该医院相关报修工单与处理记录',
    color: '#c58a87',
    active: activeTab.value === 'repair',
  },
])

const handleSummarySelect = (item: { key?: string | number }) => {
  switch (item.key) {
    case 'projects':
      activeTab.value = 'project'
      break
    case 'alerts':
      activeTab.value = 'alert'
      break
    case 'workhours':
      activeTab.value = 'workhours'
      break
    case 'repairs':
      activeTab.value = 'repair'
      break
    default:
      break
  }
}

const alertLevelTagType = (value?: string) => {
  if (value === '严重') return 'danger'
  if (value === '警告') return 'warning'
  return 'info'
}

const contractStatusTagType = (value?: string) => {
  if (!value) return 'info'
  if (value.includes('超期') || value.includes('到期')) return 'danger'
  if (value.includes('待签')) return 'warning'
  if (value.includes('维护') || value.includes('正常')) return 'success'
  return 'info'
}

const repairSeverityTagType = (value?: string) => {
  if (!value) return 'info'
  if (value.includes('高') || value.includes('严重') || value.includes('紧急')) return 'danger'
  if (value.includes('中')) return 'warning'
  return 'info'
}

const repairStatusTagType = (value?: string) => {
  if (!value) return 'info'
  if (value.includes('完成') || value.includes('关闭') || value.includes('已解决')) return 'success'
  if (value.includes('处理中') || value.includes('待处理') || value.includes('跟进')) return 'warning'
  return 'info'
}

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

<style scoped>
.hospital-360-view {
  display: grid;
}

.hospital-360-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 20px;
}

.hospital-360-head__content {
  display: grid;
  gap: 14px;
}

.hospital-360-head__meta {
  display: inline-flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  border-radius: 999px;
  border: 1px solid #e7ebf0;
  background: linear-gradient(135deg, rgba(248, 250, 252, 0.96), rgba(236, 242, 248, 0.96));
  color: #526071;
  font-size: 13px;
}

.hospital-360-head__meta-label,
.hospital-360-head__hint,
.hospital-360-panel__eyebrow {
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #8b98a7;
}

.hospital-360-head__actions {
  display: grid;
  justify-items: end;
  gap: 10px;
  min-width: 320px;
}

.hospital-360-overview {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
  margin-bottom: 18px;
}

.hospital-360-overview__item {
  display: grid;
  gap: 8px;
  padding: 16px 18px;
  border-radius: 18px;
  border: 1px solid #e7ebf0;
  background: linear-gradient(145deg, rgba(248, 250, 252, 0.98), rgba(239, 244, 248, 0.95));
}

.hospital-360-overview__label {
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #8b98a7;
}

.hospital-360-overview__value {
  font-size: 26px;
  line-height: 1;
  color: #1f2937;
}

.hospital-360-overview__note {
  font-size: 13px;
  line-height: 1.6;
  color: #66768a;
}

.hospital-360-panel__intro {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 18px;
  margin-bottom: 18px;
}

.hospital-360-panel__copy {
  display: grid;
  gap: 10px;
}

.hospital-360-panel__title-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
}

.hospital-360-panel__title {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
  color: #1f2937;
}

.hospital-360-panel__description {
  margin: 0;
  font-size: 13px;
  line-height: 1.7;
  color: #66768a;
}

.hospital-360-panel__stat {
  display: grid;
  justify-items: end;
  gap: 4px;
  min-width: 112px;
  padding: 16px 18px;
  border-radius: 16px;
  border: 1px solid #e7ebf0;
  background: linear-gradient(135deg, rgba(248, 250, 252, 0.98), rgba(239, 244, 248, 0.98));
}

.hospital-360-panel__aside {
  display: grid;
  justify-items: end;
  gap: 12px;
}

.hospital-360-panel__jump {
  min-width: 128px;
}

.hospital-360-panel__stat-value {
  font-size: 30px;
  font-weight: 700;
  line-height: 1;
  color: #1f2937;
}

.hospital-360-panel__stat-label {
  font-size: 12px;
  color: #8b98a7;
}

.hospital-360-tabs__label {
  display: inline-flex;
  align-items: baseline;
  gap: 8px;
}

.hospital-360-tabs__label em {
  font-style: normal;
  font-size: 12px;
  color: #8b98a7;
}

.hospital-360-tabs__meta {
  display: grid;
  grid-template-columns: minmax(0, 1.2fr) minmax(0, 1fr);
  gap: 14px;
  margin-bottom: 14px;
  padding: 12px 14px;
  border-radius: 14px;
  border: 1px solid #eef2f6;
  background: #f8fafc;
  font-size: 13px;
  color: #66768a;
}

.hospital-360-tabs__meta-copy {
  display: grid;
  gap: 6px;
  align-content: start;
}

.hospital-360-tabs__meta-secondary {
  color: #8b98a7;
}

.hospital-360-tabs__highlights {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;
}

.hospital-360-tabs__highlight {
  display: grid;
  gap: 4px;
  padding: 10px 12px;
  border-radius: 12px;
  border: 1px solid #e4eaf0;
  background: rgba(255, 255, 255, 0.8);
}

.hospital-360-tabs__highlight strong {
  font-size: 16px;
  line-height: 1.2;
  color: #1f2937;
}

.hospital-360-tabs__highlight span {
  font-size: 12px;
  color: #8b98a7;
}

.hospital-360-table__actions {
  display: flex;
  justify-content: center;
}

.hospital-360-empty :deep(.el-card__body) {
  padding-block: 48px;
}

@media (max-width: 992px) {
  .hospital-360-overview {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .hospital-360-head,
  .hospital-360-panel__intro,
  .hospital-360-tabs__meta {
    flex-direction: column;
    align-items: stretch;
  }

  .hospital-360-head__actions {
    justify-items: stretch;
    min-width: 0;
  }

  .hospital-360-panel__stat {
    justify-items: start;
  }

  .hospital-360-panel__aside {
    justify-items: start;
  }

  .hospital-360-tabs__highlights {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 768px) {
  .hospital-360-overview,
  .hospital-360-tabs__highlights {
    grid-template-columns: 1fr;
  }

  .hospital-360-head__meta {
    border-radius: 16px;
  }

  .hospital-360-tabs__meta {
    padding: 12px;
  }
}
</style>
