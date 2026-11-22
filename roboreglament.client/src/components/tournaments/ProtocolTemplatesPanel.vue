
<script setup lang="ts">import { computed, ref, watch } from 'vue'

import {
  getProtocolTemplates,
  createProtocolTemplate,
  updateProtocolTemplate,
  deleteProtocolTemplate,
  previewProtocol,
} from '@/api/protocols'

import ProtocolTemplateEditor
  from '@/components/tournaments/ProtocolTemplateEditor.vue'

import type { Tournament } from '@/types/tournament'

import type {
  ProtocolTemplate,
  SaveProtocolTemplateRequest,
  PreviewProtocolResponse,
  PreviewScoreRequest,
} from '@/types/protocol'

const props = defineProps<{
  tournament: Tournament
}>()

const selectedDisciplineId = ref(
  props.tournament.disciplines[0]?.id ?? 0,
)

const currentDiscipline = computed(() =>
  props.tournament.disciplines.find(
    item => item.id === selectedDisciplineId.value,
  ),
)

const canManage = computed(() =>
  props.tournament.status !== 'Completed',
)

const templates = ref<ProtocolTemplate[]>([])

const isLoading = ref(false)
const isSaving = ref(false)
const deletingId = ref<number | null>(null)
const isPreviewing = ref(false)

const editorOpen = ref(false)
const editingTemplate = ref<ProtocolTemplate | null>(null)

const previewTemplate = ref<ProtocolTemplate | null>(null)
const scoreValues = ref<Record<number, number | string>>({})
const previewResult = ref<PreviewProtocolResponse | null>(null)

const error = ref('')
const success = ref('')
const previewError = ref('')

const reloadCount = ref(0)

const activeTemplate = computed(() =>
  templates.value.find(item => item.isActive),
)

const isBusy = computed(() =>
  isSaving.value ||
  deletingId.value !== null,
)

function sortedCriteria(template: ProtocolTemplate) {
  return [...template.criteria]
    .sort((a, b) => a.order - b.order)
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat('ru-RU', {
    maximumFractionDigits: 2,
  }).format(value)
}

function messageFromError(
  err: unknown,
  fallback: string,
): string {
  return err instanceof Error ? err.message : fallback
}

function resetModes() {
  editorOpen.value = false
  editingTemplate.value = null

  previewTemplate.value = null
  previewResult.value = null
  previewError.value = ''
  scoreValues.value = {}
}

function changeDiscipline(event: Event) {
  const select = event.target as HTMLSelectElement
  const nextId = Number(select.value)

  if (
    editorOpen.value &&
    !window.confirm(
      'Несохранённые изменения будут потеряны. Продолжить?',
    )
  ) {
    select.value = String(selectedDisciplineId.value)
    return
  }

  selectedDisciplineId.value = nextId
  resetModes()

  error.value = ''
  success.value = ''
}

async function load(
  tournamentId: number,
  disciplineId: number,
  signal: AbortSignal,
) {
  isLoading.value = true
  error.value = ''
  templates.value = []

  try {
    const result = await getProtocolTemplates(
      tournamentId,
      disciplineId,
      signal,
    )

    if (!signal.aborted) {
      templates.value = result
    }
  } catch (err) {
    if (!signal.aborted) {
      error.value = messageFromError(
        err,
        'Не удалось загрузить протоколы.',
      )
    }
  } finally {
    if (!signal.aborted) {
      isLoading.value = false
    }
  }
}

function reload() {
  reloadCount.value++
}

function startCreate() {
  if (!canManage.value || isBusy.value) return

  resetModes()
  editorOpen.value = true

  error.value = ''
  success.value = ''
}

function startEdit(template: ProtocolTemplate) {
  if (
    !canManage.value ||
    template.isLocked ||
    isBusy.value
  ) return

  resetModes()

  editingTemplate.value = template
  editorOpen.value = true

  error.value = ''
  success.value = ''
}

function closeEditor() {
  if (isSaving.value) return

  editorOpen.value = false
  editingTemplate.value = null
}

async function save(
  request: SaveProtocolTemplateRequest,
) {
  if (
    !canManage.value ||
    isSaving.value ||
    !selectedDisciplineId.value
  ) return

  const tournamentId = props.tournament.id
  const disciplineId = selectedDisciplineId.value
  const templateId = editingTemplate.value?.id

  isSaving.value = true
  error.value = ''
  success.value = ''

  try {
    if (templateId === undefined) {
      await createProtocolTemplate(
        tournamentId,
        disciplineId,
        request,
      )
    } else {
      await updateProtocolTemplate(
        tournamentId,
        disciplineId,
        templateId,
        request,
      )
    }

    editorOpen.value = false
    editingTemplate.value = null

    success.value = templateId === undefined
      ? 'Шаблон протокола создан.'
      : 'Изменения протокола сохранены.'

    reload()
  } catch (err) {
    error.value = messageFromError(
      err,
      'Не удалось сохранить протокол.',
    )
  } finally {
    isSaving.value = false
  }
}

async function remove(template: ProtocolTemplate) {
  if (
    !canManage.value ||
    template.isLocked ||
    isBusy.value
  ) return

  if (!window.confirm(
    `Удалить шаблон «${template.name}»? ` +
    'Это действие нельзя отменить.',
  )) return

  deletingId.value = template.id
  error.value = ''
  success.value = ''

  try {
    await deleteProtocolTemplate(
      props.tournament.id,
      selectedDisciplineId.value,
      template.id,
    )

    if (previewTemplate.value?.id === template.id) {
      resetModes()
    }

    success.value = 'Шаблон протокола удалён.'
    reload()
  } catch (err) {
    error.value = messageFromError(
      err,
      'Не удалось удалить протокол.',
    )
  } finally {
    deletingId.value = null
  }
}

function openPreview(template: ProtocolTemplate) {
  if (
    editorOpen.value &&
    !window.confirm(
      'Несохранённые изменения будут потеряны. Продолжить?',
    )
  ) return
  resetModes()

  previewTemplate.value = template

  scoreValues.value = Object.fromEntries(
    template.criteria.map(criterion => [
      criterion.id,
      criterion.minValue,
    ]),
  )

  error.value = ''
  success.value = ''
}

function closePreview() {
  if (isPreviewing.value) return

  previewTemplate.value = null
  previewResult.value = null
  previewError.value = ''
}

async function calculatePreview() {
  const template = previewTemplate.value

  if (!template || isPreviewing.value) return

  previewError.value = ''
  previewResult.value = null

  const scores: PreviewScoreRequest[] = []

  for (const criterion of template.criteria) {
    const rawValue = scoreValues.value[criterion.id]

    if (
      rawValue === undefined ||
      rawValue === '' ||
      typeof rawValue !== 'number' ||
      !Number.isFinite(rawValue)
    ) {
      previewError.value =
        `Укажите балл для критерия «${criterion.name}».`
      return
    }

    if (
      rawValue < criterion.minValue ||
      (
        criterion.maxValue !== null &&
        rawValue > criterion.maxValue
      ) ||
      Number(rawValue.toFixed(2)) !== rawValue
    ) {
      previewError.value =
        `Недопустимый балл: «${criterion.name}».`
      return
    }

    scores.push({
      criterionId: criterion.id,
      value: rawValue,
    })
  }

  isPreviewing.value = true

  try {
    previewResult.value = await previewProtocol(
      props.tournament.id,
      selectedDisciplineId.value,
      template.id,
      scores,
    )
  } catch (err) {
    previewError.value = messageFromError(
      err,
      'Не удалось рассчитать результат.',
    )
  } finally {
    isPreviewing.value = false
  }
}

watch(
  [
    () => props.tournament.id,
    selectedDisciplineId,
    reloadCount,
  ],
  ([tournamentId, disciplineId], _previous, onCleanup) => {
    if (!disciplineId) {
      templates.value = []
      isLoading.value = false
      return
    }

    const controller = new AbortController()

    onCleanup(() => controller.abort())

    void load(
      tournamentId,
      disciplineId,
      controller.signal,
    )
  },
  { immediate: true },
)</script>

<template>
  <section class="organizer-panel protocol-templates">
    <div class="organizer-heading">
      <div>
        <h2>Конструктор судейских протоколов</h2>

        <p>
          Настройка критериев и правил подсчёта
          результатов для каждой дисциплины.
        </p>
      </div>
    </div>

    <div v-if="tournament.disciplines.length === 0">
      <p>
        Сначала добавьте дисциплину во вкладке
        «Дисциплины».
      </p>
    </div>

    <template v-else>
      <div class="protocol-templates__toolbar">
        <label>
          Дисциплина

          <select :value="selectedDisciplineId"
                  :disabled="isBusy || isPreviewing"
                  @change="changeDiscipline">
            <option v-for="discipline in tournament.disciplines"
                    :key="discipline.id"
                    :value="discipline.id">
              {{ discipline.name }}
            </option>
          </select>
        </label>

        <button v-if="canManage && !editorOpen"
                type="button"
                class="btn btn-primary"
                :disabled="isBusy || isLoading"
                @click="startCreate">
          + Создать протокол
        </button>
      </div>

      <p v-if="currentDiscipline?.description"
         class="protocol-templates__description">
        {{ currentDiscipline.description }}
      </p>

      <p v-if="!canManage" class="protocol-templates__notice">
        Турнир завершён. Шаблоны доступны только для просмотра
        и предварительного расчёта.
      </p>

      <p v-if="error"
         class="organizer-error"
         role="alert">
        {{ error }}
      </p>

      <p v-if="success"
         class="organizer-success"
         role="status">
        {{ success }}
      </p>

      <!-- Редактор -->

      <ProtocolTemplateEditor v-if="editorOpen"
                              :key="editingTemplate?.id ?? 'new'"
                              :template="editingTemplate"
                              :busy="isSaving"
                              @save="save"
                              @cancel="closeEditor" />

      <!-- Список шаблонов -->

      <div class="protocol-templates__list-heading">
        <div>
          <h3>Шаблоны дисциплины</h3>

          <p v-if="activeTemplate">
            Активный: <strong>{{ activeTemplate.name }}</strong>
          </p>

          <p v-else-if="templates.length && !isLoading">
            Активный шаблон не выбран.
          </p>
        </div>

        <button type="button"
                class="btn btn-outline"
                :disabled="isLoading || isBusy"
                @click="reload">
          Обновить
        </button>
      </div>

      <p v-if="isLoading">Загрузка протоколов...</p>

      <p v-else-if="!templates.length && !error"
         class="protocol-templates__empty">
        Для выбранной дисциплины пока нет шаблонов.
      </p>

      <div v-else
           class="protocol-templates__grid">
        <article v-for="template in templates"
                 :key="template.id"
                 class="protocol-template-card">
          <div class="protocol-template-card__heading">
            <div>
              <h3>{{ template.name }}</h3>
              <p>{{ template.description || 'Без описания' }}</p>
            </div>

            <div class="protocol-template-card__badges">
              <span v-if="template.isActive"
                    class="protocol-template-card__badge protocol-template-card__badge--active">
                Активный
              </span>

              <span v-if="template.isLocked"
                    class="protocol-template-card__badge">
                Заблокирован
              </span>
            </div>
          </div>

          <p class="protocol-template-card__meta">
            Критериев: {{ template.criteria.length }}
          </p>

          <details class="protocol-template-card__details">
            <summary>Посмотреть критерии</summary>

            <ol>
              <li v-for="criterion in sortedCriteria(template)"
                  :key="criterion.id">
                <strong>{{ criterion.name }}</strong>

                <span>
                  {{ formatNumber(criterion.minValue) }}–
                  {{ formatNumber(criterion.maxValue ?? 0) }}
                  баллов · ×{{ formatNumber(criterion.weight) }}
                </span>

                <span v-if="criterion.isPenalty">
                  · штраф
                </span>
              </li>
            </ol>
          </details>

          <div class="organizer-actions">
            <button type="button"
                    class="btn btn-outline"
                    :disabled="isBusy || isLoading"
                    @click="openPreview(template)">
              Проверить расчёт
            </button>

            <button v-if="canManage && !template.isLocked"
                    type="button"
                    class="btn btn-outline"
                    :disabled="isBusy || isLoading"
                    @click="startEdit(template)">
              Редактировать
            </button>

            <button v-if="canManage && !template.isLocked"
                    type="button"
                    class="organizer-danger"
                    :disabled="isBusy || isLoading"
                    @click="remove(template)">
              {{
                deletingId === template.id
                  ? 'Удаление...'
                  : 'Удалить'
              }}
            </button>
          </div>

          <p v-if="template.isLocked"
             class="protocol-template-card__locked">
            Изменение заблокировано: по шаблону уже
            сохранены судейские оценки или отправлен
            судейский протокол.
          </p>
        </article>
      </div>

      <!-- Предварительный просмотр -->

      <div v-if="previewTemplate"
           class="protocol-preview">
        <div class="protocol-preview__heading">
          <div>
            <h3>Пробный расчёт</h3>
            <p>{{ previewTemplate.name }}</p>
          </div>

          <button type="button"
                  class="btn btn-outline"
                  :disabled="isPreviewing"
                  @click="closePreview">
            Закрыть
          </button>
        </div>

        <form @submit.prevent="calculatePreview">
          <p class="protocol-templates__description">
            Укажите оценки для каждого критерия.
            Данные не сохраняются в БД.
          </p>

          <div v-for="criterion in sortedCriteria(previewTemplate)"
               :key="criterion.id"
               class="protocol-preview__criterion">
            <div>
              <strong>{{ criterion.name }}</strong>

              <p>
                Диапазон:
                {{ formatNumber(criterion.minValue) }}–
                {{ formatNumber(criterion.maxValue ?? 0) }};
                коэффициент:
                {{ formatNumber(criterion.weight) }}

                <span v-if="criterion.isPenalty">
                  · штраф
                </span>
              </p>
            </div>

            <label>
              Баллы
              <input v-model.number="scoreValues[criterion.id]"
                     type="number"
                     :min="criterion.minValue"
                     :max="criterion.maxValue ?? undefined"
                     step="0.01"
                     required
                     :disabled="isPreviewing"
                     @input="previewResult = null" />
            </label>
          </div>

          <p v-if="previewError"
             class="organizer-error"
             role="alert">
            {{ previewError }}
          </p>

          <div class="organizer-actions">
            <button type="submit"
                    class="btn btn-primary"
                    :disabled="isPreviewing">
              {{
                isPreviewing
                  ? 'Вычисление...'
                  : 'Рассчитать результат'
              }}
            </button>
          </div>
        </form>

        <div v-if="previewResult"
             class="protocol-preview__result">
          <h4>Результат расчёта</h4>

          <div v-for="criterion in previewResult.criteria"
               :key="criterion.criterionId"
               class="protocol-preview__result-row">
            <span>{{ criterion.name }}</span>

            <strong>
              {{
                criterion.contribution > 0
                  ? '+'
                  : ''
              }}{{ formatNumber(criterion.contribution) }}
            </strong>
          </div>

          <div class="protocol-preview__total">
            <div>
              <span>Итоговый балл</span>
              <strong>
                {{ formatNumber(previewResult.total) }}
              </strong>
            </div>

            <div>
              <span>Наилучший возможный итог</span>
              <strong>
                {{ formatNumber(previewResult.maximumPossible) }}
              </strong>
            </div>
          </div>
        </div>
      </div>
    </template>
  </section>
</template>
