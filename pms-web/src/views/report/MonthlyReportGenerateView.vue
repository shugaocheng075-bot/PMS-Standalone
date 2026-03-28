<template>
  <div ref="pageRef" class="page-shell report-page">
    <div class="page-head">
      <div>
        <h2 class="page-title">月度报告生成</h2>
        <div class="page-subtitle">先核对主管、驻场/远程、负责医院和产品，再生成月报</div>
      </div>
      <div class="head-actions no-print">
        <el-button size="small" @click="onPrint">打印</el-button>
      </div>
    </div>

    <AppTableCard>
      <el-form :model="form" label-width="100px" class="generate-form">
        <el-form-item label="报告月份" required>
          <el-date-picker
            v-model="form.reportMonth"
            type="month"
            placeholder="选择月份"
            format="YYYY-MM"
            value-format="YYYY-MM"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="组别" required>
          <el-select
            v-model="form.groupName"
            filterable
            clearable
            placeholder="请选择运维主管组别"
            style="width: 100%"
            :loading="groupOptionsLoading"
          >
            <el-option
              v-for="item in groupOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item>
          <div class="form-actions">
            <el-button :disabled="!canPreview" :loading="sourceLoading" @click="loadSourcePreview()">
              刷新来源预览
            </el-button>
            <el-button type="primary" :loading="generating" @click="doGenerate" :disabled="!canGenerate">
              生成月度报告
            </el-button>
            <el-button :disabled="!canGenerate" @click="doExport">
              导出月报CSV
            </el-button>
          </div>
        </el-form-item>
      </el-form>
    </AppTableCard>

    <AppTableCard class="source-card" v-loading="sourceLoading">
      <template #header>
        <div class="panel-head">
          <span class="panel-title">数据来源预览</span>
          <el-tag v-if="sourcePreview?.supervisorName" type="primary">主管：{{ sourcePreview.supervisorName }}</el-tag>
        </div>
      </template>

      <div v-if="sourcePreview" class="source-content">
        <div class="source-stats">
          <div v-for="item in sourceStats" :key="item.label" class="source-stat-item">
            <span class="source-stat-label">{{ item.label }}</span>
            <span class="source-stat-value">{{ item.value }}</span>
          </div>
        </div>

        <el-alert
          v-if="previewWarnings.length > 0"
          type="warning"
          :closable="false"
          show-icon
          class="warning-alert"
        >
          <template #title>口径提醒</template>
          <div class="warning-lines">
            <div v-for="item in previewWarnings" :key="item">{{ item }}</div>
          </div>
        </el-alert>

        <el-descriptions :column="2" border size="small" class="source-descriptions">
          <el-descriptions-item label="团队人数口径">
            以人员台账所属组别为准；驻场按人员台账的“是否驻场”判断。
          </el-descriptions-item>
          <el-descriptions-item label="主管归属口径">
            以权限配置中的系统角色和上级主管为准，不再依赖前端猜测。
          </el-descriptions-item>
          <el-descriptions-item label="医院/产品口径">
            以项目台账中运维负责人和人员1-5匹配人员姓名，汇总负责医院与产品。
          </el-descriptions-item>
          <el-descriptions-item label="人均口径">
            同时展示全员人均和除驻场人均，除驻场会扣减驻场人员负责的医院、产品与人数。
          </el-descriptions-item>
        </el-descriptions>

        <div class="jump-actions">
          <el-button plain type="primary" @click="goToPersonnelPage()">去人员权限页</el-button>
          <el-button plain type="primary" @click="goToProjectPage()">去项目台账</el-button>
          <el-button plain type="primary" @click="goToHandoverPage()">去交接管理</el-button>
          <el-button plain type="primary" @click="goToInspectionPage()">去巡检计划</el-button>
          <el-button plain type="primary" @click="goToAnnualReportPage()">去年度报告</el-button>
          <el-button plain type="primary" @click="goToMajorDemandPage()">去重大需求</el-button>
          <el-button plain @click="goToWorkHoursPage('病假')">登记病假</el-button>
          <el-button plain @click="goToWorkHoursPage('事假')">登记事假</el-button>
          <el-button plain @click="goToWorkHoursPage('其他特殊')">登记其他特殊</el-button>
          <el-button plain @click="goToRepairPage()">去报修记录</el-button>
        </div>

        <div class="metric-grid">
          <div class="metric-card accent-blue">
            <div class="metric-title">全员人均</div>
            <div class="metric-line">人数 {{ sourcePreview.perCapitaMetrics.allPersonnelAverage.headcount }}</div>
            <div class="metric-line">客户 {{ sourcePreview.perCapitaMetrics.allPersonnelAverage.customerCount }}</div>
            <div class="metric-line">产品 {{ sourcePreview.perCapitaMetrics.allPersonnelAverage.productCount }}</div>
            <div class="metric-highlight">人均客户 {{ sourcePreview.perCapitaMetrics.allPersonnelAverage.customerPerPerson.toFixed(1) }}</div>
            <div class="metric-highlight">人均产品 {{ sourcePreview.perCapitaMetrics.allPersonnelAverage.productPerPerson.toFixed(1) }}</div>
          </div>
          <div class="metric-card accent-amber">
            <div class="metric-title">除驻场人均</div>
            <div class="metric-line">人数 {{ sourcePreview.perCapitaMetrics.excludeOnsiteAverage.headcount }}</div>
            <div class="metric-line">客户 {{ sourcePreview.perCapitaMetrics.excludeOnsiteAverage.customerCount }}</div>
            <div class="metric-line">产品 {{ sourcePreview.perCapitaMetrics.excludeOnsiteAverage.productCount }}</div>
            <div class="metric-highlight">人均客户 {{ sourcePreview.perCapitaMetrics.excludeOnsiteAverage.customerPerPerson.toFixed(1) }}</div>
            <div class="metric-highlight">人均产品 {{ sourcePreview.perCapitaMetrics.excludeOnsiteAverage.productPerPerson.toFixed(1) }}</div>
          </div>
        </div>

        <div class="section-block">
          <div class="section-head">
            <h3>人员来源明细</h3>
            <el-tag type="info">{{ sourcePreview.personnelItems.length }} 人</el-tag>
          </div>
          <el-table :data="sourcePreview.personnelItems" border stripe max-height="420" scrollbar-always-on>
            <el-table-column prop="name" label="人员" width="100" fixed="left" />
            <el-table-column label="服务方式" width="100">
              <template #default="scope">
                <el-tag :type="scope.row.isOnsite ? 'success' : 'info'">{{ scope.row.serviceMode }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="department" label="部门/区域" width="120" show-overflow-tooltip />
            <el-table-column prop="supervisorName" label="主管" width="110" show-overflow-tooltip />
            <el-table-column prop="responsibilityHospitalCount" label="医院数" width="80" align="right" />
            <el-table-column prop="responsibilityProductCount" label="产品数" width="80" align="right" />
            <el-table-column label="负责医院" min-width="260" show-overflow-tooltip>
              <template #default="scope">
                <span :title="fullList(scope.row.hospitalNames)">{{ shortList(scope.row.hospitalNames) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="负责产品" min-width="320" show-overflow-tooltip>
              <template #default="scope">
                <span :title="fullList(scope.row.productNames)">{{ shortList(scope.row.productNames) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="匹配状态" width="110">
              <template #default="scope">
                <el-tag :type="scope.row.matchingStatus === '已匹配' ? 'success' : 'warning'">{{ scope.row.matchingStatus }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" fixed="right">
              <template #default="scope">
                <el-space>
                  <el-button link type="primary" @click="goToPersonnelPage(scope.row.name, scope.row.personnelId)">维护人员</el-button>
                  <el-button link @click="goToWorkHoursPage(undefined, scope.row.name)">查看工时</el-button>
                  <el-button link @click="goToAnnualReportPage(scope.row.name)">年报</el-button>
                </el-space>
              </template>
            </el-table-column>
          </el-table>
        </div>

        <div v-if="sourcePreview.onsiteDeductionItems.length > 0" class="section-block">
          <div class="section-head">
            <h3>驻场扣减明细</h3>
            <el-tag type="warning">{{ sourcePreview.onsiteDeductionItems.length }} 人</el-tag>
          </div>
          <el-table :data="sourcePreview.onsiteDeductionItems" border stripe max-height="280" scrollbar-always-on>
            <el-table-column prop="name" label="驻场人员" width="110" />
            <el-table-column prop="deductedHospitalCount" label="扣减医院数" width="100" align="right" />
            <el-table-column prop="deductedProductCount" label="扣减产品数" width="100" align="right" />
            <el-table-column label="扣减医院" min-width="260" show-overflow-tooltip>
              <template #default="scope">
                <span :title="fullList(scope.row.hospitalNames)">{{ shortList(scope.row.hospitalNames) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="扣减产品" min-width="320" show-overflow-tooltip>
              <template #default="scope">
                <span :title="fullList(scope.row.productNames)">{{ shortList(scope.row.productNames) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="160" fixed="right">
              <template #default="scope">
                <el-space>
                  <el-button link type="primary" @click="goToPersonnelPage(scope.row.name)">人员页</el-button>
                  <el-button link @click="goToProjectPage({ maintenancePersonName: scope.row.name, hospitalName: singleValue(scope.row.hospitalNames) })">项目页</el-button>
                  <el-button link @click="goToHandoverPage(singleValue(scope.row.hospitalNames))">交接</el-button>
                  <el-button link @click="goToInspectionPage(singleValue(scope.row.hospitalNames), singleValue(scope.row.productNames))">巡检</el-button>
                </el-space>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>

      <el-empty v-else description="选择月份和组别后，可在此核对来源数据" />
    </AppTableCard>

    <AppTableCard v-if="result" class="result-card">
      <template #header>
        <div class="panel-head">
          <span class="panel-title">生成结果</span>
          <el-tag type="success">已生成</el-tag>
        </div>
      </template>

      <el-descriptions :column="2" border size="small">
        <el-descriptions-item label="ID">{{ result.id }}</el-descriptions-item>
        <el-descriptions-item label="月份">{{ result.reportMonth }}</el-descriptions-item>
        <el-descriptions-item label="组别">{{ result.groupName }}</el-descriptions-item>
        <el-descriptions-item label="团队总人数">{{ result.teamTotal }}</el-descriptions-item>
        <el-descriptions-item label="周报提交率">{{ (result.weeklyReportRate * 100).toFixed(1) }}%</el-descriptions-item>
        <el-descriptions-item label="月报提交率">{{ (result.monthlyReportRate * 100).toFixed(1) }}%</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ result.createdAt }}</el-descriptions-item>
      </el-descriptions>

      <div class="json-sections" v-if="jsonSections.length > 0">
        <h4 class="json-title">报告数据段</h4>
        <el-collapse>
          <el-collapse-item v-for="sec in jsonSections" :key="sec.label" :title="sec.label" :name="sec.label">
            <pre class="json-pre">{{ sec.content }}</pre>
          </el-collapse-item>
        </el-collapse>
      </div>
    </AppTableCard>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRouter } from 'vue-router'
import { usePrint } from '../../composables/usePrint'
import { fetchAccessActors } from '../../api/modules/access'
import {
  exportMonthlyReport,
  fetchMonthlyReportSourcePreview,
  generateMonthlyReport,
} from '../../api/modules/report'
import { fetchPersonnel } from '../../api/modules/personnel'
import type { MonthlyReportItem } from '../../types/monthly-report'
import type { MonthlyReportSourcePreview } from '../../types/report'
import { getErrorMessage } from '../../utils/error'
import AppTableCard from '../../components/AppTableCard.vue'

const { printArea } = usePrint()
const pageRef = ref<HTMLElement | null>(null)
const generating = ref(false)
const groupOptionsLoading = ref(false)
const sourceLoading = ref(false)
const router = useRouter()
const groupOptions = ref<Array<{ label: string; value: string }>>([])
const form = reactive({
  reportMonth: '',
  groupName: '',
})
const result = ref<MonthlyReportItem | null>(null)
const sourcePreview = ref<MonthlyReportSourcePreview | null>(null)
const lastGeneratedKey = ref('')

const canGenerate = computed(() => Boolean(form.reportMonth && form.groupName))
const canPreview = computed(() => Boolean(form.reportMonth && form.groupName))

const sourceStats = computed(() => {
  if (!sourcePreview.value) return []

  return [
    { label: '主管', value: sourcePreview.value.supervisorName || '未识别' },
    { label: '团队人数', value: `${sourcePreview.value.teamSummary.totalHeadcount} 人` },
    { label: '驻场人数', value: `${sourcePreview.value.teamSummary.onsiteCount} 人` },
    { label: '远程/标服', value: `${sourcePreview.value.teamSummary.remoteCount} 人` },
    { label: '客户数', value: `${sourcePreview.value.projectSummary.totalCustomerCount} 家` },
    { label: '产品数', value: `${sourcePreview.value.projectSummary.totalProductCount} 个` },
    { label: '除驻场扣减医院', value: `${sourcePreview.value.projectSummary.onsiteDeductedCustomerCount} 家` },
    { label: '除驻场扣减产品', value: `${sourcePreview.value.projectSummary.onsiteDeductedProductCount} 个` },
  ]
})

const previewWarnings = computed(() => sourcePreview.value?.warnings ?? [])

const jsonSections = computed(() => {
  if (!result.value) return []
  const sections: { label: string; content: string }[] = []
  const tryParse = (label: string, json: string | undefined | null) => {
    if (!json) return
    try {
      sections.push({ label, content: JSON.stringify(JSON.parse(json), null, 2) })
    } catch {
      sections.push({ label, content: json })
    }
  }
  tryParse('驻场人员', result.value.teamOnsiteJson)
  tryParse('团队结构', result.value.teamSummaryJson)
  tryParse('项目情况', result.value.projectOverviewJson)
  tryParse('人均核算', result.value.perCapitaMetricsJson)
  tryParse('交接事项', result.value.handoverItemsJson)
  tryParse('重大需求验收', result.value.majorDemandAcceptanceJson)
  tryParse('巡检记录', result.value.inspectionRecordsJson)
  tryParse('年度服务报告', result.value.annualServiceReportsJson)
  tryParse('事件/报修', result.value.incidentsJson)
  tryParse('下月巡检计划', result.value.nextMonthInspectionPlanJson)
  tryParse('下月年报计划', result.value.nextMonthAnnualReportPlanJson)
  tryParse('下月其他计划', result.value.nextMonthOtherPlanJson)
  return sections
})

const shortList = (items: string[]) => {
  if (!items.length) return '-'
  if (items.length <= 3) return items.join('、')
  return `${items.slice(0, 3).join('、')} 等 ${items.length} 项`
}

const fullList = (items: string[]) => (items.length ? items.join('\n') : '-')

const singleValue = (items: string[]) => (items.length === 1 ? items[0] : undefined)

const goToPersonnelPage = (name?: string, id?: number) => {
  void router.push({
    path: '/permission/manage',
    query: {
      ...(id ? { action: 'edit', id: String(id) } : {}),
      ...(form.groupName ? { groupName: form.groupName } : {}),
      ...(name ? { name } : {}),
    },
  })
}

const goToProjectPage = (options?: { maintenancePersonName?: string; hospitalName?: string; productName?: string; action?: 'edit' }) => {
  void router.push({
    path: '/project/list',
    query: {
      ...(form.groupName ? { groupName: form.groupName } : {}),
      ...(options?.maintenancePersonName ? { maintenancePersonName: options.maintenancePersonName } : {}),
      ...(options?.hospitalName ? { hospitalName: options.hospitalName } : {}),
      ...(options?.productName ? { productName: options.productName } : {}),
      ...(options?.action ? { action: options.action } : {}),
    },
  })
}

const goToWorkHoursPage = (workType?: string, personnelName?: string, hospitalName?: string, action?: 'create') => {
  void router.push({
    path: '/workhours/list',
    query: {
      ...(action ? { action } : {}),
      ...(workType ? { workType } : {}),
      ...(personnelName ? { personnelName } : {}),
      ...(hospitalName ? { hospitalName } : {}),
    },
  })
}

const goToRepairPage = () => {
  void router.push({
    path: '/repair/list',
    query: {
      status: '处理中',
    },
  })
}

const goToHandoverPage = (hospitalName?: string) => {
  void router.push({
    path: '/handover/list',
    query: {
      ...(form.groupName ? { fromGroup: form.groupName } : {}),
      ...(hospitalName ? { hospitalName } : {}),
      action: 'detail',
    },
  })
}

const goToInspectionPage = (hospitalName?: string, productName?: string) => {
  void router.push({
    path: '/inspection/plan',
    query: {
      ...(form.groupName ? { groupName: form.groupName } : {}),
      ...(hospitalName ? { hospitalName } : {}),
      ...(productName ? { productName } : {}),
      action: 'edit',
    },
  })
}

const goToAnnualReportPage = (servicePerson?: string, hospitalName?: string) => {
  const reportYear = form.reportMonth ? String(new Date(`${form.reportMonth}-01`).getFullYear()) : undefined
  void router.push({
    path: '/annual-report/list',
    query: {
      ...(form.groupName ? { groupName: form.groupName } : {}),
      ...(servicePerson ? { servicePerson } : {}),
      ...(hospitalName ? { hospitalName } : {}),
      ...(reportYear ? { reportYear } : {}),
      action: 'edit',
    },
  })
}

const goToMajorDemandPage = () => {
  void router.push({
    path: '/major-demand/list',
    query: {
      status: '处理中',
      ...(form.groupName ? { keyword: form.groupName } : {}),
    },
  })
}

const loadGroupOptions = async () => {
  groupOptionsLoading.value = true
  try {
    const [personnelRes, actorRes] = await Promise.all([
      fetchPersonnel({ page: 1, size: 5000 }),
      fetchAccessActors(),
    ])

    const items = personnelRes.data.items ?? []
    const actorById = new Map(actorRes.data.map((item) => [item.personnelId, item]))
    const supervisorMap = new Map<string, string>()
    const managerMap = new Map<string, string>()
    const fallbackGroups = new Set<string>()

    for (const item of items) {
      const groupName = item.groupName?.trim()
      if (!groupName) continue
      fallbackGroups.add(groupName)

      const actor = actorById.get(item.id)
      const normalizedRole = (actor?.systemRole ?? '').toLowerCase()
      if (normalizedRole === 'supervisor' && !supervisorMap.has(groupName)) {
        supervisorMap.set(groupName, item.name?.trim() || '')
      }

      if (normalizedRole === 'manager' && !managerMap.has(groupName)) {
        managerMap.set(groupName, item.name?.trim() || '')
      }
    }

    const options = (supervisorMap.size > 0
      ? Array.from(supervisorMap.entries()).map(([groupName, ownerName]) => ({
          value: groupName,
          label: ownerName ? `${groupName}（运维主管：${ownerName}）` : groupName,
        }))
      : managerMap.size > 0
        ? Array.from(managerMap.entries()).map(([groupName, ownerName]) => ({
            value: groupName,
            label: ownerName ? `${groupName}（经理：${ownerName}）` : groupName,
          }))
        : Array.from(fallbackGroups.values()).map((groupName) => ({
          value: groupName,
          label: groupName,
        })))
      .sort((a, b) => a.value.localeCompare(b.value, 'zh-CN'))

    groupOptions.value = options
  } catch (error) {
    groupOptions.value = []
    ElMessage.error(getErrorMessage(error, '加载运维主管组别失败'))
  } finally {
    groupOptionsLoading.value = false
  }
}

const loadSourcePreview = async (showError = true) => {
  if (!canPreview.value) {
    sourcePreview.value = null
    return
  }

  sourceLoading.value = true
  try {
    const res = await fetchMonthlyReportSourcePreview({
      reportMonth: form.reportMonth,
      groupName: form.groupName,
    })
    sourcePreview.value = res.data
  } catch (error) {
    sourcePreview.value = null
    if (showError) {
      ElMessage.error(getErrorMessage(error, '加载数据来源预览失败'))
    }
  } finally {
    sourceLoading.value = false
  }
}

const doGenerate = async (): Promise<boolean> => {
  if (!canGenerate.value) return false

  generating.value = true
  try {
    if (!sourcePreview.value) {
      await loadSourcePreview(false)
    }

    const res = await generateMonthlyReport({
      reportMonth: form.reportMonth,
      groupName: form.groupName,
    })
    result.value = res.data
    lastGeneratedKey.value = `${form.reportMonth}::${form.groupName}`
    ElMessage.success('月度报告生成成功')
    return true
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '生成月度报告失败'))
    return false
  } finally {
    generating.value = false
  }
}

const doExport = async () => {
  if (!canGenerate.value) return
  try {
    const currentKey = `${form.reportMonth}::${form.groupName}`
    if (lastGeneratedKey.value !== currentKey) {
      const ok = await doGenerate()
      if (!ok) return
    }
    const blob = await exportMonthlyReport({
      reportMonth: form.reportMonth,
      groupName: form.groupName,
    })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `月报_${form.reportMonth}_${form.groupName}.csv`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('月报导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出月报失败'))
  }
}

watch(
  () => [form.reportMonth, form.groupName] as const,
  ([reportMonth, groupName]) => {
    if (!reportMonth || !groupName) {
      sourcePreview.value = null
      return
    }

    void loadSourcePreview(false)
  },
)

const onPrint = () => printArea(pageRef.value, '月度报告')

onMounted(() => {
  void loadGroupOptions()
})
</script>

<style scoped>
.report-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.generate-form {
  max-width: 720px;
}

.form-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.panel-title {
  font-weight: 700;
  color: #1f3f70;
}

.source-content {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.source-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(170px, 1fr));
  gap: 10px;
}

.source-stat-item {
  background: linear-gradient(135deg, #f8fbff, #eef5ff);
  border: 1px solid #dbe7f8;
  border-radius: 10px;
  padding: 12px 14px;
}

.source-stat-label {
  display: block;
  color: #6b7280;
  font-size: 12px;
  margin-bottom: 6px;
}

.source-stat-value {
  color: #1f2937;
  font-size: 18px;
  font-weight: 700;
}

.warning-alert {
  margin-top: 2px;
}

.warning-lines {
  display: flex;
  flex-direction: column;
  gap: 4px;
  white-space: pre-wrap;
}

.source-descriptions {
  margin-bottom: 2px;
}

.jump-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.metric-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 12px;
}

.metric-card {
  border-radius: 12px;
  padding: 16px;
  border: 1px solid #e5e7eb;
  background: #fff;
}

.accent-blue {
  background: linear-gradient(180deg, #f8fbff 0%, #eef6ff 100%);
  border-color: #cfe2ff;
}

.accent-amber {
  background: linear-gradient(180deg, #fffaf0 0%, #fff3dd 100%);
  border-color: #f6d28b;
}

.metric-title {
  font-size: 16px;
  font-weight: 700;
  color: #1f3f70;
  margin-bottom: 10px;
}

.metric-line {
  color: #4b5563;
  line-height: 1.8;
}

.metric-highlight {
  color: #111827;
  font-weight: 700;
  line-height: 1.8;
}

.section-block {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.section-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.section-head h3 {
  margin: 0;
  font-size: 15px;
  color: #1f2937;
}

.result-card {
  margin-top: 4px;
}

.json-title {
  margin: 16px 0 8px;
}

.json-pre {
  background: #f5f7fa;
  padding: 12px;
  border-radius: 6px;
  font-size: 12px;
  line-height: 1.5;
  max-height: 320px;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-all;
}

@media (max-width: 768px) {
  .form-actions {
    width: 100%;
  }

  .form-actions :deep(.el-button) {
    flex: 1 1 auto;
  }
}
</style>
