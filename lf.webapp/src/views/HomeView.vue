<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import AudienceCard from '@/components/home/AudienceCard.vue';
import AuthorCard from '@/components/home/AuthorCard.vue';
import FaqItem from '@/components/home/FaqItem.vue';
import SectionHeading from '@/components/home/SectionHeading.vue';
import HeroDiagram from '@/components/home/HeroDiagram.vue';
import DisciplineStrip from '@/components/home/DisciplineStrip.vue';
import OutcomeCard from '@/components/home/OutcomeCard.vue';
import ProcessStep from '@/components/home/ProcessStep.vue';
import CtaBand from '@/components/home/CtaBand.vue';
import GeometricBackdrop from '@/components/layout/GeometricBackdrop.vue';
import authorPortrait from '@/assets/author-portrait.jpg';
import authorSecondary from '@/assets/author-secondary.jpg';
import ctaWorkspace from '@/assets/home/cta-workspace.jpg';
import heroPractice from '@/assets/home/hero-practice.jpg';
import learningWorkflow from '@/assets/home/learning-workflow.jpg';

const { tm } = useI18n();

const OUTCOMES = ['planning', 'flow', 'analysis', 'leadership'];
const AUDIENCE = [
  { key: 'pm', icon: 'pm' },
  { key: 'product', icon: 'product' },
  { key: 'team', icon: 'team' },
];
const APPROACH_STEPS = ['1', '2', '3'];
const FAQ_KEYS = ['1', '2', '3', '4', '5', '6', '7'];

const reduceMotion = typeof window !== 'undefined'
  && typeof window.matchMedia === 'function'
  && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

function reveal(delay = 0) {
  if (reduceMotion) return {};
  return {
    initial: { opacity: 0, y: 20 },
    visibleOnce: { opacity: 1, y: 0, transition: { duration: 380, delay } },
  };
}

const disciplines = computed(() => {
  const items = tm('home.disciplines.items');
  return Array.isArray(items) ? items : [];
});

const authorHighlights = computed(() => {
  const items = tm('home.author.highlights');
  return Array.isArray(items) ? items : [];
});

function index(n) {
  return String(n).padStart(2, '0');
}
</script>

<template>
  <div class="landing">
    <section
      id="hero"
      class="landing-hero"
      aria-labelledby="hero-title"
    >
      <span
        class="blueprint-grid blueprint-grid--band blueprint-grid--fade"
        aria-hidden="true"
      />

      <div class="landing-hero__inner layout-max">
        <div
          v-motion="reveal()"
          class="landing-hero__copy"
        >
          <p class="mono-label landing-hero__eyebrow">
            {{ $t('home.hero.eyebrow') }}
          </p>
          <h1
            id="hero-title"
            class="landing-hero__title font-display"
          >
            {{ $t('home.hero.headline') }}
          </h1>
          <p class="landing-hero__subtitle">
            {{ $t('home.hero.subheadline') }}
          </p>

          <a
            :href="$t('home.author.url')"
            target="_blank"
            rel="noopener noreferrer"
            class="landing-hero__byline"
          >
            <img
              :src="authorSecondary"
              :alt="$t('home.author.name')"
              class="landing-hero__byline-photo"
            >
            <span>{{ $t('home.hero.byline', { name: $t('home.author.name') }) }}</span>
          </a>

          <div class="landing-hero__actions">
            <a
              href="#audience"
              class="landing-hero__cta landing-hero__cta--primary"
            >
              {{ $t('home.hero.cta_primary') }}
            </a>
            <router-link
              :to="{ name: 'Login' }"
              class="landing-hero__cta landing-hero__cta--ghost"
            >
              {{ $t('home.hero.cta_secondary') }}
              <span aria-hidden="true">→</span>
            </router-link>
          </div>

          <p class="mono-label landing-hero__note">
            {{ $t('home.hero.note') }}
          </p>
        </div>

        <div
          v-motion="reveal(120)"
          class="landing-hero__diagram"
        >
          <HeroDiagram
            :labels="disciplines"
            :image-src="heroPractice"
            :image-alt="$t('home.images.hero_alt')"
          />
        </div>
      </div>
    </section>

    <section
      class="landing-strip"
      aria-hidden="true"
    >
      <div class="layout-max landing-strip__inner">
        <DisciplineStrip :items="disciplines" />
      </div>
    </section>

    <section
      id="outcomes"
      class="landing-section"
      aria-labelledby="outcomes-title"
    >
      <GeometricBackdrop dense />
      <div class="layout-max landing-section__inner">
        <SectionHeading
          v-motion="reveal()"
          :index="index(1)"
          :eyebrow="$t('home.outcomes.eyebrow')"
          :title="$t('home.outcomes.title')"
          :subtitle="$t('home.outcomes.subtitle')"
          heading-id="outcomes-title"
        />

        <div class="landing-grid landing-grid--outcomes">
          <OutcomeCard
            v-for="(key, i) in OUTCOMES"
            :key="key"
            v-motion="reveal(i * 90)"
            :index="index(i + 1)"
            :icon="key"
            :title="$t(`home.outcomes.items.${key}.title`)"
            :description="$t(`home.outcomes.items.${key}.description`)"
          />
        </div>
      </div>
    </section>

    <section
      id="audience"
      class="landing-section landing-section--band-soft"
      aria-labelledby="audience-title"
    >
      <div class="layout-max landing-section__inner">
        <SectionHeading
          v-motion="reveal()"
          :index="index(2)"
          :eyebrow="$t('home.audience.eyebrow')"
          :title="$t('home.audience.title')"
          :subtitle="$t('home.audience.subtitle')"
          heading-id="audience-title"
        />

        <div class="landing-grid landing-grid--audience">
          <AudienceCard
            v-for="(role, i) in AUDIENCE"
            :key="role.key"
            v-motion="reveal(i * 90)"
            :index="index(i + 1)"
            :icon="role.icon"
            :title="$t(`home.audience.items.${role.key}.title`)"
            :description="$t(`home.audience.items.${role.key}.description`)"
          />
        </div>
      </div>
    </section>

    <section
      id="approach"
      class="landing-section"
      aria-labelledby="approach-title"
    >
      <GeometricBackdrop dense />
      <div class="layout-max landing-section__inner landing-approach">
        <SectionHeading
          v-motion="reveal()"
          :index="index(3)"
          :eyebrow="$t('home.approach.eyebrow')"
          :title="$t('home.approach.title')"
          :subtitle="$t('home.approach.subtitle')"
          heading-id="approach-title"
        />

        <div class="landing-approach__grid">
          <ol
            v-motion="reveal(60)"
            class="landing-approach__steps"
          >
            <ProcessStep
              v-for="(step, i) in APPROACH_STEPS"
              :key="step"
              :step="step"
              :title="$t(`home.approach.steps.${step}.title`)"
              :description="$t(`home.approach.steps.${step}.description`)"
              :last="i === APPROACH_STEPS.length - 1"
            />
          </ol>

          <figure
            v-motion="reveal(120)"
            class="landing-approach__visual"
          >
            <img
              :src="learningWorkflow"
              :alt="$t('home.images.approach_alt')"
              loading="lazy"
              decoding="async"
            >
            <figcaption class="mono-label">
              {{ $t('home.images.approach_caption') }}
            </figcaption>
          </figure>
        </div>
      </div>
    </section>

    <section
      id="author"
      class="landing-section landing-section--band-soft"
      aria-labelledby="author-title"
    >
      <div class="layout-max landing-section__inner">
        <SectionHeading
          v-motion="reveal()"
          :index="index(4)"
          :eyebrow="$t('home.author.eyebrow')"
          :title="$t('home.author.title')"
          :subtitle="$t('home.author.subtitle')"
          heading-id="author-title"
        />

        <div v-motion="reveal(80)">
          <AuthorCard
            :name="$t('home.author.name')"
            :role="$t('home.author.role')"
            :bio="$t('home.author.bio')"
            :highlights="authorHighlights"
            :cta="$t('home.author.cta')"
            :href="$t('home.author.url')"
            :photo="authorPortrait"
          />
        </div>
      </div>
    </section>

    <section
      id="faq"
      class="landing-section"
      aria-labelledby="faq-title"
    >
      <div class="layout-max landing-section__inner landing-faq">
        <SectionHeading
          v-motion="reveal()"
          :index="index(5)"
          :eyebrow="$t('home.faq.eyebrow')"
          :title="$t('home.faq.title')"
          heading-id="faq-title"
        />

        <div
          v-motion="reveal(60)"
          class="landing-faq__list"
        >
          <FaqItem
            v-for="key in FAQ_KEYS"
            :key="key"
            :index="index(Number(key))"
            :question="$t(`home.faq.items.${key}.question`)"
            :answer="$t(`home.faq.items.${key}.answer`)"
          />
        </div>
      </div>
    </section>

    <section
      id="start"
      class="landing-section landing-section--cta"
      aria-labelledby="cta-band-heading"
    >
      <div class="layout-max landing-section__inner">
        <div v-motion="reveal()">
          <CtaBand
            heading-id="cta-band-heading"
            :eyebrow="$t('home.cta.eyebrow')"
            :title="$t('home.cta.title')"
            :subtitle="$t('home.cta.subtitle')"
          >
            <template #media>
              <img
                :src="ctaWorkspace"
                :alt="$t('home.images.cta_alt')"
                class="landing-cta__image"
                loading="lazy"
                decoding="async"
              >
            </template>

            <router-link
              :to="{ name: 'Login' }"
              class="landing-hero__cta landing-hero__cta--primary"
            >
              {{ $t('home.cta.primary') }}
            </router-link>
            <a
              :href="`mailto:${$t('home.cta.email')}?subject=${$t('home.cta.notify_subject')}`"
              class="landing-cta__notify"
            >
              {{ $t('home.cta.notify_cta') }}
            </a>
          </CtaBand>
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
  display: flex;
  align-items: center;
  min-height: calc(100vh - var(--header-height));
  padding: clamp(3rem, 8vw, 6rem) 1.5rem;
  background:
    radial-gradient(ellipse 70% 60% at 80% -10%, var(--industrial-accent-wash), transparent 70%),
    var(--band-bg);
  color: var(--band-ink);
}

.landing-hero__inner {
  position: relative;
  z-index: 1;
  display: grid;
  gap: clamp(2.5rem, 6vw, 4rem);
  align-items: center;
}

.landing-hero__eyebrow {
  color: var(--band-accent);
  margin: 0 0 1.25rem;
}

.landing-hero__title {
  margin: 0;
  font-size: clamp(2.5rem, 6vw, 4.5rem);
  font-weight: 600;
  letter-spacing: -0.04em;
  line-height: 1.03;
}

.landing-hero__subtitle {
  margin: 1.5rem 0 0;
  max-width: 34rem;
  color: var(--band-ink-muted);
  font-size: clamp(1.02rem, 2vw, 1.2rem);
  line-height: 1.6;
}

.landing-hero__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 1rem;
  margin-top: 2.25rem;
}

.landing-hero__cta {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.9rem 1.75rem;
  border-radius: var(--radius-pill);
  font-size: 0.9rem;
  font-weight: 600;
  transition: background-color 0.15s ease, border-color 0.15s ease, color 0.15s ease;
}

.landing-hero__cta--primary {
  background: var(--color-accent-coral);
  color: #fff;
}

.landing-hero__cta--primary:hover {
  background: var(--color-accent-coral-dark);
}

.landing-hero__cta--ghost {
  border: 1px solid var(--band-line);
  color: var(--band-ink);
}

.landing-hero__cta--ghost:hover {
  border-color: var(--band-accent);
  color: var(--band-accent);
}

.landing-hero__note {
  margin: 1.75rem 0 0;
  color: var(--band-ink-muted);
}

.landing-hero__byline {
  display: inline-flex;
  align-items: center;
  gap: 0.6rem;
  margin-top: 1.5rem;
  color: var(--band-ink-muted);
  font-size: 0.85rem;
  font-weight: 500;
  transition: color 0.15s ease;
}

.landing-hero__byline:hover {
  color: var(--band-accent);
}

.landing-hero__byline-photo {
  width: 1.75rem;
  height: 1.75rem;
  border-radius: 50%;
  object-fit: cover;
  object-position: top center;
  border: 1px solid var(--band-line);
}

.landing-hero__diagram {
  display: none;
}

.landing-strip {
  border-bottom: 1px solid var(--color-border-subtle);
  background: var(--color-surface-950);
}

.landing-strip__inner {
  padding: 1.5rem;
  overflow-x: auto;
}

.landing-section {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  padding: clamp(4rem, 9vw, 7rem) 1.5rem;
}

.landing-section--band-soft {
  background: var(--color-surface-900);
  border-block: 1px solid var(--color-border-subtle);
}

.landing-section--cta {
  padding-block: clamp(3rem, 7vw, 5rem);
}

.landing-section__inner {
  position: relative;
  z-index: 1;
}

.landing-grid {
  display: grid;
  gap: 1.25rem;
  margin-top: 2.75rem;
}

.landing-grid--outcomes {
  grid-template-columns: 1fr;
}

.landing-grid--audience {
  grid-template-columns: 1fr;
  max-width: 52rem;
}

.landing-approach__steps,
.landing-faq__list {
  margin: 2.75rem 0 0;
  padding: 0;
  list-style: none;
}

.landing-approach {
  max-width: none;
}

.landing-approach__grid {
  display: grid;
  gap: clamp(2rem, 5vw, 4rem);
}

.landing-approach__visual {
  position: relative;
  min-width: 0;
  margin: 2.75rem 0 0;
  overflow: hidden;
  align-self: start;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--industrial-panel);
}

.landing-approach__visual img {
  display: block;
  width: 100%;
  aspect-ratio: 4 / 3;
  object-fit: cover;
}

.landing-approach__visual figcaption {
  padding: 0.85rem 1rem;
  border-top: 1px solid var(--color-border-subtle);
  color: var(--color-ink-faint);
}

.landing-faq {
  max-width: 46rem;
}

.landing-faq__list {
  border-top: 1px solid var(--color-border-subtle);
}

.landing-cta__notify {
  color: var(--band-ink);
  font-size: 0.9rem;
  font-weight: 500;
  border-bottom: 1px solid var(--band-line);
  transition: color 0.15s ease, border-color 0.15s ease;
}

.landing-cta__notify:hover {
  color: var(--band-accent);
  border-color: var(--band-accent);
}

.landing-cta__image {
  display: block;
  width: 100%;
  min-height: 15rem;
  aspect-ratio: 16 / 10;
  object-fit: cover;
}

@media (min-width: 768px) {
  .landing-grid--outcomes {
    grid-template-columns: repeat(2, 1fr);
  }

  .landing-grid--audience {
    grid-template-columns: repeat(3, 1fr);
    max-width: none;
  }
}

@media (min-width: 1024px) {
  .landing-hero__inner {
    grid-template-columns: minmax(0, 1.1fr) minmax(20rem, 0.9fr);
  }

  .landing-hero__diagram {
    display: block;
  }

  .landing-grid--outcomes {
    grid-template-columns: repeat(4, 1fr);
  }

  .landing-approach__grid {
    grid-template-columns: minmax(0, 1fr) minmax(22rem, 0.9fr);
  }
}
</style>
