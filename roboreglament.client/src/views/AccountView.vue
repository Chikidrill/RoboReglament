
<script setup lang="ts">
  import { ref } from 'vue'
  import { useAuthStore } from '@/stores/auth'

  const authStore = useAuthStore()

  const copyMessage = ref('')
  const copyError = ref('')

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
    </div>
  </section>
</template>
