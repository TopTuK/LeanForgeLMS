<script setup>
defineProps({
  step: { type: String, required: true },
  title: { type: String, required: true },
  description: { type: String, required: true },
  last: { type: Boolean, default: false },
});
</script>

<template>
  <li
    class="process-step"
    :class="last ? 'process-step--last' : ''"
  >
    <span
      class="process-step__rail"
      aria-hidden="true"
    >
      <span class="process-step__node">{{ step }}</span>
    </span>

    <div class="process-step__body">
      <h3 class="process-step__title">
        {{ title }}
      </h3>
      <p class="process-step__text">
        {{ description }}
      </p>
    </div>
  </li>
</template>

<style scoped>
.process-step {
  display: grid;
  grid-template-columns: 2.75rem 1fr;
  gap: 1.25rem;
  padding-bottom: 2.25rem;
}

.process-step--last {
  padding-bottom: 0;
}

.process-step__rail {
  position: relative;
  display: flex;
  justify-content: center;
}

.process-step__node {
  z-index: 1;
  display: grid;
  place-items: center;
  width: 2.75rem;
  height: 2.75rem;
  border: 1px solid var(--industrial-line-strong);
  border-radius: var(--radius-md);
  background: var(--industrial-accent-wash);
  color: var(--color-accent-coral);
  font-family: var(--font-mono);
  font-size: 0.85rem;
  font-weight: 500;
}

.process-step__rail::after {
  content: "";
  position: absolute;
  top: 2.75rem;
  bottom: -2.25rem;
  width: 1px;
  background: var(--color-border-subtle);
}

.process-step--last .process-step__rail::after {
  display: none;
}

.process-step__body {
  padding-top: 0.35rem;
}

.process-step__title {
  margin: 0;
  color: var(--color-ink);
  font-family: var(--font-sans);
  font-size: 1.1rem;
  font-weight: 600;
}

.process-step__text {
  margin: 0.5rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
  line-height: 1.65;
  max-width: 34rem;
}
</style>
