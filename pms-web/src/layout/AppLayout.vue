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
            <el-menu :default-active="activePath" router class="side-menu">
              <el-menu-item v-for="item in group.items" :key="item.path" :index="item.path">
                <el-icon class="menu-item-icon"><Menu /></el-icon>
                <span class="menu-item-label">{{ item.label }}</span>
              </el-menu-item>
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
          <el-menu :default-active="activePath" router class="side-menu">
            <el-menu-item
              v-for="item in group.items"
              :key="`mobile-${item.path}`"
              :index="item.path"
              @click="mobileMenuVisible = false"
            >
              <el-icon class="menu-item-icon"><Menu /></el-icon>
              <span class="menu-item-label">{{ item.label }}</span>
            </el-menu-item>
          </el-menu>
        </div>
        <div v-if="filteredSideMenuGroups.length === 0" class="empty-menu-tip">未找到匹配功能</div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Bell, Menu, User } from '@element-plus/icons-vue'
import { logout } from '../api/modules/auth'
import { useAccessControl } from '../composables/useAccessControl'
import { clearAuthState } from '../constants/access'

const route = useRoute()
const router = useRouter()
const activePath = computed(() => route.path)
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

const sideMenuGroups: Array<{ title: string; items: Array<{ label: string; path: string; permission: string }> }> = [
  {
    title: '首页功能',
    items: [
    { label: '首页', path: '/dashboard', permission: 'dashboard.view' },
    { label: '统一预警中心', path: '/alert/center', permission: 'alert-center.view' },
    ],
  },
  {
    title: '项目管理',
    items: [
    { label: '项目台账', path: '/project/list', permission: 'project.view' },
    { label: '待办列表', path: '/complex-model/ms010001-013-001', permission: 'project.view' },
    { label: '合同预警', path: '/contract/alerts', permission: 'contract.view' },
    ],
  },
  {
    title: '运营管理',
    items: [
    { label: '交接管理', path: '/handover/list', permission: 'handover.view' },
    { label: '巡检计划', path: '/inspection/plan', permission: 'inspection.view' },
    { label: '年度报告', path: '/annual-report/list', permission: 'annual-report.view' },
    { label: '医院管理', path: '/hospital/list', permission: 'hospital.view' },
    { label: '权限管理', path: '/permission/manage', permission: 'permission.manage' },
    { label: '产品管理', path: '/product/list', permission: 'product.view' },
    { label: '重大需求', path: '/major-demand/list', permission: 'major-demand.view' },
    { label: '报修记录', path: '/repair/list', permission: 'repair.view' },
    { label: '工时管理', path: '/workhours/list', permission: 'workhours.view' },
    { label: '月度报告', path: '/monthly-report/list', permission: 'monthly-report.view' },
    { label: '数据维护中心', path: '/maintenance/data', permission: 'maintenance.manage' },
    ],
  },
]

const filteredSideMenuGroups = computed(() => {
  const visibleGroups = sideMenuGroups
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => access.canPermission(item.permission)),
    }))
    .filter((group) => group.items.length > 0)

  const keyword = searchKeyword.value.trim().toLowerCase()
  if (!keyword) {
    return visibleGroups
  }

  return visibleGroups
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => {
        const label = item.label.toLowerCase()
        const path = item.path.toLowerCase()
        return label.includes(keyword) || path.includes(keyword)
      }),
    }))
    .filter((group) => group.items.length > 0)
})

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
