<template>
  <div class="app-shell">
    <header class="top-nav">
      <div class="nav-left">
        <div class="company-main">北京嘉和美康信息技术有限公司</div>
        <div class="company-sub">Goodwill Information Technology Co., Ltd.</div>
      </div>

      <nav class="nav-center">
        <a class="nav-link" :class="{ active: activeTop === 'home' }" @click.prevent="goTo('/dashboard')">首页</a>
        <a class="nav-link" :class="{ active: activeTop === 'project' }" @click.prevent="goTo('/project/list')">项目管理</a>
        <a class="nav-link" :class="{ active: activeTop === 'service' }" @click.prevent="goTo('/handover/list')">运营管理</a>
      </nav>

      <div class="nav-right">
        <el-icon><Bell /></el-icon>
        <el-icon><User /></el-icon>
        <span class="user-name">部门经理</span>
      </div>
    </header>

    <div class="workspace">
      <aside class="left-pane">
        <div class="left-search">
          <el-input placeholder="搜索功能（名称/拼音首字母）" size="small" clearable />
        </div>

        <div class="menu-section">
          <div class="section-title">常用功能</div>
          <div class="quick-card" @click="goTo(quickEntry.path)">{{ quickEntry.label }}</div>
        </div>

        <div class="menu-section">
          <div class="section-title">{{ sectionTitle }}</div>
          <el-menu :default-active="activePath" router class="side-menu">
            <el-menu-item v-for="item in sideMenus" :key="item.path" :index="item.path">{{ item.label }}</el-menu-item>
          </el-menu>
        </div>
      </aside>

      <main class="main-pane">
        <div class="content-wrap">
          <router-view />
        </div>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Bell, User } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const activePath = computed(() => route.path)

const activeTop = computed(() => {
  if (route.path.startsWith('/project') || route.path.startsWith('/contract')) return 'project'
  if (route.path.startsWith('/handover') || route.path.startsWith('/inspection') || route.path.startsWith('/annual-report') || route.path.startsWith('/hospital') || route.path.startsWith('/personnel') || route.path.startsWith('/product') || route.path.startsWith('/maintenance')) return 'service'
  return 'home'
})

const sideMenuMap: Record<string, Array<{ label: string; path: string }>> = {
  home: [
    { label: '驾驶舱', path: '/dashboard' },
    { label: '综合设计方案', path: '/docs/design-spec' },
  ],
  project: [
    { label: '项目台账', path: '/project/list' },
    { label: '合同预警', path: '/contract/alerts' },
  ],
  service: [
    { label: '交接管理', path: '/handover/list' },
    { label: '巡检计划', path: '/inspection/plan' },
    { label: '年度报告', path: '/annual-report/list' },
    { label: '医院管理', path: '/hospital/list' },
    { label: '人员管理', path: '/personnel/list' },
    { label: '产品管理', path: '/product/list' },
    { label: '数据维护中心', path: '/maintenance/data' },
  ],
}

const sideMenus = computed(() => sideMenuMap[activeTop.value] ?? sideMenuMap.home)

const sectionTitle = computed(() => {
  if (activeTop.value === 'project') return '项目管理'
  if (activeTop.value === 'service') return '运营管理'
  return '首页功能'
})

const quickEntry = computed(() => {
  if (activeTop.value === 'project') return { label: '项目台账', path: '/project/list' }
  if (activeTop.value === 'service') return { label: '交接管理', path: '/handover/list' }
  return { label: '驾驶舱', path: '/dashboard' }
})

const goTo = (path: string) => {
  router.push(path)
}
</script>

<style scoped>
.app-shell {
  min-height: 100vh;
  background: #f1f4fb;
}

.top-nav {
  height: 44px;
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
  flex-direction: column;
  line-height: 1.05;
}

.company-main {
  font-size: 12px;
  font-weight: 600;
}

.company-sub {
  margin-top: 1px;
  font-size: 10px;
  opacity: 0.9;
}

.nav-center {
  display: flex;
  align-items: center;
  height: 100%;
  gap: 2px;
}

.nav-link {
  height: 100%;
  padding: 0 18px;
  display: inline-flex;
  align-items: center;
  color: rgba(255, 255, 255, 0.86);
  font-size: 13px;
  text-decoration: none;
  cursor: pointer;
  border-bottom: 3px solid transparent;
}

.nav-link:hover {
  color: #fff;
  background: rgba(255, 255, 255, 0.08);
}

.nav-link.active {
  color: #fff;
  border-bottom-color: #65d6ff;
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

.quick-card {
  border: 1px solid #d5ddf1;
  background: #fff;
  border-radius: 6px;
  padding: 10px 12px;
  font-size: 13px;
  color: #2e3b63;
  cursor: pointer;
}

.quick-card:hover {
  border-color: #88a0e8;
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
</style>
