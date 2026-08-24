<script setup>
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import CourseCard from '@/components/home/CourseCard.vue';
import AuthorCard from '@/components/home/AuthorCard.vue';
import FaqItem from '@/components/home/FaqItem.vue';
import GeometricBackdrop from '@/components/layout/GeometricBackdrop.vue';

const { tm, t } = useI18n();

const COURSES = [
  { key: 'llm_agentic', icon: 'llm', extraFilters: ['management'] },
  { key: 'kanban', icon: 'kanban', extraFilters: ['management'] },
];

const FILTERS = ['all', 'ai', 'flow', 'management'];
const FAQ_KEYS = ['1', '2', '3', '4', '5'];

const activeFilter = ref('all');

const facts = computed(() => {
  const items = tm('home.facts.items');
  return Array.isArray(items) ? items : [];
});

const authorHighlights = computed(() => {
  const items = tm('home.author.highlights');
  return Array.isArray(items) ? items : [];
});

const visibleCourses = computed(() => {
  if (activeFilter.value === 'all') return COURSES;
  return COURSES.filter((course) => {
    const primary = t(`home.courses.items.${course.key}.filter`);
    return primary === activeFilter.value || course.extraFilters.includes(activeFilter.value);
  });
});
</script>

<template>
  <div class="landing">
    <section
      id="hero"
      class="landing-hero"
    >
      <GeometricBackdrop />
      <div class="container relative z-10 mx-auto grid items-center gap-12 px-6 py-16 md:py-24 lg:grid-cols-[minmax(0,1.15fr)_minmax(18rem,0.85fr)] lg:gap-16">
        <div class="max-w-3xl">
          <p class="mb-4 text-sm font-semibold tracking-[0.14em] text-accent-coral uppercase">
            {{ $t('home.hero.brand_line') }}
          </p>
          <h1 class="font-display text-4xl font-semibold tracking-tight text-ink md:text-6xl lg:text-7xl">
            {{ $t('home.hero.headline') }}
          </h1>
          <p class="mt-6 max-w-xl text-lg leading-relaxed text-ink-muted md:text-xl">
            {{ $t('home.hero.subheadline') }}
          </p>
          <div class="mt-10 flex flex-col gap-5 sm:flex-row sm:items-center">
            <a
              href="#courses"
              class="btn-accent inline-flex w-fit items-center justify-center rounded-pill px-7 py-3.5 text-sm font-semibold"
            >
              {{ $t('home.hero.cta_primary') }}
            </a>
            <p class="text-sm text-ink-muted">
              {{ $t('home.hero.trust_line') }}
            </p>
          </div>
        </div>

        <div
          class="landing-panel hidden lg:block"
          aria-hidden="true"
        >
          <span class="landing-panel__square" />
          <span class="landing-panel__circle" />
          <span class="landing-panel__arc" />
        </div>
      </div>
    </section>

    <section class="border-y border-border-subtle bg-surface-900">
      <div class="container mx-auto grid gap-8 px-6 py-10 sm:grid-cols-3">
        <div
          v-for="(fact, index) in facts"
          :key="index"
        >
          <p class="font-display text-2xl font-semibold tracking-tight text-ink">
            {{ fact.value }}
          </p>
          <p class="mt-1 text-sm leading-relaxed text-ink-muted">
            {{ fact.label }}
          </p>
        </div>
      </div>
    </section>

    <section
      id="courses"
      class="relative overflow-hidden py-20 md:py-24"
    >
      <GeometricBackdrop dense />
      <div class="container relative z-10 mx-auto px-6">
        <div class="mb-8 max-w-2xl">
          <h2 class="font-display text-3xl font-semibold tracking-tight text-ink md:text-4xl">
            {{ $t('home.courses.title') }}
          </h2>
          <p class="mt-3 leading-relaxed text-ink-muted">
            {{ $t('home.courses.subtitle') }}
          </p>
        </div>

        <div class="mb-8 flex flex-wrap gap-2">
          <button
            v-for="filter in FILTERS"
            :key="filter"
            type="button"
            class="filter-chip rounded-pill px-4 py-1.5 text-sm font-medium"
            :class="{ 'is-active': activeFilter === filter }"
            @click="activeFilter = filter"
          >
            {{ $t(`home.courses.filters.${filter}`) }}
          </button>
        </div>

        <div class="grid max-w-4xl grid-cols-1 gap-5 md:grid-cols-2">
          <CourseCard
            v-for="course in visibleCourses"
            :key="course.key"
            :icon="course.icon"
            :title="$t(`home.courses.items.${course.key}.title`)"
            :description="$t(`home.courses.items.${course.key}.description`)"
            :duration="$t(`home.courses.items.${course.key}.duration`)"
            :category="$t(`home.courses.items.${course.key}.category`)"
            :cta="$t('home.courses.cta')"
            :to="{ name: 'Login' }"
          />
        </div>
      </div>
    </section>

    <section
      id="author"
      class="border-y border-border-subtle bg-surface-900 py-20 md:py-24"
    >
      <div class="container mx-auto px-6">
        <div class="mb-10 max-w-2xl">
          <h2 class="font-display text-3xl font-semibold tracking-tight text-ink md:text-4xl">
            {{ $t('home.author.title') }}
          </h2>
          <p class="mt-3 leading-relaxed text-ink-muted">
            {{ $t('home.author.subtitle') }}
          </p>
        </div>
        <AuthorCard
          :name="$t('home.author.name')"
          :role="$t('home.author.role')"
          :bio="$t('home.author.bio')"
          :highlights="authorHighlights"
          :cta="$t('home.author.cta')"
          :href="t('home.author.url')"
        />
      </div>
    </section>

    <section
      id="faq"
      class="py-20 md:py-24"
    >
      <div class="container mx-auto max-w-3xl px-6">
        <h2 class="mb-8 font-display text-3xl font-semibold tracking-tight text-ink md:text-4xl">
          {{ $t('home.faq.title') }}
        </h2>
        <FaqItem
          v-for="key in FAQ_KEYS"
          :key="key"
          :question="$t(`home.faq.items.${key}.question`)"
          :answer="$t(`home.faq.items.${key}.answer`)"
        />
      </div>
    </section>

    <section
      id="contacts"
      class="relative overflow-hidden border-t border-border-subtle bg-surface-900 py-20 md:py-24"
    >
      <GeometricBackdrop dense />
      <div class="container relative z-10 mx-auto px-6">
        <div class="max-w-2xl">
          <h2 class="font-display text-3xl font-semibold tracking-tight text-ink md:text-4xl">
            {{ $t('home.contacts.title') }}
          </h2>
          <p class="mt-3 leading-relaxed text-ink-muted">
            {{ $t('home.contacts.subtitle') }}
          </p>
          <a
            :href="`mailto:${$t('home.contacts.support')}`"
            class="mt-6 inline-block border-b border-accent-coral/40 font-semibold text-accent-coral hover:border-accent-coral"
          >
            {{ $t('home.contacts.support') }}
          </a>

          <div class="flat-card mt-10 max-w-md rounded-card p-6 md:p-8">
            <h3 class="text-xl font-semibold text-ink">
              {{ $t('home.contacts.notify_title') }}
            </h3>
            <p class="mt-2 text-sm text-ink-muted">
              {{ $t('home.contacts.notify_subtitle') }}
            </p>
            <a
              :href="`mailto:${$t('home.contacts.support')}?subject=${$t('home.contacts.notify_subject')}`"
              class="btn-accent mt-6 inline-flex w-fit items-center justify-center rounded-pill px-6 py-3 text-sm font-semibold"
            >
              {{ $t('home.contacts.notify_cta') }}
            </a>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.landing-hero {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  min-height: calc(100vh - 4.5rem);
  display: flex;
  align-items: center;
  background:
    linear-gradient(
      115deg,
      var(--industrial-hero-start) 0%,
      var(--industrial-hero-middle) 58%,
      var(--industrial-hero-end) 100%
    );
}

.landing-panel {
  position: relative;
  aspect-ratio: 1;
  max-width: 26rem;
  margin-left: auto;
  border: 1px solid var(--industrial-line);
  background: var(--industrial-panel);
}

.landing-panel__square {
  position: absolute;
  width: 8rem;
  height: 8rem;
  top: 18%;
  right: 18%;
  border: 1px solid color-mix(in srgb, var(--color-accent-coral) 55%, transparent);
  background: var(--industrial-accent-wash);
  transform: rotate(16deg);
}

.landing-panel__circle {
  position: absolute;
  width: 6rem;
  height: 6rem;
  left: 16%;
  bottom: 18%;
  border: 1px solid var(--industrial-line-strong);
  border-radius: 50%;
}

.landing-panel__arc {
  position: absolute;
  width: 12rem;
  height: 12rem;
  left: 30%;
  top: 28%;
  border-radius: 50%;
  border: 1px solid transparent;
  border-top-color: var(--color-accent-coral);
  border-right-color: var(--color-accent-coral);
}
</style>
