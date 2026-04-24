<template>
  <el-dialog
    class="app-form-dialog"
    :model-value="modelValue"
    :title="title"
    :width="resolvedWidth"
    :destroy-on-close="destroyOnClose"
    :close-on-click-modal="closeOnClickModal"
    :close-on-press-escape="closeOnPressEscape"
    v-bind="$attrs"
    @update:model-value="onUpdate"
  >
    <div class="app-form-dialog-content">
      <slot />
    </div>
    <template v-if="$slots.footer" #footer>
      <div class="app-form-dialog-footer">
        <slot name="footer" />
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    modelValue: boolean
    title: string
    size?: 'sm' | 'md' | 'lg' | 'xl'
    width?: string | number
    destroyOnClose?: boolean
    closeOnClickModal?: boolean
    closeOnPressEscape?: boolean
  }>(),
  {
    size: 'md',
    width: undefined,
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

const resolvedWidth = computed(() => props.width ?? sizeWidthMap[props.size])

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>()

const onUpdate = (value: boolean) => {
  emit('update:modelValue', value)
}
</script>

<style scoped>
.app-form-dialog-content {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.app-form-dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

:deep(.app-form-dialog .el-dialog__header) {
  padding: 20px 24px 16px;
}

:deep(.app-form-dialog .el-dialog__body) {
  padding: 24px;
}

:deep(.app-form-dialog .el-dialog__footer) {
  padding: 16px 24px 20px;
}

:deep(.app-form-dialog .el-dialog__title) {
  font-size: 18px;
  font-weight: 700;
  color: #1f2937;
}
</style>
