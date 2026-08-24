<script setup>
import { cn } from '@/lib/utils';

const props = defineProps({
  variant: { type: String, default: 'default' },
  class: { type: [String, Object, Array], default: '' },
});
</script>

<template>
  <span
    :class="cn(
      'inline-flex items-center rounded-md border px-2 py-0.5 text-[0.7rem] font-semibold tracking-wide uppercase',
      variant === 'coral' && 'border-accent-coral/40 bg-accent-soft text-accent-coral',
      variant === 'muted' && 'border-border-subtle bg-surface-900 text-ink-muted',
      variant === 'solid' && 'border-ink bg-ink text-surface-950',
      variant === 'default' && 'border-border-subtle bg-card text-ink',
      props.class,
    )"
  >
    <slot />
  </span>
</template>
