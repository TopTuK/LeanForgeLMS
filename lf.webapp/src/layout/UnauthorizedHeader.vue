<script setup>
import { ref } from 'vue';
import ThemeToggleButton from '@/components/layout/ThemeToggleButton.vue';
import LocaleToggleButton from '@/components/layout/LocaleToggleButton.vue';
import logo from '@/assets/logo.svg';

const navLinks = [
    { href: '#courses', labelKey: 'nav.courses' },
    { href: '#faq', labelKey: 'nav.faq' },
    { href: '#contacts', labelKey: 'nav.contacts' },
];

const isMobileMenuOpen = ref(false);

function closeMobileMenu() {
    isMobileMenuOpen.value = false;
}
</script>

<template>
  <div class="app-header">
    <div class="container mx-auto px-6 relative z-10 flex items-center justify-between h-[4.5rem]">
      <router-link
        to="/"
        class="app-header__brand"
        @click="closeMobileMenu"
      >
        <img
          :src="logo"
          alt=""
          class="w-8 h-8"
        >
        <span class="font-bold text-ink text-lg tracking-tight">
          {{ $t('common.brand_name') }}
        </span>
      </router-link>

      <nav class="hidden md:flex items-center gap-8">
        <a
          v-for="link in navLinks"
          :key="link.href"
          :href="link.href"
          class="text-sm font-medium text-ink-muted hover:text-ink transition"
        >
          {{ $t(link.labelKey) }}
        </a>
      </nav>

      <div class="hidden md:flex items-center gap-4">
        <ThemeToggleButton />
        <LocaleToggleButton />
        <router-link
          to="/login"
          class="header-signin"
        >
          {{ $t('nav.login') }}
          <span aria-hidden="true">→</span>
        </router-link>
      </div>

      <button
        type="button"
        class="md:hidden text-ink"
        :aria-label="$t('nav.menu_toggle')"
        @click="isMobileMenuOpen = !isMobileMenuOpen"
      >
        <svg
          v-if="!isMobileMenuOpen"
          width="24"
          height="24"
          viewBox="0 0 24 24"
          fill="none"
        >
          <path
            d="M4 6H20M4 12H20M4 18H20"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
          />
        </svg>
        <svg
          v-else
          width="24"
          height="24"
          viewBox="0 0 24 24"
          fill="none"
        >
          <path
            d="M6 6L18 18M6 18L18 6"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
          />
        </svg>
      </button>
    </div>

    <nav
      v-if="isMobileMenuOpen"
      class="app-header__nav--mobile md:hidden relative z-10 border-t border-border-subtle"
    >
      <a
        v-for="link in navLinks"
        :key="link.href"
        :href="link.href"
        class="block px-6 py-3 text-sm font-medium text-ink-muted hover:text-ink transition"
        @click="closeMobileMenu"
      >
        {{ $t(link.labelKey) }}
      </a>
      <div class="flex items-center justify-between px-6 py-3 border-t border-border-subtle">
        <div class="flex items-center gap-4">
          <ThemeToggleButton />
          <LocaleToggleButton />
        </div>
        <router-link
          to="/login"
          class="header-signin"
          @click="closeMobileMenu"
        >
          {{ $t('nav.login') }}
          <span aria-hidden="true">→</span>
        </router-link>
      </div>
    </nav>
  </div>
</template>

<style scoped>
.app-header {
    position: relative;
    width: 100%;
    height: 100%;
    background-color: var(--color-surface-950);
    border-bottom: 1px solid var(--color-border-subtle);
}

.app-header__brand {
    display: flex;
    align-items: center;
    gap: 0.625rem;
}

.app-header__nav--mobile {
    background: var(--color-surface-950);
}

.header-signin {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.65rem 1.1rem;
    color: #ffffff;
    background: var(--color-accent-coral);
    border: 1px solid var(--color-accent-coral);
    border-radius: 999px;
    font-size: 0.875rem;
    font-weight: 700;
    line-height: 1;
    box-shadow: 0 6px 16px rgba(236, 104, 60, 0.2);
    transition: transform 0.15s ease, background-color 0.15s ease, box-shadow 0.15s ease;
}

.header-signin:hover {
    color: #ffffff;
    background: var(--color-accent-coral-dark);
    transform: translateY(-1px);
    box-shadow: 0 9px 22px rgba(236, 104, 60, 0.28);
}

.header-signin span {
    transition: transform 0.15s ease;
}

.header-signin:hover span {
    transform: translateX(2px);
}
</style>
