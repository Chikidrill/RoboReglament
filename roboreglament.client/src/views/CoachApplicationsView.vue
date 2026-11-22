
<script setup lang="ts">import {
  computed,
  onUnmounted,
  onMounted,
  ref,
} from 'vue'

import { getMyApplications } from '@/api/applications'
import { isRegistrationOpen } from '@/utils/application'

import CoachApplicationCard
  from '@/components/tournaments/CoachApplicationCard.vue'

import type {
  CoachApplicationItem,
  TournamentApplication,
} from '@/types/application'

const items = ref<CoachApplicationItem[]>([])

const isLoading = ref(true)
const loadError = ref('')
const successMessage = ref('')

const searchQuery = ref('')
const statusFilter = ref('all')

const controller = new AbortController()

const filteredItems = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()

  return items.value.filter(item => {
    const application = item.application

    const matchesQuery =
      !query ||
      item.tournamentName.toLowerCase().includes(query) ||
      application.teamName.toLowerCase().includes(query) ||
      application.disciplineName.toLowerCase().includes(query)

    const effectiveStatus = application.isAccredited
      ? 'Accredited'
      : application.status

    const matchesStatus =
      statusFilter.value === 'all' ||
      effectiveStatus === statusFilter.value

    return matchesQuery && matchesStatus
  })
})

async function loadApplications() {
  isLoading.value = true
  loadError.value = ''
  successMessage.value = ''

  try {
    const result = await getMyApplications(
      undefined,
      controller.signal,
    )

    if (!controller.signal.aborted) {
      items.value = result
    }
  } catch (err) {
    if (!controller.signal.aborted) {
      loadError.value = err instanceof Error
        ? err.message
        : 'Не удалось загрузить заявки.'
    }
  } finally {
    if (!controller.signal.aborted) {
      isLoading.value = false
    }
  }
}

function replaceApplication(updated: TournamentApplication) {
  items.value = items.value.map(item =>
    item.application.id === updated.id
      ? { ...item, application: updated }
      : item,
  )
}

function removeApplication(applicationId: number) {
  items.value = items.value.filter(
    item => item.application.id !== applicationId,
  )

  successMessage.value = 'Черновик удалён.'
}

onMounted(() => {
  void loadApplications()
})

onUnmounted(() => controller.abort())</script>

<template>
  <section class="coach-my-applications">
    <header class="coach-team-details__heading">
      <div>
        <h1>Мои заявки</h1>

        <p>
          Управление заявками ваших команд
          на соревнования.
        </p>
      </div>

      <RouterLink :to="{ name: 'tournaments' }"
                  class="btn btn-primary">
        + Подать заявку
      </RouterLink>
    </header>

    <div class="coach-my-applications__toolbar">
      <input v-model="searchQuery"
             type="search"
             placeholder="Поиск по турниру или команде"
             aria-label="Поиск заявок" />

      <select v-model="statusFilter"
              aria-label="Статус заявки">
        <option value="all">Все статусы</option>
        <option value="Draft">Черновики</option>
        <option value="Pending">На рассмотрении</option>
        <option value="Approved">Одобренные</option>
        <option value="Rejected">Отклонённые</option>
        <option value="Withdrawn">Отозванные</option>
        <option value="Accredited">Аккредитованные</option>
      </select>

      <button type="button"
              class="btn btn-outline"
              :disabled="isLoading"
              @click="loadApplications">
        Обновить
      </button>
    </div>

    <p v-if="successMessage"
       class="coach-applications__success"
       role="status">
      {{ successMessage }}
    </p>

    <div v-if="isLoading"
         class="coach-team-details__message"
         role="status">
      Загрузка заявок...
    </div>

    <div v-else-if="loadError"
         class="coach-team-details__message"
         role="alert">
      <p>{{ loadError }}</p>

      <button type="button"
              class="btn btn-outline"
              @click="loadApplications">
        Повторить
      </button>
    </div>

    <div v-else-if="items.length === 0"
         class="coach-team-details__message">
      <h3>У вас пока нет заявок</h3>

      <p>
        Выберите соревнование и подайте заявку
        от своей команды.
      </p>

      <RouterLink :to="{ name: 'tournaments' }"
                  class="btn btn-primary">
        Перейти к соревнованиям
      </RouterLink>
    </div>

    <div v-else-if="filteredItems.length === 0"
         class="coach-team-details__message">
      По заданным параметрам заявки не найдены.
    </div>

    <template v-else>
      <p class="coach-my-applications__count">
        Найдено заявок: {{ filteredItems.length }}
      </p>

      <div class="coach-my-applications__grid">
        <div v-for="item in filteredItems"
             :key="item.application.id"
             class="coach-my-applications__entry">
          <RouterLink :to="{
              name: 'tournament-details',
              params: { id: item.application.tournamentId },
            }"
                      class="coach-my-applications__tournament">
            {{ item.tournamentName }} →
          </RouterLink>

          <CoachApplicationCard :application="item.application"
                                :registration-open="
              isRegistrationOpen(
                item.tournamentStatus,
                item.registrationDeadline,
              )
            "
                                @updated="replaceApplication"
                                @removed="removeApplication" />
        </div>
      </div>
    </template>
  </section>
</template>
