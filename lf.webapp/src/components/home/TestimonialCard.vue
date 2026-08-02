<script setup>
defineProps({
    quote: { type: String, required: true },
    name: { type: String, required: true },
    role: { type: String, required: true },
    tag: { type: String, default: '' },
});
</script>

<template>
  <article class="flat-card rounded-card p-6 flex flex-col gap-5 h-full">
    <span
      v-if="tag"
      class="text-xs font-semibold uppercase tracking-wide text-accent-coral"
    >
      {{ tag }}
    </span>
    <p class="text-base text-ink leading-relaxed flex-1">
      {{ quote }}
    </p>
    <div>
      <p class="text-sm font-semibold text-ink">
        {{ name }}
      </p>
      <p class="text-xs text-ink-muted mt-0.5">
        {{ role }}
      </p>
    </div>
  </article>
</template>
