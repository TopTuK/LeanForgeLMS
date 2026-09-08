<script setup>
import { ArrowLeft } from 'lucide-vue-next';
import { SUPPORT_EMAIL, TELEGRAM_HANDLE, TELEGRAM_URL } from '@/config';
import contactDesk from '@/assets/contact/contact-desk.jpg';
</script>

<template>
  <section class="contact-page">
    <span
      class="blueprint-grid blueprint-grid--band blueprint-grid--fade"
      aria-hidden="true"
    />

    <div class="contact-page__inner layout-max">
      <router-link
        :to="{ name: 'Home' }"
        class="contact-page__back"
      >
        <ArrowLeft
          class="size-4"
          aria-hidden="true"
        />
        {{ $t('contact.back') }}
      </router-link>

      <div class="contact-page__layout">
        <figure class="contact-page__media">
          <img
            :src="contactDesk"
            :alt="$t('contact.image_alt')"
            width="1600"
            height="1195"
            decoding="async"
          >
        </figure>

        <div class="contact-page__copy">
          <p class="mono-label contact-page__eyebrow">
            {{ $t('contact.eyebrow') }}
          </p>
          <h1 class="contact-page__title font-display">
            {{ $t('contact.title') }}
          </h1>
          <p class="contact-page__subtitle">
            {{ $t('contact.subtitle') }}
          </p>

          <dl class="contact-card">
            <div class="contact-card__row">
              <dt>{{ $t('contact.admin_label') }}</dt>
              <dd>{{ $t('common.author_name') }}</dd>
            </div>
            <div class="contact-card__row">
              <dt>{{ $t('contact.telegram_label') }}</dt>
              <dd>
                <a
                  :href="TELEGRAM_URL"
                  target="_blank"
                  rel="noopener noreferrer"
                >{{ TELEGRAM_HANDLE }}</a>
              </dd>
            </div>
            <div class="contact-card__row">
              <dt>{{ $t('contact.email_label') }}</dt>
              <dd>
                <a :href="`mailto:${SUPPORT_EMAIL}`">{{ SUPPORT_EMAIL }}</a>
              </dd>
            </div>
            <div class="contact-card__row">
              <dt>{{ $t('common.inn_label') }}</dt>
              <dd>
                <span class="contact-card__inn">{{ $t('common.inn_value') }}</span>
                <span class="contact-card__note">{{ $t('contact.inn_note') }}</span>
              </dd>
            </div>
            <div class="contact-card__row">
              <dt>{{ $t('common.tax_status_label') }}</dt>
              <dd>{{ $t('common.tax_status_note') }}</dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.contact-page {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  min-height: calc(100vh - var(--header-height));
  padding: clamp(2.5rem, 6vw, 4.5rem) 1.5rem clamp(4rem, 8vw, 6rem);
  background:
    radial-gradient(ellipse 60% 50% at 12% 8%, var(--industrial-accent-wash), transparent 68%),
    var(--band-bg);
  color: var(--band-ink);
}

.contact-page__inner {
  position: relative;
  z-index: 1;
}

.contact-page__back {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 500;
  transition: color 0.15s ease;
}

.contact-page__back:hover {
  color: var(--color-accent-coral);
}

.contact-page__layout {
  display: grid;
  grid-template-areas:
    "copy"
    "media";
  gap: clamp(2rem, 5vw, 3.5rem);
  margin-top: 2rem;
  align-items: start;
}

.contact-page__media {
  grid-area: media;
  margin: 0;
  overflow: hidden;
  border-radius: var(--radius-card);
  border: 1px solid var(--color-border-subtle);
  background: var(--color-card);
}

.contact-page__media img {
  display: block;
  width: 100%;
  aspect-ratio: 16 / 10;
  object-fit: cover;
  object-position: 40% 55%;
}

.contact-page__copy {
  grid-area: copy;
}

.contact-page__eyebrow {
  margin: 0 0 1rem;
  color: var(--color-accent-coral);
}

.contact-page__title {
  margin: 0;
  font-size: clamp(2rem, 5vw, 2.9rem);
  font-weight: 600;
  letter-spacing: -0.03em;
  line-height: 1.1;
  color: var(--color-ink);
}

.contact-page__subtitle {
  margin: 1rem 0 0;
  max-width: 34rem;
  color: var(--color-ink-muted);
  font-size: 1rem;
  line-height: 1.6;
}

.contact-card {
  margin: 2rem 0 0;
  border: 1px solid var(--color-border-subtle);
  border-top: 2px solid var(--color-accent-coral);
  border-radius: var(--radius-card);
  background: var(--color-card);
}

.contact-card__row {
  display: grid;
  grid-template-columns: 1fr;
  gap: 0.25rem;
  padding: 1.1rem 1.25rem;
}

.contact-card__row + .contact-card__row {
  border-top: 1px solid var(--color-border-subtle);
}

.contact-card__row dt {
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 0.12em;
  font-size: 0.68rem;
  color: var(--color-ink-faint);
}

.contact-card__row dd {
  margin: 0;
  font-size: 0.95rem;
  color: var(--color-ink);
}

.contact-card__row dd a {
  color: var(--color-accent-coral);
  transition: color 0.15s ease;
}

.contact-card__row dd a:hover {
  color: var(--color-accent-coral-dark);
}

.contact-card__inn {
  font-family: var(--font-mono);
  letter-spacing: 0.06em;
}

.contact-card__note {
  display: block;
  margin-top: 0.3rem;
  font-size: 0.78rem;
  color: var(--color-ink-faint);
}

@media (min-width: 560px) {
  .contact-card__row {
    grid-template-columns: 10.5rem 1fr;
    gap: 1rem;
    align-items: baseline;
  }

  .contact-card__row dt {
    padding-top: 0.15rem;
  }
}

@media (min-width: 960px) {
  .contact-page__layout {
    grid-template-columns: minmax(18rem, 0.92fr) minmax(22rem, 1.08fr);
    grid-template-areas: "media copy";
    gap: clamp(2.5rem, 5vw, 4.5rem);
    align-items: center;
  }

  .contact-page__media {
    position: sticky;
    top: calc(var(--header-height) + 1.5rem);
  }

  .contact-page__media img {
    aspect-ratio: 4 / 3;
  }
}
</style>
