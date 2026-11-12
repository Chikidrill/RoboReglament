
import { computed, ref, watch } from 'vue'
import { defineStore } from 'pinia'

import { useAuthStore } from '@/stores/auth'

export const dashboardRoles = [
  { value: 'Administrator', label: 'Администратор' },
  { value: 'Organizer', label: 'Организатор' },
  { value: 'Coach', label: 'Тренер' },
  { value: 'Judge', label: 'Судья' },
  { value: 'Participant', label: 'Участник' },
] as const

export type DashboardRole =
  (typeof dashboardRoles)[number]['value']

export const useDashboardStore = defineStore('dashboard', () => {
  const authStore = useAuthStore()

  const selectedRole = ref<DashboardRole | null>(null)

  const availableRoles = computed(() =>
    dashboardRoles.filter(role =>
      authStore.user?.roles.includes(role.value),
    ),
  )

  const activeRole = computed<DashboardRole | null>(() => {
    if (
      selectedRole.value &&
      availableRoles.value.some(
        role => role.value === selectedRole.value,
      )
    ) {
      return selectedRole.value
    }

    return availableRoles.value[0]?.value ?? null
  })

  function setRole(role: DashboardRole) {
    const isAllowed = availableRoles.value.some(
      item => item.value === role,
    )

    if (!isAllowed) {
      return
    }

    selectedRole.value = role

    const userId = authStore.user?.id

    if (userId) {
      sessionStorage.setItem(
        `roboreglament.dashboard.${userId}`,
        role,
      )
    }
  }

  watch(
    () => authStore.user?.id,
    userId => {
      selectedRole.value = null

      if (!userId) {
        return
      }

      const savedRole = sessionStorage.getItem(
        `roboreglament.dashboard.${userId}`,
      )

      const matchingRole = dashboardRoles.find(
        role => role.value === savedRole,
      )

      if (matchingRole) {
        selectedRole.value = matchingRole.value
      }
    },
    { immediate: true },
  )

  return {
    availableRoles,
    activeRole,
    setRole,
  }
})
