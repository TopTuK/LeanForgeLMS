<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { ChevronLeft, ChevronRight, PanelRightClose, PanelRightOpen } from 'lucide-vue-next';
import {
  fetchEnrollment,
  completeLesson,
  fetchEnrollmentLessonMediaObjectUrl,
  fetchEnrollmentLessonPartFileObjectUrl,
} from '@/services/enrollmentService';
import LearnerQuizPart from '@/components/courses/lesson/LearnerQuizPart.vue';
import CourseOutlineRail from '@/components/courses/learn/CourseOutlineRail.vue';

const OUTLINE_STORAGE_KEY = 'course-learn-outline-collapsed';

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

const isMobile = ref(false);
const outlineCollapsed = ref(false);

function readStoredCollapse() {
  try {
    return localStorage.getItem(OUTLINE_STORAGE_KEY) === '1';
  } catch {
    return false;
  }
}

function persistCollapse(collapsed) {
  try {
    localStorage.setItem(OUTLINE_STORAGE_KEY, collapsed ? '1' : '0');
  } catch {
    // Ignore storage failures (private mode, quota).
  }
}

function updateViewport() {
  isMobile.value = window.matchMedia('(max-width: 1023px)').matches;
}

function toggleOutline() {
  outlineCollapsed.value = !outlineCollapsed.value;
  if (!isMobile.value) persistCollapse(outlineCollapsed.value);
}

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
const previousLesson = computed(() =>
  selectedIndex.value > 0 ? flatLessons.value[selectedIndex.value - 1] ?? null : null);
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

onMounted(() => {
  updateViewport();
  outlineCollapsed.value = isMobile.value ? true : readStoredCollapse();
  window.addEventListener('resize', updateViewport);
  load();
});

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateViewport);
  clearMediaObjectUrls();
});

watch(isMobile, (mobile) => {
  if (mobile) outlineCollapsed.value = true;
  else outlineCollapsed.value = readStoredCollapse();
});

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

watch(() => route.params.enrollmentId, load);

function selectLesson(lessonId) {
  selectedLessonId.value = lessonId;
  loadMediaForSelectedLesson();
  if (isMobile.value) outlineCollapsed.value = true;
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

async function downloadFile(part, file) {
  if (!selectedLesson.value || downloadingFileId.value === file.id) return;

  downloadingFileId.value = file.id;
  downloadErrorFileId.value = null;
  let objectUrl;
  try {
    objectUrl = await fetchEnrollmentLessonPartFileObjectUrl(
      enrollmentId.value,
      selectedLesson.value.id,
      part.id,
      file.id,
    );
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
  <div
    class="course-learn"
    :class="{ 'course-learn--outline-hidden': outlineCollapsed }"
  >
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
        class="course-learn__text-btn"
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
        class="course-learn__text-btn"
        @click="goToCourses"
      >
        {{ $t('courses.learn.back') }}
      </button>
    </div>

    <template v-else-if="enrollment">
      <header class="course-learn__header">
        <div class="course-learn__header-main">
          <button
            type="button"
            class="course-learn__text-btn"
            @click="goToCourses"
          >
            {{ $t('courses.learn.back') }}
          </button>
          <h1>{{ enrollment.courseTitle }}</h1>
          <div class="course-learn__progress">
            <span>{{ $t('courses.active.progress_label', { percent: progressPercent }) }}</span>
            <div class="course-learn__progress-bar">
              <div
                class="course-learn__progress-fill"
                :style="{ width: `${progressPercent}%` }"
              />
            </div>
          </div>
        </div>

        <button
          type="button"
          class="course-learn__structure-btn"
          :aria-pressed="!outlineCollapsed"
          @click="toggleOutline"
        >
          <PanelRightOpen
            v-if="outlineCollapsed"
            :size="16"
          />
          <PanelRightClose
            v-else
            :size="16"
          />
          {{ outlineCollapsed ? $t('courses.learn.show_structure') : $t('courses.learn.hide_structure') }}
        </button>
      </header>

      <p
        v-if="errorMessage"
        class="course-learn__alert"
        role="alert"
      >
        {{ errorMessage }}
      </p>

      <div class="course-learn__body">
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
          <!-- eslint-disable-next-line vue/no-v-html -->
          <div
            v-else
            class="course-learn__prose"
            v-html="selectedLesson.content"
          />

          <div class="course-learn__actions">
            <button
              type="button"
              class="course-learn__nav-btn"
              :disabled="!previousLesson"
              @click="previousLesson && selectLesson(previousLesson.id)"
            >
              <ChevronLeft :size="16" />
              {{ $t('courses.learn.previous') }}
            </button>

            <div class="course-learn__actions-center">
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

            <button
              type="button"
              class="course-learn__nav-btn"
              :disabled="!nextLesson"
              @click="nextLesson && selectLesson(nextLesson.id)"
            >
              {{ $t('courses.learn.next') }}
              <ChevronRight :size="16" />
            </button>
          </div>
        </section>

        <div
          v-if="!outlineCollapsed"
          class="course-learn__rail-wrap"
        >
          <button
            v-if="isMobile"
            type="button"
            class="course-learn__rail-backdrop"
            :aria-label="$t('courses.learn.hide_structure')"
            @click="outlineCollapsed = true"
          />
          <CourseOutlineRail
            class="course-learn__rail"
            :chapters="enrollment.chapters"
            :selected-lesson-id="selectedLessonId"
            :title="$t('courses.learn.structure_title')"
            @select="selectLesson"
          />
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.course-learn {
  min-height: calc(100vh - 4.5rem);
  background: var(--color-surface-950);
  padding: 1.25rem 1.25rem 2.5rem;
}

@media (min-width: 768px) {
  .course-learn {
    padding: 1.5rem 1.75rem 3rem;
  }
}

.course-learn__hint {
  margin: 2rem 0;
  color: var(--color-ink-muted);
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

.course-learn__text-btn {
  display: inline-flex;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-ink-muted);
  font: inherit;
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
}

.course-learn__text-btn:hover {
  color: var(--color-ink);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.course-learn__header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--color-border-subtle);
}

.course-learn__header-main {
  min-width: 0;
  flex: 1;
}

.course-learn__header h1 {
  margin: 0.35rem 0 0.65rem;
  color: var(--color-ink);
  font-size: clamp(1.35rem, 2.2vw, 1.75rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.2;
}

.course-learn__progress {
  max-width: 16rem;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 600;
}

.course-learn__progress-bar {
  height: 0.35rem;
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

.course-learn__structure-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.45rem 0.7rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.45rem;
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
  font: inherit;
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
}

.course-learn__structure-btn:hover {
  color: var(--color-ink);
  background: var(--color-surface-900);
}

.course-learn__alert {
  margin-bottom: 1rem;
  padding: 0.75rem 1rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.5rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral-dark);
  font-size: 0.9rem;
  font-weight: 600;
}

.course-learn__body {
  display: grid;
  gap: 0;
  align-items: start;
}

@media (min-width: 1024px) {
  .course-learn__body {
    grid-template-columns: minmax(0, 1fr) 17.5rem;
    gap: 1.5rem;
  }

  .course-learn--outline-hidden .course-learn__body {
    grid-template-columns: minmax(0, 1fr);
  }
}

.course-learn__content {
  min-width: 0;
  width: 100%;
  max-width: 48rem;
  margin-inline: auto;
  padding: 0.25rem 0 1rem;
}

.course-learn--outline-hidden .course-learn__content {
  max-width: 44rem;
}

.course-learn__content-header {
  margin-bottom: 1.35rem;
}

.course-learn__breadcrumb {
  margin: 0 0 0.35rem;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 600;
}

.course-learn__content-header h2 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.35rem, 2vw, 1.65rem);
  font-weight: 800;
  letter-spacing: -0.02em;
  line-height: 1.25;
}

.course-learn__parts {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.course-learn__prose {
  color: var(--color-ink);
  font-size: 1.02rem;
  line-height: 1.7;
  overflow-wrap: anywhere;
}

.course-learn__prose :deep(h1) {
  margin: 1.35rem 0 0.65rem;
  font-size: 1.65rem;
  font-weight: 800;
  letter-spacing: -0.03em;
}

.course-learn__prose :deep(h2) {
  margin: 1.25rem 0 0.55rem;
  font-size: 1.35rem;
  font-weight: 800;
}

.course-learn__prose :deep(h3) {
  margin: 1.1rem 0 0.45rem;
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

.course-learn__prose :deep(a) {
  color: var(--color-accent-coral-dark);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.course-learn__prose :deep(mark) {
  background: color-mix(in srgb, var(--color-accent-coral) 28%, transparent);
  border-radius: 0.15rem;
  padding: 0.05em 0.15em;
}

.course-learn__prose :deep(img) {
  display: block;
  max-width: 100%;
  height: auto;
  margin: 0.85rem 0;
  border-radius: 0.45rem;
}

.course-learn__media-image,
.course-learn__media-player {
  display: block;
  width: 100%;
  max-width: 100%;
  border-radius: 0.5rem;
}

.course-learn__media-player--audio {
  height: 2.75rem;
}

.course-learn__files {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.course-learn__files-item {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.65rem;
  padding: 0.65rem 0.75rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.5rem;
}

.course-learn__files-name {
  flex: 1;
  min-width: 8rem;
  font-size: 0.9rem;
  font-weight: 600;
  overflow-wrap: anywhere;
}

.course-learn__files-download {
  padding: 0.35rem 0.65rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.4rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  font: inherit;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
}

.course-learn__files-download:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.course-learn__files-error {
  width: 100%;
  color: var(--color-accent-coral-dark);
  font-size: 0.8rem;
}

.course-learn__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.85rem;
  margin-top: 2rem;
  padding-top: 1.25rem;
  border-top: 1px solid var(--color-border-subtle);
}

.course-learn__actions-center {
  display: flex;
  justify-content: center;
  flex: 1;
  min-width: 10rem;
}

.course-learn__nav-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.45rem 0.55rem;
  border: 0;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink-muted);
  font: inherit;
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
}

.course-learn__nav-btn:hover:not(:disabled) {
  color: var(--color-ink);
  background: var(--color-surface-900);
}

.course-learn__nav-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.course-learn__complete-btn {
  padding: 0.55rem 1.1rem;
  border: 0;
  border-radius: 0.5rem;
  background: var(--color-accent-coral);
  color: #fff;
  font: inherit;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
}

.course-learn__complete-btn:hover:not(:disabled) {
  background: var(--color-accent-coral-dark);
}

.course-learn__complete-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.course-learn__complete-btn--done {
  background: var(--color-surface-800);
  color: var(--color-ink-muted);
}

.course-learn__quiz-hint {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
  text-align: center;
}

.course-learn__rail-wrap {
  position: relative;
}

@media (max-width: 1023px) {
  .course-learn__rail-wrap {
    position: fixed;
    inset: 0;
    z-index: 40;
    display: flex;
    justify-content: flex-end;
  }

  .course-learn__rail-backdrop {
    position: absolute;
    inset: 0;
    border: 0;
    background: rgb(15 23 42 / 0.35);
    cursor: pointer;
  }

  .course-learn__rail {
    position: relative;
    z-index: 1;
    width: min(20rem, 88vw);
    height: 100%;
    background: var(--color-surface-950);
    border-left: 1px solid var(--color-border-subtle);
    box-shadow: -12px 0 32px -20px rgb(15 23 42 / 0.4);
  }
}

@media (min-width: 1024px) {
  .course-learn__rail {
    position: sticky;
    top: 1rem;
    max-height: calc(100vh - 6rem);
    border: 1px solid var(--color-border-subtle);
    border-radius: 0.65rem;
    background: var(--color-surface-900);
  }
}
</style>
