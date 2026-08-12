<script setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/authStore';

const route = useRoute();
const { t } = useI18n();
const authStore = useAuthStore();

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
</script>

<template>
  <section class="courses-page container mx-auto px-6 py-12">
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

    <router-view />
  </section>
</template>
