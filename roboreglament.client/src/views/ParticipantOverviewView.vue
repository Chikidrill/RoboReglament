
<script setup lang="ts">import { onMounted, ref } from 'vue'
import { getParticipantOverview } from '@/api/participant-overview'
import type { ParticipantOverview } from '@/types/participant-overview'
import '@/styles/pages/participant-overview.css'
const overview = ref<ParticipantOverview | null>(null)
const loading = ref(false)
const error = ref('')

const applicationStatuses: Record<string, string> = {
  Draft: 'Черновик',
  Pending: 'На рассмотрении',
  Approved: 'Одобрена',
  Rejected: 'Отклонена',
  Withdrawn: 'Отозвана',
}

const matchStatuses: Record<string, string> = {
  Scheduled: 'Запланирован',
  InProgress: 'В процессе',
  Completed: 'Завершён',
  Cancelled: 'Отменён',
}

function applicationStatus(value: string): string {
  return applicationStatuses[value] ?? value
}

function matchStatus(value: string): string {
  return matchStatuses[value] ?? value
}

function formatDate(value: string | null): string {
  if (!value) return 'Время не назначено'

  return new Date(value).toLocaleString('ru-RU')
}

async function load() {
  loading.value = true
  error.value = ''

  try {
    overview.value = await getParticipantOverview()
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Не удалось загрузить кабинет участника.'
  } finally {
    loading.value = false
  }
}

onMounted(() => { void load() })</script>

<template>
  <main class="participant-overview">
    <header class="participant-overview__header">
      <div>
        <h1>Моё участие</h1>
        <p>Мои команды, заявки и выступления.</p>
      </div>

      <button class="btn btn-outline"
              type="button"
              :disabled="loading"
              @click="load">
        Обновить
      </button>
    </header>

    <p v-if="error" role="alert">{{ error }}</p>
    <p v-if="loading">Загрузка...</p>

    <template v-if="overview && !loading">
      <section class="participant-overview__section">
        <h2>Мои команды</h2>

        <p v-if="!overview.teams.length">
          Вас пока не добавили ни в одну команду.
        </p>

        <article v-for="team in overview.teams"
                 :key="team.teamId"
                 class="participant-overview__item">
          <strong>{{ team.teamName }}</strong>
          <p>{{ team.organization || 'Организация не указана' }}</p>
        </article>
      </section>

      <section class="participant-overview__section">
        <h2>Мои соревнования</h2>

        <p v-if="!overview.applications.length">
          Заявок с вашим участием пока нет.
        </p>

        <article v-for="application in overview.applications"
                 :key="application.applicationId"
                 class="participant-overview__item">
          
          <RouterLink
            :to="{
              name: 'tournament-details',
              params: { id: application.tournamentId },
            }"
            class="participant-overview__title-link"
          >
            {{ application.tournamentName }}
          </RouterLink>


          <p>
            Дисциплина: {{ application.disciplineName }}
          </p>
          <p>Команда: {{ application.teamName }}</p>
          <p>
            Статус заявки:
            {{ applicationStatus(application.applicationStatus) }}
          </p>
          <p>
            Аккредитация:
            {{ application.isAccredited ? 'Пройдена' : 'Не пройдена' }}
          </p>
        </article>
      </section>

      <section class="participant-overview__section">
        <h2>Мои выступления</h2>

        <p v-if="!overview.matches.length">
          Назначенных выступлений пока нет.
        </p>

        <article v-for="match in overview.matches"
                 :key="`${match.matchId}-${match.teamId}`"
                 class="participant-overview__item">
          <strong>
            {{ match.name || `Выступление №${match.number}` }}
          </strong>

          <p>Дата: {{ formatDate(match.scheduledAt) }}</p>
          <p>Статус: {{ matchStatus(match.status) }}</p>

          
          <RouterLink
            :to="{
              name: 'tournament-details',
              params: { id: match.tournamentId },
            }"
            class="participant-overview__button"
          >
            Перейти к соревнованию
          </RouterLink>

        </article>
      </section>
    </template>
  </main>
</template>
