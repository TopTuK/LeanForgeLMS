<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ArrowLeft } from 'lucide-vue-next';

const { tm, rt } = useI18n();

const sections = computed(() => {
  const items = tm('offer.sections');
  return Array.isArray(items) ? items : [];
});
</script>

<template>
  <article class="offer-page">
    <span
      class="blueprint-grid blueprint-grid--band blueprint-grid--fade"
      aria-hidden="true"
    />

    <div class="offer-page__inner layout-max">
      <router-link
        :to="{ name: 'Home' }"
        class="offer-page__back"
      >
        <ArrowLeft
          class="size-4"
          aria-hidden="true"
        />
        {{ $t('offer.back') }}
      </router-link>

      <p class="mono-label offer-page__eyebrow">
        {{ $t('offer.eyebrow') }}
      </p>
      <h1 class="offer-page__title font-display">
        {{ $t('offer.title') }}
      </h1>
      <p class="offer-page__meta">
        {{ $t('offer.updated') }}
      </p>
      <p class="offer-page__binding">
        {{ $t('offer.binding') }}
      </p>

      <p class="offer-page__preamble">
        {{ $t('offer.preamble') }}
      </p>
      <p class="offer-page__preamble">
        {{ $t('offer.requisites') }}
      </p>

      <section
        v-for="(section, index) in sections"
        :key="section.heading || index"
        class="offer-section"
      >
        <h2 class="offer-section__heading font-display">
          {{ rt(section.heading) }}
        </h2>
        <template
          v-for="(block, blockIndex) in section.blocks"
          :key="blockIndex"
        >
          <p
            v-if="block.type === 'p'"
            class="offer-section__p"
          >
            {{ rt(block.text) }}
          </p>
          <ul
            v-else-if="block.type === 'ul'"
            class="offer-section__list"
          >
            <li
              v-for="item in block.items"
              :key="item"
            >
              {{ rt(item) }}
            </li>
          </ul>
        </template>
      </section>
    </div>
  </article>
</template>

<style scoped>
.offer-page {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  min-height: calc(100vh - var(--header-height));
  padding: clamp(2.5rem, 6vw, 4.5rem) 1.5rem clamp(4rem, 8vw, 6rem);
  background:
    radial-gradient(ellipse 60% 50% at 12% 8%, var(--industrial-accent-wash), transparent 68%),
    var(--band-bg);
  color: var(--band-ink);
}

.offer-page__inner {
  position: relative;
  z-index: 1;
  max-width: 42rem;
}

.offer-page__back {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 500;
  transition: color 0.15s ease;
}

.offer-page__back:hover {
  color: var(--color-accent-coral);
}

.offer-page__eyebrow {
  margin: 2rem 0 1rem;
  color: var(--color-accent-coral);
}

.offer-page__title {
  margin: 0;
  font-size: clamp(2rem, 5vw, 2.9rem);
  font-weight: 600;
  letter-spacing: -0.03em;
  line-height: 1.1;
  color: var(--color-ink);
}

.offer-page__meta,
.offer-page__binding {
  margin: 0.75rem 0 0;
  color: var(--color-ink-faint);
  font-size: 0.85rem;
  line-height: 1.5;
}

.offer-page__preamble {
  margin: 1.25rem 0 0;
  color: var(--color-ink);
  font-size: 1rem;
  line-height: 1.7;
}

.offer-section {
  margin-top: 2.25rem;
}

.offer-section__heading {
  margin: 0 0 0.85rem;
  font-size: 1.2rem;
  font-weight: 600;
  letter-spacing: -0.02em;
  color: var(--color-ink);
}

.offer-section__p {
  margin: 0 0 0.75rem;
  color: var(--color-ink);
  font-size: 0.95rem;
  line-height: 1.7;
}

.offer-section__list {
  margin: 0 0 0.85rem;
  padding-left: 1.25rem;
  color: var(--color-ink);
  font-size: 0.95rem;
  line-height: 1.7;
}

.offer-section__list li + li {
  margin-top: 0.35rem;
}
</style>
