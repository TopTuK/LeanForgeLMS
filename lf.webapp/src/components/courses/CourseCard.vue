<script setup>
import { computed } from 'vue';
import StatusBadge from '@/components/courses/form/StatusBadge.vue';

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

defineEmits(['view-details', 'continue', 'manage']);


const showCoverImage = computed(() => props.coverType === 'Image' && Boolean(props.coverImageUrl));
const hasCover = computed(() => props.coverType === 'Color' || showCoverImage.value);

const coverStyle = computed(() => (
    props.coverType === 'Color' && props.coverColor
        ? { backgroundColor: `var(--color-cover-${props.coverColor.toLowerCase()})` }
        : {}
));
</script>

<template>
  <article
    class="course-card"
    :class="[`course-card--${status}`, { 'is-busy': busy }]"
  >
    <div
      class="course-card__cover"
      :class="{ 'course-card__cover--empty': !hasCover }"
      :style="coverStyle"
    >
      <img
        v-if="showCoverImage"
        :src="coverImageUrl"
        alt=""
        class="course-card__cover-image"
      >
      <div
        v-if="hasCover"
        class="course-card__scrim"
        aria-hidden="true"
      />


      <div
        class="course-card__cover-content"
        :class="{ 'course-card__cover-content--light': hasCover }"
      >
        <div class="course-card__cover-badges">
          <span
            v-if="duration"
            class="course-card__pill course-card__pill--muted"
          >{{ duration }}</span>
          <span class="course-card__pill course-card__pill--accent">{{ category }}</span>
          <StatusBadge
            v-if="status === 'teaching' && isPublished !== null"
            :variant="isPublished ? 'published' : 'draft'"
            :label="isPublished ? $t('courses.editor.published') : $t('courses.editor.draft')"
          />
        </div>

        <div class="course-card__cover-text">
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
      </div>
    </div>

    <div class="course-card__footer">
      <button
        v-if="status === 'available'"
        type="button"
        :disabled="busy"
        class="course-card__submit"
        @click="$emit('view-details')"
      >
        <span>{{ $t('courses.available.view_details') }}</span>
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
  height: 100%;
  overflow: hidden;
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
  z-index: 2;
  filter: drop-shadow(0 0 1px rgba(255, 255, 255, 0.7));
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
  position: relative;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  min-height: 12.5rem;
  overflow: hidden;
  background: var(--color-surface-800);
}

.course-card__cover--empty {
  background-image:
    linear-gradient(var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(135deg, var(--color-surface-900), var(--color-surface-800));
  background-size: 18px 18px, 18px 18px, auto;
}

.course-card__cover-image {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.course-card__scrim {
  position: absolute;
  inset: 0;
  background: linear-gradient(to top, rgba(10, 10, 10, 0.78) 0%, rgba(10, 10, 10, 0.4) 45%, rgba(10, 10, 10, 0) 85%);
}

.course-card__cover-content {
  position: relative;
  z-index: 1;
  display: flex;
  flex: 1;
  flex-direction: column;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.85rem 1.25rem 1.1rem;
}

.course-card__cover-badges {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.4rem;
  padding-right: 2.5rem;
}

.course-card__pill {
  border-radius: var(--radius-pill);
  padding: 0.3rem 0.75rem;
  font-size: 0.72rem;
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(20, 20, 20, 0.18);
}

.course-card__pill--muted {
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
}

.course-card__pill--accent {
  background: #ffffff;
  color: var(--color-accent-coral-dark);
  font-weight: 700;
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

.course-card__cover-content--light .course-card__title,
.course-card__cover-content--light .course-card__instructor,
.course-card__cover-content--light .course-card__description {
  text-shadow: 0 1px 4px rgba(0, 0, 0, 0.35);
}

.course-card__cover-content--light .course-card__title {
  color: #ffffff;
}

.course-card__cover-content--light .course-card__instructor {
  color: rgba(255, 255, 255, 0.78);
}

.course-card__cover-content--light .course-card__description {
  color: rgba(255, 255, 255, 0.9);
}

.course-card__footer {
  margin: 1.1rem 1.5rem 0;
  padding: 1.1rem 0 1.5rem;
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
