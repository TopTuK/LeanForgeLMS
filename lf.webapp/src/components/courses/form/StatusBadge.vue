<script setup>
defineProps({
  variant: {
    type: String,
    default: 'draft',
    validator: (value) => ['draft', 'published', 'preview'].includes(value),
  },
  label: { type: String, required: true },
});
</script>

<template>
  <span
    class="status-badge"
    :class="`status-badge--${variant}`"
  >
    {{ label }}
  </span>
</template>

<style scoped>
.status-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.2rem 0.55rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 999px;
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  line-height: 1.2;
}

.status-badge--draft {
  color: var(--color-ink-muted);
  background: var(--color-surface-900);
}

.status-badge--published {
  color: var(--color-accent-coral);
  background: var(--color-accent-soft);
  border-color: color-mix(in srgb, var(--color-accent-coral) 40%, transparent);
}

.status-badge--preview {
  color: var(--color-ink);
  background: var(--industrial-accent-wash);
}
</style>
