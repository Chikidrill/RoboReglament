
<script setup lang="ts">import { computed, ref, watch } from 'vue'

import {
  getTournamentApplications,
  reviewTournamentApplication,
  accreditTournamentApplication,
} from '@/api/applications'

import {
  getApplicationStatusLabel,
  formatApplicationDate,
} from '@/utils/application'

import type { Tournament } from '@/types/tournament'
import type { TournamentApplication } from '@/types/application'

const props = defineProps<{
  tournament: Tournament
}>()

const applications = ref<TournamentApplication[]>([])
const comments = ref<Record<number, string>>({})
const accreditationComments = ref<Record<number, string>>({})

const isLoading = ref(true)
const busyId = ref<number | null>(null)

const error = ref('')
const success = ref('')
const search = ref('')
const disciplineFilter = ref(0)
const statusFilter = ref('all')
const reloadCount = ref(0)

const filtered = computed(() =>
  applications.value.filter(application => {
    const effectiveStatus = application.isAccredited
      ? 'Accredited'
      : application.status

    return (
      application.teamName.toLowerCase()
        .includes(search.value.trim().toLowerCase()) &&
      (
        disciplineFilter.value === 0 ||
        application.disciplineId === disciplineFilter.value
      ) &&
      (
        statusFilter.value === 'all' ||
        effectiveStatus === statusFilter.value
      )
    )
  }),
)

function canReview(application: TournamentApplication) {
  return props.tournament.status === 'Registration' &&
    application.status === 'Pending' &&
    !application.isAccredited
}

function canAccredit(application: TournamentApplication) {
  return (
    props.tournament.status === 'Registration' ||
    props.tournament.status === 'InProgress'
  ) &&
    application.status === 'Approved' &&
    !application.isAccredited
}

async function load(signal: AbortSignal) {
  isLoading.value = true
  error.value = ''

  try {
    const result = await getTournamentApplications(
      props.tournament.id,
      signal,
    )

    if (!signal.aborted) {
      applications.value = result.filter(
        item => item.status !== 'Draft',
      )
    }
  } catch (err) {
    if (!signal.aborted) {
      error.value = err instanceof Error
        ? err.message
        : 'Не удалось загрузить заявки.'
    }
  } finally {
    if (!signal.aborted) {
      isLoading.value = false
    }
  }
}

function replaceApplication(updated: TournamentApplication) {
  applications.value = applications.value.map(item =>
    item.id === updated.id ? updated : item,
  )
}

async function review(
  application: TournamentApplication,
  status: 'Approved' | 'Rejected',
) {
  if (busyId.value !== null || !canReview(application)) return

  error.value = ''
  success.value = ''

  const comment = comments.value[application.id]?.trim() || null

  if (status === 'Rejected' && !comment) {
    error.value = 'Укажите причину отклонения заявки.'
    return
  }

  if (!window.confirm(
    `${status === 'Approved' ? 'Одобрить' : 'Отклонить'} ` +
    `заявку команды «${application.teamName}»?`,
  )) return

  busyId.value = application.id

  try {
    const updated = await reviewTournamentApplication(
      props.tournament.id,
      application.id,
      status,
      comment,
    )

    replaceApplication(updated)

    success.value = status === 'Approved'
      ? 'Заявка одобрена.'
      : 'Заявка отклонена.'
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Не удалось рассмотреть заявку.'
  } finally {
    busyId.value = null
  }
}

async function accredit(application: TournamentApplication) {
  if (busyId.value !== null || !canAccredit(application)) return

  if (!window.confirm(
    `Аккредитовать команду «${application.teamName}»? ` +
    'После этого заявленный состав будет зафиксирован.',
  )) return

  busyId.value = application.id
  error.value = ''
  success.value = ''

  try {
    const updated = await accreditTournamentApplication(
      props.tournament.id,
      application.id,
      accreditationComments.value[application.id]?.trim() || null,
    )

    replaceApplication(updated)
    success.value = 'Команда аккредитована.'
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Не удалось аккредитовать команду.'
  } finally {
    busyId.value = null
  }
}

watch(
  [
    () => props.tournament.id,
    reloadCount,
  ],
  (_values, _previous, onCleanup) => {
    const controller = new AbortController()
    onCleanup(() => controller.abort())
    void load(controller.signal)
  },
  { immediate: true },
)</script>

<template>
    <section class="organizer-panel">
        <h2>Заявки и аккредитация</h2>

        <div class="organizer-metrics">
            <div>
                <span>Всего заявок</span>
                <strong>{{ applications.length }}</strong>
            </div>

            <div>
                <span>На рассмотрении</span>
                <strong>
                    {{ applications.filter(a => a.status === 'Pending').length }}
                </strong>
            </div>

            <div>
                <span>Одобрено</span>
                <strong>
                    {{ applications.filter(a => a.status === 'Approved').length }}
                </strong>
            </div>

            <div>
                <span>Аккредитовано</span>
                <strong>
                    {{ applications.filter(a => a.isAccredited).length }}
                </strong>
            </div>
        </div>

        <div class="organizer-toolbar">
            <input v-model="search"
                   type="search"
                   placeholder="Название команды"
                   aria-label="Поиск команды" />

            <select v-model.number="disciplineFilter"
                    aria-label="Дисциплина">
                <option :value="0">Все дисциплины</option>
                <option v-for="discipline in tournament.disciplines"
                        :key="discipline.id"
                        :value="discipline.id">
                    {{ discipline.name }}
                </option>
            </select>

            <select v-model="statusFilter" aria-label="Статус">
                <option value="all">Все статусы</option>
                <option value="Pending">На рассмотрении</option>
                <option value="Approved">Одобренные</option>
                <option value="Rejected">Отклонённые</option>
                <option value="Withdrawn">Отозванные</option>
                <option value="Accredited">Аккредитованные</option>
            </select>

            <button type="button"
                    class="btn btn-outline"
                    :disabled="isLoading || busyId !== null"
                    @click="reloadCount++">
                Обновить
            </button>
        </div>

        <p v-if="error" class="organizer-error" role="alert">
            {{ error }}
        </p>

        <p v-if="success" class="organizer-success" role="status">
            {{ success }}
        </p>

        <p v-if="isLoading">Загрузка заявок...</p>

        <template v-else>
            <p v-if="!filtered.length && !error">
                Заявки не найдены.
            </p>

            <div class="organizer-grid">
                <article v-for="application in filtered"
                         :key="application.id"
                         class="coach-applications__item">
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

                    <p>Дисциплина: {{ application.disciplineName }}</p>
                    <p>Подана: {{ formatApplicationDate(application.submittedAt) }}</p>
                    <p>Участников: {{ application.participants.length }}</p>

                    <details class="coach-applications__participants">
                        <summary>Заявленный состав</summary>

                        <ul>
                            <li v-for="person in application.participants"
                                :key="person.id">
                                {{ person.lastName }}
                                {{ person.firstName }}
                                {{ person.middleName }}
                            </li>
                        </ul>
                    </details>

                    <p v-if="application.comment">
                        Комментарий: {{ application.comment }}
                    </p>

                    <p v-if="application.accreditedAt">
                        Аккредитация:
                        {{ formatApplicationDate(application.accreditedAt) }}
                    </p>

                    <p v-if="application.accreditationComment">
                        {{ application.accreditationComment }}
                    </p>

                    <div v-if="canReview(application)" class="organizer-form">
                        <label>
                            Комментарий
                            <textarea v-model="comments[application.id]"
                                      maxlength="1000"
                                      rows="3"
                                      placeholder="При отклонении комментарий обязателен"
                                      :disabled="busyId !== null" />
                        </label>

                        <div class="organizer-actions">
                            <button type="button"
                                    class="btn btn-primary"
                                    :disabled="busyId !== null"
                                    @click="review(application, 'Approved')">
                                Одобрить
                            </button>

                            <button type="button"
                                    class="organizer-danger"
                                    :disabled="busyId !== null"
                                    @click="review(application, 'Rejected')">
                                Отклонить
                            </button>
                        </div>
                    </div>

                    <div v-if="canAccredit(application)"
                         class="organizer-form">
                        <label>
                            Комментарий аккредитации
                            <textarea v-model="accreditationComments[application.id]"
                                      maxlength="1000"
                                      rows="2"
                                      :disabled="busyId !== null" />
                        </label>

                        <button type="button"
                                class="btn btn-primary"
                                :disabled="busyId !== null"
                                @click="accredit(application)">
                            Аккредитовать команду
                        </button>
                    </div>
                </article>
            </div>
        </template>
    </section>
</template>
