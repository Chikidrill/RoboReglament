
import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

import {
  ApiError,
  getCurrentUser,
  login as loginRequest,
  logout as logoutRequest,
  register as registerRequest,
} from '@/api/auth'

import type {
  AuthUser,
  LoginRequest,
  RegisterRequest,
} from '@/types/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  const isInitialized = ref(false)
  const initializationError = ref('')
   
  const isAuthenticated = computed(() => user.value !== null)

  const fullName = computed(() => {
    if (!user.value) {
      return ''
    }

    return `${user.value.firstName} ${user.value.lastName}`
  })

  const initials = computed(() => {
    if (!user.value) {
      return ''
    }

    return (
      user.value.firstName.charAt(0) +
      user.value.lastName.charAt(0)
    ).toUpperCase()
  })


  const roleNames: Record<string, string> = {
    Administrator: 'Администратор',
    Organizer: 'Организатор',
    Judge: 'Судья',
    Coach: 'Тренер',
    Participant: 'Участник',
  }

  const displayRoles = computed(() => {
    if (!user.value) {
      return ''
    }

    return user.value.roles
      .map(role => roleNames[role] ?? role)
      .join(' · ')
  })


  async function initialize() {
    if (isInitialized.value) {
      return
    }

    initializationError.value = ''

    try {
      user.value = await getCurrentUser()
    } catch (error) {
      user.value = null

      if (!(error instanceof ApiError && error.status === 401)) {
        initializationError.value =
          error instanceof Error
            ? error.message
            : 'Не удалось проверить авторизацию.'
      }
    } finally {
      isInitialized.value = true
    }
  }

  async function login(credentials: LoginRequest) {
    await loginRequest(credentials)

    user.value = await getCurrentUser()
    isInitialized.value = true
    initializationError.value = ''
  }

  async function register(data: RegisterRequest) {
    await registerRequest(data)

    user.value = await getCurrentUser()
    isInitialized.value = true
    initializationError.value = ''
  }

  async function logout() {
    await logoutRequest()

    user.value = null
    isInitialized.value = true
    initializationError.value = ''
  }

  return {
    user,
    isInitialized,
    initializationError,
    isAuthenticated,
    fullName,
    initials,
    displayRoles,
    initialize,
    login,
    register,
    logout,
  }
})
