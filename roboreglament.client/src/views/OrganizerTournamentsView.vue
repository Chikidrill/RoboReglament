
<script setup lang="ts">import {
  computed,
  onMounted,
  onUnmounted,
  ref,
} from 'vue'

import { useRouter } from 'vue-router'

import {
  getMyTournaments,
  createTournament,
} from '@/api/tournaments'

import TournamentEditor
  from '@/components/tournaments/TournamentEditor.vue'

import type {
  Tournament,
  TournamentRequest,
} from '@/types/tournament'

import {
  formatTournamentDateRange,
  getTournamentStatus,
} from '@/utils/tournament'

const router = useRouter()
const controller = new AbortController()

const tournaments = ref<Tournament[]>([])
const isLoading = ref(true)
const isSaving = ref(false)
const showForm = ref(false)
const error = ref('')
const search = ref('')
const statusFilter = ref('all')

const filtered = computed(() =>
  tournaments.value.filter(tournament => {
    const query = search.value.trim().toLowerCase()

    return (
      `${tournament.name} ${tournament.location ?? ''}`
        .toLowerCase().includes(query) &&
      (
        statusFilter.value === 'all' ||
        tournament.status === statusFilter.value
      )
    )
  }),
)

async function load() {
  isLoading.value = true
  error.value = ''

  try {
    tournaments.value = await getMyTournaments(
      controller.signal,
    )
  } catch (err) {
    if (!controller.signal.aborted) {
      error.value = err instanceof Error
        ? err.message
        : 'Не удалось загрузить турниры.'
    }
  } finally {
    isLoading.value = false
  }
}

async function save(data: TournamentRequest) {
  if (isSaving.value) return

  isSaving.value = true
  error.value = ''

  try {
    const created = await createTournament(data)

    await router.push({
      name: 'organizer-tournament-details',
      params: { id: created.id },
    })
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Не удалось создать турнир.'
  } finally {
    isSaving.value = false
  }
}

onMounted(() => void load())
onUnmounted(() => controller.abort())</script>

<template>
  <section class="organizer-page">
    <header class="organizer-heading">
      <div>
        <h1>Мои турниры</h1>
        <p>Создание и управление соревнованиями.</p>
      </div>

      <button v-if="!showForm"
              type="button"
              class="btn btn-primary"
              @click="showForm = true; error = ''">
        + Создать турнир
      </button>
    </header>

    <p v-if="error" class="organizer-error" role="alert">
      {{ error }}
    </p>

    <div v-if="showForm" class="organizer-panel">
      <h2>Создание турнира</h2>

      <TournamentEditor :busy="isSaving"
                        @save="save"
                        @cancel="showForm = false" />
    </div>

    <div class="organizer-toolbar">
      <input v-model="search"
             type="search"
             placeholder="Поиск турниров"
             aria-label="Поиск турниров" />

      <select v-model="statusFilter" aria-label="Статус">
        <option value="all">Все статусы</option>
        <option value="Draft">Черновики</option>
        <option value="Registration">Регистрация</option>
        <option value="InProgress">Идёт турнир</option>
        <option value="Completed">Завершённые</option>
      </select>

      <button type="button"
              class="btn btn-outline"
              :disabled="isLoading || isSaving"
              @click="load">
        Обновить
      </button>
    </div>

    <p v-if="isLoading">Загрузка турниров...</p>

    <div v-else-if="filtered.length" class="organizer-grid">
      <article v-for="tournament in filtered"
               :key="tournament.id"
               class="organizer-panel">
        <span class="status-badge"
              :class="`status-badge--${
            getTournamentStatus(tournament).tone
          }`">
          {{
            tournament.status === 'Draft'
              ? 'Черновик'
              : getTournamentStatus(tournament).label
          }}
        </span>

        <h2>{{ tournament.name }}</h2>

        <p>
          {{ tournament.location || 'Место не указано' }}
        </p>

        <p>
          {{
            formatTournamentDateRange(
              tournament.startDate,
              tournament.endDate,
            )
          }}
        </p>

        <p>Дисциплин: {{ tournament.disciplines.length }}</p>

        <RouterLink :to="{
            name: 'organizer-tournament-details',
            params: { id: tournament.id },
          }"
                    class="btn btn-outline">
          Управление турниром →
        </RouterLink>
      </article>
    </div>

    <p v-else-if="!error" class="organizer-panel">
      {{
        tournaments.length
          ? 'По заданным параметрам ничего не найдено.'
          : 'У вас пока нет турниров.'
      }}
    </p>
  </section>
</template>
