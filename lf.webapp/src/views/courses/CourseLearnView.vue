<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { fetchEnrollment, completeLesson } from '@/services/enrollmentService';

const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const enrollmentId = computed(() => Number(route.params.enrollmentId));

const enrollment = ref(null);
const loading = ref(true);
const notFound = ref(false);
const forbidden = ref(false);
const errorMessage = ref('');
const completing = ref(false);
const selectedLessonId = ref(null);

const flatLessons = computed(() => {
  if (!enrollment.value) return [];
  return enrollment.value.chapters.flatMap((chapter) =>
    chapter.lessons.map((lesson) => ({ ...lesson, chapterTitle: chapter.title })));
});

const selectedLesson = computed(() =>
  flatLessons.value.find((l) => l.id === selectedLessonId.value) ?? null);

const totalLessons = computed(() => flatLessons.value.length);
const completedLessonsCount = computed(() => flatLessons.value.filter((l) => l.isCompleted).length);
const progressPercent = computed(() =>
  totalLessons.value === 0 ? 0 : Math.round((completedLessonsCount.value / totalLessons.value) * 100));

const selectedIndex = computed(() => flatLessons.value.findIndex((l) => l.id === selectedLessonId.value));
const nextLesson = computed(() =>
  selectedIndex.value >= 0 ? flatLessons.value[selectedIndex.value + 1] ?? null : null);

async function load() {
  loading.value = true;
  notFound.value = false;
  forbidden.value = false;
  errorMessage.value = '';

  try {
    enrollment.value = await fetchEnrollment(enrollmentId.value);
    const firstIncomplete = flatLessons.value.find((l) => !l.isCompleted);
    selectedLessonId.value = (firstIncomplete ?? flatLessons.value[0])?.id ?? null;
  } catch (err) {
    if (err.response?.status === 404) notFound.value = true;
    else if (err.response?.status === 403) forbidden.value = true;
    else errorMessage.value = t('courses.learn.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(() => route.params.enrollmentId, load);

function selectLesson(lessonId) {
  selectedLessonId.value = lessonId;
}

async function markComplete() {
  if (!selectedLesson.value || selectedLesson.value.isCompleted) return;

  completing.value = true;
  errorMessage.value = '';
  try {
    enrollment.value = await completeLesson(enrollmentId.value, selectedLesson.value.id);
    if (nextLesson.value) selectedLessonId.value = nextLesson.value.id;
  } catch {
    errorMessage.value = t('courses.learn.complete_error');
  } finally {
    completing.value = false;
  }
}

function goToCourses() {
  router.push({ name: 'CoursesActive' });
}
</script>

<template>
  <div class="course-learn">
    <div class="container mx-auto px-6 py-10 md:py-14">
      <p
        v-if="loading"
        class="course-learn__hint"
      >
        {{ $t('courses.learn.loading') }}
      </p>

      <div
        v-else-if="notFound"
        class="course-learn__state"
      >
        <h1>{{ $t('courses.learn.not_found') }}</h1>
        <button
          type="button"
          class="course-learn__back"
          @click="goToCourses"
        >
          {{ $t('courses.learn.back') }}
        </button>
      </div>

      <div
        v-else-if="forbidden"
        class="course-learn__state"
      >
        <h1>{{ $t('courses.learn.forbidden') }}</h1>
        <button
          type="button"
          class="course-learn__back"
          @click="goToCourses"
        >
          {{ $t('courses.learn.back') }}
        </button>
      </div>

      <template v-else-if="enrollment">
        <header class="course-learn__header">
          <div>
            <p class="course-learn__eyebrow">
              {{ $t('courses.learn.eyebrow') }}
            </p>
            <h1>{{ enrollment.courseTitle }}</h1>
          </div>
          <div class="course-learn__progress">
            <span>{{ $t('courses.active.progress_label', { percent: progressPercent }) }}</span>
            <div class="course-learn__progress-bar">
              <div
                class="course-learn__progress-fill"
                :style="{ width: `${progressPercent}%` }"
              />
            </div>
          </div>
        </header>

        <p
          v-if="errorMessage"
          class="course-learn__alert"
          role="alert"
        >
          {{ errorMessage }}
        </p>

        <div class="course-learn__layout">
          <aside class="course-learn__rail">
            <div
              v-for="chapter in enrollment.chapters"
              :key="chapter.id"
              class="course-learn__chapter"
            >
              <h2 class="course-learn__chapter-title">
                {{ chapter.title }}
              </h2>
              <button
                v-for="lesson in chapter.lessons"
                :key="lesson.id"
                type="button"
                class="course-learn__lesson"
                :class="{
                  'course-learn__lesson--active': lesson.id === selectedLessonId,
                  'course-learn__lesson--done': lesson.isCompleted,
                }"
                @click="selectLesson(lesson.id)"
              >
                <span
                  class="course-learn__lesson-dot"
                  aria-hidden="true"
                />
                {{ lesson.title }}
              </button>
            </div>
          </aside>

          <section
            v-if="selectedLesson"
            class="course-learn__content"
          >
            <div class="course-learn__content-header">
              <p class="course-learn__breadcrumb">
                {{ selectedLesson.chapterTitle }}
              </p>
              <h2>{{ selectedLesson.title }}</h2>
            </div>

            <div
              class="course-learn__prose"
              v-html="selectedLesson.content"
            />

            <div class="course-learn__actions">
              <button
                v-if="selectedLesson.isCompleted"
                type="button"
                class="course-learn__complete-btn course-learn__complete-btn--done"
                disabled
              >
                {{ $t('courses.learn.completed') }}
              </button>
              <button
                v-else
                type="button"
                class="course-learn__complete-btn"
                :disabled="completing"
                @click="markComplete"
              >
                {{ completing ? $t('courses.learn.completing') : $t('courses.learn.mark_complete') }}
              </button>
            </div>
          </section>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.course-learn {
  min-height: calc(100vh - 4.5rem);
  background-color: var(--color-surface-950);
  background-image:
    linear-gradient(var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--industrial-grid) 1px, transparent 1px);
  background-size: 40px 40px;
}

.course-learn__hint {
  margin: 2rem 0;
  color: var(--color-ink);
  font-size: 0.95rem;
}

.course-learn__state {
  max-width: 36rem;
  padding: 2rem 0;
}

.course-learn__state h1 {
  margin: 0 0 1.25rem;
  color: var(--color-ink);
  font-size: 1.75rem;
  font-weight: 800;
}

.course-learn__back {
  display: inline-flex;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  text-decoration: underline;
  text-underline-offset: 0.2em;
}

.course-learn__header {
  display: flex;
  flex-wrap: wrap;
  align-items: end;
  justify-content: space-between;
  gap: 1.25rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid var(--industrial-line);
}

.course-learn__eyebrow {
  margin: 0 0 0.5rem;
  color: var(--color-accent-coral);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.course-learn__header h1 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.6rem, 2.4vw, 2.1rem);
  font-weight: 800;
  letter-spacing: -0.03em;
}

.course-learn__progress {
  min-width: 14rem;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  color: var(--color-ink-muted);
  font-size: 0.8rem;
  font-weight: 600;
}

.course-learn__progress-bar {
  height: 0.4rem;
  border-radius: 999px;
  background: var(--color-surface-800);
  overflow: hidden;
}

.course-learn__progress-fill {
  height: 100%;
  border-radius: 999px;
  background: var(--color-accent-coral);
  transition: width 0.2s ease;
}

.course-learn__alert {
  margin-bottom: 1.25rem;
  padding: 0.85rem 1rem;
  color: var(--color-ink);
  background: color-mix(in srgb, var(--color-accent-coral) 12%, var(--color-surface-950));
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.35rem;
}

.course-learn__layout {
  display: grid;
  gap: 1.25rem;
  grid-template-columns: minmax(0, 1fr);
  align-items: start;
}

.course-learn__rail {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  padding: 1.25rem;
  background: var(--industrial-panel);
  border: 1px solid var(--industrial-line-strong);
  border-radius: 0.4rem;
}

.course-learn__chapter-title {
  margin: 0 0 0.5rem;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.course-learn__lesson {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  width: 100%;
  padding: 0.55rem 0.65rem;
  border: 1px solid transparent;
  border-radius: 0.3rem;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
  text-align: left;
  cursor: pointer;
  transition: background-color 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}

.course-learn__lesson:hover {
  background: var(--color-surface-900);
  color: var(--color-ink);
}

.course-learn__lesson--active {
  background: var(--industrial-accent-wash);
  border-color: var(--color-accent-coral);
  color: var(--color-ink);
}

.course-learn__lesson-dot {
  width: 0.55rem;
  height: 0.55rem;
  flex-shrink: 0;
  border-radius: 999px;
  border: 1.5px solid var(--color-ink-faint);
}

.course-learn__lesson--done .course-learn__lesson-dot {
  background: var(--color-accent-coral);
  border-color: var(--color-accent-coral);
}

.course-learn__content {
  padding: 1.75rem;
  background: var(--industrial-panel);
  border: 1px solid var(--industrial-line-strong);
  border-radius: 0.4rem;
}

.course-learn__content-header {
  margin-bottom: 1.25rem;
  padding-bottom: 1.1rem;
  border-bottom: 1px solid var(--industrial-line);
}

.course-learn__breadcrumb {
  margin: 0 0 0.4rem;
  color: var(--color-ink-faint);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.course-learn__content-header h2 {
  margin: 0;
  color: var(--color-ink);
  font-size: 1.4rem;
  font-weight: 800;
  letter-spacing: -0.02em;
}

.course-learn__prose {
  color: var(--color-ink);
  font-size: 1rem;
  line-height: 1.7;
}

.course-learn__prose :deep(h2) {
  margin: 1.25rem 0 0.65rem;
  font-size: 1.35rem;
  font-weight: 800;
}

.course-learn__prose :deep(h3) {
  margin: 1.1rem 0 0.5rem;
  font-size: 1.1rem;
  font-weight: 700;
}

.course-learn__prose :deep(p) {
  margin: 0.55rem 0;
}

.course-learn__prose :deep(ul),
.course-learn__prose :deep(ol) {
  margin: 0.55rem 0;
  padding-left: 1.35rem;
}

.course-learn__prose :deep(blockquote) {
  margin: 0.85rem 0;
  padding: 0.35rem 0 0.35rem 0.95rem;
  border-left: 3px solid var(--color-accent-coral);
  color: var(--color-ink-muted);
}

.course-learn__prose :deep(code) {
  padding: 0.12rem 0.35rem;
  border-radius: 0.2rem;
  background: var(--color-surface-900);
  border: 1px solid var(--industrial-line);
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 0.88em;
}

.course-learn__prose :deep(img) {
  display: block;
  max-width: 100%;
  height: auto;
  margin: 0.85rem 0;
  border: 1px solid var(--industrial-line);
  border-radius: 0.25rem;
}

.course-learn__actions {
  margin-top: 1.5rem;
  padding-top: 1.25rem;
  border-top: 1px solid var(--industrial-line);
}

.course-learn__complete-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.7rem 1.4rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 999px;
  background: var(--color-accent-coral);
  color: #ffffff;
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.course-learn__complete-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 8px 20px rgba(236, 104, 60, 0.22);
}

.course-learn__complete-btn:disabled {
  cursor: not-allowed;
  opacity: 0.75;
}

.course-learn__complete-btn--done {
  background: transparent;
  color: var(--color-ink-muted);
  border-color: var(--color-border-subtle);
}

@media (min-width: 1024px) {
  .course-learn__layout {
    grid-template-columns: 18rem minmax(0, 1fr);
  }
}
</style>
