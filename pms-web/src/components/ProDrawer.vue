<template>
  <el-drawer
    class="pro-drawer"
    :model-value="modelValue"
    :title="title"
    :size="resolvedWidth"
    :destroy-on-close="destroyOnClose"
    :close-on-click-modal="closeOnClickModal"
    :close-on-press-escape="closeOnPressEscape"
    direction="rtl"
    v-bind="$attrs"
    @update:model-value="onUpdate"
  >
    <div class="pro-drawer-content">
      <slot />
    </div>
    <template v-if="$slots.footer" #footer>
      <div class="pro-drawer-footer">
        <slot name="footer" />
      </div>
    </template>
  </el-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    modelValue: boolean
    title: string
    sizeClass?: 'sm' | 'md' | 'lg' | 'xl'
    size?: string | number
    destroyOnClose?: boolean
    closeOnClickModal?: boolean
    closeOnPressEscape?: boolean
  }>(),
  {
    sizeClass: 'md',
    size: undefined,
    destroyOnClose: true,
    closeOnClickModal: true,
    closeOnPressEscape: true,
  },
)

const sizeWidthMap: Record<'sm' | 'md' | 'lg' | 'xl', string> = {
  sm: '460px',
  md: '560px',
  lg: '680px',
  xl: '760px',
}

const resolvedWidth = computed(() => props.size ?? sizeWidthMap[props.sizeClass])   

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>()

const onUpdate = (value: boolean) => {
  emit('update:modelValue', value)
}
</script>

<style scoped>
.pro-drawer-content {
  display: flex;
  flex-direction: column;
  gap: 20px;
  min-height: 100%;
  padding-right: 4px;
}

.pro-drawer-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 16px;
  border-top: 1px solid #eef2f6;
}

:deep(.pro-drawer .el-drawer__header) {
  padding: 20px 24px 16px;
  border-bottom: 1px solid #eef2f6;
  background: #ffffff;
}

:deep(.pro-drawer .el-drawer__title) {
  font-size: 18px;
  font-weight: 700;
  color: #1f2937;
}

:deep(.pro-drawer .el-drawer__body) {
  padding: 24px;
}

@media (max-width: 992px) {
  :deep(.pro-drawer .el-drawer__body) {
    padding: 18px;
  }
}
</style>
