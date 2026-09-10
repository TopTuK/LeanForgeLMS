<script setup>
import MarkdownIt from 'markdown-it';
import { ArrowLeft } from 'lucide-vue-next';

import offerSource from '@/content/offer.ru.md?raw';
import { sanitizeHtml } from '@/lib/sanitizeHtml';

// The offer is a Russian-only legal document. It is authored in
// src/content/offer.ru.md and rendered here — it is not part of the i18n bundle.
const md = new MarkdownIt({ html: false, linkify: true, typographer: true });
const offerHtml = sanitizeHtml(md.render(offerSource));
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
        На главную
      </router-link>

      <p class="mono-label offer-page__eyebrow">
        Правовая информация
      </p>

      <div
        v-safe-html="offerHtml"
        class="offer-doc"
      />
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
  margin: 2rem 0 1.5rem;
  color: var(--color-accent-coral);
}

.offer-doc :deep(h1) {
  margin: 0 0 1.5rem;
  font-family: var(--font-display, inherit);
  font-size: clamp(1.9rem, 4.5vw, 2.6rem);
  font-weight: 600;
  letter-spacing: -0.03em;
  line-height: 1.15;
  color: var(--color-ink);
}

.offer-doc :deep(h2) {
  margin: 2.25rem 0 0.85rem;
  font-family: var(--font-display, inherit);
  font-size: 1.2rem;
  font-weight: 600;
  letter-spacing: -0.02em;
  color: var(--color-ink);
}

.offer-doc :deep(h3) {
  margin: 1.5rem 0 0.6rem;
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-ink);
}

.offer-doc :deep(p) {
  margin: 0 0 0.75rem;
  color: var(--color-ink);
  font-size: 0.95rem;
  line-height: 1.7;
}

.offer-doc :deep(ul) {
  margin: 0 0 0.85rem;
  padding-left: 1.25rem;
  color: var(--color-ink);
  font-size: 0.95rem;
  line-height: 1.7;
}

.offer-doc :deep(li + li) {
  margin-top: 0.35rem;
}

.offer-doc :deep(a) {
  color: var(--color-accent-coral);
  overflow-wrap: anywhere;
}
</style>
