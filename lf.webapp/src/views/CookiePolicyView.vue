<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ArrowLeft, CheckCircle2, ShieldCheck } from 'lucide-vue-next';
import cookiePolicyImage from '@/assets/legal/cookie-policy.jpg';

const { tm, rt } = useI18n();

const getList = (key) => computed(() => {
  const items = tm(key);
  return Array.isArray(items) ? items : [];
});

const highlights = getList('cookies.highlights');
const cookieRecords = getList('cookies.cookie_records');
const storageRecords = getList('cookies.storage_records');
const sections = getList('cookies.sections');
</script>

<template>
  <article class="cookie-page">
    <span
      class="blueprint-grid blueprint-grid--band blueprint-grid--fade"
      aria-hidden="true"
    />

    <div class="cookie-page__inner layout-max">
      <router-link
        :to="{ name: 'Home' }"
        class="cookie-page__back"
      >
        <ArrowLeft
          class="size-4"
          aria-hidden="true"
        />
        {{ $t('cookies.back') }}
      </router-link>

      <header class="cookie-hero">
        <div class="cookie-hero__copy">
          <p class="mono-label cookie-page__eyebrow">
            {{ $t('cookies.eyebrow') }}
          </p>
          <h1 class="cookie-page__title font-display">
            {{ $t('cookies.title') }}
          </h1>
          <p class="cookie-page__intro">
            {{ $t('cookies.intro') }}
          </p>
          <p class="cookie-page__meta">
            {{ $t('cookies.updated') }}
          </p>
        </div>

        <figure class="cookie-hero__media">
          <img
            :src="cookiePolicyImage"
            :alt="$t('cookies.image_alt')"
            width="1024"
            height="768"
            decoding="async"
          >
          <figcaption>
            <ShieldCheck
              class="size-4"
              aria-hidden="true"
            />
            {{ $t('cookies.image_caption') }}
          </figcaption>
        </figure>
      </header>

      <ul
        class="cookie-highlights"
        :aria-label="$t('cookies.highlights_label')"
      >
        <li
          v-for="highlight in highlights"
          :key="highlight.label"
        >
          <CheckCircle2
            class="size-5"
            aria-hidden="true"
          />
          <span>
            <strong>{{ rt(highlight.value) }}</strong>
            {{ rt(highlight.label) }}
          </span>
        </li>
      </ul>

      <div class="cookie-layout">
        <aside class="cookie-nav">
          <p class="mono-label">
            {{ $t('cookies.contents_label') }}
          </p>
          <nav :aria-label="$t('cookies.contents_label')">
            <a href="#essential">{{ $t('cookies.cookies_heading') }}</a>
            <a href="#preferences">{{ $t('cookies.storage_heading') }}</a>
            <a
              v-for="(section, index) in sections"
              :key="section.heading"
              :href="`#section-${index + 1}`"
            >
              {{ rt(section.heading) }}
            </a>
          </nav>
        </aside>

        <div class="cookie-document">
          <section
            id="essential"
            class="cookie-section"
          >
            <p class="mono-label cookie-section__kicker">
              {{ $t('cookies.essential_label') }}
            </p>
            <h2 class="cookie-section__heading font-display">
              {{ $t('cookies.cookies_heading') }}
            </h2>
            <p class="cookie-section__lead">
              {{ $t('cookies.cookies_intro') }}
            </p>

            <div class="record-list">
              <article
                v-for="record in cookieRecords"
                :key="record.name"
                class="record-card"
              >
                <div class="record-card__title">
                  <code>{{ rt(record.name) }}</code>
                  <span>{{ rt(record.duration) }}</span>
                </div>
                <p>{{ rt(record.purpose) }}</p>
              </article>
            </div>
          </section>

          <section
            id="preferences"
            class="cookie-section"
          >
            <p class="mono-label cookie-section__kicker">
              {{ $t('cookies.preferences_label') }}
            </p>
            <h2 class="cookie-section__heading font-display">
              {{ $t('cookies.storage_heading') }}
            </h2>
            <p class="cookie-section__lead">
              {{ $t('cookies.storage_intro') }}
            </p>

            <dl class="storage-list">
              <div
                v-for="record in storageRecords"
                :key="record.name"
                class="storage-list__row"
              >
                <dt>{{ rt(record.name) }}</dt>
                <dd>{{ rt(record.purpose) }}</dd>
              </div>
            </dl>
          </section>

          <section
            v-for="(section, index) in sections"
            :id="`section-${index + 1}`"
            :key="section.heading"
            class="cookie-section"
          >
            <p class="mono-label cookie-section__kicker">
              {{ rt(section.label) }}
            </p>
            <h2 class="cookie-section__heading font-display">
              {{ rt(section.heading) }}
            </h2>
            <template
              v-for="(block, blockIndex) in section.blocks"
              :key="blockIndex"
            >
              <p
                v-if="block.type === 'p'"
                class="cookie-section__p"
              >
                {{ rt(block.text) }}
              </p>
              <ul
                v-else-if="block.type === 'ul'"
                class="cookie-section__list"
              >
                <li
                  v-for="item in block.items"
                  :key="item"
                >
                  {{ rt(item) }}
                </li>
              </ul>
            </template>
          </section>

          <aside class="cookie-contact">
            <div>
              <p class="mono-label">
                {{ $t('cookies.contact_label') }}
              </p>
              <p>{{ $t('cookies.contact_text') }}</p>
            </div>
            <a :href="`mailto:${$t('cookies.contact_email')}`">
              {{ $t('cookies.contact_email') }}
            </a>
          </aside>
        </div>
      </div>
    </div>
  </article>
</template>

<style scoped>
.cookie-page {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  min-height: calc(100vh - var(--header-height));
  padding: clamp(2rem, 5vw, 4rem) 1.5rem clamp(4rem, 8vw, 7rem);
  background:
    radial-gradient(ellipse 60% 42% at 8% 4%, var(--industrial-accent-wash), transparent 70%),
    var(--band-bg);
  color: var(--band-ink);
}

.cookie-page__inner {
  position: relative;
  z-index: 1;
}

.cookie-page__back {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 500;
  transition: color 0.15s ease;
}

.cookie-page__back:hover,
.cookie-nav a:hover,
.cookie-contact a:hover {
  color: var(--color-accent-coral);
}

.cookie-hero {
  display: grid;
  gap: clamp(2rem, 6vw, 5rem);
  margin-top: clamp(2rem, 5vw, 3.5rem);
  align-items: center;
}

.cookie-page__eyebrow {
  margin: 0 0 1rem;
  color: var(--color-accent-coral);
}

.cookie-page__title {
  max-width: 38rem;
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(2.5rem, 7vw, 4.5rem);
  font-weight: 600;
  letter-spacing: -0.055em;
  line-height: 0.98;
}

.cookie-page__intro {
  max-width: 38rem;
  margin: 1.5rem 0 0;
  color: var(--color-ink-muted);
  font-size: clamp(1rem, 2vw, 1.12rem);
  line-height: 1.75;
}

.cookie-page__meta {
  margin: 1.4rem 0 0;
  color: var(--color-ink-faint);
  font-family: var(--font-mono);
  font-size: 0.7rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.cookie-hero__media {
  position: relative;
  margin: 0;
  overflow: hidden;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-card);
  box-shadow: 0 1.5rem 4rem color-mix(in srgb, var(--color-surface-900) 18%, transparent);
}

.cookie-hero__media::after {
  position: absolute;
  inset: 0;
  border: 1px solid color-mix(in srgb, white 12%, transparent);
  border-radius: inherit;
  pointer-events: none;
  content: "";
}

.cookie-hero__media img {
  display: block;
  width: 100%;
  aspect-ratio: 4 / 3;
  object-fit: cover;
}

.cookie-hero__media figcaption {
  position: absolute;
  right: 1rem;
  bottom: 1rem;
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.6rem 0.75rem;
  border: 1px solid color-mix(in srgb, white 18%, transparent);
  border-radius: 0.55rem;
  background: color-mix(in srgb, var(--color-surface-900) 86%, transparent);
  color: var(--color-ink-muted);
  font-size: 0.72rem;
  backdrop-filter: blur(12px);
}

.cookie-highlights {
  display: grid;
  gap: 0;
  margin: clamp(2.5rem, 7vw, 5rem) 0 0;
  padding: 0;
  border-block: 1px solid var(--color-border-subtle);
  list-style: none;
}

.cookie-highlights li {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  padding: 1rem 0;
  color: var(--color-ink-muted);
  font-size: 0.85rem;
}

.cookie-highlights li + li {
  border-top: 1px solid var(--color-border-subtle);
}

.cookie-highlights svg {
  flex: 0 0 auto;
  color: var(--color-accent-coral);
}

.cookie-highlights strong {
  margin-right: 0.25rem;
  color: var(--color-ink);
  font-weight: 600;
}

.cookie-layout {
  display: grid;
  gap: clamp(2.5rem, 7vw, 5rem);
  max-width: 65rem;
  margin: clamp(4rem, 10vw, 7rem) auto 0;
}

.cookie-nav {
  display: none;
}

.cookie-nav .mono-label {
  margin: 0 0 1rem;
  color: var(--color-ink-faint);
}

.cookie-nav nav {
  display: flex;
  flex-direction: column;
  gap: 0.8rem;
}

.cookie-nav a {
  color: var(--color-ink-muted);
  font-size: 0.8rem;
  line-height: 1.4;
  transition: color 0.15s ease;
}

.cookie-document {
  min-width: 0;
}

.cookie-section {
  scroll-margin-top: calc(var(--header-height) + 2rem);
}

.cookie-section + .cookie-section {
  margin-top: clamp(4rem, 9vw, 6rem);
  padding-top: clamp(3rem, 7vw, 4.5rem);
  border-top: 1px solid var(--color-border-subtle);
}

.cookie-section__kicker {
  margin: 0 0 0.9rem;
  color: var(--color-accent-coral);
}

.cookie-section__heading {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.55rem, 4vw, 2.05rem);
  font-weight: 600;
  letter-spacing: -0.035em;
}

.cookie-section__lead,
.cookie-section__p,
.cookie-section__list {
  color: var(--color-ink-muted);
  font-size: 0.96rem;
  line-height: 1.8;
}

.cookie-section__lead {
  max-width: 43rem;
  margin: 1rem 0 0;
}

.cookie-section__p {
  margin: 1rem 0 0;
}

.cookie-section__list {
  margin: 1rem 0 0;
  padding-left: 1.25rem;
}

.cookie-section__list li + li {
  margin-top: 0.5rem;
}

.record-list {
  margin-top: 2rem;
  border-block: 1px solid var(--color-border-subtle);
}

.record-card {
  display: grid;
  gap: 0.75rem;
  padding: 1.4rem 0;
}

.record-card + .record-card {
  border-top: 1px solid var(--color-border-subtle);
}

.record-card__title {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.record-card code {
  color: var(--color-ink);
  font-family: var(--font-mono);
  font-size: 0.82rem;
  overflow-wrap: anywhere;
}

.record-card__title span {
  color: var(--color-ink-faint);
  font-family: var(--font-mono);
  font-size: 0.68rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.record-card p {
  max-width: 43rem;
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  line-height: 1.7;
}

.storage-list {
  margin: 2rem 0 0;
  border-block: 1px solid var(--color-border-subtle);
}

.storage-list__row {
  display: grid;
  gap: 0.45rem;
  padding: 1.15rem 0;
}

.storage-list__row + .storage-list__row {
  border-top: 1px solid var(--color-border-subtle);
}

.storage-list__row dt {
  color: var(--color-ink);
  font-weight: 600;
}

.storage-list__row dd {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  line-height: 1.65;
}

.cookie-contact {
  display: flex;
  flex-wrap: wrap;
  align-items: end;
  justify-content: space-between;
  gap: 1.5rem;
  margin-top: clamp(4rem, 9vw, 6rem);
  padding: 1.5rem;
  border: 1px solid var(--color-border-subtle);
  border-left: 3px solid var(--color-accent-coral);
  border-radius: var(--radius-card);
  background: var(--color-card);
}

.cookie-contact .mono-label {
  margin: 0;
  color: var(--color-accent-coral);
}

.cookie-contact p:not(.mono-label) {
  max-width: 34rem;
  margin: 0.65rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  line-height: 1.65;
}

.cookie-contact a {
  color: var(--color-ink);
  font-family: var(--font-mono);
  font-size: 0.82rem;
  transition: color 0.15s ease;
}

@media (min-width: 640px) {
  .cookie-highlights {
    grid-template-columns: repeat(3, 1fr);
  }

  .cookie-highlights li {
    padding: 1.1rem 1.25rem;
  }

  .cookie-highlights li:first-child {
    padding-left: 0;
  }

  .cookie-highlights li + li {
    border-top: 0;
    border-left: 1px solid var(--color-border-subtle);
  }

  .storage-list__row {
    grid-template-columns: minmax(10rem, 0.7fr) 1.3fr;
    gap: 2rem;
  }
}

@media (min-width: 900px) {
  .cookie-hero {
    grid-template-columns: minmax(0, 1.08fr) minmax(22rem, 0.92fr);
  }

  .cookie-layout {
    grid-template-columns: 11rem minmax(0, 1fr);
  }

  .cookie-nav {
    position: sticky;
    top: calc(var(--header-height) + 2rem);
    display: block;
    align-self: start;
  }
}
</style>
