<script setup>
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import CourseCard from '@/components/home/CourseCard.vue';
import TestimonialCard from '@/components/home/TestimonialCard.vue';
import AuthorCard from '@/components/home/AuthorCard.vue';
import ExpertCard from '@/components/home/ExpertCard.vue';
import FaqItem from '@/components/home/FaqItem.vue';

const { tm, t } = useI18n();

const COURSE_KEYS = [
    'backend',
    'frontend',
    'devops',
    'databases',
    'architecture',
    'communications',
    'simple_code',
    'career',
];

const COURSE_ICONS = {
    backend: 'backend',
    frontend: 'frontend',
    devops: 'devops',
    databases: 'databases',
    architecture: 'architecture',
    communications: 'architecture',
    simple_code: 'architecture',
    career: 'career',
};

const COURSE_FILTERS = ['all', 'backend', 'frontend', 'devops', 'architecture', 'career'];
const REVIEW_FILTERS = ['all', 'difference', 'audience', 'after'];
const TESTIMONIAL_KEYS = ['1', '2', '3', '4', '5', '6'];
const EXPERT_KEYS = ['1', '2', '3'];
const FAQ_KEYS = ['1', '2', '3', '4', '5'];

const authorHighlights = computed(() => {
    const items = tm('home.author.highlights');
    return Array.isArray(items) ? items : [];
});

const INITIAL_COURSE_COUNT = 4;
const INITIAL_REVIEW_COUNT = 3;

const courseFilter = ref('all');
const reviewFilter = ref('all');
const coursesExpanded = ref(false);
const reviewsExpanded = ref(false);
const newsletterEmail = ref('');

const stats = [
    { value: 'home.stats.completion_value', label: 'home.stats.completion_label' },
    { value: 'home.stats.practice_value', label: 'home.stats.practice_label' },
    { value: 'home.stats.courses_value', label: 'home.stats.courses_label' },
    { value: 'home.stats.rating_value', label: 'home.stats.rating_label' },
];

const filteredCourses = computed(() => {
    const items = COURSE_KEYS.map((key) => {
        const meta = tm(`home.courses.items.${key}`);
        return {
            key,
            icon: COURSE_ICONS[key],
            filter: typeof meta === 'object' && meta?.filter ? meta.filter : key,
        };
    });

    if (courseFilter.value === 'all') return items;
    return items.filter((item) => item.filter === courseFilter.value);
});

const visibleCourses = computed(() => {
    if (coursesExpanded.value || filteredCourses.value.length <= INITIAL_COURSE_COUNT) {
        return filteredCourses.value;
    }
    return filteredCourses.value.slice(0, INITIAL_COURSE_COUNT);
});

const hiddenCourseCount = computed(() =>
    Math.max(0, filteredCourses.value.length - INITIAL_COURSE_COUNT),
);

const filteredTestimonials = computed(() => {
    const items = TESTIMONIAL_KEYS.map((key) => {
        const meta = tm(`home.testimonials.items.${key}`);
        return {
            key,
            filter: typeof meta === 'object' && meta?.filter ? meta.filter : 'all',
        };
    });

    if (reviewFilter.value === 'all') return items;
    return items.filter((item) => item.filter === reviewFilter.value);
});

const visibleTestimonials = computed(() => {
    if (reviewsExpanded.value || filteredTestimonials.value.length <= INITIAL_REVIEW_COUNT) {
        return filteredTestimonials.value;
    }
    return filteredTestimonials.value.slice(0, INITIAL_REVIEW_COUNT);
});

const hiddenReviewCount = computed(() =>
    Math.max(0, filteredTestimonials.value.length - INITIAL_REVIEW_COUNT),
);

function onCourseFilter(filter) {
    courseFilter.value = filter;
    coursesExpanded.value = false;
}

function onReviewFilter(filter) {
    reviewFilter.value = filter;
    reviewsExpanded.value = false;
}

function onNewsletterSubmit(event) {
    event.preventDefault();
    newsletterEmail.value = '';
}
</script>

<template>
  <div class="industrial-page">
    <section
      id="hero"
      class="industrial-hero min-h-[calc(100vh-4.5rem)] flex items-center overflow-hidden"
    >
      <div
        class="industrial-curve industrial-curve--hero"
        aria-hidden="true"
      />
      <div
        class="industrial-square industrial-square--one"
        aria-hidden="true"
      />
      <div
        class="industrial-square industrial-square--two"
        aria-hidden="true"
      />

      <div class="container mx-auto px-6 py-16 md:py-24 relative z-10">
        <div class="grid lg:grid-cols-[minmax(0,1.25fr)_minmax(20rem,0.75fr)] gap-12 lg:gap-20 items-center">
          <div class="max-w-3xl">
            <p class="text-sm md:text-base font-semibold tracking-wide text-accent-coral mb-4">
              {{ $t('home.hero.brand_line') }}
            </p>
            <h1 class="text-4xl md:text-6xl lg:text-7xl font-extrabold text-ink leading-[1.05] tracking-tight">
              {{ $t('home.hero.headline') }}
            </h1>
            <p class="mt-6 text-lg md:text-xl text-ink-muted max-w-xl leading-relaxed">
              {{ $t('home.hero.subheadline') }}
            </p>
            <div class="mt-10 flex flex-col sm:flex-row sm:items-center gap-5">
              <a
                href="#courses"
                class="btn-accent inline-flex items-center justify-center rounded-pill px-7 py-3.5 text-sm font-semibold w-fit"
              >
                {{ $t('home.hero.cta_primary') }}
              </a>
              <p class="text-sm text-ink-muted">
                {{ $t('home.hero.trust_line') }}
              </p>
            </div>
          </div>

          <div
            class="industrial-blueprint hidden lg:block"
            aria-hidden="true"
          >
            <div class="industrial-blueprint__index">
              LF—01
            </div>
            <div class="industrial-blueprint__square industrial-blueprint__square--large" />
            <div class="industrial-blueprint__square industrial-blueprint__square--small" />
            <svg
              viewBox="0 0 440 440"
              fill="none"
            >
              <path d="M-20 335C82 335 80 176 190 176C300 176 304 44 460 44" />
              <path d="M-30 382C118 382 107 225 222 225C337 225 337 92 475 92" />
              <circle
                cx="190"
                cy="176"
                r="6"
              />
              <circle
                cx="337"
                cy="92"
                r="6"
              />
            </svg>
            <span class="industrial-blueprint__mark industrial-blueprint__mark--top">A.01</span>
            <span class="industrial-blueprint__mark industrial-blueprint__mark--bottom">SYSTEM / CRAFT</span>
          </div>
        </div>
      </div>
    </section>

    <section class="industrial-band border-y border-border-subtle">
      <div class="container mx-auto px-6 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-10 py-14">
        <div
          v-for="stat in stats"
          :key="stat.value"
          class="flex flex-col gap-2"
        >
          <p class="text-3xl md:text-4xl font-extrabold text-ink tracking-tight">
            {{ $t(stat.value) }}
          </p>
          <p class="text-sm text-ink-muted leading-snug max-w-[16rem]">
            {{ $t(stat.label) }}
          </p>
        </div>
      </div>
    </section>

    <section
      id="courses"
      class="industrial-section py-20 md:py-24"
    >
      <div
        class="industrial-curve industrial-curve--section"
        aria-hidden="true"
      />
      <div class="container mx-auto px-6">
        <h2 class="text-3xl md:text-4xl font-extrabold text-ink tracking-tight mb-8">
          {{ $t('home.courses.title') }}
        </h2>

        <div class="flex flex-wrap gap-2 mb-10">
          <button
            v-for="filter in COURSE_FILTERS"
            :key="filter"
            type="button"
            class="filter-chip rounded-pill px-4 py-2 text-sm font-medium"
            :class="{ 'is-active': courseFilter === filter }"
            @click="onCourseFilter(filter)"
          >
            {{ $t(`home.courses.filters.${filter}`) }}
          </button>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          <CourseCard
            v-for="course in visibleCourses"
            :key="course.key"
            :icon="course.icon"
            :title="$t(`home.courses.items.${course.key}.title`)"
            :description="$t(`home.courses.items.${course.key}.description`)"
            :duration="$t(`home.courses.items.${course.key}.duration`)"
            :category="$t(`home.courses.items.${course.key}.category`)"
          />
        </div>

        <div
          v-if="hiddenCourseCount > 0"
          class="mt-10"
        >
          <button
            type="button"
            class="text-sm font-semibold text-accent-coral hover:text-accent-coral-dark transition border-b border-accent-coral/40 hover:border-accent-coral-dark"
            @click="coursesExpanded = !coursesExpanded"
          >
            {{
              coursesExpanded
                ? $t('home.courses.show_less')
                : $t('home.courses.show_more', { n: hiddenCourseCount })
            }}
          </button>
        </div>
      </div>
    </section>

    <section
      id="testimonials"
      class="industrial-band industrial-section py-20 md:py-24 border-y border-border-subtle"
    >
      <div
        class="industrial-square industrial-square--reviews"
        aria-hidden="true"
      />
      <div class="container mx-auto px-6">
        <h2 class="text-3xl md:text-4xl font-extrabold text-ink tracking-tight mb-8">
          {{ $t('home.testimonials.title') }}
        </h2>

        <div class="flex flex-wrap gap-2 mb-10">
          <button
            v-for="filter in REVIEW_FILTERS"
            :key="filter"
            type="button"
            class="filter-chip rounded-pill px-4 py-2 text-sm font-medium"
            :class="{ 'is-active': reviewFilter === filter }"
            @click="onReviewFilter(filter)"
          >
            {{ $t(`home.testimonials.filters.${filter}`) }}
          </button>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-5">
          <TestimonialCard
            v-for="item in visibleTestimonials"
            :key="item.key"
            :quote="$t(`home.testimonials.items.${item.key}.quote`)"
            :name="$t(`home.testimonials.items.${item.key}.name`)"
            :role="$t(`home.testimonials.items.${item.key}.role`)"
            :tag="$t(`home.testimonials.items.${item.key}.tag`)"
          />
        </div>

        <div
          v-if="hiddenReviewCount > 0"
          class="mt-10"
        >
          <button
            type="button"
            class="text-sm font-semibold text-accent-coral hover:text-accent-coral-dark transition border-b border-accent-coral/40 hover:border-accent-coral-dark"
            @click="reviewsExpanded = !reviewsExpanded"
          >
            {{
              reviewsExpanded
                ? $t('home.testimonials.show_less')
                : $t('home.testimonials.show_more')
            }}
          </button>
        </div>
      </div>
    </section>

    <section
      id="author"
      class="industrial-section py-20 md:py-24"
    >
      <div
        class="industrial-square industrial-square--author"
        aria-hidden="true"
      />
      <div class="container mx-auto px-6">
        <div class="max-w-2xl mb-10">
          <h2 class="text-3xl md:text-4xl font-extrabold text-ink tracking-tight">
            {{ $t('home.author.title') }}
          </h2>
          <p class="mt-3 text-ink-muted leading-relaxed">
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
      id="experts"
      class="industrial-section py-20 md:py-24"
    >
      <div class="container mx-auto px-6">
        <div class="max-w-2xl mb-10">
          <h2 class="text-3xl md:text-4xl font-extrabold text-ink tracking-tight">
            {{ $t('home.experts.title') }}
          </h2>
          <p class="mt-3 text-ink-muted leading-relaxed">
            {{ $t('home.experts.subtitle') }}
          </p>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-x-12 max-w-4xl">
          <ExpertCard
            v-for="key in EXPERT_KEYS"
            :key="key"
            :name="$t(`home.experts.items.${key}.name`)"
            :role="$t(`home.experts.items.${key}.role`)"
          />
        </div>
      </div>
    </section>

    <section
      id="faq"
      class="industrial-band industrial-section py-20 md:py-24 border-y border-border-subtle"
    >
      <div
        class="industrial-curve industrial-curve--faq"
        aria-hidden="true"
      />
      <div class="container mx-auto px-6 max-w-3xl">
        <h2 class="text-3xl md:text-4xl font-extrabold text-ink tracking-tight mb-8">
          {{ $t('home.faq.title') }}
        </h2>
        <div>
          <FaqItem
            v-for="key in FAQ_KEYS"
            :key="key"
            :question="$t(`home.faq.items.${key}.question`)"
            :answer="$t(`home.faq.items.${key}.answer`)"
          />
        </div>
      </div>
    </section>

    <section
      id="contacts"
      class="industrial-section py-20 md:py-24 overflow-hidden"
    >
      <div
        class="industrial-square industrial-square--contact"
        aria-hidden="true"
      />
      <div class="container mx-auto px-6">
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-12 lg:gap-20 items-start">
          <div>
            <h2 class="text-3xl md:text-4xl font-extrabold text-ink tracking-tight">
              {{ $t('home.contacts.title') }}
            </h2>
            <p class="mt-3 text-ink-muted leading-relaxed max-w-md">
              {{ $t('home.contacts.subtitle') }}
            </p>
            <a
              :href="`mailto:${$t('home.contacts.support')}`"
              class="inline-block mt-6 text-accent-coral font-semibold border-b border-accent-coral/40 hover:border-accent-coral transition"
            >
              {{ $t('home.contacts.support') }}
            </a>
          </div>

          <div class="flat-card rounded-card p-6 md:p-8">
            <h3 class="text-xl font-bold text-ink">
              {{ $t('home.contacts.newsletter_title') }}
            </h3>
            <p class="mt-2 text-sm text-ink-muted">
              {{ $t('home.contacts.newsletter_subtitle') }}
            </p>
            <form
              class="mt-6 flex flex-col gap-4"
              @submit="onNewsletterSubmit"
            >
              <label class="flex flex-col gap-1.5">
                <span class="text-xs font-medium text-ink-muted">{{ $t('home.contacts.email_label') }}</span>
                <input
                  v-model="newsletterEmail"
                  type="email"
                  required
                  :placeholder="$t('home.contacts.email_placeholder')"
                  class="rounded-md border border-border-subtle bg-surface-950 px-4 py-3 text-sm text-ink placeholder:text-ink-faint focus:outline-none focus:border-ink"
                >
              </label>
              <button
                type="submit"
                class="btn-accent rounded-pill px-6 py-3 text-sm font-semibold w-fit"
              >
                {{ $t('home.contacts.subscribe') }}
              </button>
              <p class="text-xs text-ink-faint leading-relaxed">
                {{ $t('home.contacts.consent') }}
              </p>
            </form>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.industrial-page {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  background-color: var(--color-surface-950);
  background-image:
    linear-gradient(var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--industrial-grid) 1px, transparent 1px);
  background-size: 40px 40px;
}

.industrial-hero,
.industrial-section {
  position: relative;
}

.industrial-hero {
  background:
    linear-gradient(
      115deg,
      var(--industrial-hero-start) 0%,
      var(--industrial-hero-middle) 58%,
      var(--industrial-hero-end) 100%
    );
}

.industrial-band {
  position: relative;
  background-color: var(--industrial-band);
  background-image:
    linear-gradient(135deg, var(--industrial-grid) 25%, transparent 25%),
    linear-gradient(315deg, var(--industrial-grid) 25%, transparent 25%);
  background-size: 28px 28px;
}

.industrial-section > .container,
.industrial-band > .container {
  position: relative;
  z-index: 2;
}

.industrial-square {
  position: absolute;
  border: 1px solid rgba(236, 104, 60, 0.3);
  pointer-events: none;
  transform: rotate(12deg);
}

.industrial-square::after {
  content: "";
  position: absolute;
  width: 42%;
  height: 42%;
  right: -18%;
  bottom: -18%;
  background: var(--industrial-accent-wash);
  border: 1px solid var(--industrial-line);
}

.industrial-square--one {
  width: 9rem;
  height: 9rem;
  top: 8%;
  right: 5%;
}

.industrial-square--two {
  width: 3.5rem;
  height: 3.5rem;
  left: 3%;
  bottom: 10%;
  border-color: var(--industrial-line);
  transform: rotate(-8deg);
}

.industrial-square--reviews {
  width: 8rem;
  height: 8rem;
  right: -2rem;
  top: 3rem;
}

.industrial-square--author {
  width: 7rem;
  height: 7rem;
  right: 4%;
  top: 2.5rem;
  transform: rotate(-14deg);
}

.industrial-square--contact {
  width: 12rem;
  height: 12rem;
  left: -6rem;
  bottom: -4rem;
  transform: rotate(24deg);
}

.industrial-curve {
  position: absolute;
  pointer-events: none;
  border: 1px solid var(--industrial-line);
  border-radius: 50%;
}

.industrial-curve--hero {
  width: 44rem;
  height: 44rem;
  right: -20rem;
  bottom: -28rem;
  box-shadow:
    0 0 0 42px rgba(236, 104, 60, 0.035),
    0 0 0 84px var(--industrial-grid);
}

.industrial-curve--section {
  width: 30rem;
  height: 30rem;
  left: -25rem;
  top: 4rem;
  border-color: rgba(236, 104, 60, 0.22);
  box-shadow: 0 0 0 28px rgba(236, 104, 60, 0.025);
}

.industrial-curve--faq {
  width: 24rem;
  height: 24rem;
  right: -18rem;
  top: 2rem;
  border-color: rgba(236, 104, 60, 0.2);
  box-shadow: 0 0 0 24px var(--industrial-grid);
}

.industrial-blueprint {
  position: relative;
  aspect-ratio: 1;
  max-width: 27.5rem;
  margin-left: auto;
  border: 1px solid var(--industrial-line);
  background-color: var(--industrial-panel);
  background-image:
    linear-gradient(var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--industrial-grid) 1px, transparent 1px);
  background-size: 32px 32px;
}

.industrial-blueprint::before,
.industrial-blueprint::after {
  content: "";
  position: absolute;
  background: var(--color-accent-coral);
}

.industrial-blueprint::before {
  width: 4rem;
  height: 0.35rem;
  top: -0.2rem;
  left: 2rem;
}

.industrial-blueprint::after {
  width: 0.35rem;
  height: 4rem;
  right: -0.2rem;
  bottom: 2rem;
}

.industrial-blueprint svg {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
}

.industrial-blueprint path {
  stroke: var(--color-ink-muted);
  stroke-width: 1.5;
}

.industrial-blueprint circle {
  fill: var(--color-accent-coral);
}

.industrial-blueprint__index,
.industrial-blueprint__mark {
  position: absolute;
  z-index: 2;
  color: var(--color-ink-muted);
  font-size: 0.65rem;
  font-weight: 700;
  letter-spacing: 0.14em;
}

.industrial-blueprint__index {
  top: 1.25rem;
  left: 1.25rem;
  color: var(--color-accent-coral);
}

.industrial-blueprint__mark--top {
  top: 1.25rem;
  right: 1.25rem;
}

.industrial-blueprint__mark--bottom {
  right: 1.25rem;
  bottom: 1.25rem;
}

.industrial-blueprint__square {
  position: absolute;
  border: 1px solid var(--industrial-line-strong);
}

.industrial-blueprint__square--large {
  width: 8rem;
  height: 8rem;
  right: 3.5rem;
  top: 5rem;
  background: var(--industrial-accent-wash);
}

.industrial-blueprint__square--small {
  width: 3rem;
  height: 3rem;
  left: 4rem;
  bottom: 4rem;
  background: var(--color-accent-coral);
  border-color: var(--color-accent-coral);
}

@media (max-width: 1023px) {
  .industrial-square--one {
    right: -4rem;
    opacity: 0.55;
  }

  .industrial-curve--hero {
    right: -35rem;
  }
}

@media (prefers-reduced-motion: no-preference) {
  .industrial-blueprint__square--large {
    animation: blueprint-drift 10s ease-in-out infinite alternate;
  }
}

@keyframes blueprint-drift {
  from {
    transform: translate3d(0, 0, 0);
  }
  to {
    transform: translate3d(-10px, 12px, 0);
  }
}
</style>
