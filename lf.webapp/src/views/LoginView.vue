<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ArrowLeft } from 'lucide-vue-next';

const { tm } = useI18n();

const benefits = computed(() => {
  const items = tm('login.benefits');
  return Array.isArray(items) ? items : [];
});

function signInWithPmi() {
  window.location.href = '/api/Auth/SignInPmi';
}

function signInWithGoogle() {
  window.location.href = '/api/Auth/SignInGoogle';
}
</script>

<template>
  <section class="login-page">
    <span
      class="blueprint-grid blueprint-grid--band blueprint-grid--fade"
      aria-hidden="true"
    />
    <span
      class="login-page__ring"
      aria-hidden="true"
    />
    <span
      class="login-page__square"
      aria-hidden="true"
    />

    <router-link
      :to="{ name: 'Home' }"
      class="login-page__back"
    >
      <ArrowLeft
        class="size-4"
        aria-hidden="true"
      />
      {{ $t('login.back') }}
    </router-link>

    <div class="login-page__inner">
      <p class="mono-label login-page__eyebrow">
        {{ $t('login.eyebrow') }}
      </p>
      <h1 class="login-page__title font-display">
        {{ $t('login.title') }}
      </h1>
      <p class="login-page__subtitle">
        {{ $t('login.subtitle') }}
      </p>

      <div class="login-console">
        <span
          class="mono-label login-console__tag"
          aria-hidden="true"
        >// auth</span>

        <div class="login-console__options">
          <button
            type="button"
            class="login-card"
            @click="signInWithPmi"
          >
            <span
              class="login-card__no mono-label"
              aria-hidden="true"
            >01</span>
            <span
              class="login-card__mark login-card__mark--pmi"
              aria-hidden="true"
            >PMI</span>
            <span class="login-card__copy">
              <strong>{{ $t('login.pmi.title') }}</strong>
              <span>{{ $t('login.pmi.description') }}</span>
            </span>
            <span
              class="login-card__arrow"
              aria-hidden="true"
            >→</span>
          </button>

          <button
            type="button"
            class="login-card"
            @click="signInWithGoogle"
          >
            <span
              class="login-card__no mono-label"
              aria-hidden="true"
            >02</span>
            <span
              class="login-card__mark login-card__mark--google"
              aria-hidden="true"
            >
              <svg
                viewBox="0 0 48 48"
                width="22"
                height="22"
              >
                <path
                  fill="#FFC107"
                  d="M43.611 20.083H42V20H24v8h11.303c-1.649 4.657-6.08 8-11.303 8-6.627 0-12-5.373-12-12s5.373-12 12-12c3.059 0 5.842 1.154 7.961 3.039l5.657-5.657C34.046 6.053 29.268 4 24 4 12.955 4 4 12.955 4 24s8.955 20 20 20 20-8.955 20-20c0-1.341-.138-2.65-.389-3.917z"
                />
                <path
                  fill="#FF3D00"
                  d="M6.306 14.691l6.571 4.819C14.655 15.108 18.961 12 24 12c3.059 0 5.842 1.154 7.961 3.039l5.657-5.657C34.046 6.053 29.268 4 24 4 16.318 4 9.656 8.337 6.306 14.691z"
                />
                <path
                  fill="#4CAF50"
                  d="M24 44c5.166 0 9.86-1.977 13.409-5.192l-6.19-5.238C29.211 35.091 26.715 36 24 36c-5.202 0-9.619-3.317-11.283-7.946l-6.522 5.025C9.505 39.556 16.227 44 24 44z"
                />
                <path
                  fill="#1976D2"
                  d="M43.611 20.083H42V20H24v8h11.303a12.04 12.04 0 0 1-4.087 5.571l.003-.002 6.19 5.238C36.971 39.205 44 34 44 24c0-1.341-.138-2.65-.389-3.917z"
                />
              </svg>
            </span>
            <span class="login-card__copy">
              <strong>{{ $t('login.google.title') }}</strong>
              <span>{{ $t('login.google.description') }}</span>
            </span>
            <span
              class="login-card__arrow"
              aria-hidden="true"
            >→</span>
          </button>
        </div>

        <p class="login-console__note">
          {{ $t('login.note') }}
        </p>
      </div>

      <ul
        v-if="benefits.length"
        class="login-page__benefits"
      >
        <li
          v-for="(benefit, i) in benefits"
          :key="i"
        >
          <span
            class="login-page__benefit-tick"
            aria-hidden="true"
          />
          {{ benefit }}
        </li>
      </ul>
    </div>
  </section>
</template>

<style scoped>
.login-page {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: calc(100vh - var(--header-height));
  isolation: isolate;
  overflow: hidden;
  padding: clamp(3.5rem, 9vw, 6rem) 1.5rem;
  background:
    radial-gradient(ellipse 60% 55% at 82% -10%, var(--industrial-accent-wash), transparent 70%),
    var(--band-bg);
  color: var(--band-ink);
}

.login-page__ring,
.login-page__square {
  position: absolute;
  border: 1px solid var(--band-line);
  pointer-events: none;
}

.login-page__ring {
  width: 26rem;
  height: 26rem;
  right: -9rem;
  top: -9rem;
  border-radius: 50%;
  border-top-color: color-mix(in srgb, var(--band-accent) 45%, transparent);
}

.login-page__square {
  width: 12rem;
  height: 12rem;
  left: -4rem;
  bottom: -4rem;
  transform: rotate(18deg);
  background: var(--band-panel);
}

.login-page__back {
  position: absolute;
  top: clamp(1.25rem, 4vw, 2rem);
  left: clamp(1.25rem, 4vw, 2rem);
  z-index: 2;
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  color: var(--band-ink-muted);
  font-size: 0.82rem;
  font-weight: 500;
  transition: color 0.15s ease;
}

.login-page__back:hover {
  color: var(--band-accent);
}

.login-page__inner {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 34rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
}

.login-page__eyebrow {
  color: var(--band-accent);
  margin: 0 0 1rem;
}

.login-page__title {
  margin: 0;
  font-size: clamp(2.1rem, 5vw, 3.2rem);
  font-weight: 600;
  letter-spacing: -0.04em;
  line-height: 1.08;
}

.login-page__subtitle {
  margin: 1rem 0 0;
  max-width: 30rem;
  color: var(--band-ink-muted);
  font-size: 1rem;
  line-height: 1.6;
}

.login-console {
  position: relative;
  width: 100%;
  margin-top: 2.5rem;
  padding: clamp(1.5rem, 4vw, 2.25rem);
  background: var(--band-panel);
  border: 1px solid var(--band-line);
  border-top: 1px solid color-mix(in srgb, var(--band-accent) 55%, transparent);
  border-radius: var(--radius-card);
  backdrop-filter: blur(6px);
}

.login-console::before,
.login-console::after {
  content: "";
  position: absolute;
  width: 12px;
  height: 12px;
  border: 1px solid var(--band-accent);
}

.login-console::before {
  top: -1px;
  left: -1px;
  border-width: 1px 0 0 1px;
}

.login-console::after {
  bottom: -1px;
  right: -1px;
  border-width: 0 1px 1px 0;
}

.login-console__tag {
  display: block;
  margin-bottom: 1.25rem;
  color: var(--band-ink-muted);
  text-align: left;
}

.login-console__options {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.login-card {
  display: grid;
  grid-template-columns: auto auto 1fr auto;
  gap: 0.9rem;
  align-items: center;
  padding: 1.15rem 1.25rem;
  text-align: left;
  color: var(--band-ink);
  background: color-mix(in srgb, var(--band-ink) 4%, transparent);
  border: 1px solid var(--band-line);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: border-color 0.15s ease, background-color 0.15s ease, transform 0.15s ease;
}

.login-card:hover {
  border-color: var(--band-accent);
  background: color-mix(in srgb, var(--band-accent) 8%, transparent);
  transform: translateY(-2px);
}

.login-card:focus-visible {
  outline: 2px solid var(--band-accent);
  outline-offset: 2px;
}

.login-card__no {
  color: var(--band-ink-muted);
}

.login-card__mark {
  display: grid;
  width: 2.75rem;
  height: 2.75rem;
  place-items: center;
  border-radius: 0.55rem;
}

.login-card__mark--pmi {
  color: #fff;
  background: var(--color-accent-coral);
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.06em;
}

.login-card__mark--google {
  background: #fff;
  border: 1px solid var(--band-line);
}

.login-card__copy {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}

.login-card__copy strong {
  font-size: 0.95rem;
  font-weight: 600;
}

.login-card__copy span {
  color: var(--band-ink-muted);
  font-size: 0.8rem;
  line-height: 1.45;
}

.login-card__arrow {
  color: var(--band-ink-muted);
  transition: color 0.15s ease, transform 0.15s ease;
}

.login-card:hover .login-card__arrow {
  color: var(--band-accent);
  transform: translateX(3px);
}

.login-console__note {
  margin: 1.5rem 0 0;
  color: var(--band-ink-muted);
  font-size: 0.72rem;
  letter-spacing: 0.02em;
  text-align: left;
}

.login-page__benefits {
  margin: 2rem 0 0;
  padding: 0;
  list-style: none;
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 0.6rem 1.5rem;
}

.login-page__benefits li {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  color: var(--band-ink-muted);
  font-size: 0.82rem;
}

.login-page__benefit-tick {
  flex-shrink: 0;
  width: 0.7rem;
  height: 1px;
  background: var(--band-accent);
}

@media (max-width: 480px) {
  .login-card {
    grid-template-columns: auto 1fr auto;
  }

  .login-card__no {
    display: none;
  }
}
</style>
