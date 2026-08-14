<script setup>
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { fetchCategories, fetchCourses, createCourse } from '@/services/courseService';

const { t } = useI18n();
const router = useRouter();

const title = ref('');
const shortIntroduction = ref('');
const description = ref('');
const category = ref(null);
const categories = ref([]);

const submitting = ref(false);
const errorMessage = ref('');

const drafts = ref([]);
const draftsLoading = ref(false);
const draftsError = ref('');

const categoryOptions = computed(() => categories.value.map((c) => ({ value: c.id, text: c.name })));

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
    drafts.value = result.items;
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

async function submit() {
  errorMessage.value = '';

  if (!title.value.trim() || !shortIntroduction.value.trim() || !description.value.trim() || !category.value) {
    errorMessage.value = t('courses.create.validation_error');
    return;
  }

  submitting.value = true;
  try {
    const course = await createCourse({
      title: title.value,
      shortIntroduction: shortIntroduction.value,
      description: description.value,
      categoryId: category.value,
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
  <div class="course-create">
    <h1 class="course-create__title">
      {{ $t('courses.create.title') }}
    </h1>
    <p class="course-create__subtitle">
      {{ $t('courses.create.subtitle') }}
    </p>

    <va-alert
      v-if="errorMessage"
      color="danger"
      closeable
      class="course-create__alert"
      @close="errorMessage = ''"
    >
      {{ errorMessage }}
    </va-alert>

    <div class="course-create__card">
      <va-input
        v-model="title"
        class="course-create__field"
        :label="$t('courses.create.field_title')"
      />
      <va-input
        v-model="shortIntroduction"
        type="textarea"
        class="course-create__field"
        :label="$t('courses.create.field_short_introduction')"
      />
      <va-input
        v-model="description"
        type="textarea"
        class="course-create__field"
        :label="$t('courses.create.field_description')"
      />
      <va-select
        v-model="category"
        class="course-create__field"
        :label="$t('courses.create.field_category')"
        :placeholder="$t('courses.create.category_placeholder')"
        :options="categoryOptions"
        text-by="text"
        value-by="value"
      />

      <va-button
        :loading="submitting"
        @click="submit"
      >
        {{ $t('courses.create.submit') }}
      </va-button>
    </div>

    <div class="course-create__coming-soon">
      <h2 class="course-create__coming-soon-title">
        {{ $t('courses.create.coming_soon_section_title') }}
      </h2>
      <p>{{ $t('courses.create.coming_soon') }}</p>
    </div>

    <div class="course-create__drafts">
      <h2 class="course-create__drafts-title">
        {{ $t('courses.create.your_drafts_title') }}
      </h2>

      <va-alert
        v-if="draftsError"
        color="danger"
        closeable
        @close="draftsError = ''"
      >
        {{ draftsError }}
      </va-alert>

      <p
        v-else-if="!draftsLoading && drafts.length === 0"
        class="course-create__drafts-empty"
      >
        {{ $t('courses.create.your_drafts_empty') }}
      </p>

      <ul
        v-else
        class="course-create__drafts-list"
      >
        <li
          v-for="draft in drafts"
          :key="draft.id"
          class="course-create__drafts-item"
        >
          <span>{{ draft.title }}</span>
          <router-link :to="{ name: 'CourseEdit', params: { id: draft.id } }">
            {{ $t('courses.create.edit_action') }}
          </router-link>
        </li>
      </ul>
    </div>

    <router-link
      :to="{ name: 'CoursesAvailable' }"
      class="course-create__back"
    >
      {{ $t('courses.create.back') }}
    </router-link>
  </div>
</template>

<style scoped>
.course-create {
    max-width: 40rem;
    margin: 0 auto;
}

.course-create__title {
    font-size: 1.5rem;
    font-weight: 700;
}

.course-create__subtitle {
    color: var(--color-ink-muted, var(--va-secondary));
    margin-bottom: 1.5rem;
}

.course-create__alert {
    margin-bottom: 1rem;
}

.course-create__card {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 1.5rem;
    border-radius: 0.75rem;
    border: 1px solid var(--va-background-border);
    margin-bottom: 1.5rem;
}

.course-create__field {
    width: 100%;
}

.course-create__coming-soon {
    padding: 1rem 1.5rem;
    border-radius: 0.75rem;
    border: 1px dashed var(--va-background-border);
    color: var(--color-ink-muted, var(--va-secondary));
    margin-bottom: 1.5rem;
}

.course-create__coming-soon-title {
    font-size: 1rem;
    font-weight: 600;
    margin-bottom: 0.25rem;
}

.course-create__drafts-title {
    font-size: 1.1rem;
    font-weight: 600;
    margin-bottom: 0.75rem;
}

.course-create__drafts-list {
    list-style: none;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding: 0;
}

.course-create__drafts-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem 1rem;
    border-radius: 0.5rem;
    border: 1px solid var(--va-background-border);
}

.course-create__drafts-empty {
    color: var(--color-ink-muted, var(--va-secondary));
}

.course-create__back {
    display: inline-block;
    margin-top: 1.5rem;
}
</style>
