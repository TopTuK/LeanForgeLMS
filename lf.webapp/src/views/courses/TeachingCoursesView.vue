<script setup>
import { computed, inject, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import CourseCard from '@/components/courses/CourseCard.vue';
import { fetchCourseCoverImageObjectUrl } from '@/services/courseService';
import { fetchTeachingCourses } from '@/services/teachingService';
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
    // Includes courses the user was assigned to teach, not only the ones they created.
    courses.value = await fetchTeachingCourses();
    await loadCoverImages(courses.value, (c) => c.id);
  } catch {
    errorMessage.value = t('courses.teaching.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(loadCourses);

// Assigned instructors can't open the content editor, so "manage" lands them on the course's groups.
function onManage(course) {
  router.push(course.canEditContent
    ? { name: 'CourseEdit', params: { id: course.id } }
    : { name: 'TeachingGroups', params: { courseId: course.id } });
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
      <div
        v-for="(course, idx) in visibleCourses"
        :key="course.id"
        class="teaching-course"
      >
        <CourseCard
          status="teaching"
          :index="idx"
          :title="course.title"
          :description="course.shortIntroduction"
          :category="course.categoryName"
          :is-published="course.isPublished ?? null"
          :cover-type="course.coverType"
          :cover-color="course.coverColor"
          :cover-image-url="coverImageUrls[course.id] ?? null"
          @manage="onManage(course)"
        />
        <nav class="teaching-course__links">
          <router-link :to="{ name: 'TeachingGroups', params: { courseId: course.id } }">
            {{ $t('teaching.groups_link') }}
          </router-link>
          <router-link :to="{ name: 'TeachingLectures', params: { courseId: course.id } }">
            {{ $t('teaching.lectures_link') }}
          </router-link>
        </nav>
      </div>
    </div>
    <p
      v-else
      class="catalog-state-panel"
    >
      {{ $t('courses.teaching.empty') }}
    </p>
  </div>
</template>

<style scoped>
.teaching-course {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.teaching-course__links {
  display: flex;
  gap: 1rem;
  padding-inline: 0.25rem;
  font-size: 0.85rem;
  font-weight: 600;
}

.teaching-course__links a {
  color: var(--color-accent-coral);
}

.teaching-course__links a:hover {
  text-decoration: underline;
}
</style>
