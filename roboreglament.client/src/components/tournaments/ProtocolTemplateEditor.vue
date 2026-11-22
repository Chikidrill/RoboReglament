
<script setup lang="ts">import { computed, ref } from 'vue'

import type {
  ProtocolCriterion,
  ProtocolTemplate,
  SaveProtocolTemplateRequest,
} from '@/types/protocol'

const props = withDefaults(defineProps<{
  template?: ProtocolTemplate | null
  busy?: boolean
}>(), {
  template: null,
  busy: false,
})

const emit = defineEmits<{
  save: [data: SaveProtocolTemplateRequest]
  cancel: []
}>()

interface EditableCriterion {
  localId: number
  name: string
  description: string
  minValue: number
  maxValue: number
  weight: number
  isPenalty: boolean
}

let nextLocalId = 0

function makeCriterion(
  source?: ProtocolCriterion,
): EditableCriterion {
  return {
    localId: ++nextLocalId,
    name: source?.name ?? '',
    description: source?.description ?? '',
    minValue: source?.minValue ?? 0,
    maxValue: source?.maxValue ?? 10,
    weight: source?.weight ?? 1,
    isPenalty: source?.isPenalty ?? false,
  }
}

const name = ref(props.template?.name ?? '')
const description = ref(props.template?.description ?? '')
const isActive = ref(props.template?.isActive ?? true)
const error = ref('')

const criteria = ref<EditableCriterion[]>(
  props.template?.criteria.length
    ? [...props.template.criteria]
        .sort((a, b) => a.order - b.order)
        .map(item => makeCriterion(item))
    : [makeCriterion()],
)

const theoreticalMaximum = computed(() => {
  const total = criteria.value.reduce((sum, item) => {
    const value = item.isPenalty
      ? -item.minValue
      : item.maxValue

    return sum + value * item.weight
  }, 0)

  return Number.isFinite(total) ? total : 0
})

function formatNumber(value: number): string {
  return new Intl.NumberFormat('ru-RU', {
    maximumFractionDigits: 2,
  }).format(value)
}

function addCriterion() {
  if (criteria.value.length >= 100 || props.busy) return

  criteria.value.push(makeCriterion())
  error.value = ''
}

function removeCriterion(index: number) {
  if (props.busy || criteria.value.length <= 1) return

  criteria.value.splice(index, 1)
  error.value = ''
}

function moveCriterion(index: number, direction: -1 | 1) {
  if (props.busy) return

  const destination = index + direction

  if (destination < 0 || destination >= criteria.value.length) {
    return
  }

  const [item] = criteria.value.splice(index, 1)

  if (item) {
    criteria.value.splice(destination, 0, item)
  }
}

function isValidDecimal(value: number): boolean {
  return (
    typeof value === 'number' &&
    Number.isFinite(value) &&
    Number(value.toFixed(2)) === value
  )
}

function validate(): string | null {
  if (!name.value.trim()) {
    return 'Введите название протокола.'
  }

  if (name.value.trim().length > 200) {
    return 'Название протокола слишком длинное.'
  }

  if (description.value.trim().length > 2000) {
    return 'Описание протокола слишком длинное.'
  }

  if (criteria.value.length < 1 || criteria.value.length > 100) {
    return 'Допускается от 1 до 100 критериев.'
  }

  const names = new Set<string>()

  for (let i = 0; i < criteria.value.length; i++) {
    const item = criteria.value[i]!

    const criterionName = item.name.trim()

    if (!criterionName) {
      return `Укажите название критерия №${i + 1}.`
    }

    if (criterionName.length > 200) {
      return `Слишком длинное название критерия №${i + 1}.`
    }

    if (item.description.trim().length > 1000) {
      return `Слишком длинное описание критерия №${i + 1}.`
    }

    const normalizedName =
      criterionName.toLocaleLowerCase('ru-RU')

    if (names.has(normalizedName)) {
      return `Критерий «${criterionName}» повторяется.`
    }

    names.add(normalizedName)

    if (
      !isValidDecimal(item.minValue) ||
      !isValidDecimal(item.maxValue) ||
      item.minValue < 0 ||
      item.maxValue < item.minValue ||
      item.maxValue > 100000
    ) {
      return `Проверьте диапазон баллов критерия №${i + 1}.`
    }

    if (
      !isValidDecimal(item.weight) ||
      item.weight <= 0 ||
      item.weight > 100
    ) {
      return `Недопустимый коэффициент критерия №${i + 1}.`
    }
  }

  return null
}

function submit() {
  if (props.busy) return

  error.value = validate() ?? ''

  if (error.value) return

  emit('save', {
    name: name.value.trim(),
    description: description.value.trim() || null,
    isActive: isActive.value,
    criteria: criteria.value.map(item => ({
      name: item.name.trim(),
      description: item.description.trim() || null,
      minValue: item.minValue,
      maxValue: item.maxValue,
      weight: item.weight,
      isPenalty: item.isPenalty,
    })),
  })
}</script>

<template>
  <form class="protocol-editor"
        @submit.prevent="submit">
    <fieldset :disabled="busy">
      <div class="protocol-editor__heading">
        <h3>
          {{
            template
              ? 'Редактирование протокола'
              : 'Новый протокол'
          }}
        </h3>

        <p>
          Настройте критерии, по которым судьи будут
          оценивать выступления команд.
        </p>
      </div>

      <div class="protocol-editor__basics">
        <label>
          Название протокола *
          <input v-model="name"
                 type="text"
                 maxlength="200"
                 required
                 placeholder="Например, Протокол Робосумо" />
        </label>

        <label>
          Описание
          <textarea v-model="description"
                    maxlength="2000"
                    rows="3"
                    placeholder="Общие правила оценки выступлений" />
        </label>

        <label class="protocol-editor__check">
          <input v-model="isActive"
                 type="checkbox" />

          <span>
            Сделать шаблон активным

            <small>
              Активным может быть только один
              шаблон в каждой дисциплине.
            </small>
          </span>
        </label>
      </div>

      <div class="protocol-editor__criteria-heading">
        <div>
          <h3>Критерии оценки</h3>

          <p>
            Порядок критериев в списке будет
            сохранён в протоколе.
          </p>
        </div>

        <button type="button"
                class="btn btn-outline"
                :disabled="busy || criteria.length >= 100"
                @click="addCriterion">
          + Добавить критерий
        </button>
      </div>

      <div class="protocol-editor__criteria">
        <article v-for="(criterion, index) in criteria"
                 :key="criterion.localId"
                 class="protocol-criterion">
          <div class="protocol-criterion__heading">
            <strong>Критерий №{{ index + 1 }}</strong>

            <div class="protocol-criterion__controls">
              <button type="button"
                      class="protocol-criterion__control"
                      title="Поднять выше"
                      :aria-label="`Поднять критерий ${index + 1} выше`"
                      :disabled="busy || index === 0"
                      @click="moveCriterion(index, -1)">
                ↑
              </button>

              <button type="button"
                      class="protocol-criterion__control"
                      title="Опустить ниже"
                      :aria-label="`Опустить критерий ${index + 1} ниже`"
                      :disabled="busy || index === criteria.length - 1"
                      @click="moveCriterion(index, 1)">
                ↓
              </button>

              <button type="button"
                      class="organizer-danger"
                      :disabled="busy || criteria.length <= 1"
                      @click="removeCriterion(index)">
                Удалить
              </button>
            </div>
          </div>

          <div class="protocol-editor__fields">
            <label class="protocol-editor__full">
              Название критерия *
              <input v-model="criterion.name"
                     type="text"
                     required
                     maxlength="200"
                     placeholder="Например, Точность выполнения" />
            </label>

            <label class="protocol-editor__full">
              Описание
              <textarea v-model="criterion.description"
                        maxlength="1000"
                        rows="2"
                        placeholder="Условия начисления баллов" />
            </label>

            <label>
              Минимальный балл
              <input v-model.number="criterion.minValue"
                     type="number"
                     min="0"
                     max="100000"
                     step="0.01"
                     required />
            </label>

            <label>
              Максимальный балл
              <input v-model.number="criterion.maxValue"
                     type="number"
                     min="0"
                     max="100000"
                     step="0.01"
                     required />
            </label>

            <label>
              Весовой коэффициент
              <input v-model.number="criterion.weight"
                     type="number"
                     min="0.01"
                     max="100"
                     step="0.01"
                     required />
            </label>

            <div class="protocol-criterion__kind">
              <label class="protocol-editor__check">
                <input v-model="criterion.isPenalty"
                       type="checkbox" />

                <span>Штрафной критерий</span>
              </label>

              <small>
                {{
                  criterion.isPenalty
                    ? 'Баллы вычитаются из результата'
                    : 'Баллы прибавляются к результату'
                }}
              </small>
            </div>
          </div>
        </article>
      </div>

      <div class="protocol-editor__summary">
        <div>
          <span>Количество критериев</span>
          <strong>{{ criteria.length }}</strong>
        </div>

        <div>
          <span>Наилучший возможный итог</span>
          <strong>
            {{ formatNumber(theoreticalMaximum) }}
          </strong>
        </div>
      </div>

      <p class="protocol-editor__hint">
        Итог рассчитывается как сумма баллов,
        умноженных на коэффициенты. Штрафные
        критерии вычитаются. Допускается
        не более двух знаков после запятой.
      </p>

      <p v-if="error"
         class="organizer-error"
         role="alert">
        {{ error }}
      </p>

      <div class="organizer-actions">
        <button type="button"
                class="btn btn-outline"
                :disabled="busy"
                @click="emit('cancel')">
          Отмена
        </button>

        <button type="submit"
                class="btn btn-primary"
                :disabled="busy">
          {{
            busy
              ? 'Сохранение...'
              : 'Сохранить протокол'
          }}
        </button>
      </div>
    </fieldset>
  </form>
</template>
