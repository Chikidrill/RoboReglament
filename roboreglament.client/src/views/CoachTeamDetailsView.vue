
<script setup lang="ts">import {
  onUnmounted,
  ref,
  watch,
} from 'vue'
import { useRoute } from 'vue-router'

import {
  getMyTeams,
  updateTeam,
} from '@/api/teams'

import {
  getTeamParticipants,
  createParticipant,
  updateParticipant,
  deleteParticipant,
} from '@/api/participants'

import type { Team } from '@/types/team'
import type {
  TeamParticipant,
  ParticipantRequest,
} from '@/types/participant'

const route = useRoute()

const team = ref<Team | null>(null)
const participants = ref<TeamParticipant[]>([])

const isLoading = ref(true)
const loadError = ref('')
const successMessage = ref('')

const isEditingTeam = ref(false)
const isSavingTeam = ref(false)
const teamError = ref('')

const teamForm = ref({
  name: '',
  organization: '',
  city: '',
})

const showParticipantForm = ref(false)
const editingParticipantId = ref<number | null>(null)
const isSavingParticipant = ref(false)
const participantError = ref('')
const deleteError = ref('')
const deletingParticipantId = ref<number | null>(null)

function emptyParticipantForm() {
  return {
    firstName: '',
    lastName: '',
    middleName: '',
    birthDate: '',
  }
}

const participantForm = ref(emptyParticipantForm())

let loadController: AbortController | null = null

function getTeamId(): number {
  const id = Number(route.params.id)
  return Number.isSafeInteger(id) && id > 0 ? id : 0
}

function getFullName(participant: TeamParticipant) {
  return [
    participant.lastName,
    participant.firstName,
    participant.middleName,
  ].filter(Boolean).join(' ')
}

function formatDate(value: string | null): string {
  if (!value) {
    return 'Не указана'
  }

  const [year, month, day] = value.split('-')
  return `${day}.${month}.${year}`
}

function sortParticipants() {
  participants.value.sort((a, b) =>
    getFullName(a).localeCompare(
      getFullName(b),
      'ru',
    ),
  )
}

function syncParticipantsCount() {
  if (team.value) {
    team.value.participantsCount = participants.value.length
  }
}

async function loadDetails() {
  loadController?.abort()

  const controller = new AbortController()
  loadController = controller

  isLoading.value = true
  loadError.value = ''
  team.value = null
  participants.value = []

  isEditingTeam.value = false
  showParticipantForm.value = false
  editingParticipantId.value = null

  const teamId = getTeamId()

  if (!teamId) {
    loadError.value = 'Некорректный идентификатор команды.'
    isLoading.value = false
    return
  }

  try {
    // Проверяем, что команда принадлежит тренеру.
    const myTeams = await getMyTeams(controller.signal)
    const myTeam = myTeams.find(item => item.id === teamId)

    if (!myTeam) {
      loadError.value =
        'Команда не найдена среди ваших команд.'
      return
    }

    const result = await getTeamParticipants(
      teamId,
      controller.signal,
    )

    if (controller.signal.aborted) {
      return
    }

    team.value = myTeam
    participants.value = result
    sortParticipants()
    syncParticipantsCount()
  } catch (error) {
    if (!controller.signal.aborted) {
      loadError.value =
        error instanceof Error
          ? error.message
          : 'Не удалось загрузить команду.'
    }
  } finally {
    if (!controller.signal.aborted) {
      isLoading.value = false
    }
  }
}

function startEditingTeam() {
  if (!team.value) {
    return
  }

  teamForm.value = {
    name: team.value.name,
    organization: team.value.organization ?? '',
    city: team.value.city ?? '',
  }

  teamError.value = ''
  isEditingTeam.value = true
}

function cancelEditingTeam() {
  if (isSavingTeam.value) {
    return
  }

  isEditingTeam.value = false
  teamError.value = ''
}

async function saveTeam() {
  if (!team.value || isSavingTeam.value) {
    return
  }

  const name = teamForm.value.name.trim()

  if (!name) {
    teamError.value = 'Введите название команды.'
    return
  }

  isSavingTeam.value = true
  teamError.value = ''
  successMessage.value = ''

  try {
    const updated = await updateTeam(team.value.id, {
      name,
      organization: teamForm.value.organization.trim() || null,
      city: teamForm.value.city.trim() || null,
    })

    team.value = updated
    isEditingTeam.value = false
    successMessage.value = 'Данные команды обновлены.'
  } catch (error) {
    teamError.value =
      error instanceof Error
        ? error.message
        : 'Не удалось сохранить команду.'
  } finally {
    isSavingTeam.value = false
  }
}

function startAddingParticipant() {
  participantForm.value = emptyParticipantForm()
  editingParticipantId.value = null
  participantError.value = ''
  deleteError.value = ''
  showParticipantForm.value = true
}

function startEditingParticipant(participant: TeamParticipant) {
  participantForm.value = {
    firstName: participant.firstName,
    lastName: participant.lastName,
    middleName: participant.middleName ?? '',
    birthDate: participant.birthDate ?? '',
  }

  editingParticipantId.value = participant.id
  participantError.value = ''
  deleteError.value = ''
  showParticipantForm.value = true
}

function cancelParticipantForm() {
  if (isSavingParticipant.value) {
    return
  }

  showParticipantForm.value = false
  editingParticipantId.value = null
  participantError.value = ''
}

async function saveParticipant() {
  if (!team.value || isSavingParticipant.value) {
    return
  }

  const data: ParticipantRequest = {
    firstName: participantForm.value.firstName.trim(),
    lastName: participantForm.value.lastName.trim(),
    middleName: participantForm.value.middleName.trim() || null,
    birthDate: participantForm.value.birthDate || null,
  }

  if (!data.firstName || !data.lastName) {
    participantError.value =
      'Имя и фамилия участника обязательны.'
    return
  }

  isSavingParticipant.value = true
  participantError.value = ''
  successMessage.value = ''

  try {
    if (editingParticipantId.value !== null) {
      const updated = await updateParticipant(
        team.value.id,
        editingParticipantId.value,
        data,
      )

      participants.value = participants.value.map(
        item => item.id === updated.id ? updated : item,
      )

      successMessage.value =
        'Данные участника обновлены.'
    } else {
      const created = await createParticipant(
        team.value.id,
        data,
      )

      participants.value.push(created)
      successMessage.value =
        'Участник добавлен в команду.'
    }

    sortParticipants()
    syncParticipantsCount()

    showParticipantForm.value = false
    editingParticipantId.value = null
    participantForm.value = emptyParticipantForm()
  } catch (error) {
    participantError.value =
      error instanceof Error
        ? error.message
        : 'Не удалось сохранить участника.'
  } finally {
    isSavingParticipant.value = false
  }
}

async function removeParticipant(participant: TeamParticipant) {
  if (!team.value || deletingParticipantId.value !== null) {
    return
  }

  const confirmed = window.confirm(
    `Удалить участника «${getFullName(participant)}» из команды?`,
  )

  if (!confirmed) {
    return
  }

  deletingParticipantId.value = participant.id
  deleteError.value = ''
  successMessage.value = ''

  try {
    await deleteParticipant(team.value.id, participant.id)

    participants.value = participants.value.filter(
      item => item.id !== participant.id,
    )

    if (editingParticipantId.value === participant.id) {
      cancelParticipantForm()
    }

    syncParticipantsCount()

    successMessage.value =
      'Участник удалён из команды.'
  } catch (error) {
    deleteError.value =
      error instanceof Error
        ? error.message
        : 'Не удалось удалить участника.'
  } finally {
    deletingParticipantId.value = null
  }
}

watch(
  () => route.params.id,
  () => {
    void loadDetails()
  },
  { immediate: true },
)

onUnmounted(() => {
  loadController?.abort()
})</script>

<template>
  <section class="coach-team-details">
    <RouterLink to="/account/teams"
                class="coach-team-details__back">
      ← Мои команды
    </RouterLink>

    <div v-if="isLoading"
         class="coach-team-details__message"
         role="status">
      Загрузка команды...
    </div>

    <div v-else-if="loadError"
         class="coach-team-details__message"
         role="alert">
      {{ loadError }}
    </div>

    <template v-else-if="team">
      <header class="coach-team-details__heading">
        <div>
          <h1>{{ team.name }}</h1>
          <p>
            {{ team.organization || 'Организация не указана' }}
            ·
            {{ team.city || 'Город не указан' }}
          </p>
        </div>

        <button v-if="!isEditingTeam"
                type="button"
                class="btn btn-outline"
                @click="startEditingTeam">
          Редактировать команду
        </button>
      </header>

      <p v-if="successMessage"
         class="coach-team-details__success"
         role="status">
        {{ successMessage }}
      </p>

      <!-- Редактирование команды -->
      <form v-if="isEditingTeam"
            class="coach-team-details__panel"
            @submit.prevent="saveTeam">
        <h2>Редактирование команды</h2>

        <div class="coach-team-details__fields">
          <label>
            Название команды *
            <input v-model="teamForm.name"
                   type="text"
                   maxlength="200"
                   required
                   :disabled="isSavingTeam" />
          </label>

          <label>
            Организация
            <input v-model="teamForm.organization"
                   type="text"
                   maxlength="300"
                   :disabled="isSavingTeam" />
          </label>

          <label>
            Город
            <input v-model="teamForm.city"
                   type="text"
                   maxlength="200"
                   :disabled="isSavingTeam" />
          </label>
        </div>

        <p v-if="teamError"
           class="coach-team-details__error"
           role="alert">
          {{ teamError }}
        </p>

        <div class="coach-team-details__actions">
          <button type="button"
                  class="btn btn-outline"
                  :disabled="isSavingTeam"
                  @click="cancelEditingTeam">
            Отмена
          </button>

          <button type="submit"
                  class="btn btn-primary"
                  :disabled="isSavingTeam || !teamForm.name.trim()">
            {{ isSavingTeam ? 'Сохранение...' : 'Сохранить' }}
          </button>
        </div>
      </form>

      <!-- Состав команды -->
      <div class="coach-team-details__section-heading">
        <div>
          <h2>Состав команды</h2>
          <p>Участников: {{ participants.length }}</p>
        </div>

        <button v-if="!showParticipantForm"
                type="button"
                class="btn btn-primary"
                @click="startAddingParticipant">
          + Добавить участника
        </button>
      </div>

      <!-- Добавление / редактирование участника -->
      <form v-if="showParticipantForm"
            class="coach-team-details__panel"
            @submit.prevent="saveParticipant">
        <h2>
          {{
            editingParticipantId === null
              ? 'Новый участник'
              : 'Редактирование участника'
          }}
        </h2>

        <div class="coach-team-details__fields">
          <label>
            Фамилия *
            <input v-model="participantForm.lastName"
                   type="text"
                   maxlength="100"
                   required
                   :disabled="isSavingParticipant" />
          </label>

          <label>
            Имя *
            <input v-model="participantForm.firstName"
                   type="text"
                   maxlength="100"
                   required
                   :disabled="isSavingParticipant" />
          </label>

          <label>
            Отчество
            <input v-model="participantForm.middleName"
                   type="text"
                   maxlength="100"
                   :disabled="isSavingParticipant" />
          </label>

          <label>
            Дата рождения
            <input v-model="participantForm.birthDate"
                   type="date"
                   :disabled="isSavingParticipant" />
          </label>
        </div>

        <p v-if="participantError"
           class="coach-team-details__error"
           role="alert">
          {{ participantError }}
        </p>

        <div class="coach-team-details__actions">
          <button type="button"
                  class="btn btn-outline"
                  :disabled="isSavingParticipant"
                  @click="cancelParticipantForm">
            Отмена
          </button>

          <button type="submit"
                  class="btn btn-primary"
                  :disabled="
              isSavingParticipant ||
              !participantForm.firstName.trim() ||
              !participantForm.lastName.trim()
            ">
            {{
              isSavingParticipant
                ? 'Сохранение...'
                : 'Сохранить участника'
            }}
          </button>
        </div>
      </form>

      <p v-if="deleteError"
         class="coach-team-details__error"
         role="alert">
        {{ deleteError }}
      </p>

      <div v-if="participants.length === 0"
           class="coach-team-details__message">
        В команде пока нет участников.
      </div>

      <div v-else
           class="coach-team-details__table-wrapper">
        <table class="coach-team-details__table">
          <thead>
            <tr>
              <th>ФИО</th>
              <th>Дата рождения</th>
              <th>Действия</th>
            </tr>
          </thead>

          <tbody>
            <tr v-for="participant in participants"
                :key="participant.id">
              <td>
                {{ getFullName(participant) }}
              </td>

              <td>
                {{ formatDate(participant.birthDate) }}
              </td>

              <td>
                <div class="coach-team-details__row-actions">
                  <button type="button"
                          class="btn btn-outline"
                          :disabled="
                      isSavingParticipant ||
                      deletingParticipantId !== null
                    "
                          @click="startEditingParticipant(participant)">
                    Изменить
                  </button>

                  <button type="button"
                          class="coach-team-details__delete"
                          :disabled="
                      isSavingParticipant ||
                      deletingParticipantId !== null
                    "
                          @click="removeParticipant(participant)">
                    {{
                      deletingParticipantId === participant.id
                        ? 'Удаление...'
                        : 'Удалить'
                    }}
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </section>
</template>
