<script setup>
import { onMounted, onUnmounted, watch } from 'vue';

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  title: { type: String, required: true },
  confirmLabel: { type: String, default: 'Save' },
  cancelLabel: { type: String, default: 'Cancel' },
  danger: { type: Boolean, default: false },
  size: {
    type: String,
    default: 'md',
    validator: (value) => ['md', 'lg'].includes(value),
  },
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

onMounted(() => {
  window.addEventListener('keydown', onKeydown);
});

onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown);
  document.body.style.overflow = '';
});
</script>

<template>
  <Teleport to="body">
    <div
      v-if="modelValue"
      class="forge-dialog"
      role="dialog"
      aria-modal="true"
      :aria-label="title"
    >
      <button
        type="button"
        class="forge-dialog__backdrop"
        aria-label="Close"
        @click="close"
      />
      <div
        class="forge-dialog__panel"
        :class="`forge-dialog__panel--${size}`"
      >
        <header class="forge-dialog__header">
          <h2 class="forge-dialog__title">
            {{ title }}
          </h2>
          <button
            type="button"
            class="forge-dialog__close"
            aria-label="Close"
            @click="close"
          >
            ×
          </button>
        </header>

        <div class="forge-dialog__body">
          <slot />
        </div>

        <footer class="forge-dialog__footer">
          <button
            type="button"
            class="forge-dialog__btn forge-dialog__btn--ghost"
            @click="close"
          >
            {{ cancelLabel }}
          </button>
          <button
            type="button"
            class="forge-dialog__btn"
            :class="danger ? 'forge-dialog__btn--danger' : 'forge-dialog__btn--accent'"
            @click="confirm"
          >
            {{ confirmLabel }}
          </button>
        </footer>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.forge-dialog {
  position: fixed;
  inset: 0;
  z-index: 80;
  display: grid;
  place-items: center;
  padding: 1.25rem;
}

.forge-dialog__backdrop {
  position: absolute;
  inset: 0;
  border: 0;
  background: rgba(16, 16, 15, 0.55);
  cursor: pointer;
}

.forge-dialog__panel {
  position: relative;
  z-index: 1;
  width: min(100%, 28rem);
  max-height: min(90vh, 40rem);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: var(--color-surface-950);
  border: 1px solid var(--industrial-line-strong);
  border-radius: var(--radius-card);
  box-shadow: 0 24px 60px rgba(16, 16, 15, 0.28);
  animation: forge-dialog-rise 0.22s ease both;
}

.forge-dialog__panel--lg {
  width: min(100%, 36rem);
}

.forge-dialog__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.15rem 1.25rem 0.85rem;
  border-bottom: 1px solid var(--industrial-line);
}

.forge-dialog__title {
  margin: 0;
  color: var(--color-ink);
  font-size: 1.05rem;
  font-weight: 800;
  letter-spacing: -0.02em;
}

.forge-dialog__close {
  display: grid;
  place-items: center;
  width: 2rem;
  height: 2rem;
  padding: 0;
  color: var(--color-ink-muted);
  background: transparent;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.25rem;
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
  transition: color 0.15s ease, border-color 0.15s ease;
}

.forge-dialog__close:hover {
  color: var(--color-ink);
  border-color: var(--color-ink-faint);
}

.forge-dialog__body {
  padding: 1.15rem 1.25rem;
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.forge-dialog__footer {
  display: flex;
  justify-content: flex-end;
  flex-wrap: wrap;
  gap: 0.65rem;
  padding: 0.85rem 1.25rem 1.15rem;
  border-top: 1px solid var(--industrial-line);
}

.forge-dialog__btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 2.5rem;
  padding: 0.55rem 1.1rem;
  border-radius: var(--radius-pill);
  border: 1px solid transparent;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.15s ease, border-color 0.15s ease, color 0.15s ease, transform 0.12s ease;
}

.forge-dialog__btn:active {
  transform: scale(0.97);
}

.forge-dialog__btn--ghost {
  color: var(--color-ink-muted);
  background: transparent;
  border-color: var(--color-border-subtle);
}

.forge-dialog__btn--ghost:hover {
  color: var(--color-ink);
  border-color: var(--color-ink-faint);
}

.forge-dialog__btn--accent {
  color: #fff;
  background: var(--color-accent-coral);
}

.forge-dialog__btn--accent:hover {
  background: var(--color-accent-coral-dark);
}

.forge-dialog__btn--danger {
  color: #fff;
  background: #b33a2b;
}

.forge-dialog__btn--danger:hover {
  background: #9a3124;
}

@keyframes forge-dialog-rise {
  from {
    opacity: 0;
    transform: translateY(0.6rem) scale(0.98);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}
</style>
