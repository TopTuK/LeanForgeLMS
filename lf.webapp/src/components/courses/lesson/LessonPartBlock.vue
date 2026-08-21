<script setup>
import { GripVertical, Plus, Trash2 } from 'lucide-vue-next';
import { useI18n } from 'vue-i18n';
import LessonPartToolbox from './LessonPartToolbox.vue';

defineProps({
  index: { type: Number, required: true },
  total: { type: Number, required: true },
  toolboxOpen: { type: Boolean, default: false },
  insertIndex: { type: Number, default: 0 },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['add', 'select-type', 'remove']);

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
        class="part-block__handle part-block-drag"
        :disabled="disabled"
        :title="t('courses.lessonEditor.parts.drag')"
        :aria-label="t('courses.lessonEditor.parts.drag')"
      >
        <GripVertical :size="16" />
      </button>
      <button
        type="button"
        class="part-block__plus"
        :disabled="disabled"
        :title="t('courses.lessonEditor.parts.add_after')"
        :aria-label="t('courses.lessonEditor.parts.add_after')"
        :aria-expanded="toolboxOpen"
        @click="emit('add')"
      >
        <Plus :size="14" />
      </button>
      <button
        type="button"
        class="part-block__icon part-block__icon--danger"
        :disabled="disabled"
        :title="t('courses.lessonEditor.parts.delete')"
        :aria-label="t('courses.lessonEditor.parts.delete')"
        @click="emit('remove')"
      >
        <Trash2 :size="14" />
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
  grid-template-columns: 2.1rem minmax(0, 1fr);
  gap: 0.35rem;
  align-items: start;
  padding: 0.15rem 0;
  border-radius: 0.55rem;
}

.part-block__gutter {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.15rem;
  padding-top: 0.45rem;
  opacity: 0;
  transition: opacity 0.12s ease;
}

@media (hover: hover) {
  .part-block:hover .part-block__gutter,
  .part-block:focus-within .part-block__gutter,
  .part-block--toolbox-open .part-block__gutter {
    opacity: 1;
  }
}

@media (hover: none) {
  .part-block__gutter {
    opacity: 1;
  }
}

.part-block__handle,
.part-block__plus,
.part-block__icon {
  display: inline-grid;
  place-items: center;
  width: 1.65rem;
  height: 1.65rem;
  padding: 0;
  border: 0;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink-muted);
  cursor: pointer;
}

.part-block__handle {
  cursor: grab;
  color: var(--color-ink-faint);
}

.part-block__handle:hover:not(:disabled),
.part-block__plus:hover:not(:disabled),
.part-block__icon:hover:not(:disabled) {
  background: var(--color-surface-900);
  color: var(--color-ink);
}

.part-block__icon--danger:hover:not(:disabled) {
  background: color-mix(in srgb, #b33a2b 10%, transparent);
  color: #b33a2b;
}

.part-block__handle:disabled,
.part-block__plus:disabled,
.part-block__icon:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.part-block__main {
  position: relative;
  min-width: 0;
}

.part-block__toolbox {
  position: absolute;
  top: -0.15rem;
  left: 0;
  z-index: 5;
}

.part-block__body {
  min-width: 0;
  border-radius: 0.55rem;
  transition: background-color 0.12s ease;
}

.part-block:hover .part-block__body,
.part-block:focus-within .part-block__body {
  background: color-mix(in srgb, var(--color-surface-900) 65%, transparent);
}
</style>
