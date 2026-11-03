
<script setup lang="ts">
  import {
    computed,
    onMounted,
    onUnmounted,
    ref,
  } from 'vue'

  import {
    getAdminUsers,
    updateAdminUserRoles,
  } from '@/api/admin'

  import type { AdminUser } from '@/types/admin'

  const users = ref<AdminUser[]>([])
  const isLoading = ref(true)
  const error = ref('')
  const searchQuery = ref('')

  const editingUserId = ref<string | null>(null)
  const selectedRoles = ref<string[]>([])
  const isSavingRoles = ref(false)
  const editError = ref('')
  const successMessage = ref('')

  const controller = new AbortController()

  const roleNames: Record<string, string> = {
    'Platform Owner': 'Владелец платформы',
    PlatformOwner: 'Владелец платформы',
    Administrator: 'Администратор',
    Organizer: 'Организатор',
    Judge: 'Судья',
    Coach: 'Тренер',
    Participant: 'Участник',
  }

  // Только эти роли можно редактировать
  // через обычный административный API.
  const editableRoles = [
    { value: 'Participant', label: 'Участник' },
    { value: 'Coach', label: 'Тренер' },
    { value: 'Judge', label: 'Судья' },
    { value: 'Organizer', label: 'Организатор' },
  ]

  const editableRoleValues = new Set(
    editableRoles.map(role => role.value),
  )

  function getRoleName(role: string): string {
    return roleNames[role] ?? role
  }

  function getFullName(user: AdminUser): string {
    return [
      user.lastName,
      user.firstName,
      user.middleName,
    ]
      .filter(Boolean)
      .join(' ')
  }

  const filteredUsers = computed(() => {
    const query = searchQuery.value
      .trim()
      .toLocaleLowerCase('ru-RU')

    if (!query) {
      return users.value
    }

    return users.value.filter(user => {
      const fullName = getFullName(user)
        .toLocaleLowerCase('ru-RU')

      const email = user.email
        .toLocaleLowerCase('ru-RU')

      return (
        fullName.includes(query) ||
        email.includes(query)
      )
    })
  })

  async function loadUsers() {
    isLoading.value = true
    error.value = ''
    successMessage.value = ''

    try {
      users.value = await getAdminUsers(controller.signal)
    } catch (err) {
      if (!controller.signal.aborted) {
        error.value =
          err instanceof Error
            ? err.message
            : 'Не удалось загрузить пользователей.'
      }
    } finally {
      if (!controller.signal.aborted) {
        isLoading.value = false
      }
    }
  }

  function startEditing(user: AdminUser) {
    if (isSavingRoles.value) {
      return
    }

    editingUserId.value = user.id

    // Берём только обычные роли.
    // Administrator и Platform Owner не редактируем.
    selectedRoles.value = user.roles.filter(
      role => editableRoleValues.has(role),
    )

    editError.value = ''
    successMessage.value = ''
  }

  function cancelEditing() {
    if (isSavingRoles.value) {
      return
    }

    editingUserId.value = null
    selectedRoles.value = []
    editError.value = ''
  }

  function hasRoleChanges(user: AdminUser): boolean {
    return editableRoles.some(role => {
      const wasAssigned = user.roles.includes(role.value)
      const isSelected = selectedRoles.value.includes(role.value)

      return wasAssigned !== isSelected
    })
  }

  async function saveRoles(user: AdminUser) {
    if (
      isSavingRoles.value ||
      editingUserId.value !== user.id ||
      !hasRoleChanges(user)
    ) {
      return
    }

    isSavingRoles.value = true
    editError.value = ''
    successMessage.value = ''

    try {
      const updatedUser = await updateAdminUserRoles(
        user.id,
        [...selectedRoles.value],
      )

      // Сразу обновляем данные в таблице
      // без перезагрузки всей страницы.
      users.value = users.value.map(item =>
        item.id === user.id
          ? {
            ...item,
            roles: updatedUser.roles,
          }
          : item,
      )

      editingUserId.value = null
      selectedRoles.value = []

      successMessage.value =
        `Роли пользователя ${getFullName(user)} обновлены.`
    } catch (err) {
      editError.value =
        err instanceof Error
          ? err.message
          : 'Не удалось сохранить изменения.'
    } finally {
      isSavingRoles.value = false
    }
  }

  onMounted(() => {
    void loadUsers()
  })

  onUnmounted(() => {
    controller.abort()
  })
</script>

<template>
  <section class="admin-page">
    <div class="admin-page__heading">
      <div>
        <h1>Управление пользователями</h1>

        <p>
          Просмотр пользователей платформы
          и управление их ролями.
        </p>
      </div>

      <button class="btn btn-outline"
              type="button"
              :disabled="isLoading || isSavingRoles || editingUserId !== null"
              @click="loadUsers">
        Обновить список
      </button>
    </div>

    <div class="admin-page__toolbar">
      <label class="admin-page__search">
        <span>Поиск пользователя</span>

        <input v-model="searchQuery"
               type="search"
               placeholder="ФИО или электронная почта" />
      </label>

      <div class="admin-page__count">
        Всего пользователей:
        <strong>{{ users.length }}</strong>
      </div>
    </div>

    <p v-if="successMessage"
       class="admin-page__success"
       role="status">
      {{ successMessage }}
    </p>

    <div v-if="isLoading"
         class="admin-page__message"
         role="status">
      Загрузка пользователей...
    </div>

    <div v-else-if="error"
         class="admin-page__message"
         role="alert">
      <p>{{ error }}</p>

      <button class="btn btn-outline"
              type="button"
              @click="loadUsers">
        Повторить
      </button>
    </div>

    <template v-else>
      <div v-if="filteredUsers.length > 0"
           class="admin-table-wrapper">
        <table class="admin-users-table">
          <thead>
            <tr>
              <th scope="col">Пользователь</th>
              <th scope="col">Электронная почта</th>
              <th scope="col">Роли</th>
              <th scope="col">Действия</th>
            </tr>
          </thead>

          <tbody>
            <template v-for="user in filteredUsers"
                      :key="user.id">
              <tr>
                <td class="admin-users-table__name">
                  {{ getFullName(user) }}
                </td>

                <td class="admin-users-table__email">
                  {{ user.email }}
                </td>

                <td>
                  <div class="admin-users-table__roles">
                    <span v-for="role in user.roles"
                          :key="role"
                          class="admin-role">
                      {{ getRoleName(role) }}
                    </span>

                    <span v-if="user.roles.length === 0"
                          class="admin-users-table__empty">
                      Нет назначенных ролей
                    </span>
                  </div>
                </td>

                <td class="admin-users-table__actions">
                  <button class="admin-page__edit-button"
                          type="button"
                          :disabled="
                      isSavingRoles ||
                      (editingUserId !== null &&
                        editingUserId !== user.id)
                    "
                          @click="
                      editingUserId === user.id
                        ? cancelEditing()
                        : startEditing(user)
                    ">
                    {{
                      editingUserId === user.id
                        ? 'Закрыть'
                        : 'Изменить роли'
                    }}
                  </button>
                </td>
              </tr>

              <tr v-if="editingUserId === user.id">
                <td colspan="4"
                    class="admin-role-editor-cell">
                  <div class="admin-role-editor">
                    <h3>
                      Роли пользователя:
                      {{ getFullName(user) }}
                    </h3>

                    <p class="admin-role-editor__description">
                      Выберите роли, которые нужно
                      назначить пользователю.
                    </p>

                    <div class="admin-role-editor__options">
                      <label v-for="role in editableRoles"
                             :key="role.value"
                             class="admin-role-editor__option">
                        <input v-model="selectedRoles"
                               type="checkbox"
                               :value="role.value"
                               :disabled="isSavingRoles" />

                        <span>{{ role.label }}</span>
                      </label>
                    </div>

                    <p class="admin-role-editor__hint">
                      Роли администратора и владельца
                      платформы здесь не изменяются.
                    </p>

                    <p v-if="editError"
                       class="admin-role-editor__error"
                       role="alert">
                      {{ editError }}
                    </p>

                    <div class="admin-role-editor__actions">
                      <button class="btn btn-primary"
                              type="button"
                              :disabled="
                          isSavingRoles ||
                          !hasRoleChanges(user)
                        "
                              @click="saveRoles(user)">
                        {{
                          isSavingRoles
                            ? 'Сохранение...'
                            : 'Сохранить'
                        }}
                      </button>

                      <button class="btn btn-outline"
                              type="button"
                              :disabled="isSavingRoles"
                              @click="cancelEditing">
                        Отмена
                      </button>
                    </div>
                  </div>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>

      <div v-else
           class="admin-page__message">
        <p>
          {{
            searchQuery.trim()
              ? 'Пользователи по вашему запросу не найдены.'
              : 'Пользователей пока нет.'
          }}
        </p>

        <button v-if="searchQuery.trim()"
                class="btn btn-outline"
                type="button"
                @click="searchQuery = ''">
          Сбросить поиск
        </button>
      </div>

      <p v-if="filteredUsers.length > 0"
         class="admin-page__results">
        Показано пользователей: {{ filteredUsers.length }}
      </p>
    </template>
  </section>
</template>
