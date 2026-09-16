<script setup>
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { fetchCategories, fetchCourses, createCourse } from '@/services/courseService';
import CourseDetailsForm from '@/components/courses/form/CourseDetailsForm.vue';
import StudioShell from '@/components/courses/studio/StudioShell.vue';

const { t } = useI18n();
const router = useRouter();

const details = ref({
  title: '',
  shortIntroduction: '',
  description: '',
  categoryId: null,
  pricingType: 'Free',
  price: null,
  enrollmentMode: 'Open',
  coverType: 'Color',
  coverColor: 'Coral',
  coverImageStorageObjectId: null,
});
const categories = ref([]);

const submitting = ref(false);
const errorMessage = ref('');

const drafts = ref([]);
const draftsLoading = ref(false);
const draftsError = ref('');

async function loadCategories() {
  try {
    categories.value = await fetchCategories();
  } catch {
    errorMessage.value = t('courses.create.load_error');
  }
}

async function loadDrafts() {
  draftsLoading.value = true;
  draftsError.value = '';
  try {
    const result = await fetchCourses({ page: 1, pageSize: 50 });
    drafts.value = result.items.filter((c) => !c.isPublished);
  } catch {
    draftsError.value = t('courses.create.load_error');
  } finally {
    draftsLoading.value = false;
  }
}

onMounted(() => {
  loadCategories();
  loadDrafts();
});

function handleInvalid() {
  errorMessage.value = t('courses.create.validation_error');
}

async function handleSubmit() {
  errorMessage.value = '';
  submitting.value = true;
  const d = details.value;
  try {
    const course = await createCourse({
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
    router.push({ name: 'CourseEdit', params: { id: course.id } });
  } catch (err) {
    errorMessage.value = err.response?.status === 400
      ? t('courses.create.validation_error')
      : t('courses.create.load_error');
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <StudioShell>
    <header class="create-header">
      <router-link
        :to="{ name: 'CoursesAvailable' }"
        class="create-header__back"
      >
        {{ $t('courses.create.back') }}
      </router-link>
      <h1>{{ $t('courses.create.title') }}</h1>
      <p class="create-header__subtitle">
        {{ $t('courses.create.subtitle') }}
      </p>
    </header>

    <div
      v-if="errorMessage"
      class="create-alert"
      role="alert"
    >
      <span>{{ errorMessage }}</span>
      <button
        type="button"
        class="create-alert__close"
        :aria-label="$t('courses.create.dismiss_error')"
        @click="errorMessage = ''"
      >
        ×
      </button>
    </div>

    <div class="create-layout">
      <CourseDetailsForm
        v-model="details"
        :categories="categories"
        :submitting="submitting"
        :submit-label="$t('courses.create.submit')"
        :submitting-label="$t('courses.create.submitting')"
        @submit="handleSubmit"
        @invalid="handleInvalid"
      />

      <aside class="create-rail">
        <h2>{{ $t('courses.create.your_drafts_title') }}</h2>
        <p
          v-if="draftsError"
          class="create-hint create-hint--error"
        >
          {{ draftsError }}
        </p>
        <p
          v-else-if="draftsLoading"
          class="create-hint"
        >
          {{ $t('courses.create.drafts_loading') }}
        </p>
        <p
          v-else-if="drafts.length === 0"
          class="create-hint"
        >
          {{ $t('courses.create.your_drafts_empty') }}
        </p>
        <ul
          v-else
          class="create-drafts"
        >
          <li
            v-for="draft in drafts"
            :key="draft.id"
          >
            <router-link
              :to="{ name: 'CourseEdit', params: { id: draft.id } }"
              class="create-draft"
            >
              <span class="create-draft__title">{{ draft.title }}</span>
              <span class="create-draft__action">{{ $t('courses.create.edit_action') }}</span>
            </router-link>
          </li>
        </ul>
      </aside>
    </div>
  </StudioShell>
</template>

<style scoped>
.create-header {
  margin-bottom: 1.75rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid var(--color-border-subtle);
}

.create-header__back {
  display: inline-block;
  margin-bottom: 0.65rem;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
  font-weight: 600;
  text-decoration: none;
}

.create-header__back:hover {
  color: var(--color-ink);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.create-header h1 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.6rem, 3vw, 2.1rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.15;
}

.create-header__subtitle {
  max-width: 36rem;
  margin: 0.65rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
  line-height: 1.6;
}

.create-alert {
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

.create-alert__close {
  border: 0;
  background: transparent;
  color: inherit;
  font-size: 1.2rem;
  cursor: pointer;
}

.create-layout {
  display: grid;
  gap: 1.75rem;
}

@media (min-width: 960px) {
  .create-layout {
    grid-template-columns: minmax(0, 1.5fr) minmax(14rem, 0.7fr);
    align-items: start;
  }
}

.create-hint {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
}

.create-hint--error {
  color: var(--color-accent-coral-dark);
}

.create-rail {
  padding: 1.15rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.75rem;
  background: var(--color-surface-900);
}

.create-rail h2 {
  margin: 0 0 0.85rem;
  color: var(--color-ink);
  font-size: 0.95rem;
  font-weight: 700;
}

.create-drafts {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.create-draft {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.55rem 0.65rem;
  border-radius: 0.45rem;
  color: var(--color-ink);
  text-decoration: none;
}

.create-draft:hover {
  background: var(--color-surface-950);
}

.create-draft__title {
  font-size: 0.88rem;
  font-weight: 600;
  overflow-wrap: anywhere;
}

.create-draft__action {
  flex-shrink: 0;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 600;
}
</style>
