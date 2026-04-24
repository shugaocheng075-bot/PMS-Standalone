<template>
  <el-card shadow="never" class="table-card app-table-card" v-bind="$attrs">
    <slot />
  </el-card>
</template>

<style scoped>
.app-table-card {
  overflow: hidden;
  border-radius: 16px;
  border: 1px solid #e7ebf0;
  box-shadow: 0 10px 24px rgba(15, 23, 42, 0.04);
}

.app-table-card :deep(.el-card__header) {
  padding: 14px 16px 0;
  border-bottom: none;
  background: transparent;
}

.app-table-card :deep(.el-card__body) {
  padding: 14px 16px 16px;
}

@media (max-width: 992px) {
  .app-table-card :deep(.el-card__header),
  .app-table-card :deep(.el-card__body) {
    padding-left: 18px;
    padding-right: 18px;
  }
}
</style>
