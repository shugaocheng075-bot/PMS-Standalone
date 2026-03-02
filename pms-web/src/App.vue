<template>
  <LoginView v-if="isLoginRoute" />
  <AppLayout v-else />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppLayout from './layout/AppLayout.vue'
import LoginView from './views/login/LoginView.vue'

const route = useRoute()
const isLoginRoute = computed(() => route.path === '/login')
</script>
