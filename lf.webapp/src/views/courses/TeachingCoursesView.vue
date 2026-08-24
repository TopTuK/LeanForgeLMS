<script setup>
import { computed, inject, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import CourseCard from '@/components/courses/CourseCard.vue';
import { fetchCourses, fetchCourseCoverImageObjectUrl } from '@/services/courseService';
import { useCourseCoverImages } from '@/composables/useCourseCoverImages';

const { t } = useI18n();
const router = useRouter();

const courses = ref([]);
const loading = ref(false);
const errorMessage = ref('');
const { coverImageUrls, load: loadCoverImages } = useCourseCoverImages(fetchCourseCoverImageObjectUrl);
const searchQuery = inject('courseSearch', ref(''));
const visibleCourses = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();
  if (!query) return courses.value;
  return courses.value.filter((course) => (
    course.title.toLowerCase().includes(query)
    || (course.shortIntroduction ?? '').toLowerCase().includes(query)
  ));
});

async function loadCourses() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchCourses({ page: 1, pageSize: 50 });
    courses.value = result.items;
    await loadCoverImages(result.items, (c) => c.id);
  } catch {
    errorMessage.value = t('courses.teaching.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(loadCourses);

function onManage(courseId) {
  router.push({ name: 'CourseEdit', params: { id: courseId } });
}
</script>

<template>
  <div>
    <div class="catalog-section-heading mb-8">
      <span
        class="catalog-section-index"
        aria-hidden="true"
      >04</span>
      <div>
        <h2 class="text-xl font-bold text-ink">
          {{ $t('courses.teaching.title') }}
        </h2>
        <p class="mt-2 text-sm text-ink-muted leading-relaxed">
          {{ $t('courses.teaching.subtitle') }}
        </p>
      </div>
    </div>

    <p
      v-if="errorMessage"
      class="catalog-state-panel catalog-state-panel--error mb-4"
    >
      {{ errorMessage }}
    </p>

    <p
      v-if="loading"
      class="text-sm text-ink-muted"
    >
      {{ $t('courses.loading') }}
    </p>
    <div
      v-else-if="visibleCourses.length"
      class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5"
    >
      <CourseCard
        v-for="(course, idx) in visibleCourses"
        :key="course.id"
        status="teaching"
        :index="idx"
        :title="course.title"
        :description="course.shortIntroduction"
        :category="course.categoryName"
        :is-published="course.isPublished ?? null"
        :cover-type="course.coverType"
        :cover-color="course.coverColor"
        :cover-image-url="coverImageUrls[course.id] ?? null"
        @manage="onManage(course.id)"
      />
    </div>
    <p
      v-else
      class="catalog-state-panel"
    >
      {{ $t('courses.teaching.empty') }}
    </p>
  </div>
</template>
