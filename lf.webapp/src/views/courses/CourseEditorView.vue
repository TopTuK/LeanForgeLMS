<script setup>
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute } from 'vue-router';
import {
  fetchCourse,
  addChapter,
  renameChapter,
  moveChapter,
  addLesson,
  updateLesson,
  moveLesson,
  removeLesson,
  publishCourse,
} from '@/services/courseService';

const { t } = useI18n();
const route = useRoute();
const courseId = Number(route.params.id);

const course = ref(null);
const loading = ref(true);
const notFound = ref(false);
const forbidden = ref(false);
const errorMessage = ref('');
const publishing = ref(false);

async function loadCourse() {
  loading.value = true;
  notFound.value = false;
  forbidden.value = false;
  errorMessage.value = '';
  try {
    course.value = await fetchCourse(courseId);
  } catch (err) {
    if (err.response?.status === 404) notFound.value = true;
    else if (err.response?.status === 403) forbidden.value = true;
    else errorMessage.value = t('courses.editor.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(loadCourse);

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
const newLessonContent = ref('');
const newLessonIncludeInPreview = ref(false);

function openAddLessonModal(chapter) {
  addLessonChapter.value = chapter;
  newLessonTitle.value = '';
  newLessonContent.value = '';
  newLessonIncludeInPreview.value = false;
  addLessonModalShown.value = true;
}

function confirmAddLesson() {
  if (!newLessonTitle.value.trim()) return;
  runMutation(() => addLesson(courseId, addLessonChapter.value.id, {
    title: newLessonTitle.value,
    content: newLessonContent.value,
    includeInPreview: newLessonIncludeInPreview.value,
  }));
}

// Edit lesson
const editLessonModalShown = ref(false);
const editLessonChapter = ref(null);
const editLessonTarget = ref(null);
const editLessonTitleInput = ref('');
const editLessonContentInput = ref('');
const editLessonIncludeInPreviewInput = ref(false);

function openEditLessonModal(chapter, lesson) {
  editLessonChapter.value = chapter;
  editLessonTarget.value = lesson;
  editLessonTitleInput.value = lesson.title;
  editLessonContentInput.value = lesson.content;
  editLessonIncludeInPreviewInput.value = lesson.includeInPreview;
  editLessonModalShown.value = true;
}

function confirmEditLesson() {
  if (!editLessonTitleInput.value.trim()) return;
  runMutation(() => updateLesson(courseId, editLessonChapter.value.id, editLessonTarget.value.id, {
    title: editLessonTitleInput.value,
    content: editLessonContentInput.value,
    includeInPreview: editLessonIncludeInPreviewInput.value,
  }));
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
  <div class="course-editor">
    <template v-if="notFound">
      <va-alert color="warning">
        {{ $t('courses.editor.not_found') }}
      </va-alert>
      <router-link
        :to="{ name: 'CoursesCreate' }"
        class="course-editor__back"
      >
        {{ $t('courses.editor.back_to_courses') }}
      </router-link>
    </template>

    <template v-else-if="forbidden">
      <va-alert color="danger">
        {{ $t('courses.editor.forbidden') }}
      </va-alert>
      <router-link
        :to="{ name: 'CoursesCreate' }"
        class="course-editor__back"
      >
        {{ $t('courses.editor.back_to_courses') }}
      </router-link>
    </template>

    <template v-else-if="course">
      <div class="course-editor__header">
        <div>
          <h1 class="course-editor__title">
            {{ course.title }}
          </h1>
          <span
            class="course-editor__status"
            :class="course.isPublished ? 'course-editor__status--published' : 'course-editor__status--draft'"
          >
            {{ course.isPublished ? $t('courses.editor.published') : $t('courses.editor.draft') }}
          </span>
        </div>
        <va-button
          :loading="publishing"
          :disabled="course.isPublished"
          @click="publish"
        >
          {{ $t('courses.editor.publish') }}
        </va-button>
      </div>

      <va-alert
        v-if="errorMessage"
        color="danger"
        closeable
        class="course-editor__alert"
        @close="errorMessage = ''"
      >
        {{ errorMessage }}
      </va-alert>

      <div class="course-editor__chapters-header">
        <h2>{{ $t('courses.editor.chapters_title') }}</h2>
        <va-button
          preset="secondary"
          size="small"
          @click="openAddChapterModal"
        >
          {{ $t('courses.editor.add_chapter') }}
        </va-button>
      </div>

      <p
        v-if="course.chapters.length === 0"
        class="course-editor__empty"
      >
        {{ $t('courses.editor.no_chapters') }}
      </p>

      <div
        v-for="(chapter, chapterIndex) in course.chapters"
        :key="chapter.id"
        class="course-editor__chapter"
      >
        <div class="course-editor__chapter-header">
          <span class="course-editor__chapter-title">{{ chapter.title }}</span>
          <div class="course-editor__actions">
            <va-button
              preset="secondary"
              size="small"
              icon="arrow_upward"
              :disabled="chapterIndex === 0"
              :title="$t('courses.editor.move_up')"
              @click="moveChapterDirection(chapter, 'Up')"
            />
            <va-button
              preset="secondary"
              size="small"
              icon="arrow_downward"
              :disabled="chapterIndex === course.chapters.length - 1"
              :title="$t('courses.editor.move_down')"
              @click="moveChapterDirection(chapter, 'Down')"
            />
            <va-button
              preset="secondary"
              size="small"
              @click="openRenameChapterModal(chapter)"
            >
              {{ $t('courses.editor.rename_chapter') }}
            </va-button>
            <va-button
              preset="secondary"
              size="small"
              @click="openAddLessonModal(chapter)"
            >
              {{ $t('courses.editor.add_lesson') }}
            </va-button>
          </div>
        </div>

        <p
          v-if="chapter.lessons.length === 0"
          class="course-editor__empty course-editor__empty--lessons"
        >
          {{ $t('courses.editor.no_lessons') }}
        </p>

        <ul
          v-else
          class="course-editor__lessons"
        >
          <li
            v-for="(lesson, lessonIndex) in chapter.lessons"
            :key="lesson.id"
            class="course-editor__lesson"
          >
            <div>
              <span class="course-editor__lesson-title">{{ lesson.title }}</span>
              <span
                v-if="lesson.includeInPreview"
                class="course-editor__lesson-preview-badge"
              >
                {{ $t('courses.editor.include_in_preview_label') }}
              </span>
            </div>
            <div class="course-editor__actions">
              <va-button
                preset="secondary"
                size="small"
                icon="arrow_upward"
                :disabled="lessonIndex === 0"
                :title="$t('courses.editor.move_up')"
                @click="moveLessonDirection(chapter, lesson, 'Up')"
              />
              <va-button
                preset="secondary"
                size="small"
                icon="arrow_downward"
                :disabled="lessonIndex === chapter.lessons.length - 1"
                :title="$t('courses.editor.move_down')"
                @click="moveLessonDirection(chapter, lesson, 'Down')"
              />
              <va-button
                preset="secondary"
                size="small"
                @click="openEditLessonModal(chapter, lesson)"
              >
                {{ $t('courses.editor.rename_lesson') }}
              </va-button>
              <va-button
                preset="secondary"
                size="small"
                color="danger"
                @click="openRemoveLessonModal(chapter, lesson)"
              >
                {{ $t('courses.editor.remove_lesson') }}
              </va-button>
            </div>
          </li>
        </ul>
      </div>

      <router-link
        :to="{ name: 'CoursesCreate' }"
        class="course-editor__back"
      >
        {{ $t('courses.editor.back_to_courses') }}
      </router-link>
    </template>

    <va-modal
      v-model="addChapterModalShown"
      :title="$t('courses.editor.add_chapter')"
      :ok-text="$t('courses.editor.save')"
      @ok="confirmAddChapter"
    >
      <va-input
        v-model="newChapterTitle"
        :label="$t('courses.editor.chapter_title_placeholder')"
      />
    </va-modal>

    <va-modal
      v-model="renameChapterModalShown"
      :title="$t('courses.editor.rename_chapter')"
      :ok-text="$t('courses.editor.save')"
      @ok="confirmRenameChapter"
    >
      <va-input
        v-model="renameChapterTitleInput"
        :label="$t('courses.editor.chapter_title_placeholder')"
      />
    </va-modal>

    <va-modal
      v-model="addLessonModalShown"
      size="large"
      :title="$t('courses.editor.add_lesson')"
      :ok-text="$t('courses.editor.save')"
      @ok="confirmAddLesson"
    >
      <va-input
        v-model="newLessonTitle"
        class="course-editor__modal-field"
        :label="$t('courses.editor.lesson_title_label')"
      />
      <va-input
        v-model="newLessonContent"
        type="textarea"
        class="course-editor__modal-field"
        :label="$t('courses.editor.lesson_content_label')"
      />
      <va-checkbox
        v-model="newLessonIncludeInPreview"
        :label="$t('courses.editor.include_in_preview_label')"
      />
    </va-modal>

    <va-modal
      v-model="editLessonModalShown"
      size="large"
      :title="$t('courses.editor.rename_lesson')"
      :ok-text="$t('courses.editor.save')"
      @ok="confirmEditLesson"
    >
      <va-input
        v-model="editLessonTitleInput"
        class="course-editor__modal-field"
        :label="$t('courses.editor.lesson_title_label')"
      />
      <va-input
        v-model="editLessonContentInput"
        type="textarea"
        class="course-editor__modal-field"
        :label="$t('courses.editor.lesson_content_label')"
      />
      <va-checkbox
        v-model="editLessonIncludeInPreviewInput"
        :label="$t('courses.editor.include_in_preview_label')"
      />
    </va-modal>

    <va-modal
      v-model="removeLessonModalShown"
      :title="$t('courses.editor.remove_lesson')"
      :message="$t('courses.editor.remove_lesson_confirm', { title: removeLessonTarget?.title })"
      :ok-text="$t('courses.editor.remove_lesson')"
      @ok="confirmRemoveLesson"
    />
  </div>
</template>

<style scoped>
.course-editor {
    max-width: 48rem;
    margin: 0 auto;
}

.course-editor__header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 1rem;
}

.course-editor__title {
    font-size: 1.5rem;
    font-weight: 700;
}

.course-editor__status {
    display: inline-block;
    margin-top: 0.25rem;
    padding: 0.15rem 0.6rem;
    border-radius: 999px;
    font-size: 0.75rem;
    font-weight: 600;
}

.course-editor__status--draft {
    background: var(--va-background-element);
    color: var(--va-secondary);
}

.course-editor__status--published {
    background: var(--va-success);
    color: white;
}

.course-editor__alert {
    margin-bottom: 1rem;
}

.course-editor__chapters-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin: 1.5rem 0 0.75rem;
}

.course-editor__empty {
    color: var(--color-ink-muted, var(--va-secondary));
    padding: 0.75rem 0;
}

.course-editor__empty--lessons {
    padding-left: 1rem;
    font-size: 0.9rem;
}

.course-editor__chapter {
    border: 1px solid var(--va-background-border);
    border-radius: 0.75rem;
    padding: 1rem;
    margin-bottom: 1rem;
}

.course-editor__chapter-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.5rem;
}

.course-editor__chapter-title {
    font-weight: 600;
    font-size: 1.05rem;
}

.course-editor__actions {
    display: flex;
    gap: 0.4rem;
    flex-wrap: wrap;
}

.course-editor__lessons {
    list-style: none;
    padding: 0;
    margin: 0.75rem 0 0;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.course-editor__lesson {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.5rem;
    padding: 0.6rem 0.75rem;
    border-radius: 0.5rem;
    background: var(--va-background-element);
}

.course-editor__lesson-title {
    font-weight: 500;
}

.course-editor__lesson-preview-badge {
    margin-left: 0.5rem;
    font-size: 0.7rem;
    padding: 0.1rem 0.5rem;
    border-radius: 999px;
    background: var(--va-info);
    color: white;
}

.course-editor__modal-field {
    width: 100%;
    margin-bottom: 1rem;
}

.course-editor__back {
    display: inline-block;
    margin-top: 1.5rem;
}
</style>
