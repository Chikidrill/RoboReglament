
<script setup lang="ts">
  import { computed, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'

  import {
    dashboardRoles,
    useDashboardStore,
  } from '@/stores/dashboard'

  import type { DashboardRole } from '@/stores/dashboard'

  const route = useRoute()
  const router = useRouter()
  const dashboardStore = useDashboardStore()

  const activeRoleLabel = computed(() =>
    dashboardRoles.find(
      role => role.value === dashboardStore.activeRole,
    )?.label ?? 'Личный кабинет',
  )

  const isAdministratorWorkspace = computed(
    () => dashboardStore.activeRole === 'Administrator',
  )

  function changeWorkspace(event: Event) {
    const target = event.target as HTMLSelectElement
    const role = target.value as DashboardRole

    dashboardStore.setRole(role)

    // После переключения открываем обзор
    // выбранного рабочего кабинета.
    void router.push({ name: 'dashboard-overview' })
  }

  // При прямом открытии /admin автоматически
  // выбираем административный кабинет.
  watch(
    () => route.path,
    path => {
      if (
        path === '/admin' ||
        path.startsWith('/admin/')
      ) {
        dashboardStore.setRole('Administrator')
      }
    },
    { immediate: true },
  )
</script>

<template>
  <aside class="dashboard-sidebar">
    <div class="dashboard-sidebar__inner">
      <div class="dashboard-sidebar__workspace">
        <p class="dashboard-sidebar__title">
          РАБОЧИЙ КАБИНЕТ
        </p>

        <label v-if="dashboardStore.availableRoles.length > 1"
               class="dashboard-sidebar__role-label"
               for="dashboard-role">
          Выбранная роль
        </label>

        <select v-if="dashboardStore.availableRoles.length > 1"
                id="dashboard-role"
                class="dashboard-sidebar__role-select"
                :value="dashboardStore.activeRole ?? ''"
                @change="changeWorkspace">
          <option v-for="role in dashboardStore.availableRoles"
                  :key="role.value"
                  :value="role.value">
            {{ role.label }}
          </option>
        </select>

        <div v-else
             class="dashboard-sidebar__role-static">
          {{ activeRoleLabel }}
        </div>
      </div>

      <nav class="dashboard-sidebar__nav"
           aria-label="Навигация рабочего кабинета">
        <RouterLink to="/account/workspace"
                    class="dashboard-sidebar__link"
                    exact-active-class="dashboard-sidebar__link--active">
          <span aria-hidden="true">▦</span>
          Обзор
        </RouterLink>

        <RouterLink to="/account"
                    class="dashboard-sidebar__link"
                    exact-active-class="dashboard-sidebar__link--active">
          <span aria-hidden="true">◯</span>
          Профиль
        </RouterLink>

        <RouterLink v-if="isAdministratorWorkspace"
                    to="/admin"
                    class="dashboard-sidebar__link"
                    exact-active-class="dashboard-sidebar__link--active">
          <span aria-hidden="true">♙</span>
          Пользователи
        </RouterLink>
      </nav>
    </div>
  </aside>
</template>
