<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import {
  fetchCourse,
  fetchCourseCoverImageObjectUrl,
  addChapter,
  renameChapter,
  moveChapter,
  addLesson,
  moveLesson,
  removeLesson,
  publishCourse,
} from '@/services/courseService';
import ForgeField from '@/components/courses/forge/ForgeField.vue';
import ForgeDialog from '@/components/courses/forge/ForgeDialog.vue';
import ForgeStatusStamp from '@/components/courses/forge/ForgeStatusStamp.vue';

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const courseId = Number(route.params.id);

const course = ref(null);
const loading = ref(true);
const notFound = ref(false);
const forbidden = ref(false);
const errorMessage = ref('');
const publishing = ref(false);
const coverImageUrl = ref('');

async function loadCourse() {
  loading.value = true;
  notFound.value = false;
  forbidden.value = false;
  errorMessage.value = '';
  try {
    course.value = await fetchCourse(courseId);
    if (course.value.coverType === 'Image') {
      coverImageUrl.value = await fetchCourseCoverImageObjectUrl(courseId);
    }
  } catch (err) {
    if (err.response?.status === 404) notFound.value = true;
    else if (err.response?.status === 403) forbidden.value = true;
    else errorMessage.value = t('courses.editor.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(loadCourse);

onBeforeUnmount(() => {
  if (coverImageUrl.value) URL.revokeObjectURL(coverImageUrl.value);
});

async function runMutation(action) {
  errorMessage.value = '';
  try {
    course.value = await action();
  } catch (err) {
    if (err.response?.status === 409) errorMessage.value = t('courses.editor.publish_error');
    else if (err.response?.status === 403) errorMessage.value = t('courses.editor.forbidden');
    else errorMessage.value = t('courses.editor.save_error');
  }
}

const chapterCountLabel = computed(() => {
  if (!course.value) return '';
  return String(course.value.chapters.length).padStart(2, '0');
});

function chapterIndex(index) {
  return `CH—${String(index + 1).padStart(2, '0')}`;
}

// Add chapter
const addChapterModalShown = ref(false);
const newChapterTitle = ref('');

function openAddChapterModal() {
  newChapterTitle.value = '';
  addChapterModalShown.value = true;
}

function confirmAddChapter() {
  if (!newChapterTitle.value.trim()) return;
  runMutation(() => addChapter(courseId, newChapterTitle.value));
}

// Rename chapter
const renameChapterModalShown = ref(false);
const renameChapterTarget = ref(null);
const renameChapterTitleInput = ref('');

function openRenameChapterModal(chapter) {
  renameChapterTarget.value = chapter;
  renameChapterTitleInput.value = chapter.title;
  renameChapterModalShown.value = true;
}

function confirmRenameChapter() {
  if (!renameChapterTitleInput.value.trim()) return;
  runMutation(() => renameChapter(courseId, renameChapterTarget.value.id, renameChapterTitleInput.value));
}

function moveChapterDirection(chapter, direction) {
  runMutation(() => moveChapter(courseId, chapter.id, direction));
}

// Add lesson
const addLessonModalShown = ref(false);
const addLessonChapter = ref(null);
const newLessonTitle = ref('');
const newLessonIncludeInPreview = ref(false);
const addingLesson = ref(false);

function openAddLessonModal(chapter) {
  addLessonChapter.value = chapter;
  newLessonTitle.value = '';
  newLessonIncludeInPreview.value = false;
  addLessonModalShown.value = true;
}

function openLessonEditor(chapter, lesson) {
  router.push({
    name: 'LessonEdit',
    params: {
      courseId,
      chapterId: chapter.id,
      lessonId: lesson.id,
    },
  });
}

async function confirmAddLesson() {
  if (!newLessonTitle.value.trim() || !addLessonChapter.value || addingLesson.value) return;

  const chapterId = addLessonChapter.value.id;
  const existingIds = new Set(
    (course.value?.chapters.find((c) => c.id === chapterId)?.lessons ?? []).map((l) => l.id),
  );

  addingLesson.value = true;
  errorMessage.value = '';
  try {
    const updated = await addLesson(courseId, chapterId, {
      title: newLessonTitle.value.trim(),
      content: '',
      includeInPreview: newLessonIncludeInPreview.value,
    });
    course.value = updated;
    addLessonModalShown.value = false;

    const chapter = updated.chapters.find((c) => c.id === chapterId);
    const created = chapter?.lessons.find((l) => !existingIds.has(l.id))
      ?? chapter?.lessons.at(-1);
    if (created) openLessonEditor(chapter, created);
  } catch (err) {
    if (err.response?.status === 403) errorMessage.value = t('courses.editor.forbidden');
    else errorMessage.value = t('courses.editor.save_error');
  } finally {
    addingLesson.value = false;
  }
}

function moveLessonDirection(chapter, lesson, direction) {
  runMutation(() => moveLesson(courseId, chapter.id, lesson.id, direction));
}

// Remove lesson
const removeLessonModalShown = ref(false);
const removeLessonChapter = ref(null);
const removeLessonTarget = ref(null);

function openRemoveLessonModal(chapter, lesson) {
  removeLessonChapter.value = chapter;
  removeLessonTarget.value = lesson;
  removeLessonModalShown.value = true;
}

function confirmRemoveLesson() {
  runMutation(() => removeLesson(courseId, removeLessonChapter.value.id, removeLessonTarget.value.id));
}

async function publish() {
  publishing.value = true;
  await runMutation(() => publishCourse(courseId));
  publishing.value = false;
}
</script>

<template>
  <div class="course-assembly">
    <div
      class="course-assembly__square"
      aria-hidden="true"
    />
    <div
      class="course-assembly__curve"
      aria-hidden="true"
    />

    <div class="container mx-auto px-6 py-10 md:py-14 relative z-10">
      <template v-if="loading">
        <p class="course-assembly__hint">
          {{ $t('courses.editor.loading') }}
        </p>
      </template>

      <template v-else-if="notFound">
        <div class="course-assembly__state">
          <p class="course-assembly__eyebrow">
            {{ $t('courses.editor.eyebrow') }}
          </p>
          <h1>{{ $t('courses.editor.not_found') }}</h1>
          <router-link
            :to="{ name: 'CoursesCreate' }"
            class="course-assembly__back"
          >
            {{ $t('courses.editor.back_to_courses') }}
          </router-link>
        </div>
      </template>

      <template v-else-if="forbidden">
        <div class="course-assembly__state">
          <p class="course-assembly__eyebrow">
            {{ $t('courses.editor.eyebrow') }}
          </p>
          <h1>{{ $t('courses.editor.forbidden') }}</h1>
          <router-link
            :to="{ name: 'CoursesCreate' }"
            class="course-assembly__back"
          >
            {{ $t('courses.editor.back_to_courses') }}
          </router-link>
        </div>
      </template>

      <template v-else-if="course">
        <div
          v-if="course.coverType === 'Color'"
          class="course-assembly__cover"
          :style="{ backgroundColor: `var(--color-cover-${course.coverColor?.toLowerCase()})` }"
          aria-hidden="true"
        />
        <img
          v-else-if="course.coverType === 'Image' && coverImageUrl"
          :src="coverImageUrl"
          alt=""
          class="course-assembly__cover course-assembly__cover--image"
        >

        <header class="course-assembly__header">
          <div class="course-assembly__header-copy">
            <p class="course-assembly__eyebrow">
              {{ $t('courses.editor.eyebrow') }}
            </p>
            <div class="course-assembly__title-row">
              <h1>{{ course.title }}</h1>
              <ForgeStatusStamp
                :variant="course.isPublished ? 'published' : 'draft'"
                :label="course.isPublished ? $t('courses.editor.published') : $t('courses.editor.draft')"
              />
            </div>
            <p class="course-assembly__subtitle">
              {{ $t('courses.editor.subtitle') }}
            </p>
          </div>

          <div class="course-assembly__header-actions">
            <div
              class="course-assembly__meta"
              aria-hidden="true"
            >
              <span>ASM / {{ chapterCountLabel }}</span>
              <span>CURRICULUM</span>
            </div>
            <button
              type="button"
              class="course-assembly__publish btn-accent"
              :disabled="course.isPublished || publishing"
              @click="publish"
            >
              <span v-if="publishing">{{ $t('courses.editor.publishing') }}</span>
              <span v-else>{{ $t('courses.editor.publish') }}</span>
            </button>
          </div>
        </header>

        <div
          v-if="errorMessage"
          class="course-assembly__alert"
          role="alert"
        >
          <span>{{ errorMessage }}</span>
          <button
            type="button"
            class="course-assembly__alert-close"
            :aria-label="$t('courses.editor.dismiss_error')"
            @click="errorMessage = ''"
          >
            ×
          </button>
        </div>

        <div class="course-assembly__toolbar">
          <div>
            <h2>{{ $t('courses.editor.chapters_title') }}</h2>
            <p>{{ $t('courses.editor.chapters_hint') }}</p>
          </div>
          <button
            type="button"
            class="course-assembly__ghost-btn"
            @click="openAddChapterModal"
          >
            + {{ $t('courses.editor.add_chapter') }}
          </button>
        </div>

        <div
          v-if="course.chapters.length === 0"
          class="course-assembly__empty"
        >
          <span
            class="course-assembly__empty-mark"
            aria-hidden="true"
          >LF—00</span>
          <p>{{ $t('courses.editor.no_chapters') }}</p>
          <button
            type="button"
            class="course-assembly__ghost-btn"
            @click="openAddChapterModal"
          >
            + {{ $t('courses.editor.add_chapter') }}
          </button>
        </div>

        <section
          v-for="(chapter, chapterIndexPos) in course.chapters"
          :key="chapter.id"
          class="course-assembly__chapter"
        >
          <div class="course-assembly__chapter-header">
            <div class="course-assembly__chapter-title">
              <span
                class="course-assembly__chapter-index"
                aria-hidden="true"
              >{{ chapterIndex(chapterIndexPos) }}</span>
              <h3>{{ chapter.title }}</h3>
            </div>
            <div class="course-assembly__actions">
              <button
                type="button"
                class="course-assembly__icon-btn"
                :disabled="chapterIndexPos === 0"
                :title="$t('courses.editor.move_up')"
                :aria-label="$t('courses.editor.move_up')"
                @click="moveChapterDirection(chapter, 'Up')"
              >
                ↑
              </button>
              <button
                type="button"
                class="course-assembly__icon-btn"
                :disabled="chapterIndexPos === course.chapters.length - 1"
                :title="$t('courses.editor.move_down')"
                :aria-label="$t('courses.editor.move_down')"
                @click="moveChapterDirection(chapter, 'Down')"
              >
                ↓
              </button>
              <button
                type="button"
                class="course-assembly__ghost-btn course-assembly__ghost-btn--compact"
                @click="openRenameChapterModal(chapter)"
              >
                {{ $t('courses.editor.rename_chapter') }}
              </button>
              <button
                type="button"
                class="course-assembly__ghost-btn course-assembly__ghost-btn--compact"
                @click="openAddLessonModal(chapter)"
              >
                + {{ $t('courses.editor.add_lesson') }}
              </button>
            </div>
          </div>

          <p
            v-if="chapter.lessons.length === 0"
            class="course-assembly__hint course-assembly__hint--inset"
          >
            {{ $t('courses.editor.no_lessons') }}
          </p>

          <ul
            v-else
            class="course-assembly__lessons"
          >
            <li
              v-for="(lesson, lessonIndex) in chapter.lessons"
              :key="lesson.id"
              class="course-assembly__lesson"
            >
              <button
                type="button"
                class="course-assembly__lesson-copy course-assembly__lesson-open"
                @click="openLessonEditor(chapter, lesson)"
              >
                <span
                  class="course-assembly__lesson-index"
                  aria-hidden="true"
                >{{ String(lessonIndex + 1).padStart(2, '0') }}</span>
                <div>
                  <span class="course-assembly__lesson-title">{{ lesson.title }}</span>
                  <ForgeStatusStamp
                    v-if="lesson.includeInPreview"
                    class="course-assembly__lesson-stamp"
                    variant="preview"
                    :label="$t('courses.editor.include_in_preview_label')"
                  />
                </div>
              </button>
              <div class="course-assembly__actions">
                <button
                  type="button"
                  class="course-assembly__icon-btn"
                  :disabled="lessonIndex === 0"
                  :title="$t('courses.editor.move_up')"
                  :aria-label="$t('courses.editor.move_up')"
                  @click="moveLessonDirection(chapter, lesson, 'Up')"
                >
                  ↑
                </button>
                <button
                  type="button"
                  class="course-assembly__icon-btn"
                  :disabled="lessonIndex === chapter.lessons.length - 1"
                  :title="$t('courses.editor.move_down')"
                  :aria-label="$t('courses.editor.move_down')"
                  @click="moveLessonDirection(chapter, lesson, 'Down')"
                >
                  ↓
                </button>
                <button
                  type="button"
                  class="course-assembly__ghost-btn course-assembly__ghost-btn--compact"
                  @click="openLessonEditor(chapter, lesson)"
                >
                  {{ $t('courses.editor.rename_lesson') }}
                </button>
                <button
                  type="button"
                  class="course-assembly__ghost-btn course-assembly__ghost-btn--compact course-assembly__ghost-btn--danger"
                  @click="openRemoveLessonModal(chapter, lesson)"
                >
                  {{ $t('courses.editor.remove_lesson') }}
                </button>
              </div>
            </li>
          </ul>
        </section>

        <router-link
          :to="{ name: 'CoursesCreate' }"
          class="course-assembly__back"
        >
          {{ $t('courses.editor.back_to_courses') }}
        </router-link>
      </template>
    </div>

    <ForgeDialog
      v-model="addChapterModalShown"
      :title="$t('courses.editor.add_chapter')"
      :confirm-label="$t('courses.editor.save')"
      :cancel-label="$t('courses.editor.cancel')"
      @confirm="confirmAddChapter"
    >
      <ForgeField
        v-model="newChapterTitle"
        :label="$t('courses.editor.chapter_title_placeholder')"
      />
    </ForgeDialog>

    <ForgeDialog
      v-model="renameChapterModalShown"
      :title="$t('courses.editor.rename_chapter')"
      :confirm-label="$t('courses.editor.save')"
      :cancel-label="$t('courses.editor.cancel')"
      @confirm="confirmRenameChapter"
    >
      <ForgeField
        v-model="renameChapterTitleInput"
        :label="$t('courses.editor.chapter_title_placeholder')"
      />
    </ForgeDialog>

    <ForgeDialog
      v-model="addLessonModalShown"
      :title="$t('courses.editor.add_lesson')"
      :confirm-label="$t('courses.editor.open_lesson_editor')"
      :cancel-label="$t('courses.editor.cancel')"
      @confirm="confirmAddLesson"
    >
      <ForgeField
        v-model="newLessonTitle"
        :label="$t('courses.editor.lesson_title_label')"
      />
      <label class="course-assembly__check">
        <input
          v-model="newLessonIncludeInPreview"
          type="checkbox"
        >
        <span>{{ $t('courses.editor.include_in_preview_label') }}</span>
      </label>
    </ForgeDialog>

    <ForgeDialog
      v-model="removeLessonModalShown"
      :title="$t('courses.editor.remove_lesson')"
      :confirm-label="$t('courses.editor.remove_lesson')"
      :cancel-label="$t('courses.editor.cancel')"
      danger
      @confirm="confirmRemoveLesson"
    >
      <p class="course-assembly__confirm-copy">
        {{ $t('courses.editor.remove_lesson_confirm', { title: removeLessonTarget?.title }) }}
      </p>
    </ForgeDialog>
  </div>
</template>

<style scoped>
.course-assembly {
  position: relative;
  min-height: calc(100vh - 4.5rem);
  overflow: hidden;
  isolation: isolate;
  background-color: var(--color-surface-950);
  background-image:
    linear-gradient(var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(
      115deg,
      var(--industrial-hero-start) 0%,
      var(--industrial-hero-middle) 58%,
      var(--industrial-hero-end) 100%
    );
  background-size: 40px 40px, 40px 40px, auto;
}

.course-assembly__square,
.course-assembly__curve {
  position: absolute;
  pointer-events: none;
  z-index: 0;
}

.course-assembly__square {
  top: 5rem;
  right: 6%;
  width: 6.5rem;
  height: 6.5rem;
  border: 1px solid var(--industrial-line);
  transform: rotate(16deg);
  animation: assembly-drift 11s ease-in-out infinite alternate;
}

.course-assembly__square::after {
  content: "";
  position: absolute;
  inset: 0.7rem;
  background: var(--industrial-accent-wash);
  border: 1px solid var(--industrial-line);
}

.course-assembly__curve {
  width: 16rem;
  height: 16rem;
  left: -5rem;
  bottom: 8%;
  border: 1px solid var(--industrial-line);
  border-radius: 50%;
  box-shadow: 0 0 0 36px var(--industrial-grid);
}

.course-assembly__cover {
  height: 8rem;
  margin-bottom: 1.5rem;
  border-radius: var(--radius-card);
  animation: assembly-rise 0.4s ease both;
}

.course-assembly__cover--image {
  width: 100%;
  object-fit: cover;
}

.course-assembly__header {
  display: flex;
  flex-wrap: wrap;
  align-items: end;
  justify-content: space-between;
  gap: 1.5rem;
  margin-bottom: 1.75rem;
  padding-bottom: 1.35rem;
  border-bottom: 1px solid var(--industrial-line);
  animation: assembly-rise 0.4s ease both;
}

.course-assembly__eyebrow {
  margin: 0 0 0.65rem;
  color: var(--color-accent-coral);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.course-assembly__title-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
}

.course-assembly__title-row h1,
.course-assembly__state h1 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.85rem, 4.5vw, 3rem);
  font-weight: 800;
  letter-spacing: -0.04em;
  line-height: 1.08;
}

.course-assembly__subtitle {
  max-width: 36rem;
  margin: 0.8rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.98rem;
  line-height: 1.6;
}

.course-assembly__header-actions {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.85rem;
}

.course-assembly__meta {
  display: none;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.3rem;
  color: var(--color-ink-faint);
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.course-assembly__publish {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 2.85rem;
  padding: 0.7rem 1.35rem;
  border: 0;
  border-radius: var(--radius-pill);
  font-family: inherit;
  font-size: 0.88rem;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.15s ease, transform 0.12s ease, opacity 0.15s ease;
}

.course-assembly__publish:hover:not(:disabled) {
  transform: translateY(-1px);
}

.course-assembly__publish:active:not(:disabled) {
  transform: scale(0.97);
}

.course-assembly__publish:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.course-assembly__alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
  padding: 0.85rem 1rem;
  color: var(--color-accent-coral);
  background: var(--color-accent-soft);
  border: 1px solid var(--color-accent-coral);
  border-radius: var(--radius-card);
  font-size: 0.9rem;
  font-weight: 600;
}

.course-assembly__alert-close {
  border: 0;
  background: transparent;
  color: inherit;
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
}

.course-assembly__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: end;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
  animation: assembly-rise 0.4s ease 0.05s both;
}

.course-assembly__toolbar h2 {
  margin: 0;
  color: var(--color-ink);
  font-size: 1.15rem;
  font-weight: 800;
  letter-spacing: -0.02em;
}

.course-assembly__toolbar p {
  margin: 0.35rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
}

.course-assembly__ghost-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 2.4rem;
  padding: 0.5rem 0.95rem;
  color: var(--color-ink);
  background: transparent;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-pill);
  font-family: inherit;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
  transition: color 0.15s ease, border-color 0.15s ease, background-color 0.15s ease, transform 0.12s ease;
}

.course-assembly__ghost-btn:hover {
  border-color: var(--color-ink-faint);
  background: var(--industrial-accent-wash);
}

.course-assembly__ghost-btn:active {
  transform: scale(0.97);
}

.course-assembly__ghost-btn--compact {
  min-height: 2.1rem;
  padding: 0.35rem 0.75rem;
  font-size: 0.75rem;
}

.course-assembly__ghost-btn--danger {
  color: #b33a2b;
  border-color: rgba(179, 58, 43, 0.35);
}

.course-assembly__ghost-btn--danger:hover {
  background: rgba(179, 58, 43, 0.08);
}

.course-assembly__icon-btn {
  display: grid;
  place-items: center;
  width: 2.1rem;
  height: 2.1rem;
  padding: 0;
  color: var(--color-ink-muted);
  background: var(--color-surface-950);
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.25rem;
  font-size: 0.9rem;
  cursor: pointer;
  transition: color 0.15s ease, border-color 0.15s ease, background-color 0.15s ease;
}

.course-assembly__icon-btn:hover:not(:disabled) {
  color: var(--color-ink);
  border-color: var(--color-ink-faint);
  background: var(--industrial-accent-wash);
}

.course-assembly__icon-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.course-assembly__empty {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 1rem;
  padding: 2rem 1.5rem;
  background: var(--industrial-panel);
  border: 1px dashed var(--industrial-line-strong);
  border-radius: var(--radius-card);
  backdrop-filter: blur(10px);
  animation: assembly-rise 0.45s ease 0.08s both;
}

.course-assembly__empty-mark {
  color: var(--color-ink-faint);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.14em;
}

.course-assembly__empty p,
.course-assembly__hint,
.course-assembly__confirm-copy {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
  line-height: 1.55;
}

.course-assembly__hint--inset {
  padding: 0.75rem 0 0.25rem 0.15rem;
  font-size: 0.88rem;
}

.course-assembly__chapter {
  margin-bottom: 1rem;
  padding: 1.15rem 1.15rem 1rem;
  background: var(--industrial-panel);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  backdrop-filter: blur(10px);
  box-shadow: 0 14px 40px rgba(20, 20, 20, 0.05);
  animation: assembly-rise 0.4s ease both;
}

.course-assembly__chapter-header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.85rem;
  padding-bottom: 0.85rem;
  border-bottom: 1px solid var(--industrial-line);
}

.course-assembly__chapter-title {
  display: flex;
  align-items: baseline;
  gap: 0.75rem;
  min-width: 0;
}

.course-assembly__chapter-index {
  color: var(--color-ink-faint);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  white-space: nowrap;
}

.course-assembly__chapter-title h3 {
  margin: 0;
  color: var(--color-ink);
  font-size: 1.05rem;
  font-weight: 800;
  letter-spacing: -0.02em;
}

.course-assembly__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.4rem;
}

.course-assembly__lessons {
  list-style: none;
  margin: 0.85rem 0 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
}

.course-assembly__lesson {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.75rem 0.85rem;
  background: var(--color-surface-950);
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.35rem;
  transition: border-color 0.15s ease, background-color 0.15s ease;
}

.course-assembly__lesson:hover {
  border-color: var(--industrial-line-strong);
  background: var(--industrial-accent-wash);
}

.course-assembly__lesson-copy {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  min-width: 0;
}

.course-assembly__lesson-open {
  padding: 0;
  border: 0;
  background: transparent;
  text-align: left;
  cursor: pointer;
}

.course-assembly__lesson-open:hover .course-assembly__lesson-title {
  color: var(--color-accent-coral-dark);
}

.course-assembly__lesson-index {
  margin-top: 0.15rem;
  color: var(--color-ink-faint);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.08em;
}

.course-assembly__lesson-title {
  color: var(--color-ink);
  font-size: 0.92rem;
  font-weight: 600;
}

.course-assembly__lesson-stamp {
  margin-left: 0.55rem;
  vertical-align: middle;
}

.course-assembly__back {
  display: inline-block;
  margin-top: 1.75rem;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  font-weight: 600;
  text-decoration: none;
  transition: color 0.15s ease;
}

.course-assembly__back:hover {
  color: var(--color-accent-coral);
}

.course-assembly__state {
  max-width: 36rem;
  padding: 2rem 0;
  animation: assembly-rise 0.4s ease both;
}

.course-assembly__check {
  display: inline-flex;
  align-items: center;
  gap: 0.65rem;
  color: var(--color-ink);
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
}

.course-assembly__check input {
  width: 1rem;
  height: 1rem;
  accent-color: var(--color-accent-coral);
}

@media (min-width: 768px) {
  .course-assembly__header-actions {
    align-items: flex-end;
  }

  .course-assembly__meta {
    display: flex;
  }
}

@keyframes assembly-rise {
  from {
    opacity: 0;
    transform: translateY(0.7rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes assembly-drift {
  from {
    transform: rotate(16deg) translateY(0);
  }
  to {
    transform: rotate(24deg) translateY(0.45rem);
  }
}
</style>
