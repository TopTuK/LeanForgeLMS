<script setup>
defineProps({
    title: { type: String, required: true },
    description: { type: String, required: true },
    category: { type: String, required: true },
    duration: { type: String, required: true },
    instructor: { type: String, default: '' },
    status: {
        type: String,
        required: true,
        validator: (value) => ['available', 'active', 'finished', 'teaching'].includes(value),
    },
    progress: { type: Number, default: 0 },
    completedOn: { type: String, default: '' },
    studentsCount: { type: Number, default: 0 },
});
</script>

<template>
  <article class="flat-card rounded-card p-6 flex flex-col gap-4 h-full hover:border-ink/30 transition">
    <div class="flex items-center gap-2">
      <span class="rounded-pill bg-surface-800 px-3 py-1 text-xs font-medium text-ink-muted">{{ duration }}</span>
      <span class="rounded-pill bg-accent-soft px-3 py-1 text-xs font-semibold text-accent-coral">{{ category }}</span>
    </div>

    <div>
      <h3 class="font-semibold text-ink text-lg leading-snug">
        {{ title }}
      </h3>
      <p
        v-if="instructor"
        class="mt-1 text-xs text-ink-faint"
      >
        {{ instructor }}
      </p>
    </div>

    <p class="text-sm text-ink-muted flex-1 leading-relaxed">
      {{ description }}
    </p>

    <div class="pt-4 border-t border-border-subtle">
      <button
        v-if="status === 'available'"
        type="button"
        disabled
        class="btn-accent w-full rounded-pill px-4 py-2.5 text-sm font-semibold opacity-60 cursor-not-allowed"
      >
        {{ $t('courses.available.enroll') }}
      </button>

      <div v-else-if="status === 'active'">
        <div class="flex items-center justify-between text-xs font-medium text-ink-muted mb-1.5">
          <span>{{ $t('courses.active.progress_label', { percent: progress }) }}</span>
        </div>
        <div class="h-2 rounded-pill bg-surface-800 overflow-hidden">
          <div
            class="h-full rounded-pill bg-accent-coral"
            :style="{ width: `${progress}%` }"
          />
        </div>
      </div>

      <div
        v-else-if="status === 'finished'"
        class="flex items-center gap-2 text-sm font-medium text-ink"
      >
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          aria-hidden="true"
          class="text-accent-coral shrink-0"
        >
          <circle
            cx="12"
            cy="12"
            r="9"
            stroke="currentColor"
            stroke-width="1.5"
          />
          <path
            d="M8 12.5L10.5 15L16 9"
            stroke="currentColor"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        </svg>
        {{ $t('courses.finished.completed_on', { date: completedOn }) }}
      </div>

      <div
        v-else-if="status === 'teaching'"
        class="flex items-center justify-between gap-3"
      >
        <span class="text-xs font-medium text-ink-muted">
          {{ $t('courses.teaching.students_count', { count: studentsCount }) }}
        </span>
        <button
          type="button"
          disabled
          class="rounded-pill border border-border-subtle px-4 py-2 text-xs font-semibold text-ink-muted opacity-60 cursor-not-allowed"
        >
          {{ $t('courses.teaching.manage') }}
        </button>
      </div>
    </div>
  </article>
</template>
