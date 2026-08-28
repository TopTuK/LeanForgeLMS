<script setup>
import { Type, Image, Film, Headphones, ListChecks, Paperclip } from 'lucide-vue-next';
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
    icon: Type,
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
    icon: Film,
  },
  {
    type: 'audio',
    labelKey: 'courses.lessonEditor.parts.type_audio',
    descKey: 'courses.lessonEditor.parts.desc_audio',
    icon: Headphones,
  },
  {
    type: 'quiz',
    labelKey: 'courses.lessonEditor.parts.type_quiz',
    descKey: 'courses.lessonEditor.parts.desc_quiz',
    icon: ListChecks,
  },
  {
    type: 'files',
    labelKey: 'courses.lessonEditor.parts.type_files',
    descKey: 'courses.lessonEditor.parts.desc_files',
    icon: Paperclip,
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
        :title="t(item.descKey)"
        :aria-label="`${t(item.labelKey)}. ${t(item.descKey)}`"
        @click="choose(item.type)"
      >
        <span
          class="part-type-card__icon"
          aria-hidden="true"
        >
          <component
            :is="item.icon"
            :size="22"
            :stroke-width="1.75"
          />
        </span>
        <span class="part-type-card__title">{{ t(item.labelKey) }}</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.part-type-panel {
  width: 100%;
  max-width: 22rem;
  padding: 0.85rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.75rem;
  background: var(--color-surface-950);
  box-shadow: 0 12px 28px -20px rgb(15 23 42 / 0.35);
  animation: part-type-panel-in 0.14s ease;
}

@keyframes part-type-panel-in {
  from {
    opacity: 0;
    transform: translateY(-3px);
  }
  to {
    opacity: 1;
    transform: none;
  }
}

.part-type-panel__heading {
  margin: 0 0 0.65rem;
  padding: 0 0.15rem;
  color: var(--color-ink-muted);
  font-size: 0.72rem;
  font-weight: 600;
  letter-spacing: 0.02em;
}

.part-type-panel__grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.45rem;
}

.part-type-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  min-height: 4.75rem;
  padding: 0.7rem 0.4rem;
  border: 1px solid transparent;
  border-radius: 0.55rem;
  background: var(--color-surface-900);
  color: var(--color-ink);
  cursor: pointer;
  transition:
    background-color 0.12s ease,
    border-color 0.12s ease,
    color 0.12s ease;
}

.part-type-card__icon {
  display: inline-grid;
  place-items: center;
  width: 2.35rem;
  height: 2.35rem;
  border-radius: 0.5rem;
  color: var(--color-ink-muted);
  transition: color 0.12s ease, background-color 0.12s ease;
}

.part-type-card__title {
  color: var(--color-ink-muted);
  font-size: 0.75rem;
  font-weight: 600;
  line-height: 1.2;
  text-align: center;
  transition: color 0.12s ease;
}

.part-type-card:hover:not(:disabled),
.part-type-card:focus-visible {
  border-color: var(--color-border-subtle);
  background: var(--color-surface-950);
  outline: none;
}

.part-type-card:hover:not(:disabled) .part-type-card__icon,
.part-type-card:focus-visible .part-type-card__icon {
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
}

.part-type-card:hover:not(:disabled) .part-type-card__title,
.part-type-card:focus-visible .part-type-card__title {
  color: var(--color-ink);
}

.part-type-card:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
</style>
