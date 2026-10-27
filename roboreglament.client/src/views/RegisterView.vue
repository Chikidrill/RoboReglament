
<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'

import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const form = reactive({
  firstName: '',
  lastName: '',
  middleName: '',
  email: '',
  password: '',
  confirmPassword: '',
})

const showPassword = ref(false)
const isLoading = ref(false)
const errorMessage = ref('')

async function handleRegister() {
  if (isLoading.value) return

  errorMessage.value = ''

  if (!form.firstName.trim() || !form.lastName.trim()) {
    errorMessage.value = 'Укажите имя и фамилию.'
    return
  }

  if (form.password.length < 6) {
    errorMessage.value = 'Пароль должен содержать не менее 6 символов.'
    return
  }

  if (
    !/\p{Ll}/u.test(form.password) ||
    !/\p{Nd}/u.test(form.password)
  ) {
    errorMessage.value =
      'Пароль должен содержать строчную букву и цифру.'
    return
  }

  if (form.password !== form.confirmPassword) {
    errorMessage.value = 'Пароли не совпадают.'
    return
  }

  isLoading.value = true

  try {
    await authStore.register({
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      middleName: form.middleName.trim() || null,
      email: form.email.trim(),
      password: form.password,
    })

    await router.replace({ name: 'account' })
  } catch (error) {
    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Не удалось зарегистрироваться.'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="register-page">
    <section class="register-card">
      <h1>Создание аккаунта</h1>

      <p class="register-card__description">
        Зарегистрируйтесь для участия в соревнованиях
      </p>

      <form class="register-form" @submit.prevent="handleRegister">
        <div class="register-form__row">
          <div class="register-form__field">
            <label for="register-first-name">Имя</label>

            <input id="register-first-name"
                   v-model="form.firstName"
                   type="text"
                   placeholder="Введите имя"
                   autocomplete="given-name"
                   maxlength="100"
                   :disabled="isLoading"
                   required />
          </div>

          <div class="register-form__field">
            <label for="register-last-name">Фамилия</label>

            <input id="register-last-name"
                   v-model="form.lastName"
                   type="text"
                   placeholder="Введите фамилию"
                   autocomplete="family-name"
                   maxlength="100"
                   :disabled="isLoading"
                   required />
          </div>
        </div>

        <div class="register-form__field">
          <label for="register-middle-name">
            Отчество <span>(необязательно)</span>
          </label>

          <input id="register-middle-name"
                 v-model="form.middleName"
                 type="text"
                 placeholder="Введите отчество"
                 autocomplete="additional-name"
                 maxlength="100"
                 :disabled="isLoading" />
        </div>

        <div class="register-form__field">
          <label for="register-email">Электронная почта</label>

          <input id="register-email"
                 v-model="form.email"
                 type="email"
                 placeholder="example@mail.ru"
                 autocomplete="email"
                 :disabled="isLoading"
                 required />
        </div>

        <div class="register-form__field">
          <label for="register-password">Пароль</label>

          <div class="register-form__password">
            <input id="register-password"
                   v-model="form.password"
                   :type="showPassword ? 'text' : 'password'"
                   placeholder="Не менее 6 символов"
                   autocomplete="new-password"
                   :disabled="isLoading"
                   minlength="6"
                   required />

            <button type="button"
                    class="register-form__toggle-password"
                    :aria-label="showPassword ? 'Скрыть пароль' : 'Показать пароль'"
                    :aria-pressed="showPassword"
                    @click="showPassword = !showPassword">
              {{ showPassword ? 'Скрыть' : 'Показать' }}
            </button>
          </div>

          <span class="register-form__hint">
            Минимум 6 символов, строчная буква и цифра
          </span>
        </div>

        <div class="register-form__field">
          <label for="register-confirm-password">
            Подтверждение пароля
          </label>

          <input id="register-confirm-password"
                 v-model="form.confirmPassword"
                 type="password"
                 placeholder="Повторите пароль"
                 autocomplete="new-password"
                 :disabled="isLoading"
                 required />
        </div>

        <p v-if="errorMessage"
           class="register-form__error"
           role="alert">
          {{ errorMessage }}
        </p>

        <button type="submit"
                class="btn btn-primary register-form__submit"
                :disabled="isLoading">
          {{
            isLoading
              ? 'Создание аккаунта...'
              : 'Зарегистрироваться →'
          }}
        </button>
      </form>

      <div class="register-card__footer">
        <span>Уже есть аккаунт?</span>
        <RouterLink to="/login">Войти</RouterLink>
      </div>
    </section>
  </div>
</template>
