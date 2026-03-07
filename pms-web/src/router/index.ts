import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '../views/dashboard/DashboardView.vue'
import AlertCenterView from '../views/alert/AlertCenterView.vue'
import ProjectListView from '../views/project/ProjectListView.vue'
import ContractAlertView from '../views/contract/ContractAlertView.vue'
import HandoverListView from '../views/handover/HandoverListView.vue'
import InspectionPlanView from '../views/inspection/InspectionPlanView.vue'
import AnnualReportView from '../views/annual-report/AnnualReportView.vue'
import HospitalListView from '../views/hospital/HospitalListView.vue'
import PersonnelListView from '../views/personnel/PersonnelListView.vue'
import ProductListView from '../views/product/ProductListView.vue'
import DataMaintenanceView from '../views/maintenance/DataMaintenanceView.vue'
import MajorDemandView from '../views/major-demand/MajorDemandView.vue'
import RepairRecordView from '../views/repair/RepairRecordView.vue'
import WorkHoursView from '../views/workhours/WorkHoursView.vue'
import MonthlyReportView from '../views/monthly-report/MonthlyReportView.vue'
import MS010001013001View from '../views/complex-model/MS010001013001View.vue'
import LoginView from '../views/login/LoginView.vue'
import KpiDashboardView from '../views/dashboard/KpiDashboardView.vue'
import WorkHoursReportView from '../views/report/WorkHoursReportView.vue'
import MonthlyReportGenerateView from '../views/report/MonthlyReportGenerateView.vue'
import { clearAuthState, isAuthenticated, resolveRoutePermission } from '../constants/access'
import { useAccessControl } from '../composables/useAccessControl'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/login',
    },
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { title: '登录', public: true },
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: DashboardView,
      meta: { title: '首页', permission: 'dashboard.view' },
    },
    {
      path: '/alert/center',
      name: 'alert-center',
      component: AlertCenterView,
      meta: { title: '统一预警中心', permission: 'alert-center.view' },
    },
    {
      path: '/project/list',
      name: 'project-list',
      component: ProjectListView,
      meta: { title: '项目台账', permission: 'project.view' },
    },
    {
      path: '/contract/alerts',
      name: 'contract-alerts',
      component: ContractAlertView,
      meta: { title: '合同预警', permission: 'contract.view' },
    },
    {
      path: '/handover/list',
      name: 'handover-list',
      component: HandoverListView,
      meta: { title: '交接管理', permission: 'handover.view' },
    },
    {
      path: '/inspection/plan',
      name: 'inspection-plan',
      component: InspectionPlanView,
      meta: { title: '巡检计划', permission: 'inspection.view' },
    },
    {
      path: '/annual-report/list',
      name: 'annual-report-list',
      component: AnnualReportView,
      meta: { title: '年度报告', permission: 'annual-report.view' },
    },
    {
      path: '/hospital/list',
      name: 'hospital-list',
      component: HospitalListView,
      meta: { title: '医院管理', permission: 'hospital.view' },
    },
    {
      path: '/personnel/list',
      name: 'personnel-list',
      redirect: '/permission/manage',
      meta: { title: '权限管理', permission: 'permission.manage' },
    },
    {
      path: '/permission/manage',
      name: 'permission-manage',
      component: PersonnelListView,
      meta: { title: '权限管理', permission: 'permission.manage' },
    },
    {
      path: '/product/list',
      name: 'product-list',
      component: ProductListView,
      meta: { title: '产品管理', permission: 'product.view' },
    },
    {
      path: '/major-demand/list',
      name: 'major-demand-list',
      component: MajorDemandView,
      meta: { title: '重大需求', permission: 'major-demand.view' },
    },
    {
      path: '/repair/list',
      name: 'repair-list',
      component: RepairRecordView,
      meta: { title: '报修记录', permission: 'repair.view' },
    },
    {
      path: '/workhours/list',
      name: 'workhours-list',
      component: WorkHoursView,
      meta: { title: '工时管理', permission: 'workhours.view' },
    },
    {
      path: '/monthly-report/list',
      name: 'monthly-report-list',
      component: MonthlyReportView,
      meta: { title: '月度报告', permission: 'monthly-report.view' },
    },
    {
      path: '/complex-model/ms010001-013-001',
      name: 'complex-model-ms010001-013-001',
      component: MS010001013001View,
      meta: { title: '待办列表', permission: 'project.view' },
    },
    {
      path: '/maintenance/data',
      name: 'maintenance-data',
      component: DataMaintenanceView,
      meta: { title: '数据维护中心', permission: 'maintenance.manage' },
    },
    {
      path: '/dashboard/kpi',
      name: 'dashboard-kpi',
      component: KpiDashboardView,
      meta: { title: 'KPI 概览', permission: 'dashboard.view' },
    },
    {
      path: '/report/workhours',
      name: 'report-workhours',
      component: WorkHoursReportView,
      meta: { title: '工时报表', permission: 'workhours.view' },
    },
    {
      path: '/report/monthly-generate',
      name: 'report-monthly-generate',
      component: MonthlyReportGenerateView,
      meta: { title: '月度报告生成', permission: 'monthly-report.view' },
    },
    {
      path: '/docs/design-spec',
      redirect: '/dashboard',
    },
    {
      path: '/docs/:pathMatch(.*)*',
      redirect: '/dashboard',
    },
  ],
})

router.beforeEach(async (to) => {
  const publicRoute = Boolean(to.meta?.public)
  const authenticated = isAuthenticated()

  if (!authenticated && !publicRoute) {
    return '/login'
  }

  if (authenticated && to.path === '/login') {
    try {
      const access = useAccessControl()
      await access.ensureAccessProfileLoaded()
      return access.getFirstAccessiblePath()
    } catch {
      clearAuthState()
      return true
    }
  }

  if (publicRoute) {
    return true
  }

  const access = useAccessControl()
  try {
    await access.ensureAccessProfileLoaded()
  } catch {
    clearAuthState()
    return '/login'
  }

  const metaPermission = typeof to.meta?.permission === 'string'
    ? to.meta.permission
    : resolveRoutePermission(to.path)

  if (!access.canPermission(metaPermission)) {
    const fallback = access.getFirstAccessiblePath()
    if (to.path === fallback) {
      return false
    }

    return fallback
  }

  return true
})

export default router
