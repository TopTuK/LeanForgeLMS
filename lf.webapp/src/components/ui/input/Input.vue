<script setup>
import { useId } from 'vue';
import { cn } from '@/lib/utils';

const props = defineProps({
  modelValue: { type: [String, Number], default: '' },
  class: { type: [String, Object, Array], default: '' },
  type: { type: String, default: 'text' },
});

const emit = defineEmits(['update:modelValue']);
const id = useId();
</script>

<template>
  <input
    :id="id"
    :type="type"
    :value="modelValue"
    :class="cn(
      'flex h-10 w-full rounded-md border border-border-subtle bg-card px-3 py-2 text-sm text-ink shadow-none transition-colors placeholder:text-ink-faint focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50',
      props.class,
    )"
    @input="emit('update:modelValue', $event.target.value)"
  >
</template>
