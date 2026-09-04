<script setup>
import { onBeforeUnmount, onMounted, ref } from 'vue';
import { Menu } from 'lucide-vue-next';
import ThemeToggleButton from '@/components/layout/ThemeToggleButton.vue';
import LocaleToggleButton from '@/components/layout/LocaleToggleButton.vue';
import { Button } from '@/components/ui/button';
import { Sheet } from '@/components/ui/sheet';
import logo from '@/assets/logo.svg';

const navLinks = [
  { href: '#audience', labelKey: 'nav.audience' },
  { href: '#approach', labelKey: 'nav.approach' },
  { href: '#faq', labelKey: 'nav.faq' },
];

const mobileOpen = ref(false);
const condensed = ref(false);

let scrollEl = null;

function handleScroll() {
  condensed.value = (scrollEl?.scrollTop ?? 0) > 8;
}

onMounted(() => {
  scrollEl = document.querySelector('.base-scroll');
  if (scrollEl) {
    scrollEl.addEventListener('scroll', handleScroll, { passive: true });
    handleScroll();
  }
});

onBeforeUnmount(() => {
  scrollEl?.removeEventListener('scroll', handleScroll);
});

function closeMobile() {
  mobileOpen.value = false;
}
</script>

<template>
  <div
    class="app-header"
    :class="{ 'is-condensed': condensed }"
  >
    <div class="app-header__bar layout-max">
      <router-link
        :to="{ name: 'Home' }"
        class="app-header__brand"
        @click="closeMobile"
      >
        <img
          :src="logo"
          alt=""
          class="app-header__logo"
        >
        <span class="app-header__name font-display">
          <span class="sm:hidden">{{ $t('common.brand_short') }}</span>
          <span class="hidden sm:inline">{{ $t('common.brand_name') }}</span>
        </span>
      </router-link>

      <nav class="app-header__nav">
        <a
          v-for="link in navLinks"
          :key="link.href"
          :href="link.href"
          class="app-header__link"
        >
          {{ $t(link.labelKey) }}
        </a>
      </nav>

      <div class="app-header__actions">
        <ThemeToggleButton />
        <LocaleToggleButton />
        <Button
          as-child
          size="pill"
        >
          <router-link :to="{ name: 'Login' }">
            {{ $t('nav.login') }}
            <span aria-hidden="true">→</span>
          </router-link>
        </Button>
      </div>

      <button
        type="button"
        class="app-header__burger"
        :aria-label="$t('nav.menu_toggle')"
        @click="mobileOpen = true"
      >
        <Menu class="size-6" />
      </button>
    </div>

    <Sheet
      v-model:open="mobileOpen"
      side="right"
      :title="$t('common.brand_short')"
    >
      <nav class="flex flex-col gap-1">
        <a
          v-for="link in navLinks"
          :key="link.href"
          :href="link.href"
          class="rounded-md px-2 py-2 text-sm font-medium text-ink-muted hover:bg-surface-900 hover:text-ink"
          @click="closeMobile"
        >
          {{ $t(link.labelKey) }}
        </a>
      </nav>
      <div class="mt-6 flex items-center justify-between">
        <div class="flex items-center gap-3">
          <ThemeToggleButton />
          <LocaleToggleButton />
        </div>
        <Button
          as-child
          size="sm"
        >
          <router-link
            :to="{ name: 'Login' }"
            @click="closeMobile"
          >
            {{ $t('nav.login') }}
          </router-link>
        </Button>
      </div>
    </Sheet>
  </div>
</template>

<style scoped>
.app-header {
  width: 100%;
  height: 100%;
  background: color-mix(in srgb, var(--color-surface-950) 82%, transparent);
  border-bottom: 1px solid transparent;
  backdrop-filter: blur(12px);
  transition: background-color 0.2s ease, border-color 0.2s ease;
}

.app-header.is-condensed {
  background: color-mix(in srgb, var(--color-surface-950) 94%, transparent);
  border-bottom-color: var(--color-border-subtle);
}

.app-header__bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1.5rem;
  height: 100%;
  padding-inline: 1.5rem;
}

.app-header__brand {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  min-width: 0;
}

.app-header__logo {
  width: 2rem;
  height: 2rem;
  transition: width 0.2s ease, height 0.2s ease;
}

.app-header.is-condensed .app-header__logo {
  width: 1.65rem;
  height: 1.65rem;
}

.app-header__name {
  font-size: 0.95rem;
  font-weight: 600;
  letter-spacing: -0.02em;
  color: var(--color-ink);
}

.app-header__nav {
  display: none;
  align-items: center;
  gap: 2rem;
}

.app-header__link {
  font-family: var(--font-mono);
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.12em;
  color: var(--color-ink-muted);
  transition: color 0.15s ease;
}

.app-header__link:hover {
  color: var(--color-ink);
}

.app-header__actions {
  display: none;
  align-items: center;
  gap: 0.75rem;
}

.app-header__burger {
  display: inline-flex;
  color: var(--color-ink);
}

@media (min-width: 768px) {
  .app-header__nav,
  .app-header__actions {
    display: flex;
  }

  .app-header__burger {
    display: none;
  }
}
</style>
