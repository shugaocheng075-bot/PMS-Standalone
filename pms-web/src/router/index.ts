import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '../views/dashboard/DashboardView.vue'
import ProjectListView from '../views/project/ProjectListView.vue'
import ContractAlertView from '../views/contract/ContractAlertView.vue'
import HandoverListView from '../views/handover/HandoverListView.vue'
import InspectionPlanView from '../views/inspection/InspectionPlanView.vue'
import AnnualReportView from '../views/annual-report/AnnualReportView.vue'
import HospitalListView from '../views/hospital/HospitalListView.vue'
import PersonnelListView from '../views/personnel/PersonnelListView.vue'
import ProductListView from '../views/product/ProductListView.vue'
import DesignSpecView from '../views/docs/DesignSpecView.vue'
import DataMaintenanceView from '../views/maintenance/DataMaintenanceView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/dashboard',
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: DashboardView,
      meta: { title: '驾驶舱' },
    },
    {
      path: '/project/list',
      name: 'project-list',
      component: ProjectListView,
      meta: { title: '项目台账' },
    },
    {
      path: '/contract/alerts',
      name: 'contract-alerts',
      component: ContractAlertView,
      meta: { title: '合同预警' },
    },
    {
      path: '/handover/list',
      name: 'handover-list',
      component: HandoverListView,
      meta: { title: '交接管理' },
    },
    {
      path: '/inspection/plan',
      name: 'inspection-plan',
      component: InspectionPlanView,
      meta: { title: '巡检计划' },
    },
    {
      path: '/annual-report/list',
      name: 'annual-report-list',
      component: AnnualReportView,
      meta: { title: '年度报告' },
    },
    {
      path: '/hospital/list',
      name: 'hospital-list',
      component: HospitalListView,
      meta: { title: '医院管理' },
    },
    {
      path: '/personnel/list',
      name: 'personnel-list',
      component: PersonnelListView,
      meta: { title: '人员管理' },
    },
    {
      path: '/product/list',
      name: 'product-list',
      component: ProductListView,
      meta: { title: '产品管理' },
    },
    {
      path: '/maintenance/data',
      name: 'maintenance-data',
      component: DataMaintenanceView,
      meta: { title: '数据维护中心' },
    },
    {
      path: '/docs/design-spec',
      name: 'design-spec',
      component: DesignSpecView,
      meta: { title: '综合设计方案' },
    },
  ],
})

export default router
