
<script setup lang="ts">
import { computed } from 'vue'

import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'

import type { DashboardRole } from '@/stores/dashboard'

const dashboardStore = useDashboardStore()
const authStore = useAuthStore()

interface WorkspaceInfo {
  title: string
  description: string
  futureSections: string[]
}

const workspaces: Record<DashboardRole, WorkspaceInfo> = {
  Participant: {
    title: 'Кабинет участника',
    description:
      'Информация об участии в соревнованиях.',
    futureSections: ['Мои соревнования'],
  },

  Coach: {
    title: 'Кабинет тренера',
    description:
      'Управление командами и заявками на соревнования.',
    futureSections: ['Мои команды', 'Мои заявки'],
  },

  Organizer: {
    title: 'Кабинет организатора',
    description:
      'Организация турниров, обработка заявок и аккредитация.',
    futureSections: [
      'Мои турниры',
      'Заявки',
      'Аккредитация',
    ],
  },

  Judge: {
    title: 'Кабинет судьи',
    description:
      'Работа с судейскими протоколами и результатами.',
    futureSections: [
      'Назначенные соревнования',
      'Протоколы',
    ],
  },

  Administrator: {
    title: 'Кабинет администратора',
    description:
      'Управление пользователями и их ролями.',
    futureSections: [],
  },
}

const currentWorkspace = computed<WorkspaceInfo>(() => {
  const role = dashboardStore.activeRole

  if (role) {
    return workspaces[role]
  }

  return {
    title: 'Личный кабинет',
    description: 'Информация о вашей учётной записи.',
    futureSections: [],
  }
})
</script>

<template>
  <section class="dashboard-overview">
    <div class="dashboard-overview__heading">
      <h1>{{ currentWorkspace.title }}</h1>

      <p>{{ currentWorkspace.description }}</p>
    </div>

    <div class="dashboard-overview__welcome">
      <h2>
        Добро пожаловать,
        {{ authStore.user?.firstName }}!
      </h2>

      <p>
        Используйте боковое меню для перехода
        между доступными разделами.
      </p>
    </div>

    <div class="dashboard-overview__section">
      <h2>Доступные разделы</h2>

      <div class="dashboard-overview__cards">
        <RouterLink to="/account"
                    class="dashboard-overview__card">
          <h3>Мой профиль</h3>
          <p>Личные данные и информация об аккаунте.</p>
          <span>Перейти к профилю →</span>
        </RouterLink>

        <RouterLink v-if="dashboardStore.activeRole === 'Administrator'"
                    to="/admin"
                    class="dashboard-overview__card">
          <h3>Управление пользователями</h3>
          <p>
            Просмотр пользователей и назначение ролей.
          </p>
          <span>Открыть раздел →</span>
        </RouterLink>
      </div>
    </div>
  </section>
</template>
