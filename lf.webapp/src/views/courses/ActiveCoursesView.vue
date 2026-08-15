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
    enrollments.value = await fetchMyEnrollments({ status: 'active' });
  } catch {
    errorMessage.value = t('courses.active.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(loadEnrollments);

function onContinue(enrollmentId) {
  router.push({ name: 'CourseLearn', params: { enrollmentId } });
}
</script>

<template>
  <div>
    <div class="max-w-2xl mb-8">
      <h2 class="text-xl font-bold text-ink">
        {{ $t('courses.active.title') }}
      </h2>
      <p class="mt-2 text-sm text-ink-muted leading-relaxed">
        {{ $t('courses.active.subtitle') }}
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
      v-else-if="enrollments.length"
      class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5"
    >
      <CourseCard
        v-for="item in enrollments"
        :key="item.id"
        status="active"
        :title="item.courseTitle"
        :description="item.courseShortIntroduction"
        :category="item.categoryName"
        :progress="item.progressPercent"
        @continue="onContinue(item.id)"
      />
    </div>
    <p
      v-else
      class="text-sm text-ink-muted"
    >
      {{ $t('courses.active.empty') }}
    </p>
  </div>
</template>
