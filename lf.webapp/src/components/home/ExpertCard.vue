<script setup>
defineProps({
    name: { type: String, required: true },
    role: { type: String, required: true },
});
</script>

<template>
  <article class="flex flex-col gap-2 py-4 border-b border-border-subtle last:border-b-0">
    <h3 class="font-semibold text-ink text-lg">
      {{ name }}
    </h3>
    <p class="text-sm text-ink-muted leading-relaxed">
      {{ role }}
    </p>
  </article>
</template>
