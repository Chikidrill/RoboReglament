
<script setup lang="ts">
  import { onUnmounted, ref, watch } from 'vue'
  import { useRoute } from 'vue-router'

  import { getMyTeams, updateTeam } from '@/api/teams'

  import {
    getTeamParticipants,
    lookupParticipant,
    createParticipant,
    deleteParticipant,
  } from '@/api/participants'

  import type { Team } from '@/types/teams'

  import type {
    TeamParticipant,
    ParticipantLookupResponse,
  } from '@/types/participant'

  const route = useRoute()

  const team = ref<Team | null>(null)
  const participants = ref<TeamParticipant[]>([])

  const isLoading = ref(true)
  const loadError = ref('')
  const successMessage = ref('')

  // Редактирование команды
  const isEditingTeam = ref(false)
  const isSavingTeam = ref(false)
  const teamError = ref('')

  const teamForm = ref({
    name: '',
    organization: '',
    city: '',
  })

  // Поиск и добавление участников
  const showAddForm = ref(false)
  const publicIdInput = ref('')
  const lookupResult = ref<ParticipantLookupResponse | null>(null)

  const isSearching = ref(false)
  const isAdding = ref(false)

  const searchError = ref('')
  const addError = ref('')
  const deleteError = ref('')
  const deletingParticipantId = ref<number | null>(null)

  let loadController: AbortController | null = null
  let searchController: AbortController | null = null

  function getTeamId(): number {
    const id = Number(route.params.id)

    return Number.isSafeInteger(id) && id > 0
      ? id
      : 0
  }

  function getFullName(person: {
    lastName: string
    firstName: string
    middleName: string | null
  }): string {
    return [
      person.lastName,
      person.firstName,
      person.middleName,
    ]
      .filter(Boolean)
      .join(' ')
  }

  function sortParticipants() {
    participants.value.sort((a, b) =>
      getFullName(a).localeCompare(getFullName(b), 'ru'),
    )
  }

  function syncParticipantsCount() {
    if (team.value) {
      team.value.participantsCount = participants.value.length
    }
  }

  // Загрузка информации о команде
  async function loadDetails() {
    loadController?.abort()
    searchController?.abort()

    const controller = new AbortController()
    loadController = controller

    isLoading.value = true
    loadError.value = ''
    successMessage.value = ''

    team.value = null
    participants.value = []

    isEditingTeam.value = false
    showAddForm.value = false
    resetLookup()

    const teamId = getTeamId()

    if (!teamId) {
      loadError.value = 'Некорректный ID команды.'
      isLoading.value = false
      return
    }

    try {
      // Получаем только команды авторизованного тренера
      const myTeams = await getMyTeams(controller.signal)

      const myTeam = myTeams.find(
        item => item.id === teamId,
      )

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

  // Редактирование команды
  function startEditingTeam() {
    if (!team.value) return

    teamForm.value = {
      name: team.value.name,
      organization: team.value.organization ?? '',
      city: team.value.city ?? '',
    }

    teamError.value = ''
    isEditingTeam.value = true
  }

  function cancelEditingTeam() {
    if (isSavingTeam.value) return

    isEditingTeam.value = false
    teamError.value = ''
  }

  async function saveTeam() {
    if (!team.value || isSavingTeam.value) return

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
        organization:
          teamForm.value.organization.trim() || null,
        city: teamForm.value.city.trim() || null,
      })

      team.value = updated
      isEditingTeam.value = false
      successMessage.value = 'Данные команды обновлены.'
    } catch (error) {
      teamError.value =
        error instanceof Error
          ? error.message
          : 'Не удалось обновить команду.'
    } finally {
      isSavingTeam.value = false
    }
  }

  // Сброс результатов поиска
  function resetLookup() {
    searchController?.abort()

    lookupResult.value = null
    searchError.value = ''
    addError.value = ''
    isSearching.value = false
  }

  function openAddForm() {
    publicIdInput.value = ''
    resetLookup()

    successMessage.value = ''
    showAddForm.value = true
  }

  function closeAddForm() {
    if (isAdding.value) return

    showAddForm.value = false
    publicIdInput.value = ''
    resetLookup()
  }

  // Поиск зарегистрированного участника по PublicId
  async function searchUser() {
    if (!team.value || isAdding.value) return

    resetLookup()

    const rawId = publicIdInput.value.trim()
    const publicId = Number(rawId)

    if (
      !/^[1-9]\d*$/.test(rawId) ||
      !Number.isSafeInteger(publicId)
    ) {
      searchError.value =
        'Введите корректный числовой ID пользователя.'
      return
    }

    const controller = new AbortController()
    searchController = controller

    isSearching.value = true
    searchError.value = ''
    addError.value = ''

    try {
      lookupResult.value = await lookupParticipant(
        team.value.id,
        publicId,
        controller.signal,
      )
    } catch (error) {
      if (!controller.signal.aborted) {
        searchError.value =
          error instanceof Error
            ? error.message
            : 'Не удалось найти пользователя.'
      }
    } finally {
      if (!controller.signal.aborted) {
        isSearching.value = false
      }
    }
  }

  // Добавление найденного аккаунта в команду
  async function addParticipant() {
    if (
      !team.value ||
      !lookupResult.value ||
      lookupResult.value.alreadyInTeam ||
      isAdding.value
    ) {
      return
    }

    const teamId = team.value.id
    const publicId = lookupResult.value.publicId

    isAdding.value = true
    addError.value = ''
    successMessage.value = ''

    try {
      const created = await createParticipant(
        teamId,
        { publicId },
      )

      participants.value.push(created)

      sortParticipants()
      syncParticipantsCount()

      successMessage.value =
        `Участник ${getFullName(created)} добавлен в команду.`

      closeAddFormAfterSuccess()
    } catch (error) {
      addError.value =
        error instanceof Error
          ? error.message
          : 'Не удалось добавить участника.'
    } finally {
      isAdding.value = false
    }
  }

  function closeAddFormAfterSuccess() {
    showAddForm.value = false
    publicIdInput.value = ''
    resetLookup()
  }

  // Исключение участника из команды
  async function removeParticipant(
    participant: TeamParticipant,
  ) {
    if (
      !team.value ||
      deletingParticipantId.value !== null
    ) {
      return
    }

    const confirmed = window.confirm(
      `Исключить ${getFullName(participant)} из команды?`,
    )

    if (!confirmed) return

    deletingParticipantId.value = participant.id
    deleteError.value = ''
    successMessage.value = ''

    try {
      await deleteParticipant(
        team.value.id,
        participant.id,
      )

      participants.value = participants.value.filter(
        item => item.id !== participant.id,
      )

      syncParticipantsCount()

      // Если этот пользователь найден в форме,
      // обновляем признак членства.
      if (
        lookupResult.value?.publicId ===
        participant.publicId
      ) {
        lookupResult.value.alreadyInTeam = false
      }

      successMessage.value =
        'Участник исключён из команды.'
    } catch (error) {
      deleteError.value =
        error instanceof Error
          ? error.message
          : 'Не удалось исключить участника.'
    } finally {
      deletingParticipantId.value = null
    }
  }

  // Реакция на переход между командами
  watch(
    () => route.params.id,
    () => {
      void loadDetails()
    },
    { immediate: true },
  )

  onUnmounted(() => {
    loadController?.abort()
    searchController?.abort()
  })
</script>

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
      <p>{{ loadError }}</p>

      <button type="button"
              class="btn btn-outline"
              @click="loadDetails">
        Повторить
      </button>
    </div>

    <template v-else-if="team">
      <!-- Информация о команде -->
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
                  :disabled="
              isSavingTeam || !teamForm.name.trim()
            ">
            {{ isSavingTeam ? 'Сохранение...' : 'Сохранить' }}
          </button>
        </div>
      </form>

      <!-- Заголовок состава -->
      <div class="coach-team-details__section-heading">
        <div>
          <h2>Состав команды</h2>
          <p>Участников: {{ participants.length }}</p>
        </div>

        <button v-if="!showAddForm"
                type="button"
                class="btn btn-primary"
                @click="openAddForm">
          + Добавить участника
        </button>
      </div>

      <!-- Поиск и добавление -->
      <div v-if="showAddForm"
           class="coach-team-details__panel">
        <h2>Добавить участника</h2>

        <p class="coach-team-details__hint">
          Введите публичный ID зарегистрированного
          пользователя. Его можно узнать в личном
          кабинете участника.
        </p>

        <form class="coach-team-details__lookup"
              @submit.prevent="searchUser">
          <label for="participant-public-id">
            Публичный ID пользователя
          </label>

          <div class="coach-team-details__lookup-row">
            <input id="participant-public-id"
                   v-model="publicIdInput"
                   type="text"
                   inputmode="numeric"
                   autocomplete="off"
                   placeholder="Например, 10000042"
                   :disabled="isAdding"
                   @input="resetLookup" />

            <button type="submit"
                    class="btn btn-primary"
                    :disabled="isSearching || isAdding">
              {{ isSearching ? 'Поиск...' : 'Найти' }}
            </button>
          </div>
        </form>

        <p v-if="searchError"
           class="coach-team-details__error"
           role="alert">
          {{ searchError }}
        </p>

        <!-- Результат поиска -->
        <div v-if="lookupResult"
             class="coach-team-details__found-user">
          <div>
            <strong>
              {{ getFullName(lookupResult) }}
            </strong>

            <p>
              Публичный ID:
              {{ lookupResult.publicId }}
            </p>

            <p v-if="lookupResult.alreadyInTeam"
               class="coach-team-details__already">
              Пользователь уже состоит в команде.
            </p>
          </div>

          <button type="button"
                  class="btn btn-primary"
                  :disabled="
              lookupResult.alreadyInTeam || isAdding
            "
                  @click="addParticipant">
            {{
              isAdding
                ? 'Добавление...'
                : 'Добавить в команду'
            }}
          </button>
        </div>

        <p v-if="addError"
           class="coach-team-details__error"
           role="alert">
          {{ addError }}
        </p>

        <div class="coach-team-details__actions">
          <button type="button"
                  class="btn btn-outline"
                  :disabled="isAdding"
                  @click="closeAddForm">
            Закрыть
          </button>
        </div>
      </div>

      <!-- Ошибка удаления -->
      <p v-if="deleteError"
         class="coach-team-details__error"
         role="alert">
        {{ deleteError }}
      </p>

      <!-- Пустая команда -->
      <div v-if="participants.length === 0"
           class="coach-team-details__message">
        В команде пока нет участников.
        Добавьте зарегистрированного пользователя
        по его публичному ID.
      </div>

      <!-- Таблица состава -->
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
            <tr v-for="participant in participants"
                :key="participant.id">
              <td>
                {{ getFullName(participant) }}
              </td>

              <td>
                {{ participant.publicId }}
              </td>

              <td>
                <button type="button"
                        class="coach-team-details__delete"
                        :disabled="
                    deletingParticipantId !== null
                  "
                        @click="removeParticipant(participant)">
                  {{
                    deletingParticipantId === participant.id
                      ? 'Исключение...'
                      : 'Исключить'
                  }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </section>
</template>
