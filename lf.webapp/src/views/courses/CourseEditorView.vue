<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import draggable from 'vuedraggable';
import { GripVertical, Plus, Trash2 } from 'lucide-vue-next';
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
import StudioShell from '@/components/courses/studio/StudioShell.vue';
import StudioButton from '@/components/courses/studio/StudioButton.vue';
import StudioIconButton from '@/components/courses/studio/StudioIconButton.vue';
import StudioConfirmDialog from '@/components/courses/studio/StudioConfirmDialog.vue';
import StudioPromptDialog from '@/components/courses/studio/StudioPromptDialog.vue';

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
const reordering = ref(false);
const coverImageUrl = ref('');

const addChapterModalShown = ref(false);
const submittingChapter = ref(false);
const submittingLesson = ref(false);

const editingChapterId = ref(null);
const editingChapterTitle = ref('');
const chapterRenameRef = ref(null);

const removeLessonModalShown = ref(false);
const removeLessonChapter = ref(null);
const removeLessonTarget = ref(null);

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
    return true;
  } catch (err) {
    if (err.response?.status === 409) errorMessage.value = t('courses.editor.publish_error');
    else if (err.response?.status === 403) errorMessage.value = t('courses.editor.forbidden');
    else errorMessage.value = t('courses.editor.save_error');
    return false;
  }
}

function findMove(oldList, newList) {
  if (!oldList?.length || oldList.length !== newList.length) return null;
  for (let toIndex = 0; toIndex < newList.length; toIndex += 1) {
    if (oldList[toIndex]?.id !== newList[toIndex]?.id) {
      const fromIndex = oldList.findIndex((item) => item.id === newList[toIndex].id);
      if (fromIndex < 0 || fromIndex === toIndex) return null;
      return { id: oldList[fromIndex].id, fromIndex, toIndex };
    }
  }
  return null;
}

async function applyDirectionalMoves(moveFn, steps, direction) {
  let updated = course.value;
  for (let i = 0; i < steps; i += 1) {
    updated = await moveFn(direction);
  }
  course.value = updated;
}

async function onChaptersReorder(newChapters) {
  if (!course.value || reordering.value) return;
  const move = findMove(course.value.chapters, newChapters);
  if (!move) return;

  reordering.value = true;
  errorMessage.value = '';
  course.value = { ...course.value, chapters: newChapters };
  try {
    const direction = move.toIndex > move.fromIndex ? 'Down' : 'Up';
    const steps = Math.abs(move.toIndex - move.fromIndex);
    await applyDirectionalMoves(
      (dir) => moveChapter(courseId, move.id, dir),
      steps,
      direction,
    );
  } catch {
    errorMessage.value = t('courses.editor.save_error');
    await loadCourse();
  } finally {
    reordering.value = false;
  }
}

async function onLessonsReorder(chapter, newLessons) {
  if (!course.value || reordering.value) return;
  const move = findMove(chapter.lessons, newLessons);
  if (!move) return;

  reordering.value = true;
  errorMessage.value = '';
  course.value = {
    ...course.value,
    chapters: course.value.chapters.map((ch) => (
      ch.id === chapter.id ? { ...ch, lessons: newLessons } : ch
    )),
  };
  try {
    const direction = move.toIndex > move.fromIndex ? 'Down' : 'Up';
    const steps = Math.abs(move.toIndex - move.fromIndex);
    await applyDirectionalMoves(
      (dir) => moveLesson(courseId, chapter.id, move.id, dir),
      steps,
      direction,
    );
  } catch {
    errorMessage.value = t('courses.editor.save_error');
    await loadCourse();
  } finally {
    reordering.value = false;
  }
}

function startAddChapter() {
  addChapterModalShown.value = true;
}

async function confirmAddChapter(title) {
  if (submittingChapter.value) return;
  const trimmed = title?.trim();
  if (!trimmed) return;
  submittingChapter.value = true;
  try {
    await runMutation(() => addChapter(courseId, trimmed));
  } finally {
    submittingChapter.value = false;
  }
}

async function startRenameChapter(chapter) {
  editingChapterId.value = chapter.id;
  editingChapterTitle.value = chapter.title;
  await nextTick();
  chapterRenameRef.value?.focus();
  chapterRenameRef.value?.select();
}

function cancelRenameChapter() {
  editingChapterId.value = null;
  editingChapterTitle.value = '';
}

async function confirmRenameChapter(chapter) {
  const title = editingChapterTitle.value.trim();
  if (!title || title === chapter.title) {
    cancelRenameChapter();
    return;
  }
  const ok = await runMutation(() => renameChapter(courseId, chapter.id, title));
  if (ok) cancelRenameChapter();
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

async function startAddLesson(chapter) {
  if (submittingLesson.value) return;

  const existingIds = new Set((chapter.lessons ?? []).map((l) => l.id));
  errorMessage.value = '';
  submittingLesson.value = true;
  try {
    const updated = await addLesson(courseId, chapter.id, {
      title: t('courses.editor.untitled_lesson'),
      content: '',
      includeInPreview: false,
    });
    course.value = updated;

    const updatedChapter = updated.chapters.find((c) => c.id === chapter.id);
    const created = updatedChapter?.lessons.find((l) => !existingIds.has(l.id))
      ?? updatedChapter?.lessons.at(-1);
    if (created) openLessonEditor(updatedChapter, created);
  } catch (err) {
    if (err.response?.status === 403) errorMessage.value = t('courses.editor.forbidden');
    else errorMessage.value = t('courses.editor.save_error');
  } finally {
    submittingLesson.value = false;
  }
}

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

const chapterCount = computed(() => course.value?.chapters.length ?? 0);
const lessonCount = computed(() => (
  course.value?.chapters.reduce((sum, ch) => sum + (ch.lessons?.length ?? 0), 0) ?? 0
));
</script>

<template>
  <StudioShell>
    <template v-if="loading">
      <p class="studio-hint">
        {{ $t('courses.editor.loading') }}
      </p>
    </template>

    <template v-else-if="notFound || forbidden">
      <div class="studio-state">
        <h1>{{ notFound ? $t('courses.editor.not_found') : $t('courses.editor.forbidden') }}</h1>
        <router-link
          :to="{ name: 'CoursesCreate' }"
          class="studio-link"
        >
          {{ $t('courses.editor.back_to_courses') }}
        </router-link>
      </div>
    </template>

    <template v-else-if="course">
      <header class="studio-topbar">
        <div class="studio-topbar__copy">
          <router-link
            :to="{ name: 'CoursesCreate' }"
            class="studio-link studio-link--muted"
          >
            {{ $t('courses.editor.back_to_courses') }}
          </router-link>
          <h1>{{ course.title }}</h1>
          <p class="studio-topbar__meta">
            <span
              class="studio-badge"
              :data-variant="course.isPublished ? 'published' : 'draft'"
            >
              {{ course.isPublished ? $t('courses.editor.published') : $t('courses.editor.draft') }}
            </span>
            <span>{{ $t('courses.editor.outline_summary', { chapters: chapterCount, lessons: lessonCount }) }}</span>
          </p>
        </div>
        <StudioButton
          variant="primary"
          :disabled="course.isPublished || publishing"
          @click="publish"
        >
          {{ publishing ? $t('courses.editor.publishing') : $t('courses.editor.publish') }}
        </StudioButton>
      </header>

      <div
        v-if="errorMessage"
        class="studio-alert"
        role="alert"
      >
        <span>{{ errorMessage }}</span>
        <button
          type="button"
          class="studio-alert__close"
          :aria-label="$t('courses.editor.dismiss_error')"
          @click="errorMessage = ''"
        >
          ×
        </button>
      </div>

      <div class="studio-layout">
        <aside class="studio-outline">
          <div class="studio-outline__header">
            <h2>{{ $t('courses.editor.chapters_title') }}</h2>
            <StudioButton
              variant="quiet"
              size="sm"
              @click="startAddChapter"
            >
              <Plus :size="16" />
              {{ $t('courses.editor.add_chapter') }}
            </StudioButton>
          </div>

          <div
            v-if="course.chapters.length === 0"
            class="studio-empty"
          >
            <p>{{ $t('courses.editor.no_chapters') }}</p>
            <StudioButton
              variant="ghost"
              size="sm"
              @click="startAddChapter"
            >
              <Plus :size="16" />
              {{ $t('courses.editor.add_chapter') }}
            </StudioButton>
          </div>

          <draggable
            v-else
            :model-value="course.chapters"
            item-key="id"
            handle=".studio-drag"
            :disabled="reordering || editingChapterId != null"
            class="studio-outline__list"
            @update:model-value="onChaptersReorder"
          >
            <template #item="{ element: chapter }">
              <section class="studio-chapter">
                <div class="studio-chapter__head">
                  <button
                    type="button"
                    class="studio-drag"
                    :aria-label="$t('courses.editor.drag_chapter')"
                    :disabled="reordering"
                  >
                    <GripVertical :size="16" />
                  </button>

                  <input
                    v-if="editingChapterId === chapter.id"
                    ref="chapterRenameRef"
                    v-model="editingChapterTitle"
                    class="studio-inline-input studio-inline-input--chapter"
                    :aria-label="$t('courses.editor.chapter_title_placeholder')"
                    @keydown.enter.prevent="confirmRenameChapter(chapter)"
                    @keydown.escape.prevent="cancelRenameChapter"
                    @blur="confirmRenameChapter(chapter)"
                  >
                  <button
                    v-else
                    type="button"
                    class="studio-chapter__title"
                    @click="startRenameChapter(chapter)"
                  >
                    {{ chapter.title }}
                  </button>

                  <StudioButton
                    variant="quiet"
                    size="sm"
                    :disabled="submittingLesson"
                    @click="startAddLesson(chapter)"
                  >
                    <Plus :size="14" />
                    {{ $t('courses.editor.add_lesson') }}
                  </StudioButton>
                </div>

                <draggable
                  :model-value="chapter.lessons"
                  item-key="id"
                  handle=".studio-drag"
                  :disabled="reordering"
                  class="studio-lessons"
                  @update:model-value="(list) => onLessonsReorder(chapter, list)"
                >
                  <template #item="{ element: lesson }">
                    <div class="studio-lesson">
                      <button
                        type="button"
                        class="studio-drag"
                        :aria-label="$t('courses.editor.drag_lesson')"
                        :disabled="reordering"
                      >
                        <GripVertical :size="14" />
                      </button>
                      <button
                        type="button"
                        class="studio-lesson__open"
                        @click="openLessonEditor(chapter, lesson)"
                      >
                        <span>{{ lesson.title }}</span>
                        <span
                          v-if="lesson.includeInPreview"
                          class="studio-badge studio-badge--sm"
                          data-variant="preview"
                        >
                          {{ $t('courses.editor.include_in_preview_label') }}
                        </span>
                      </button>
                      <StudioIconButton
                        danger
                        :label="$t('courses.editor.remove_lesson')"
                        @click="openRemoveLessonModal(chapter, lesson)"
                      >
                        <Trash2 />
                      </StudioIconButton>
                    </div>
                  </template>
                </draggable>

                <p
                  v-if="chapter.lessons.length === 0"
                  class="studio-hint studio-hint--inset"
                >
                  {{ $t('courses.editor.no_lessons') }}
                </p>
              </section>
            </template>
          </draggable>
        </aside>

        <section class="studio-details">
          <div
            v-if="course.coverType === 'Color'"
            class="studio-cover"
            :style="{ backgroundColor: `var(--color-cover-${course.coverColor?.toLowerCase()})` }"
            aria-hidden="true"
          />
          <img
            v-else-if="course.coverType === 'Image' && coverImageUrl"
            :src="coverImageUrl"
            alt=""
            class="studio-cover studio-cover--image"
          >
          <div class="studio-details__card">
            <h2>{{ $t('courses.editor.details_title') }}</h2>
            <p>{{ $t('courses.editor.subtitle') }}</p>
            <p class="studio-details__tip">
              {{ $t('courses.editor.details_tip') }}
            </p>
          </div>
        </section>
      </div>
    </template>

    <StudioPromptDialog
      v-model="addChapterModalShown"
      :title="$t('courses.editor.add_chapter')"
      :label="$t('courses.editor.chapter_title_placeholder')"
      :placeholder="$t('courses.editor.chapter_title_placeholder')"
      :confirm-label="$t('courses.editor.add_chapter')"
      :cancel-label="$t('courses.editor.cancel')"
      @confirm="confirmAddChapter"
    />

    <StudioConfirmDialog
      v-model="removeLessonModalShown"
      :title="$t('courses.editor.remove_lesson')"
      :confirm-label="$t('courses.editor.remove_lesson')"
      :cancel-label="$t('courses.editor.cancel')"
      danger
      @confirm="confirmRemoveLesson"
    >
      <p>{{ $t('courses.editor.remove_lesson_confirm', { title: removeLessonTarget?.title }) }}</p>
    </StudioConfirmDialog>
  </StudioShell>
</template>

<style scoped>
.studio-hint {
  margin: 2rem 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
}

.studio-hint--inset {
  margin: 0.35rem 0 0.15rem 1.85rem;
  font-size: 0.85rem;
}

.studio-state h1 {
  margin: 0 0 1rem;
  font-size: 1.75rem;
  font-weight: 800;
  letter-spacing: -0.03em;
}

.studio-link {
  color: var(--color-accent-coral-dark);
  font-size: 0.9rem;
  font-weight: 600;
  text-decoration: none;
}

.studio-link:hover {
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.studio-link--muted {
  color: var(--color-ink-muted);
}

.studio-topbar {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid var(--color-border-subtle);
}

.studio-topbar h1 {
  margin: 0.35rem 0 0.5rem;
  color: var(--color-ink);
  font-size: clamp(1.6rem, 3vw, 2.1rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.15;
}

.studio-topbar__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.65rem;
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
}

.studio-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  background: var(--color-surface-900);
  color: var(--color-ink-muted);
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.studio-badge--sm {
  font-size: 0.65rem;
  padding: 0.1rem 0.4rem;
}

.studio-badge[data-variant='published'] {
  background: color-mix(in srgb, #3a7d44 14%, transparent);
  color: #2f6b38;
}

.studio-badge[data-variant='draft'] {
  background: var(--color-surface-900);
}

.studio-badge[data-variant='preview'] {
  background: color-mix(in srgb, var(--color-accent-coral) 14%, transparent);
  color: var(--color-accent-coral-dark);
}

.studio-alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.15rem;
  padding: 0.8rem 1rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.6rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral-dark);
  font-size: 0.9rem;
  font-weight: 600;
}

.studio-alert__close {
  border: 0;
  background: transparent;
  color: inherit;
  font-size: 1.2rem;
  cursor: pointer;
}

.studio-layout {
  display: grid;
  gap: 1.5rem;
}

@media (min-width: 960px) {
  .studio-layout {
    grid-template-columns: minmax(0, 1.4fr) minmax(16rem, 0.8fr);
    align-items: start;
  }
}

.studio-outline {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.studio-outline__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.studio-outline__header h2 {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.studio-outline__list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.studio-empty {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.85rem;
  padding: 1.5rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.75rem;
  background: var(--color-surface-900);
}

.studio-empty p {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.92rem;
  line-height: 1.5;
}

.studio-chapter {
  padding: 0.75rem 0.85rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.65rem;
  background: transparent;
}

.studio-chapter__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem;
  margin-bottom: 0.35rem;
}

.studio-chapter__title {
  flex: 1;
  min-width: 8rem;
  padding: 0.35rem 0.45rem;
  border: 1px solid transparent;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink);
  font-size: 0.98rem;
  font-weight: 700;
  text-align: left;
  cursor: text;
}

.studio-chapter__title:hover {
  background: var(--color-surface-900);
}

.studio-drag {
  display: inline-grid;
  place-items: center;
  width: 1.6rem;
  height: 1.6rem;
  padding: 0;
  border: 0;
  border-radius: 0.35rem;
  background: transparent;
  color: var(--color-ink-faint);
  cursor: grab;
}

.studio-drag:hover:not(:disabled) {
  background: var(--color-surface-900);
  color: var(--color-ink-muted);
}

.studio-drag:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.studio-lessons {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  margin-top: 0.25rem;
}

.studio-lesson {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.2rem 0.2rem 0.2rem 0;
  border-radius: 0.5rem;
}

.studio-lesson:hover {
  background: var(--color-surface-900);
}

.studio-lesson__open {
  flex: 1;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.45rem;
  min-width: 0;
  padding: 0.4rem 0.35rem;
  border: 0;
  background: transparent;
  color: var(--color-ink);
  font-size: 0.9rem;
  font-weight: 560;
  text-align: left;
  cursor: pointer;
}

.studio-lesson__open:hover {
  color: var(--color-accent-coral-dark);
}

.studio-inline-input {
  width: 100%;
  padding: 0.55rem 0.75rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.5rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  font-family: inherit;
  font-size: 0.9rem;
  outline: none;
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-accent-coral) 18%, transparent);
}

.studio-inline-input--chapter {
  flex: 1;
  min-width: 8rem;
  font-weight: 700;
}

.studio-details__card {
  padding: 1.25rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.75rem;
  background: var(--color-surface-900);
}

.studio-details__card h2 {
  margin: 0 0 0.5rem;
  font-size: 1rem;
  font-weight: 700;
}

.studio-details__card p {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  line-height: 1.55;
}

.studio-details__tip {
  margin-top: 0.85rem !important;
  padding-top: 0.85rem;
  border-top: 1px solid var(--color-border-subtle);
}

.studio-cover {
  height: 7.5rem;
  margin-bottom: 1rem;
  border-radius: 0.75rem;
}

.studio-cover--image {
  width: 100%;
  object-fit: cover;
}
</style>
