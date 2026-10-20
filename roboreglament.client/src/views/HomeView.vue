

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

import { getTournaments } from '@/api/tournaments'
import TournamentCard from '@/components/tournaments/TournamentCard.vue'
import type { Tournament } from '@/types/tournament'

const tournaments = ref<Tournament[]>([])
const isLoading = ref(true)
const error = ref('')

const controller = new AbortController()

function getToday(): string {
  const today = new Date()

  const year = today.getFullYear()
  const month = String(today.getMonth() + 1).padStart(2, '0')
  const day = String(today.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

const upcomingTournaments = computed(() => {
  const today = getToday()

  return tournaments.value
    .filter(tournament =>
      tournament.endDate >= today &&
      tournament.status !== 'Draft'
    )
    .sort((a, b) =>
      a.startDate.localeCompare(b.startDate)
    )
    .slice(0, 2)
})

async function loadTournaments() {
  isLoading.value = true
  error.value = ''

  try {
    tournaments.value = await getTournaments(controller.signal)
  } catch (err) {
    if (!controller.signal.aborted) {
      error.value = 'Не удалось загрузить ближайшие турниры.'
      console.error(err)
    }
  } finally {
    if (!controller.signal.aborted) {
      isLoading.value = false
    }
  }
}

onMounted(() => {
  void loadTournaments()
})

onUnmounted(() => {
  controller.abort()
})
</script>


<template>
  <div class="home-page">
    <section class="home-hero">
      <div class="home-hero__content">
        <span class="eyebrow">ЕДИНАЯ СИСТЕМА СОРЕВНОВАНИЙ</span>

        <h1>
          РобоРегламент — пространство для соревнований
        </h1>

        <p>
          Создавайте турниры, заявляйте команды и следите
          за участием — всё необходимое в одном месте.
        </p>

        <div class="home-hero__actions">
          <RouterLink to="/tournaments" class="btn btn-primary">
            Перейти к турнирам →
          </RouterLink>

          <RouterLink to="/about" class="btn btn-outline">
            О платформе
          </RouterLink>
        </div>
      </div>

      <div class="home-hero__steps">
        <h2>Путь команды</h2>

        <div class="step">
          <span>01</span>
          <div>
            <strong>Выбрать соревнование</strong>
            <p>Посмотреть условия и сроки</p>
          </div>
        </div>

        <div class="step">
          <span>02</span>
          <div>
            <strong>Подать заявку</strong>
            <p>Заявить команду и её состав</p>
          </div>
        </div>

        <div class="step">
          <span>03</span>
          <div>
            <strong>Пройти аккредитацию</strong>
            <p>Подтвердить участие</p>
          </div>
        </div>
      </div>
    </section>

    <section class="home-navigation">
      <h2>Быстрый доступ</h2>

      <div class="navigation-grid">
        <RouterLink to="/tournaments" class="navigation-card">
          <span>01</span>
          <h3>Турниры</h3>
          <p>Соревнования, даты и условия участия</p>
        </RouterLink>

        <RouterLink to="/login" class="navigation-card">
          <span>02</span>
          <h3>Мои команды</h3>
          <p>Участники и составы команд</p>
        </RouterLink>

        <RouterLink to="/login" class="navigation-card">
          <span>03</span>
          <h3>Организаторам</h3>
          <p>Создание турниров и работа с заявками</p>
        </RouterLink>

        <RouterLink to="/regulations" class="navigation-card">
          <span>04</span>
          <h3>Регламент</h3>
          <p>Правила проведения соревнований</p>
        </RouterLink>
      </div>
    </section>

    <section class="home-tournaments">
      <div class="home-tournaments__header">
        <div>
          <h2>Ближайшие турниры</h2>
          <p>Актуальные соревнования и сроки регистрации</p>
        </div>

        <RouterLink to="/tournaments" class="home-tournaments__link">
          Все турниры →
        </RouterLink>
      </div>

      <div v-if="isLoading" class="home-tournaments__message">
        Загружаем турниры...
      </div>

      <div v-else-if="error" class="home-tournaments__message" role="alert">
        {{ error }}
      </div>

      <div v-else-if="upcomingTournaments.length > 0"
           class="home-tournaments__grid">
        <TournamentCard v-for="tournament in upcomingTournaments"
                        :key="tournament.id"
                        :tournament="tournament" />
      </div>

      <div v-else class="home-tournaments__message">
        Пока нет предстоящих турниров.
      </div>
    </section>

  </div>
</template>
