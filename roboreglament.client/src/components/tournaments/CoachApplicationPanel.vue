
<script setup lang="ts">import { computed, ref, watch } from 'vue'

import { useAuthStore } from '@/stores/auth'

import { getMyTeams } from '@/api/teams'

import {
  getTournamentApplications,
  createTournamentApplication,
  refreshApplicationParticipants,
} from '@/api/applications'

import type { Team } from '@/types/teams'
import type { Tournament } from '@/types/tournament'

import type {
  TournamentApplication,
  ApplicationStatus,
} from '@/types/application'

const props = defineProps<{
  tournament: Tournament
}>()

const authStore = useAuthStore()

const teams = ref<Team[]>([])
const applications = ref<TournamentApplication[]>([])

const isLoading = ref(true)
const isSubmitting = ref(false)
const updatingApplicationId = ref<number | null>(null)

const selectedTeamId = ref(0)

const loadError = ref('')
const actionError = ref('')
const successMessage = ref('')

const registrationOpen = computed(() => {
  if (props.tournament.status !== 'Registration') {
    return false
  }

  const deadline = props.tournament.registrationDeadline

  if (!deadline) {
    return true
  }

  const deadlineMs = Date.parse(deadline)

  return Number.isFinite(deadlineMs) &&
    Date.now() <= deadlineMs
})

// Команды, для которых заявка ещё не создана.
const availableTeams = computed(() =>
  teams.value.filter(team =>
    !applications.value.some(
      application => application.teamId === team.id,
    ),
  ),
)

// Подача заявки требует хотя бы одного участника.
const selectedTeam = computed(() =>
  availableTeams.value.find(
    team => team.id === selectedTeamId.value,
  ),
)

const canSubmit = computed(() =>
  registrationOpen.value &&
  (selectedTeam.value?.participantsCount ?? 0) > 0 &&
  !isSubmitting.value,
)

function statusLabel(status: ApplicationStatus): string {
  switch (status) {
    case 'Pending':
      return 'На рассмотрении'
    case 'Approved':
      return 'Одобрена'
    case 'Rejected':
      return 'Отклонена'
  }
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('ru-RU', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function participantName(person: {
  lastName: string
  firstName: string
  middleName: string | null
}): string {
  return [
    person.lastName,
    person.firstName,
    person.middleName,
  ].filter(Boolean).join(' ')
}

// Получение команд тренера и его заявок.
async function loadData(signal: AbortSignal) {
  isLoading.value = true
  loadError.value = ''
  actionError.value = ''
  successMessage.value = ''

  teams.value = []
  applications.value = []
  selectedTeamId.value = 0

  try {
    const [myTeams, myApplications] = await Promise.all([
      getMyTeams(signal),
      getTournamentApplications(
        props.tournament.id,
        signal,
      ),
    ])

    if (signal.aborted) return

    teams.value = myTeams
    applications.value = myApplications

    const firstAvailable = myTeams.find(
      team => !myApplications.some(
        application => application.teamId === team.id,
      ),
    )

    selectedTeamId.value = firstAvailable?.id ?? 0
  } catch (error) {
    if (!signal.aborted) {
      loadError.value =
        error instanceof Error
          ? error.message
          : 'Не удалось получить заявки.'
    }
  } finally {
    if (!signal.aborted) {
      isLoading.value = false
    }
  }
}

// Перезагружаем данные при смене турнира
// или авторизованного пользователя.
watch(
  [
    () => props.tournament.id,
    () => authStore.isInitialized,
    () => authStore.user?.id,
    () => authStore.user?.roles.join(','),
  ],
  (_values, _oldValues, onCleanup) => {
    const isCoach = authStore.user?.roles.includes('Coach')

    if (!authStore.isInitialized || !isCoach) {
      isLoading.value = false
      return
    }

    const controller = new AbortController()

    onCleanup(() => controller.abort())

    void loadData(controller.signal)
  },
  { immediate: true },
)

// Подача новой заявки.
async function submitApplication() {
  if (!canSubmit.value) return

  isSubmitting.value = true
  actionError.value = ''
  successMessage.value = ''

  try {
    const created = await createTournamentApplication(
      props.tournament.id,
      {
        teamId: selectedTeamId.value,
      },
    )

    applications.value.unshift(created)

    // Убираем команду из доступных для подачи.
    selectedTeamId.value =
      availableTeams.value[0]?.id ?? 0

    successMessage.value =
      `Заявка команды «${created.teamName}» подана.`
  } catch (error) {
    actionError.value =
      error instanceof Error
        ? error.message
        : 'Не удалось подать заявку.'
  } finally {
    isSubmitting.value = false
  }
}

// Синхронизация заявки с текущим составом команды.
async function refreshParticipants(
  application: TournamentApplication,
) {
  if (
    updatingApplicationId.value !== null ||
    application.isAccredited ||
    !registrationOpen.value
  ) {
    return
  }

  const confirmed = window.confirm(
    'Обновить заявленный состав из текущего состава ' +
    'команды? Заявка снова получит статус ' +
    '«На рассмотрении».',
  )

  if (!confirmed) return

  updatingApplicationId.value = application.id
  actionError.value = ''
  successMessage.value = ''

  try {
    const updated = await refreshApplicationParticipants(
      props.tournament.id,
      application.id,
    )

    applications.value = applications.value.map(
      item => item.id === updated.id ? updated : item,
    )

    successMessage.value =
      `Состав заявки команды «${updated.teamName}» обновлён.`
  } catch (error) {
    actionError.value =
      error instanceof Error
        ? error.message
        : 'Не удалось обновить состав заявки.'
  } finally {
    updatingApplicationId.value = null
  }
}</script>

<template>
  <div class="coach-applications">
    <div v-if="isLoading"
         class="coach-applications__message"
         role="status">
      Загрузка заявок...
    </div>

    <div v-else-if="loadError"
         class="coach-applications__message"
         role="alert">
      {{ loadError }}
    </div>

    <template v-else>
      <p v-if="successMessage"
         class="coach-applications__success"
         role="status">
        {{ successMessage }}
      </p>

      <p v-if="actionError"
         class="coach-applications__error"
         role="alert">
        {{ actionError }}
      </p>

      <!-- Новая заявка -->
      <form v-if="registrationOpen && availableTeams.length > 0"
            class="coach-applications__form"
            @submit.prevent="submitApplication">
        <label for="application-team">
          Выберите команду
        </label>

        <select id="application-team"
                v-model.number="selectedTeamId"
                :disabled="isSubmitting">
          <option :value="0">
            Выберите команду
          </option>

          <option v-for="team in availableTeams"
                  :key="team.id"
                  :value="team.id">
            {{ team.name }} — {{ team.participantsCount }} участников
          </option>
        </select>

        <p v-if="selectedTeam && selectedTeam.participantsCount === 0"
           class="coach-applications__notice">
          В выбранной команде нет участников.
          Сначала добавьте их в состав.
        </p>

        <button type="submit"
                class="btn btn-primary tournament-details__button"
                :disabled="!canSubmit">
          {{
            isSubmitting
              ? 'Подача заявки...'
              : 'Подать заявку'
          }}
        </button>
      </form>

      <p v-else-if="!registrationOpen"
         class="coach-applications__notice">
        Приём заявок на этот турнир сейчас закрыт.
      </p>

      <p v-else-if="teams.length === 0"
         class="coach-applications__notice">
        У вас пока нет команд.
        Создайте команду в кабинете тренера.
      </p>

      <p v-else-if="availableTeams.length === 0"
         class="coach-applications__notice">
        Для всех ваших команд заявки уже созданы.
      </p>

      <RouterLink v-if="teams.length === 0"
                  :to="{ name: 'coach-teams' }"
                  class="coach-applications__link">
        Перейти к моим командам
      </RouterLink>

      <!-- Поданные заявки -->
      <div v-if="applications.length > 0"
           class="coach-applications__list">
        <h3>Мои заявки</h3>

        <article v-for="application in applications"
                 :key="application.id"
                 class="coach-applications__item">
          <div class="coach-applications__item-heading">
            <strong>{{ application.teamName }}</strong>
            <span class="coach-applications__status"
                  :class="`coach-applications__status--${application.status.toLowerCase()}`">
              {{
                application.isAccredited
                  ? 'Аккредитована'
                  : statusLabel(application.status)
              }}
            </span>
          </div>

          <p>
            Подана: {{ formatDate(application.submittedAt) }}
          </p>

          <p>
            Заявлено участников:
            {{ application.participants.length }}
          </p>

          <p v-if="application.comment"
             class="coach-applications__comment">
            Комментарий организатора:
            {{ application.comment }}
          </p>

          <details class="coach-applications__participants">
            <summary>
              Посмотреть заявленный состав
            </summary>

            <ul>
              <li v-for="person in application.participants"
                  :key="person.id">
                {{ participantName(person) }}
              </li>
            </ul>
          </details>

          <button v-if="
              registrationOpen &&
              !application.isAccredited
            "
                  type="button"
                  class="btn btn-outline coach-applications__refresh"
                  :disabled="updatingApplicationId !== null"
                  @click="refreshParticipants(application)">
            {{
              updatingApplicationId === application.id
                ? 'Обновление...'
                : 'Обновить состав заявки'
            }}
          </button>

          <p v-if="application.isAccredited"
             class="coach-applications__notice">
            Состав зафиксирован после аккредитации.
          </p>
        </article>
      </div>
    </template>
  </div>
</template>
