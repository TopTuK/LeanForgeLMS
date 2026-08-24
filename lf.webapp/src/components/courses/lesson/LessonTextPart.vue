<script setup>
import { nextTick } from 'vue';
import RichEditor from '@/components/courses/form/RichEditor.vue';

defineProps({
  modelValue: { type: String, default: '' },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['update:modelValue', 'slash']);

function plainText(html) {
  if (!html) return '';
  return html
    .replace(/<[^>]+>/g, '')
    .replace(/&nbsp;/gi, ' ')
    .trim();
}

function onUpdate(html) {
  if (plainText(html) === '/') {
    emit('update:modelValue', '');
    nextTick(() => emit('slash'));
    return;
  }
  emit('update:modelValue', html);
}
</script>

<template>
  <RichEditor
    :model-value="modelValue"
    compact
    :allow-image="false"
    :disabled="disabled"
    :placeholder="$t('courses.lessonEditor.parts.text_placeholder')"
    @update:model-value="onUpdate"
  />
</template>
