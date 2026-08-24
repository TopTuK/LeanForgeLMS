<script setup>
import { cn } from '@/lib/utils';

const props = defineProps({
  modelValue: { type: String, default: '' },
  class: { type: [String, Object, Array], default: '' },
  rows: { type: Number, default: 4 },
});

const emit = defineEmits(['update:modelValue']);
</script>

<template>
  <textarea
    :value="modelValue"
    :rows="rows"
    :class="cn(
      'flex min-h-24 w-full rounded-md border border-border-subtle bg-card px-3 py-2 text-sm text-ink placeholder:text-ink-faint focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50',
      props.class,
    )"
    @input="emit('update:modelValue', $event.target.value)"
  />
</template>
