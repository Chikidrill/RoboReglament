
<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'

import { getTournamentById } from '@/api/tournaments'

import type { Tournament } from '@/types/tournament'

import {
  formatTournamentDateRange,
  formatDateTime,
  getTournamentStatus,
} from '@/utils/tournament'

const route = useRoute()

const tournament = ref<Tournament | null>(null)
const isLoading = ref(true)
const error = ref('')

const statusInfo = computed(() =>
  tournament.value
    ? getTournamentStatus(tournament.value)
    : { label: '', tone: 'gray' },
)

const canApply = computed(() =>
  tournament.value?.status === 'Registration' &&
  statusInfo.value.label === 'Регистрация открыта',
)

async function loadTournament(
  idParam: unknown,
  signal: AbortSignal,
) {
  tournament.value = null
  error.value = ''
  isLoading.value = true

  const id = typeof idParam === 'string'
    ? Number(idParam)
    : NaN

  if (!Number.isInteger(id) || id <= 0) {
    error.value = 'Некорректный идентификатор турнира.'
    isLoading.value = false
    return
  }

  try {
    tournament.value = await getTournamentById(id, signal)
  } catch (err) {
    if (!signal.aborted) {
      error.value =
        err instanceof Error
          ? err.message
          : 'Не удалось загрузить турнир.'
    }
  } finally {
    if (!signal.aborted) {
      isLoading.value = false
    }
  }
}

watch(
  () => route.params.id,
  (id, _oldId, onCleanup) => {
    const controller = new AbortController()

    onCleanup(() => controller.abort())

    void loadTournament(id, controller.signal)
  },
  { immediate: true },
)
</script>

<template>
  <section class="tournament-details">
    <RouterLink to="/tournaments" class="tournament-details__back">
      ← Все турниры
    </RouterLink>

    <div v-if="isLoading" class="details-message" role="status">
      Загрузка турнира...
    </div>

    <div v-else-if="error" class="details-message" role="alert">
      {{ error }}
    </div>

    <template v-else-if="tournament">
      <div class="tournament-details__hero">
        <span class="details-status"
              :class="`details-status--${statusInfo.tone}`">
          {{ statusInfo.label }}
        </span>

        <h1>{{ tournament.name }}</h1>

        <p class="tournament-details__location">
          ⌖ {{ tournament.location || 'Место не указано' }}
        </p>

        <p class="tournament-details__dates">
          {{
 formatTournamentDateRange(
            tournament.startDate,
            tournament.endDate,
          )
          }}
        </p>
      </div>

      <div class="tournament-details__grid">
        <div class="tournament-details__main">
          <h2>О турнире</h2>

          <p class="tournament-details__description">
            {{ tournament.description || 'Описание пока не добавлено.' }}
          </p>
        </div>

        <aside class="tournament-details__aside">
          <h2>Участие в турнире</h2>

          <div class="details-field">
            <span>Даты проведения</span>
            <strong>
              {{
 formatTournamentDateRange(
                tournament.startDate,
                tournament.endDate,
              )
              }}
            </strong>
          </div>

          <div class="details-field">
            <span>Приём заявок до</span>
            <strong>
              {{
 tournament.registrationDeadline
                ? formatDateTime(tournament.registrationDeadline)
                : 'Срок не указан'
              }}
            </strong>
          </div>

          <RouterLink v-if="canApply"
                      to="/login"
                      class="btn btn-primary tournament-details__button">
            Войти, чтобы подать заявку →
          </RouterLink>

          <p v-else class="tournament-details__notice">
            Подача заявок сейчас недоступна.
          </p>
        </aside>
      </div>
    </template>
  </section>
</template>

