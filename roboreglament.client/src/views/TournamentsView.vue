
<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

import { getTournaments } from '@/api/tournaments'
import TournamentCard from '@/components/tournaments/TournamentCard.vue'

import type {
  Tournament,
  TournamentStatus,
} from '@/types/tournament'

const tournaments = ref<Tournament[]>([])

const isLoading = ref(true)
const error = ref('')

const searchQuery = ref('')
const selectedCity = ref('all')
const selectedStatus = ref<TournamentStatus | 'all'>('all')
const selectedDate = ref<'all' | 'upcoming' | 'past'>('all')

const controller = new AbortController()

function getToday(): string {
  const today = new Date()

  const year = today.getFullYear()
  const month = String(today.getMonth() + 1).padStart(2, '0')
  const day = String(today.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

const cities = computed(() => {
  const values = tournaments.value
    .map(t => t.location?.trim())
    .filter((city): city is string => Boolean(city))

  return [...new Set(values)].sort((a, b) =>
    a.localeCompare(b, 'ru'),
  )
})

const filteredTournaments = computed(() => {
  const search = searchQuery.value.trim().toLocaleLowerCase('ru-RU')
  const today = getToday()

  return tournaments.value
    .filter(tournament => {
      const matchesSearch =
        tournament.name.toLocaleLowerCase('ru-RU').includes(search) ||
        (tournament.location ?? '')
          .toLocaleLowerCase('ru-RU')
          .includes(search)

      const matchesCity =
        selectedCity.value === 'all' ||
        tournament.location?.trim() === selectedCity.value

      const matchesStatus =
        selectedStatus.value === 'all' ||
        tournament.status === selectedStatus.value

      const isPast = tournament.endDate < today

      const matchesDate =
        selectedDate.value === 'all' ||
        (selectedDate.value === 'past' && isPast) ||
        (selectedDate.value === 'upcoming' && !isPast)

      return (
        matchesSearch &&
        matchesCity &&
        matchesStatus &&
        matchesDate
      )
    })
    .sort((a, b) => {
      const aPast = a.endDate < today
      const bPast = b.endDate < today

      // Предстоящие турниры показываем выше прошедших.
      if (aPast !== bPast) {
        return aPast ? 1 : -1
      }

      const dateComparison = a.startDate.localeCompare(b.startDate)

      // Прошедшие турниры: от новых к старым.
      if (aPast) {
        return -dateComparison || a.id - b.id
      }

      // Предстоящие: от ближайших к более поздним.
      return dateComparison || a.id - b.id
    })
})

async function loadTournaments() {
  isLoading.value = true
  error.value = ''

  try {
    tournaments.value = await getTournaments(controller.signal)
  } catch (err) {
    if (!controller.signal.aborted) {
      error.value =
        err instanceof Error
          ? err.message
          : 'Не удалось загрузить турниры.'
    }
  } finally {
    if (!controller.signal.aborted) {
      isLoading.value = false
    }
  }
}

function resetFilters() {
  searchQuery.value = ''
  selectedCity.value = 'all'
  selectedStatus.value = 'all'
  selectedDate.value = 'all'
}

onMounted(() => {
  void loadTournaments()
})

onUnmounted(() => {
  controller.abort()
})
</script>

<template>
  <section class="tournaments-page">
    <div class="tournaments-page__heading">
      <h1>Турниры и соревнования</h1>

      <p>
        Выбирайте соревнования и следите за регистрацией команд.
      </p>
    </div>

    <div class="tournaments-filters">
      <label class="tournaments-filter">
        <span>Поиск</span>
        <input v-model="searchQuery"
               type="search"
               placeholder="Название турнира или город" />
      </label>

      <label class="tournaments-filter">
        <span>Даты</span>
        <select v-model="selectedDate">
          <option value="all">Все даты</option>
          <option value="upcoming">Предстоящие и текущие</option>
          <option value="past">Прошедшие</option>
        </select>
      </label>

      <label class="tournaments-filter">
        <span>Город</span>
        <select v-model="selectedCity">
          <option value="all">Все города</option>
          <option v-for="city in cities"
                  :key="city"
                  :value="city">
            {{ city }}
          </option>
        </select>
      </label>

      <label class="tournaments-filter">
        <span>Статус</span>
        <select v-model="selectedStatus">
          <option value="all">Все статусы</option>
          <option value="Draft">Планируется</option>
          <option value="Registration">Регистрация</option>
          <option value="InProgress">Идёт турнир</option>
          <option value="Completed">Завершён</option>
        </select>
      </label>

      <button class="tournaments-filters__reset"
              type="button"
              @click="resetFilters">
        Сбросить
      </button>
    </div>

    <div v-if="isLoading" class="tournaments-message" role="status">
      Загружаем турниры...
    </div>

    <div v-else-if="error" class="tournaments-message" role="alert">
      <p>{{ error }}</p>

      <button class="btn btn-outline" @click="loadTournaments">
        Повторить
      </button>
    </div>

    <template v-else>
      <div class="tournaments-results">
        <span>Найдено турниров: {{ filteredTournaments.length }}</span>
        <span>Сначала ближайшие</span>
      </div>

      <div v-if="filteredTournaments.length > 0"
           class="tournaments-grid">
        <TournamentCard v-for="tournament in filteredTournaments"
                        :key="tournament.id"
                        :tournament="tournament" />
      </div>

      <div v-else class="tournaments-message">
        <h2>Турниры не найдены</h2>
        <p>Попробуйте изменить параметры поиска.</p>

        <button class="btn btn-outline" @click="resetFilters">
          Сбросить фильтры
        </button>
      </div>
    </template>
  </section>
</template>

