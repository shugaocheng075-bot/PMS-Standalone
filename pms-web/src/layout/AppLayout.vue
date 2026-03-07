<template>
  <div class="app-shell">
    <header class="top-nav">
      <div class="nav-left">
        <el-button v-if="isMobileViewport" link class="mobile-menu-btn" @click="mobileMenuVisible = true">
          <el-icon><Menu /></el-icon>
        </el-button>
        <div class="company-block">
          <div class="company-main">北京嘉和美康信息技术有限公司</div>
          <div class="company-sub">Goodwill Information Technology Co., Ltd.</div>
        </div>
      </div>

      <div class="nav-right">
        <div class="nav-actions">
          <span class="nav-icon"><el-icon><Bell /></el-icon></span>
          <span class="nav-icon"><el-icon><User /></el-icon></span>
        </div>
        <span class="nav-divider"></span>
        <span class="user-name">{{ displayUserName }}</span>
        <el-button link class="logout-btn" @click="onLogout">退出</el-button>
      </div>
    </header>

    <div class="workspace">
      <aside class="left-pane" v-if="!isMobileViewport">
        <div class="left-search">
          <el-input v-model="searchKeyword" placeholder="搜索功能（名称）" size="small" clearable />
        </div>

        <div class="menu-section">
          <div v-for="group in filteredSideMenuGroups" :key="group.title" class="menu-group">
            <div class="section-title">{{ group.title }}</div>
            <el-menu :default-active="activeMenuKey" class="side-menu" @select="handleMenuSelect">
              <template v-for="item in group.items" :key="item.key">
                <el-sub-menu v-if="item.children?.length" :index="item.key">
                  <template #title>
                    <el-icon class="menu-item-icon"><Menu /></el-icon>
                    <span class="menu-item-label">{{ item.label }}</span>
                  </template>
                  <el-menu-item v-for="child in item.children" :key="child.key" :index="child.key">
                    <span class="submenu-item-label">{{ child.label }}</span>
                  </el-menu-item>
                </el-sub-menu>
                <el-menu-item v-else :index="item.key">
                  <el-icon class="menu-item-icon"><Menu /></el-icon>
                  <span class="menu-item-label">{{ item.label }}</span>
                </el-menu-item>
              </template>
            </el-menu>
          </div>
          <div v-if="filteredSideMenuGroups.length === 0" class="empty-menu-tip">未找到匹配功能</div>
        </div>
      </aside>

      <main class="main-pane">
        <div class="content-wrap">
          <router-view />
        </div>
      </main>
    </div>

    <el-drawer v-model="mobileMenuVisible" direction="ltr" size="82%" class="mobile-menu-drawer" :with-header="false">
      <div class="left-search mobile-search">
        <el-input v-model="searchKeyword" placeholder="搜索功能（名称）" size="small" clearable />
      </div>

      <div class="menu-section">
        <div v-for="group in filteredSideMenuGroups" :key="`mobile-${group.title}`" class="menu-group">
          <div class="section-title">{{ group.title }}</div>
          <el-menu :default-active="activeMenuKey" class="side-menu" @select="handleMobileMenuSelect">
            <template v-for="item in group.items" :key="`mobile-${item.key}`">
              <el-sub-menu v-if="item.children?.length" :index="`mobile-${item.key}`">
                <template #title>
                  <el-icon class="menu-item-icon"><Menu /></el-icon>
                  <span class="menu-item-label">{{ item.label }}</span>
                </template>
                <el-menu-item v-for="child in item.children" :key="`mobile-${child.key}`" :index="child.key">
                  <span class="submenu-item-label">{{ child.label }}</span>
                </el-menu-item>
              </el-sub-menu>
              <el-menu-item v-else :index="item.key">
                <el-icon class="menu-item-icon"><Menu /></el-icon>
                <span class="menu-item-label">{{ item.label }}</span>
              </el-menu-item>
            </template>
          </el-menu>
        </div>
        <div v-if="filteredSideMenuGroups.length === 0" class="empty-menu-tip">未找到匹配功能</div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter, type RouteLocationRaw } from 'vue-router'
import { Bell, Menu, User } from '@element-plus/icons-vue'
import { logout } from '../api/modules/auth'
import { useAccessControl } from '../composables/useAccessControl'
import { clearAuthState } from '../constants/access'

const route = useRoute()
const router = useRouter()
const searchKeyword = ref('')
const mobileMenuVisible = ref(false)
const isMobileViewport = ref(false)

const access = useAccessControl()

const displayUserName = computed(() => {
  const profile = access.accessProfile.value
  if (!profile) {
    return ''
  }

  if (profile.isAdmin) {
    return 'admin'
  }

  return profile.personnelName || '用户'
})

type MenuEntry = {
  key: string
  label: string
  permission: string
  route?: RouteLocationRaw
  children?: MenuEntry[]
}

type MenuGroup = {
  title: string
  items: MenuEntry[]
}

const sideMenuGroups: MenuGroup[] = [
  {
    title: '首页功能',
    items: [
    { key: 'dashboard', label: '首页', route: { path: '/dashboard' }, permission: 'dashboard.view' },
    { key: 'dashboard-kpi', label: 'KPI 概览', route: { path: '/dashboard/kpi' }, permission: 'dashboard.view' },
    { key: 'alert-center', label: '统一预警中心', route: { path: '/alert/center' }, permission: 'alert-center.view' },
    ],
  },
  {
    title: '项目管理',
    items: [
    {
      key: 'project-list',
      label: '项目台账',
      route: { path: '/project/list' },
      permission: 'project.view',
      children: [
        { key: 'project-list-all', label: '项目列表', route: { path: '/project/list' }, permission: 'project.view' },
        { key: 'project-overdue', label: '超期项目', route: { path: '/project/list', query: { contractStatus: '超期未签署' } }, permission: 'project.view' },
        { key: 'project-signed', label: '已签合同', route: { path: '/project/list', query: { contractStatus: '合同已签署' } }, permission: 'project.view' },
      ],
    },
    { key: 'todo-list', label: '待办列表', route: { path: '/complex-model/ms010001-013-001' }, permission: 'project.view' },
    { key: 'contract-alerts', label: '合同预警', route: { path: '/contract/alerts' }, permission: 'contract.view' },
    ],
  },
  {
    title: '运营管理',
    items: [
    {
      key: 'handover-list',
      label: '交接管理',
      route: { path: '/handover/list' },
      permission: 'handover.view',
      children: [
        { key: 'handover-all', label: '全部交接', route: { path: '/handover/list' }, permission: 'handover.view' },
        { key: 'handover-pending', label: '未发', route: { path: '/handover/list', query: { stage: '未发' } }, permission: 'handover.view' },
        { key: 'handover-progress', label: '交接中', route: { path: '/handover/list', query: { stage: '交接中' } }, permission: 'handover.view' },
        { key: 'handover-completed', label: '已交接', route: { path: '/handover/list', query: { stage: '已交接' } }, permission: 'handover.view' },
      ],
    },
    {
      key: 'inspection-plan',
      label: '巡检计划',
      route: { path: '/inspection/plan' },
      permission: 'inspection.view',
      children: [
        { key: 'inspection-plan-all', label: '计划列表', route: { path: '/inspection/plan' }, permission: 'inspection.view' },
        { key: 'inspection-plan-progress', label: '执行中', route: { path: '/inspection/plan', query: { status: '执行中' } }, permission: 'inspection.view' },
        { key: 'inspection-plan-completed', label: '已完成', route: { path: '/inspection/plan', query: { status: '已完成' } }, permission: 'inspection.view' },
        { key: 'inspection-result-warning', label: '结果警告', route: { path: '/inspection/plan', query: { tab: 'results', healthLevel: '警告' } }, permission: 'inspection.view' },
      ],
    },
    {
      key: 'annual-report-list',
      label: '年度报告',
      route: { path: '/annual-report/list' },
      permission: 'annual-report.view',
      children: [
        { key: 'annual-report-all', label: '全部报告', route: { path: '/annual-report/list' }, permission: 'annual-report.view' },
        { key: 'annual-report-writing', label: '编写中', route: { path: '/annual-report/list', query: { status: '编写中' } }, permission: 'annual-report.view' },
        { key: 'annual-report-submitted', label: '已提交', route: { path: '/annual-report/list', query: { status: '已提交' } }, permission: 'annual-report.view' },
        { key: 'annual-report-completed', label: '已完成', route: { path: '/annual-report/list', query: { status: '已完成' } }, permission: 'annual-report.view' },
      ],
    },
    {
      key: 'hospital-list',
      label: '医院管理',
      route: { path: '/hospital/list' },
      permission: 'hospital.view',
      children: [
        { key: 'hospital-list-all', label: '医院列表', route: { path: '/hospital/list' }, permission: 'hospital.view' },
        { key: 'hospital-create', label: '新增医院', route: { path: '/hospital/list', query: { action: 'create' } }, permission: 'hospital.manage' },
      ],
    },
    {
      key: 'permission-manage',
      label: '权限管理',
      route: { path: '/permission/manage' },
      permission: 'permission.manage',
      children: [
        { key: 'personnel-list', label: '人员权限列表', route: { path: '/permission/manage' }, permission: 'permission.manage' },
        { key: 'personnel-create', label: '新增人员', route: { path: '/permission/manage', query: { action: 'create' } }, permission: 'personnel.manage' },
      ],
    },
    {
      key: 'product-list',
      label: '产品管理',
      route: { path: '/product/list' },
      permission: 'product.view',
      children: [
        { key: 'product-list-all', label: '产品列表', route: { path: '/product/list' }, permission: 'product.view' },
        { key: 'product-create', label: '新增产品', route: { path: '/product/list', query: { action: 'create' } }, permission: 'product.manage' },
      ],
    },
    {
      key: 'major-demand-list',
      label: '重大需求',
      route: { path: '/major-demand/list' },
      permission: 'major-demand.view',
      children: [
        { key: 'major-demand-all', label: '全部需求', route: { path: '/major-demand/list' }, permission: 'major-demand.view' },
        { key: 'major-demand-processing', label: '处理中', route: { path: '/major-demand/list', query: { status: '处理中' } }, permission: 'major-demand.view' },
        { key: 'major-demand-verifying', label: '待验证', route: { path: '/major-demand/list', query: { status: '待验证' } }, permission: 'major-demand.view' },
      ],
    },
    {
      key: 'repair-list',
      label: '报修记录',
      route: { path: '/repair/list' },
      permission: 'repair.view',
      children: [
        { key: 'repair-list-all', label: '报修列表', route: { path: '/repair/list' }, permission: 'repair.view' },
        { key: 'repair-create', label: '新增报修', route: { path: '/repair/list', query: { action: 'create' } }, permission: 'repair.manage' },
        { key: 'repair-pending', label: '待处理', route: { path: '/repair/list', query: { status: '待处理' } }, permission: 'repair.view' },
        { key: 'repair-progress', label: '处理中', route: { path: '/repair/list', query: { status: '处理中' } }, permission: 'repair.view' },
      ],
    },
    {
      key: 'workhours-list',
      label: '工时管理',
      route: { path: '/workhours/list' },
      permission: 'workhours.view',
      children: [
        { key: 'workhours-list-all', label: '工时列表', route: { path: '/workhours/list' }, permission: 'workhours.view' },
        { key: 'workhours-create', label: '新增工时', route: { path: '/workhours/list', query: { action: 'create' } }, permission: 'workhours.manage' },
        { key: 'workhours-sick', label: '病假登记', route: { path: '/workhours/list', query: { action: 'create', workType: '病假' } }, permission: 'workhours.manage' },
        { key: 'workhours-personal', label: '事假登记', route: { path: '/workhours/list', query: { action: 'create', workType: '事假' } }, permission: 'workhours.manage' },
        { key: 'workhours-special', label: '其他特殊', route: { path: '/workhours/list', query: { action: 'create', workType: '其他特殊' } }, permission: 'workhours.manage' },
      ],
    },
    {
      key: 'monthly-report-list',
      label: '月度报告',
      route: { path: '/monthly-report/list' },
      permission: 'monthly-report.view',
      children: [
        { key: 'monthly-report-list-all', label: '月报列表', route: { path: '/monthly-report/list' }, permission: 'monthly-report.view' },
        { key: 'monthly-report-create', label: '新增月报', route: { path: '/monthly-report/list', query: { action: 'create' } }, permission: 'monthly-report.manage' },
        { key: 'monthly-report-generate', label: '月报生成', route: { path: '/report/monthly-generate' }, permission: 'monthly-report.view' },
      ],
    },
    { key: 'maintenance-data', label: '数据维护中心', route: { path: '/maintenance/data' }, permission: 'maintenance.manage' },
    ],
  },
  {
    title: '报表中心',
    items: [
    { key: 'report-workhours', label: '工时报表', route: { path: '/report/workhours' }, permission: 'workhours.view' },
    { key: 'report-monthly-generate', label: '月度报告生成', route: { path: '/report/monthly-generate' }, permission: 'monthly-report.view' },
    ],
  },
]

const readQueryValue = (value: unknown) => {
  if (typeof value === 'string') {
    return value
  }

  if (Array.isArray(value) && typeof value[0] === 'string') {
    return value[0]
  }

  return ''
}

const flattenMenuEntries = (items: MenuEntry[]): MenuEntry[] => items.flatMap((item) => [item, ...(item.children ? flattenMenuEntries(item.children) : [])])

const allMenuEntries = computed(() => flattenMenuEntries(sideMenuGroups.flatMap((group) => group.items)))

const routeMatchesMenu = (targetRoute: RouteLocationRaw | undefined) => {
  if (!targetRoute || typeof targetRoute !== 'object' || !('path' in targetRoute)) {
    return false
  }

  if ((targetRoute.path ?? '') !== route.path) {
    return false
  }

  const targetQuery = targetRoute.query ?? {}
  return Object.entries(targetQuery).every(([key, value]) => readQueryValue(route.query[key]) === String(value ?? ''))
}

const activeMenuKey = computed(() => {
  const matched = allMenuEntries.value.find((item) => routeMatchesMenu(item.route))
  return matched?.key ?? route.path
})

const menuRouteMap = computed(() => new Map(allMenuEntries.value.map((item) => [item.key, item.route]).filter((entry): entry is [string, RouteLocationRaw] => Boolean(entry[1]))))

const filterMenuEntry = (item: MenuEntry, keyword: string): MenuEntry | null => {
  if (!access.canPermission(item.permission)) {
    return null
  }

  const routePath = item.route && typeof item.route === 'object' && 'path' in item.route ? item.route.path ?? '' : ''
  const selfMatched = !keyword
    || item.label.toLowerCase().includes(keyword)
    || routePath.toLowerCase().includes(keyword)

  const visibleChildren = item.children
    ?.map((child) => filterMenuEntry(child, keyword))
    .filter((child): child is MenuEntry => Boolean(child))

  if (visibleChildren && visibleChildren.length > 0) {
    return {
      ...item,
      children: selfMatched ? item.children?.filter((child) => access.canPermission(child.permission)) ?? visibleChildren : visibleChildren,
    }
  }

  if (!item.children && selfMatched) {
    return item
  }

  if (item.children && selfMatched) {
    return {
      ...item,
      children: item.children?.filter((child) => access.canPermission(child.permission)) ?? [],
    }
  }

  return null
}

const filteredSideMenuGroups = computed(() => {
  const keyword = searchKeyword.value.trim().toLowerCase()
  return sideMenuGroups
    .map((group) => ({
      ...group,
      items: group.items
        .map((item) => filterMenuEntry(item, keyword))
        .filter((item): item is MenuEntry => Boolean(item)),
    }))
    .filter((group) => group.items.length > 0)
})

const handleMenuSelect = async (key: string) => {
  const targetRoute = menuRouteMap.value.get(key)
  if (!targetRoute) {
    return
  }

  await router.push(targetRoute)
}

const handleMobileMenuSelect = async (key: string) => {
  await handleMenuSelect(key)
  mobileMenuVisible.value = false
}

const onLogout = async () => {
  try {
    await logout()
  } catch {
  }
  clearAuthState()
  await router.replace('/login')
}

const updateViewportState = () => {
  if (typeof window === 'undefined') {
    return
  }

  isMobileViewport.value = window.innerWidth <= 992
  if (!isMobileViewport.value) {
    mobileMenuVisible.value = false
  }
}

onMounted(() => {
  void (async () => {
    await access.ensureAccessProfileLoaded()
    updateViewportState()
    window.addEventListener('resize', updateViewportState)
  })()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateViewportState)
})

</script>

<style scoped>
.app-shell {
  height: 100vh;
  overflow: hidden;
  background: #f4f8fd;
}

.top-nav {
  position: sticky;
  top: 0;
  z-index: 1000;
  min-height: 48px;
  background: linear-gradient(90deg, #0066b2 0%, #0a79be 100%);
  color: #fff;
  display: flex;
  flex-wrap: nowrap;
  align-items: center;
  justify-content: space-between;
  padding: 0 12px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.16);
  overflow: hidden;
}

.top-nav::before {
  content: '';
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(
    -30deg,
    rgba(255, 255, 255, 0.08) 0,
    rgba(255, 255, 255, 0.08) 10px,
    transparent 10px,
    transparent 18px
  );
  opacity: 0.32;
  pointer-events: none;
}

.nav-left {
  position: relative;
  z-index: 1;
  display: flex;
  flex-wrap: nowrap;
  flex: 1 1 auto;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.company-block {
  display: flex;
  flex-direction: column;
  line-height: 1.05;
  min-width: 0;
}

.company-main {
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 360px;
}

.company-sub {
  margin-top: 1px;
  font-size: 10px;
  opacity: 0.9;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 300px;
}

.nav-right {
  position: relative;
  z-index: 1;
  display: flex;
  flex-wrap: nowrap;
  flex: 0 0 auto;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  white-space: nowrap;
}

.nav-actions {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.nav-divider {
  width: 1px;
  height: 16px;
  background: rgba(255, 255, 255, 0.35);
}

.nav-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  border-radius: 4px;
  color: rgba(255, 255, 255, 0.92);
  transition: background-color 0.2s ease, color 0.2s ease;
}

.nav-icon:hover {
  background: rgba(255, 255, 255, 0.14);
  color: #fff;
}

.user-name {
  font-size: 12px;
  white-space: nowrap;
}

.logout-btn {
  color: #fff;
  padding: 0 6px;
}

.logout-btn:hover {
  color: #fff;
  background: rgba(255, 255, 255, 0.12);
  border-radius: 4px;
}

.workspace {
  display: flex;
  height: calc(100vh - 48px);
  overflow: hidden;
}

.left-pane {
  width: 176px;
  background: #edf5fb;
  border-right: 1px solid #d7e6f3;
  padding: 8px 6px 12px;
  overflow-y: auto;
}

.left-search {
  margin-bottom: 10px;
}

.menu-section + .menu-section {
  margin-top: 10px;
}

.menu-group + .menu-group {
  margin-top: 10px;
}

.empty-menu-tip {
  padding: 8px 10px;
  font-size: 12px;
  color: #5b7f9c;
}

.section-title {
  position: relative;
  margin: 6px 0 6px;
  padding-left: 7px;
  font-size: 12px;
  font-weight: 600;
  color: #1e567f;
}

.section-title::before {
  content: '';
  position: absolute;
  left: 0;
  top: 3px;
  width: 2px;
  height: 12px;
  border-radius: 2px;
  background: #4ca591;
}

.side-menu {
  border: none;
  background: transparent;
}

.side-menu :deep(.el-menu-item) {
  margin-bottom: 4px;
  border-radius: 0;
  border: none;
  border-left: 3px solid transparent;
  background: transparent;
  color: #24557e;
  height: 30px;
  line-height: 30px;
  font-size: 12px;
  padding-left: 10px !important;
}

.menu-item-icon {
  margin-right: 6px;
  font-size: 13px;
  color: #4f7496;
}

.menu-item-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.side-menu :deep(.el-menu-item.is-active) {
  color: #0066b2;
  background: #d8eafa;
  border-left-color: #27b294;
}

.side-menu :deep(.el-menu-item.is-active) .menu-item-icon {
  color: #0a67ac;
}

.side-menu :deep(.el-menu-item:hover) {
  background: #e7f1fb;
  color: #0a5f9f;
}

.main-pane {
  flex: 1;
  padding: 14px;
  height: calc(100vh - 48px);
  overflow-y: auto;
  overflow-x: hidden;
}

.content-wrap {
  width: 100%;
}

.mobile-menu-btn {
  color: #fff;
  font-size: 18px;
  padding: 4px;
}

.mobile-search {
  margin-bottom: 12px;
}

@media (max-width: 992px) {
  .top-nav {
    padding: 8px 10px;
  }

  .company-sub {
    display: none;
  }

  .nav-right {
    gap: 8px;
  }

  .nav-divider {
    display: none;
  }

  .user-name {
    display: none;
  }

  .workspace {
    height: calc(100vh - 52px);
  }

  .main-pane {
    width: 100%;
    padding: 10px;
    height: calc(100vh - 52px);
  }
}

@media (max-width: 1280px) {
  .company-sub {
    display: none;
  }
}
</style>
