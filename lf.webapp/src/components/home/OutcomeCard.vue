<script setup>
import { computed } from 'vue';
import { Target, Workflow, FileSearch, Handshake } from 'lucide-vue-next';

const ICONS = { planning: Target, flow: Workflow, analysis: FileSearch, leadership: Handshake };

const props = defineProps({
  index: { type: String, required: true },
  icon: {
    type: String,
    required: true,
    validator: (value) => ['planning', 'flow', 'analysis', 'leadership'].includes(value),
  },
  title: { type: String, required: true },
  description: { type: String, required: true },
});

const iconComponent = computed(() => ICONS[props.icon]);
</script>

<template>
  <article class="outcome-card">
    <div class="outcome-card__top">
      <span
        class="outcome-card__icon"
        aria-hidden="true"
      >
        <component
          :is="iconComponent"
          :size="20"
        />
      </span>
      <span class="outcome-card__index mono-label">{{ index }}</span>
    </div>

    <h3 class="outcome-card__title">
      {{ title }}
    </h3>
    <p class="outcome-card__text">
      {{ description }}
    </p>
  </article>
</template>

<style scoped>
.outcome-card {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  height: 100%;
  padding: 1.6rem;
  background: var(--color-card);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  transition: border-color 0.15s ease, transform 0.15s ease;
}

.outcome-card:hover {
  border-color: color-mix(in srgb, var(--color-accent-coral) 45%, var(--color-border-subtle));
  transform: translateY(-2px);
}

.outcome-card__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.outcome-card__icon {
  display: grid;
  place-items: center;
  width: 2.75rem;
  height: 2.75rem;
  border-radius: var(--radius-md);
  color: var(--color-accent-coral);
  background: var(--color-accent-soft);
}

.outcome-card__index {
  color: var(--color-ink-faint);
}

.outcome-card__title {
  margin: 0;
  color: var(--color-ink);
  font-family: var(--font-sans);
  font-size: 1.05rem;
  font-weight: 600;
  line-height: 1.35;
}

.outcome-card__text {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  line-height: 1.6;
}
</style>
