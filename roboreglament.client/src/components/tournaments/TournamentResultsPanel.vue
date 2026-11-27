
<script setup lang="ts">import {
  computed,
  onMounted,
  onUnmounted,
  ref,
} from 'vue'

import { getTournamentResults } from '@/api/results'

import type {
  TournamentResults,
  ResultMatch,
  ResultSide
} from '@/types/results'

import type {
  JudgeAggregationMethod,
  RankingMethod,
} from '@/types/tournament'

const props = defineProps<{
  tournamentId: number
}>()

type ActiveTab = 'standings' | 'matches'

const data = ref<TournamentResults | null>(null)
const selectedId = ref(0)
const activeTab = ref<ActiveTab>('standings')

const loading = ref(false)
const error = ref('')

let controller: AbortController | null = null

const discipline = computed(() =>
  data.value?.disciplines.find(
    x => x.disciplineId === selectedId.value,
  ) ?? null,
)

async function load() {
  controller?.abort()

  const current = new AbortController()
  controller = current

  loading.value = true
  error.value = ''

  try {
    const result = await getTournamentResults(
      props.tournamentId,
      current.signal,
    )

    if (current.signal.aborted) return

    data.value = result

    if (!result.disciplines.some(
      x => x.disciplineId === selectedId.value,
    )) {
      selectedId.value =
        result.disciplines[0]?.disciplineId ?? 0
    }
  } catch (err) {
    if (!current.signal.aborted) {
      error.value = err instanceof Error
        ? err.message
        : 'Не удалось получить результаты турнира.'
    }
  } finally {
    if (!current.signal.aborted) {
      loading.value = false
    }
  }
}

function formatScore(value: number): string {
  return new Intl.NumberFormat('ru-RU', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value)
}

function aggregationLabel(
  method: JudgeAggregationMethod,
): string {
  switch (method) {
    case 'Average': return 'Среднее оценок судей'
    case 'Sum': return 'Сумма баллов судей'
    case 'AgreedActual': return 'Фактически набранные баллы'
  }
}

function rankingLabel(method: RankingMethod): string {
  switch (method) {
    case 'BestResult': return 'Лучший результат'
    case 'SumResults': return 'Сумма результатов'
    case 'AverageResults': return 'Среднее результатов'
    case 'MatchPoints': return 'Турнирные очки'
  }
}

function matchName(match: ResultMatch): string {
  if (match.name) return match.name

  return discipline.value?.format === 'Run'
    ? `Заезд №${match.number}`
    : `Матч №${match.number}`
}

function matchTime(value: string | null): string {
  return value
    ? new Date(value).toLocaleString('ru-RU')
    : 'Время не назначено'
}


function sideOutcome(
  match: ResultMatch,
  side: ResultSide,
): string {
  if (side.isWinner) return 'Победа'

  if (match.isDraw) {
    const highestScore = Math.max(
      ...match.sides.map(x => x.totalScore),
    )

    if (side.totalScore === highestScore) {
      return 'Ничья'
    }
  }

  return 'Поражение'
}


onMounted(() => { void load() })
onUnmounted(() => controller?.abort())</script>

<template>
  <section id="results" class="results-panel">
    <div class="results-panel__heading">
      <div>
        <h2>Результаты соревнования</h2>

        <p>
          Рейтинг команд и результаты завершённых
          выступлений по дисциплинам.
        </p>
      </div>

      <button type="button"
              class="btn btn-outline"
              :disabled="loading"
              @click="load">
        Обновить результаты
      </button>
    </div>

    <p v-if="error" class="organizer-error" role="alert">
      {{ error }}
    </p>

    <p v-if="loading" class="results-panel__empty">
      Загрузка результатов...
    </p>

    <template v-else-if="data && !error">
      <div v-if="data.disciplines.length === 0"
           class="results-panel__empty">
        Дисциплины пока не добавлены.
      </div>

      <template v-else>
        <div class="results-panel__toolbar">
          <label>
            Дисциплина

            <select v-model.number="selectedId">
              <option v-for="item in data.disciplines"
                      :key="item.disciplineId"
                      :value="item.disciplineId">
                {{ item.name }}
              </option>
            </select>
          </label>
        </div>

        <template v-if="discipline">
          <div class="results-panel__info">
            <span>
              Подсчёт:
              <strong>
                {{
                  aggregationLabel(
                    discipline.aggregationMethod,
                  )
                }}
              </strong>
            </span>

            <span>
              Рейтинг:
              <strong>
                {{
                  rankingLabel(
                    discipline.rankingMethod,
                  )
                }}
              </strong>
            </span>

            <span v-if="discipline.format === 'Match'">
              Победа / ничья / поражение:
              <strong>
                {{ discipline.winPoints }} /
                {{ discipline.drawPoints }} /
                {{ discipline.lossPoints }}
              </strong>
            </span>
          </div>

          <div class="results-panel__tabs"
               role="group"
               aria-label="Раздел результатов">
            <button type="button"
                    :class="{
                'results-panel__tab--active':
                  activeTab === 'standings',
              }"
                    :aria-pressed="activeTab === 'standings'"
                    @click="activeTab = 'standings'">
              Рейтинг команд
            </button>

            <button type="button"
                    :class="{
                'results-panel__tab--active':
                  activeTab === 'matches',
              }"
                    :aria-pressed="activeTab === 'matches'"
                    @click="activeTab = 'matches'">
              Выступления ({{ discipline.matches.length }})
            </button>
          </div>

          <!-- Рейтинг -->

          <div v-if="activeTab === 'standings'">
            <p v-if="discipline.standings.length === 0"
               class="results-panel__empty">
              Завершённых выступлений пока нет.
              Рейтинг появится после публикации
              первых результатов.
            </p>

            <div v-else
                 class="results-panel__table-scroll">
              <table class="results-panel__table">
                <thead>
                  <tr>
                    <th scope="col">Место</th>
                    <th scope="col">Команда</th>
                    <th scope="col">Выступлений</th>

                    <template v-if="discipline.format === 'Match'">
                      <th scope="col">Победы</th>
                      <th scope="col">Ничьи</th>
                      <th scope="col">Поражения</th>
                    </template>

                    <th scope="col">
                      {{
                        discipline.rankingMethod === 'MatchPoints'
                          ? 'Турнирные очки'
                          : 'Результат'
                      }}
                    </th>
                  </tr>
                </thead>

                <tbody>
                  <tr v-for="team in discipline.standings"
                      :key="team.teamId"
                      :class="{
                      'results-panel__leader':
                        team.position === 1,
                    }">
                    <td>
                      <strong>{{ team.position }}</strong>
                    </td>

                    <td>{{ team.teamName }}</td>
                    <td>{{ team.played }}</td>

                    <template v-if="discipline.format === 'Match'">
                      <td>{{ team.wins }}</td>
                      <td>{{ team.draws }}</td>
                      <td>{{ team.losses }}</td>
                    </template>

                    <td>
                      <strong>
                        {{ formatScore(team.rankingValue) }}
                      </strong>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <p v-if="discipline.standings.length > 0"
               class="results-panel__footnote">
              При одинаковом рейтинговом показателе
              команды занимают одинаковое место.
            </p>
          </div>

          <!-- Завершённые выступления -->

          <div v-else class="results-panel__match-list">
            <p v-if="discipline.matches.length === 0"
               class="results-panel__empty">
              Завершённых выступлений пока нет.
            </p>

            <details v-for="match in discipline.matches"
                     :key="match.matchId"
                     class="results-panel__match">
              <summary>
                <span>
                  <strong>{{ matchName(match) }}</strong>
                  <small>
                    {{ matchTime(match.scheduledAt) }}
                  </small>
                </span>

                <span>
                  {{
                    discipline.format === 'Match' &&
                    match.isDraw
                      ? 'Ничья'
                      : 'Завершено'
                  }}
                </span>
              </summary>

              <div class="results-panel__table-scroll">
                <table class="results-panel__table">
                  <thead>
                    <tr>
                      <th scope="col">Сторона</th>
                      <th scope="col">Команды</th>
                      <th scope="col">Баллы</th>
                      <th v-if="discipline.format === 'Match'"
                          scope="col">
                        Итог
                      </th>
                    </tr>
                  </thead>

                  <tbody>
                    <tr v-for="side in match.sides"
                        :key="side.sideId">
                      <td>
                        {{
                          side.name ||
                          `Сторона №${side.number}`
                        }}
                      </td>

                      <td>
                        {{
                          side.teams
                            .map(team => team.name)
                            .join(', ')
                        }}
                      </td>

                      <td>
                        <strong>
                          {{ formatScore(side.totalScore) }}
                        </strong>
                      </td>
                      <td v-if="discipline.format === 'Match'">
                        {{ sideOutcome(match, side) }}
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </details>
          </div>
        </template>
      </template>
    </template>
  </section>
</template>
