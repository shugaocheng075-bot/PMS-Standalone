<template>
  <div class="app-shell">
    <header class="top-nav">
      <div class="nav-left">
        <el-button v-if="isMobileViewport" link class="mobile-menu-btn" @click="mobileMenuVisible = true">
          <el-icon><Menu /></el-icon>
        </el-button>
        <el-button v-else link class="collapse-menu-btn" @click="toggleSidebar">
          <el-icon><component :is="sidebarCollapsed ? Expand : Fold" /></el-icon>
        </el-button>
        <div class="company-block">
          <div class="company-kicker">
            <span class="product-badge">PMS Enterprise</span>
            <span class="product-caption">运维协同工作台</span>
          </div>
          <div class="company-main">北京嘉和美康信息技术有限公司</div>
          <div class="company-sub">Goodwill Information Technology Co., Ltd.</div>
        </div>
      </div>

      <div class="nav-right">
        <span class="nav-suite-tag">Operations Console</span>
        <div class="nav-actions">
          <el-popover
            placement="bottom-end"
            :width="360"
            trigger="click"
            popper-class="notification-popover"
            @show="loadNotifications"
          >
            <template #reference>
              <el-badge
                :value="unreadCount"
                :hidden="unreadCount === 0"
                :max="99"
                class="notification-badge"
                aria-live="polite"
                role="status"
              >
                <span class="nav-icon"><el-icon><Bell /></el-icon></span>
              </el-badge>
            </template>
            <div class="notification-panel">
              <div class="notification-header">
                <span class="notification-title">通知 ({{ unreadCount }})</span>
                <el-button v-if="unreadCount > 0" link type="primary" size="small" @click="handleMarkAllRead">全部已读</el-button>
              </div>
              <div v-if="notificationLoading" class="notification-loading">
                <el-icon class="is-loading"><Loading /></el-icon>
                <span>加载中...</span>
              </div>
              <div v-else-if="notifications.length === 0" class="notification-empty">暂无通知</div>
              <div v-else class="notification-list">
                <div
                  v-for="item in notifications"
                  :key="item.id"
                  class="notification-item"
                  :class="{ 'is-unread': !item.isRead }"
                  @click="handleNotificationClick(item)"
                >
                  <div class="notification-item-title">
                    <span v-if="!item.isRead" class="unread-dot"></span>
                    {{ item.title }}
                  </div>
                  <div class="notification-item-content">{{ item.content }}</div>
                  <div class="notification-item-time">{{ formatTime(item.createdAt) }}</div>
                </div>
              </div>
            </div>
          </el-popover>
          <span class="nav-icon"><el-icon><User /></el-icon></span>
        </div>
        <span class="nav-divider"></span>
        <!-- Scope switch: supervisor+ can switch personnel identity -->
        <el-select
          v-if="canSwitchScope"
          v-model="selectedPersonnelId"
          size="small"
          class="scope-switch-select"
          placeholder="切换身份"
          filterable
          @change="onScopeSwitch"
        >
          <el-option
            v-for="actor in actorList"
            :key="actor.personnelId"
            :label="actor.personnelName"
            :value="actor.personnelId"
          >
            <span>{{ actor.personnelName }}</span>
            <span style="color: #909399; font-size: 12px; margin-left: 8px;">{{ actor.systemRole }}</span>
          </el-option>
        </el-select>
        <span class="user-name">{{ displayUserName }}</span>
        <el-button link class="logout-btn" @click="$router.push('/profile')">资料</el-button>
        <el-button link class="logout-btn" @click="showChangePasswordDialog = true">改密</el-button>
        <el-button link class="logout-btn" @click="onLogout">退出</el-button>
      </div>
    </header>

    <!-- 修改密码对话框 -->
    <el-dialog v-model="showChangePasswordDialog" title="修改密码" width="400" :close-on-click-modal="false">
      <el-form ref="passwordFormRef" :model="passwordForm" :rules="passwordFormRules" label-width="80px">
        <el-form-item label="原密码" prop="oldPassword">
          <el-input v-model="passwordForm.oldPassword" type="password" show-password placeholder="请输入原密码" />
        </el-form-item>
        <el-form-item label="新密码" prop="newPassword">
          <el-input v-model="passwordForm.newPassword" type="password" show-password placeholder="至少8位，包含字母和数字" />
        </el-form-item>
        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input v-model="passwordForm.confirmPassword" type="password" show-password placeholder="再次输入新密码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showChangePasswordDialog = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="changingPassword" @click="onChangePassword" icon="Edit">确认修改</el-button>
      </template>
    </el-dialog>

    <div class="workspace">
      <aside class="left-pane notranslate" :class="{ 'is-collapsed': sidebarCollapsed }" v-if="!isMobileViewport" lang="zh-CN" translate="no">
        <div v-if="!sidebarCollapsed" class="left-pane-overview">
          <div class="left-pane-kicker">Enterprise Navigation</div>
          <div class="left-pane-title">工作导航</div>
          <div class="left-pane-note">{{ currentRoleLabel }} · {{ accessibleMenuCount }} 个可访问入口</div>
        </div>

        <div v-if="!sidebarCollapsed" class="left-search">
          <el-input v-model="searchKeyword" placeholder="搜索功能（名称）" size="small" clearable />
        </div>

        <div class="menu-section">
          <div v-for="group in filteredSideMenuGroups" :key="group.title" class="menu-group">
            <div v-if="!sidebarCollapsed" class="section-title">{{ group.title }}</div>
            <el-menu :default-active="activeMenuKey" :collapse="sidebarCollapsed" :collapse-transition="false" class="side-menu" @select="handleMenuSelect">
              <template v-for="item in group.items" :key="item.key">
                <el-menu-item :index="item.key" class="menu-parent-item">
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
        <div class="content-topbar">
          <div class="content-topbar-main">
            <div class="content-breadcrumbs">
              <el-breadcrumb class="page-breadcrumb" separator="/">
                <el-breadcrumb-item v-for="crumb in breadcrumbItems" :key="crumb">{{ crumb }}</el-breadcrumb-item>
              </el-breadcrumb>
            </div>
            <div class="content-topbar-title-row">
              <span class="content-topbar-title">{{ currentWorkspaceTitle }}</span>
              <span class="content-topbar-role">{{ currentRoleLabel }}</span>
            </div>
            <div class="content-topbar-description">统一交付与运维工作台，支持从首页直接切入执行模块。</div>
          </div>
          <div class="content-topbar-meta">
            <span class="topbar-meta-label">交付视图</span>
            <span class="topbar-meta-value">{{ currentDateLabel }}</span>
          </div>
        </div>
        <div class="content-wrap">
          <router-view v-slot="{ Component }">
            <transition name="fade-slide" mode="out-in">
              <component :is="Component" />
            </transition>
          </router-view>
        </div>
        <footer class="workspace-footer">
          <span class="workspace-footer-brand">PMS Enterprise</span>
          <span class="workspace-footer-text">医疗运维项目管理平台</span>
        </footer>
      </main>
    </div>

    <el-drawer v-model="mobileMenuVisible" direction="ltr" size="82%" class="mobile-menu-drawer notranslate" :with-header="false">
      <div class="left-search mobile-search">
        <el-input v-model="searchKeyword" placeholder="搜索功能（名称）" size="small" clearable />
      </div>

      <div class="menu-section">
        <div v-for="group in filteredSideMenuGroups" :key="`mobile-${group.title}`" class="menu-group">
          <div class="section-title">{{ group.title }}</div>
          <el-menu :default-active="activeMenuKey" class="side-menu" @select="handleMobileMenuSelect">
            <template v-for="item in group.items" :key="`mobile-${item.key}`">
              <el-menu-item :index="item.key" class="menu-parent-item">
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
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter, type RouteLocationRaw } from 'vue-router'
import { Bell, Expand, Fold, Loading, Menu, User } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { logout, changePassword } from '../api/modules/auth'
import { fetchAccessActors } from '../api/modules/access'
import { fetchNotificationSummary, fetchNotifications, markNotificationAsRead, markAllNotificationsAsRead } from '../api/modules/notification'
import { useAccessControl } from '../composables/useAccessControl'
import { clearAuthState } from '../constants/access'
import type { NotificationItem } from '../types/notification'
import type { PersonnelActor } from '../types/access'

const route = useRoute()
const router = useRouter()
const searchKeyword = ref('')
const mobileMenuVisible = ref(false)
const isMobileViewport = ref(false)
const sidebarCollapsed = ref(false)

const SIDEBAR_STORAGE_KEY = 'pms-layout-sidebar-collapsed'

const access = useAccessControl()

const toggleSidebar = () => {
  sidebarCollapsed.value = !sidebarCollapsed.value
}

// Scope switch state
const actorList = ref<PersonnelActor[]>([])
const selectedPersonnelId = ref(access.currentPersonnelId.value)
const canSwitchScope = computed(() => access.isManager() || access.isRegionalManager() || access.isSupervisor())

const loadActors = async () => {
  try {
    const res = await fetchAccessActors()
    actorList.value = res.data ?? []
  } catch {
    // silent
  }
}

const onScopeSwitch = async (personnelId: number) => {
  access.setCurrentPersonnelId(personnelId)
  await access.ensureAccessProfileLoaded(true)
  router.go(0) // reload page to reflect new scope
}

// Notification state
const unreadCount = ref(0)
const notifications = ref<NotificationItem[]>([])
const notificationLoading = ref(false)
let notificationTimer: ReturnType<typeof setInterval> | null = null

const loadNotificationSummary = async () => {
  try {
    const res = await fetchNotificationSummary()
    unreadCount.value = res.data?.unreadCount ?? 0
  } catch {
    // silent
  }
}

const loadNotifications = async () => {
  notificationLoading.value = true
  try {
    const res = await fetchNotifications({ page: 1, size: 20 })
    notifications.value = res.data?.items ?? []
  } catch {
    notifications.value = []
  } finally {
    notificationLoading.value = false
  }
}

const handleMarkAllRead = async () => {
  try {
    await markAllNotificationsAsRead()
    unreadCount.value = 0
    notifications.value = notifications.value.map(n => ({ ...n, isRead: true }))
  } catch {
    // silent
  }
}

const handleNotificationClick = async (item: NotificationItem) => {
  if (!item.isRead) {
    try {
      await markNotificationAsRead(item.id)
      item.isRead = true
      unreadCount.value = Math.max(0, unreadCount.value - 1)
    } catch {
      // silent
    }
  }
  if (item.relatedPath) {
    await router.push(item.relatedPath)
  }
}

const formatTime = (dateStr: string) => {
  const d = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - d.getTime()
  const diffMin = Math.floor(diffMs / 60000)
  if (diffMin < 1) return '刚刚'
  if (diffMin < 60) return `${diffMin}分钟前`
  const diffHour = Math.floor(diffMin / 60)
  if (diffHour < 24) return `${diffHour}小时前`
  const diffDay = Math.floor(diffHour / 24)
  if (diffDay < 30) return `${diffDay}天前`
  return d.toLocaleDateString('zh-CN')
}

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

const currentRoleLabel = computed(() => {
  const profile = access.accessProfile.value
  if (!profile) {
    return '企业工作台'
  }

  if (profile.isAdmin) {
    return '系统管理员'
  }

  switch (profile.systemRole) {
    case 'manager':
      return '管理视角'
    case 'regional_manager':
      return '区域经理视角'
    case 'supervisor':
      return '运维主管视角'
    default:
      return '执行视角'
  }
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
    ],
  },
  {
    title: '报表中心',
    items: [
    { key: 'report-workhours', label: '工时报表', route: { path: '/report/workhours' }, permission: 'workhours.view' },
    { key: 'report-monthly-generate', label: '月度报告生成', route: { path: '/report/monthly-generate' }, permission: 'monthly-report.view' },
    ],
  },
  {
    title: '系统管理',
    items: [
    { key: 'audit-log', label: '操作日志', route: { path: '/audit/log' }, permission: 'audit.view' },
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

const findMatchedMenuKey = (items: MenuEntry[]): string | null => {
  for (const item of items) {
    if (routeMatchesMenu(item.route)) {
      return item.key
    }

    if (item.children?.some((child) => routeMatchesMenu(child.route))) {
      return item.key
    }
  }

  return null
}

const findMatchedMenuTrail = (groups: MenuGroup[]) => {
  for (const group of groups) {
    for (const item of group.items) {
      if (routeMatchesMenu(item.route)) {
        return [group.title, item.label]
      }

      const matchedChild = item.children?.find((child) => routeMatchesMenu(child.route))
      if (matchedChild) {
        return [group.title, item.label, matchedChild.label]
      }
    }
  }

  return [] as string[]
}

const activeMenuKey = computed(() => {
  for (const group of sideMenuGroups) {
    const matchedKey = findMatchedMenuKey(group.items)
    if (matchedKey) {
      return matchedKey
    }
  }

  return route.path
})

const menuRouteMap = computed(() => new Map(
  sideMenuGroups
    .flatMap((group) => group.items)
    .map((item) => [item.key, item.route])
    .filter((entry): entry is [string, RouteLocationRaw] => Boolean(entry[1])),
))

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

const accessibleMenuCount = computed(() => {
  return filteredSideMenuGroups.value.reduce((total, group) => {
    return total + group.items.reduce((sum, item) => sum + (item.children?.length ?? 1), 0)
  }, 0)
})

const breadcrumbItems = computed(() => {
  const trail = findMatchedMenuTrail(sideMenuGroups)
  const routeTitle = typeof route.meta?.title === 'string' ? route.meta.title : ''

  if (trail.length === 0) {
    return routeTitle ? [routeTitle] : ['工作台']
  }

  if (routeTitle && trail[trail.length - 1] !== routeTitle) {
    return [...trail, routeTitle]
  }

  return trail
})

const currentDateLabel = computed(() => new Date().toLocaleDateString('zh-CN', {
  year: 'numeric',
  month: 'long',
  day: 'numeric',
  weekday: 'long',
}))

const currentWorkspaceTitle = computed(() => {
  const title = typeof route.meta?.title === 'string' ? route.meta.title : ''
  return title || '工作台'
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

// Change password state
const showChangePasswordDialog = ref(false)
const changingPassword = ref(false)
const passwordFormRef = ref<FormInstance>()
const passwordForm = reactive({ oldPassword: '', newPassword: '', confirmPassword: '' })

const passwordFormRules: FormRules<typeof passwordForm> = {
  oldPassword: [
    { required: true, message: '请输入原密码', trigger: 'blur' },
    { min: 6, max: 128, message: '原密码长度需在6到128个字符之间', trigger: 'blur' },
  ],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 8, max: 128, message: '新密码长度需在8到128个字符之间', trigger: 'blur' },
    { pattern: /^(?=.*[A-Za-z])(?=.*\d).+$/, message: '新密码需至少包含字母和数字', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请再次输入新密码', trigger: 'blur' },
    {
      validator: (_rule, value: string, callback) => {
        if (value !== passwordForm.newPassword) {
          callback(new Error('两次输入的新密码不一致'))
          return
        }

        callback()
      },
      trigger: 'blur',
    },
  ],
}

const resetPasswordForm = () => {
  passwordForm.oldPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmPassword = ''
  passwordFormRef.value?.clearValidate()
}

const onChangePassword = async () => {
  const valid = await passwordFormRef.value?.validate().catch(() => false)
  if (!valid) {
    return
  }

  if (passwordForm.oldPassword === passwordForm.newPassword) {
    ElMessage.warning('新密码不能与原密码相同')
    return
  }

  changingPassword.value = true
  try {
    await changePassword({
      oldPassword: passwordForm.oldPassword,
      newPassword: passwordForm.newPassword,
    })
    ElMessage.success('密码修改成功')
    showChangePasswordDialog.value = false
    resetPasswordForm()
  } catch {
    ElMessage.error('密码修改失败，请检查原密码是否正确')
  } finally {
    changingPassword.value = false
  }
}

watch(showChangePasswordDialog, (visible) => {
  if (!visible) {
    resetPasswordForm()
  }
})

watch(sidebarCollapsed, (collapsed) => {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(SIDEBAR_STORAGE_KEY, collapsed ? '1' : '0')
})

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
    try {
      await access.ensureAccessProfileLoaded()
      sidebarCollapsed.value = typeof window !== 'undefined' && window.localStorage.getItem(SIDEBAR_STORAGE_KEY) === '1'
      updateViewportState()
      window.addEventListener('resize', updateViewportState)
      await loadNotificationSummary()
      notificationTimer = setInterval(loadNotificationSummary, 60000)
      if (canSwitchScope.value) {
        await loadActors()
      }
    } catch {
      clearAuthState()
      await router.replace('/login')
    }
  })()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateViewportState)
  if (notificationTimer) {
    clearInterval(notificationTimer)
    notificationTimer = null
  }
})

</script>

<style scoped>
.app-shell {
  position: relative;
  height: 100vh;
  overflow: hidden;
  background: linear-gradient(180deg, #ecf2f9 0%, #e7edf6 100%);
}

.app-shell::before {
  content: "";
  position: absolute;
  inset: 0;
  pointer-events: none;
  background:
    radial-gradient(circle at 0% 0%, rgb(21 101 255 / 0.1), transparent 28%),
    radial-gradient(circle at 100% 12%, rgb(18 182 165 / 0.12), transparent 28%),
    linear-gradient(180deg, rgb(255 255 255 / 0.22), transparent 26%);
}

.top-nav {
  position: sticky;
  top: 0;
  z-index: 1000;
  min-height: 72px;
  background:
    linear-gradient(96deg, #081a2c 0%, #103456 46%, #0c5e5f 100%);
  color: #fff;
  display: flex;
  flex-wrap: nowrap;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.12);
  overflow: hidden;
  box-shadow: 0 18px 38px rgb(7 22 39 / 0.24);
  backdrop-filter: blur(12px);
}

.top-nav::before {
  content: "";
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at 12% 18%, rgba(151, 205, 255, 0.24), transparent 30%),
    radial-gradient(circle at 86% 0%, rgba(95, 251, 226, 0.2), transparent 36%);
  pointer-events: none;
}

.top-nav::after {
  content: "";
  position: absolute;
  inset: auto auto 0 0;
  width: 100%;
  height: 1px;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.3), transparent);
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

.company-kicker {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 8px;
}

.product-badge {
  display: inline-flex;
  align-items: center;
  padding: 5px 10px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.16);
  background: rgba(255, 255, 255, 0.1);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.16);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.product-caption {
  font-size: 12px;
  color: rgba(231, 240, 255, 0.78);
}

.company-main {
  font-size: 18px;
  font-weight: 700;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 420px;
  letter-spacing: 0.02em;
}

.company-sub {
  margin-top: 4px;
  font-size: 11px;
  color: rgba(226, 236, 248, 0.78);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 340px;
}

.nav-right {
  position: relative;
  z-index: 1;
  display: flex;
  flex-wrap: nowrap;
  flex: 0 0 auto;
  align-items: center;
  gap: 12px;
  font-size: 14px;
  white-space: nowrap;
}

.nav-suite-tag {
  display: inline-flex;
  align-items: center;
  padding: 8px 12px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.14);
  background: rgba(255, 255, 255, 0.1);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.12);
  font-size: 12px;
  font-weight: 600;
  color: #eaf1ff;
}

.nav-actions {
  display: inline-flex;
  align-items: center;
  gap: 10px;
}

.nav-divider {
  width: 1px;
  height: 20px;
  background: linear-gradient(180deg, transparent 0%, rgba(255, 255, 255, 0.75) 50%, transparent 100%);
}

.scope-switch-select {
  width: 168px;
}

.scope-switch-select :deep(.el-select__wrapper) {
  min-height: 38px;
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.14);
  box-shadow: none;
}

.scope-switch-select :deep(.el-select__selection-item),
.scope-switch-select :deep(.el-select__placeholder),
.scope-switch-select :deep(.el-select__caret) {
  color: #ffffff;
}

.nav-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.14);
  background: rgba(255, 255, 255, 0.08);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.12);
  color: rgba(255, 255, 255, 0.94);
  transition:
    transform 0.2s ease,
    background-color 0.2s ease,
    color 0.2s ease,
    border-color 0.2s ease;
}

.nav-icon:hover {
  transform: translateY(-1px);
  border-color: rgba(255, 255, 255, 0.2);
  background: rgba(255, 255, 255, 0.16);
  color: #fff;
}

.notification-badge :deep(.el-badge__content.is-fixed) {
  top: 7px;
  right: 8px;
  border: none;
  background: linear-gradient(135deg, #ff7b5c 0%, #f53f3f 100%);
  box-shadow: 0 8px 18px rgba(243, 90, 74, 0.35);
}

.user-name {
  font-size: 13px;
  font-weight: 600;
  color: #f4f8ff;
  white-space: nowrap;
}

.logout-btn {
  color: #fff;
  min-height: 34px;
  padding: 0 10px;
  border-radius: 999px;
}

.logout-btn:hover {
  color: #fff;
  background: rgba(255, 255, 255, 0.12);
  border-radius: 999px;
}

.workspace {
  position: relative;
  display: flex;
  gap: 16px;
  height: calc(100vh - 72px);
  padding: 16px;
  overflow: hidden;
}

.left-pane {
  width: 252px;
  flex-shrink: 0;
  border-radius: 26px;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.94) 0%, rgba(247, 250, 254, 0.92) 100%);
  border: 1px solid rgba(255, 255, 255, 0.76);
  padding: 16px 14px;
  overflow: auto;
  font-family: "Microsoft YaHei", "PingFang SC", sans-serif;
  -webkit-text-size-adjust: 100%;
  text-size-adjust: 100%;
  box-shadow: var(--pms-shadow-xs);
  z-index: 10;
}

.left-pane-overview {
  margin-bottom: 16px;
  padding: 16px 16px 18px;
  border-radius: 20px;
  color: #ffffff;
  background:
    radial-gradient(circle at 18% 12%, rgba(146, 214, 255, 0.24), transparent 24%),
    linear-gradient(145deg, #0f365d 0%, #0a5b99 48%, #0b7f81 100%);
  box-shadow: 0 22px 38px rgba(13, 52, 93, 0.22);
}

.left-pane-kicker {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: rgba(230, 241, 255, 0.78);
}

.left-pane-title {
  margin-top: 12px;
  font-size: 22px;
  line-height: 1.1;
  font-weight: 700;
}

.left-pane-note {
  margin-top: 8px;
  font-size: 12px;
  line-height: 1.7;
  color: rgba(232, 242, 255, 0.82);
}

.left-search {
  position: sticky;
  top: 0;
  z-index: 2;
  margin-bottom: 16px;
  padding-bottom: 14px;
  background: linear-gradient(180deg, rgba(250, 252, 255, 0.98), rgba(250, 252, 255, 0.88) 72%, transparent);
}

.left-search :deep(.el-input__wrapper) {
  min-height: 40px;
  border-radius: 14px;
  background: linear-gradient(180deg, rgba(247, 250, 254, 0.96), rgba(241, 246, 252, 0.92));
}

.menu-section + .menu-section {
  margin-top: 16px;
}

.menu-group + .menu-group {
  margin-top: 12px;
}

.empty-menu-tip {
  padding: 8px 16px;
  font-size: 13px;
  font-family: inherit;
  color: var(--pms-slate-400);
}

.section-title {
  position: relative;
  margin: 12px 0 12px;
  padding-left: 12px;
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  font-family: inherit;
  color: var(--pms-slate-500);
}

.section-title::before {
  content: "";
  position: absolute;
  left: 0;
  top: 1px;
  width: 4px;
  height: 16px;
  border-radius: 4px;
  background: linear-gradient(180deg, var(--pms-primary) 0%, var(--pms-accent) 100%);
}

.side-menu {
  border: none;
  background: transparent;
  font-family: inherit;
}

.side-menu :deep(.el-menu-item) {
  position: relative;
  margin-bottom: 4px;
  border-radius: 14px;
  border: 1px solid transparent;
  background: transparent;
  color: var(--pms-slate-600);
  height: 44px;
  line-height: 44px;
  font-family: "Microsoft YaHei", "PingFang SC", sans-serif;
  font-size: 14px;
  font-weight: 500;
  font-synthesis: none;
  padding-left: 14px !important;
  transition:
    transform 0.22s ease,
    background-color 0.22s ease,
    color 0.22s ease,
    border-color 0.22s ease,
    box-shadow 0.22s ease;
}

.side-menu :deep(.el-menu-item)::before {
  content: "";
  position: absolute;
  inset: 0;
  border-radius: inherit;
  background: linear-gradient(135deg, rgba(21, 101, 255, 0.12), rgba(18, 182, 165, 0.06));
  opacity: 0;
  transition: opacity 0.22s ease;
}

.menu-item-icon {
  margin-right: 12px;
  font-size: 16px;
  color: var(--pms-slate-400);
  transition: color 0.2s ease;
}

.menu-item-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  letter-spacing: 0;
  font-family: inherit;
}

.side-menu :deep(.el-menu-item.is-active) {
  color: var(--pms-primary);
  border-color: rgba(21, 101, 255, 0.18);
  background: rgba(255, 255, 255, 0.84);
  box-shadow: 0 12px 24px rgba(21, 101, 255, 0.08);
  font-weight: 600;
}

.side-menu :deep(.el-menu-item.is-active)::before {
  opacity: 1;
}

.side-menu :deep(.el-menu-item.is-active) .menu-item-icon {
  color: var(--pms-primary);
}

.side-menu :deep(.el-menu-item:hover:not(.is-active)) {
  transform: translateY(-1px);
  border-color: rgba(160, 177, 197, 0.18);
  background: rgba(248, 251, 255, 0.82);
  color: var(--pms-slate-900);
}

.side-menu :deep(.el-menu-item:hover:not(.is-active))::before {
  opacity: 0.6;
}

.side-menu :deep(.el-menu-item:hover:not(.is-active)) .menu-item-icon {
  color: var(--pms-slate-600);
}

.side-menu :deep(.menu-parent-item) {
  font-weight: 600;
}

.main-pane {
  position: relative;
  flex: 1;
  min-width: 0;
  padding: 4px 4px 12px 0;
  height: 100%;
  overflow: auto;
  background: transparent;
}

.content-wrap {
  position: relative;
  width: 100%;
  min-height: 100%;
  max-width: 1720px;
  margin: 0 auto;
  animation: app-pane-rise 0.28s ease;
}

.mobile-menu-btn {
  color: #fff;
  font-size: 18px;
  padding: 6px;
  border-radius: 999px;
}

.mobile-menu-btn:hover {
  background: rgba(255, 255, 255, 0.12);
}

.mobile-search {
  margin-bottom: 12px;
}

.collapse-menu-btn {
  width: 40px;
  height: 40px;
  border-radius: 12px;
  color: inherit;
}

.collapse-menu-btn:hover {
  background: rgba(148, 163, 184, 0.12);
}

.left-pane.is-collapsed {
  width: 84px;
  padding-inline: 10px;
}

.left-pane.is-collapsed .menu-group + .menu-group {
  margin-top: 8px;
}

.left-pane.is-collapsed :deep(.el-menu--collapse) {
  border-right: none;
}

.left-pane.is-collapsed :deep(.el-menu-item) {
  justify-content: center;
  padding-inline: 0 !important;
}

.left-pane.is-collapsed .menu-item-icon {
  margin-right: 0;
}

.left-pane.is-collapsed .menu-item-label {
  display: none;
}

.main-pane {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.content-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 16px 20px;
  border-radius: 22px;
  background:
    radial-gradient(circle at 100% 0, rgba(76, 165, 145, 0.12), transparent 24%),
    linear-gradient(180deg, rgba(255, 255, 255, 0.9), rgba(248, 251, 255, 0.84));
  border: 1px solid rgba(226, 232, 240, 0.92);
  box-shadow: 0 14px 30px rgba(15, 23, 42, 0.05);
  backdrop-filter: blur(10px);
}

.content-topbar-main {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}

.content-breadcrumbs {
  display: flex;
  flex-direction: column;
  gap: 0;
  min-width: 0;
}

.content-topbar-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.content-topbar-title {
  font-size: 22px;
  line-height: 1.1;
  font-weight: 700;
  color: var(--pms-slate-900);
}

.content-topbar-role {
  display: inline-flex;
  align-items: center;
  padding: 5px 10px;
  border-radius: 999px;
  background: rgba(21, 101, 255, 0.08);
  color: var(--pms-primary);
  font-size: 12px;
  font-weight: 700;
  white-space: nowrap;
}

.content-topbar-description {
  font-size: 12px;
  color: var(--pms-slate-500);
  line-height: 1.6;
}

.content-topbar-meta {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  border-radius: 999px;
  background: rgba(248, 250, 252, 0.96);
  border: 1px solid rgba(226, 232, 240, 0.95);
  color: var(--pms-slate-600);
  white-space: nowrap;
}

.topbar-meta-label {
  font-size: 12px;
  font-weight: 600;
  color: var(--pms-slate-500);
}

.topbar-meta-value {
  font-size: 13px;
  font-weight: 600;
  color: var(--pms-slate-900);
}

.page-breadcrumb :deep(.el-breadcrumb__inner),
.page-breadcrumb :deep(.el-breadcrumb__separator) {
  color: var(--pms-slate-500);
  font-size: 12px;
}

.page-breadcrumb :deep(.el-breadcrumb__item:last-child .el-breadcrumb__inner) {
  color: var(--pms-slate-700);
  font-weight: 600;
}

.workspace-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 8px 6px 2px;
  color: var(--pms-slate-500);
  font-size: 12px;
}

.workspace-footer-brand {
  font-weight: 700;
  color: var(--pms-slate-700);
}

:deep(.mobile-menu-drawer .el-drawer) {
  border-radius: 0 28px 28px 0;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98), rgba(247, 250, 254, 0.94));
}

:deep(.mobile-menu-drawer .el-drawer__body) {
  padding: 18px 16px;
  font-family: "Microsoft YaHei", "PingFang SC", sans-serif;
  -webkit-text-size-adjust: 100%;
  text-size-adjust: 100%;
}

:deep(.notification-popover) {
  padding: 14px !important;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98), rgba(247, 250, 254, 0.96));
}

@media (max-width: 992px) {
  .top-nav {
    min-height: 60px;
    padding: 10px 12px;
  }

  .company-kicker,
  .company-sub,
  .nav-suite-tag {
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

  .content-topbar {
    flex-wrap: wrap;
    padding: 12px 14px;
  }

  .content-topbar-title-row {
    flex-wrap: wrap;
  }

  .content-topbar-meta {
    width: 100%;
    justify-content: space-between;
  }

  .workspace-footer {
    padding-inline: 2px;
    flex-wrap: wrap;
  }

  .workspace {
    gap: 0;
    height: calc(100vh - 60px);
    padding: 12px;
  }

  .main-pane {
    width: 100%;
    padding: 0;
    height: calc(100vh - 60px);
  }
}

@media (max-width: 1280px) {
  .nav-suite-tag,
  .company-sub {
    display: none;
  }
}

.notification-badge {
  display: inline-flex;
  cursor: pointer;
}

.notification-panel {
  max-height: 440px;
  display: flex;
  flex-direction: column;
}

.notification-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 2px 4px 12px;
  border-bottom: 1px solid #edf2f7;
  margin-bottom: 10px;
}

.notification-title {
  font-weight: 700;
  font-size: 15px;
  color: var(--pms-slate-900);
}

.notification-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 24px 0;
  color: var(--pms-slate-500);
}

.notification-empty {
  text-align: center;
  padding: 24px 0;
  color: var(--pms-slate-500);
  font-size: 13px;
}

.notification-list {
  overflow-y: auto;
  max-height: 360px;
}

.notification-item {
  padding: 12px 12px 14px;
  margin-bottom: 8px;
  border-radius: 16px;
  border: 1px solid transparent;
  background: linear-gradient(180deg, rgba(248, 251, 255, 0.98), rgba(244, 248, 252, 0.92));
  cursor: pointer;
  transition:
    transform 0.2s ease,
    border-color 0.2s ease,
    box-shadow 0.2s ease,
    background 0.2s ease;
}

.notification-item:hover {
  background: #ffffff;
  border-color: rgba(21, 101, 255, 0.16);
  box-shadow: 0 12px 24px rgba(21, 101, 255, 0.08);
  transform: translateY(-1px);
}

.notification-item.is-unread {
  border-color: rgba(21, 101, 255, 0.16);
  background: linear-gradient(180deg, rgba(237, 244, 255, 0.96), rgba(245, 250, 255, 0.92));
}

.notification-item-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--pms-slate-900);
  display: flex;
  align-items: center;
  gap: 6px;
}

.unread-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--pms-primary) 0%, var(--pms-accent) 100%);
  box-shadow: 0 0 0 4px rgba(21, 101, 255, 0.12);
  flex-shrink: 0;
}

.notification-item-content {
  font-size: 12px;
  color: var(--pms-slate-600);
  margin-top: 4px;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.notification-item-time {
  font-size: 11px;
  color: var(--pms-slate-500);
  margin-top: 4px;
}

@keyframes app-pane-rise {
  from {
    opacity: 0;
    transform: translateY(6px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
