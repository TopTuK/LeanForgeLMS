<script setup>
defineProps({
  narrow: { type: Boolean, default: false },
});
</script>

<template>
  <div
    class="studio-shell"
    :class="{ 'studio-shell--narrow': narrow }"
  >
    <div class="studio-shell__inner">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.studio-shell {
  min-height: calc(100vh - 4.5rem);
  background: var(--color-surface-950);
}

.studio-shell__inner {
  width: min(1120px, 100%);
  margin: 0 auto;
  padding: 1.5rem 1.25rem 3rem;
}

.studio-shell--narrow .studio-shell__inner {
  width: min(800px, 100%);
  padding-top: 0;
}

@media (min-width: 768px) {
  .studio-shell__inner {
    padding: 2rem 1.75rem 4rem;
  }

  .studio-shell--narrow .studio-shell__inner {
    padding-top: 0;
  }
}
</style>
