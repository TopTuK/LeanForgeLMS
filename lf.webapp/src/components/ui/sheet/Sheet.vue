<script setup>
import {
  DialogClose,
  DialogContent,
  DialogOverlay,
  DialogPortal,
  DialogRoot,
  DialogTitle,
} from 'reka-ui';

const open = defineModel('open', { type: Boolean, default: false });

defineProps({
  title: { type: String, default: '' },
  side: { type: String, default: 'right' },
});
</script>

<template>
  <DialogRoot v-model:open="open">
    <DialogPortal>
      <DialogOverlay class="fixed inset-0 z-50 bg-ink/40" />
      <DialogContent
        class="fixed inset-y-0 z-50 w-[min(20rem,100vw)] border-border-subtle bg-card p-4 shadow-lg focus:outline-none"
        :class="side === 'left' ? 'left-0 border-r' : 'right-0 border-l'"
      >
        <div class="mb-4 flex items-center justify-between">
          <DialogTitle class="font-display text-base font-semibold">
            {{ title }}
          </DialogTitle>
          <DialogClose class="text-sm text-ink-muted hover:text-ink">
            ×
          </DialogClose>
        </div>
        <slot />
      </DialogContent>
    </DialogPortal>
  </DialogRoot>
</template>
