<template>
  <div class="app-root notranslate" lang="zh-CN" translate="no">
    <LoginView v-if="isLoginRoute" />
    <AppLayout v-else />
  </div>
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'
import { useRoute } from 'vue-router'

const AppLayout = defineAsyncComponent(() => import('./layout/AppLayout.vue'))
const LoginView = defineAsyncComponent(() => import('./views/login/LoginView.vue'))

const route = useRoute()
const isLoginRoute = computed(() => route.path === '/login')
</script>
