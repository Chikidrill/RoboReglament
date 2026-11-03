
<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const authStore = useAuthStore()

const isAdministrator = computed(() =>
  authStore.user?.roles.includes('Administrator') ?? false,
)

const isAdminSection = computed(() =>
  route.path === '/admin' ||
  route.path.startsWith('/admin/'),
)

const sectionTitle = computed(() =>
  isAdminSection.value
    ? 'АДМИНИСТРАТОР'
    : 'ЛИЧНЫЙ КАБИНЕТ',
)
</script>

<template>
  <aside class="dashboard-sidebar">
    <div class="dashboard-sidebar__inner">
      <p class="dashboard-sidebar__title">
        {{ sectionTitle }}
      </p>

      <nav class="dashboard-sidebar__nav"
           aria-label="Навигация личного кабинета">
        <RouterLink to="/account"
                    class="dashboard-sidebar__link"
                    exact-active-class="dashboard-sidebar__link--active">
          <span aria-hidden="true">◦</span>
          Профиль
        </RouterLink>

        <RouterLink v-if="isAdministrator"
                    to="/admin"
                    class="dashboard-sidebar__link"
                    exact-active-class="dashboard-sidebar__link--active">
          <span aria-hidden="true">◦</span>
          Пользователи
        </RouterLink>
      </nav>
    </div>
  </aside>
</template>
