
<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'

import { useAuthStore } from '@/stores/auth'

  const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')

const showPassword = ref(false)
const isLoading = ref(false)
const errorMessage = ref('')

async function handleLogin() {
  if (isLoading.value) return

  errorMessage.value = ''
  isLoading.value = true

  try {
    await authStore.login({
      email: email.value.trim(),
      password: password.value,
    })

    const redirect = route.query.redirect

    if (
      typeof redirect === 'string' &&
      redirect.startsWith('/') &&
      !redirect.startsWith('//')
    ) {
      await router.replace(redirect)
    } else {
      await router.replace({ name: 'account' })
    }
  } catch (error) {
    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Не удалось войти в систему.'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <section class="login-card">
      <h1>Вход в систему</h1>

      <p class="login-card__description">
        Доступ к системе управления соревнованиями
      </p>

      <div class="login-card__tabs">
        <span class="login-card__tab login-card__tab--active">
          Вход
        </span>

        <RouterLink to="/register" class="login-card__tab">
          Регистрация
        </RouterLink>
      </div>

      <form class="login-form" @submit.prevent="handleLogin">
        <div class="login-form__field">
          <label for="login-email">Электронная почта</label>

          <input id="login-email"
                 v-model="email"
                 type="email"
                 placeholder="example@mail.ru"
                 autocomplete="username"
                 :disabled="isLoading"
                 required />
        </div>

        <div class="login-form__field">
          <label for="login-password">Пароль</label>

          <div class="login-form__password">
            <input id="login-password"
                   v-model="password"
                   :type="showPassword ? 'text' : 'password'"
                   placeholder="Введите пароль"
                   autocomplete="current-password"
                   :disabled="isLoading"
                   required />

            <button type="button"
                    class="login-form__toggle-password"
                    :aria-label="showPassword ? 'Скрыть пароль' : 'Показать пароль'"
                    :aria-pressed="showPassword"
                    @click="showPassword = !showPassword">
              {{ showPassword ? 'Скрыть' : 'Показать' }}
            </button>
          </div>
        </div>

        <p v-if="errorMessage"
           class="login-form__error"
           role="alert">
          {{ errorMessage }}
        </p>

        <button type="submit"
                class="btn btn-primary login-form__submit"
                :disabled="isLoading">
          {{ isLoading ? 'Выполняется вход...' : 'Войти в аккаунт →' }}
        </button>
      </form>

      <div class="login-card__footer">
        <span>Нет аккаунта?</span>

        <RouterLink to="/register">
          Зарегистрироваться
        </RouterLink>
      </div>
    </section>
  </div>
</template>
