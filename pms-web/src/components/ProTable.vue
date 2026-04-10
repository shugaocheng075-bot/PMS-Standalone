<template>
  <div class="pro-table-wrapper">
    <!-- Search Form Area -->
    <div class="filter-card pro-card" v-if="$slots.search">
      <slot name="search"></slot>
    </div>

    <!-- Table Details Area -->
    <div class="table-card pro-card">
      <!-- Toolbar Area -->
      <div class="pro-table-toolbar" v-if="title || $slots.toolbar">
        <div class="toolbar-left">
          <span class="toolbar-title" v-if="title">{{ title }}</span>
        </div>
        <div class="toolbar-right">
          <slot name="toolbar"></slot>
          <el-divider direction="vertical" v-if="$slots.toolbar" />
          <el-tooltip content="刷新数据" placement="top">
            <el-button circle @click="$emit('refresh')">
              <el-icon><Refresh /></el-icon>
            </el-button>
          </el-tooltip>
        </div>
      </div>

      <!-- Main Table -->
      <div class="pro-table-content" v-loading="loading">
        <el-table :data="data" v-bind="$attrs" style="width: 100%" :border="false">
          <!-- Inject Columns -->
          <slot></slot>
          <!-- Empty State -->
          <template #empty>
            <div class="empty-state">
              <el-empty :image-size="80" description="暂无数据" />
            </div>
          </template>
        </el-table>
      </div>

      <!-- Pagination Area -->
      <div class="pager" v-if="showPagination">
        <el-pagination
          background
          :current-page="page"
          :page-size="size"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @update:current-page="$emit('update:page', $event)"
          @update:page-size="$emit('update:size', $event)"
          @change="$emit('pagination-change')"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Refresh } from '@element-plus/icons-vue'

defineOptions({ name: 'ProTable' })

const props = withDefaults(defineProps<{
  title?: string
  loading?: boolean
  data?: any[]
  showPagination?: boolean
  total?: number
  page?: number
  size?: number
}>(), {
  title: '',
  loading: false,
  data: () => [],
  showPagination: true,
  total: 0,
  page: 1,
  size: 10
})

defineEmits(['update:page', 'update:size', 'pagination-change', 'refresh'])
</script>

<style scoped>
.pro-table-wrapper {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.pro-card {
  border-radius: var(--pms-card-border-radius);
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-light);
  transition: box-shadow 0.2s ease, border-color 0.2s ease;
}
.pro-card:hover {
  box-shadow: var(--pms-card-shadow);
}

.filter-card {
  padding: 20px 24px 4px 24px;
}

.table-card {
  padding: 0;
  overflow: hidden;
}

.pro-table-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 24px;
  border-bottom: 1px solid var(--el-border-color-light);
}

.toolbar-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.toolbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.pro-table-content {
  padding: 0;
}

.pro-table-content :deep(.el-table) {
  --el-table-border-color: transparent !important;
}

.pro-table-content :deep(.el-table th.el-table__cell) {
  background-color: #f7f8fa;
  color: #1d2129;
  font-weight: 600;
  height: 48px;
  border-bottom: 1px solid #e5e6eb;
}

.pro-table-content :deep(.el-table td.el-table__cell) {
  border-bottom: 1px solid #f2f3f5;
  padding: 10px 16px;
}

/* Hide all ugly native borders */
.pro-table-content :deep(.el-table--border .el-table__inner-wrapper::after),
.pro-table-content :deep(.el-table--border::after),
.pro-table-content :deep(.el-table--border::before),
.pro-table-content :deep(.el-table__inner-wrapper::before) {
  display: none !important;
}

.empty-state {
  padding: 60px 0;
}

.pager {
  padding: 16px 24px;
  border-top: 1px solid var(--el-border-color-light);
  margin-top: 0;
  display: flex;
  justify-content: flex-end;
}
</style>