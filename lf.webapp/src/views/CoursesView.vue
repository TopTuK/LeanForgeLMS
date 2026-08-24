<script setup>
import { computed, onMounted, provide, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Plus } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/authStore';
import { fetchCourses } from '@/services/courseService';
import GeometricBackdrop from '@/components/layout/GeometricBackdrop.vue';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

const route = useRoute();
const { t } = useI18n();
const authStore = useAuthStore();

const isStudioRoute = computed(() =>
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

const searchQuery = ref('');
provide('courseSearch', searchQuery);

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
  <section :class="isStudioRoute ? 'courses-studio-shell' : 'courses-catalog'">
    <template v-if="!isStudioRoute">
      <GeometricBackdrop dense />

      <div class="container relative z-10 mx-auto px-6 py-12 md:py-16">
        <div class="courses-catalog__heading">
          <div>
            <p class="courses-catalog__eyebrow">
              {{ $t('courses.eyebrow') }}
            </p>
            <h1>{{ $t('courses.title') }}</h1>
          </div>

          <Button
            v-if="authStore.canCreateCourses"
            as-child
            size="pill"
          >
            <router-link :to="{ name: 'CoursesCreate' }">
              <Plus class="size-4" />
              {{ $t('courses.create_action') }}
            </router-link>
          </Button>
        </div>

        <router-link
          v-if="latestDraft"
          :to="{ name: 'CourseEdit', params: { id: latestDraft.id } }"
          class="courses-catalog__draft"
        >
          <span>{{ $t('courses.continue_draft', { title: latestDraft.title }) }}</span>
          <span class="text-accent-coral">{{ $t('courses.create.edit_action') }} →</span>
        </router-link>

        <div class="mb-6 max-w-sm">
          <Input
            v-model="searchQuery"
            :placeholder="$t('courses.search_placeholder')"
          />
        </div>

        <nav
          class="courses-catalog__tabs"
          :aria-label="$t('courses.title')"
        >
          <router-link
            v-for="tab in tabs"
            :key="tab.name"
            :to="{ name: tab.name }"
            class="courses-catalog__tab"
            :class="{ 'is-active': route.name === tab.name }"
          >
            {{ tab.label }}
          </router-link>
        </nav>

        <router-view />
      </div>
    </template>
    <template v-else>
      <router-view />
    </template>
  </section>
</template>

<style scoped>
.courses-studio-shell {
  min-height: calc(100vh - 4.5rem);
}

.courses-catalog {
  position: relative;
  isolation: isolate;
  min-height: calc(100vh - 4.5rem);
  overflow: hidden;
  background: var(--color-surface-950);
}

.courses-catalog__heading {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1.25rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid var(--color-border-subtle);
}

.courses-catalog__eyebrow {
  margin: 0 0 0.4rem;
  color: var(--color-accent-coral);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.courses-catalog__heading h1 {
  margin: 0;
  color: var(--color-ink);
  font-family: var(--font-display);
  font-size: clamp(1.8rem, 4vw, 2.4rem);
  font-weight: 600;
  letter-spacing: -0.03em;
}

.courses-catalog__draft {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
  padding: 1rem 1.15rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-card);
  text-decoration: none;
  color: var(--color-ink);
  font-size: 0.9rem;
}

.courses-catalog__tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 2rem;
}

.courses-catalog__tab {
  padding: 0.5rem 1rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 999px;
  color: var(--color-ink-muted);
  font-size: 0.85rem;
  font-weight: 600;
  text-decoration: none;
}

.courses-catalog__tab:hover {
  color: var(--color-ink);
}

.courses-catalog__tab.is-active {
  background: var(--color-ink);
  border-color: var(--color-ink);
  color: var(--color-surface-950);
}
</style>
