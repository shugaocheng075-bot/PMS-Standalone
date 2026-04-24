<template>
  <div class="app-root notranslate" lang="zh-CN" translate="no">
    <router-view v-if="isLoginRoute" />
    <AppLayout v-else />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppLayout from './layout/AppLayout.vue'

const route = useRoute()
const isLoginRoute = computed(() => route.name === 'login')
</script>
