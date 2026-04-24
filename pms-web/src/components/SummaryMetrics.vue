<template>
  <div class="metrics-grid" :class="gridClass">
    <component
      :is="item.clickable === false ? 'div' : 'button'"
      v-for="item in items"
      :key="item.key ?? item.title"
      :type="item.clickable === false ? undefined : 'button'"
      class="metric-card"
      :class="{
        'metric-card--action': item.clickable !== false,
        'is-active': item.active,
      }"
      @click="onSelect(item)"
    >
      <div class="metric-card-head">
        <span class="metric-title">{{ item.title }}</span>
        <span v-if="item.context" class="metric-context">{{ item.context }}</span>
      </div>
      <div class="metric-value" :style="item.color ? { color: item.color } : undefined">{{ item.value }}</div>
      <div v-if="item.note" class="metric-note">{{ item.note }}</div>
    </component>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

type SummaryMetricItem = {
  key?: string | number
  title: string
  value: string | number
  context?: string
  note?: string
  color?: string
  active?: boolean
  clickable?: boolean
}

const props = withDefaults(defineProps<{
  items: SummaryMetricItem[]
  columns?: 4 | 5 | 6
}>(), {
  columns: 4,
})

const emit = defineEmits<{
  select: [item: SummaryMetricItem]
}>()

const gridClass = computed(() => `metrics-grid--${props.columns}`)

const onSelect = (item: SummaryMetricItem) => {
  if (item.clickable === false) {
    return
  }

  emit('select', item)
}
</script>