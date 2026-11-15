
<script setup lang="ts">
  import { computed, ref, watch } from 'vue'

  import { getMyTeams } from '@/api/teams'

  import {
    getMyApplications,
    createTournamentApplication,
  } from '@/api/applications'

  import { isRegistrationOpen } from '@/utils/application'

  import CoachApplicationCard
    from '@/components/tournaments/CoachApplicationCard.vue'

  import type { Team } from '@/types/teams'
  import type { Tournament } from '@/types/tournament'
  import type { TournamentApplication } from '@/types/application'

  const props = defineProps<{
    tournament: Tournament
  }>()

  const teams = ref<Team[]>([])
  const applications = ref<TournamentApplication[]>([])

  const selectedTeamId = ref(0)

  const isLoading = ref(true)
  const isCreating = ref(false)

  const loadError = ref('')
  const actionError = ref('')
  const successMessage = ref('')
  const reloadCount = ref(0)

  const registrationOpen = computed(() =>
    isRegistrationOpen(
      props.tournament.status,
      props.tournament.registrationDeadline,
    ),
  )

  const availableTeams = computed(() =>
    teams.value.filter(team =>
      !applications.value.some(
        application => application.teamId === team.id,
      ),
    ),
  )

  const selectedTeam = computed(() =>
    availableTeams.value.find(
      team => team.id === selectedTeamId.value,
    ),
  )

  const canSaveDraft = computed(() =>
    registrationOpen.value &&
    selectedTeam.value !== undefined &&
    !isCreating.value,
  )

  const canSubmit = computed(() =>
    canSaveDraft.value &&
    (selectedTeam.value?.participantsCount ?? 0) > 0,
  )

  function selectAvailableTeam() {
    if (!availableTeams.value.some(
      team => team.id === selectedTeamId.value,
    )) {
      selectedTeamId.value =
        availableTeams.value[0]?.id ?? 0
    }
  }

  async function loadData(
    tournamentId: number,
    signal: AbortSignal,
  ) {
    isLoading.value = true
    loadError.value = ''
    actionError.value = ''
    successMessage.value = ''

    try {
      const [myTeams, items] = await Promise.all([
        getMyTeams(signal),
        getMyApplications(tournamentId, signal),
      ])

      if (signal.aborted) return

      teams.value = myTeams
      applications.value = items.map(item => item.application)

      selectAvailableTeam()
    } catch (err) {
      if (!signal.aborted) {
        loadError.value = err instanceof Error
          ? err.message
          : 'Не удалось загрузить заявки.'
      }
    } finally {
      if (!signal.aborted) {
        isLoading.value = false
      }
    }
  }

  async function createApplication(saveAsDraft: boolean) {
    if (saveAsDraft ? !canSaveDraft.value : !canSubmit.value) {
      return
    }

    if (!saveAsDraft &&
      !window.confirm('Подать заявку на участие в турнире?')) {
      return
    }

    isCreating.value = true
    actionError.value = ''
    successMessage.value = ''

    const tournamentId = props.tournament.id

    try {
      const created = await createTournamentApplication(
        tournamentId,
        {
          teamId: selectedTeamId.value,
          saveAsDraft,
        },
      )

      applications.value.unshift(created)
      selectAvailableTeam()

      successMessage.value = saveAsDraft
        ? 'Черновик заявки сохранён.'
        : 'Заявка успешно отправлена.'
    } catch (err) {
      actionError.value = err instanceof Error
        ? err.message
        : 'Не удалось создать заявку.'
    } finally {
      isCreating.value = false
    }
  }

  function replaceApplication(updated: TournamentApplication) {
    applications.value = applications.value.map(
      item => item.id === updated.id ? updated : item,
    )
  }

  function removeApplication(applicationId: number) {
    applications.value = applications.value.filter(
      item => item.id !== applicationId,
    )

    successMessage.value = 'Черновик удалён.'

    selectAvailableTeam()
  }

  watch(
    [
      () => props.tournament.id,
      reloadCount,
    ],
    ([tournamentId], _oldValues, onCleanup) => {
      const controller = new AbortController()

      onCleanup(() => controller.abort())

      void loadData(tournamentId, controller.signal)
    },
    { immediate: true },
  )
</script>

<template>
  <div class="coach-applications">
    <p v-if="isLoading"
       class="coach-applications__message">
      Загрузка заявок...
    </p>

    <div v-else-if="loadError"
         class="coach-applications__message">
      <p role="alert">{{ loadError }}</p>

      <button type="button"
              class="btn btn-outline"
              @click="reloadCount++">
        Повторить
      </button>
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

      <form v-if="registrationOpen && availableTeams.length > 0"
            class="coach-applications__form"
            @submit.prevent="createApplication(false)">
        <label for="application-team">
          Выберите команду
        </label>

        <select id="application-team"
                v-model.number="selectedTeamId"
                :disabled="isCreating">
          <option :value="0">
            Выберите команду
          </option>

          <option v-for="team in availableTeams"
                  :key="team.id"
                  :value="team.id">
            {{ team.name }} — {{ team.participantsCount }} участников
          </option>
        </select>

        <p v-if="selectedTeam?.participantsCount === 0"
           class="coach-applications__notice">
          В команде нет участников. Сейчас можно
          сохранить только черновик.
        </p>

        <button type="submit"
                class="btn btn-primary"
                :disabled="!canSubmit">
          Подать заявку
        </button>

        <button type="button"
                class="btn btn-outline"
                :disabled="!canSaveDraft"
                @click="createApplication(true)">
          Сохранить черновик
        </button>
      </form>

      <p v-else-if="!registrationOpen"
         class="coach-applications__notice">
        Приём заявок закрыт.
      </p>

      <p v-else-if="teams.length === 0"
         class="coach-applications__notice">
        Сначала создайте команду.
      </p>

      <p v-else
         class="coach-applications__notice">
        Для всех ваших команд уже созданы заявки.
      </p>

      <RouterLink v-if="teams.length === 0"
                  :to="{ name: 'coach-teams' }"
                  class="coach-applications__link">
        Мои команды
      </RouterLink>

      <div v-if="applications.length > 0"
           class="coach-applications__list">
        <h3>Мои заявки на турнир</h3>

        <CoachApplicationCard v-for="application in applications"
                              :key="application.id"
                              :application="application"
                              :registration-open="registrationOpen"
                              @updated="replaceApplication"
                              @removed="removeApplication" />
      </div>

      <RouterLink :to="{ name: 'coach-applications' }"
                  class="coach-applications__all-link">
        Все мои заявки →
      </RouterLink>
    </template>
  </div>
</template>
