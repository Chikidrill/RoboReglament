
<script setup lang="ts">
import { RouterView } from 'vue-router'
import AppHeader from '@/components/layout/AppHeader.vue'
</script>

<template>
  <div class="app-layout">
    <AppHeader />

    <main class="page-container main-content">
      <RouterView />
    </main>
  </div>
</template>
