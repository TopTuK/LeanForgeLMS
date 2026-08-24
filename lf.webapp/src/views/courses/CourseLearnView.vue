<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { fetchEnrollment, completeLesson, fetchEnrollmentLessonMediaObjectUrl, fetchEnrollmentLessonPartFileObjectUrl } from '@/services/enrollmentService';
import LearnerQuizPart from '@/components/courses/lesson/LearnerQuizPart.vue';

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

const selectedLessonParts = computed(() => {
  const parts = selectedLesson.value?.parts;
  if (!Array.isArray(parts) || parts.length === 0) return [];
  return parts.map((part) => ({
    id: part.id,
    type: String(part.partType).toLowerCase(),
    html: part.html ?? '',
    mediaUrl: part.mediaUrl ?? null,
    quizQuestions: part.quizQuestions ?? [],
    quizPassThreshold: part.quizPassThresholdPercent ?? null,
    files: part.files ?? [],
  }));
});

const hasQuizPart = computed(() => selectedLessonParts.value.some((part) => part.type === 'quiz'));

// Media endpoints require auth, so a plain <img>/<video>/<audio> src can't hit them directly
// (no bearer header on a browser-initiated resource fetch) — blob-fetch and cache per part id.
const mediaObjectUrls = ref({});

function clearMediaObjectUrls() {
  Object.values(mediaObjectUrls.value).forEach((url) => URL.revokeObjectURL(url));
  mediaObjectUrls.value = {};
}

async function loadMediaForSelectedLesson() {
  const lesson = selectedLesson.value;
  if (!lesson) return;

  const pending = selectedLessonParts.value.filter(
    (part) => part.type !== 'text' && part.mediaUrl && !mediaObjectUrls.value[part.id],
  );
  await Promise.all(pending.map(async (part) => {
    try {
      const objectUrl = await fetchEnrollmentLessonMediaObjectUrl(enrollmentId.value, lesson.id, part.id);
      mediaObjectUrls.value = { ...mediaObjectUrls.value, [part.id]: objectUrl };
    } catch {
      // Leave unresolved; the media block just won't render for this part.
    }
  }));
}

onBeforeUnmount(clearMediaObjectUrls);

async function load() {
  loading.value = true;
  notFound.value = false;
  forbidden.value = false;
  errorMessage.value = '';

  try {
    enrollment.value = await fetchEnrollment(enrollmentId.value);
    const firstIncomplete = flatLessons.value.find((l) => !l.isCompleted);
    selectedLessonId.value = (firstIncomplete ?? flatLessons.value[0])?.id ?? null;
    await loadMediaForSelectedLesson();
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
  loadMediaForSelectedLesson();
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

function onQuizSubmitted(updatedEnrollment) {
  enrollment.value = updatedEnrollment;
}

const downloadingFileId = ref(null);
const downloadErrorFileId = ref(null);

// Files parts are downloads, not inline media — fetch on click rather than preloading like
// image/video/audio, then trigger a browser save via a synthetic anchor click.
async function downloadFile(part, file) {
  if (!selectedLesson.value || downloadingFileId.value === file.id) return;

  downloadingFileId.value = file.id;
  downloadErrorFileId.value = null;
  let objectUrl;
  try {
    objectUrl = await fetchEnrollmentLessonPartFileObjectUrl(enrollmentId.value, selectedLesson.value.id, part.id, file.id);
    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = file.fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
  } catch {
    downloadErrorFileId.value = file.id;
  } finally {
    if (objectUrl) URL.revokeObjectURL(objectUrl);
    downloadingFileId.value = null;
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
              v-if="selectedLessonParts.length > 0"
              class="course-learn__parts"
            >
              <template
                v-for="part in selectedLessonParts"
                :key="part.id"
              >
                <!-- eslint-disable-next-line vue/no-v-html -->
                <div
                  v-if="part.type === 'text'"
                  class="course-learn__prose"
                  v-html="part.html"
                />
                <LearnerQuizPart
                  v-else-if="part.type === 'quiz'"
                  :part="part"
                  :enrollment-id="enrollmentId"
                  :lesson-id="selectedLesson.id"
                  @submitted="onQuizSubmitted"
                />
                <ul
                  v-else-if="part.type === 'files'"
                  class="course-learn__files"
                >
                  <li
                    v-for="file in part.files"
                    :key="file.id"
                    class="course-learn__files-item"
                  >
                    <span class="course-learn__files-name">{{ file.fileName }}</span>
                    <button
                      type="button"
                      class="course-learn__files-download"
                      :disabled="downloadingFileId === file.id"
                      @click="downloadFile(part, file)"
                    >
                      {{ downloadingFileId === file.id ? t('courses.learn.files.downloading') : t('courses.learn.files.download') }}
                    </button>
                    <span
                      v-if="downloadErrorFileId === file.id"
                      class="course-learn__files-error"
                    >
                      {{ t('courses.learn.files.download_error') }}
                    </span>
                  </li>
                </ul>
                <div
                  v-else
                  class="course-learn__media"
                >
                  <img
                    v-if="part.type === 'image' && mediaObjectUrls[part.id]"
                    :src="mediaObjectUrls[part.id]"
                    alt=""
                    class="course-learn__media-image"
                  >
                  <video
                    v-else-if="part.type === 'video' && mediaObjectUrls[part.id]"
                    :src="mediaObjectUrls[part.id]"
                    class="course-learn__media-player"
                    controls
                    preload="metadata"
                  />
                  <audio
                    v-else-if="part.type === 'audio' && mediaObjectUrls[part.id]"
                    :src="mediaObjectUrls[part.id]"
                    class="course-learn__media-player course-learn__media-player--audio"
                    controls
                    preload="metadata"
                  />
                </div>
              </template>
            </div>
            <div
              v-else
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
              <p
                v-else-if="hasQuizPart"
                class="course-learn__quiz-hint"
              >
                {{ $t('courses.learn.quiz.complete_via_quiz_hint') }}
              </p>
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
    none;
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
  overflow-wrap: anywhere;
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
  min-width: 0;
  padding: 1.75rem;
  background: var(--industrial-panel);
  border: 1px solid var(--industrial-line-strong);
  border-radius: 0.4rem;
  overflow-wrap: anywhere;
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

.course-learn__parts {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.course-learn__media-image {
  display: block;
  max-width: 100%;
  height: auto;
  border: 1px solid var(--industrial-line);
  border-radius: 0.25rem;
}

.course-learn__media-player {
  display: block;
  width: 100%;
  max-height: 22rem;
  background: var(--color-surface-900);
}

.course-learn__media-player--audio {
  height: 2.6rem;
}

.course-learn__files {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.course-learn__files-item {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
  padding: 0.7rem 0.9rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.55rem;
  background: var(--color-surface-950);
}

.course-learn__files-name {
  overflow: hidden;
  flex: 1 1 auto;
  min-width: 0;
  color: var(--color-ink);
  font-size: 0.9rem;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.course-learn__files-download {
  flex: none;
  padding: 0.35rem 0.75rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.45rem;
  background: transparent;
  color: var(--color-accent-coral-dark);
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
}

.course-learn__files-download:hover:not(:disabled) {
  background: color-mix(in srgb, var(--color-accent-coral) 10%, transparent);
}

.course-learn__files-download:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.course-learn__files-error {
  flex: none;
  color: var(--color-accent-coral-dark);
  font-size: 0.78rem;
  font-weight: 600;
}

.course-learn__prose {
  color: var(--color-ink);
  font-size: 1rem;
  line-height: 1.7;
  overflow-wrap: anywhere;
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

.course-learn__quiz-hint {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
}

@media (min-width: 1024px) {
  .course-learn__layout {
    grid-template-columns: 18rem minmax(0, 1fr);
  }
}
</style>
