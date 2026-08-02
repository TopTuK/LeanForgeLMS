<script setup>
defineProps({
    icon: {
        type: String,
        required: true,
        validator: (value) =>
            ['backend', 'frontend', 'devops', 'databases', 'architecture', 'career'].includes(value),
    },
    title: { type: String, required: true },
    description: { type: String, required: true },
    duration: { type: String, required: true },
    category: { type: String, required: true },
});
</script>

<template>
  <article class="flat-card rounded-card p-6 flex flex-col gap-4 h-full hover:border-ink/30 transition">
    <div class="w-11 h-11 rounded-md bg-accent-soft flex items-center justify-center text-accent-coral">
      <svg
        v-if="icon === 'backend'"
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <rect
          x="3"
          y="4"
          width="18"
          height="6"
          rx="1.5"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <rect
          x="3"
          y="14"
          width="18"
          height="6"
          rx="1.5"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <circle
          cx="7"
          cy="7"
          r="1"
          fill="currentColor"
        />
        <circle
          cx="7"
          cy="17"
          r="1"
          fill="currentColor"
        />
      </svg>
      <svg
        v-else-if="icon === 'frontend'"
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
          d="M3 8H21"
          stroke="currentColor"
          stroke-width="1.5"
        />
      </svg>
      <svg
        v-else-if="icon === 'devops'"
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <path
          d="M12 3a9 9 0 0 1 9 9M12 3a9 9 0 0 0-9 9M21 12a9 9 0 0 1-9 9M3 12a9 9 0 0 0 9 9"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
        />
        <circle
          cx="12"
          cy="12"
          r="2.5"
          stroke="currentColor"
          stroke-width="1.5"
        />
      </svg>
      <svg
        v-else-if="icon === 'databases'"
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <ellipse
          cx="12"
          cy="6"
          rx="8"
          ry="3"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <path
          d="M4 6V18C4 19.6569 7.58172 21 12 21C16.4183 21 20 19.6569 20 18V6"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <path
          d="M4 12C4 13.6569 7.58172 15 12 15C16.4183 15 20 13.6569 20 12"
          stroke="currentColor"
          stroke-width="1.5"
        />
      </svg>
      <svg
        v-else-if="icon === 'architecture'"
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <path
          d="M4 7h6v6H4V7zm10 0h6v4h-6V7zM4 15h6v4H4v-4zm10 2h6v2h-6v-2z"
          stroke="currentColor"
          stroke-width="1.5"
        />
      </svg>
      <svg
        v-else
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <path
          d="M12 3l2.2 6.6H21l-5.4 3.9 2.1 6.5L12 16.8 6.3 20l2.1-6.5L3 9.6h6.8L12 3z"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linejoin="round"
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
  </article>
</template>
