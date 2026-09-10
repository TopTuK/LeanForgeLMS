<script setup>
import { computed } from 'vue';
import { BookOpen, BookmarkCheck, Briefcase } from 'lucide-vue-next';

const ICONS = { lessons: BookOpen, progress: BookmarkCheck, practice: Briefcase };

const props = defineProps({
  index: { type: String, required: true },
  icon: {
    type: String,
    required: true,
    validator: (value) => ['lessons', 'progress', 'practice'].includes(value),
  },
  title: { type: String, required: true },
  description: { type: String, required: true },
});

const iconComponent = computed(() => ICONS[props.icon]);
</script>

<template>
  <li class="process-step">
    <div class="process-step__top">
      <span
        class="process-step__icon"
        aria-hidden="true"
      >
        <component
          :is="iconComponent"
          :size="20"
        />
      </span>
      <span class="process-step__index mono-label">{{ index }}</span>
    </div>

    <h3 class="process-step__title">
      {{ title }}
    </h3>
    <p class="process-step__text">
      {{ description }}
    </p>
  </li>
</template>

<style scoped>
.process-step {
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

.process-step:hover {
  border-color: color-mix(in srgb, var(--color-accent-coral) 45%, var(--color-border-subtle));
  transform: translateY(-2px);
}

.process-step__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.process-step__icon {
  display: grid;
  place-items: center;
  width: 2.75rem;
  height: 2.75rem;
  border-radius: var(--radius-md);
  color: var(--color-accent-coral);
  background: var(--color-accent-soft);
}

.process-step__index {
  color: var(--color-ink-faint);
}

.process-step__title {
  margin: 0;
  color: var(--color-ink);
  font-family: var(--font-sans);
  font-size: 1.05rem;
  font-weight: 600;
  line-height: 1.35;
}

.process-step__text {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  line-height: 1.6;
}
</style>
