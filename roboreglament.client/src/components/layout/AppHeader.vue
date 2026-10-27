
<script setup lang="ts">
  import { ref } from 'vue'
  import { useRouter } from 'vue-router'

  import { useAuthStore } from '@/stores/auth'

  const router = useRouter()
  const authStore = useAuthStore()

  const isLoggingOut = ref(false)
  const logoutError = ref('')

  const navigation = [
    { label: 'Главная', to: '/' },
    { label: 'Турниры', to: '/tournaments' },
    { label: 'О платформе', to: '/about' },
    { label: 'Регламент', to: '/regulations' },
  ]

  async function handleLogout() {
    if (isLoggingOut.value) {
      return
    }

    isLoggingOut.value = true
    logoutError.value = ''

    try {
      await authStore.logout()
      await router.replace({ name: 'home' })
    } catch (error) {
      logoutError.value =
        error instanceof Error
          ? error.message
          : 'Не удалось выйти из аккаунта.'
    } finally {
      isLoggingOut.value = false
    }
  }
</script>

<template>
  <header class="site-header">
    <div class="site-header__inner page-container">
      <RouterLink to="/" class="site-header__brand">
        <span class="site-header__logo">✦</span>
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

        <RouterLink v-if="authStore.isAuthenticated"
                    to="/account"
                    class="site-header__nav-link"
                    exact-active-class="is-active">
          Личный кабинет
        </RouterLink>
      </nav>

      <div class="site-header__actions">
        <template v-if="authStore.isAuthenticated">
          <div class="site-header__user">
            <RouterLink to="/account"
                        class="site-header__user-info">
              <span class="site-header__user-name">
                {{ authStore.fullName }}
              </span>

              <span class="site-header__user-role">
                {{ authStore.displayRoles }}
              </span>
            </RouterLink>

            <RouterLink to="/account"
                        class="site-header__avatar"
                        aria-label="Открыть личный кабинет">
              {{ authStore.initials }}
            </RouterLink>

            <button type="button"
                    class="site-header__logout"
                    :disabled="isLoggingOut"
                    @click="handleLogout">
              {{ isLoggingOut ? 'Выход...' : 'Выйти' }}
            </button>
          </div>
        </template>

        <template v-else-if="authStore.isInitialized">
          <RouterLink to="/login" class="btn btn-outline">
            Войти
          </RouterLink>

          <RouterLink to="/register" class="btn btn-primary">
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

    <p v-if="logoutError"
       class="site-header__error"
       role="alert">
      {{ logoutError }}
    </p>
  </header>
</template>
