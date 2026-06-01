<template>
  <el-card shadow="never" class="table-card app-table-card" v-bind="$attrs">
    <slot />
  </el-card>
</template>

<style scoped>
.app-table-card {
  overflow: hidden;
  border-radius: var(--pms-radius-lg, 8px);
  border: 1px solid var(--pms-border, #e5e7eb);
  box-shadow: var(--pms-shadow-card, 0 1px 2px rgba(15, 23, 42, 0.04));
}

.app-table-card :deep(.el-card__header) {
  padding: 14px 16px 0;
  border-bottom: none;
  background: transparent;
}

.app-table-card :deep(.el-card__body) {
  padding: 16px;
}

@media (max-width: 992px) {
  .app-table-card :deep(.el-card__header),
  .app-table-card :deep(.el-card__body) {
    padding-left: 14px;
    padding-right: 14px;
  }
}
</style>
