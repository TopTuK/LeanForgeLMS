<script setup>
import { onMounted, ref } from 'vue';
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
    <div class="bay-section-heading mb-8">
      <span
        class="bay-section-index"
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
      class="bay-state-panel bay-state-panel--error mb-4"
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
      v-else-if="courses.length"
      class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5"
    >
      <CourseCard
        v-for="(course, idx) in courses"
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
      class="bay-state-panel"
    >
      {{ $t('courses.teaching.empty') }}
    </p>
  </div>
</template>
