<script setup>
import { onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import {
  fetchCategories,
  fetchCourse,
  fetchCourseCoverImageObjectUrl,
  updateCourse,
} from '@/services/courseService';
import CourseDetailsForm from '@/components/courses/form/CourseDetailsForm.vue';
import StudioShell from '@/components/courses/studio/StudioShell.vue';

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const courseId = Number(route.params.id);

const course = ref(null);
const details = ref(null);
const categories = ref([]);
const coverImageUrl = ref('');

const loading = ref(true);
const notFound = ref(false);
const forbidden = ref(false);
const submitting = ref(false);
const errorMessage = ref('');

function toDetails(source) {
  return {
    title: source.title,
    shortIntroduction: source.shortIntroduction,
    description: source.description,
    categoryId: source.categoryId,
    pricingType: source.pricingType,
    price: source.price,
    enrollmentMode: source.enrollmentMode,
    coverType: source.coverType === 'Image' ? 'Image' : 'Color',
    coverColor: source.coverColor ?? 'Coral',
    // Null keeps the current image on the server; set only after a new upload.
    coverImageStorageObjectId: null,
  };
}

async function loadCourse() {
  try {
    const [loadedCourse, loadedCategories] = await Promise.all([fetchCourse(courseId), fetchCategories()]);
    course.value = loadedCourse;
    categories.value = loadedCategories;
    details.value = toDetails(loadedCourse);

    if (loadedCourse.coverType === 'Image' && !loadedCourse.isPublished) {
      try {
        coverImageUrl.value = await fetchCourseCoverImageObjectUrl(courseId);
      } catch {
        coverImageUrl.value = '';
      }
    }
  } catch (err) {
    if (err.response?.status === 404) notFound.value = true;
    else if (err.response?.status === 403) forbidden.value = true;
    else errorMessage.value = t('courses.settings.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(loadCourse);

onBeforeUnmount(() => {
  if (coverImageUrl.value) URL.revokeObjectURL(coverImageUrl.value);
});

function handleInvalid() {
  errorMessage.value = t('courses.settings.validation_error');
}

async function handleSubmit() {
  errorMessage.value = '';
  submitting.value = true;
  const d = details.value;
  try {
    await updateCourse(courseId, {
      title: d.title,
      shortIntroduction: d.shortIntroduction,
      description: d.description,
      categoryId: d.categoryId,
      pricingType: d.pricingType,
      price: d.pricingType === 'Paid' ? Number(d.price) : null,
      enrollmentMode: d.enrollmentMode,
      coverType: d.coverType,
      coverColor: d.coverType === 'Color' ? d.coverColor : null,
      coverImageStorageObjectId: d.coverType === 'Image' ? d.coverImageStorageObjectId : null,
    });
    router.push({ name: 'CourseEdit', params: { id: courseId } });
  } catch (err) {
    const status = err.response?.status;
    if (status === 409) errorMessage.value = t('courses.settings.conflict_error');
    else if (status === 400) errorMessage.value = t('courses.settings.validation_error');
    else errorMessage.value = t('courses.settings.save_error');
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <StudioShell>
    <p
      v-if="loading"
      class="settings-hint"
    >
      {{ $t('courses.editor.loading') }}
    </p>

    <div
      v-else-if="notFound || forbidden"
      class="settings-state"
    >
      <h1>{{ notFound ? $t('courses.editor.not_found') : $t('courses.editor.forbidden') }}</h1>
      <router-link
        :to="{ name: 'CoursesCreate' }"
        class="settings-link"
      >
        {{ $t('courses.editor.back_to_courses') }}
      </router-link>
    </div>

    <template v-else>
      <header class="settings-header">
        <router-link
          :to="{ name: 'CourseEdit', params: { id: courseId } }"
          class="settings-header__back"
        >
          {{ $t('courses.settings.back') }}
        </router-link>
        <h1>{{ $t('courses.settings.title') }}</h1>
        <p class="settings-header__subtitle">
          {{ $t('courses.settings.subtitle') }}
        </p>
      </header>

      <div
        v-if="errorMessage"
        class="settings-alert"
        role="alert"
      >
        <span>{{ errorMessage }}</span>
        <button
          type="button"
          class="settings-alert__close"
          :aria-label="$t('courses.editor.dismiss_error')"
          @click="errorMessage = ''"
        >
          ×
        </button>
      </div>

      <div
        v-if="course?.isPublished"
        class="settings-locked"
        role="status"
      >
        <p>{{ $t('courses.settings.locked_published') }}</p>
        <router-link
          :to="{ name: 'CourseEdit', params: { id: courseId } }"
          class="settings-link"
        >
          {{ $t('courses.settings.back') }}
        </router-link>
      </div>

      <div
        v-else-if="details"
        class="settings-layout"
      >
        <CourseDetailsForm
          v-model="details"
          :categories="categories"
          :submitting="submitting"
          :submit-label="$t('courses.settings.submit')"
          :submitting-label="$t('courses.settings.submitting')"
          :existing-cover-image-url="coverImageUrl"
          :has-existing-cover-image="course.coverType === 'Image'"
          @submit="handleSubmit"
          @invalid="handleInvalid"
        >
          <template #actions>
            <router-link
              :to="{ name: 'CourseEdit', params: { id: courseId } }"
              class="settings-link settings-link--muted"
            >
              {{ $t('courses.editor.cancel') }}
            </router-link>
          </template>
        </CourseDetailsForm>
      </div>
    </template>
  </StudioShell>
</template>

<style scoped>
.settings-hint {
  margin: 2rem 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
}

.settings-state h1 {
  margin: 0 0 1rem;
  font-size: 1.75rem;
  font-weight: 800;
  letter-spacing: -0.03em;
}

.settings-link {
  color: var(--color-accent-coral-dark);
  font-size: 0.9rem;
  font-weight: 600;
  text-decoration: none;
}

.settings-link:hover {
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.settings-link--muted {
  color: var(--color-ink-muted);
}

.settings-header {
  margin-bottom: 1.75rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid var(--color-border-subtle);
}

.settings-header__back {
  display: inline-block;
  margin-bottom: 0.65rem;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
  font-weight: 600;
  text-decoration: none;
}

.settings-header__back:hover {
  color: var(--color-ink);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.settings-header h1 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.6rem, 3vw, 2.1rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.15;
}

.settings-header__subtitle {
  max-width: 36rem;
  margin: 0.65rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
  line-height: 1.6;
}

.settings-alert {
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

.settings-alert__close {
  border: 0;
  background: transparent;
  color: inherit;
  font-size: 1.2rem;
  cursor: pointer;
}

.settings-locked {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  max-width: 40rem;
  padding: 1.15rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.75rem;
  background: var(--color-surface-900);
}

.settings-locked p {
  margin: 0;
  color: var(--color-ink);
  font-size: 0.95rem;
  line-height: 1.55;
}

.settings-layout {
  max-width: 48rem;
}
</style>
