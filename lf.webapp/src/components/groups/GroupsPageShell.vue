<script setup>
import GeometricBackdrop from '@/components/layout/GeometricBackdrop.vue';

defineProps({
  eyebrow: { type: String, default: '' },
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
  wide: { type: Boolean, default: false },
});
</script>

<template>
  <section class="lf-page">
    <GeometricBackdrop dense />

    <div
      class="lf-page__inner"
      :class="{ 'lf-page__inner--wide': wide }"
    >
      <slot name="before-heading" />

      <div class="lf-page__heading">
        <div>
          <p
            v-if="eyebrow"
            class="mono-label lf-page__eyebrow"
          >
            {{ eyebrow }}
          </p>
          <h1 class="lf-page__title font-display">
            {{ title }}
          </h1>
          <p
            v-if="subtitle"
            class="lf-page__subtitle"
          >
            {{ subtitle }}
          </p>
        </div>
        <div
          v-if="$slots.actions"
          class="lf-page__actions"
        >
          <slot name="actions" />
        </div>
      </div>

      <slot />
    </div>
  </section>
</template>

<!-- Not scoped: the hint/error/card classes are used by the slot content of the pages built on this shell. -->
<style>
.lf-page {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  padding: clamp(2.5rem, 6vw, 4rem) 1.5rem clamp(4rem, 8vw, 6rem);
}

.lf-page__inner {
  position: relative;
  z-index: 1;
  max-width: 48rem;
  margin-inline: auto;
}

.lf-page__inner--wide {
  max-width: 64rem;
}

.lf-page__heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.lf-page__eyebrow {
  margin: 0 0 1rem;
  color: var(--color-accent-coral);
}

.lf-page__title {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.9rem, 4vw, 2.5rem);
  font-weight: 600;
  letter-spacing: -0.03em;
  line-height: 1.1;
}

.lf-page__subtitle {
  margin: 0.85rem 0 0;
  max-width: 40rem;
  color: var(--color-ink-muted);
  line-height: 1.6;
}

.lf-page__actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.lf-page__hint {
  margin: 2rem 0 0;
  color: var(--color-ink-muted);
}

.lf-page__error {
  margin: 1.5rem 0 0;
  padding: 0.6rem 0.85rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.5rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
  font-size: 0.9rem;
  font-weight: 600;
}

.lf-page__card {
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-card);
  padding: clamp(1rem, 3vw, 1.35rem);
}

.lf-page__list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin: 2rem 0 0;
  padding: 0;
  list-style: none;
}

.lf-page__meta {
  font-size: 0.85rem;
  color: var(--color-ink-muted);
}

.lf-page__field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin-top: 0.9rem;
}

.lf-page__field label,
.lf-page__field legend {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-ink);
}

.lf-page__segmented {
  display: inline-flex;
  gap: 0.25rem;
  margin-top: 2rem;
  padding: 0.25rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 999px;
  background: var(--color-card);
}

.lf-page__segmented button {
  padding: 0.35rem 0.9rem;
  border-radius: 999px;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-ink-muted);
}

.lf-page__segmented button.is-active {
  background: var(--color-ink);
  color: var(--color-surface-950);
}
</style>
