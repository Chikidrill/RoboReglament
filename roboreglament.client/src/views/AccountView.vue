
<script setup lang="ts">
  import { ref } from 'vue'
  import { useAuthStore } from '@/stores/auth'
  import { changePassword } from '@/api/auth'
  const authStore = useAuthStore()

  const copyMessage = ref('')
  const copyError = ref('')

  const isPasswordFormOpen = ref(false)

  const currentPassword = ref('')
  const newPassword = ref('')
  const confirmPassword = ref('')

  const passwordBusy = ref(false)
  const passwordError = ref('')
  const passwordSuccess = ref('')

  function togglePasswordForm() {
    isPasswordFormOpen.value = !isPasswordFormOpen.value

    passwordError.value = ''
    passwordSuccess.value = ''

    if (!isPasswordFormOpen.value) {
      currentPassword.value = ''
      newPassword.value = ''
      confirmPassword.value = ''
    }
  }

  async function submitPassword() {
    if (passwordBusy.value) return

    passwordError.value = ''
    passwordSuccess.value = ''

    if (!currentPassword.value || !newPassword.value ||
      !confirmPassword.value) {
      passwordError.value = 'Заполните все поля.'
      return
    }

    if (newPassword.value !== confirmPassword.value) {
      passwordError.value = 'Новые пароли не совпадают.'
      return
    }

    if (newPassword.value === currentPassword.value) {
      passwordError.value =
        'Новый пароль должен отличаться от текущего.'
      return
    }

    passwordBusy.value = true

    try {
      await changePassword({
        currentPassword: currentPassword.value,
        newPassword: newPassword.value,
      })

      currentPassword.value = ''
      newPassword.value = ''
      confirmPassword.value = ''

      isPasswordFormOpen.value = false
      passwordSuccess.value = 'Пароль успешно изменён.'
    } catch (err) {
      passwordError.value = err instanceof Error
        ? err.message
        : 'Не удалось изменить пароль.'
    } finally {
      passwordBusy.value = false
    }
  }

  async function copyPublicId() {
    const publicId = authStore.user?.publicId

    if (publicId === undefined) return

    copyMessage.value = ''
    copyError.value = ''

    try {
      await navigator.clipboard.writeText(
        String(publicId),
      )

      copyMessage.value = 'ID скопирован.'
    } catch {
      copyError.value =
        'Не удалось скопировать ID автоматически.'
    }
  }
</script>

<template>
  <section class="account-page">
    <h1>Личный кабинет</h1>

    <p class="account-page__description">
      Информация о вашей учётной записи
    </p>

    <div v-if="authStore.user"
         class="account-card">
      <div class="account-card__heading">
        <div class="account-card__avatar">
          {{ authStore.initials }}
        </div>

        <div>
          <h2>{{ authStore.fullName }}</h2>
          <span>{{ authStore.displayRoles }}</span>
        </div>
      </div>

      <!-- Публичный ID -->
      <div class="account-card__public-id">
        <div>
          <span class="account-card__id-label">
            Публичный ID пользователя
          </span>

          <strong class="account-card__id-value">
            {{ authStore.user.publicId }}
          </strong>

          <p class="account-card__id-hint">
            Используйте этот ID для поиска вашего
            аккаунта внутри платформы.
          </p>
        </div>

        <button type="button"
                class="btn btn-outline"
                @click="copyPublicId">
          Копировать
        </button>
      </div>

      <p v-if="copyMessage"
         class="account-card__copy-success"
         role="status">
        {{ copyMessage }}
      </p>

      <p v-if="copyError"
         class="account-card__copy-error"
         role="alert">
        {{ copyError }}
      </p>

      <div class="account-card__field">
        <span>Электронная почта</span>
        <strong>{{ authStore.user.email }}</strong>
      </div>

      <div class="account-card__field">
        <span>Фамилия</span>
        <strong>{{ authStore.user.lastName }}</strong>
      </div>

      <div class="account-card__field">
        <span>Имя</span>
        <strong>{{ authStore.user.firstName }}</strong>
      </div>

      <div class="account-card__field">
        <span>Отчество</span>
        <strong>
          {{ authStore.user.middleName || 'Не указано' }}
        </strong>
      </div>

      <div class="account-password">
        <div class="account-password__heading">
          <div>
            <h3>Смена пароля</h3>
            <p>
              Вы можете изменить пароль для входа
              в свою учётную запись.
            </p>
          </div>

          <button type="button"
                  class="btn btn-outline"
                  :disabled="passwordBusy"
                  @click="togglePasswordForm">
            {{
        isPasswordFormOpen
          ? 'Отмена'
          : 'Изменить пароль'
            }}
          </button>
        </div>

        <p v-if="passwordSuccess"
           class="account-password__success"
           role="status">
          {{ passwordSuccess }}
        </p>

        <form v-if="isPasswordFormOpen"
              class="account-password__form"
              @submit.prevent="submitPassword">
          <label>
            Текущий пароль
            <input v-model="currentPassword"
                   type="password"
                   autocomplete="current-password"
                   required
                   :disabled="passwordBusy"
                   placeholder="Введите текущий пароль" />
          </label>

          <label>
            Новый пароль
            <input v-model="newPassword"
                   type="password"
                   autocomplete="new-password"
                   required
                   :disabled="passwordBusy"
                   placeholder="Введите новый пароль" />
          </label>

          <label>
            Повторите новый пароль
            <input v-model="confirmPassword"
                   type="password"
                   autocomplete="new-password"
                   required
                   :disabled="passwordBusy"
                   placeholder="Повторите новый пароль" />
          </label>

          <p v-if="passwordError"
             class="account-password__error"
             role="alert">
            {{ passwordError }}
          </p>

          <div class="account-password__actions">
            <button type="submit"
                    class="btn btn-primary"
                    :disabled="passwordBusy">
              {{
          passwordBusy
            ? 'Сохранение...'
            : 'Сохранить новый пароль'
              }}
            </button>
          </div>
        </form>
      </div>

    </div>
  </section>
</template>
