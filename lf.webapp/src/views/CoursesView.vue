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

const activeTabIndex = computed(() => {
  const i = tabs.value.findIndex((tab) => tab.name === route.name);
  return String(i >= 0 ? i + 1 : 1).padStart(2, '0');
});

function tabIndex(i) {
  return String(i + 1).padStart(2, '0');
}

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
  <section :class="isForgeRoute ? 'courses-forge-shell' : 'courses-bay'">
    <template v-if="!isForgeRoute">
      <div
        class="courses-bay__square courses-bay__square--one"
        aria-hidden="true"
      />
      <div
        class="courses-bay__square courses-bay__square--two"
        aria-hidden="true"
      />
      <div
        class="courses-bay__curve"
        aria-hidden="true"
      />

      <div class="container mx-auto px-6 py-12 md:py-16 relative z-10">
        <div class="courses-bay__heading">
          <div>
            <p class="courses-bay__eyebrow">
              {{ $t('courses.eyebrow') }}
            </p>
            <h1 class="courses-bay__title">
              {{ $t('courses.title') }}
            </h1>
          </div>

          <div class="courses-bay__heading-side">
            <span
              class="courses-bay__meta"
              aria-hidden="true"
            >CATALOG / {{ activeTabIndex }}</span>

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
        </div>

        <router-link
          v-if="latestDraft"
          :to="{ name: 'CourseEdit', params: { id: latestDraft.id } }"
          class="courses-bay__draft"
        >
          <span
            class="courses-bay__draft-bar"
            aria-hidden="true"
          />
          <span class="courses-bay__draft-text">{{ $t('courses.continue_draft', { title: latestDraft.title }) }}</span>
          <span class="courses-bay__draft-action">{{ $t('courses.create.edit_action') }} &rarr;</span>
        </router-link>

        <nav
          class="courses-bay__tabs"
          :aria-label="$t('courses.title')"
        >
          <router-link
            v-for="(tab, i) in tabs"
            :key="tab.name"
            :to="{ name: tab.name }"
            class="courses-bay__tab"
            :class="{ 'is-active': route.name === tab.name }"
          >
            <span
              class="courses-bay__tab-index"
              aria-hidden="true"
            >{{ tabIndex(i) }}</span>
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
.courses-forge-shell {
  min-height: calc(100vh - 4.5rem);
}

.courses-bay {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  min-height: calc(100vh - 4.5rem);
  background-color: var(--color-surface-950);
  background-image:
    linear-gradient(var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--industrial-grid) 1px, transparent 1px);
  background-size: 40px 40px;
}

.courses-bay__square,
.courses-bay__curve {
  position: absolute;
  pointer-events: none;
  z-index: 0;
}

.courses-bay__square {
  border: 1px solid var(--industrial-line);
  transform: rotate(-10deg);
}

.courses-bay__square::after {
  content: "";
  position: absolute;
  width: 40%;
  height: 40%;
  left: -16%;
  top: -16%;
  background: var(--industrial-accent-wash);
  border: 1px solid var(--industrial-line);
}

.courses-bay__square--one {
  width: 8rem;
  height: 8rem;
  top: 5rem;
  right: 6%;
}

.courses-bay__square--two {
  width: 3.75rem;
  height: 3.75rem;
  left: 4%;
  bottom: 14%;
  border-color: var(--industrial-line);
  transform: rotate(16deg);
  opacity: 0.8;
}

.courses-bay__curve {
  width: 26rem;
  height: 26rem;
  right: -14rem;
  bottom: -16rem;
  border: 1px solid var(--industrial-line);
  border-radius: 50%;
  box-shadow: 0 0 0 36px var(--industrial-grid);
}

.courses-bay__heading {
  position: relative;
  z-index: 2;
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1.5rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid var(--industrial-line);
}

.courses-bay__eyebrow {
  margin: 0 0 0.5rem;
  color: var(--color-accent-coral);
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.1em;
}

.courses-bay__title {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.9rem, 4vw, 2.5rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.05;
}

.courses-bay__heading-side {
  display: flex;
  align-items: center;
  gap: 1.25rem;
}

.courses-bay__meta {
  display: none;
  color: var(--color-ink-faint);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  white-space: nowrap;
}

.courses-bay__draft {
  position: relative;
  z-index: 2;
  display: flex;
  align-items: center;
  gap: 0.9rem;
  margin-bottom: 2.5rem;
  padding: 1rem 1.25rem 1rem 1.5rem;
  background: var(--industrial-panel);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  backdrop-filter: blur(10px);
  text-decoration: none;
  transition: border-color 0.15s ease, background-color 0.15s ease;
}

.courses-bay__draft:hover {
  border-color: var(--industrial-line-strong);
}

.courses-bay__draft-bar {
  flex-shrink: 0;
  width: 0.2rem;
  align-self: stretch;
  border-radius: 999px;
  background: var(--color-accent-coral);
}

.courses-bay__draft-text {
  flex: 1;
  color: var(--color-ink);
  font-size: 0.9rem;
  font-weight: 500;
}

.courses-bay__draft-action {
  flex-shrink: 0;
  color: var(--color-accent-coral);
  font-size: 0.85rem;
  font-weight: 700;
  white-space: nowrap;
}

.courses-bay__tabs {
  position: relative;
  z-index: 2;
  display: flex;
  flex-wrap: wrap;
  gap: 0.6rem;
  margin-bottom: 2.5rem;
}

.courses-bay__tab {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.55rem 1.1rem 0.55rem 0.7rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-pill);
  color: var(--color-ink-muted);
  font-size: 0.85rem;
  font-weight: 600;
  text-decoration: none;
  transition: color 0.15s ease, border-color 0.15s ease, background-color 0.15s ease;
}

.courses-bay__tab-index {
  color: var(--color-ink-faint);
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.06em;
}

.courses-bay__tab:hover {
  color: var(--color-ink);
  border-color: var(--color-ink-faint);
}

.courses-bay__tab.is-active {
  background-color: var(--color-ink);
  border-color: var(--color-ink);
  color: #ffffff;
}

.courses-bay__tab.is-active .courses-bay__tab-index {
  color: var(--color-accent-coral);
}

@media (min-width: 768px) {
  .courses-bay__meta {
    display: inline-block;
  }
}
</style>
