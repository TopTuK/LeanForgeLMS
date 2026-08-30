<script setup>
import { useId } from 'vue';

defineProps({
  index: { type: String, default: '' },
  eyebrow: { type: String, default: '' },
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
  headingId: { type: String, default: null },
  tone: {
    type: String,
    default: 'default',
    validator: (value) => ['default', 'band'].includes(value),
  },
});

const fallbackId = `section-heading-${useId()}`;
</script>

<template>
  <div
    class="section-heading"
    :class="tone === 'band' ? 'section-heading--band' : ''"
  >
    <p
      v-if="index || eyebrow"
      class="section-heading__meta"
    >
      <span
        v-if="index"
        class="catalog-section-index section-heading__index"
      >{{ index }}</span>
      <span
        v-if="eyebrow"
        class="mono-label section-heading__eyebrow"
      >{{ eyebrow }}</span>
    </p>

    <h2
      :id="headingId ?? fallbackId"
      class="section-heading__title font-display"
    >
      {{ title }}
    </h2>

    <p
      v-if="subtitle"
      class="section-heading__subtitle"
    >
      {{ subtitle }}
    </p>
  </div>
</template>

<style scoped>
.section-heading {
  max-width: 44rem;
}

.section-heading__meta {
  display: flex;
  align-items: center;
  gap: 0.9rem;
  margin: 0 0 1.1rem;
}

.section-heading__eyebrow {
  color: var(--color-accent-coral);
}

.section-heading__title {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.9rem, 4vw, 2.9rem);
  font-weight: 600;
  letter-spacing: -0.035em;
  line-height: 1.1;
}

.section-heading__subtitle {
  margin: 1rem 0 0;
  color: var(--color-ink-muted);
  font-size: 1.02rem;
  line-height: 1.65;
}

.section-heading--band .section-heading__title {
  color: var(--band-ink);
}

.section-heading--band .section-heading__subtitle {
  color: var(--band-ink-muted);
}

.section-heading--band .section-heading__index {
  color: var(--band-accent);
  border-color: var(--band-line);
  background: var(--band-panel);
}

.section-heading--band .section-heading__eyebrow {
  color: var(--band-accent);
}
</style>
