<template>
  <el-drawer
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
  padding-right: 8px; /* For scrollbar */
}
.pro-drawer-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 16px;
  border-top: 1px solid var(--el-border-color-light);
}
</style>
