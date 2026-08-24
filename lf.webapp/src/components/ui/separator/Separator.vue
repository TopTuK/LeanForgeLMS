<script setup>
import { Separator } from 'reka-ui';
import { cn } from '@/lib/utils';

const props = defineProps({
  orientation: { type: String, default: 'horizontal' },
  class: { type: [String, Object, Array], default: '' },
});
</script>

<template>
  <Separator
    :orientation="orientation"
    :class="cn(
      'shrink-0 bg-border-subtle',
      orientation === 'horizontal' ? 'h-px w-full' : 'h-full w-px',
      props.class,
    )"
  />
</template>
