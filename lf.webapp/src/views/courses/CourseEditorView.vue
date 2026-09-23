<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import draggable from 'vuedraggable';
import {
  BookOpen,
  ChevronDown,
  ChevronRight,
  Eye,
  FileText,
  FolderOpen,
  GripVertical,
  Layers,
  Library,
  Pencil,
  Plus,
  Trash2,
} from 'lucide-vue-next';
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

const collapsedChapterIds = ref(new Set());

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

function isChapterCollapsed(chapterId) {
  return collapsedChapterIds.value.has(chapterId);
}

function toggleChapter(chapterId) {
  const next = new Set(collapsedChapterIds.value);
  if (next.has(chapterId)) next.delete(chapterId);
  else next.add(chapterId);
  collapsedChapterIds.value = next;
}

function lessonCountLabel(count) {
  if (count === 1) return t('courses.editor.chapter_lesson_count_one', { count });
  return t('courses.editor.chapter_lesson_count', { count });
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
          <p class="studio-topbar__eyebrow">
            <Library
              :size="14"
              aria-hidden="true"
            />
            {{ $t('courses.editor.eyebrow') }}
          </p>
          <h1>{{ course.title }}</h1>
          <p class="studio-topbar__meta">
            <span
              class="studio-badge"
              :data-variant="course.isPublished ? 'published' : 'draft'"
            >
              {{ course.isPublished ? $t('courses.editor.published') : $t('courses.editor.draft') }}
            </span>
            <span class="studio-stat">
              <Layers
                :size="14"
                aria-hidden="true"
              />
              {{ chapterCount }}
            </span>
            <span class="studio-stat">
              <FileText
                :size="14"
                aria-hidden="true"
              />
              {{ lessonCount }}
            </span>
            <span class="studio-topbar__summary">
              {{ $t('courses.editor.outline_summary', { chapters: chapterCount, lessons: lessonCount }) }}
            </span>
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
            <div class="studio-outline__heading">
              <span
                class="studio-outline__icon"
                aria-hidden="true"
              >
                <BookOpen :size="18" />
              </span>
              <div>
                <h2>{{ $t('courses.editor.chapters_title') }}</h2>
                <p class="studio-outline__hint">
                  {{ $t('courses.editor.chapters_hint') }}
                </p>
              </div>
            </div>
            <StudioButton
              variant="ghost"
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
            <span
              class="studio-empty__icon"
              aria-hidden="true"
            >
              <FolderOpen :size="28" />
            </span>
            <p>{{ $t('courses.editor.no_chapters') }}</p>
            <StudioButton
              variant="primary"
              size="sm"
              @click="startAddChapter"
            >
              <Plus :size="16" />
              {{ $t('courses.editor.no_chapters_cta') }}
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
            <template #item="{ element: chapter, index: chapterIndex }">
              <section
                class="studio-chapter"
                :class="{ 'studio-chapter--collapsed': isChapterCollapsed(chapter.id) }"
              >
                <div class="studio-chapter__head">
                  <button
                    type="button"
                    class="studio-drag"
                    :aria-label="$t('courses.editor.drag_chapter')"
                    :disabled="reordering"
                  >
                    <GripVertical :size="16" />
                  </button>

                  <button
                    type="button"
                    class="studio-chapter__toggle"
                    :aria-expanded="!isChapterCollapsed(chapter.id)"
                    :aria-label="isChapterCollapsed(chapter.id)
                      ? $t('courses.editor.expand_chapter')
                      : $t('courses.editor.collapse_chapter')"
                    @click="toggleChapter(chapter.id)"
                  >
                    <ChevronRight
                      v-if="isChapterCollapsed(chapter.id)"
                      :size="16"
                    />
                    <ChevronDown
                      v-else
                      :size="16"
                    />
                  </button>

                  <span
                    class="studio-chapter__badge"
                    :title="$t('courses.editor.chapter_index', { n: chapterIndex + 1 })"
                    aria-hidden="true"
                  >
                    <Layers :size="14" />
                    <span>{{ chapterIndex + 1 }}</span>
                  </span>

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
                    <span class="studio-chapter__title-text">{{ chapter.title }}</span>
                    <Pencil
                      class="studio-chapter__edit"
                      :size="13"
                      aria-hidden="true"
                    />
                  </button>

                  <span class="studio-chapter__count">
                    {{ lessonCountLabel(chapter.lessons?.length ?? 0) }}
                  </span>

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

                <div
                  v-show="!isChapterCollapsed(chapter.id)"
                  class="studio-chapter__body"
                >
                  <draggable
                    :model-value="chapter.lessons"
                    item-key="id"
                    handle=".studio-drag"
                    :disabled="reordering"
                    class="studio-lessons"
                    @update:model-value="(list) => onLessonsReorder(chapter, list)"
                  >
                    <template #item="{ element: lesson, index: lessonIndex }">
                      <div class="studio-lesson">
                        <span
                          class="studio-lesson__rail"
                          aria-hidden="true"
                        />
                        <button
                          type="button"
                          class="studio-drag"
                          :aria-label="$t('courses.editor.drag_lesson')"
                          :disabled="reordering"
                        >
                          <GripVertical :size="14" />
                        </button>
                        <span
                          class="studio-lesson__icon"
                          aria-hidden="true"
                        >
                          <FileText :size="15" />
                        </span>
                        <button
                          type="button"
                          class="studio-lesson__open"
                          :aria-label="$t('courses.editor.open_lesson')"
                          @click="openLessonEditor(chapter, lesson)"
                        >
                          <span class="studio-lesson__index">
                            {{ chapterIndex + 1 }}.{{ lessonIndex + 1 }}
                          </span>
                          <span class="studio-lesson__title">{{ lesson.title }}</span>
                          <span
                            v-if="lesson.includeInPreview"
                            class="studio-badge studio-badge--sm"
                            data-variant="preview"
                          >
                            <Eye
                              :size="10"
                              aria-hidden="true"
                            />
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

                  <div
                    v-if="chapter.lessons.length === 0"
                    class="studio-lesson-empty"
                  >
                    <span
                      class="studio-lesson-empty__rail"
                      aria-hidden="true"
                    />
                    <FileText
                      :size="14"
                      aria-hidden="true"
                    />
                    <p>{{ $t('courses.editor.no_lessons') }}</p>
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
                </div>
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
            <div class="studio-details__head">
              <h2>{{ $t('courses.editor.details_title') }}</h2>
              <router-link
                v-if="!course.isPublished"
                :to="{ name: 'CourseSettings', params: { id: course.id } }"
                class="studio-link"
              >
                {{ $t('courses.editor.edit_details') }}
              </router-link>
            </div>
            <dl class="studio-details__summary">
              <dt>{{ $t('courses.create.field_category') }}</dt>
              <dd>{{ course.categoryName }}</dd>
              <dt>{{ $t('courses.create.field_pricing') }}</dt>
              <dd>
                {{ course.pricingType === 'Paid'
                  ? $t('courses.editor.price_summary', { price: course.price })
                  : $t('courses.create.pricing_free') }}
              </dd>
              <dt>{{ $t('courses.create.field_enrollment_mode') }}</dt>
              <dd>{{ course.enrollmentMode === 'Managed' ? $t('courses.create.mode_managed') : $t('courses.create.mode_open') }}</dd>
            </dl>
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

.studio-topbar__eyebrow {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin: 0.55rem 0 0;
  color: var(--color-ink-faint);
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.studio-topbar h1 {
  margin: 0.2rem 0 0.55rem;
  color: var(--color-ink);
  font-family: var(--font-display);
  font-size: clamp(1.55rem, 3vw, 2.05rem);
  font-weight: 700;
  letter-spacing: -0.03em;
  line-height: 1.15;
}

.studio-topbar__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.55rem 0.85rem;
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
}

.studio-stat {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  color: var(--color-ink-muted);
  font-weight: 650;
}

.studio-topbar__summary {
  color: var(--color-ink-faint);
}

.studio-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
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
    grid-template-columns: minmax(0, 1.55fr) minmax(15rem, 0.75fr);
    align-items: start;
  }
}

.studio-outline {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.1rem 1.1rem 1.25rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.9rem;
  background:
    linear-gradient(
      180deg,
      color-mix(in srgb, var(--color-surface-900) 70%, transparent) 0%,
      transparent 8rem
    ),
    var(--color-surface-950);
}

.studio-outline__header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.85rem;
}

.studio-outline__heading {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  min-width: 0;
}

.studio-outline__icon {
  display: inline-grid;
  place-items: center;
  flex-shrink: 0;
  width: 2.35rem;
  height: 2.35rem;
  border-radius: 0.65rem;
  background: color-mix(in srgb, var(--color-accent-coral) 12%, var(--color-surface-900));
  color: var(--color-accent-coral-dark);
}

.studio-outline__header h2 {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 750;
  letter-spacing: -0.02em;
}

.studio-outline__hint {
  margin: 0.2rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  line-height: 1.45;
}

.studio-outline__list {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.studio-empty {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.85rem;
  padding: 1.75rem 1.35rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.8rem;
  background: var(--color-surface-900);
}

.studio-empty__icon {
  display: inline-grid;
  place-items: center;
  width: 3rem;
  height: 3rem;
  border-radius: 0.85rem;
  background: color-mix(in srgb, var(--color-accent-coral) 10%, transparent);
  color: var(--color-accent-coral-dark);
}

.studio-empty p {
  margin: 0;
  max-width: 28rem;
  color: var(--color-ink-muted);
  font-size: 0.92rem;
  line-height: 1.5;
}

.studio-chapter {
  position: relative;
  overflow: clip;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.8rem;
  background: var(--color-surface-950);
  box-shadow: 0 1px 0 color-mix(in srgb, var(--color-ink) 3%, transparent);
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.studio-chapter:hover {
  border-color: color-mix(in srgb, var(--color-accent-coral) 28%, var(--color-border-subtle));
  box-shadow: 0 8px 22px -18px color-mix(in srgb, var(--color-ink) 45%, transparent);
}

.studio-chapter--collapsed {
  background: color-mix(in srgb, var(--color-surface-900) 55%, var(--color-surface-950));
}

.studio-chapter__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem 0.4rem;
  padding: 0.7rem 0.75rem;
  background:
    linear-gradient(
      90deg,
      color-mix(in srgb, var(--color-accent-coral) 7%, transparent),
      transparent 42%
    );
  border-bottom: 1px solid transparent;
}

.studio-chapter:not(.studio-chapter--collapsed) .studio-chapter__head {
  border-bottom-color: var(--color-border-subtle);
}

.studio-chapter__toggle {
  display: inline-grid;
  place-items: center;
  width: 1.7rem;
  height: 1.7rem;
  padding: 0;
  border: 0;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink-muted);
  cursor: pointer;
}

.studio-chapter__toggle:hover {
  background: var(--color-surface-900);
  color: var(--color-ink);
}

.studio-chapter__badge {
  display: inline-flex;
  align-items: center;
  gap: 0.28rem;
  min-width: 2.6rem;
  padding: 0.22rem 0.45rem;
  border-radius: 0.45rem;
  background: color-mix(in srgb, var(--color-accent-coral) 14%, var(--color-surface-900));
  color: var(--color-accent-coral-dark);
  font-size: 0.72rem;
  font-weight: 750;
  letter-spacing: 0.02em;
}

.studio-chapter__title {
  flex: 1;
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  min-width: 8rem;
  padding: 0.35rem 0.45rem;
  border: 1px solid transparent;
  border-radius: 0.45rem;
  background: transparent;
  color: var(--color-ink);
  font-size: 0.98rem;
  font-weight: 720;
  text-align: left;
  cursor: text;
}

.studio-chapter__title-text {
  min-width: 0;
  overflow-wrap: anywhere;
}

.studio-chapter__edit {
  flex-shrink: 0;
  opacity: 0;
  color: var(--color-ink-faint);
  transition: opacity 0.12s ease;
}

.studio-chapter__title:hover {
  background: var(--color-surface-900);
}

.studio-chapter__title:hover .studio-chapter__edit,
.studio-chapter__title:focus-visible .studio-chapter__edit {
  opacity: 1;
}

.studio-chapter__count {
  color: var(--color-ink-faint);
  font-size: 0.75rem;
  font-weight: 650;
  white-space: nowrap;
}

.studio-chapter__body {
  padding: 0.35rem 0.55rem 0.65rem 0.35rem;
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
  gap: 0.15rem;
}

.studio-lesson {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.2rem;
  margin-left: 2.15rem;
  padding: 0.15rem 0.2rem 0.15rem 0.15rem;
  border-radius: 0.55rem;
  transition: background-color 0.12s ease, transform 0.12s ease;
}

.studio-lesson:hover {
  background: var(--color-surface-900);
  transform: translateX(2px);
}

.studio-lesson__rail {
  position: absolute;
  top: -0.35rem;
  bottom: 50%;
  left: -0.95rem;
  width: 0.85rem;
  border-left: 1.5px solid var(--color-border-subtle);
  border-bottom: 1.5px solid var(--color-border-subtle);
  border-bottom-left-radius: 0.45rem;
  pointer-events: none;
}

.studio-lesson:first-child .studio-lesson__rail {
  top: -0.15rem;
}

.studio-lesson__icon {
  display: inline-grid;
  place-items: center;
  flex-shrink: 0;
  width: 1.7rem;
  height: 1.7rem;
  border-radius: 0.4rem;
  background: var(--color-surface-900);
  color: var(--color-ink-muted);
}

.studio-lesson:hover .studio-lesson__icon {
  background: color-mix(in srgb, var(--color-accent-coral) 12%, var(--color-surface-900));
  color: var(--color-accent-coral-dark);
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

.studio-lesson__index {
  flex-shrink: 0;
  min-width: 1.8rem;
  color: var(--color-ink-faint);
  font-family: var(--font-mono);
  font-size: 0.75rem;
  font-weight: 600;
}

.studio-lesson__title {
  min-width: 0;
  overflow-wrap: anywhere;
}

.studio-lesson__open:hover .studio-lesson__title {
  color: var(--color-accent-coral-dark);
}

.studio-lesson-empty {
  position: relative;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.45rem 0.65rem;
  margin: 0.2rem 0 0.1rem 2.15rem;
  padding: 0.55rem 0.65rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.55rem;
  color: var(--color-ink-muted);
  font-size: 0.84rem;
}

.studio-lesson-empty__rail {
  position: absolute;
  top: -0.35rem;
  bottom: 50%;
  left: -0.95rem;
  width: 0.85rem;
  border-left: 1.5px solid var(--color-border-subtle);
  border-bottom: 1.5px solid var(--color-border-subtle);
  border-bottom-left-radius: 0.45rem;
  pointer-events: none;
}

.studio-lesson-empty p {
  margin: 0;
  flex: 1;
  min-width: 10rem;
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

.studio-details__head {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.studio-details__card h2 {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
}

.studio-details__summary {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 0.35rem 0.85rem;
  margin: 0 0 0.85rem;
  font-size: 0.88rem;
}

.studio-details__summary dt {
  color: var(--color-ink-muted);
  font-weight: 600;
}

.studio-details__summary dd {
  margin: 0;
  color: var(--color-ink);
  overflow-wrap: anywhere;
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

@media (max-width: 640px) {
  .studio-outline {
    padding: 0.9rem;
  }

  .studio-chapter__count {
    width: 100%;
    order: 5;
    margin-left: 3.2rem;
  }

  .studio-lesson {
    margin-left: 1.35rem;
  }

  .studio-lesson-empty {
    margin-left: 1.35rem;
  }

  .studio-topbar__summary {
    display: none;
  }
}
</style>
