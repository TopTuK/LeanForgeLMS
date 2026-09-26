<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute } from 'vue-router';
import { ArrowLeft, ArrowRight } from 'lucide-vue-next';
import authPmi from '@/assets/login/auth-pmi.png';
import authGoogle from '@/assets/login/auth-google.png';
import authYandex from '@/assets/login/auth-yandex.png';
import authVk from '@/assets/login/auth-vk.svg';
import authMailRu from '@/assets/login/auth-mailru.svg';
import authOk from '@/assets/login/auth-ok.svg';
import { trackLoginStarted } from '@/lib/analytics';

const { tm } = useI18n();
const route = useRoute();

const isEmailRequiredError = computed(() => route.query.error === 'email_required');

const primaryProviders = [
  {
    id: 'pmi',
    analytics: 'pmi',
    icon: authPmi,
    mark: 'pmi',
    titleKey: 'login.pmi.title',
    descriptionKey: 'login.pmi.description',
    href: '/api/Auth/SignInPmi',
  },
  {
    id: 'google',
    analytics: 'google',
    icon: authGoogle,
    mark: 'google',
    titleKey: 'login.google.title',
    descriptionKey: 'login.google.description',
    href: '/api/Auth/SignInGoogle',
  },
  {
    id: 'yandex',
    analytics: 'yandex',
    icon: authYandex,
    mark: 'yandex',
    titleKey: 'login.yandex.title',
    descriptionKey: 'login.yandex.description',
    href: '/api/Auth/SignInYandex',
  },
];

// One VK ID app signs in with VK, Mail.ru or OK; "provider" picks which login VK ID opens.
const vkProviders = [
  {
    id: 'vk',
    analytics: 'vk',
    icon: authVk,
    mark: 'plain',
    titleKey: 'login.vk.title',
    descriptionKey: 'login.vk.description',
    href: '/api/Auth/SignInVk?provider=vkid',
  },
  {
    id: 'mailru',
    analytics: 'mail_ru',
    icon: authMailRu,
    mark: 'plain',
    titleKey: 'login.mailru.title',
    descriptionKey: 'login.mailru.description',
    href: '/api/Auth/SignInVk?provider=mail_ru',
  },
  {
    id: 'ok',
    analytics: 'ok',
    icon: authOk,
    mark: 'plain',
    titleKey: 'login.ok.title',
    descriptionKey: 'login.ok.description',
    href: '/api/Auth/SignInVk?provider=ok_ru',
  },
];

const benefits = computed(() => {
  const items = tm('login.benefits');
  return Array.isArray(items) ? items : [];
});

function indexLabel(index) {
  return String(index + 1).padStart(2, '0');
}

function signIn(provider) {
  trackLoginStarted(provider.analytics);
  window.location.href = provider.href;
}
</script>

<template>
  <section class="login">
    <span
      class="login__grid"
      aria-hidden="true"
    />

    <div class="login__shell">
      <div class="login__mast">
        <router-link
          :to="{ name: 'Home' }"
          class="login__back"
        >
          <ArrowLeft
            class="size-4"
            aria-hidden="true"
          />
          {{ $t('login.back') }}
        </router-link>
        <span class="mono-label login__mast-id">LF-AUTH</span>
      </div>

      <div class="login__layout">
        <header class="login__intro">
          <p class="mono-label login__eyebrow">
            {{ $t('login.eyebrow') }}
          </p>
          <h1 class="login__title font-display">
            {{ $t('login.title') }}
          </h1>
          <p class="login__subtitle">
            {{ $t('login.subtitle') }}
          </p>

          <ol
            v-if="benefits.length"
            class="login__spec"
          >
            <li
              v-for="(benefit, i) in benefits"
              :key="i"
            >
              <span class="login__spec-no">{{ indexLabel(i) }}</span>
              <span>{{ benefit }}</span>
            </li>
          </ol>
        </header>

        <div class="login__plate">
          <span
            class="login__tick login__tick--tl"
            aria-hidden="true"
          />
          <span
            class="login__tick login__tick--tr"
            aria-hidden="true"
          />
          <span
            class="login__tick login__tick--bl"
            aria-hidden="true"
          />
          <span
            class="login__tick login__tick--br"
            aria-hidden="true"
          />

          <div class="login__plate-bar">
            <span class="mono-label">Auth</span>
            <span class="mono-label login__plate-count">01-06</span>
          </div>

          <p
            v-if="isEmailRequiredError"
            role="alert"
            class="login__error"
          >
            {{ $t('login.errors.emailRequired') }}
          </p>

          <div class="login__options">
            <button
              v-for="(provider, index) in primaryProviders"
              :key="provider.id"
              type="button"
              class="login-row"
              @click="signIn(provider)"
            >
              <span
                class="login-row__no"
                aria-hidden="true"
              >{{ indexLabel(index) }}</span>
              <span
                class="login-row__mark"
                :class="`login-row__mark--${provider.mark}`"
                aria-hidden="true"
              >
                <img
                  :src="provider.icon"
                  alt=""
                  width="36"
                  height="36"
                >
              </span>
              <span class="login-row__copy">
                <strong>{{ $t(provider.titleKey) }}</strong>
                <span>{{ $t(provider.descriptionKey) }}</span>
              </span>
              <ArrowRight
                class="login-row__arrow size-4"
                aria-hidden="true"
              />
            </button>
          </div>

          <p class="mono-label login__family">
            VK ID
          </p>

          <div class="login__options">
            <button
              v-for="(provider, index) in vkProviders"
              :key="provider.id"
              type="button"
              class="login-row"
              @click="signIn(provider)"
            >
              <span
                class="login-row__no"
                aria-hidden="true"
              >{{ indexLabel(index + primaryProviders.length) }}</span>
              <span
                class="login-row__mark"
                :class="`login-row__mark--${provider.mark}`"
                aria-hidden="true"
              >
                <img
                  :src="provider.icon"
                  alt=""
                  width="36"
                  height="36"
                >
              </span>
              <span class="login-row__copy">
                <strong>{{ $t(provider.titleKey) }}</strong>
                <span>{{ $t(provider.descriptionKey) }}</span>
              </span>
              <ArrowRight
                class="login-row__arrow size-4"
                aria-hidden="true"
              />
            </button>
          </div>

          <p class="login__note">
            {{ $t('login.note') }}
          </p>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.login {
  position: relative;
  display: flex;
  flex: 1 0 auto;
  isolation: isolate;
  overflow: hidden;
  min-height: calc(100vh - var(--header-height));
  background: var(--band-bg);
  color: var(--band-ink);
}

.login__grid {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background-image:
    linear-gradient(var(--band-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--band-grid) 1px, transparent 1px);
  background-size: 28px 28px;
  mask-image: radial-gradient(ellipse 85% 75% at 50% 42%, #000 10%, transparent 74%);
}

.login__shell {
  position: relative;
  z-index: 1;
  display: flex;
  flex: 1;
  flex-direction: column;
  justify-content: center;
  width: 100%;
  max-width: 72rem;
  margin-inline: auto;
  padding: clamp(1.25rem, 3vw, 2.25rem) clamp(1.25rem, 4vw, 3rem) clamp(2.5rem, 6vw, 4.5rem);
}

.login__mast {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--band-line);
}

.login__back {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  color: var(--band-ink-muted);
  font-size: 0.82rem;
  font-weight: 500;
  transition: color 0.15s ease;
}

.login__back:hover {
  color: var(--band-accent);
}

.login__mast-id {
  color: var(--band-ink-muted);
}

.login__layout {
  display: grid;
  grid-template-columns: minmax(16rem, 0.9fr) minmax(22rem, 1.1fr);
  gap: clamp(2rem, 5vw, 4.5rem);
  align-items: center;
  margin-top: clamp(1.75rem, 4vw, 3rem);
}

.login__eyebrow {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin: 0 0 1.15rem;
  color: var(--band-accent);
}

.login__eyebrow::before {
  content: "";
  width: 1.6rem;
  height: 1px;
  background: var(--band-accent);
}

.login__title {
  margin: 0;
  font-size: clamp(2.75rem, 6vw, 4.25rem);
  font-weight: 500;
  letter-spacing: -0.045em;
  line-height: 0.96;
}

.login__subtitle {
  margin: 1.15rem 0 0;
  max-width: 26rem;
  color: var(--band-ink-muted);
  font-size: 0.98rem;
  line-height: 1.65;
}

.login__spec {
  margin: 2.25rem 0 0;
  padding: 0;
  list-style: none;
  max-width: 26rem;
  border-top: 1px solid var(--band-line);
}

.login__spec li {
  display: grid;
  grid-template-columns: 2.25rem minmax(0, 1fr);
  gap: 0.75rem;
  align-items: baseline;
  padding: 0.8rem 0;
  border-bottom: 1px solid var(--band-line);
  font-size: 0.9rem;
  line-height: 1.4;
}

.login__spec-no {
  font-family: var(--font-mono);
  font-size: 0.68rem;
  letter-spacing: 0.12em;
  color: var(--band-accent);
}

.login__plate {
  position: relative;
  background: var(--color-surface-950);
  border: 1px solid var(--band-line);
  border-radius: 2px;
  box-shadow: 0 1px 0 color-mix(in srgb, var(--band-ink) 6%, transparent);
}

.login__tick {
  position: absolute;
  width: 9px;
  height: 9px;
  pointer-events: none;
}

.login__tick--tl,
.login__tick--tr,
.login__tick--bl,
.login__tick--br {
  border-color: var(--band-accent);
  border-style: solid;
}

.login__tick--tl {
  top: -1px;
  left: -1px;
  border-width: 1px 0 0 1px;
}

.login__tick--tr {
  top: -1px;
  right: -1px;
  border-width: 1px 1px 0 0;
}

.login__tick--bl {
  bottom: -1px;
  left: -1px;
  border-width: 0 0 1px 1px;
}

.login__tick--br {
  right: -1px;
  bottom: -1px;
  border-width: 0 1px 1px 0;
}

.login__plate-bar,
.login__family,
.login__note,
.login__error {
  padding-inline: 1.15rem;
}

.login__plate-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-block: 0.7rem;
  color: var(--band-ink-muted);
  background: color-mix(in srgb, var(--band-ink) 3.5%, transparent);
  border-bottom: 1px solid var(--band-line);
}

.login__plate-count {
  letter-spacing: 0.18em;
}

.login__error {
  margin: 0;
  padding-block: 0.85rem;
  border-bottom: 1px solid color-mix(in srgb, var(--color-accent-coral) 40%, var(--band-line));
  border-left: 2px solid var(--color-accent-coral);
  background: color-mix(in srgb, var(--color-accent-coral) 10%, transparent);
  color: var(--band-ink);
  font-size: 0.84rem;
  line-height: 1.5;
  text-align: left;
}

.login__family {
  margin: 0;
  padding-block: 0.55rem;
  color: var(--band-ink-muted);
  font-size: 0.62rem;
  background: color-mix(in srgb, var(--band-ink) 3.5%, transparent);
  border-top: 1px solid var(--band-line);
  border-bottom: 1px solid var(--band-line);
}

.login-row {
  display: grid;
  grid-template-columns: 2rem 2.25rem minmax(0, 1fr) auto;
  gap: 0.85rem;
  align-items: center;
  width: 100%;
  padding: 0.9rem 1.15rem;
  text-align: left;
  color: var(--band-ink);
  background: transparent;
  border: 0;
  border-bottom: 1px solid var(--band-line);
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.login__options .login-row:last-child {
  border-bottom: 0;
}

.login-row:hover {
  background: color-mix(in srgb, var(--band-accent) 7%, transparent);
}

.login-row:focus-visible {
  position: relative;
  z-index: 1;
  outline: 2px solid var(--band-accent);
  outline-offset: -2px;
}

.login-row__no {
  font-family: var(--font-mono);
  font-size: 0.68rem;
  letter-spacing: 0.08em;
  color: var(--band-ink-muted);
}

.login-row:hover .login-row__no {
  color: var(--band-accent);
}

.login-row__mark {
  display: grid;
  width: 2.25rem;
  height: 2.25rem;
  place-items: center;
  overflow: hidden;
  border-radius: 2px;
}

.login-row__mark img {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.login-row__mark--pmi {
  background: var(--color-accent-coral);
}

.login-row__mark--google {
  background: #fff;
  border: 1px solid var(--band-line);
}

.login-row__mark--google img {
  object-fit: contain;
  padding: 0.28rem;
}

.login-row__mark--yandex {
  background: #fc3f1d;
}

.login-row__copy {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 0;
}

.login-row__copy strong {
  font-size: 0.92rem;
  font-weight: 600;
  letter-spacing: -0.01em;
}

.login-row__copy span {
  color: var(--band-ink-muted);
  font-size: 0.78rem;
  line-height: 1.4;
  text-wrap: pretty;
}

.login-row__arrow {
  color: var(--band-ink-muted);
  transition: color 0.15s ease, transform 0.15s ease;
}

.login-row:hover .login-row__arrow {
  color: var(--band-accent);
  transform: translateX(3px);
}

.login__note {
  margin: 0;
  padding-block: 0.8rem 0.95rem;
  color: var(--band-ink-muted);
  font-size: 0.72rem;
  letter-spacing: 0.01em;
  border-top: 1px solid var(--band-line);
}

@media (max-width: 860px) {
  .login__layout {
    grid-template-columns: 1fr;
    align-items: stretch;
  }

  .login__subtitle,
  .login__spec {
    max-width: none;
  }
}

@media (max-width: 480px) {
  .login-row {
    grid-template-columns: 2.25rem minmax(0, 1fr) auto;
    gap: 0.7rem;
  }

  .login-row__no {
    display: none;
  }

  .login__mast-id {
    display: none;
  }
}

@media (prefers-reduced-motion: reduce) {
  .login-row,
  .login-row__arrow,
  .login__back {
    transition: none;
  }

  .login-row:hover .login-row__arrow {
    transform: none;
  }
}
</style>
