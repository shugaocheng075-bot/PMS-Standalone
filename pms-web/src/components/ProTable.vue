<template>
  <div class="pro-table-wrapper">
    <div class="filter-card pro-card" v-if="$slots.search">
      <slot name="search"></slot>
    </div>

    <div class="table-card pro-card">
      <div class="pro-table-toolbar" v-if="title || $slots.toolbar">
        <div class="toolbar-left">
          <span class="toolbar-title" v-if="title">{{ title }}</span>
          <span class="toolbar-meta" v-if="showPagination">共 {{ total }} 条</span>
        </div>
        <div class="toolbar-right">
          <slot name="toolbar"></slot>
          <el-divider direction="vertical" v-if="$slots.toolbar" />
          <el-tooltip content="刷新数据" placement="top">
            <el-button circle class="refresh-button" @click="$emit('refresh')">
              <el-icon><Refresh /></el-icon>
            </el-button>
          </el-tooltip>
        </div>
      </div>

      <div class="pro-table-content" v-loading="loading">
        <el-table :data="data" v-bind="$attrs" style="width: 100%" :border="false">
          <slot></slot>
          <template #empty>
            <div class="empty-state">
              <el-empty :image-size="80" description="暂无数据" />
            </div>
          </template>
        </el-table>
      </div>

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
  gap: 12px;
}

.pro-card {
  position: relative;
  overflow: hidden;
  border-radius: var(--pms-radius-lg, 8px);
  background: var(--pms-surface, #ffffff);
  border: 1px solid var(--pms-border, #e5e7eb);
  box-shadow: var(--pms-shadow-card, 0 1px 2px rgba(15, 23, 42, 0.04));
}

.filter-card {
  padding: 16px 16px 0;
}

.table-card {
  padding: 0;
  overflow: hidden;
}

.pro-table-toolbar {
  display: flex;
  position: relative;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px;
  border-bottom: 1px solid var(--pms-border-soft, #eef2f6);
  background: var(--pms-surface, #ffffff);
}

.toolbar-left,
.toolbar-right {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
}

.toolbar-left {
  gap: 8px;
}

.toolbar-title {
  display: inline-flex;
  align-items: center;
  font-size: 14px;
  font-weight: 700;
  color: var(--pms-text, #111827);
}

.toolbar-title::before {
  display: none;
}

.toolbar-meta {
  display: inline-flex;
  align-items: center;
  padding: 3px 8px;
  border-radius: 999px;
  background: var(--pms-surface-muted, #f8fafc);
  color: var(--pms-text-muted, #6b7280);
  font-size: 11px;
  font-weight: 600;
}

.toolbar-right {
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 8px;
}

.pro-table-content {
  position: relative;
}

.pro-table-content :deep(.el-table) {
  --el-table-border-color: var(--pms-border-soft, #eef2f6) !important;
  background: transparent;
}

.pro-table-content :deep(.el-table th.el-table__cell) {
  background: var(--pms-surface-muted, #f8fafc);
  color: var(--pms-text, #111827);
  font-weight: 600;
  height: 40px;
  padding: 6px 12px;
  border-bottom: 1px solid var(--pms-border, #e5e7eb);
}

.pro-table-content :deep(.el-table td.el-table__cell) {
  border-bottom: 1px solid var(--pms-border-soft, #eef2f6);
  padding: 8px 12px;
}

.pro-table-content :deep(.el-table td.el-table__cell .cell) {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  word-break: keep-all;
  line-height: 1.35;
}

.pro-table-content :deep(.el-table--border .el-table__inner-wrapper::after),
.pro-table-content :deep(.el-table--border::after),
.pro-table-content :deep(.el-table--border::before),
.pro-table-content :deep(.el-table__inner-wrapper::before) {
  display: none !important;
}

.empty-state {
  margin: 16px;
  padding: 28px 0;
  border-radius: var(--pms-radius-lg, 8px);
  border: 1px dashed var(--pms-border, #e5e7eb);
  background: var(--pms-surface-muted, #f8fafc);
}

.pager {
  padding: 12px 16px 14px;
  border-top: 1px solid var(--pms-border-soft, #eef2f6);
  margin-top: 0;
  display: flex;
  justify-content: flex-end;
}

.refresh-button {
  border-color: var(--pms-border, #e5e7eb);
  background: var(--pms-surface, #ffffff);
}

.refresh-button:hover {
  color: var(--pms-primary, #2563eb);
  border-color: var(--pms-primary-soft, #bfd4ff);
}

@media (max-width: 992px) {
  .filter-card {
    padding: 14px 14px 0;
  }

  .pro-table-toolbar {
    padding: 14px 14px 10px;
  }

  .toolbar-right {
    width: 100%;
    justify-content: space-between;
  }

  .pager {
    padding: 12px 14px 14px;
    justify-content: flex-start;
  }
}
</style>
