
import { createRouter, createWebHistory } from 'vue-router'

import PublicLayout from '@/layouts/PublicLayout.vue'

import HomeView from '@/views/HomeView.vue'
import TournamentsView from '@/views/TournamentsView.vue'
import TournamentDetailsView from '@/views/TournamentDetailsView.vue'
import PlaceholderView from '@/views/PlaceholderView.vue'
import AboutView from '@/views/AboutView.vue'

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
          component: PlaceholderView,
          props: {
            title: 'Вход в систему',
            description: 'Здесь будет форма авторизации.',
          },
        },
        {
          path: 'register',
          name: 'register',
          component: PlaceholderView,
          props: {
            title: 'Регистрация',
            description: 'Здесь будет форма создания аккаунта.',
          },
        },
      ],
    },
  ],
})

export default router
