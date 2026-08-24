<script setup>
import {
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogOverlay,
  DialogPortal,
  DialogRoot,
  DialogTitle,
} from 'reka-ui';
import { X } from 'lucide-vue-next';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';

const open = defineModel('open', { type: Boolean, default: false });

defineProps({
  title: { type: String, default: '' },
  description: { type: String, default: '' },
  confirmLabel: { type: String, default: '' },
  cancelLabel: { type: String, default: '' },
  danger: { type: Boolean, default: false },
  hideFooter: { type: Boolean, default: false },
});

const emit = defineEmits(['confirm', 'cancel']);

function onConfirm() {
  emit('confirm');
}

function onCancel() {
  open.value = false;
  emit('cancel');
}
</script>

<template>
  <DialogRoot v-model:open="open">
    <DialogPortal>
      <DialogOverlay class="fixed inset-0 z-50 bg-ink/40 backdrop-blur-[2px]" />
      <DialogContent
        :class="cn(
          'fixed left-1/2 top-1/2 z-50 w-[min(32rem,calc(100vw-2rem))] -translate-x-1/2 -translate-y-1/2 rounded-lg border border-border-subtle bg-card p-6 text-ink shadow-lg focus:outline-none',
        )"
      >
        <div class="flex items-start justify-between gap-4">
          <div class="space-y-1">
            <DialogTitle
              v-if="title"
              class="font-display text-lg font-semibold tracking-tight"
            >
              {{ title }}
            </DialogTitle>
            <DialogDescription
              v-if="description"
              class="text-sm text-ink-muted"
            >
              {{ description }}
            </DialogDescription>
          </div>
          <DialogClose
            class="rounded-md p-1 text-ink-muted hover:bg-surface-900 hover:text-ink"
            @click="onCancel"
          >
            <X class="size-4" />
          </DialogClose>
        </div>

        <div
          v-if="$slots.default"
          class="mt-4"
        >
          <slot />
        </div>

        <div
          v-if="!hideFooter && (confirmLabel || cancelLabel || $slots.footer)"
          class="mt-6 flex justify-end gap-2"
        >
          <slot name="footer">
            <Button
              v-if="cancelLabel"
              variant="outline"
              @click="onCancel"
            >
              {{ cancelLabel }}
            </Button>
            <Button
              v-if="confirmLabel"
              :variant="danger ? 'destructive' : 'default'"
              @click="onConfirm"
            >
              {{ confirmLabel }}
            </Button>
          </slot>
        </div>
      </DialogContent>
    </DialogPortal>
  </DialogRoot>
</template>
