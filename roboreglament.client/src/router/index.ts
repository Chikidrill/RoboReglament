
import { createRouter, createWebHistory } from 'vue-router'

import PublicLayout from '@/layouts/PublicLayout.vue'

import HomeView from '@/views/HomeView.vue'
import TournamentsView from '@/views/TournamentsView.vue'
import TournamentDetailsView from '@/views/TournamentDetailsView.vue'
import PlaceholderView from '@/views/PlaceholderView.vue'
import AboutView from '@/views/AboutView.vue'
import LoginView from '@/views/LoginView.vue'
import RegisterView from '@/views/RegisterView.vue'

import AccountView from '@/views/AccountView.vue'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),

  routes: [
    {
      path: '/',
      component: PublicLayout,

      children: [
        {
          path: '',
          name: 'home',
          component: HomeView,
        },
        {
          path: 'tournaments',
          name: 'tournaments',
          component: TournamentsView,
        },
        {
          path: 'tournaments/:id',
          name: 'tournament-details',
          component: TournamentDetailsView,
        },
        {
          path: 'about',
          name: 'about',
          component: AboutView,
          props: {
            title: 'О платформе',
            description: 'Информация о системе РобоРегламент.',
          },
        },
        {
          path: 'regulations',
          name: 'regulations',
          component: PlaceholderView,
          props: {
            title: 'Регламент',
            description: 'Правила проведения соревнований.',
          },
        },
        {
          path: 'login',
          name: 'login',
          component: LoginView,
        },
        {
          path: 'register',
          name: 'register',
          component: RegisterView,
        },
        {
          path: 'account',
          name: 'account',
          component: AccountView,
          meta: {
            requiresAuth: true,
          },
        },
        {
          path: 'forbidden',
          name: 'forbidden',
          component: PlaceholderView,
          props: {
            title: 'Доступ запрещён',
            description: 'У вашей учётной записи нет прав для просмотра этой страницы.',
          },
        },
        {
          path: 'admin',
          name: 'admin',
          component: PlaceholderView,
          props: {
            title: 'Кабинет администратора',
            description: 'Раздел управления пользователями и ролями.',
          },
          meta: {
            requiresAuth: true,
            roles: ['Administrator'],
          },
        },
      ],
    },
  ],
})



router.beforeEach(async (to) => {
  const authStore = useAuthStore()

  if (!authStore.isInitialized) {
    await authStore.initialize()
  }

  const requiredRoles = to.meta.roles as string[] | undefined

  const requiresAuth =
    to.meta.requiresAuth || (requiredRoles?.length ?? 0) > 0

  // Неавторизованный пользователь
  if (requiresAuth && !authStore.isAuthenticated) {
    return {
      name: 'login',
      query: {
        redirect: to.fullPath,
      },
    }
  }

  // Авторизованному пользователю не нужны формы входа
  if (
    authStore.isAuthenticated &&
    (to.name === 'login' || to.name === 'register')
  ) {
    return { name: 'account' }
  }

  // Проверка ролей
  if (requiredRoles && requiredRoles.length > 0) {
    const userRoles = authStore.user?.roles ?? []

    const hasAccess = requiredRoles.some(role =>
      userRoles.includes(role)
    )

    if (!hasAccess) {
      return { name: 'forbidden' }
    }
  }
})




export default router
