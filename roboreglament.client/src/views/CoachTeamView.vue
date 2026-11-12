
<script setup lang="ts">import {
  onMounted,
  onUnmounted,
  ref,
} from 'vue'

import {
  createTeam,
  getMyTeams,
} from '@/api/teams'

import type { Team } from '@/types/teams'

const teams = ref<Team[]>([])

const isLoading = ref(true)
const loadError = ref('')

const showCreateForm = ref(false)
const isCreating = ref(false)
const createError = ref('')
const successMessage = ref('')

const name = ref('')
const organization = ref('')
const city = ref('')

const controller = new AbortController()

async function loadTeams() {
  if (controller.signal.aborted) {
    return
  }

  isLoading.value = true
  loadError.value = ''

  try {
    teams.value = await getMyTeams(controller.signal)
  } catch (error) {
    if (!controller.signal.aborted) {
      loadError.value =
        error instanceof Error
          ? error.message
          : 'Не удалось загрузить команды.'
    }
  } finally {
    if (!controller.signal.aborted) {
      isLoading.value = false
    }
  }
}

function openCreateForm() {
  createError.value = ''
  successMessage.value = ''
  showCreateForm.value = true
}

function closeCreateForm() {
  if (isCreating.value) {
    return
  }

  showCreateForm.value = false
  createError.value = ''
}

async function handleCreateTeam() {
  if (isCreating.value) {
    return
  }

  const teamName = name.value.trim()

  if (!teamName) {
    createError.value = 'Введите название команды.'
    return
  }

  isCreating.value = true
  createError.value = ''
  successMessage.value = ''

  try {
    const createdTeam = await createTeam({
      name: teamName,
      organization: organization.value.trim() || null,
      city: city.value.trim() || null,
    })

    teams.value = [...teams.value, createdTeam].sort(
      (a, b) => a.name.localeCompare(b.name, 'ru'),
    )

    name.value = ''
    organization.value = ''
    city.value = ''

    showCreateForm.value = false

    successMessage.value =
      `Команда «${createdTeam.name}» успешно создана.`
  } catch (error) {
    createError.value =
      error instanceof Error
        ? error.message
        : 'Не удалось создать команду.'
  } finally {
    isCreating.value = false
  }
}

onMounted(() => {
  void loadTeams()
})

onUnmounted(() => {
  controller.abort()
})</script>

<template>
  <section class="coach-teams">
    <div class="coach-teams__heading">
      <div>
        <h1>Мои команды</h1>

        <p>
          Управление командами и составами участников.
        </p>
      </div>

      <button v-if="!showCreateForm"
              type="button"
              class="btn btn-primary"
              @click="openCreateForm">
        + Создать команду
      </button>
    </div>

    <p v-if="successMessage"
       class="coach-teams__success"
       role="status">
      {{ successMessage }}
    </p>

    <!-- Форма создания команды -->
    <form v-if="showCreateForm"
          class="coach-teams__form"
          @submit.prevent="handleCreateTeam">
      <h2>Новая команда</h2>

      <div class="coach-teams__field">
        <label for="team-name">
          Название команды *
        </label>

        <input id="team-name"
               v-model="name"
               type="text"
               maxlength="200"
               placeholder="Например, Вектор"
               required
               :disabled="isCreating" />
      </div>

      <div class="coach-teams__form-row">
        <div class="coach-teams__field">
          <label for="team-organization">
            Организация
          </label>

          <input id="team-organization"
                 v-model="organization"
                 type="text"
                 maxlength="300"
                 placeholder="Например, ТУСУР"
                 :disabled="isCreating" />
        </div>

        <div class="coach-teams__field">
          <label for="team-city">
            Город
          </label>

          <input id="team-city"
                 v-model="city"
                 type="text"
                 maxlength="200"
                 placeholder="Например, Томск"
                 :disabled="isCreating" />
        </div>
      </div>

      <p v-if="createError"
         class="coach-teams__error"
         role="alert">
        {{ createError }}
      </p>

      <div class="coach-teams__form-actions">
        <button type="button"
                class="btn btn-outline"
                :disabled="isCreating"
                @click="closeCreateForm">
          Отмена
        </button>

        <button type="submit"
                class="btn btn-primary"
                :disabled="isCreating || !name.trim()">
          {{ isCreating ? 'Создание...' : 'Создать команду' }}
        </button>
      </div>
    </form>

    <!-- Список команд -->
    <div class="coach-teams__list-heading">
      <h2>Список команд</h2>

      <span v-if="!isLoading && !loadError">
        Всего команд: {{ teams.length }}
      </span>
    </div>

    <div v-if="isLoading"
         class="coach-teams__message"
         role="status">
      Загрузка команд...
    </div>

    <div v-else-if="loadError"
         class="coach-teams__message">
      <p role="alert">{{ loadError }}</p>

      <button type="button"
              class="btn btn-outline"
              @click="loadTeams">
        Повторить
      </button>
    </div>

    <div v-else-if="teams.length === 0"
         class="coach-teams__empty">
      <h3>У вас пока нет команд</h3>

      <p>
        Создайте первую команду, чтобы добавить
        участников и подавать заявки на турниры.
      </p>

      <button v-if="!showCreateForm"
              type="button"
              class="btn btn-primary"
              @click="openCreateForm">
        Создать первую команду
      </button>
    </div>

    <div v-else
         class="coach-teams__grid">
      <article v-for="team in teams"
               :key="team.id"
               class="coach-team-card">
        <div class="coach-team-card__heading">
          <h3>{{ team.name }}</h3>

          <span class="coach-team-card__count">
            {{ team.participantsCount }} участников
          </span>
        </div>

        <div class="coach-team-card__details">
          <p>
            <strong>Организация:</strong>
            {{ team.organization || 'Не указана' }}
          </p>

          <p>
            <strong>Город:</strong>
            {{ team.city || 'Не указан' }}
          </p>
        </div>
      </article>
    </div>
  </section>
</template>
