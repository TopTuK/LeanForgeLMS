<script setup>
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import CourseCard from '@/components/courses/CourseCard.vue';
import { fetchMyEnrollments } from '@/services/enrollmentService';

const { t } = useI18n();
const router = useRouter();

const enrollments = ref([]);
const loading = ref(false);
const errorMessage = ref('');

async function loadEnrollments() {
  loading.value = true;
  errorMessage.value = '';
  try {
    enrollments.value = await fetchMyEnrollments({ status: 'completed' });
  } catch {
    errorMessage.value = t('courses.finished.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(loadEnrollments);

function formatCompletedOn(value) {
  return value ? new Date(value).toLocaleDateString() : '';
}

function onReview(enrollmentId) {
  router.push({ name: 'CourseLearn', params: { enrollmentId } });
}
</script>

<template>
  <div>
    <div class="bay-section-heading mb-8">
      <span
        class="bay-section-index"
        aria-hidden="true"
      >03</span>
      <div>
        <h2 class="text-xl font-bold text-ink">
          {{ $t('courses.finished.title') }}
        </h2>
        <p class="mt-2 text-sm text-ink-muted leading-relaxed">
          {{ $t('courses.finished.subtitle') }}
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
      v-else-if="enrollments.length"
      class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5"
    >
      <CourseCard
        v-for="(item, idx) in enrollments"
        :key="item.id"
        status="finished"
        :index="idx"
        :title="item.courseTitle"
        :description="item.courseShortIntroduction"
        :category="item.categoryName"
        :completed-on="formatCompletedOn(item.completedAt)"
        @continue="onReview(item.id)"
      />
    </div>
    <p
      v-else
      class="bay-state-panel"
    >
      {{ $t('courses.finished.empty') }}
    </p>
  </div>
</template>
