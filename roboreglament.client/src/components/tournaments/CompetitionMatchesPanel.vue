
<script setup lang="ts">
  import { computed, ref, watch } from 'vue'
  import CompetitionProtocolReview
    from '@/components/tournaments/CompetitionProtocolReview.vue'
  import {
    getMatches,
    getEligibleMatchTeams,
    createMatch,
    updateMatch,
    deleteMatch,
    drawMatches,
    assignMatchJudge,
    removeMatchJudge,
    updateMatchStatus,
  } from '@/api/matches'

  import {
    updateDisciplineRules,
  } from '@/api/tournaments'

  import {
    getProtocolTemplates,
  } from '@/api/protocols'

  import type {
    Tournament,
    Discipline,
    CompetitionFormat,
    ScoringMode,
    JudgeAggregationMethod,
    RankingMethod
  } from '@/types/tournament'

  import type {
    CompetitionMatch,
    EligibleMatchTeam,
    MatchStatus,
    SaveMatchRequest,
  } from '@/types/match'

  import type {
    ProtocolTemplate,
  } from '@/types/protocol'

  const props = defineProps<{
    tournament: Tournament
  }>()

  const emit = defineEmits<{
    (event: 'rules-saved', value: Discipline): void
  }>()

  const selectedDisciplineId = ref(
    props.tournament.disciplines[0]?.id ?? 0,
  )

  const discipline = computed(() =>
    props.tournament.disciplines.find(
      item => item.id === selectedDisciplineId.value,
    ),
  )

  const canPrepare = computed(() =>
    props.tournament.status === 'Registration' ||
    props.tournament.status === 'InProgress',
  )

  const canEditRules = computed(() =>
    props.tournament.status === 'Draft',
  )

  const matches = ref<CompetitionMatch[]>([])
  const eligibleTeams = ref<EligibleMatchTeam[]>([])
  const templates = ref<ProtocolTemplate[]>([])

  const loading = ref(false)
  const busy = ref(false)
  const error = ref('')
  const success = ref('')
  const reloadVersion = ref(0)
  const reviewMatchId = ref<number | null>(null)
  // Настройки дисциплины.

  const rulesFormat = ref<CompetitionFormat>('Run')
  const rulesScoring = ref<ScoringMode>('PerTeam')
  const rulesSides = ref(1)

  const rulesAggregation =
    ref<JudgeAggregationMethod>('Average')

  const rulesRanking =
    ref<RankingMethod>('BestResult')

  const rulesWinPoints = ref(3)
  const rulesDrawPoints = ref(1)
  const rulesLossPoints = ref(0)

  const rulesMin = ref(1)
  const rulesMax = ref(1)


  const rulesValid = computed(() => {
    const basicValid =
      rulesSides.value >=
      (rulesFormat.value === 'Run' ? 1 : 2) &&
      rulesSides.value <=
      (rulesFormat.value === 'Run' ? 1 : 4) &&
      rulesMin.value >=
      (rulesScoring.value === 'PerAlliance' ? 2 : 1) &&
      rulesMin.value <= rulesMax.value &&
      rulesMax.value <= 4 &&
      (rulesScoring.value !== 'PerTeam' ||
        rulesMax.value === 1)

    const rankingValid =
      rulesFormat.value === 'Run'
        ? rulesRanking.value !== 'MatchPoints'
        : rulesRanking.value === 'MatchPoints'

    const points = [
      rulesWinPoints.value,
      rulesDrawPoints.value,
      rulesLossPoints.value,
    ]

    const pointsValid =
      points.every(value =>
        Number.isInteger(value) &&
        value >= 0 &&
        value <= 100
      ) &&
      rulesWinPoints.value > rulesDrawPoints.value &&
      rulesDrawPoints.value >= rulesLossPoints.value

    return basicValid && rankingValid && pointsValid
  })


  function onFormatChange() {
    rulesSides.value = rulesFormat.value === 'Run' ? 1 : 2
    rulesRanking.value = rulesFormat.value === 'Match'
      ? 'MatchPoints'
      : 'BestResult'
  }

  function onScoringChange() {
    rulesMin.value = rulesScoring.value === 'PerTeam' ? 1 : 2
    rulesMax.value = rulesMin.value
  }

  async function saveRules() {
    const current = discipline.value
    if (!current || !canEditRules.value || !rulesValid.value) return

    await execute(async () => {
      const saved = await updateDisciplineRules(
        props.tournament.id,
        current.id,
        {
          format: rulesFormat.value,
          scoringMode: rulesScoring.value,
          sidesPerEvent: rulesSides.value,
          minTeamsPerSide: rulesMin.value,
          maxTeamsPerSide: rulesMax.value,
          judgeAggregationMethod: rulesAggregation.value,
          rankingMethod: rulesRanking.value,
          winPoints: rulesWinPoints.value,
          drawPoints: rulesDrawPoints.value,
          lossPoints: rulesLossPoints.value,

        },
      )

      emit('rules-saved', saved)
      success.value = 'Правила дисциплины сохранены.'
    })
  }

  // Данные выступлений.

  const activeTemplates = computed(() =>
    templates.value.filter(
      item => item.isActive && item.criteria.length > 0,
    ),
  )

  const drawTeamsPerSide = ref(1)
  const drawPrefix = ref('Отборочный')

  const teamsPerEvent = computed(() =>
    (discipline.value?.sidesPerEvent ?? 0) *
    drawTeamsPerSide.value,
  )

  const canDraw = computed(() =>
    canPrepare.value &&
    !busy.value &&
    !loading.value &&
    matches.value.length === 0 &&
    activeTemplates.value.length > 0 &&
    eligibleTeams.value.length > 0 &&
    teamsPerEvent.value > 0 &&
    eligibleTeams.value.length % teamsPerEvent.value === 0,
  )

  async function load(
    tournamentId: number,
    disciplineId: number,
    signal: AbortSignal,
  ) {
    loading.value = true
    error.value = ''

    try {
      const [loadedMatches, loadedTeams, loadedTemplates] =
        await Promise.all([
          getMatches(tournamentId, disciplineId, signal),
          getEligibleMatchTeams(tournamentId, disciplineId, signal),
          getProtocolTemplates(tournamentId, disciplineId, signal),
        ])

      if (signal.aborted) return

      matches.value = loadedMatches
      eligibleTeams.value = loadedTeams
      templates.value = loadedTemplates
    } catch (err) {
      if (!signal.aborted) {
        error.value = err instanceof Error
          ? err.message
          : 'Не удалось загрузить выступления.'
      }
    } finally {
      if (!signal.aborted) loading.value = false
    }
  }

  function reload() {
    reloadVersion.value++
  }

  async function execute(action: () => Promise<void>) {
    if (busy.value) return

    busy.value = true
    error.value = ''
    success.value = ''

    try {
      await action()
    } catch (err) {
      error.value = err instanceof Error
        ? err.message
        : 'Не удалось выполнить операцию.'
    } finally {
      busy.value = false
    }
  }

  function changeDiscipline(event: Event) {
    const select = event.target as HTMLSelectElement

    if (formOpen.value &&
      !window.confirm('Закрыть форму без сохранения?')) {
      select.value = String(selectedDisciplineId.value)
      return
    }

    selectedDisciplineId.value = Number(select.value)
    closeForm()
    error.value = ''
    success.value = ''
  }

  watch(
    discipline,
    current => {
      if (!current) return

      rulesFormat.value = current.format
      rulesScoring.value = current.scoringMode
      rulesSides.value = current.sidesPerEvent
      rulesMin.value = current.minTeamsPerSide
      rulesMax.value = current.maxTeamsPerSide

      drawTeamsPerSide.value = current.minTeamsPerSide

      rulesAggregation.value = current.judgeAggregationMethod
      rulesRanking.value = current.rankingMethod

      rulesWinPoints.value = current.winPoints
      rulesDrawPoints.value = current.drawPoints
      rulesLossPoints.value = current.lossPoints

    },
    { immediate: true },
  )

  watch(
    [
      () => props.tournament.id,
      selectedDisciplineId,
      reloadVersion,
    ],
    ([tournamentId, disciplineId], _old, onCleanup) => {
      const controller = new AbortController()
      onCleanup(() => controller.abort())

      if (!disciplineId) {
        matches.value = []
        eligibleTeams.value = []
        templates.value = []
        loading.value = false
        return
      }

      void load(tournamentId, disciplineId, controller.signal)
    },
    { immediate: true },
  )

  // Редактор выступления.

  interface FormSide {
    name: string
    teamIds: number[]
  }

  const formOpen = ref(false)
  const editingId = ref<number | null>(null)

  const formName = ref('')
  const formDate = ref('')
  const formTemplateId = ref(0)
  const formSides = ref<FormSide[]>([])

  const availableFormTemplates = computed(() =>
    templates.value.filter(
      item =>
        (item.isActive || item.id === formTemplateId.value) &&
        item.criteria.length > 0,
    ),
  )

  function makeSide(): FormSide {
    return {
      name: '',
      teamIds: Array.from(
        { length: discipline.value?.minTeamsPerSide ?? 1 },
        () => 0,
      ),
    }
  }

  function startCreate() {
    if (!canPrepare.value || busy.value) return

    editingId.value = null
    formName.value = ''
    formDate.value = ''
    formTemplateId.value = 0

    formSides.value = Array.from(
      { length: discipline.value?.sidesPerEvent ?? 1 },
      () => makeSide(),
    )

    formOpen.value = true
    error.value = ''
    success.value = ''
  }

  function toLocalDateTime(value: string | null): string {
    if (!value) return ''

    const date = new Date(value)
    if (!Number.isFinite(date.getTime())) return ''

    const local = new Date(
      date.getTime() - date.getTimezoneOffset() * 60_000,
    )

    return local.toISOString().slice(0, 16)
  }

  function startEdit(match: CompetitionMatch) {
    if (match.status !== 'Scheduled' ||
      !canPrepare.value || busy.value) return

    editingId.value = match.id
    formName.value = match.name ?? ''
    formDate.value = toLocalDateTime(match.scheduledAt)
    formTemplateId.value = match.protocolTemplateId ?? 0

    formSides.value = match.sides.map(side => ({
      name: side.name ?? '',
      teamIds: side.teams.map(team => team.teamId),
    }))

    formOpen.value = true
    error.value = ''
    success.value = ''
  }

  function closeForm() {
    formOpen.value = false
    editingId.value = null
    formSides.value = []
  }

  function addTeamSlot(side: FormSide) {
    if (side.teamIds.length <
      (discipline.value?.maxTeamsPerSide ?? 1)) {
      side.teamIds.push(0)
    }
  }

  function removeTeamSlot(side: FormSide, index: number) {
    if (side.teamIds.length >
      (discipline.value?.minTeamsPerSide ?? 1)) {
      side.teamIds.splice(index, 1)
    }
  }

  function teamUsedElsewhere(
    teamId: number,
    sideIndex: number,
    teamIndex: number,
  ): boolean {
    return formSides.value.some((side, s) =>
      side.teamIds.some((id, t) =>
        id === teamId &&
        (s !== sideIndex || t !== teamIndex),
      ),
    )
  }

  const formValid = computed(() => {
    const current = discipline.value
    if (!current) return false

    if (formSides.value.length !== current.sidesPerEvent)
      return false

    const ids = formSides.value.flatMap(side => side.teamIds)

    if (!ids.length || ids.some(id => id <= 0))
      return false

    if (new Set(ids).size !== ids.length)
      return false

    return formSides.value.every(side =>
      side.teamIds.length >= current.minTeamsPerSide &&
      side.teamIds.length <= current.maxTeamsPerSide,
    )
  })

  async function saveMatch() {
    const current = discipline.value

    if (!current || !canPrepare.value || !formValid.value)
      return

    let scheduledAt: string | null = null

    if (formDate.value) {
      const date = new Date(formDate.value)

      if (!Number.isFinite(date.getTime())) {
        error.value = 'Некорректная дата выступления.'
        return
      }

      scheduledAt = date.toISOString()
    }

    const request: SaveMatchRequest = {
      name: formName.value.trim() || null,
      scheduledAt,
      protocolTemplateId: formTemplateId.value || null,
      sides: formSides.value.map(side => ({
        name: side.name.trim() || null,
        teamIds: [...side.teamIds],
      })),
    }

    await execute(async () => {
      if (editingId.value === null) {
        await createMatch(
          props.tournament.id,
          current.id,
          request,
        )
      } else {
        await updateMatch(
          props.tournament.id,
          current.id,
          editingId.value,
          request,
        )
      }

      closeForm()
      success.value = 'Выступление сохранено.'
      reload()
    })
  }

  async function removeMatch(match: CompetitionMatch) {
    const current = discipline.value
    if (!current || !canPrepare.value) return

    if (!window.confirm(
      `Удалить ${match.name ?? `выступление №${match.number}`}?`,
    )) return

    await execute(async () => {
      await deleteMatch(props.tournament.id, current.id, match.id)

      success.value = 'Выступление удалено.'
      reload()
    })
  }

  async function runDraw() {
    const current = discipline.value
    if (!current || !canDraw.value) return

    if (!window.confirm(
      `Выполнить жеребьёвку для ${eligibleTeams.value.length} команд?`,
    )) return

    await execute(async () => {
      const created = await drawMatches(
        props.tournament.id,
        current.id,
        {
          teamsPerSide: drawTeamsPerSide.value,
          protocolTemplateId: null,
          namePrefix: drawPrefix.value.trim() || null,
        },
      )

      success.value = `Сформировано выступлений: ${created.length}.`
      reload()
    })
  }

  // Судьи и статусы.

  const judgeInputs = ref<Record<number, string>>({})

  async function addJudge(match: CompetitionMatch) {
    const current = discipline.value
    const raw = (judgeInputs.value[match.id] ?? '').trim()

    if (!current || !/^[1-9]\d*$/.test(raw)) {
      error.value = 'Введите корректный публичный ID судьи.'
      return
    }

    const publicId = Number(raw)

    if (!Number.isSafeInteger(publicId)) {
      error.value = 'Публичный ID слишком большой.'
      return
    }

    await execute(async () => {
      await assignMatchJudge(
        props.tournament.id,
        current.id,
        match.id,
        publicId,
      )

      judgeInputs.value[match.id] = ''
      success.value = 'Судья назначен.'
      reload()
    })
  }

  async function removeJudge(
    match: CompetitionMatch,
    publicId: number,
  ) {
    const current = discipline.value
    if (!current) return

    if (!window.confirm('Снять судью с выступления?'))
      return

    await execute(async () => {
      await removeMatchJudge(
        props.tournament.id,
        current.id,
        match.id,
        publicId,
      )

      success.value = 'Судья снят.'
      reload()
    })
  }

  async function changeStatus(
    match: CompetitionMatch,
    status: MatchStatus,
  ) {
    const current = discipline.value
    if (!current) return

    if (!window.confirm('Подтвердить изменение статуса?'))
      return

    await execute(async () => {
      await updateMatchStatus(
        props.tournament.id,
        current.id,
        match.id,
        status,
      )

      success.value = 'Статус выступления изменён.'
      reload()
    })
  }

  function personName(person: {
    firstName: string
    lastName: string
    middleName: string | null
  }): string {
    return [
      person.lastName,
      person.firstName,
      person.middleName,
    ].filter(Boolean).join(' ')
  }

  function statusText(status: MatchStatus): string {
    switch (status) {
      case 'Scheduled': return 'Запланирован'
      case 'InProgress': return 'Проводится'
      case 'Completed': return 'Завершён'
      case 'Cancelled': return 'Отменён'
    }
  }

  function formatDate(value: string | null): string {
    return value
      ? new Date(value).toLocaleString('ru-RU')
      : 'Время не назначено'
  }
</script>


<template>
  <section class="organizer-panel competition-panel">
    <div class="organizer-heading">
      <div>
        <h2>Управление выступлениями</h2>
        <p>Заезды, матчи, союзы и назначение судей.</p>
      </div>
    </div>

    <p v-if="!tournament.disciplines.length">
      Сначала создайте дисциплину.
    </p>

    <template v-else>
      <div class="competition-toolbar">
        <label>
          Дисциплина
          <select :value="selectedDisciplineId"
                  :disabled="busy"
                  @change="changeDiscipline">
            <option v-for="item in tournament.disciplines"
                    :key="item.id"
                    :value="item.id">
              {{ item.name }}
            </option>
          </select>
        </label>

        <button type="button"
                class="btn btn-outline"
                :disabled="busy || loading"
                @click="reload">
          Обновить
        </button>
      </div>

      <p v-if="error" class="organizer-error" role="alert">
        {{ error }}
      </p>

      <p v-if="success" class="organizer-success" role="status">
        {{ success }}
      </p>

      <template v-if="discipline">
        <!-- Правила дисциплины -->

        <section class="competition-box">
          <h3>Правила проведения</h3>

          <div v-if="canEditRules" class="competition-form">
            <div class="competition-grid">
              <label>
                Формат выступления
                <select v-model="rulesFormat"
                        :disabled="busy"
                        @change="onFormatChange">
                  <option value="Run">Заезд</option>
                  <option value="Match">Матч</option>
                </select>
              </label>

              <label>
                Тип зачёта
                <select v-model="rulesScoring"
                        :disabled="busy"
                        @change="onScoringChange">
                  <option value="PerTeam">
                    По отдельным командам
                  </option>
                  <option value="PerAlliance">
                    По союзам
                  </option>
                </select>
              </label>

              <label>
                Количество сторон
                <input v-model.number="rulesSides"
                       type="number"
                       :min="rulesFormat === 'Run' ? 1 : 2"
                       :max="rulesFormat === 'Run' ? 1 : 4"
                       :disabled="busy || rulesFormat === 'Run'" />
              </label>

              <label>
                Минимум команд в стороне
                <input v-model.number="rulesMin"
                       type="number"
                       :min="rulesScoring === 'PerAlliance' ? 2 : 1"
                       :max="rulesScoring === 'PerAlliance' ? 4 : 1"
                       :disabled="busy || rulesScoring === 'PerTeam'" />
              </label>

              <label>
                Максимум команд в стороне
                <input v-model.number="rulesMax"
                       type="number"
                       :min="rulesMin"
                       :max="rulesScoring === 'PerAlliance' ? 4 : 1"
                       :disabled="busy || rulesScoring === 'PerTeam'" />
              </label>

              <label>
                Подсчёт судейских баллов

                <select v-model="rulesAggregation" :disabled="busy">
                  <option value="Average">
                    Среднее арифметическое
                  </option>
                  <option value="Sum">
                    Сумма баллов судей
                  </option>
                  <option value="AgreedActual">
                    Фактически набранные баллы
                  </option>
                </select>
              </label>

              <label>
                Формирование рейтинга

                <select v-model="rulesRanking" :disabled="busy">
                  <template v-if="rulesFormat === 'Run'">
                    <option value="BestResult">
                      Лучший результат
                    </option>
                    <option value="SumResults">
                      Сумма результатов
                    </option>
                    <option value="AverageResults">
                      Среднее результатов
                    </option>
                  </template>

                  <option v-else value="MatchPoints">
                    Турнирные очки за матчи
                  </option>
                </select>
              </label>

              <template v-if="rulesFormat === 'Match'">
                <label>
                  Очки за победу
                  <input v-model.number="rulesWinPoints"
                         type="number"
                         min="0"
                         max="100"
                         step="1"
                         :disabled="busy" />
                </label>

                <label>
                  Очки за ничью
                  <input v-model.number="rulesDrawPoints"
                         type="number"
                         min="0"
                         max="100"
                         step="1"
                         :disabled="busy" />
                </label>

                <label>
                  Очки за поражение
                  <input v-model.number="rulesLossPoints"
                         type="number"
                         min="0"
                         max="100"
                         step="1"
                         :disabled="busy" />
                </label>
              </template>

            </div>

            <div class="organizer-actions">
              <button type="button"
                      class="btn btn-primary"
                      :disabled="busy || !rulesValid"
                      @click="saveRules">
                Сохранить правила
              </button>
            </div>
          </div>

          <div v-else class="competition-summary">
            <p>
              <strong>Формат:</strong>
              {{
                discipline.format === 'Run'
                  ? 'Заезд'
                  : 'Матч'
              }}
            </p>

            <p>
              <strong>Зачёт:</strong>
              {{
                discipline.scoringMode === 'PerAlliance'
                  ? 'По союзам'
                  : 'По отдельным командам'
              }}
            </p>

            <p>
              <strong>Сторон:</strong>
              {{ discipline.sidesPerEvent }}
            </p>

            <p>
              <strong>Команд в стороне:</strong>
              {{ discipline.minTeamsPerSide }}–
              {{ discipline.maxTeamsPerSide }}
            </p>

            
            <p>
              <strong>Подсчёт баллов:</strong>
              {{
                discipline.judgeAggregationMethod === 'Average'
                  ? 'Среднее арифметическое'
                  : discipline.judgeAggregationMethod === 'Sum'
                    ? 'Сумма баллов судей'
                    : 'Фактически набранные баллы'
              }}
            </p>

            <p>
              <strong>Рейтинг:</strong>
              {{
                discipline.rankingMethod === 'BestResult'
                  ? 'Лучший результат'
                  : discipline.rankingMethod === 'SumResults'
                    ? 'Сумма результатов'
                    : discipline.rankingMethod === 'AverageResults'
                      ? 'Среднее результатов'
                      : 'Турнирные очки'
              }}
            </p>

            <p v-if="discipline.format === 'Match'">
              <strong>Очки (победа / ничья / поражение):</strong>
              {{ discipline.winPoints }} /
              {{ discipline.drawPoints }} /
              {{ discipline.lossPoints }}
            </p>

          </div>
        </section>

        <p v-if="tournament.status === 'Draft'">
          Сначала сохраните правила дисциплины и откройте
          регистрацию. После этого станет доступно создание
          выступлений.
        </p>

        <!-- Автоматическая жеребьёвка -->

        <section v-if="canPrepare" class="competition-box">
          <h3>Автоматическая жеребьёвка</h3>

          <p>
            Аккредитованных команд:
            <strong>{{ eligibleTeams.length }}</strong>
          </p>

          <div class="competition-grid">
            <label>
              Команд в одной стороне
              <input v-model.number="drawTeamsPerSide"
                     type="number"
                     :min="discipline.minTeamsPerSide"
                     :max="discipline.maxTeamsPerSide"
                     :disabled="busy || !!matches.length" />
            </label>

            <label>
              Название выступлений
              <input v-model="drawPrefix"
                     maxlength="100"
                     placeholder="Отборочный"
                     :disabled="busy || !!matches.length" />
            </label>
          </div>

          <p>
            Команд на выступление: {{ teamsPerEvent }}.
            Каждая команда участвует в жеребьёвке один раз.
          </p>

          <p v-if="matches.length">
            Повторная жеребьёвка недоступна:
            в дисциплине уже есть выступления.
          </p>

          <p v-else-if="
             eligibleTeams.length &&
             teamsPerEvent>
            0 &&
            eligibleTeams.length % teamsPerEvent !== 0
            ">
            Количество команд не делится на размер выступления
            без остатка. Измените размер или состав участников.
          </p>

          <div class="organizer-actions">
            <button type="button"
                    class="btn btn-primary"
                    :disabled="!canDraw"
                    @click="runDraw">
              Провести жеребьёвку
            </button>
          </div>
        </section>

        <!-- Ручное создание и редактирование -->

        <div class="competition-section-heading">
          <h3>Список выступлений</h3>

          <button v-if="canPrepare && !formOpen"
                  type="button"
                  class="btn btn-primary"
                  :disabled="
                  busy ||
                  loading ||
                  !activeTemplates.length ||
                  eligibleTeams.length <
                discipline.sidesPerEvent *
                discipline.minTeamsPerSide
            "
            @click="startCreate"
          >
            + Создать выступление
          </button>
        </div>

        <form v-if="formOpen && canPrepare"
              class="competition-form competition-editor"
              @submit.prevent="saveMatch">
          <h3>
            {{
              editingId === null
                ? 'Новое выступление'
                : 'Редактирование выступления'
            }}
          </h3>

          <div class="competition-grid">
            <label>
              Название
              <input v-model="formName"
                     maxlength="200"
                     placeholder="Например, полуфинал №1"
                     :disabled="busy" />
            </label>

            <label>
              Дата и время
              <input v-model="formDate"
                     type="datetime-local"
                     :disabled="busy" />
            </label>

            <label>
              Судейский протокол
              <select v-model.number="formTemplateId"
                      :disabled="busy">
                <option :value="0">
                  Активный шаблон дисциплины
                </option>
                <option v-for="item in availableFormTemplates"
                        :key="item.id"
                        :value="item.id">
                  {{ item.name }}
                </option>
              </select>
            </label>
          </div>

          <div v-for="(side, sideIndex) in formSides"
               :key="sideIndex"
               class="competition-side-editor">
            <h4>
              {{
                discipline.scoringMode === 'PerAlliance'
                  ? `Союз №${sideIndex + 1}`
                  : `Сторона №${sideIndex + 1}`
              }}
            </h4>

            <label>
              Название стороны
              <input v-model="side.name"
                     maxlength="200"
                     :placeholder="
                  discipline.scoringMode === 'PerAlliance'
                    ? 'Например, красный союз'
                    : 'Необязательно'
                "
                     :disabled="busy" />
            </label>

            <div v-for="(_teamId, teamIndex) in side.teamIds"
                 :key="teamIndex"
                 class="competition-team-row">
              <label>
                Команда {{ teamIndex + 1 }}
                <select v-model.number="side.teamIds[teamIndex]"
                        required
                        :disabled="busy">
                  <option :value="0" disabled>
                    Выберите команду
                  </option>

                  <option v-for="team in eligibleTeams"
                          :key="team.id"
                          :value="team.id"
                          :disabled="
                      teamUsedElsewhere(
                        team.id, sideIndex, teamIndex
                      )
                    ">
                    {{ team.name }}
                  </option>
                </select>
              </label>

              <button v-if="
                      side.teamIds.length>
                discipline.minTeamsPerSide
                "
                type="button"
                class="organizer-danger"
                :disabled="busy"
                @click="removeTeamSlot(side, teamIndex)"
                >
                Убрать
              </button>
            </div>

            <button v-if="
                    side.teamIds.length <
                discipline.maxTeamsPerSide
              "
              type="button"
              class="btn btn-outline"
              :disabled="busy"
              @click="addTeamSlot(side)"
            >
              + Добавить команду
            </button>
          </div>

          <div class="organizer-actions">
            <button type="submit"
                    class="btn btn-primary"
                    :disabled="busy || !formValid">
              {{ busy ? 'Сохранение...' : 'Сохранить' }}
            </button>

            <button type="button"
                    class="btn btn-outline"
                    :disabled="busy"
                    @click="closeForm">
              Отмена
            </button>
          </div>
        </form>

        <!-- Список созданных выступлений -->

        <p v-if="loading">Загрузка выступлений...</p>

        <p v-else-if="!matches.length">
          Для выбранной дисциплины пока нет выступлений.
        </p>

        <div v-else class="competition-match-list">
          <article v-for="match in matches"
                   :key="match.id"
                   class="competition-match-card">
            <div class="competition-match-heading">
              <div>
                <h4>
                  {{
                    match.name ||
                    (match.format === 'Run'
                      ? `Заезд №${match.number}`
                      : `Матч №${match.number}`)
                  }}
                </h4>

                <p>{{ formatDate(match.scheduledAt) }}</p>

                <p>
                  Протокол:
                  {{ match.protocolTemplateName || 'Не назначен' }}
                </p>
              </div>

              <span class="competition-status">
                {{ statusText(match.status) }}
              </span>
            </div>

            <div class="competition-match-sides">
              <div v-for="side in match.sides"
                   :key="side.id"
                   class="competition-match-side">
                <strong>
                  {{
                    side.name ||
                    (match.scoringMode === 'PerAlliance'
                      ? `Союз №${side.number}`
                      : `Сторона №${side.number}`)
                  }}
                </strong>

                <ul>
                  <li v-for="team in side.teams"
                      :key="team.id">
                    {{ team.teamName }}
                  </li>
                </ul>
              </div>
            </div>

            <!-- Назначенные судьи -->

            <div class="competition-judges">
              <h5>Судьи</h5>

              <p v-if="!match.judges.length">
                Судьи пока не назначены.
              </p>

              <div v-for="judge in match.judges"
                   :key="judge.id"
                   class="competition-judge">
                <span>
                  {{ personName(judge) }}
                  · ID {{ judge.publicId }}
                </span>

                <button v-if="
                    canPrepare &&
                    match.status === 'Scheduled'
                  "
                        type="button"
                        class="organizer-danger"
                        :disabled="busy"
                        @click="
                    removeJudge(match, judge.publicId)
                  ">
                  Снять
                </button>
              </div>

              <div v-if="
                  canPrepare &&
                  match.status === 'Scheduled'
                "
                   class="competition-judge-add">
                <input v-model="judgeInputs[match.id]"
                       type="text"
                       inputmode="numeric"
                       placeholder="Публичный ID судьи"
                       :disabled="busy" />

                <button type="button"
                        class="btn btn-outline"
                        :disabled="busy"
                        @click="addJudge(match)">
                  Назначить судью
                </button>
              </div>
            </div>

            <!-- Управление выступлением -->

            <div v-if="
                canPrepare &&
                match.status === 'Scheduled'
              "
                 class="organizer-actions">
              <button type="button"
                      class="btn btn-outline"
                      :disabled="busy"
                      @click="startEdit(match)">
                Изменить состав
              </button>

              <button v-if="tournament.status === 'InProgress'"
                      type="button"
                      class="btn btn-primary"
                      :disabled="busy || !match.judges.length"
                      @click="changeStatus(match, 'InProgress')">
                Начать
              </button>

              <button type="button"
                      class="btn btn-outline"
                      :disabled="busy"
                      @click="changeStatus(match, 'Cancelled')">
                Отменить
              </button>

              <button type="button"
                      class="organizer-danger"
                      :disabled="busy"
                      @click="removeMatch(match)">
                Удалить
              </button>
            </div>

            <div v-if="match.status === 'InProgress'"
                 class="organizer-actions">
              <p>
                Ожидаются судейские протоколы.
              </p>

              <button type="button"
                      class="btn btn-primary"
                      :disabled="busy"
                      @click="changeStatus(match, 'Completed')">
                Завершить выступление
              </button>
            </div>
            
            <div
              v-if="
                match.status === 'InProgress' ||
                match.status === 'Completed'
              "
              class="competition-review-entry"
            >
              <button
                type="button"
                class="btn btn-outline"
                @click="
                  reviewMatchId =
                    reviewMatchId === match.id ? null : match.id
                "
              >
                {{
                  reviewMatchId === match.id
                    ? 'Скрыть протоколы'
                    : 'Проверить протоколы судей'
                }}
              </button>

              <CompetitionProtocolReview
                v-if="reviewMatchId === match.id"
                :key="match.id"
                :tournament-id="tournament.id"
                :discipline-id="discipline.id"
                :match-id="match.id"
                :editable="
                  tournament.status === 'InProgress' &&
                  match.status === 'InProgress'
                "
              />
            </div>

          </article>
        </div>
      </template>
    </template>
  </section>
</template>
