<script setup>
import ForgeStatusStamp from '@/components/courses/forge/ForgeStatusStamp.vue';

const props = defineProps({
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
    index: { type: Number, default: null },
    isPublished: { type: Boolean, default: null },
    coverType: { type: String, default: 'None' },
    coverColor: { type: String, default: null },
    coverImageUrl: { type: String, default: null },
});

defineEmits(['enroll', 'continue', 'manage']);

const cardMark = (() => {
    if (props.index === null) return '';
    return String(props.index + 1).padStart(2, '0');
})();
</script>

<template>
  <article
    class="course-card"
    :class="[`course-card--${status}`, { 'is-busy': busy }]"
  >
    <span
      v-if="cardMark"
      class="course-card__mark"
      aria-hidden="true"
    >{{ cardMark }}</span>

    <div
      v-if="coverType === 'Color'"
      class="course-card__cover"
      :style="{ backgroundColor: `var(--color-cover-${coverColor?.toLowerCase()})` }"
      aria-hidden="true"
    />
    <img
      v-else-if="coverType === 'Image' && coverImageUrl"
      :src="coverImageUrl"
      alt=""
      class="course-card__cover course-card__cover--image"
    >

    <div class="course-card__tags">
      <span
        v-if="duration"
        class="course-card__pill course-card__pill--muted"
      >{{ duration }}</span>
      <span class="course-card__pill course-card__pill--accent">{{ category }}</span>
      <ForgeStatusStamp
        v-if="status === 'teaching' && isPublished !== null"
        :variant="isPublished ? 'published' : 'draft'"
        :label="isPublished ? $t('courses.editor.published') : $t('courses.editor.draft')"
      />
    </div>

    <div class="course-card__body">
      <h3 class="course-card__title">
        {{ title }}
      </h3>
      <p
        v-if="instructor"
        class="course-card__instructor"
      >
        {{ instructor }}
      </p>
      <p class="course-card__description">
        {{ description }}
      </p>
    </div>

    <div class="course-card__footer">
      <button
        v-if="status === 'available'"
        type="button"
        :disabled="busy"
        class="course-card__submit"
        @click="$emit('enroll')"
      >
        <span>{{ $t('courses.available.enroll') }}</span>
        <span aria-hidden="true">→</span>
      </button>

      <div v-else-if="status === 'active'">
        <div class="course-card__gauge-head">
          <span>{{ $t('courses.active.progress_label', { percent: progress }) }}</span>
        </div>
        <div class="course-card__gauge">
          <div
            class="course-card__gauge-fill"
            :style="{ width: `${progress}%` }"
          />
        </div>
        <button
          type="button"
          :disabled="busy"
          class="course-card__submit"
          @click="$emit('continue')"
        >
          <span>{{ $t('courses.active.continue') }}</span>
          <span aria-hidden="true">→</span>
        </button>
      </div>

      <div v-else-if="status === 'finished'">
        <div class="course-card__done">
          <svg
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            aria-hidden="true"
            class="shrink-0"
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
          class="course-card__outline"
          @click="$emit('continue')"
        >
          {{ $t('courses.finished.review') }}
        </button>
      </div>

      <div
        v-else-if="status === 'teaching'"
        class="course-card__teaching"
      >
        <span
          v-if="studentsCount"
          class="course-card__students"
        >
          {{ $t('courses.teaching.students_count', { count: studentsCount }) }}
        </span>
        <span v-else />
        <button
          type="button"
          class="course-card__outline"
          @click="$emit('manage')"
        >
          {{ $t('courses.teaching.manage') }}
        </button>
      </div>
    </div>
  </article>
</template>

<style scoped>
.course-card {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
  height: 100%;
  padding: 1.5rem;
  background: var(--industrial-panel);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  backdrop-filter: blur(8px);
  transition: border-color 0.18s ease, transform 0.18s ease, box-shadow 0.18s ease;
}

.course-card::before,
.course-card::after {
  content: "";
  position: absolute;
  width: 0.85rem;
  height: 0.85rem;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.18s ease;
}

.course-card::before {
  top: 0.55rem;
  left: 0.55rem;
  border-top: 1.5px solid var(--color-accent-coral);
  border-left: 1.5px solid var(--color-accent-coral);
}

.course-card::after {
  bottom: 0.55rem;
  right: 0.55rem;
  border-bottom: 1.5px solid var(--color-accent-coral);
  border-right: 1.5px solid var(--color-accent-coral);
}

.course-card:hover {
  border-color: var(--industrial-line-strong);
  transform: translateY(-2px);
  box-shadow: 0 14px 34px rgba(20, 20, 20, 0.08);
}

.course-card:hover::before,
.course-card:hover::after {
  opacity: 1;
}

.course-card__cover {
  height: 5rem;
  margin: -1.5rem -1.5rem 0;
  border-radius: var(--radius-card) var(--radius-card) 0 0;
}

.course-card__cover--image {
  width: calc(100% + 3rem);
  object-fit: cover;
}

.course-card__mark {
  position: absolute;
  top: 1.1rem;
  right: 1.25rem;
  color: var(--color-ink-faint);
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.12em;
}

.course-card__tags {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
  padding-right: 1.75rem;
}

.course-card__pill {
  border-radius: var(--radius-pill);
  padding: 0.3rem 0.75rem;
  font-size: 0.72rem;
  font-weight: 600;
}

.course-card__pill--muted {
  background: var(--color-surface-800);
  color: var(--color-ink-muted);
}

.course-card__pill--accent {
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
  font-weight: 700;
}

.course-card__body {
  flex: 1;
}

.course-card__title {
  margin: 0;
  color: var(--color-ink);
  font-size: 1.1rem;
  font-weight: 700;
  line-height: 1.3;
}

.course-card__instructor {
  margin: 0.25rem 0 0;
  color: var(--color-ink-faint);
  font-size: 0.75rem;
}

.course-card__description {
  margin: 0.65rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
  line-height: 1.55;
}

.course-card__footer {
  padding-top: 1.1rem;
  border-top: 1px dashed var(--industrial-line);
}

.course-card__submit {
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.7rem 1rem;
  border: 0;
  border-radius: var(--radius-pill);
  background: var(--color-accent-coral);
  color: #ffffff;
  font-family: inherit;
  font-size: 0.88rem;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.15s ease, transform 0.12s ease, opacity 0.15s ease;
}

.course-card__submit:hover:not(:disabled) {
  background-color: var(--color-accent-coral-dark);
  transform: translateY(-1px);
}

.course-card__submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.course-card__outline {
  width: 100%;
  padding: 0.6rem 1rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-pill);
  background: transparent;
  color: var(--color-ink-muted);
  font-family: inherit;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
  transition: color 0.15s ease, border-color 0.15s ease;
}

.course-card__outline:hover {
  color: var(--color-ink);
  border-color: var(--color-ink-faint);
}

.course-card__gauge-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.5rem;
  color: var(--color-ink-muted);
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.03em;
}

.course-card__gauge {
  position: relative;
  height: 0.5rem;
  margin-bottom: 0.9rem;
  overflow: hidden;
  border-radius: var(--radius-pill);
  background: var(--color-surface-800);
  background-image: repeating-linear-gradient(
    90deg,
    transparent,
    transparent calc(10% - 1px),
    var(--color-border-subtle) 10%
  );
}

.course-card__gauge-fill {
  height: 100%;
  border-radius: var(--radius-pill);
  background: var(--color-accent-coral);
  transition: width 0.25s ease;
}

.course-card__done {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.9rem;
  color: var(--color-ink);
  font-size: 0.85rem;
  font-weight: 600;
}

.course-card__done svg {
  color: var(--color-accent-coral);
}

.course-card__teaching {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.course-card__teaching .course-card__outline {
  width: auto;
  padding: 0.55rem 1.1rem;
}

.course-card__students {
  color: var(--color-ink-muted);
  font-size: 0.75rem;
  font-weight: 600;
}
</style>
