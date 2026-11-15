
<script setup lang="ts">import { onMounted, onUnmounted, ref } from 'vue'

import {
  getTeamCoaches,
  lookupTeamCoach,
  addTeamCoach,
  removeTeamCoach,
} from '@/api/teams'

import type {
  TeamCoach,
  TeamCoachLookup,
} from '@/types/teams'

const props = defineProps<{
  teamId: number
}>()

const coaches = ref<TeamCoach[]>([])
const foundCoach = ref<TeamCoachLookup | null>(null)

const publicId = ref('')
const showForm = ref(false)
const isLoading = ref(true)
const busy = ref(false)

const error = ref('')
const success = ref('')

const controller = new AbortController()

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

async function loadCoaches() {
  isLoading.value = true
  error.value = ''

  try {
    coaches.value = await getTeamCoaches(
      props.teamId,
      controller.signal,
    )
  } catch (err) {
    if (!controller.signal.aborted) {
      error.value = err instanceof Error
        ? err.message
        : 'Не удалось загрузить тренеров.'
    }
  } finally {
    if (!controller.signal.aborted) {
      isLoading.value = false
    }
  }
}

function resetSearch() {
  foundCoach.value = null
  error.value = ''
}

function openForm() {
  resetSearch()
  publicId.value = ''
  success.value = ''
  showForm.value = true
}

function closeForm() {
  resetSearch()
  publicId.value = ''
  showForm.value = false
}

async function searchCoach() {
  if (busy.value) return

  resetSearch()

  const raw = publicId.value.trim()
  const id = Number(raw)

  if (!/^[1-9]\d*$/.test(raw) ||
      !Number.isSafeInteger(id)) {
    error.value = 'Введите корректный публичный ID.'
    return
  }

  busy.value = true

  try {
    foundCoach.value = await lookupTeamCoach(
      props.teamId,
      id,
    )
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Тренер не найден.'
  } finally {
    busy.value = false
  }
}

async function addCoach() {
  const coach = foundCoach.value

  if (!coach || coach.alreadyInTeam || busy.value) return

  busy.value = true
  error.value = ''
  success.value = ''

  try {
    await addTeamCoach(props.teamId, coach.publicId)

    success.value = `Тренер ${fullName(coach)} добавлен.`

    closeForm()
    await loadCoaches()
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Не удалось добавить тренера.'
  } finally {
    busy.value = false
  }
}

async function excludeCoach(coach: TeamCoach) {
  if (coach.isSelf || busy.value) return

  if (!window.confirm(
    `Исключить тренера ${fullName(coach)}?`,
  )) return

  busy.value = true
  error.value = ''
  success.value = ''

  try {
    await removeTeamCoach(props.teamId, coach.publicId)

    success.value = 'Тренер исключён из команды.'

    if (foundCoach.value?.publicId === coach.publicId) {
      foundCoach.value.alreadyInTeam = false
    }

    await loadCoaches()
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : 'Не удалось исключить тренера.'
  } finally {
    busy.value = false
  }
}

onMounted(() => {
  void loadCoaches()
})

onUnmounted(() => controller.abort())</script>

<template>
  <section>
    <div class="coach-team-details__section-heading">
      <div>
        <h2>Тренеры команды</h2>
        <p>Тренеров: {{ coaches.length }}</p>
      </div>

      <button v-if="!showForm"
              type="button"
              class="btn btn-primary"
              :disabled="busy || isLoading"
              @click="openForm">
        + Добавить тренера
      </button>
    </div>

    <p v-if="error"
       class="coach-team-details__error"
       role="alert">
      {{ error }}
    </p>

    <p v-if="success"
       class="coach-team-details__success"
       role="status">
      {{ success }}
    </p>

    <div v-if="showForm"
         class="coach-team-details__panel">
      <h2>Добавление тренера</h2>

      <p class="coach-team-details__hint">
        Пользователь должен быть зарегистрирован
        и иметь роль тренера.
      </p>

      <form class="coach-team-details__lookup"
            @submit.prevent="searchCoach">
        <label for="coach-public-id">
          Публичный ID тренера
        </label>

        <div class="coach-team-details__lookup-row">
          <input id="coach-public-id"
                 v-model="publicId"
                 type="text"
                 inputmode="numeric"
                 placeholder="Например, 10000042"
                 :disabled="busy"
                 @input="resetSearch" />

          <button type="submit"
                  class="btn btn-primary"
                  :disabled="busy">
            Найти
          </button>
        </div>
      </form>

      <div v-if="foundCoach"
           class="coach-team-details__found-user">
        <div>
          <strong>{{ fullName(foundCoach) }}</strong>
          <p>ID: {{ foundCoach.publicId }}</p>

          <p v-if="foundCoach.alreadyInTeam">
            Тренер уже состоит в команде.
          </p>
        </div>

        <button type="button"
                class="btn btn-primary"
                :disabled="busy || foundCoach.alreadyInTeam"
                @click="addCoach">
          Добавить в команду
        </button>
      </div>

      <div class="coach-team-details__actions">
        <button type="button"
                class="btn btn-outline"
                :disabled="busy"
                @click="closeForm">
          Закрыть
        </button>
      </div>
    </div>

    <p v-if="isLoading"
       class="coach-team-details__message">
      Загрузка тренеров...
    </p>

    <div v-else
         class="coach-team-details__table-wrapper">
      <table class="coach-team-details__table">
        <thead>
          <tr>
            <th>ФИО</th>
            <th>Публичный ID</th>
            <th>Действия</th>
          </tr>
        </thead>

        <tbody>
          <tr v-for="coach in coaches"
              :key="coach.publicId">
            <td>
              {{ fullName(coach) }}
              <span v-if="coach.isSelf"> (Вы)</span>
            </td>

            <td>{{ coach.publicId }}</td>

            <td>
              <button v-if="!coach.isSelf"
                      type="button"
                      class="coach-team-details__delete"
                      :disabled="busy"
                      @click="excludeCoach(coach)">
                Исключить
              </button>

              <span v-else>—</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <button v-if="!isLoading && error"
            type="button"
            class="btn btn-outline"
            :disabled="busy"
            @click="loadCoaches">
      Обновить список
    </button>
  </section>
</template>
