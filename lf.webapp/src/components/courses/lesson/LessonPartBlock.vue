<script setup>
import { useI18n } from 'vue-i18n';
import LessonPartToolbox from './LessonPartToolbox.vue';

defineProps({
  index: { type: Number, required: true },
  total: { type: Number, required: true },
  toolboxOpen: { type: Boolean, default: false },
  insertIndex: { type: Number, default: 0 },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['add', 'select-type', 'move-up', 'move-down', 'remove']);

const { t } = useI18n();
</script>

<template>
  <article
    class="part-block"
    :class="{ 'part-block--toolbox-open': toolboxOpen }"
  >
    <div class="part-block__gutter">
      <button
        type="button"
        class="part-block__plus"
        :disabled="disabled"
        :title="t('courses.lessonEditor.parts.add_after')"
        :aria-label="t('courses.lessonEditor.parts.add_after')"
        :aria-expanded="toolboxOpen"
        @click="emit('add')"
      >
        +
      </button>
      <button
        type="button"
        class="part-block__icon"
        :disabled="disabled || index === 0"
        :title="t('courses.lessonEditor.parts.move_up')"
        :aria-label="t('courses.lessonEditor.parts.move_up')"
        @click="emit('move-up')"
      >
        ↑
      </button>
      <button
        type="button"
        class="part-block__icon"
        :disabled="disabled || index === total - 1"
        :title="t('courses.lessonEditor.parts.move_down')"
        :aria-label="t('courses.lessonEditor.parts.move_down')"
        @click="emit('move-down')"
      >
        ↓
      </button>
      <button
        type="button"
        class="part-block__icon part-block__icon--danger"
        :disabled="disabled"
        :title="t('courses.lessonEditor.parts.delete')"
        :aria-label="t('courses.lessonEditor.parts.delete')"
        @click="emit('remove')"
      >
        ×
      </button>
    </div>

    <div class="part-block__main">
      <div
        v-if="toolboxOpen"
        class="part-block__toolbox"
      >
        <LessonPartToolbox
          :insert-index="insertIndex"
          @select="emit('select-type', $event)"
        />
      </div>
      <div class="part-block__body">
        <slot />
      </div>
    </div>
  </article>
</template>

<style scoped>
.part-block {
  display: grid;
  grid-template-columns: 2.4rem minmax(0, 1fr);
  gap: 0.65rem;
  align-items: start;
}

.part-block__gutter {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.2rem;
  padding-top: 0.35rem;
}

@media (hover: hover) {
  .part-block__gutter {
    opacity: 0;
    transition: opacity 0.15s ease;
  }

  .part-block:hover .part-block__gutter,
  .part-block:focus-within .part-block__gutter,
  .part-block--toolbox-open .part-block__gutter {
    opacity: 1;
  }
}

.part-block__plus,
.part-block__icon {
  width: 1.7rem;
  height: 1.7rem;
  padding: 0;
  border: 1px solid var(--industrial-line);
  border-radius: 0.25rem;
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
  font-size: 0.95rem;
  line-height: 1;
  cursor: pointer;
}

.part-block__plus {
  font-weight: 700;
  color: var(--color-ink);
}

.part-block__plus:hover,
.part-block__icon:hover:not(:disabled) {
  border-color: var(--color-accent-coral);
  color: var(--color-accent-coral-dark);
}

.part-block__icon--danger:hover:not(:disabled) {
  border-color: var(--color-accent-coral);
  color: var(--color-accent-coral-dark);
}

.part-block__plus:disabled,
.part-block__icon:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.part-block__main {
  min-width: 0;
}

.part-block__toolbox {
  margin-bottom: 0.55rem;
}

.part-block__body {
  min-width: 0;
  border: 1px solid var(--industrial-line);
  border-radius: 0.35rem;
  background: var(--color-surface-950);
  overflow: hidden;
}
</style>
