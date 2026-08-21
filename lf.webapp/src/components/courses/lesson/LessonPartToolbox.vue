<script setup>
import { useI18n } from 'vue-i18n';

const props = defineProps({
  insertIndex: { type: Number, required: true },
});

const emit = defineEmits(['select']);

const { t } = useI18n();

const types = [
  { type: 'text', labelKey: 'courses.lessonEditor.parts.type_text', icon: 'notes' },
  { type: 'image', labelKey: 'courses.lessonEditor.parts.type_image', icon: 'image' },
  { type: 'video', labelKey: 'courses.lessonEditor.parts.type_video', icon: 'movie' },
  { type: 'audio', labelKey: 'courses.lessonEditor.parts.type_audio', icon: 'graphic_eq' },
  { type: 'quiz', labelKey: 'courses.lessonEditor.parts.type_quiz', icon: 'quiz' },
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
        <va-icon :name="item.icon" />
      </span>
      <span class="part-toolbox__label">{{ t(item.labelKey) }}</span>
    </button>
  </div>
</template>

<style scoped>
.part-toolbox {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(5.5rem, 1fr));
  gap: 0.5rem;
  padding: 0.6rem;
  background: var(--industrial-panel);
  border: 1px solid var(--industrial-line-strong);
  border-radius: 0.4rem;
  box-shadow: 0 18px 40px -22px rgb(15 23 42 / 0.6);
  animation: part-toolbox-pop 0.14s ease;
}

@keyframes part-toolbox-pop {
  from {
    opacity: 0;
    transform: translateY(-4px) scale(0.98);
  }
  to {
    opacity: 1;
    transform: none;
  }
}

.part-toolbox__item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  padding: 0.75rem 0.5rem;
  border: 1px solid var(--industrial-line);
  border-radius: 0.3rem;
  background: var(--color-surface-900);
  color: var(--color-ink-muted);
  cursor: pointer;
  transition: border-color 0.15s ease, color 0.15s ease, transform 0.15s ease, background-color 0.15s ease;
}

.part-toolbox__icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2.1rem;
  height: 2.1rem;
  border-radius: 999px;
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
  font-size: 1.15rem;
  transition: background-color 0.15s ease, color 0.15s ease;
}

.part-toolbox__label {
  font-size: 0.76rem;
  font-weight: 700;
  letter-spacing: 0.02em;
  text-transform: uppercase;
}

.part-toolbox__item:hover,
.part-toolbox__item:focus-visible {
  border-color: var(--color-accent-coral);
  color: var(--color-ink);
  background: var(--industrial-accent-wash);
  transform: translateY(-2px);
  outline: none;
}

.part-toolbox__item:hover .part-toolbox__icon,
.part-toolbox__item:focus-visible .part-toolbox__icon {
  background: var(--color-accent-coral);
  color: #ffffff;
}

</style>
