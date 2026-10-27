
<script setup lang="ts">
    import { useAuthStore } from '@/stores/auth'

    const authStore = useAuthStore()
</script>

<template>
  <section class="account-page">
    <h1>Личный кабинет</h1>

    <p class="account-page__description">
      Информация о вашей учётной записи
    </p>

    <div v-if="authStore.user" class="account-card">
      <div class="account-card__heading">
        <div class="account-card__avatar">
          {{ authStore.initials }}
        </div>

        <div>
          <h2>{{ authStore.fullName }}</h2>
          <span>{{ authStore.displayRoles }}</span>
        </div>
      </div>

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
