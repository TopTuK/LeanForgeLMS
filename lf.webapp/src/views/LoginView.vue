<script setup>
function signInWithPmi() {
  window.location.href = '/api/Auth/SignInPmi';
}
</script>

<template>
  <section class="login-page">
    <div class="login-page__curve login-page__curve--top" aria-hidden="true" />
    <div class="login-page__curve login-page__curve--bottom" aria-hidden="true" />
    <div class="login-page__square login-page__square--one" aria-hidden="true" />
    <div class="login-page__square login-page__square--two" aria-hidden="true" />

    <div class="container mx-auto px-6 py-16 md:py-24 relative z-10">
      <div class="login-page__heading">
        <span class="login-page__eyebrow">{{ $t('login.eyebrow') }}</span>
        <h1>{{ $t('login.title') }}</h1>
        <p>{{ $t('login.subtitle') }}</p>
      </div>

      <div class="login-options">
        <button
          type="button"
          class="login-tile login-tile--pmi"
          @click="signInWithPmi"
        >
          <span class="login-tile__index" aria-hidden="true">01</span>
          <span class="login-tile__icon login-tile__icon--pmi" aria-hidden="true">
            PMI
          </span>
          <span class="login-tile__copy">
            <strong>{{ $t('login.pmi.title') }}</strong>
            <span>{{ $t('login.pmi.description') }}</span>
          </span>
          <span class="login-tile__arrow" aria-hidden="true">→</span>
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
  overflow: hidden;
  isolation: isolate;
  background-color: var(--color-surface-950);
  background-image:
    linear-gradient(var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(
      115deg,
      var(--industrial-hero-start) 0%,
      var(--industrial-hero-middle) 60%,
      var(--industrial-hero-end) 100%
    );
  background-size: 40px 40px, 40px 40px, auto;
}

.login-page__heading {
  max-width: 43rem;
  margin-inline: auto;
  text-align: center;
}

.login-page__eyebrow {
  display: inline-block;
  margin-bottom: 1rem;
  color: var(--color-accent-coral);
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.login-page__heading h1 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(2.25rem, 6vw, 4.5rem);
  font-weight: 800;
  letter-spacing: -0.045em;
  line-height: 1.05;
}

.login-page__heading p {
  max-width: 34rem;
  margin: 1.25rem auto 0;
  color: var(--color-ink-muted);
  font-size: 1.05rem;
  line-height: 1.7;
}

.login-options {
  display: grid;
  max-width: 28rem;
  margin: 3rem auto 0;
  gap: 1.25rem;
  grid-template-columns: minmax(0, 1fr);
}

.login-tile {
  position: relative;
  display: grid;
  min-height: 15rem;
  padding: 2rem;
  overflow: hidden;
  grid-template-columns: auto 1fr auto;
  align-items: end;
  gap: 1.25rem;
  color: var(--color-ink);
  text-align: left;
  background: var(--industrial-panel);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  box-shadow: 0 18px 50px rgba(20, 20, 20, 0.07);
  backdrop-filter: blur(12px);
  cursor: pointer;
  transition:
    transform 0.2s ease,
    border-color 0.2s ease,
    box-shadow 0.2s ease;
}

.login-tile::before {
  content: "";
  position: absolute;
  width: 8rem;
  height: 8rem;
  top: -4rem;
  right: -3rem;
  border: 1px solid var(--industrial-line-strong);
  transform: rotate(18deg);
  transition: transform 0.3s ease;
}

.login-tile:hover,
.login-tile:focus-visible {
  border-color: var(--color-accent-coral);
  box-shadow: 0 24px 60px rgba(236, 104, 60, 0.14);
  outline: none;
  transform: translateY(-4px);
}

.login-tile:hover::before,
.login-tile:focus-visible::before {
  transform: rotate(28deg) scale(1.08);
}

.login-tile__index {
  position: absolute;
  top: 1.25rem;
  left: 1.25rem;
  color: var(--color-ink-faint);
  font-size: 0.65rem;
  font-weight: 700;
  letter-spacing: 0.14em;
}

.login-tile__icon {
  display: inline-flex;
  width: 3.5rem;
  height: 3.5rem;
  align-items: center;
  justify-content: center;
  border-radius: 0.6rem;
}

.login-tile__icon--pmi {
  color: #ffffff;
  background: var(--color-accent-coral);
  font-size: 0.8rem;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.login-tile__copy {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 0.4rem;
}

.login-tile__copy strong {
  color: var(--color-ink);
  font-size: 1.1rem;
  font-weight: 700;
}

.login-tile__copy span {
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  line-height: 1.5;
}

.login-tile__arrow {
  color: var(--color-accent-coral);
  font-size: 1.5rem;
  transition: transform 0.2s ease;
}

.login-tile:hover .login-tile__arrow,
.login-tile:focus-visible .login-tile__arrow {
  transform: translateX(4px);
}

.login-page__note {
  margin: 1.5rem auto 0;
  color: var(--color-ink-faint);
  font-size: 0.75rem;
  text-align: center;
}

.login-page__curve,
.login-page__square {
  position: absolute;
  pointer-events: none;
}

.login-page__curve {
  border: 1px solid var(--industrial-line);
  border-radius: 50%;
}

.login-page__curve--top {
  width: 32rem;
  height: 32rem;
  top: -24rem;
  right: -8rem;
  box-shadow: 0 0 0 36px var(--industrial-accent-wash);
}

.login-page__curve--bottom {
  width: 24rem;
  height: 24rem;
  bottom: -18rem;
  left: -8rem;
  box-shadow: 0 0 0 28px var(--industrial-grid);
}

.login-page__square {
  border: 1px solid rgba(236, 104, 60, 0.34);
}

.login-page__square--one {
  width: 5rem;
  height: 5rem;
  top: 14%;
  left: 6%;
  transform: rotate(16deg);
}

.login-page__square--two {
  width: 2.5rem;
  height: 2.5rem;
  right: 7%;
  bottom: 12%;
  background: var(--industrial-accent-wash);
  transform: rotate(-12deg);
}

@media (max-width: 767px) {
  .login-options {
    grid-template-columns: 1fr;
  }

  .login-tile {
    min-height: 12rem;
    padding: 1.5rem;
  }

  .login-page__square--one {
    left: -2rem;
  }
}
</style>
