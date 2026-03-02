<template>
  <div class="app-shell">
    <header class="top-nav">
      <div class="nav-left">
        <el-button v-if="isMobileViewport" link class="mobile-menu-btn" @click="mobileMenuVisible = true">
          <el-icon><Menu /></el-icon>
        </el-button>
        <div class="company-main">北京嘉和美康信息技术有限公司</div>
        <div class="company-sub">Goodwill Information Technology Co., Ltd.</div>
      </div>

      <div class="nav-right">
        <el-icon><Bell /></el-icon>
        <el-icon><User /></el-icon>
        <el-select
          v-model="selectedPersonnelId"
          size="small"
          class="user-select"
          :loading="actorLoading"
          filterable
          placeholder="选择人员"
          @change="onChangePersonnel"
        >
          <el-option
            v-for="actor in actorOptions"
            :key="actor.personnelId"
            :label="`${actor.personnelName}（${actor.roleType}）`"
            :value="actor.personnelId"
          />
        </el-select>
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
              <el-menu-item v-for="item in group.items" :key="item.path" :index="item.path">{{ item.label }}</el-menu-item>
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
            >{{ item.label }}</el-menu-item>
          </el-menu>
        </div>
        <div v-if="filteredSideMenuGroups.length === 0" class="empty-menu-tip">未找到匹配功能</div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Bell, Menu, User } from '@element-plus/icons-vue'
import { logout } from '../api/modules/auth'
import { fetchAccessActors } from '../api/modules/access'
import { useAccessControl } from '../composables/useAccessControl'
import { clearAuthState } from '../constants/access'

const route = useRoute()
const router = useRouter()
const activePath = computed(() => route.path)
const searchKeyword = ref('')
const mobileMenuVisible = ref(false)
const isMobileViewport = ref(false)
const actorLoading = ref(false)
const actorOptions = ref<Array<{ personnelId: number; personnelName: string; roleType: string }>>([])

const access = useAccessControl()
const selectedPersonnelId = ref(access.currentPersonnelId.value)

watch(access.currentPersonnelId, (value) => {
  selectedPersonnelId.value = value
})

const displayUserName = computed(() => {
  const profile = access.accessProfile.value
  if (!profile) {
    return ''
  }

  const adminLabel = profile.isAdmin ? '管理员' : profile.roleType
  return `${profile.personnelName}（${adminLabel}）`
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

const loadActors = async () => {
  actorLoading.value = true
  try {
    const res = await fetchAccessActors()
    actorOptions.value = res.data
    if (!actorOptions.value.some((item) => item.personnelId === selectedPersonnelId.value) && actorOptions.value.length > 0) {
      selectedPersonnelId.value = actorOptions.value[0]!.personnelId
      access.setCurrentPersonnelId(selectedPersonnelId.value)
      await access.ensureAccessProfileLoaded(true)
    }
  } finally {
    actorLoading.value = false
  }
}

const onChangePersonnel = async (value: number) => {
  access.setCurrentPersonnelId(value)
  await access.ensureAccessProfileLoaded(true)

  if (!access.canAccessPath(route.path)) {
    await router.push(access.getFirstAccessiblePath())
  }
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
    await loadActors()
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
  min-height: 100vh;
  background: #f1f4fb;
}

.top-nav {
  min-height: 44px;
  background: #2f3a96;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 12px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.16);
}

.nav-left {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.nav-left > div {
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
}

.company-sub {
  margin-top: 1px;
  font-size: 10px;
  opacity: 0.9;
}

.nav-right {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
}

.user-name {
  font-size: 12px;
}

.logout-btn {
  color: #fff;
}

.user-select {
  width: 168px;
}

.workspace {
  display: flex;
  min-height: calc(100vh - 44px);
}

.left-pane {
  width: 232px;
  background: #eef2ff;
  border-right: 1px solid #d6def5;
  padding: 10px 10px 14px;
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
  color: #5f6e9b;
}

.section-title {
  position: relative;
  margin: 4px 0 8px;
  padding-left: 8px;
  font-size: 13px;
  font-weight: 600;
  color: #2b3a67;
}

.section-title::before {
  content: '';
  position: absolute;
  left: 0;
  top: 3px;
  width: 3px;
  height: 14px;
  border-radius: 2px;
  background: #2fb96f;
}

.side-menu {
  border: none;
  background: transparent;
}

.side-menu :deep(.el-menu-item) {
  margin-bottom: 6px;
  border-radius: 6px;
  border: 1px solid #d5ddf1;
  background: #fff;
  color: #2e3b63;
  height: 38px;
  line-height: 38px;
}

.side-menu :deep(.el-menu-item.is-active) {
  color: #2457d6;
  background: #eaf1ff;
  border-color: #9eb7f7;
}

.main-pane {
  flex: 1;
  padding: 14px;
  overflow: auto;
}

.content-wrap {
  width: 100%;
  min-height: calc(100vh - 72px);
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

  .user-name {
    display: none;
  }

  .workspace {
    min-height: calc(100vh - 52px);
  }

  .main-pane {
    width: 100%;
    padding: 10px;
  }

  .content-wrap {
    min-height: calc(100vh - 62px);
  }
}
</style>
