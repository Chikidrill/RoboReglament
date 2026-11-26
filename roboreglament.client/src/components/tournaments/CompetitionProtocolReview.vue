
<script setup lang="ts">import {
  computed,
  onUnmounted,
  ref,
  watch,
} from 'vue'

import {
  getProtocolReview,
  reopenJudgeProtocol,
} from '@/api/protocol-review'

import type { ProtocolReview } from '@/types/protocol-review'

const props = defineProps<{
  tournamentId: number
  disciplineId: number
  matchId: number
  editable: boolean
}>()

  const review = ref<ProtocolReview | null>(null)

  const hasActualMismatch = computed(() => {
    const current = review.value

    if (!current) return false

    return (
      current.aggregationMethod === 'AgreedActual' &&
      current.sides.some(
        side => side.status === 'Mismatch'
      )
    )
  })



const loading = ref(false)
const busy = ref(false)
const error = ref('')
const success = ref('')

let controller: AbortController | null = null

async function load() {
  controller?.abort()
  const current = new AbortController()
  controller = current

  loading.value = true
  error.value = ''

  try {
    const result = await getProtocolReview(
      props.tournamentId,
      props.disciplineId,
      props.matchId,
      current.signal,
    )

    if (!current.signal.aborted) {
      review.value = result
    }
  } catch (err) {
    if (!current.signal.aborted) {
      error.value = err instanceof Error
        ? err.message
        : 'Не удалось загрузить протоколы.'
    }
  } finally {
    if (!current.signal.aborted) {
      loading.value = false
    }
  }
}

async function reopen(protocolId: number) {
  if (!props.editable || busy.value) return

  if (!window.confirm(
    'Вернуть протокол судье на исправление? ' +
    'Судья сможет изменить оценки и отправить его повторно.',
  )) return

  busy.value = true
  error.value = ''
  success.value = ''

  try {
    await reopenJudgeProtocol(
      props.tournamentId,
      props.disciplineId,
      props.matchId,
      protocolId,
    )

    success.value = 'Протокол возвращён на исправление.'
    await load()
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Не удалось вернуть протокол.'
  } finally {
    busy.value = false
  }
}

function score(value: number | null): string {
  if (value === null) return '—'

  return new Intl.NumberFormat('ru-RU', {
    maximumFractionDigits: 2,
  }).format(value)
}

function statusName(status: string): string {
  switch (status) {
    case 'Submitted': return 'Отправлен'
    case 'Draft': return 'Черновик'
    default: return 'Не заполнен'
  }
}

watch(
  () => [
    props.tournamentId,
    props.disciplineId,
    props.matchId,
  ],
  () => { void load() },
  { immediate: true },
)

onUnmounted(() => controller?.abort())</script>

<template>
  <section class="competition-review">
    <div class="competition-review__heading">
      <div>
        <h4>Судейские протоколы</h4>
        <p>
          {{
            review?.aggregationMethod === 'AgreedActual'
              ? 'Проверка совпадения фактических баллов'
              : 'Сравнение выставленных судьями баллов'
          }}
        </p>
      </div>

      <button type="button"
              class="btn btn-outline"
              :disabled="busy || loading"
              @click="load">
        Обновить
      </button>
    </div>

    <p v-if="loading">Загрузка протоколов...</p>

    <p v-if="error" class="organizer-error" role="alert">
      {{ error }}
    </p>

    <p v-if="success" class="organizer-success" role="status">
      {{ success }}
    </p>

    <template v-if="review && !loading">
      <article v-for="side in review.sides"
               :key="side.sideId"
               class="competition-review__side">
        <div class="competition-review__heading">
          <h5>
            {{ side.name || `Сторона №${side.number}` }}
          </h5>

          <span class="competition-review__status"
                :class="{
              'competition-review__status--error':
                side.status === 'Mismatch' &&
                review.aggregationMethod === 'AgreedActual',
            }"
          </span>
        </div>

        <div class="competition-review__table-scroll">
          <table class="competition-review__table">
            <thead>
              <tr>
                <th>Критерий</th>
                <th v-for="judge in side.criteria[0]?.values ?? []"
                    :key="judge.judgePublicId">
                  Судья ID {{ judge.judgePublicId }}
                </th>
              </tr>
            </thead>


            <tbody>
              <tr v-for="criterion in side.criteria"
                  :key="criterion.criterionId"
                  :class="{
      'competition-review__mismatch':
        criterion.hasMismatch &&
        review.aggregationMethod === 'AgreedActual',
    }">
                <th scope="row">
                  {{ criterion.criterionName }}

                  <span v-if="
          criterion.hasMismatch &&
          review.aggregationMethod === 'AgreedActual'
        ">
                    · разные значения
                  </span>
                </th>

                <td v-for="judge in criterion.values"
                    :key="judge.judgePublicId">
                  <strong>{{ score(judge.value) }}</strong>

                  <small>{{ statusName(judge.status) }}</small>

                  <button v-if="
          editable &&
          judge.status === 'Submitted' &&
          judge.protocolId !== null
        "
                          type="button"
                          class="competition-review__reopen"
                          :disabled="busy"
                          @click="reopen(judge.protocolId)">
                    Вернуть
                  </button>
                </td>
              </tr>
            </tbody>

          </table>
        </div>
      </article>



      <p v-if="hasActualMismatch" class="organizer-error">
        Фактические баллы судей расходятся.
        Для завершения выступления исправьте
        спорные протоколы и получите повторное
        подтверждение от судей.
      </p>


    </template>
  </section>
</template>
