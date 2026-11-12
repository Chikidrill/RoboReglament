
<script setup lang="ts">
  import logo from '@/assets/logo.svg'

  import UserDropdown from '@/components/layout/UserDropdown.vue'
  import { useAuthStore } from '@/stores/auth'

  const authStore = useAuthStore()

  const navigation = [
    { label: 'Главная', to: '/' },
    { label: 'Турниры', to: '/tournaments' },
    { label: 'О платформе', to: '/about' },
    { label: 'Регламент', to: '/regulations' },
  ]
</script>

<template>
  <header class="site-header">
    <div class="site-header__inner page-container">
      <RouterLink to="/"
                  class="site-header__brand">
        <img :src="logo"
             alt=""
             class="site-header__logo" />

        <span>РобоРегламент</span>
      </RouterLink>

      <nav class="site-header__nav"
           aria-label="Основная навигация">
        <RouterLink v-for="item in navigation"
                    :key="item.to"
                    :to="item.to"
                    class="site-header__nav-link"
                    exact-active-class="is-active">
          {{ item.label }}
        </RouterLink>
      </nav>

      <div class="site-header__actions">
        <UserDropdown v-if="authStore.isAuthenticated" />

        <template v-else-if="authStore.isInitialized">
          <RouterLink to="/login"
                      class="btn btn-outline">
            Войти
          </RouterLink>

          <RouterLink to="/register"
                      class="btn btn-primary">
            Регистрация
          </RouterLink>
        </template>

        <span v-else
              class="site-header__loading"
              role="status">
          Загрузка...
        </span>
      </div>
    </div>
  </header>
</template>
