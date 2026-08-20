<script setup>
defineProps({
    icon: {
        type: String,
        required: true,
        validator: (value) => ['developer', 'manager', 'analyst', 'other'].includes(value),
    },
    title: { type: String, required: true },
    description: { type: String, required: true },
});
</script>

<template>
  <article class="flat-card rounded-card p-6 flex flex-col gap-4 h-full">
    <div class="w-11 h-11 rounded-md bg-accent-soft flex items-center justify-center text-accent-coral">
      <svg
        v-if="icon === 'developer'"
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <path
          d="M8 8L3 12L8 16M16 8L21 12L16 16M14 4L10 20"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
      <svg
        v-else-if="icon === 'manager'"
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <path
          d="M4 6h16M4 6v13a1 1 0 0 0 1 1h6M4 6l1.5-3h13L20 6"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
        <path
          d="M15 14l2.5 2.5L22 12"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
      <svg
        v-else-if="icon === 'analyst'"
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <path
          d="M4 19V5M4 19h16M8 19v-6M12.5 19V9M17 19v-9"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
      <svg
        v-else
        width="22"
        height="22"
        viewBox="0 0 24 24"
        fill="none"
      >
        <rect
          x="3"
          y="3"
          width="8"
          height="8"
          rx="1.5"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <rect
          x="13"
          y="3"
          width="8"
          height="8"
          rx="1.5"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <rect
          x="3"
          y="13"
          width="8"
          height="8"
          rx="1.5"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <rect
          x="13"
          y="13"
          width="8"
          height="8"
          rx="1.5"
          stroke="currentColor"
          stroke-width="1.5"
        />
      </svg>
    </div>

    <div>
      <h3 class="font-semibold text-ink text-lg leading-snug">
        {{ title }}
      </h3>
      <p class="mt-2 text-sm text-ink-muted leading-relaxed">
        {{ description }}
      </p>
    </div>
  </article>
</template>
