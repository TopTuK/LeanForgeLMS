<script setup>
import { FileText, Image, Video, AudioLines, CircleHelp } from 'lucide-vue-next';
import { useI18n } from 'vue-i18n';

const props = defineProps({
  insertIndex: { type: Number, required: true },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['select']);

const { t } = useI18n();

const types = [
  {
    type: 'text',
    labelKey: 'courses.lessonEditor.parts.type_text',
    descKey: 'courses.lessonEditor.parts.desc_text',
    icon: FileText,
  },
  {
    type: 'image',
    labelKey: 'courses.lessonEditor.parts.type_image',
    descKey: 'courses.lessonEditor.parts.desc_image',
    icon: Image,
  },
  {
    type: 'video',
    labelKey: 'courses.lessonEditor.parts.type_video',
    descKey: 'courses.lessonEditor.parts.desc_video',
    icon: Video,
  },
  {
    type: 'audio',
    labelKey: 'courses.lessonEditor.parts.type_audio',
    descKey: 'courses.lessonEditor.parts.desc_audio',
    icon: AudioLines,
  },
  {
    type: 'quiz',
    labelKey: 'courses.lessonEditor.parts.type_quiz',
    descKey: 'courses.lessonEditor.parts.desc_quiz',
    icon: CircleHelp,
  },
];

function choose(type) {
  if (props.disabled) return;
  emit('select', { type, index: props.insertIndex });
}
</script>

<template>
  <div
    class="part-type-panel"
    role="menu"
    :aria-label="t('courses.lessonEditor.parts.toolbox_label')"
  >
    <p class="part-type-panel__heading">
      {{ t('courses.lessonEditor.parts.toolbox_label') }}
    </p>
    <div class="part-type-panel__grid">
      <button
        v-for="item in types"
        :key="item.type"
        type="button"
        class="part-type-card"
        role="menuitem"
        :disabled="disabled"
        @click="choose(item.type)"
      >
        <span class="part-type-card__icon">
          <component
            :is="item.icon"
            :size="20"
          />
        </span>
        <span class="part-type-card__text">
          <span class="part-type-card__title">{{ t(item.labelKey) }}</span>
          <span class="part-type-card__hint">{{ t(item.descKey) }}</span>
        </span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.part-type-panel {
  width: 100%;
  padding: 1rem 1rem 1.05rem;
  background: var(--color-surface-950);
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.85rem;
  animation: part-type-panel-in 0.16s ease;
}

@keyframes part-type-panel-in {
  from {
    opacity: 0;
    transform: translateY(-4px);
  }
  to {
    opacity: 1;
    transform: none;
  }
}

.part-type-panel__heading {
  margin: 0 0 0.75rem;
  color: var(--color-ink-muted);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.part-type-panel__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.55rem;
}

@media (min-width: 640px) {
  .part-type-panel__grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (min-width: 900px) {
  .part-type-panel__grid {
    grid-template-columns: repeat(5, minmax(0, 1fr));
  }
}

.part-type-card {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.7rem;
  min-height: 7.25rem;
  padding: 0.9rem 0.85rem 0.95rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.7rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  cursor: pointer;
  text-align: left;
  transition:
    border-color 0.15s ease,
    background-color 0.15s ease,
    transform 0.15s ease;
}

.part-type-card__icon {
  display: inline-grid;
  place-items: center;
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.55rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
}

.part-type-card__text {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}

.part-type-card__title {
  font-size: 0.92rem;
  font-weight: 700;
  line-height: 1.25;
}

.part-type-card__hint {
  color: var(--color-ink-muted);
  font-size: 0.75rem;
  font-weight: 500;
  line-height: 1.35;
}

.part-type-card:hover:not(:disabled),
.part-type-card:focus-visible {
  background: var(--color-accent-soft);
  border-color: var(--color-accent-coral);
  outline: none;
  transform: translateY(-1px);
}

.part-type-card:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
