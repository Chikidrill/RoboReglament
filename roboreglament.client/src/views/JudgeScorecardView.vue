
<script setup lang="ts">import { computed, ref, watch } from 'vue'
import {
  onBeforeRouteLeave,
  useRoute,
} from 'vue-router'

import {
  getJudgeScorecard,
  saveJudgeDraft,
  submitJudgeProtocol,
} from '@/api/judging'

import type {
  JudgeCriterion,
  JudgeMatchScorecard,
  JudgeProtocol,
  JudgeScore,
  JudgeSideScorecard,
} from '@/types/judging'

interface SideForm {
  values: Record<number, string>
  comment: string
  snapshot: string
}

const route = useRoute()

const scorecard = ref<JudgeMatchScorecard | null>(null)
const forms = ref<Record<number, SideForm>>({})

const loading = ref(true)
const busySideId = ref<number | null>(null)

const error = ref('')
const success = ref('')

const matchId = computed(() => Number(route.params.id))

const canJudge = computed(() =>
  scorecard.value?.status === 'InProgress',
)

const criteria = computed(() =>
  [...(scorecard.value?.criteria ?? [])].sort(
    (a, b) => a.order - b.order,
  ),
)

function snapshot(
  values: Record<number, string>,
  comment: string,
): string {
  return JSON.stringify({ values, comment })
}

function createForm(
  side: JudgeSideScorecard,
  currentCriteria: JudgeCriterion[],
): SideForm {
  const values: Record<number, string> = {}

  for (const criterion of currentCriteria) {
    const saved = side.protocol?.scores.find(
      score => score.criterionId === criterion.id,
    )

    values[criterion.id] = saved
      ? String(saved.value)
      : ''
  }

  const comment = side.protocol?.comment ?? ''

  return {
    values,
    comment,
    snapshot: snapshot(values, comment),
  }
}

function isDirty(sideId: number): boolean {
  const form = forms.value[sideId]
  if (!form) return false

  return snapshot(form.values, form.comment) !== form.snapshot
}

const hasUnsavedChanges = computed(() =>
  Object.keys(forms.value).some(id => isDirty(Number(id))),
)

function filledCount(sideId: number): number {
  const form = forms.value[sideId]
  if (!form) return 0

  return criteria.value.filter(
    criterion => (form.values[criterion.id] ?? '').trim() !== '',
  ).length
}

function sideName(side: JudgeSideScorecard): string {
  if (side.name) return side.name

  return scorecard.value?.scoringMode === 'PerAlliance'
    ? `Союз №${side.number}`
    : `Сторона №${side.number}`
}

function scorecardTitle(): string {
  const current = scorecard.value

  if (!current) return 'Судейский протокол'

  return current.matchName ||
    (current.format === 'Run'
      ? `Заезд №${current.matchId}`
      : `Матч №${current.matchId}`)
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat('ru-RU', {
    maximumFractionDigits: 2,
  }).format(value)
}

function setScore(
  sideId: number,
  criterionId: number,
  event: Event,
) {
  const input = event.target as HTMLInputElement
  const form = forms.value[sideId]

  if (!form) return

  form.values[criterionId] = input.value
}

async function load(
  id: number,
  signal: AbortSignal,
) {
  loading.value = true
  error.value = ''
  success.value = ''
  scorecard.value = null
  forms.value = {}

  if (!Number.isSafeInteger(id) || id <= 0) {
    error.value = 'Некорректный ID выступления.'
    loading.value = false
    return
  }

  try {
    const data = await getJudgeScorecard(id, signal)

    if (signal.aborted) return

    scorecard.value = data

    const nextForms: Record<number, SideForm> = {}

    for (const side of data.sides) {
      nextForms[side.id] = createForm(
        side,
        data.criteria,
      )
    }

    forms.value = nextForms
  } catch (err) {
    if (!signal.aborted) {
      error.value = err instanceof Error
        ? err.message
        : 'Не удалось получить судейский протокол.'
    }
  } finally {
    if (!signal.aborted) {
      loading.value = false
    }
  }
}

watch(
  () => route.params.id,
  (value, _previous, onCleanup) => {
    const controller = new AbortController()

    onCleanup(() => controller.abort())

    void load(Number(value), controller.signal)
  },
  { immediate: true },
)

// Формируем оценки только по заполненным полям.
// Пустые поля допустимы в черновике.
function collectScores(sideId: number): JudgeScore[] {
  const form = forms.value[sideId]
  if (!form) throw new Error('Форма не найдена.')

  const scores: JudgeScore[] = []

  for (const criterion of criteria.value) {
    const raw = (form.values[criterion.id] ?? '').trim()

    if (!raw) continue

    const value = Number(raw)

    if (
      !Number.isFinite(value) ||
      value < criterion.minValue ||
      criterion.maxValue === null ||
      value > criterion.maxValue ||
      Math.abs(value * 100 - Math.round(value * 100)) > 0.000001
    ) {
      throw new Error(
        `Проверьте значение критерия «${criterion.name}».`,
      )
    }

    scores.push({
      criterionId: criterion.id,
      value,
    })
  }

  return scores
}

// Обновляем одну сторону, не сбрасывая
// несохранённые изменения других сторон.
function applyProtocol(
  sideId: number,
  protocol: JudgeProtocol,
) {
  const current = scorecard.value
  if (!current) return

  const side = current.sides.find(x => x.id === sideId)

  if (!side) return

  side.protocol = protocol

  forms.value[sideId] = createForm(
    side,
    current.criteria,
  )
}

async function save(sideId: number) {
  const current = scorecard.value
  const form = forms.value[sideId]
  const side = current?.sides.find(x => x.id === sideId)

  if (
    !current ||
    !form ||
    !side ||
    !canJudge.value ||
    side.protocol?.status === 'Submitted' ||
    busySideId.value !== null
  ) return

  let scores: JudgeScore[]

  try {
    scores = collectScores(sideId)
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Некорректные оценки.'
    return
  }

  busySideId.value = sideId
  error.value = ''
  success.value = ''

  try {
    const protocol = await saveJudgeDraft(
      current.matchId,
      sideId,
      {
        scores,
        comment: form.comment.trim() || null,
      },
    )

    applyProtocol(sideId, protocol)

    success.value = `Черновик «${sideName(side)}» сохранён.`
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Не удалось сохранить черновик.'
  } finally {
    busySideId.value = null
  }
}

function canSubmit(side: JudgeSideScorecard): boolean {
  return (
    canJudge.value &&
    busySideId.value === null &&
    side.protocol?.status === 'Draft' &&
    side.protocol.scores.length === criteria.value.length &&
    criteria.value.length > 0 &&
    !isDirty(side.id)
  )
}

async function submit(side: JudgeSideScorecard) {
  if (!canSubmit(side)) return

  if (!window.confirm(
    `Отправить протокол для «${sideName(side)}»? ` +
    'После отправки изменить оценки будет нельзя.',
  )) return

  busySideId.value = side.id
  error.value = ''
  success.value = ''

  try {
    const protocol = await submitJudgeProtocol(
      matchId.value,
      side.id,
    )

    applyProtocol(side.id, protocol)

    success.value =
      `Протокол «${sideName(side)}» успешно отправлен.`
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Не удалось отправить протокол.'
  } finally {
    busySideId.value = null
  }
}

onBeforeRouteLeave(() => {
  if (
    hasUnsavedChanges.value &&
    !window.confirm(
      'Есть несохранённые оценки. Покинуть страницу?',
    )
  ) {
    return false
  }
})</script>

<template>
  <section class="judge-page">
    <RouterLink :to="{ name: 'judge-matches' }"
                class="organizer-back">
      ← Мои выступления
    </RouterLink>

    <p v-if="loading" class="judge-empty">
      Загрузка судейского протокола...
    </p>

    <p v-if="error" class="organizer-error" role="alert">
      {{ error }}
    </p>

    <p v-if="success" class="organizer-success" role="status">
      {{ success }}
    </p>

    <template v-if="!loading && scorecard">
      <header class="judge-page__heading">
        <div>
          <h1>{{ scorecardTitle() }}</h1>
          <p>
            Судейский протокол:
            <strong>{{ scorecard.templateName }}</strong>
          </p>
        </div>

        <span class="judge-status"
              :class="`judge-status--${scorecard.status}`">
          {{
            scorecard.status === 'InProgress'
              ? 'Проводится'
              : scorecard.status === 'Scheduled'
                ? 'Ожидает начала'
                : scorecard.status === 'Completed'
                  ? 'Завершено'
                  : 'Отменено'
          }}
        </span>
      </header>

      <p v-if="!canJudge" class="judge-notice">
        {{
          scorecard.status === 'Scheduled'
            ? 'Организатор ещё не начал выступление. Выставление оценок пока недоступно.'
            : 'Выступление закрыто для оценивания. Протоколы доступны для просмотра.'
        }}
      </p>

      <p v-else class="judge-notice">
        Для каждой стороны выставьте баллы по критериям.
        Черновики можно сохранять отдельно. После проверки
        всех оценок отправьте протокол.
      </p>

      <section v-for="side in scorecard.sides"
               :key="side.id"
               class="judge-scorecard">
        <div class="judge-scorecard__header">
          <div>
            <h2>{{ sideName(side) }}</h2>

            <p>
              Команды:
              {{ side.teams.join(', ') }}
            </p>
          </div>

          <span class="judge-status"
                :class="
              side.protocol?.status === 'Submitted'
                ? 'judge-status--Completed'
                : 'judge-status--Scheduled'
            ">
            {{
              side.protocol?.status === 'Submitted'
                ? 'Отправлен'
                : side.protocol
                  ? 'Черновик'
                  : 'Не заполнен'
            }}
          </span>
        </div>

        <form v-if="forms[side.id]"
              class="judge-scorecard__form"
              @submit.prevent="save(side.id)">
          <div class="judge-scorecard__progress">
            <span>Заполнено критериев</span>
            <strong>
              {{ filledCount(side.id) }}/{{ criteria.length }}
            </strong>
          </div>

          <div v-for="criterion in criteria"
               :key="criterion.id"
               class="judge-criterion">
            <div class="judge-criterion__info">
              <strong>{{ criterion.name }}</strong>

              <p v-if="criterion.description">
                {{ criterion.description }}
              </p>

              <p>
                Допустимое значение:
                {{ formatNumber(criterion.minValue) }}
                –
                {{
                  criterion.maxValue !== null
                    ? formatNumber(criterion.maxValue)
                    : 'не задано'
                }}
              </p>

              <p>
                Коэффициент:
                {{ formatNumber(criterion.weight) }}
                <span v-if="criterion.isPenalty">
                  · штраф
                </span>
              </p>
            </div>

            <label :for="`score-${side.id}-${criterion.id}`">
              Баллы

              <input :id="`score-${side.id}-${criterion.id}`"
                     type="number"
                     inputmode="decimal"
                     step="0.01"
                     :min="criterion.minValue"
                     :max="criterion.maxValue ?? undefined"
                     :value="
                  forms[side.id]?.values[criterion.id] ?? ''
                "
                     :disabled="
                  !canJudge ||
                  side.protocol?.status === 'Submitted' ||
                  busySideId !== null
                "
                     placeholder="—"
                     @input="setScore(side.id, criterion.id, $event)" />
            </label>
          </div>

          <label class="judge-scorecard__comment">
            Комментарий судьи

            <textarea v-model="forms[side.id].comment"
                      rows="3"
                      maxlength="1000"
                      :disabled="
                !canJudge ||
                side.protocol?.status === 'Submitted' ||
                busySideId !== null
              "
                      placeholder="Необязательно" />
          </label>

          <div class="judge-scorecard__result">
            <span>Итоговый балл</span>

            <strong v-if="
              side.protocol?.total !== null &&
              side.protocol?.total !== undefined &&
              !isDirty(side.id)
            ">
              {{ formatNumber(side.protocol.total) }}
            </strong>

            <span v-else>
              {{
                isDirty(side.id)
                  ? 'Пересчёт после сохранения'
                  : 'Не рассчитан'
              }}
            </span>
          </div>

          <p v-if="side.protocol?.status === 'Submitted'"
             class="judge-scorecard__submitted">
            Протокол отправлен
            {{
              side.protocol.submittedAt
                ? new Date(
                    side.protocol.submittedAt,
                  ).toLocaleString('ru-RU')
                : ''
            }}.
            Редактирование недоступно.
          </p>

          <div v-else-if="canJudge"
               class="organizer-actions">
            <button type="submit"
                    class="btn btn-outline"
                    :disabled="
                busySideId !== null ||
                !isDirty(side.id)
              ">
              {{
                busySideId === side.id
                  ? 'Сохранение...'
                  : 'Сохранить черновик'
              }}
            </button>

            <button type="button"
                    class="btn btn-primary"
                    :disabled="!canSubmit(side)"
                    @click="submit(side)">
              Отправить протокол
            </button>
          </div>

          <p v-if="
            canJudge &&
            side.protocol?.status !== 'Submitted' &&
            !canSubmit(side)
          ">
            Для отправки необходимо заполнить все критерии
            и сохранить изменения.
          </p>
        </form>
      </section>
    </template>
  </section>
</template>
