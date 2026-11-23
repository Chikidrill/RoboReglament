
<script setup lang="ts">
  import {
    computed,
    onMounted,
    onUnmounted,
    ref,
  } from 'vue'

  import { getJudgeMatches } from '@/api/judging'

  import type {
    CompetitionMatch,
    MatchStatus,
  } from '@/types/match'

  type Filter = MatchStatus | 'all'

  const matches = ref<CompetitionMatch[]>([])
  const filter = ref<Filter>('all')
  const loading = ref(false)
  const error = ref('')

  let controller: AbortController | null = null

  const filteredMatches = computed(() => {
    const items = filter.value === 'all'
      ? matches.value
      : matches.value.filter(
        item => item.status === filter.value,
      )

    return [...items].sort((a, b) => {
      const priority: Record<MatchStatus, number> = {
        InProgress: 0,
        Scheduled: 1,
        Completed: 2,
        Cancelled: 3,
      }

      return (
        priority[a.status] - priority[b.status] ||
        (a.scheduledAt ?? '').localeCompare(
          b.scheduledAt ?? '',
        ) ||
        a.number - b.number
      )
    })
  })

  const activeCount = computed(() =>
    matches.value.filter(
      item => item.status === 'InProgress',
    ).length,
  )

  const scheduledCount = computed(() =>
    matches.value.filter(
      item => item.status === 'Scheduled',
    ).length,
  )

  async function load() {
    controller?.abort()

    const request = new AbortController()
    controller = request

    loading.value = true
    error.value = ''

    try {
      const result = await getJudgeMatches(request.signal)

      if (!request.signal.aborted) {
        matches.value = result
      }
    } catch (err) {
      if (!request.signal.aborted) {
        error.value = err instanceof Error
          ? err.message
          : 'Не удалось загрузить выступления.'
      }
    } finally {
      if (!request.signal.aborted) {
        loading.value = false
      }
    }
  }

  function statusText(status: MatchStatus): string {
    switch (status) {
      case 'Scheduled': return 'Запланировано'
      case 'InProgress': return 'Проводится'
      case 'Completed': return 'Завершено'
      case 'Cancelled': return 'Отменено'
    }
  }

  function displayName(match: CompetitionMatch): string {
    if (match.name) return match.name

    return match.format === 'Run'
      ? `Заезд №${match.number}`
      : `Матч №${match.number}`
  }

  function dateText(value: string | null): string {
    return value
      ? new Date(value).toLocaleString('ru-RU')
      : 'Время не назначено'
  }

  onMounted(() => {
    void load()
  })

  onUnmounted(() => {
    controller?.abort()
  })
</script>

<template>
  <section class="judge-page">
    <header class="judge-page__heading">
      <div>
        <h1>Мои выступления</h1>
        <p>
          Назначенные матчи и заезды.
          Здесь можно перейти к судейским протоколам.
        </p>
      </div>

      <button type="button"
              class="btn btn-outline"
              :disabled="loading"
              @click="load">
        Обновить
      </button>
    </header>

    <div class="judge-stats">
      <div>
        <span>Всего назначено</span>
        <strong>{{ matches.length }}</strong>
      </div>

      <div>
        <span>Проводятся</span>
        <strong>{{ activeCount }}</strong>
      </div>

      <div>
        <span>Запланированы</span>
        <strong>{{ scheduledCount }}</strong>
      </div>
    </div>

    <div class="judge-toolbar">
      <label>
        Статус выступления

        <select v-model="filter">
          <option value="all">Все выступления</option>
          <option value="Scheduled">Запланированные</option>
          <option value="InProgress">Проводятся</option>
          <option value="Completed">Завершённые</option>
          <option value="Cancelled">Отменённые</option>
        </select>
      </label>
    </div>

    <p v-if="error" class="organizer-error" role="alert">
      {{ error }}
    </p>

    <p v-if="loading" class="judge-empty">
      Загрузка выступлений...
    </p>

    <div v-else-if="!matches.length && !error"
         class="judge-empty">
      <h2>Нет назначенных выступлений</h2>
      <p>
        Когда организатор назначит вас судьёй
        на матч или заезд, он появится здесь.
      </p>
    </div>

    <p v-else-if="!filteredMatches.length && !error"
       class="judge-empty">
      Выступлений с выбранным статусом нет.
    </p>

    <div v-else-if="!error" class="judge-match-list">
      <article v-for="match in filteredMatches"
               :key="match.id"
               class="judge-match-card">
        <div class="judge-match-card__header">
          <div>
            <h2>{{ displayName(match) }}</h2>
            <p>{{ dateText(match.scheduledAt) }}</p>
            <p>
              {{
                match.scoringMode === 'PerAlliance'
                  ? 'Командные союзы'
                  : 'Отдельные команды'
              }}
            </p>
          </div>

          <span class="judge-status"
                :class="`judge-status--${match.status}`">
            {{ statusText(match.status) }}
          </span>
        </div>

        <div class="judge-match-card__sides">
          <div v-for="side in match.sides"
               :key="side.id"
               class="judge-side-preview">
            <strong>
              {{
                side.name ||
                (match.scoringMode === 'PerAlliance'
                  ? `Союз №${side.number}`
                  : `Сторона №${side.number}`)
              }}
            </strong>

            <p>
              {{
                side.teams
                  .map(team => team.teamName)
                  .join(', ')
              }}
            </p>
          </div>
        </div>

        <div class="judge-match-card__footer">
          <span>
            {{
              match.status === 'InProgress'
                ? 'Можно выставлять оценки'
                : match.status === 'Scheduled'
                  ? 'Ожидается начало'
                  : 'Доступен просмотр протоколов'
            }}
          </span>

          <RouterLink :to="{
              name: 'judge-scorecard',
              params: { id: match.id },
            }"
                      class="btn btn-primary">
            {{
              match.status === 'InProgress'
                ? 'Открыть судейство'
                : 'Просмотреть'
            }}
          </RouterLink>
        </div>
      </article>
    </div>
  </section>
</template>
