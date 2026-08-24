<script setup>
import { cn } from '@/lib/utils';

const props = defineProps({
  class: { type: [String, Object, Array], default: '' },
});
</script>

<template>
  <div
    :class="cn('animate-pulse rounded-md bg-surface-800', props.class)"
  />
</template>
