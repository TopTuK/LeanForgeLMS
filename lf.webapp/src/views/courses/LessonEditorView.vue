<script setup>
import {
  computed,
  onMounted,
  onUnmounted,
  ref,
  watch,
} from 'vue';
import { storeToRefs } from 'pinia';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { ChevronLeft } from 'lucide-vue-next';
import { fetchCourse, updateLesson } from '@/services/courseService';
import { useLessonPartStore } from '@/stores/lessonPartStore';
import StudioShell from '@/components/courses/studio/StudioShell.vue';
import StudioButton from '@/components/courses/studio/StudioButton.vue';
import LessonPartsEditor from '@/components/courses/lesson/LessonPartsEditor.vue';
import LessonPreview from '@/components/courses/lesson/LessonPreview.vue';

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const partStore = useLessonPartStore();
const { revision: partsRevision } = storeToRefs(partStore);

const courseId = computed(() => Number(route.params.courseId));
const chapterId = computed(() => Number(route.params.chapterId));
const lessonId = computed(() => Number(route.params.lessonId));

const course = ref(null);
const chapter = ref(null);
const loading = ref(true);
const notFound = ref(false);
const forbidden = ref(false);
const errorMessage = ref('');

const title = ref('');
const includeInPreview = ref(false);
const savedApiContent = ref('');

const savedTitle = ref('');
const savedIncludeInPreview = ref(false);

const saving = ref(false);
const loadReady = ref(false);
const viewMode = ref('edit');
let saveGeneration = 0;

const previewParts = computed(() => {
  partsRevision.value;
  return partStore.partsFor(lessonId.value);
});

const partsDirty = computed(() => {
  partsRevision.value;
  return partStore.isDirty(lessonId.value);
});

const partsUploading = computed(() => {
  partsRevision.value;
  return partStore.hasPendingUploads(lessonId.value);
});

const isDirty = computed(() =>
  title.value !== savedTitle.value
  || includeInPreview.value !== savedIncludeInPreview.value
  || partsDirty.value,
);

const saveStatus = computed(() => {
  if (saving.value) return 'saving';
  if (isDirty.value) return 'unsaved';
  return 'saved';
});

const saveStatusLabel = computed(() => {
  if (saveStatus.value === 'saving') return t('courses.lessonEditor.saving');
  if (saveStatus.value === 'unsaved') return t('courses.lessonEditor.unsaved');
  return t('courses.lessonEditor.saved');
});

function applyLesson(courseData) {
  const ch = courseData.chapters.find((c) => c.id === chapterId.value);
  const lesson = ch?.lessons.find((l) => l.id === lessonId.value);
  if (!ch || !lesson) {
    notFound.value = true;
    return false;
  }

  course.value = courseData;
  chapter.value = ch;
  title.value = lesson.title ?? '';
  includeInPreview.value = Boolean(lesson.includeInPreview);
  savedApiContent.value = lesson.content ?? '';
  savedTitle.value = title.value;
  savedIncludeInPreview.value = includeInPreview.value;
  partStore.ensureLoaded(lessonId.value, savedApiContent.value, lesson.parts ?? [], {
    courseId: courseId.value,
    chapterId: chapterId.value,
  });
  return true;
}

async function load() {
  loading.value = true;
  loadReady.value = false;
  notFound.value = false;
  forbidden.value = false;
  errorMessage.value = '';
  viewMode.value = 'edit';

  try {
    const data = await fetchCourse(courseId.value);
    if (!applyLesson(data)) return;
  } catch (err) {
    if (err.response?.status === 404) notFound.value = true;
    else if (err.response?.status === 403) forbidden.value = true;
    else errorMessage.value = t('courses.lessonEditor.load_error');
  } finally {
    loading.value = false;
    loadReady.value = !notFound.value && !forbidden.value && !!course.value;
  }
}

async function persist() {
  if (!loadReady.value) return false;
  if (!isDirty.value) return true;
  if (!title.value.trim()) {
    errorMessage.value = t('courses.lessonEditor.title_required');
    return false;
  }
  if (partsUploading.value) {
    errorMessage.value = t('courses.lessonEditor.parts.uploads_pending');
    return false;
  }

  const generation = ++saveGeneration;
  const metaDirty = title.value !== savedTitle.value
    || includeInPreview.value !== savedIncludeInPreview.value;

  errorMessage.value = '';

  if (metaDirty) {
    saving.value = true;
    const payload = {
      title: title.value.trim(),
      content: savedApiContent.value,
      includeInPreview: includeInPreview.value,
    };

    try {
      const data = await updateLesson(
        courseId.value,
        chapterId.value,
        lessonId.value,
        payload,
      );
      if (generation !== saveGeneration) return true;
      course.value = data;
      chapter.value = data.chapters.find((c) => c.id === chapterId.value) ?? chapter.value;
      savedTitle.value = payload.title;
      savedIncludeInPreview.value = payload.includeInPreview;
      title.value = payload.title;
    } catch (err) {
      if (err.response?.status === 403) {
        errorMessage.value = t('courses.lessonEditor.forbidden');
      } else if (err.response?.status === 404) {
        errorMessage.value = t('courses.lessonEditor.not_found');
        notFound.value = true;
      } else {
        errorMessage.value = t('courses.lessonEditor.save_error');
      }
      return false;
    } finally {
      if (generation === saveGeneration) saving.value = false;
    }
  }

  if (generation === saveGeneration && partStore.isDirty(lessonId.value)) {
    saving.value = true;
    try {
      await partStore.commit(courseId.value, chapterId.value, lessonId.value);
    } catch (err) {
      if (generation !== saveGeneration) return true;
      errorMessage.value = err.response?.status === 403
        ? t('courses.lessonEditor.forbidden')
        : t('courses.lessonEditor.parts.save_error');
      return false;
    } finally {
      if (generation === saveGeneration) saving.value = false;
    }
  }
  return true;
}

function onKeydown(event) {
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
    event.preventDefault();
    persist();
  }
}

onMounted(() => {
  load();
  window.addEventListener('keydown', onKeydown);
});

onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown);
});

watch(
  () => [route.params.courseId, route.params.chapterId, route.params.lessonId],
  () => {
    load();
  },
);

function goBack() {
  router.push({ name: 'CourseEdit', params: { id: courseId.value } });
}

async function onSaveClick() {
  await persist();
}

function discardChanges() {
  title.value = savedTitle.value;
  includeInPreview.value = savedIncludeInPreview.value;
  partStore.discard(lessonId.value);
  errorMessage.value = '';
}
</script>

<template>
  <StudioShell narrow>
    <template v-if="loading">
      <p class="lesson-studio__hint">
        {{ $t('courses.lessonEditor.loading') }}
      </p>
    </template>

    <template v-else-if="notFound || forbidden">
      <div class="lesson-studio__state">
        <h1>{{ notFound ? $t('courses.lessonEditor.not_found') : $t('courses.lessonEditor.forbidden') }}</h1>
        <StudioButton
          variant="ghost"
          @click="goBack"
        >
          <ChevronLeft :size="16" />
          {{ $t('courses.lessonEditor.back') }}
        </StudioButton>
      </div>
    </template>

    <template v-else-if="course && chapter">
      <header class="lesson-studio__bar">
        <button
          type="button"
          class="lesson-studio__back"
          @click="goBack"
        >
          <ChevronLeft :size="16" />
          <span>{{ chapter.title }}</span>
        </button>

        <div class="lesson-studio__bar-actions">
          <div
            class="lesson-studio__mode"
            role="tablist"
            :aria-label="$t('courses.lessonEditor.preview.mode_label')"
          >
            <button
              type="button"
              class="lesson-studio__mode-btn"
              role="tab"
              :aria-selected="viewMode === 'edit'"
              :class="{ 'is-active': viewMode === 'edit' }"
              @click="viewMode = 'edit'"
            >
              {{ $t('courses.lessonEditor.preview.mode_edit') }}
            </button>
            <button
              type="button"
              class="lesson-studio__mode-btn"
              role="tab"
              :aria-selected="viewMode === 'preview'"
              :class="{ 'is-active': viewMode === 'preview' }"
              @click="viewMode = 'preview'"
            >
              {{ $t('courses.lessonEditor.preview.mode_preview') }}
            </button>
          </div>

          <span
            class="lesson-studio__status"
            :data-status="saveStatus"
            role="status"
          >
            {{ saveStatusLabel }}
          </span>

          <StudioButton
            variant="ghost"
            size="sm"
            :disabled="!isDirty || saving"
            @click="discardChanges"
          >
            {{ $t('courses.lessonEditor.discard') }}
          </StudioButton>
          <StudioButton
            variant="primary"
            size="sm"
            :disabled="(!isDirty && !saving) || partsUploading"
            @click="onSaveClick"
          >
            {{ saving ? $t('courses.lessonEditor.saving') : $t('courses.lessonEditor.save') }}
          </StudioButton>
        </div>
      </header>

      <div
        v-if="errorMessage"
        class="lesson-studio__alert"
        role="alert"
      >
        <span>{{ errorMessage }}</span>
        <button
          type="button"
          class="lesson-studio__alert-close"
          :aria-label="$t('courses.lessonEditor.dismiss_error')"
          @click="errorMessage = ''"
        >
          ×
        </button>
      </div>

      <div
        v-if="viewMode === 'edit'"
        class="lesson-studio__canvas"
      >
        <input
          v-model="title"
          class="lesson-studio__title"
          type="text"
          :placeholder="$t('courses.lessonEditor.field_title')"
          :aria-label="$t('courses.lessonEditor.field_title')"
        >

        <label class="lesson-studio__preview-toggle">
          <input
            v-model="includeInPreview"
            type="checkbox"
          >
          <span>{{ $t('courses.lessonEditor.include_in_preview') }}</span>
        </label>

        <p class="lesson-studio__context">
          {{ course.title }}
        </p>

        <LessonPartsEditor
          :lesson-id="lessonId"
          :disabled="saving && !isDirty"
          @error="errorMessage = $event"
        />
      </div>

      <LessonPreview
        v-else
        :title="title"
        :chapter-title="chapter.title"
        :parts="previewParts"
      />
    </template>
  </StudioShell>
</template>

<style scoped>
.lesson-studio__hint {
  margin: 2rem 0;
  color: var(--color-ink-muted);
}

.lesson-studio__state {
  padding: 2rem 0;
}

.lesson-studio__state h1 {
  margin: 0 0 1rem;
  font-size: 1.6rem;
  font-weight: 800;
}

.lesson-studio__bar {
  position: sticky;
  top: 0;
  z-index: 20;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin: 0 -0.25rem 1.25rem;
  padding: 0.85rem 0.25rem;
  background: color-mix(in srgb, var(--color-surface-950) 92%, transparent);
  backdrop-filter: blur(10px);
  border-bottom: 1px solid var(--color-border-subtle);
}

.lesson-studio__back {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.35rem 0.4rem;
  border: 0;
  border-radius: 0.45rem;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
}

.lesson-studio__back:hover {
  background: var(--color-surface-900);
  color: var(--color-ink);
}

.lesson-studio__bar-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.45rem;
}

.lesson-studio__mode {
  display: inline-flex;
  padding: 0.15rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.5rem;
  background: var(--color-surface-900);
}

.lesson-studio__mode-btn {
  padding: 0.35rem 0.7rem;
  border: 0;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
}

.lesson-studio__mode-btn.is-active {
  background: var(--color-surface-950);
  color: var(--color-ink);
  box-shadow: 0 1px 2px rgb(15 23 42 / 0.06);
}

.lesson-studio__status {
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 600;
}

.lesson-studio__status[data-status='unsaved'] {
  color: var(--color-accent-coral-dark);
}

.lesson-studio__alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
  padding: 0.8rem 1rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.6rem;
  background: var(--color-accent-soft);
  color: var(--color-ink);
  font-size: 0.9rem;
}

.lesson-studio__alert-close {
  border: 0;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 1.2rem;
  cursor: pointer;
}

.lesson-studio__canvas {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-bottom: 4rem;
}

.lesson-studio__title {
  width: 100%;
  padding: 0.35rem 0;
  border: 0;
  background: transparent;
  color: var(--color-ink);
  font-family: inherit;
  font-size: clamp(1.85rem, 4vw, 2.4rem);
  font-weight: 800;
  letter-spacing: -0.035em;
  line-height: 1.15;
  outline: none;
}

.lesson-studio__title::placeholder {
  color: var(--color-ink-faint);
}

.lesson-studio__preview-toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--color-ink-muted);
  font-size: 0.85rem;
  font-weight: 560;
  cursor: pointer;
  user-select: none;
}

.lesson-studio__preview-toggle input {
  width: 0.95rem;
  height: 0.95rem;
  accent-color: var(--color-accent-coral);
}

.lesson-studio__context {
  margin: 0 0 0.5rem;
  color: var(--color-ink-faint);
  font-size: 0.82rem;
}
</style>
