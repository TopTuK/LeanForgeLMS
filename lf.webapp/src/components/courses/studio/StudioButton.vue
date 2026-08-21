<script setup>
defineProps({
  variant: {
    type: String,
    default: 'ghost',
    validator: (v) => ['primary', 'ghost', 'danger', 'quiet'].includes(v),
  },
  size: {
    type: String,
    default: 'md',
    validator: (v) => ['sm', 'md'].includes(v),
  },
  disabled: { type: Boolean, default: false },
  type: { type: String, default: 'button' },
});
</script>

<template>
  <button
    :type="type"
    class="studio-btn"
    :class="[
      `studio-btn--${variant}`,
      `studio-btn--${size}`,
    ]"
    :disabled="disabled"
  >
    <slot />
  </button>
</template>

<style scoped>
.studio-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  border-radius: 0.5rem;
  font-family: inherit;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.12s ease, border-color 0.12s ease, color 0.12s ease, opacity 0.12s ease;
}

.studio-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.studio-btn--md {
  min-height: 2.35rem;
  padding: 0.45rem 0.95rem;
  font-size: 0.875rem;
}

.studio-btn--sm {
  min-height: 1.85rem;
  padding: 0.25rem 0.65rem;
  font-size: 0.8rem;
}

.studio-btn--primary {
  border: 0;
  background: var(--color-accent-coral);
  color: #ffffff;
}

.studio-btn--primary:hover:not(:disabled) {
  background: var(--color-accent-coral-dark);
}

.studio-btn--ghost {
  border: 1px solid var(--color-border-subtle);
  background: var(--color-surface-950);
  color: var(--color-ink);
}

.studio-btn--ghost:hover:not(:disabled) {
  border-color: var(--color-ink-faint);
  background: var(--color-surface-900);
}

.studio-btn--danger {
  border: 1px solid color-mix(in srgb, #b33a2b 35%, var(--color-border-subtle));
  background: transparent;
  color: #b33a2b;
}

.studio-btn--danger:hover:not(:disabled) {
  background: color-mix(in srgb, #b33a2b 8%, transparent);
}

.studio-btn--quiet {
  border: 0;
  background: transparent;
  color: var(--color-ink-muted);
  padding-inline: 0.35rem;
}

.studio-btn--quiet:hover:not(:disabled) {
  color: var(--color-ink);
  background: var(--color-surface-900);
}
</style>
