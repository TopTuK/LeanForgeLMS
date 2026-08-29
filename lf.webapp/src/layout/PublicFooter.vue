<script setup>
import ThemeToggleButton from '@/components/layout/ThemeToggleButton.vue';
import LocaleToggleButton from '@/components/layout/LocaleToggleButton.vue';
import logo from '@/assets/logo.svg';

const navLinks = [
  { href: '#programs', labelKey: 'nav.programs' },
  { href: '#approach', labelKey: 'nav.approach' },
  { href: '#faq', labelKey: 'nav.faq' },
];

const legalLinks = ['footer.privacy', 'footer.offer', 'footer.cookies'];
</script>

<template>
  <footer class="public-footer">
    <div class="public-footer__inner layout-max">
      <div class="public-footer__brandcol">
        <div class="public-footer__brand">
          <img
            :src="logo"
            alt=""
            class="public-footer__logo"
          >
          <span class="font-display">{{ $t('common.brand_name') }}</span>
        </div>
        <p class="public-footer__manifesto">
          {{ $t('footer.manifesto') }}
        </p>
      </div>

      <nav class="public-footer__col">
        <p class="mono-label public-footer__coltitle">
          {{ $t('footer.explore') }}
        </p>
        <a
          v-for="link in navLinks"
          :key="link.href"
          :href="link.href"
        >
          {{ $t(link.labelKey) }}
        </a>
        <router-link :to="{ name: 'Login' }">
          {{ $t('nav.login') }}
        </router-link>
      </nav>

      <div class="public-footer__col">
        <p class="mono-label public-footer__coltitle">
          {{ $t('footer.contact') }}
        </p>
        <a :href="`mailto:${$t('footer.email')}`">{{ $t('footer.email') }}</a>
        <a
          :href="$t('home.author.url')"
          target="_blank"
          rel="noopener noreferrer"
        >s-sidorov.ru</a>
      </div>

      <div class="public-footer__col">
        <p class="mono-label public-footer__coltitle">
          {{ $t('footer.settings') }}
        </p>
        <div class="public-footer__toggles">
          <ThemeToggleButton />
          <LocaleToggleButton />
        </div>
      </div>
    </div>

    <div class="public-footer__bar layout-max">
      <p class="public-footer__meta">
        <span>{{ $t('common.brand_name') }}</span>
        <span class="public-footer__dot">·</span>
        <span>{{ $t('common.author_name') }}</span>
        <span class="public-footer__dot">·</span>
        <span>&copy; {{ $t('common.created_date') }}</span>
      </p>
      <nav class="public-footer__legal">
        <a
          v-for="key in legalLinks"
          :key="key"
          href="#start"
        >{{ $t(key) }}</a>
      </nav>
    </div>
  </footer>
</template>

<style scoped>
.public-footer {
  border-top: 1px solid var(--color-border-subtle);
  background: var(--color-surface-900);
}

.public-footer__inner {
  display: grid;
  grid-template-columns: 1fr;
  gap: 2.5rem;
  padding: clamp(2.5rem, 6vw, 4rem) 1.5rem 2.5rem;
}

.public-footer__brand {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  color: var(--color-ink);
  font-weight: 600;
}

.public-footer__logo {
  width: 1.75rem;
  height: 1.75rem;
}

.public-footer__manifesto {
  margin: 1rem 0 0;
  max-width: 22rem;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  line-height: 1.6;
}

.public-footer__col {
  display: flex;
  flex-direction: column;
  gap: 0.7rem;
}

.public-footer__coltitle {
  color: var(--color-ink-faint);
  margin: 0 0 0.3rem;
}

.public-footer__col a {
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  transition: color 0.15s ease;
}

.public-footer__col a:hover {
  color: var(--color-ink);
}

.public-footer__toggles {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.public-footer__bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.5rem;
  border-top: 1px solid var(--color-border-subtle);
}

.public-footer__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  margin: 0;
  color: var(--color-ink-faint);
  font-size: 0.8rem;
}

.public-footer__legal {
  display: flex;
  flex-wrap: wrap;
  gap: 1.25rem;
}

.public-footer__legal a {
  color: var(--color-ink-muted);
  font-size: 0.8rem;
  transition: color 0.15s ease;
}

.public-footer__legal a:hover {
  color: var(--color-ink);
}

@media (min-width: 640px) {
  .public-footer__inner {
    grid-template-columns: 1.4fr 1fr 1fr 1fr;
  }
}
</style>
