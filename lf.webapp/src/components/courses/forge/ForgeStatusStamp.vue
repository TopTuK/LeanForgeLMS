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
    class="forge-stamp"
    :class="`forge-stamp--${variant}`"
  >
    {{ label }}
  </span>
</template>

<style scoped>
.forge-stamp {
  display: inline-flex;
  align-items: center;
  padding: 0.2rem 0.65rem;
  border: 1px solid var(--industrial-line-strong);
  border-radius: 0.2rem;
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  line-height: 1.2;
}

.forge-stamp--draft {
  color: var(--color-ink-muted);
  background: var(--color-surface-900);
}

.forge-stamp--published {
  color: var(--color-accent-coral);
  background: var(--color-accent-soft);
  border-color: var(--color-accent-coral);
}

.forge-stamp--preview {
  color: var(--color-ink);
  background: var(--industrial-accent-wash);
  border-color: var(--industrial-line-strong);
}
</style>
