<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/authStore';
import { fetchCourses } from '@/services/courseService';

const route = useRoute();
const { t } = useI18n();
const authStore = useAuthStore();

const isForgeRoute = computed(() =>
  route.name === 'CoursesCreate'
  || route.name === 'CourseEdit'
  || route.name === 'LessonEdit',
);

const tabs = computed(() => {
  const list = [
    { name: 'CoursesAvailable', label: t('courses.tabs.available') },
    { name: 'CoursesActive', label: t('courses.tabs.active') },
    { name: 'CoursesFinished', label: t('courses.tabs.finished') },
  ];

  if (authStore.canViewTeachingCourses) {
    list.push({ name: 'CoursesTeaching', label: t('courses.tabs.teaching') });
  }

  return list;
});

// Surfaced on every /courses/* tab (not just the create form) so navigating "back" from
// the editor by any route — in-app link or the browser's own back button — never strands
// the user on a page with no visible way to resume a draft they already started.
const latestDraft = ref(null);

async function loadLatestDraft() {
  if (!authStore.canCreateCourses) return;

  try {
    const result = await fetchCourses({ page: 1, pageSize: 50 });
    latestDraft.value = result.items.find((c) => !c.isPublished) ?? null;
  } catch {
    latestDraft.value = null;
  }
}

onMounted(loadLatestDraft);
</script>

<template>
  <section :class="isForgeRoute ? 'courses-forge-shell' : 'courses-page container mx-auto px-6 py-12'">
    <template v-if="!isForgeRoute">
      <div class="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-6 mb-8">
        <div>
          <p class="text-sm font-semibold tracking-wide text-accent-coral mb-2">
            {{ $t('courses.eyebrow') }}
          </p>
          <h1 class="text-3xl md:text-4xl font-extrabold text-ink tracking-tight">
            {{ $t('courses.title') }}
          </h1>
        </div>

        <router-link
          v-if="authStore.canCreateCourses"
          :to="{ name: 'CoursesCreate' }"
          class="btn-accent inline-flex items-center justify-center gap-2 rounded-pill px-6 py-3 text-sm font-semibold w-fit"
        >
          <svg
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            aria-hidden="true"
          >
            <path
              d="M12 5V19M5 12H19"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
            />
          </svg>
          {{ $t('courses.create_action') }}
        </router-link>
      </div>

      <router-link
        v-if="latestDraft"
        :to="{ name: 'CourseEdit', params: { id: latestDraft.id } }"
        class="flat-card rounded-card flex flex-wrap items-center justify-between gap-3 px-5 py-4 mb-8 text-sm font-medium hover:opacity-90 transition"
      >
        <span>{{ $t('courses.continue_draft', { title: latestDraft.title }) }}</span>
        <span class="text-accent-coral font-semibold">{{ $t('courses.create.edit_action') }} &rarr;</span>
      </router-link>

      <nav
        class="flex flex-wrap gap-2 mb-10"
        :aria-label="$t('courses.title')"
      >
        <router-link
          v-for="tab in tabs"
          :key="tab.name"
          :to="{ name: tab.name }"
          class="filter-chip rounded-pill px-4 py-2 text-sm font-medium"
          :class="{ 'is-active': route.name === tab.name }"
        >
          {{ tab.label }}
        </router-link>
      </nav>
    </template>

    <router-view />
  </section>
</template>

<style scoped>
.courses-forge-shell {
  min-height: calc(100vh - 4.5rem);
}
</style>
