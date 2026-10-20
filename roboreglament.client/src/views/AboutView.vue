
<script setup lang="ts">
const steps = [
  {
    number: '01',
    title: 'Создать турнир',
    description: 'Организатор задаёт сроки и условия участия',
  },
  {
    number: '02',
    title: 'Заявить команду',
    description: 'Тренер регистрирует команду и текущий состав',
  },
  {
    number: '03',
    title: 'Проверить заявку',
    description: 'Организатор принимает решение о допуске',
  },
  {
    number: '04',
    title: 'Аккредитовать',
    description: 'Окончательный состав сохраняется для истории',
  },
]
</script>

<template>
  <div class="about-page">
    <section class="about-intro">
      <h1>О платформе</h1>
      <p>
        РобоРегламент — инструмент для организации
        робототехнических соревнований
      </p>
    </section>

    <section class="about-hero">
      <div class="about-hero__content">
        <span class="about-hero__label">
          РОБОТЫ · КОМАНДЫ · ТУРНИРЫ
        </span>

        <h2>
          Соревнования —<br />
          по понятным правилам
        </h2>

        <p>
          Единое пространство для организаторов, тренеров
          и команд: от регистрации до подтверждения участия.
        </p>
      </div>

      <div class="about-hero__illustration" aria-hidden="true">
        <span class="about-hero__symbol">✦</span>
        <span class="about-hero__brand">РОБО</span>
      </div>
    </section>

    <section class="about-workflow">
      <div class="about-workflow__heading">
        <h2>Как работает система</h2>
        <p>
          Все основные действия собраны в последовательном процессе.
        </p>
      </div>

      <div class="about-workflow__grid">
        <article v-for="step in steps"
                 :key="step.number"
                 class="about-step">
          <span class="about-step__number">
            {{ step.number }}
          </span>

          <h3>{{ step.title }}</h3>
          <p>{{ step.description }}</p>
        </article>
      </div>
    </section>
  </div>
</template>
