<script setup>
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import CourseCard from '@/components/courses/CourseCard.vue';
import { fetchCourses } from '@/services/courseService';

const { t } = useI18n();
const router = useRouter();

const courses = ref([]);
const loading = ref(false);
const errorMessage = ref('');

async function loadCourses() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchCourses({ page: 1, pageSize: 50 });
    courses.value = result.items;
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
    <div class="max-w-2xl mb-8">
      <h2 class="text-xl font-bold text-ink">
        {{ $t('courses.teaching.title') }}
      </h2>
      <p class="mt-2 text-sm text-ink-muted leading-relaxed">
        {{ $t('courses.teaching.subtitle') }}
      </p>
    </div>

    <p
      v-if="errorMessage"
      class="text-sm text-accent-coral mb-4"
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
        v-for="course in courses"
        :key="course.id"
        status="teaching"
        :title="course.title"
        :description="course.shortIntroduction"
        :category="course.categoryName"
        @manage="onManage(course.id)"
      />
    </div>
    <p
      v-else
      class="text-sm text-ink-muted"
    >
      {{ $t('courses.teaching.empty') }}
    </p>
  </div>
</template>
