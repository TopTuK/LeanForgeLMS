<script setup>
defineProps({
    name: { type: String, required: true },
    role: { type: String, required: true },
    bio: { type: String, required: true },
    highlights: { type: Array, default: () => [] },
    cta: { type: String, required: true },
    href: { type: String, required: true },
});
</script>

<template>
  <article class="flat-card rounded-card p-6 md:p-8 flex flex-col gap-6 md:flex-row md:items-start md:gap-10">
    <div
      class="author-mark shrink-0 w-16 h-16 md:w-20 md:h-20 rounded-md bg-accent-soft text-accent-coral flex items-center justify-center"
      aria-hidden="true"
    >
      <span class="text-xl md:text-2xl font-extrabold tracking-tight">SS</span>
    </div>

    <div class="flex flex-col gap-4 min-w-0 flex-1">
      <div>
        <p class="text-xs font-semibold uppercase tracking-wide text-accent-coral mb-2">
          {{ role }}
        </p>
        <h3 class="text-2xl md:text-3xl font-extrabold text-ink tracking-tight">
          {{ name }}
        </h3>
        <p class="mt-3 text-base text-ink-muted leading-relaxed max-w-2xl">
          {{ bio }}
        </p>
      </div>

      <ul
        v-if="highlights.length"
        class="flex flex-wrap gap-2"
      >
        <li
          v-for="item in highlights"
          :key="item"
          class="rounded-pill bg-surface-800 px-3 py-1 text-xs font-medium text-ink-muted"
        >
          {{ item }}
        </li>
      </ul>

      <a
        :href="href"
        target="_blank"
        rel="noopener noreferrer"
        class="inline-flex items-center gap-2 w-fit text-sm font-semibold text-accent-coral hover:text-accent-coral-dark transition border-b border-accent-coral/40 hover:border-accent-coral-dark"
      >
        {{ cta }}
        <span aria-hidden="true">→</span>
      </a>
    </div>
  </article>
</template>
