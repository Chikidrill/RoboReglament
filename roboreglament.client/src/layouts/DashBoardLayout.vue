
<script setup lang="ts">
import { RouterView } from 'vue-router'

import AppHeader from '@/components/layout/AppHeader.vue'
import DashboardSidebar from '@/components/layout/DashboardSidebar.vue'
</script>

<template>
  <div class="dashboard-layout">
    <AppHeader />

    <div class="dashboard-layout__body">
      <DashboardSidebar />

      <main class="dashboard-layout__content">
        <RouterView />
      </main>
    </div>
  </div>
</template>
