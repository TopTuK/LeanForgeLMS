<script setup>
defineProps({
    icon: {
        type: String,
        required: true,
        validator: (value) => ['llm', 'kanban'].includes(value),
    },
    title: { type: String, required: true },
    description: { type: String, required: true },
    duration: { type: String, required: true },
    category: { type: String, required: true },
    cta: { type: String, default: '' },
    to: { type: [String, Object], default: null },
});
</script>

<template>
  <article class="flat-card rounded-card p-6 flex flex-col gap-4 h-full hover:border-ink/30 transition">
    <div class="w-11 h-11 rounded-md bg-accent-soft flex items-center justify-center text-accent-coral">
      <svg
        v-if="icon === 'llm'"
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <rect
          x="7"
          y="7"
          width="10"
          height="10"
          rx="2"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <circle
          cx="12"
          cy="12"
          r="2"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <path
          d="M12 3v4M12 17v4M3 12h4M17 12h4M5.6 5.6l2.8 2.8M15.6 15.6l2.8 2.8M18.4 5.6l-2.8 2.8M8.4 15.6l-2.8 2.8"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
        />
      </svg>
      <svg
        v-else-if="icon === 'kanban'"
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <rect
          x="3"
          y="4"
          width="18"
          height="16"
          rx="1.5"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <path
          d="M9 4v16M15 4v16"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <rect
          x="5"
          y="7"
          width="2"
          height="5"
          rx="0.5"
          fill="currentColor"
        />
        <rect
          x="11"
          y="7"
          width="2"
          height="8"
          rx="0.5"
          fill="currentColor"
        />
        <rect
          x="17"
          y="7"
          width="2"
          height="3"
          rx="0.5"
          fill="currentColor"
        />
      </svg>
    </div>

    <div class="flex items-center gap-2">
      <span class="rounded-pill bg-surface-800 px-3 py-1 text-xs font-medium text-ink-muted">{{ duration }}</span>
      <span class="rounded-pill bg-accent-soft px-3 py-1 text-xs font-semibold text-accent-coral">{{ category }}</span>
    </div>

    <h3 class="font-semibold text-ink text-lg leading-snug">
      {{ title }}
    </h3>
    <p class="text-sm text-ink-muted flex-1 leading-relaxed">
      {{ description }}
    </p>

    <router-link
      v-if="cta && to"
      :to="to"
      class="inline-flex items-center gap-2 w-fit text-sm font-semibold text-accent-coral hover:text-accent-coral-dark transition border-b border-accent-coral/40 hover:border-accent-coral-dark"
    >
      {{ cta }}
      <span aria-hidden="true">→</span>
    </router-link>
  </article>
</template>
