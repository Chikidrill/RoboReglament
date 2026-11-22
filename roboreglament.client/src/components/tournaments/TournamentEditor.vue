
<script setup lang="ts">import { reactive, ref } from 'vue'

import type {
  Tournament,
  TournamentRequest,
} from '@/types/tournament'

const props = defineProps<{
  tournament?: Tournament
  busy?: boolean
}>()

const emit = defineEmits<{
  save: [data: TournamentRequest]
  cancel: []
}>()

function toLocalInput(value?: string | null): string {
  if (!value) return ''

  const utcValue = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value)
    ? value
    : `${value}Z`

  const date = new Date(utcValue)

  if (!Number.isFinite(date.getTime())) return ''

  return new Date(
    date.getTime() - date.getTimezoneOffset() * 60000,
  ).toISOString().slice(0, 16)
}

const form = reactive({
  name: props.tournament?.name ?? '',
  description: props.tournament?.description ?? '',
  location: props.tournament?.location ?? '',
  startDate: props.tournament?.startDate ?? '',
  endDate: props.tournament?.endDate ?? '',
  registrationDeadline: toLocalInput(
    props.tournament?.registrationDeadline,
  ),
})

const error = ref('')

function submit() {
  if (props.busy) return

  error.value = ''

  if (!form.name.trim()) {
    error.value = 'Введите название турнира.'
    return
  }

  if (!form.startDate || !form.endDate) {
    error.value = 'Укажите даты проведения.'
    return
  }

  if (form.startDate > form.endDate) {
    error.value = 'Проверьте даты проведения.'
    return
  }

  let deadline: string | null = null

  if (form.registrationDeadline) {
    const date = new Date(form.registrationDeadline)

    if (!Number.isFinite(date.getTime())) {
      error.value = 'Некорректный срок регистрации.'
      return
    }

    deadline = date.toISOString()
  }

  emit('save', {
    name: form.name.trim(),
    description: form.description.trim() || null,
    location: form.location.trim() || null,
    startDate: form.startDate,
    endDate: form.endDate,
    registrationDeadline: deadline,
  })
}</script>

<template>
  <form class="organizer-form"
        @submit.prevent="submit">
    <fieldset :disabled="busy">
      <label>
        Название турнира *
        <input v-model="form.name"
               type="text"
               maxlength="200"
               required />
      </label>

      <label>
        Место проведения
        <input v-model="form.location"
               type="text"
               maxlength="300"
               placeholder="Город, площадка" />
      </label>

      <label>
        Описание
        <textarea v-model="form.description"
                  rows="5"
                  maxlength="2000" />
      </label>

      <div class="organizer-form__row">
        <label>
          Дата начала *
          <input v-model="form.startDate"
                 type="date"
                 required />
        </label>

        <label>
          Дата окончания *
          <input v-model="form.endDate"
                 type="date"
                 required />
        </label>
      </div>

      <label>
        Окончание приёма заявок
        <input v-model="form.registrationDeadline"
               type="datetime-local" />
        <small>
          Время указывается в часовом поясе
          текущего устройства.
        </small>
      </label>

      <p v-if="error" class="organizer-error">
        {{ error }}
      </p>

      <div class="organizer-actions">
        <button type="button"
                class="btn btn-outline"
                @click="emit('cancel')">
          Отмена
        </button>

        <button type="submit" class="btn btn-primary">
          {{ busy ? 'Сохранение...' : 'Сохранить' }}
        </button>
      </div>
    </fieldset>
  </form>
</template>
