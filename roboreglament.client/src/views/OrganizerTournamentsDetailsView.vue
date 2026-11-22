
<script setup lang="ts">import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { useAuthStore } from '@/stores/auth'

import {
  getManagedTournament,
  updateTournament,
  deleteTournament,
  updateTournamentStatus,
  createDiscipline,
  updateDiscipline,
  deleteDiscipline,
  getTournamentOrganizers,
  lookupTournamentOrganizer,
  addTournamentOrganizer,
  removeTournamentOrganizer,
} from '@/api/tournaments'

import TournamentEditor
  from '@/components/tournaments/TournamentEditor.vue'

import OrganizerApplicationsPanel
  from '@/components/tournaments/OrganizerApplicationsPanel.vue'

import type {
  Tournament,
  TournamentRequest,
  TournamentStatus,
  Discipline,
  TournamentOrganizer,
  TournamentOrganizerLookup,
} from '@/types/tournament'

import {
  formatTournamentDateRange,
  formatDateTime,
  getTournamentStatus,
} from '@/utils/tournament'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const tournament = ref<Tournament | null>(null)
const organizers = ref<TournamentOrganizer[]>([])

const isLoading = ref(true)
const busy = ref(false)
const editingTournament = ref(false)

const error = ref('')
const success = ref('')

type Tab = 'overview' | 'disciplines' | 'organizers' | 'applications'

const activeTab = ref<Tab>('overview')

const tabs: { value: Tab; label: string }[] = [
  { value: 'overview', label: 'Основное' },
  { value: 'disciplines', label: 'Дисциплины' },
  { value: 'organizers', label: 'Организаторы' },
  { value: 'applications', label: 'Заявки и аккредитация' },
]

const showDisciplineForm = ref(false)
const editingDisciplineId = ref<number | null>(null)
const disciplineName = ref('')
const disciplineDescription = ref('')

const organizerPublicId = ref('')
const foundOrganizer = ref<TournamentOrganizerLookup | null>(null)

const canEdit = computed(() =>
  tournament.value?.status === 'Draft' ||
  tournament.value?.status === 'Registration',
)

const nextStatus = computed<{
  status: TournamentStatus
  label: string
} | null>(() => {
  switch (tournament.value?.status) {
    case 'Draft':
      return {
        status: 'Registration',
        label: 'Открыть регистрацию',
      }
    case 'Registration':
      return {
        status: 'InProgress',
        label: 'Начать турнир',
      }
    case 'InProgress':
      return {
        status: 'Completed',
        label: 'Завершить турнир',
      }
    default:
      return null
  }
})

function fullName(person: {
  lastName: string
  firstName: string
  middleName: string | null
}) {
  return [
    person.lastName,
    person.firstName,
    person.middleName,
  ].filter(Boolean).join(' ')
}

async function load(id: number, signal: AbortSignal) {
  isLoading.value = true
  tournament.value = null
  error.value = ''
  success.value = ''

  if (!Number.isSafeInteger(id) || id <= 0) {
    error.value = 'Некорректный ID турнира.'
    isLoading.value = false
    return
  }

  try {
    const [details, people] = await Promise.all([
      getManagedTournament(id, signal),
      getTournamentOrganizers(id, signal),
    ])

    if (signal.aborted) return

    tournament.value = details
    organizers.value = people
  } catch (err) {
    if (!signal.aborted) {
      error.value = err instanceof Error
        ? err.message
        : 'Не удалось загрузить турнир.'
    }
  } finally {
    if (!signal.aborted) {
      isLoading.value = false
    }
  }
}

async function run(action: () => Promise<void>) {
  if (busy.value) return

  const id = tournament.value?.id

  busy.value = true
  error.value = ''
  success.value = ''

  try {
    await action()
  } catch (err) {
    if (tournament.value?.id === id) {
      error.value = err instanceof Error
        ? err.message
        : 'Не удалось выполнить операцию.'
    }
  } finally {
    busy.value = false
  }
}

async function saveTournament(data: TournamentRequest) {
  const id = tournament.value?.id
  if (!id) return

  await run(async () => {
    const updated = await updateTournament(id, data)

    if (tournament.value?.id !== id) return

    tournament.value = updated
    editingTournament.value = false
    success.value = 'Изменения сохранены.'
  })
}

async function changeStatus() {
  const id = tournament.value?.id
  const next = nextStatus.value

  if (!id || !next) return

  if (!window.confirm(
    `${next.label}? Вернуться к предыдущему этапу нельзя.`,
  )) return

  await run(async () => {
    const updated = await updateTournamentStatus(
      id,
      next.status,
    )

    if (tournament.value?.id !== id) return

    tournament.value = updated
    editingTournament.value = false
    closeDisciplineForm()

    success.value = 'Статус турнира изменён.'
  })
}

async function removeTournament() {
  const id = tournament.value?.id
  if (!id) return

  if (!window.confirm('Удалить черновик турнира?')) return

  await run(async () => {
    await deleteTournament(id)

    if (tournament.value?.id === id) {
      await router.push({ name: 'organizer-tournaments' })
    }
  })
}

// Дисциплины.

function openDisciplineForm(discipline?: Discipline) {
  editingDisciplineId.value = discipline?.id ?? null
  disciplineName.value = discipline?.name ?? ''
  disciplineDescription.value = discipline?.description ?? ''
  showDisciplineForm.value = true
  error.value = ''
}

function closeDisciplineForm() {
  showDisciplineForm.value = false
  editingDisciplineId.value = null
  disciplineName.value = ''
  disciplineDescription.value = ''
}

async function saveDiscipline() {
  const id = tournament.value?.id
  if (!id || !disciplineName.value.trim()) return

  const data = {
    name: disciplineName.value.trim(),
    description: disciplineDescription.value.trim() || null,
  }

  const editingId = editingDisciplineId.value

  await run(async () => {
    const saved = editingId === null
      ? await createDiscipline(id, data)
      : await updateDiscipline(id, editingId, data)

    if (tournament.value?.id !== id) return

    const existing = tournament.value.disciplines

    tournament.value.disciplines = (
      editingId === null
        ? [...existing, saved]
        : existing.map(item =>
            item.id === saved.id ? saved : item,
          )
    ).sort((a, b) => a.name.localeCompare(b.name, 'ru'))

    closeDisciplineForm()
    success.value = 'Дисциплина сохранена.'
  })
}

async function removeDiscipline(discipline: Discipline) {
  const id = tournament.value?.id
  if (!id) return

  if (!window.confirm(
    `Удалить дисциплину «${discipline.name}»?`,
  )) return

  await run(async () => {
    await deleteDiscipline(id, discipline.id)

    if (tournament.value?.id !== id) return

    tournament.value.disciplines =
      tournament.value.disciplines.filter(
        item => item.id !== discipline.id,
      )

    closeDisciplineForm()
    success.value = 'Дисциплина удалена.'
  })
}

// Организаторы.

async function searchOrganizer() {
  const id = tournament.value?.id
  if (!id) return

  foundOrganizer.value = null

  const raw = organizerPublicId.value.trim()
  const publicId = Number(raw)

  if (!/^[1-9]\d*$/.test(raw) ||
      !Number.isSafeInteger(publicId)) {
    error.value = 'Введите корректный публичный ID.'
    return
  }

  await run(async () => {
    const result = await lookupTournamentOrganizer(id, publicId)

    if (tournament.value?.id === id) {
      foundOrganizer.value = result
    }
  })
}

async function addOrganizer() {
  const id = tournament.value?.id
  const person = foundOrganizer.value

  if (!id || !person || person.alreadyOrganizer) return

  await run(async () => {
    await addTournamentOrganizer(id, person.id)

    const people = await getTournamentOrganizers(id)

    if (tournament.value?.id !== id) return

    organizers.value = people
    organizerPublicId.value = ''
    foundOrganizer.value = null
    success.value = 'Организатор добавлен.'
  })
}

async function removeOrganizer(person: TournamentOrganizer) {
  const id = tournament.value?.id
  if (!id || person.id === authStore.user?.id) return

  if (!window.confirm(
    `Исключить организатора ${fullName(person)}?`,
  )) return

  await run(async () => {
    await removeTournamentOrganizer(id, person.id)

    const people = await getTournamentOrganizers(id)

    if (tournament.value?.id !== id) return

    organizers.value = people
    foundOrganizer.value = null
    success.value = 'Организатор исключён.'
  })
}

watch(
  () => route.params.id,
  (id, _previous, onCleanup) => {
    const controller = new AbortController()

    onCleanup(() => controller.abort())

    activeTab.value = 'overview'
    editingTournament.value = false
    closeDisciplineForm()
    foundOrganizer.value = null
    organizerPublicId.value = ''

    void load(Number(id), controller.signal)
  },
  { immediate: true },
)</script>

<template>
  <section class="organizer-page">
    <RouterLink :to="{ name: 'organizer-tournaments' }"
                class="organizer-back">
      ← Мои турниры
    </RouterLink>

    <p v-if="isLoading">Загрузка турнира...</p>

    <p v-if="error" class="organizer-error" role="alert">
      {{ error }}
    </p>

    <p v-if="success" class="organizer-success" role="status">
      {{ success }}
    </p>

    <template v-if="!isLoading && tournament">
      <header class="organizer-heading">
        <div>
          <span class="status-badge"
                :class="`status-badge--${
              getTournamentStatus(tournament).tone
            }`">
            {{
              tournament.status === 'Draft'
                ? 'Черновик'
                : getTournamentStatus(tournament).label
            }}
          </span>

          <h1>{{ tournament.name }}</h1>

          <p>{{ tournament.location || 'Место не указано' }}</p>

          <p>
            {{
              formatTournamentDateRange(
                tournament.startDate,
                tournament.endDate,
              )
            }}
          </p>
        </div>

        <RouterLink v-if="tournament.status !== 'Draft'"
                    :to="{
            name: 'tournament-details',
            params: { id: tournament.id },
          }"
                    class="btn btn-outline">
          Публичная страница →
        </RouterLink>
      </header>

      <nav class="organizer-tabs">
        <button v-for="tab in tabs"
                :key="tab.value"
                type="button"
                :class="{ 'organizer-tab--active': activeTab === tab.value }"
                :disabled="busy"
                @click="activeTab = tab.value; error = ''; success = ''">
          {{ tab.label }}
        </button>
      </nav>

      <!-- Основные настройки -->

      <section v-if="activeTab === 'overview'" class="organizer-panel">
        <h2>Информация о турнире</h2>

        <TournamentEditor v-if="editingTournament"
                          :tournament="tournament"
                          :busy="busy"
                          @save="saveTournament"
                          @cancel="editingTournament = false" />

        <template v-else>
          <p>{{ tournament.description || 'Описание не добавлено.' }}</p>

          <p>
            Окончание регистрации:
            {{
              tournament.registrationDeadline
                ? formatDateTime(tournament.registrationDeadline)
                : 'Не установлено'
            }}
          </p>

          <p>Дисциплин: {{ tournament.disciplines.length }}</p>

          <div class="organizer-actions">
            <button v-if="canEdit"
                    type="button"
                    class="btn btn-outline"
                    :disabled="busy"
                    @click="editingTournament = true">
              Редактировать
            </button>

            <button v-if="nextStatus"
                    type="button"
                    class="btn btn-primary"
                    :disabled="
                busy ||
                (
                  tournament.status === 'Draft' &&
                  tournament.disciplines.length === 0
                )
              "
                    @click="changeStatus">
              {{ nextStatus.label }}
            </button>

            <button v-if="tournament.status === 'Draft'"
                    type="button"
                    class="organizer-danger"
                    :disabled="busy"
                    @click="removeTournament">
              Удалить турнир
            </button>
          </div>

          <p v-if="
              tournament.status === 'Draft' &&
              !tournament.disciplines.length
            ">
            Для открытия регистрации необходимо
            добавить хотя бы одну дисциплину.
          </p>
        </template>
      </section>

      <!-- Дисциплины -->

      <section v-if="activeTab === 'disciplines'"
               class="organizer-panel">
        <div class="organizer-heading">
          <h2>Дисциплины</h2>

          <button v-if="tournament.status === 'Draft'"
                  type="button"
                  class="btn btn-primary"
                  :disabled="busy"
                  @click="openDisciplineForm()">
            + Добавить дисциплину
          </button>
        </div>

        <p v-if="tournament.status !== 'Draft'">
          Редактирование дисциплин недоступно
          после открытия регистрации.
        </p>

        <form v-if="showDisciplineForm && tournament.status === 'Draft'"
              class="organizer-form"
              @submit.prevent="saveDiscipline">
          <label>
            Название *
            <input v-model="disciplineName"
                   required
                   maxlength="200"
                   :disabled="busy" />
          </label>

          <label>
            Описание
            <textarea v-model="disciplineDescription"
                      maxlength="2000"
                      rows="4"
                      :disabled="busy" />
          </label>

          <div class="organizer-actions">
            <button type="button"
                    class="btn btn-outline"
                    :disabled="busy"
                    @click="closeDisciplineForm">
              Отмена
            </button>

            <button type="submit"
                    class="btn btn-primary"
                    :disabled="busy || !disciplineName.trim()">
              Сохранить
            </button>
          </div>
        </form>

        <p v-if="!tournament.disciplines.length">
          Дисциплины пока не добавлены.
        </p>

        <article v-for="discipline in tournament.disciplines"
                 :key="discipline.id"
                 class="organizer-list-item">
          <div>
            <strong>{{ discipline.name }}</strong>
            <p>{{ discipline.description || 'Без описания' }}</p>
          </div>

          <div v-if="tournament.status === 'Draft'"
               class="organizer-actions">
            <button type="button"
                    class="btn btn-outline"
                    :disabled="busy"
                    @click="openDisciplineForm(discipline)">
              Изменить
            </button>

            <button type="button"
                    class="organizer-danger"
                    :disabled="busy"
                    @click="removeDiscipline(discipline)">
              Удалить
            </button>
          </div>
        </article>
      </section>

      <!-- Организаторы -->

      <section v-if="activeTab === 'organizers'"
               class="organizer-panel">
        <h2>Организаторы турнира</h2>

        <template v-if="tournament.status !== 'Completed'">
          <form class="organizer-form"
                @submit.prevent="searchOrganizer">
            <label>
              Публичный ID организатора
              <input v-model="organizerPublicId"
                     inputmode="numeric"
                     placeholder="Например, 10000042"
                     :disabled="busy"
                     @input="foundOrganizer = null" />
            </label>

            <button type="submit"
                    class="btn btn-outline"
                    :disabled="busy">
              Найти
            </button>
          </form>

          <article v-if="foundOrganizer" class="organizer-list-item">
            <div>
              <strong>{{ fullName(foundOrganizer) }}</strong>
              <p>ID: {{ foundOrganizer.publicId }}</p>
            </div>

            <button type="button"
                    class="btn btn-primary"
                    :disabled="busy || foundOrganizer.alreadyOrganizer"
                    @click="addOrganizer">
              {{
                foundOrganizer.alreadyOrganizer
                  ? 'Уже добавлен'
                  : 'Добавить'
              }}
            </button>
          </article>
        </template>

        <article v-for="person in organizers"
                 :key="person.id"
                 class="organizer-list-item">
          <div>
            <strong>
              {{ fullName(person) }}
              {{ person.id === authStore.user?.id ? '(Вы)' : '' }}
            </strong>

            <p>ID: {{ person.publicId }}</p>
          </div>

          <button v-if="
              tournament.status !== 'Completed' &&
              person.id !== authStore.user?.id
            "
                  type="button"
                  class="organizer-danger"
                  :disabled="busy"
                  @click="removeOrganizer(person)">
            Исключить
          </button>
        </article>
      </section>

      <!-- Заявки -->

      <OrganizerApplicationsPanel v-if="activeTab === 'applications'"
                                  :key="tournament.id"
                                  :tournament="tournament" />
    </template>
  </section>
</template>
