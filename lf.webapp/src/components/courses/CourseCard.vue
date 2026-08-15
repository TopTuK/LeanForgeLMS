<script setup>
defineProps({
    title: { type: String, required: true },
    description: { type: String, required: true },
    category: { type: String, required: true },
    duration: { type: String, default: '' },
    instructor: { type: String, default: '' },
    status: {
        type: String,
        required: true,
        validator: (value) => ['available', 'active', 'finished', 'teaching'].includes(value),
    },
    progress: { type: Number, default: 0 },
    completedOn: { type: String, default: '' },
    studentsCount: { type: Number, default: 0 },
    busy: { type: Boolean, default: false },
});

defineEmits(['enroll', 'continue', 'manage']);
</script>

<template>
  <article class="flat-card rounded-card p-6 flex flex-col gap-4 h-full hover:border-ink/30 transition">
    <div class="flex items-center gap-2">
      <span
        v-if="duration"
        class="rounded-pill bg-surface-800 px-3 py-1 text-xs font-medium text-ink-muted"
      >{{ duration }}</span>
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
        :disabled="busy"
        class="btn-accent w-full rounded-pill px-4 py-2.5 text-sm font-semibold"
        :class="{ 'opacity-60 cursor-not-allowed': busy }"
        @click="$emit('enroll')"
      >
        {{ $t('courses.available.enroll') }}
      </button>

      <div v-else-if="status === 'active'">
        <div class="flex items-center justify-between text-xs font-medium text-ink-muted mb-1.5">
          <span>{{ $t('courses.active.progress_label', { percent: progress }) }}</span>
        </div>
        <div class="h-2 rounded-pill bg-surface-800 overflow-hidden mb-3">
          <div
            class="h-full rounded-pill bg-accent-coral"
            :style="{ width: `${progress}%` }"
          />
        </div>
        <button
          type="button"
          :disabled="busy"
          class="btn-accent w-full rounded-pill px-4 py-2.5 text-sm font-semibold"
          :class="{ 'opacity-60 cursor-not-allowed': busy }"
          @click="$emit('continue')"
        >
          {{ $t('courses.active.continue') }}
        </button>
      </div>

      <div v-else-if="status === 'finished'">
        <div class="flex items-center gap-2 text-sm font-medium text-ink mb-3">
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
        <button
          type="button"
          class="w-full rounded-pill border border-border-subtle px-4 py-2 text-xs font-semibold text-ink-muted hover:border-ink/30 transition"
          @click="$emit('continue')"
        >
          {{ $t('courses.finished.review') }}
        </button>
      </div>

      <div
        v-else-if="status === 'teaching'"
        class="flex items-center justify-between gap-3"
      >
        <span
          v-if="studentsCount"
          class="text-xs font-medium text-ink-muted"
        >
          {{ $t('courses.teaching.students_count', { count: studentsCount }) }}
        </span>
        <span
          v-else
          class="text-xs font-medium text-ink-muted"
        />
        <button
          type="button"
          class="rounded-pill border border-border-subtle px-4 py-2 text-xs font-semibold text-ink-muted hover:border-ink/30 transition"
          @click="$emit('manage')"
        >
          {{ $t('courses.teaching.manage') }}
        </button>
      </div>
    </div>
  </article>
</template>
