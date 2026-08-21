<script setup>
import { FileText, Image, Video, AudioLines, CircleHelp } from 'lucide-vue-next';
import { useI18n } from 'vue-i18n';

const props = defineProps({
  insertIndex: { type: Number, required: true },
});

const emit = defineEmits(['select']);

const { t } = useI18n();

const types = [
  { type: 'text', labelKey: 'courses.lessonEditor.parts.type_text', icon: FileText },
  { type: 'image', labelKey: 'courses.lessonEditor.parts.type_image', icon: Image },
  { type: 'video', labelKey: 'courses.lessonEditor.parts.type_video', icon: Video },
  { type: 'audio', labelKey: 'courses.lessonEditor.parts.type_audio', icon: AudioLines },
  { type: 'quiz', labelKey: 'courses.lessonEditor.parts.type_quiz', icon: CircleHelp },
];

function choose(type) {
  emit('select', { type, index: props.insertIndex });
}
</script>

<template>
  <div
    class="part-toolbox"
    role="menu"
    :aria-label="t('courses.lessonEditor.parts.toolbox_label')"
  >
    <button
      v-for="item in types"
      :key="item.type"
      type="button"
      class="part-toolbox__item"
      role="menuitem"
      @click="choose(item.type)"
    >
      <span class="part-toolbox__icon">
        <component
          :is="item.icon"
          :size="16"
        />
      </span>
      <span class="part-toolbox__label">{{ t(item.labelKey) }}</span>
    </button>
  </div>
</template>

<style scoped>
.part-toolbox {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 11.5rem;
  padding: 0.35rem;
  background: var(--color-surface-950);
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.65rem;
  box-shadow: 0 16px 36px -22px rgb(15 23 42 / 0.45);
  animation: part-toolbox-pop 0.12s ease;
}

@keyframes part-toolbox-pop {
  from {
    opacity: 0;
    transform: translateY(-3px);
  }
  to {
    opacity: 1;
    transform: none;
  }
}

.part-toolbox__item {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  width: 100%;
  padding: 0.5rem 0.55rem;
  border: 0;
  border-radius: 0.45rem;
  background: transparent;
  color: var(--color-ink);
  cursor: pointer;
  text-align: left;
}

.part-toolbox__icon {
  display: inline-grid;
  place-items: center;
  width: 1.7rem;
  height: 1.7rem;
  border-radius: 0.4rem;
  background: var(--color-surface-900);
  color: var(--color-ink-muted);
}

.part-toolbox__label {
  font-size: 0.88rem;
  font-weight: 600;
}

.part-toolbox__item:hover,
.part-toolbox__item:focus-visible {
  background: var(--color-surface-900);
  outline: none;
}

.part-toolbox__item:hover .part-toolbox__icon,
.part-toolbox__item:focus-visible .part-toolbox__icon {
  background: color-mix(in srgb, var(--color-accent-coral) 16%, transparent);
  color: var(--color-accent-coral-dark);
}
</style>
