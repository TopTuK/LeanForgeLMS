<script setup>
import { onMounted, onUnmounted, watch } from 'vue';
import StudioButton from './StudioButton.vue';

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  title: { type: String, required: true },
  confirmLabel: { type: String, default: 'Confirm' },
  cancelLabel: { type: String, default: 'Cancel' },
  danger: { type: Boolean, default: false },
});

const emit = defineEmits(['update:modelValue', 'confirm', 'cancel']);

function close() {
  emit('update:modelValue', false);
  emit('cancel');
}

function confirm() {
  emit('confirm');
  emit('update:modelValue', false);
}

function onKeydown(event) {
  if (!props.modelValue) return;
  if (event.key === 'Escape') {
    event.preventDefault();
    close();
  }
}

watch(
  () => props.modelValue,
  (open) => {
    document.body.style.overflow = open ? 'hidden' : '';
  },
);

onMounted(() => window.addEventListener('keydown', onKeydown));
onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown);
  document.body.style.overflow = '';
});
</script>

<template>
  <Teleport to="body">
    <div
      v-if="modelValue"
      class="studio-dialog"
      role="dialog"
      aria-modal="true"
      :aria-label="title"
    >
      <button
        type="button"
        class="studio-dialog__backdrop"
        aria-label="Close"
        @click="close"
      />
      <div class="studio-dialog__panel">
        <h2 class="studio-dialog__title">
          {{ title }}
        </h2>
        <div class="studio-dialog__body">
          <slot />
        </div>
        <div class="studio-dialog__actions">
          <StudioButton
            variant="ghost"
            @click="close"
          >
            {{ cancelLabel }}
          </StudioButton>
          <StudioButton
            :variant="danger ? 'danger' : 'primary'"
            @click="confirm"
          >
            {{ confirmLabel }}
          </StudioButton>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.studio-dialog {
  position: fixed;
  inset: 0;
  z-index: 80;
  display: grid;
  place-items: center;
  padding: 1.25rem;
}

.studio-dialog__backdrop {
  position: absolute;
  inset: 0;
  border: 0;
  background: rgb(15 23 42 / 0.35);
  cursor: pointer;
}

.studio-dialog__panel {
  position: relative;
  z-index: 1;
  width: min(26rem, 100%);
  padding: 1.35rem 1.35rem 1.15rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.75rem;
  background: var(--color-surface-950);
  box-shadow: 0 24px 48px -28px rgb(15 23 42 / 0.45);
}

.studio-dialog__title {
  margin: 0 0 0.85rem;
  color: var(--color-ink);
  font-size: 1.05rem;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.studio-dialog__body {
  color: var(--color-ink-muted);
  font-size: 0.92rem;
  line-height: 1.55;
}

.studio-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  margin-top: 1.25rem;
}
</style>
