
<script setup lang="ts">
import { RouterLink } from 'vue-router'

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
      <RouterLink to="/" class="site-header__brand">
        <span class="site-header__logo">✦</span>
        <span>РобоРегламент</span>
      </RouterLink>

      <nav class="site-header__nav" aria-label="Основная навигация">
        <RouterLink v-for="item in navigation"
                    :key="item.to"
                    :to="item.to"
                    class="site-header__nav-link"
                    exact-active-class="is-active">
          {{ item.label }}
        </RouterLink>
      </nav>

      <div class="site-header__actions">
        <RouterLink to="/login" class="btn btn-outline">
          Войти
        </RouterLink>

        <RouterLink to="/register" class="btn btn-primary">
          Регистрация
        </RouterLink>
      </div>
    </div>
  </header>
</template>
