<script setup>
import { useI18n } from 'vue-i18n';

const props = defineProps({
  insertIndex: { type: Number, required: true },
});

const emit = defineEmits(['select']);

const { t } = useI18n();

const types = [
  { type: 'text', labelKey: 'courses.lessonEditor.parts.type_text' },
  { type: 'image', labelKey: 'courses.lessonEditor.parts.type_image' },
  { type: 'video', labelKey: 'courses.lessonEditor.parts.type_video' },
  { type: 'audio', labelKey: 'courses.lessonEditor.parts.type_audio' },
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
      {{ t(item.labelKey) }}
    </button>
  </div>
</template>

<style scoped>
.part-toolbox {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  padding: 0.45rem;
  background: var(--color-surface-950);
  border: 1px solid var(--industrial-line-strong);
  border-radius: 0.35rem;
  box-shadow: 0 14px 32px -22px rgb(15 23 42 / 0.55);
}

.part-toolbox__item {
  min-width: 5.5rem;
  padding: 0.45rem 0.7rem;
  border: 1px solid var(--industrial-line);
  border-radius: 0.25rem;
  background: var(--color-surface-900);
  color: var(--color-ink);
  font-size: 0.82rem;
  font-weight: 700;
  letter-spacing: 0.02em;
  cursor: pointer;
}

.part-toolbox__item:hover,
.part-toolbox__item:focus-visible {
  border-color: var(--color-accent-coral);
  color: var(--color-accent-coral-dark);
  outline: none;
}
</style>
