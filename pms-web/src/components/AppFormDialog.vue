<template>
  <el-dialog
    :model-value="modelValue"
    :title="title"
    :width="resolvedWidth"
    :destroy-on-close="destroyOnClose"
    :close-on-click-modal="closeOnClickModal"
    :close-on-press-escape="closeOnPressEscape"
    v-bind="$attrs"
    @update:model-value="onUpdate"
  >
    <slot />
    <template v-if="$slots.footer" #footer>
      <slot name="footer" />
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
