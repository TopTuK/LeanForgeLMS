<script setup>
defineProps({
  icon: {
    type: String,
    required: true,
    validator: (value) => ['llm', 'kanban'].includes(value),
  },
  index: { type: String, default: '' },
  title: { type: String, required: true },
  description: { type: String, required: true },
  duration: { type: String, required: true },
  category: { type: String, required: true },
  cta: { type: String, default: '' },
  to: { type: [String, Object], default: null },
});
</script>

<template>
  <article class="program-card">
    <div class="program-card__head">
      <span
        class="program-card__icon"
        aria-hidden="true"
      >
        <svg
          v-if="icon === 'llm'"
          width="22"
          height="22"
          viewBox="0 0 24 24"
          fill="none"
        >
          <rect
            x="7"
            y="7"
            width="10"
            height="10"
            rx="2"
            stroke="currentColor"
            stroke-width="1.5"
          />
          <circle
            cx="12"
            cy="12"
            r="2"
            stroke="currentColor"
            stroke-width="1.5"
          />
          <path
            d="M12 3v4M12 17v4M3 12h4M17 12h4M5.6 5.6l2.8 2.8M15.6 15.6l2.8 2.8M18.4 5.6l-2.8 2.8M8.4 15.6l-2.8 2.8"
            stroke="currentColor"
            stroke-width="1.5"
            stroke-linecap="round"
          />
        </svg>
        <svg
          v-else-if="icon === 'kanban'"
          width="22"
          height="22"
          viewBox="0 0 24 24"
          fill="none"
        >
          <rect
            x="3"
            y="4"
            width="18"
            height="16"
            rx="1.5"
            stroke="currentColor"
            stroke-width="1.5"
          />
          <path
            d="M9 4v16M15 4v16"
            stroke="currentColor"
            stroke-width="1.5"
          />
          <rect
            x="5"
            y="7"
            width="2"
            height="5"
            rx="0.5"
            fill="currentColor"
          />
          <rect
            x="11"
            y="7"
            width="2"
            height="8"
            rx="0.5"
            fill="currentColor"
          />
          <rect
            x="17"
            y="7"
            width="2"
            height="3"
            rx="0.5"
            fill="currentColor"
          />
        </svg>
      </span>

      <span
        v-if="index"
        class="program-card__index mono-label"
      >{{ index }}</span>
    </div>

    <div class="program-card__meta">
      <span class="mono-label program-card__tag">{{ category }}</span>
      <span
        class="program-card__dot"
        aria-hidden="true"
      >·</span>
      <span class="mono-label program-card__tag program-card__tag--muted">{{ duration }}</span>
    </div>

    <h3 class="program-card__title">
      {{ title }}
    </h3>
    <p class="program-card__text">
      {{ description }}
    </p>

    <router-link
      v-if="cta && to"
      :to="to"
      class="program-card__cta"
    >
      {{ cta }}
      <span aria-hidden="true">→</span>
    </router-link>
  </article>
</template>

<style scoped>
.program-card {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  height: 100%;
  padding: 1.75rem;
  background: var(--color-card);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  transition: border-color 0.15s ease, transform 0.15s ease;
}

.program-card::before,
.program-card::after {
  content: "";
  position: absolute;
  width: 10px;
  height: 10px;
  border-color: var(--color-accent-coral);
  border-style: solid;
  opacity: 0;
  transition: opacity 0.15s ease;
}

.program-card::before {
  top: -1px;
  left: -1px;
  border-width: 1px 0 0 1px;
}

.program-card::after {
  bottom: -1px;
  right: -1px;
  border-width: 0 1px 1px 0;
}

.program-card:hover {
  border-color: color-mix(in srgb, var(--color-accent-coral) 40%, var(--color-border-subtle));
  transform: translateY(-3px);
}

.program-card:hover::before,
.program-card:hover::after {
  opacity: 1;
}

.program-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.program-card__icon {
  display: grid;
  place-items: center;
  width: 2.75rem;
  height: 2.75rem;
  border-radius: var(--radius-md);
  color: var(--color-accent-coral);
  background: var(--color-accent-soft);
}

.program-card__index {
  color: var(--color-ink-faint);
}

.program-card__meta {
  display: flex;
  align-items: center;
  gap: 0.55rem;
}

.program-card__tag {
  color: var(--color-accent-coral);
}

.program-card__tag--muted {
  color: var(--color-ink-faint);
}

.program-card__dot {
  color: var(--color-ink-faint);
}

.program-card__title {
  margin: 0;
  color: var(--color-ink);
  font-family: var(--font-sans);
  font-size: 1.15rem;
  font-weight: 600;
  line-height: 1.35;
}

.program-card__text {
  margin: 0;
  flex: 1;
  color: var(--color-ink-muted);
  font-size: 0.92rem;
  line-height: 1.65;
}

.program-card__cta {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  width: fit-content;
  color: var(--color-accent-coral);
  font-size: 0.9rem;
  font-weight: 600;
  border-bottom: 1px solid color-mix(in srgb, var(--color-accent-coral) 40%, transparent);
  transition: color 0.15s ease, border-color 0.15s ease;
}

.program-card__cta:hover {
  color: var(--color-accent-coral-dark);
  border-color: var(--color-accent-coral-dark);
}
</style>
