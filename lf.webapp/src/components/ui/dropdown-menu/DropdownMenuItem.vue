<script setup>
import { DropdownMenuItem } from 'reka-ui';
import { cn } from '@/lib/utils';

defineProps({
  disabled: { type: Boolean, default: false },
  destructive: { type: Boolean, default: false },
});
</script>

<template>
  <DropdownMenuItem
    :disabled="disabled"
    :class="cn(
      'flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 outline-none data-[highlighted]:bg-surface-900',
      destructive && 'text-destructive',
    )"
  >
    <slot />
  </DropdownMenuItem>
</template>
