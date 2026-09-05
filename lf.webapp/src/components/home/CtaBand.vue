<script setup>
import { useId } from 'vue';

const props = defineProps({
  eyebrow: { type: String, default: '' },
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
  headingId: { type: String, default: null },
});

const fallbackId = `cta-band-${useId()}`;
const headingId = props.headingId ?? fallbackId;
</script>

<template>
  <div
    class="cta-band"
    role="group"
    :aria-labelledby="headingId"
  >
    <span
      class="blueprint-grid blueprint-grid--band blueprint-grid--fade"
      aria-hidden="true"
    />

    <div
      class="cta-band__inner layout-max"
      :class="{ 'cta-band__inner--with-media': $slots.media }"
    >
      <div class="cta-band__content">
        <p
          v-if="eyebrow"
          class="mono-label cta-band__eyebrow"
        >
          {{ eyebrow }}
        </p>
        <h2
          :id="headingId"
          class="cta-band__title font-display"
        >
          {{ title }}
        </h2>
        <p
          v-if="subtitle"
          class="cta-band__subtitle"
        >
          {{ subtitle }}
        </p>

        <div class="cta-band__actions">
          <slot />
        </div>
      </div>

      <div
        v-if="$slots.media"
        class="cta-band__media"
      >
        <slot name="media" />
      </div>
    </div>
  </div>
</template>

<style scoped>
.cta-band {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  background:
    radial-gradient(ellipse 80% 70% at 85% -20%, var(--industrial-accent-wash), transparent 70%),
    var(--band-bg);
  color: var(--band-ink);
  border: 1px solid var(--band-line);
  border-radius: var(--radius-card);
  padding: clamp(2.5rem, 6vw, 4.5rem) clamp(1.5rem, 5vw, 4rem);
}

.cta-band__inner {
  position: relative;
  z-index: 1;
  max-width: 40rem;
}

.cta-band__inner--with-media {
  max-width: none;
}

.cta-band__content {
  max-width: 40rem;
}

.cta-band__media {
  min-width: 0;
  overflow: hidden;
  border: 1px solid var(--band-line);
  border-radius: var(--radius-md);
}

.cta-band__eyebrow {
  color: var(--band-accent);
  margin: 0 0 1rem;
}

.cta-band__title {
  margin: 0;
  font-size: clamp(1.9rem, 4.5vw, 3rem);
  font-weight: 600;
  letter-spacing: -0.035em;
  line-height: 1.08;
}

.cta-band__subtitle {
  margin: 1rem 0 0;
  color: var(--band-ink-muted);
  font-size: 1.02rem;
  line-height: 1.65;
}

.cta-band__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 1rem 1.5rem;
  margin-top: 2rem;
}

@media (min-width: 900px) {
  .cta-band__inner--with-media {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(18rem, 0.8fr);
    gap: clamp(2rem, 5vw, 4rem);
    align-items: center;
  }
}

@media (max-width: 899px) {
  .cta-band__media {
    margin-top: 2rem;
  }
}
</style>
