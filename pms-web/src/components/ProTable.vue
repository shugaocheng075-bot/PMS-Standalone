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
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.94);
  border: 1px solid #e7ebf0;
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.04);
}

.filter-card {
  padding: 14px 16px 0;
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
  padding: 14px 16px 10px;
  border-bottom: 1px solid #eef2f6;
  background: rgba(255, 255, 255, 0.96);
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
  color: #1f2937;
}

.toolbar-title::before {
  content: "";
  width: 4px;
  height: 16px;
  border-radius: 999px;
  background: #165dff;
}

.toolbar-meta {
  display: inline-flex;
  align-items: center;
  padding: 3px 8px;
  border-radius: 999px;
  background: #f8fafc;
  color: #64748b;
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
  --el-table-border-color: #edf2f7 !important;
  background: transparent;
}

.pro-table-content :deep(.el-table th.el-table__cell) {
  background: #f8fafc;
  color: #334155;
  font-weight: 600;
  height: 40px;
  padding: 6px 12px;
  border-bottom: 1px solid #e9eef5;
}

.pro-table-content :deep(.el-table td.el-table__cell) {
  border-bottom: 1px solid #eef2f6;
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
  border-radius: 14px;
  border: 1px dashed #dbe4ee;
  background: #f8fafc;
}

.pager {
  padding: 12px 16px 14px;
  border-top: 1px solid #eef2f6;
  margin-top: 0;
  display: flex;
  justify-content: flex-end;
}

.refresh-button {
  border-color: #dbe3ed;
  background: #ffffff;
}

.refresh-button:hover {
  color: #165dff;
  border-color: #bfd4ff;
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