
<script setup lang="ts">
import {
  onMounted,
  onUnmounted,
  ref,
  watch,
} from 'vue'

import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const isOpen = ref(false)
const isLoggingOut = ref(false)
const logoutError = ref('')

const dropdownRef = ref<HTMLElement | null>(null)
const triggerRef = ref<HTMLButtonElement | null>(null)

function toggleMenu() {
  isOpen.value = !isOpen.value

  if (isOpen.value) {
    logoutError.value = ''
  }
}

function closeMenu() {
  isOpen.value = false
}

function handleOutsideClick(event: PointerEvent) {
  if (!isOpen.value || !dropdownRef.value) {
    return
  }

  const target = event.target

  if (
    target instanceof Node &&
    !dropdownRef.value.contains(target)
  ) {
    closeMenu()
  }
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape' && isOpen.value) {
    closeMenu()
    triggerRef.value?.focus()
  }
}

async function handleLogout() {
  if (isLoggingOut.value) {
    return
  }

  isLoggingOut.value = true
  logoutError.value = ''

  try {
    await authStore.logout()

    closeMenu()
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

// Закрываем меню при смене маршрута.
watch(
  () => route.fullPath,
  closeMenu,
)

onMounted(() => {
  document.addEventListener(
    'pointerdown',
    handleOutsideClick,
  )

  document.addEventListener(
    'keydown',
    handleKeydown,
  )
})

onUnmounted(() => {
  document.removeEventListener(
    'pointerdown',
    handleOutsideClick,
  )

  document.removeEventListener(
    'keydown',
    handleKeydown,
  )
})
</script>

<template>
  <div ref="dropdownRef"
       class="site-header__user">
    <button ref="triggerRef"
            type="button"
            class="site-header__user-trigger"
            :aria-expanded="isOpen"
            aria-controls="user-dropdown-menu"
            aria-label="Открыть меню пользователя"
            @click="toggleMenu">
      <span class="site-header__user-info">
        <span class="site-header__user-name">
          {{ authStore.fullName }}
        </span>

        <span class="site-header__user-role">
          {{ authStore.displayRoles }}
        </span>
      </span>

      <span class="site-header__avatar">
        {{ authStore.initials }}
      </span>

      <span class="site-header__user-caret"
            aria-hidden="true">
        ▾
      </span>
    </button>

    <nav v-if="isOpen"
         id="user-dropdown-menu"
         class="user-dropdown"
         aria-label="Меню пользователя">
      <div class="user-dropdown__heading">
        <strong>{{ authStore.fullName }}</strong>

        <span>{{ authStore.user?.email }}</span>
      </div>

      <div class="user-dropdown__divider"></div>

      <RouterLink to="/account"
                  class="user-dropdown__item"
                  @click="closeMenu">
        <span class="user-dropdown__icon"
              aria-hidden="true">
          ♙
        </span>

        Личный кабинет
      </RouterLink>

      <div class="user-dropdown__divider"></div>

      <button type="button"
              class="user-dropdown__item user-dropdown__item--danger"
              :disabled="isLoggingOut"
              @click="handleLogout">
        <span class="user-dropdown__icon"
              aria-hidden="true">
          ↪
        </span>

        {{ isLoggingOut ? 'Выход...' : 'Выйти' }}
      </button>

      <p v-if="logoutError"
         class="user-dropdown__error"
         role="alert">
        {{ logoutError }}
      </p>
    </nav>
  </div>
</template>
