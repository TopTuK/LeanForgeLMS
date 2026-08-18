<script setup>
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import CourseCard from '@/components/courses/CourseCard.vue';
import { fetchCatalog, enroll, fetchCourseCoverImageObjectUrl } from '@/services/enrollmentService';
import { useCourseCoverImages } from '@/composables/useCourseCoverImages';

const { t } = useI18n();

const courses = ref([]);
const loading = ref(false);
const errorMessage = ref('');
const enrollingId = ref(null);
const { coverImageUrls, load: loadCoverImages } = useCourseCoverImages(fetchCourseCoverImageObjectUrl);

async function loadCatalog() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchCatalog({ page: 1, pageSize: 50 });
    courses.value = result.items;
    await loadCoverImages(result.items, (c) => c.id);
  } catch {
    errorMessage.value = t('courses.available.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(loadCatalog);

async function onEnroll(courseId) {
  enrollingId.value = courseId;
  errorMessage.value = '';
  try {
    await enroll(courseId);
    courses.value = courses.value.filter((c) => c.id !== courseId);
  } catch {
    errorMessage.value = t('courses.available.enroll_error');
  } finally {
    enrollingId.value = null;
  }
}
</script>

<template>
  <div>
    <div class="bay-section-heading mb-8">
      <span
        class="bay-section-index"
        aria-hidden="true"
      >01</span>
      <div>
        <h2 class="text-xl font-bold text-ink">
          {{ $t('courses.available.title') }}
        </h2>
        <p class="mt-2 text-sm text-ink-muted leading-relaxed">
          {{ $t('courses.available.subtitle') }}
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
        status="available"
        :index="idx"
        :title="course.title"
        :description="course.shortIntroduction"
        :category="course.categoryName"
        :busy="enrollingId === course.id"
        :cover-type="course.coverType"
        :cover-color="course.coverColor"
        :cover-image-url="coverImageUrls[course.id] ?? null"
        @enroll="onEnroll(course.id)"
      />
    </div>
    <p
      v-else
      class="bay-state-panel"
    >
      {{ $t('courses.available.empty') }}
    </p>
  </div>
</template>
