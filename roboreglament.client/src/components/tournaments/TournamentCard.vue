
<script setup lang="ts">
import { computed } from 'vue'
import type { Tournament } from '@/types/tournament'
import {
  formatTournamentDateRange,
  getTournamentStatus,
} from '@/utils/tournament'

const props = defineProps<{
  tournament: Tournament
}>()

const statusInfo = computed(() =>
  getTournamentStatus(props.tournament),
)

const startDate = computed(() =>
  new Date(`${props.tournament.startDate}T00:00:00`),
)

const dayLabel = computed(() => {
  const start = startDate.value
  const end = new Date(`${props.tournament.endDate}T00:00:00`)

  if (
    start.getMonth() === end.getMonth() &&
    start.getFullYear() === end.getFullYear() &&
    start.getDate() !== end.getDate()
  ) {
    return `${start.getDate()}–${end.getDate()}`
  }

  return `${start.getDate()}`
})

const monthLabel = computed(() =>
  new Intl.DateTimeFormat('ru-RU', {
    month: 'long',
    year: 'numeric',
  }).format(startDate.value),
)
</script>

<template>
  <article class="tournament-card">
    <div class="tournament-card__date">
      <span class="tournament-card__day">{{ dayLabel }}</span>
      <span class="tournament-card__month">{{ monthLabel }}</span>
    </div>

    <div class="tournament-card__content">
      <h2>{{ tournament.name }}</h2>

      <p class="tournament-card__description">
        {{ tournament.description || 'Описание пока не добавлено' }}
      </p>

      <div class="tournament-card__meta">
        <span>⌖ {{ tournament.location || 'Место не указано' }}</span>
        <span>
          {{ formatTournamentDateRange(tournament.startDate, tournament.endDate) }}
        </span>
      </div>

      <div class="tournament-card__footer">
        <span class="status-badge" :class="`status-badge--${statusInfo.tone}`">
          {{ statusInfo.label }}
        </span>

        <RouterLink :to="{
            name: 'tournament-details',
            params: { id: tournament.id },
          }"
                    class="tournament-card__link">
          Подробнее →
        </RouterLink>
      </div>
    </div>
  </article>
</template>


