<script setup>
import ForgeRichEditor from '@/components/courses/forge/ForgeRichEditor.vue';

defineProps({
  modelValue: { type: String, default: '' },
  disabled: { type: Boolean, default: false },
});

defineEmits(['update:modelValue']);
</script>

<template>
  <ForgeRichEditor
    :model-value="modelValue"
    compact
    :allow-image="false"
    :disabled="disabled"
    :placeholder="$t('courses.lessonEditor.parts.text_placeholder')"
    @update:model-value="$emit('update:modelValue', $event)"
  />
</template>
