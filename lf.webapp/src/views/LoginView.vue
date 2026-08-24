<script setup>
import GeometricBackdrop from '@/components/layout/GeometricBackdrop.vue';

function signInWithPmi() {
  window.location.href = '/api/Auth/SignInPmi';
}

function signInWithGoogle() {
  window.location.href = '/api/Auth/SignInGoogle';
}
</script>

<template>
  <section class="login-page">
    <GeometricBackdrop />

    <div class="container relative z-10 mx-auto px-6 py-16 md:py-24">
      <div class="login-page__heading">
        <span class="login-page__eyebrow">{{ $t('login.eyebrow') }}</span>
        <h1>{{ $t('login.title') }}</h1>
        <p>{{ $t('login.subtitle') }}</p>
      </div>

      <div class="login-options">
        <button
          type="button"
          class="login-card"
          @click="signInWithPmi"
        >
          <span
            class="login-card__mark login-card__mark--pmi"
            aria-hidden="true"
          >PMI</span>
          <span class="login-card__copy">
            <strong>{{ $t('login.pmi.title') }}</strong>
            <span>{{ $t('login.pmi.description') }}</span>
          </span>
          <span aria-hidden="true">→</span>
        </button>

        <button
          type="button"
          class="login-card"
          @click="signInWithGoogle"
        >
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
          <span aria-hidden="true">→</span>
        </button>
      </div>

      <p class="login-page__note">
        {{ $t('login.note') }}
      </p>
    </div>
  </section>
</template>

<style scoped>
.login-page {
  position: relative;
  display: flex;
  min-height: calc(100vh - 4.5rem);
  align-items: center;
  isolation: isolate;
  background: var(--color-surface-950);
}

.login-page__heading {
  max-width: 40rem;
  margin-inline: auto;
  text-align: center;
}

.login-page__eyebrow {
  display: inline-block;
  margin-bottom: 1rem;
  color: var(--color-accent-coral);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.login-page__heading h1 {
  margin: 0;
  color: var(--color-ink);
  font-family: var(--font-display);
  font-size: clamp(2.1rem, 5vw, 3.6rem);
  font-weight: 600;
  letter-spacing: -0.04em;
  line-height: 1.08;
}

.login-page__heading p {
  max-width: 32rem;
  margin: 1.1rem auto 0;
  color: var(--color-ink-muted);
  font-size: 1.05rem;
  line-height: 1.65;
}

.login-options {
  display: grid;
  max-width: 44rem;
  margin: 2.75rem auto 0;
  gap: 1rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.login-card {
  display: grid;
  grid-template-columns: auto 1fr auto;
  gap: 1rem;
  align-items: center;
  min-height: 8.5rem;
  padding: 1.35rem 1.4rem;
  text-align: left;
  color: var(--color-ink);
  background: var(--color-card);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  cursor: pointer;
  transition: border-color 0.15s ease, transform 0.15s ease;
}

.login-card:hover {
  border-color: var(--color-accent-coral);
  transform: translateY(-2px);
}

.login-card__mark {
  display: grid;
  width: 3rem;
  height: 3rem;
  place-items: center;
  border-radius: 0.6rem;
}

.login-card__mark--pmi {
  color: #fff;
  background: var(--color-accent-coral);
  font-size: 0.7rem;
  font-weight: 800;
  letter-spacing: 0.06em;
}

.login-card__mark--google {
  background: #fff;
  border: 1px solid var(--color-border-subtle);
}

.login-card__copy {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.login-card__copy strong {
  font-size: 1rem;
}

.login-card__copy span {
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  line-height: 1.45;
}

.login-page__note {
  margin: 1.5rem auto 0;
  color: var(--color-ink-faint);
  font-size: 0.75rem;
  text-align: center;
}

@media (max-width: 767px) {
  .login-options {
    grid-template-columns: 1fr;
  }
}
</style>
