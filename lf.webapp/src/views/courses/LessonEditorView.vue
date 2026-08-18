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
import { fetchCourse, updateLesson } from '@/services/courseService';
import { useLessonPartStore } from '@/stores/lessonPartStore';
import ForgeField from '@/components/courses/forge/ForgeField.vue';
import ForgeStatusStamp from '@/components/courses/forge/ForgeStatusStamp.vue';
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
  partStore.ensureLoaded(lessonId.value, savedApiContent.value, lesson.parts ?? []);
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
    try {
      await partStore.commit(courseId.value, chapterId.value, lessonId.value);
    } catch (err) {
      if (generation !== saveGeneration) return true;
      errorMessage.value = err.response?.status === 403
        ? t('courses.lessonEditor.forbidden')
        : t('courses.lessonEditor.parts.save_error');
      return false;
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
  <div class="lesson-forge">
    <div
      class="lesson-forge__square"
      aria-hidden="true"
    />
    <div
      class="lesson-forge__curve"
      aria-hidden="true"
    />

    <div class="container mx-auto px-6 py-10 md:py-14 relative z-10">
      <template v-if="loading">
        <p class="lesson-forge__hint">
          {{ $t('courses.lessonEditor.loading') }}
        </p>
      </template>

      <template v-else-if="notFound">
        <div class="lesson-forge__state">
          <p class="lesson-forge__eyebrow">
            {{ $t('courses.lessonEditor.eyebrow') }}
          </p>
          <h1>{{ $t('courses.lessonEditor.not_found') }}</h1>
          <button
            type="button"
            class="lesson-forge__back-btn"
            @click="goBack"
          >
            {{ $t('courses.lessonEditor.back') }}
          </button>
        </div>
      </template>

      <template v-else-if="forbidden">
        <div class="lesson-forge__state">
          <p class="lesson-forge__eyebrow">
            {{ $t('courses.lessonEditor.eyebrow') }}
          </p>
          <h1>{{ $t('courses.lessonEditor.forbidden') }}</h1>
          <button
            type="button"
            class="lesson-forge__back-btn"
            @click="goBack"
          >
            {{ $t('courses.lessonEditor.back') }}
          </button>
        </div>
      </template>

      <template v-else-if="course && chapter">
        <header class="lesson-forge__header">
          <div class="lesson-forge__header-copy">
            <p class="lesson-forge__eyebrow">
              {{ $t('courses.lessonEditor.eyebrow') }}
            </p>
            <h1>{{ $t('courses.lessonEditor.title') }}</h1>
            <p class="lesson-forge__context">
              <span>{{ course.title }}</span>
              <span
                class="lesson-forge__context-sep"
                aria-hidden="true"
              >/</span>
              <span>{{ chapter.title }}</span>
            </p>
          </div>

          <div class="lesson-forge__header-actions">
            <div
              class="lesson-forge__mode"
              role="tablist"
              :aria-label="$t('courses.lessonEditor.preview.mode_label')"
            >
              <button
                type="button"
                class="lesson-forge__mode-btn"
                role="tab"
                :aria-selected="viewMode === 'edit'"
                :class="{ 'is-active': viewMode === 'edit' }"
                @click="viewMode = 'edit'"
              >
                {{ $t('courses.lessonEditor.preview.mode_edit') }}
              </button>
              <button
                type="button"
                class="lesson-forge__mode-btn"
                role="tab"
                :aria-selected="viewMode === 'preview'"
                :class="{ 'is-active': viewMode === 'preview' }"
                @click="viewMode = 'preview'"
              >
                {{ $t('courses.lessonEditor.preview.mode_preview') }}
              </button>
            </div>
            <span
              class="lesson-forge__save-status"
              :data-status="saveStatus"
              role="status"
            >
              {{ saveStatusLabel }}
            </span>
            <ForgeStatusStamp
              v-if="course.isPublished"
              variant="published"
              :label="$t('courses.editor.published')"
            />
            <ForgeStatusStamp
              v-else
              variant="draft"
              :label="$t('courses.editor.draft')"
            />
            <va-button
              preset="secondary"
              border-color="#c4c4c4"
              :disabled="!isDirty || saving"
              @click="discardChanges"
            >
              {{ $t('courses.lessonEditor.discard') }}
            </va-button>
            <va-button
              color="primary"
              :loading="saving"
              :disabled="(!isDirty && !saving) || partsUploading"
              @click="onSaveClick"
            >
              {{ $t('courses.lessonEditor.save') }}
            </va-button>
          </div>
        </header>

        <div
          v-if="errorMessage"
          class="lesson-forge__alert"
          role="alert"
        >
          <span>{{ errorMessage }}</span>
          <button
            type="button"
            class="lesson-forge__alert-close"
            :aria-label="$t('courses.lessonEditor.dismiss_error')"
            @click="errorMessage = ''"
          >
            ×
          </button>
        </div>

        <div
          v-if="viewMode === 'edit'"
          class="lesson-forge__panel"
        >
          <div class="lesson-forge__meta">
            <ForgeField
              v-model="title"
              class="lesson-forge__title-field"
              :label="$t('courses.lessonEditor.field_title')"
              index="01"
              required
            />
            <label class="lesson-forge__preview">
              <span class="lesson-forge__preview-label">
                {{ $t('courses.lessonEditor.include_in_preview') }}
              </span>
              <va-switch v-model="includeInPreview" />
            </label>
          </div>

          <div class="lesson-forge__body">
            <div class="lesson-forge__body-meta">
              <span class="lesson-forge__body-label">
                {{ $t('courses.lessonEditor.field_content') }}
              </span>
              <span
                class="lesson-forge__body-index"
                aria-hidden="true"
              >02</span>
            </div>
            <LessonPartsEditor
              :lesson-id="lessonId"
              :disabled="saving && !isDirty"
              @error="errorMessage = $event"
            />
          </div>
        </div>

        <LessonPreview
          v-else
          :title="title"
          :chapter-title="chapter.title"
          :parts="previewParts"
        />

        <button
          type="button"
          class="lesson-forge__back"
          @click="goBack"
        >
          {{ $t('courses.lessonEditor.back') }}
        </button>
      </template>
    </div>
  </div>
</template>

<style scoped>
.lesson-forge {
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

.lesson-forge__square,
.lesson-forge__curve {
  position: absolute;
  pointer-events: none;
  z-index: 0;
}

.lesson-forge__square {
  top: 4.5rem;
  right: 7%;
  width: 5.5rem;
  height: 5.5rem;
  border: 1px solid var(--industrial-line);
  transform: rotate(12deg);
  animation: lesson-forge-drift 10s ease-in-out infinite alternate;
}

.lesson-forge__square::after {
  content: "";
  position: absolute;
  inset: 0.65rem;
  background: var(--industrial-accent-wash);
  border: 1px solid var(--industrial-line);
}

.lesson-forge__curve {
  width: 14rem;
  height: 14rem;
  left: -4.5rem;
  bottom: 10%;
  border: 1px solid var(--industrial-line);
  border-radius: 50%;
  box-shadow: 0 0 0 32px var(--industrial-grid);
}

.lesson-forge__header {
  display: flex;
  flex-wrap: wrap;
  align-items: end;
  justify-content: space-between;
  gap: 1.25rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid var(--industrial-line);
  animation: lesson-forge-rise 0.4s ease both;
}

.lesson-forge__eyebrow {
  margin: 0 0 0.55rem;
  color: var(--color-accent-coral);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.lesson-forge__header-copy h1 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.6rem, 2.4vw, 2.1rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.15;
}

.lesson-forge__context {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem;
  margin: 0.55rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.92rem;
}

.lesson-forge__context-sep {
  color: var(--color-ink-faint);
}

.lesson-forge__header-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.65rem;
}

.lesson-forge__mode {
  display: inline-flex;
  padding: 0.15rem;
  border: 1px solid var(--industrial-line-strong);
  border-radius: 0.3rem;
  background: var(--color-surface-950);
}

.lesson-forge__mode-btn {
  padding: 0.4rem 0.75rem;
  border: 0;
  border-radius: 0.2rem;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  cursor: pointer;
}

.lesson-forge__mode-btn.is-active {
  background: var(--industrial-accent-wash);
  color: var(--color-ink);
}

.lesson-forge__mode-btn:hover:not(.is-active) {
  color: var(--color-ink);
}

.lesson-forge__save-status {
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.lesson-forge__save-status[data-status='unsaved'] {
  color: var(--color-accent-coral-dark);
}

.lesson-forge__save-status[data-status='saving'] {
  color: var(--color-ink);
}

.lesson-forge__alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
  padding: 0.85rem 1rem;
  color: var(--color-ink);
  background: color-mix(in srgb, var(--color-accent-coral) 12%, var(--color-surface-950));
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.35rem;
}

.lesson-forge__alert-close {
  border: 0;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
}

.lesson-forge__panel {
  display: flex;
  flex-direction: column;
  gap: 1.35rem;
  padding: 1.35rem 1.35rem 1.5rem;
  background: var(--industrial-panel);
  border: 1px solid var(--industrial-line-strong);
  border-radius: 0.4rem;
  box-shadow: 0 18px 40px -28px rgb(15 23 42 / 0.45);
  animation: lesson-forge-rise 0.45s ease both;
}

.lesson-forge__meta {
  display: grid;
  gap: 1rem;
}

@media (min-width: 768px) {
  .lesson-forge__meta {
    grid-template-columns: minmax(0, 1fr) auto;
    align-items: end;
  }
}

.lesson-forge__preview {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  min-height: 3.1rem;
  padding: 0.75rem 1rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.35rem;
  background: var(--color-surface-950);
}

.lesson-forge__preview-label {
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.lesson-forge__body-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-bottom: 0.5rem;
}

.lesson-forge__body-label {
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.lesson-forge__body-index {
  color: var(--color-ink-faint);
  font-size: 0.65rem;
  font-weight: 700;
  letter-spacing: 0.14em;
}

.lesson-forge__hint,
.lesson-forge__state h1 {
  color: var(--color-ink);
}

.lesson-forge__hint {
  margin: 2rem 0;
  font-size: 0.95rem;
}

.lesson-forge__state {
  max-width: 36rem;
  padding: 2rem 0;
}

.lesson-forge__state h1 {
  margin: 0 0 1.25rem;
  font-size: 1.75rem;
  font-weight: 800;
}

.lesson-forge__back,
.lesson-forge__back-btn {
  display: inline-flex;
  margin-top: 1.75rem;
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

.lesson-forge__back:hover,
.lesson-forge__back-btn:hover {
  color: var(--color-accent-coral-dark);
}

@keyframes lesson-forge-drift {
  from { transform: rotate(12deg) translateY(0); }
  to { transform: rotate(18deg) translateY(0.6rem); }
}

@keyframes lesson-forge-rise {
  from {
    opacity: 0;
    transform: translateY(0.55rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
