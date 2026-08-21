<script setup>
defineProps({
  label: { type: String, required: true },
  danger: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
});
</script>

<template>
  <button
    type="button"
    class="studio-icon-btn"
    :class="{ 'studio-icon-btn--danger': danger }"
    :disabled="disabled"
    :title="label"
    :aria-label="label"
  >
    <slot />
  </button>
</template>

<style scoped>
.studio-icon-btn {
  display: inline-grid;
  place-items: center;
  width: 1.85rem;
  height: 1.85rem;
  padding: 0;
  border: 0;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink-muted);
  cursor: pointer;
  transition: background-color 0.12s ease, color 0.12s ease;
}

.studio-icon-btn:hover:not(:disabled) {
  background: var(--color-surface-900);
  color: var(--color-ink);
}

.studio-icon-btn--danger:hover:not(:disabled) {
  background: color-mix(in srgb, #b33a2b 10%, transparent);
  color: #b33a2b;
}

.studio-icon-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.studio-icon-btn :deep(svg) {
  width: 1rem;
  height: 1rem;
}
</style>
