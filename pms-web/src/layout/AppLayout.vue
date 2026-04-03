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
        <el-button @click="showChangePasswordDialog = false">取消</el-button>
        <el-button type="primary" :loading="changingPassword" @click="onChangePassword">确认修改</el-button>
      </template>
    </el-dialog>

    <div class="workspace">
      <aside class="left-pane notranslate" v-if="!isMobileViewport" lang="zh-CN" translate="no">
        <div class="left-search">
          <el-input v-model="searchKeyword" placeholder="搜索功能（名称）" size="small" clearable />
        </div>

        <div class="menu-section">
          <div v-for="group in filteredSideMenuGroups" :key="group.title" class="menu-group">
            <div class="section-title">{{ group.title }}</div>
            <el-menu :default-active="activeMenuKey" class="side-menu" @select="handleMenuSelect">
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
        <div class="content-wrap">
          <router-view v-slot="{ Component }">
            <transition name="fade-slide" mode="out-in">
              <component :is="Component" />
            </transition>
          </router-view>
        </div>
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
import { Bell, Loading, Menu, User } from '@element-plus/icons-vue'
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

const access = useAccessControl()

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
    await loadNotificationSummary()
    notificationTimer = setInterval(loadNotificationSummary, 60000)
    if (canSwitchScope.value) {
      await loadActors()
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
  height: 100vh;
  overflow: hidden;
  background: var(--el-bg-color-page);
}

.top-nav {
  position: sticky;
  top: 0;
  z-index: 1000;
  min-height: 56px;
  background: var(--el-bg-color);
  color: var(--el-text-color-primary);
  display: flex;
  flex-wrap: nowrap;
  align-items: center;
  justify-content: space-between;
  padding: 0 40px;
  border-bottom: 1px solid var(--pms-border-color);
  overflow: hidden;
}

.top-nav::before {
  display: none;
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
  font-size: 15px;
  font-weight: 700;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 360px;
  letter-spacing: -0.01em;
  color: var(--el-text-color-primary);
}

.company-sub {
  margin-top: 2px;
  font-size: 11px;
  color: var(--el-text-color-secondary);
  font-weight: 500;
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
  gap: 12px;
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
  background: var(--el-border-color);
}

.scope-switch-select {
  width: 140px;
}

.nav-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 6px;
  color: var(--el-text-color-regular);
  transition: all 0.2s ease;
  border: 1px solid transparent;
}

.nav-icon:hover {
  background: var(--el-fill-color-light);
  color: var(--el-text-color-primary);
  border-color: var(--el-border-color);
}

.user-name {
  font-size: 13px;
  font-weight: 500;
  white-space: nowrap;
}

.logout-btn {
  color: var(--el-text-color-regular) !important;
  font-size: 13px;
  padding: 0 8px;
}

.logout-btn:hover {
  color: var(--el-text-color-primary) !important;
  background: var(--el-fill-color-light);
  border-radius: 6px;
}

.workspace {
  display: flex;
  height: calc(100vh - 56px);
  overflow: hidden;
  max-width: 1440px;
  margin: 0 auto;
}

.left-pane {
  width: 240px;
  background: transparent;
  border-right: none;
  padding: 32px 16px 20px;
  overflow: auto;
  font-family: inherit;
  -webkit-text-size-adjust: 100%;
  text-size-adjust: 100%;
  z-index: 10;
}

.left-search {
  margin-bottom: 16px;
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
  margin: 12px 0 8px;
  padding-left: 12px;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-family: inherit;
  color: var(--el-text-color-secondary);
}

.section-title::before {
  display: none;
}

.side-menu {
  border: none;
  background: transparent;
  font-family: inherit;
}

.side-menu :deep(.el-menu-item) {
  margin-bottom: 2px;
  border-radius: 6px;
  border: none;
  background: transparent;
  color: var(--el-text-color-regular);
  height: 36px;
  line-height: 36px;
  font-family: inherit;
  font-size: 14px;
  font-weight: 500;
  font-synthesis: none;
  padding-left: 12px !important;
  transition: all 0.15s ease;
}

.menu-item-icon {
  margin-right: 12px;
  font-size: 16px;
  color: var(--el-text-color-secondary);
  transition: color 0.15s ease;
}

.menu-item-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  letter-spacing: 0;
  font-family: inherit;
}

.side-menu :deep(.el-menu-item.is-active) {
  color: var(--el-color-primary) !important;
  background: var(--el-bg-color) !important;
  font-weight: 600;
  box-shadow: 0 1px 2px rgba(0,0,0,0.05);
  border: 1px solid var(--el-border-color);
}

.side-menu :deep(.el-menu-item.is-active) .menu-item-icon {
  color: var(--el-color-primary);
}

.side-menu :deep(.el-menu-item:hover:not(.is-active)) {
  background: transparent !important;
  color: var(--el-text-color-primary) !important;
}

.side-menu :deep(.el-menu-item:hover:not(.is-active)) .menu-item-icon {
  color: var(--el-text-color-regular);
}

.side-menu :deep(.menu-parent-item) {
  font-weight: 500;
}

.main-pane {
  flex: 1;
  padding: 0;
  height: calc(100vh - 56px);
  overflow: auto;
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

.mobile-menu-drawer :deep(.el-drawer__body) {
  font-family: "Microsoft YaHei", "PingFang SC", sans-serif;
  -webkit-text-size-adjust: 100%;
  text-size-adjust: 100%;
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

/* Notification styles */
.notification-badge {
  display: inline-flex;
  cursor: pointer;
}

.notification-panel {
  max-height: 400px;
  display: flex;
  flex-direction: column;
}

.notification-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 8px;
  border-bottom: 1px solid #ebeef5;
  margin-bottom: 8px;
}

.notification-title {
  font-weight: 600;
  font-size: 14px;
}

.notification-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 24px 0;
  color: #909399;
}

.notification-empty {
  text-align: center;
  padding: 24px 0;
  color: #909399;
  font-size: 13px;
}

.notification-list {
  overflow-y: auto;
  max-height: 340px;
}

.notification-item {
  padding: 8px 4px;
  border-bottom: 1px solid #f2f6fc;
  cursor: pointer;
  transition: background 0.2s;
}

.notification-item:hover {
  background: #f5f7fa;
}

.notification-item.is-unread {
  background: #ecf5ff;
}

.notification-item-title {
  font-size: 13px;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
}

.unread-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #409eff;
  flex-shrink: 0;
}

.notification-item-content {
  font-size: 12px;
  color: #606266;
  margin-top: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.notification-item-time {
  font-size: 11px;
  color: #909399;
  margin-top: 4px;
}
</style>
