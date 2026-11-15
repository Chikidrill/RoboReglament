
<script setup lang="ts">import { computed, ref } from 'vue'

import type {
  TournamentApplication,
} from '@/types/application'

import {
  submitTournamentApplication,
  withdrawTournamentApplication,
  deleteApplicationDraft,
  refreshApplicationParticipants,
} from '@/api/applications'

import {
  getApplicationStatusLabel,
  formatApplicationDate,
} from '@/utils/application'

const props = defineProps<{
  application: TournamentApplication
  registrationOpen: boolean
}>()

const emit = defineEmits<{
  updated: [application: TournamentApplication]
  removed: [applicationId: number]
}>()

type Action = 'submit' | 'withdraw' | 'refresh' | 'delete'

const busy = ref<Action | null>(null)
const error = ref('')
const success = ref('')

const isDraft = computed(() =>
  props.application.status === 'Draft',
)

const isWithdrawn = computed(() =>
  props.application.status === 'Withdrawn',
)

const canSubmit = computed(() =>
  props.registrationOpen &&
  !props.application.isAccredited &&
  (isDraft.value || isWithdrawn.value),
)

const canWithdraw = computed(() =>
  props.registrationOpen &&
  !props.application.isAccredited &&
  (
    props.application.status === 'Pending' ||
    props.application.status === 'Approved'
  ),
)

const canRefresh = computed(() =>
  props.registrationOpen &&
  !props.application.isAccredited &&
  !isWithdrawn.value,
)

const canDelete = computed(() =>
  isDraft.value && !props.application.isAccredited,
)

function fullName(person: {
  firstName: string
  lastName: string
  middleName: string | null
}) {
  return [
    person.lastName,
    person.firstName,
    person.middleName,
  ].filter(Boolean).join(' ')
}

async function performAction(action: Action) {
  if (busy.value !== null) return

  const allowed = {
    submit: canSubmit.value,
    withdraw: canWithdraw.value,
    refresh: canRefresh.value,
    delete: canDelete.value,
  }

  if (!allowed[action]) return

  const confirmations: Record<Action, string> = {
    submit:
      'Отправить заявку с текущим составом команды?',
    withdraw:
      'Отозвать заявку на участие в турнире?',
    refresh: isDraft.value
      ? 'Обновить состав черновика?'
      : 'Обновить состав заявки? Она будет отправлена на повторное рассмотрение.',
    delete:
      'Удалить черновик заявки?',
  }

  if (!window.confirm(confirmations[action])) return

  busy.value = action
  error.value = ''
  success.value = ''

  const tournamentId = props.application.tournamentId
  const applicationId = props.application.id

  try {
    let updated: TournamentApplication

    switch (action) {
      case 'submit':
        updated = await submitTournamentApplication(
          tournamentId,
          applicationId,
        )
        success.value = 'Заявка отправлена.'
        break

      case 'withdraw':
        updated = await withdrawTournamentApplication(
          tournamentId,
          applicationId,
        )
        success.value = 'Заявка отозвана.'
        break

      case 'refresh':
        updated = await refreshApplicationParticipants(
          tournamentId,
          applicationId,
        )
        success.value = 'Состав заявки обновлён.'
        break

      case 'delete':
        await deleteApplicationDraft(
          tournamentId,
          applicationId,
        )
        emit('removed', applicationId)
        return
    }

    emit('updated', updated)
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Не удалось выполнить операцию.'
  } finally {
    busy.value = null
  }
}</script>

<template>
  <article class="coach-applications__item">
    <div class="coach-applications__item-heading">
      <strong>{{ application.teamName }}</strong>

      <span class="coach-applications__status"
            :class="`coach-applications__status--${
          application.isAccredited
            ? 'accredited'
            : application.status.toLowerCase()
        }`">
        {{
          application.isAccredited
            ? 'Аккредитована'
            : getApplicationStatusLabel(application.status)
        }}
      </span>
    </div>

    <p v-if="application.submittedAt">
      Подана:
      {{ formatApplicationDate(application.submittedAt) }}
    </p>

    <p v-else>Заявка ещё не отправлена.</p>

    <p>Участников: {{ application.participants.length }}</p>

    <p v-if="application.comment">
      Комментарий организатора: {{ application.comment }}
    </p>

    <p v-if="application.isAccredited">
      Состав зафиксирован после аккредитации.
    </p>

    <p v-if="application.accreditedAt">
      Аккредитация:
      {{ formatApplicationDate(application.accreditedAt) }}
    </p>

    <p v-if="application.accreditationComment">
      Комментарий аккредитации:
      {{ application.accreditationComment }}
    </p>

    <details class="coach-applications__participants">
      <summary>Заявленный состав</summary>

      <ul v-if="application.participants.length > 0">
        <li v-for="person in application.participants"
            :key="person.id">
          {{ fullName(person) }}
        </li>
      </ul>

      <p v-else>В заявке пока нет участников.</p>
    </details>

    <p v-if="error"
       class="coach-applications__error"
       role="alert">
      {{ error }}
    </p>

    <p v-if="success"
       class="coach-applications__success"
       role="status">
      {{ success }}
    </p>

    <div class="coach-applications__actions">
      <button v-if="canSubmit"
              type="button"
              class="btn btn-primary"
              :disabled="busy !== null"
              @click="performAction('submit')">
        {{
          busy === 'submit'
            ? 'Отправка...'
            : isWithdrawn
              ? 'Подать повторно'
              : 'Отправить заявку'
        }}
      </button>

      <button v-if="canRefresh"
              type="button"
              class="btn btn-outline"
              :disabled="busy !== null"
              @click="performAction('refresh')">
        Обновить состав
      </button>

      <button v-if="canWithdraw"
              type="button"
              class="coach-team-details__delete"
              :disabled="busy !== null"
              @click="performAction('withdraw')">
        Отозвать заявку
      </button>

      <button v-if="canDelete"
              type="button"
              class="coach-team-details__delete"
              :disabled="busy !== null"
              @click="performAction('delete')">
        Удалить черновик
      </button>
    </div>
  </article>
</template>
