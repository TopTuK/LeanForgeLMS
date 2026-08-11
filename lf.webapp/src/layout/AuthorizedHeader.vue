<script setup>
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import { availableLocales, setLocale, i18n } from '@/i18n/index.js';
import { currentTheme, toggleTheme } from '@/theme/index.js';
import { useAuthStore } from '@/stores/authStore';
import logo from '@/assets/logo.svg';

const router = useRouter();
const authStore = useAuthStore();

const isMobileMenuOpen = ref(false);

const currentLocale = computed(() => i18n.global.locale.value);
const nextLocale = computed(() => availableLocales.find((locale) => locale.code !== currentLocale.value));

function toggleLocale() {
    if (nextLocale.value) setLocale(nextLocale.value.code);
}

function closeMobileMenu() {
    isMobileMenuOpen.value = false;
}

async function onLogout() {
    closeMobileMenu();
    await authStore.logout();
    router.push({ name: 'Home' });
}
</script>

<template>
  <div class="app-header">
    <div class="container mx-auto px-6 relative z-10 flex items-center justify-between h-[4.5rem]">
      <router-link
        :to="{ name: 'Courses' }"
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
        <router-link
          :to="{ name: 'Courses' }"
          class="text-sm font-medium text-ink-muted hover:text-ink transition"
        >
          {{ $t('nav.courses') }}
        </router-link>
        <router-link
          v-if="authStore.isAdmin"
          :to="{ name: 'AdminUsers' }"
          class="text-sm font-medium text-ink-muted hover:text-ink transition"
        >
          {{ $t('nav.administration') }}
        </router-link>
      </nav>

      <div class="hidden md:flex items-center gap-4">
        <button
          type="button"
          class="theme-toggle"
          :aria-label="$t(currentTheme === 'dark' ? 'nav.theme_light' : 'nav.theme_dark')"
          :title="$t(currentTheme === 'dark' ? 'nav.theme_light' : 'nav.theme_dark')"
          @click="toggleTheme"
        >
          <svg
            v-if="currentTheme === 'light'"
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            aria-hidden="true"
          >
            <path
              d="M12 3V1M12 23v-2M3 12H1M23 12h-2M5.64 5.64 4.22 4.22M19.78 19.78l-1.42-1.42M18.36 5.64l1.42-1.42M4.22 19.78l1.42-1.42"
              stroke="currentColor"
              stroke-width="1.7"
              stroke-linecap="round"
            />
            <circle
              cx="12"
              cy="12"
              r="4"
              stroke="currentColor"
              stroke-width="1.7"
            />
          </svg>
          <svg
            v-else
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            aria-hidden="true"
          >
            <path
              d="M20.5 15.2A8.8 8.8 0 0 1 8.8 3.5 9 9 0 1 0 20.5 15.2Z"
              stroke="currentColor"
              stroke-width="1.7"
              stroke-linejoin="round"
            />
          </svg>
        </button>
        <button
          type="button"
          class="text-sm font-medium text-ink-muted hover:text-ink transition"
          @click="toggleLocale"
        >
          {{ nextLocale?.code.toUpperCase() }}
        </button>
        <router-link
          :to="{ name: 'Profile' }"
          class="app-header__profile-link"
        >
          <img
            v-if="authStore.avatarUrl"
            :src="authStore.avatarUrl"
            alt=""
            class="app-header__avatar"
          >
          {{ authStore.user?.firstName ?? $t('nav.profile') }}
        </router-link>
        <button
          type="button"
          class="header-signin"
          @click="onLogout"
        >
          {{ $t('nav.logout') }}
        </button>
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
      <router-link
        :to="{ name: 'Courses' }"
        class="block px-6 py-3 text-sm font-medium text-ink-muted hover:text-ink transition"
        @click="closeMobileMenu"
      >
        {{ $t('nav.courses') }}
      </router-link>
      <router-link
        :to="{ name: 'Profile' }"
        class="block px-6 py-3 text-sm font-medium text-ink-muted hover:text-ink transition"
        @click="closeMobileMenu"
      >
        {{ authStore.user?.firstName ?? $t('nav.profile') }}
      </router-link>
      <router-link
        v-if="authStore.isAdmin"
        :to="{ name: 'AdminUsers' }"
        class="block px-6 py-3 text-sm font-medium text-ink-muted hover:text-ink transition"
        @click="closeMobileMenu"
      >
        {{ $t('nav.administration') }}
      </router-link>
      <div class="flex items-center justify-between px-6 py-3 border-t border-border-subtle">
        <div class="flex items-center gap-4">
          <button
            type="button"
            class="theme-toggle"
            :aria-label="$t(currentTheme === 'dark' ? 'nav.theme_light' : 'nav.theme_dark')"
            @click="toggleTheme"
          >
            <svg
              v-if="currentTheme === 'light'"
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              aria-hidden="true"
            >
              <circle
                cx="12"
                cy="12"
                r="4"
                stroke="currentColor"
                stroke-width="1.7"
              />
              <path
                d="M12 3V1M12 23v-2M3 12H1M23 12h-2"
                stroke="currentColor"
                stroke-width="1.7"
                stroke-linecap="round"
              />
            </svg>
            <svg
              v-else
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              aria-hidden="true"
            >
              <path
                d="M20.5 15.2A8.8 8.8 0 0 1 8.8 3.5 9 9 0 1 0 20.5 15.2Z"
                stroke="currentColor"
                stroke-width="1.7"
              />
            </svg>
          </button>
          <button
            type="button"
            class="text-sm font-medium text-ink-muted hover:text-ink transition"
            @click="toggleLocale"
          >
            {{ nextLocale?.code.toUpperCase() }}
          </button>
        </div>
        <button
          type="button"
          class="header-signin"
          @click="onLogout"
        >
          {{ $t('nav.logout') }}
        </button>
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

.app-header__profile-link {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--color-ink-muted);
    transition: color 0.15s ease;
}

.app-header__profile-link:hover {
    color: var(--color-ink);
}

.app-header__avatar {
    width: 1.75rem;
    height: 1.75rem;
    border-radius: 999px;
    object-fit: cover;
}

.theme-toggle {
    display: inline-flex;
    width: 2.25rem;
    height: 2.25rem;
    align-items: center;
    justify-content: center;
    color: var(--color-ink-muted);
    background: var(--color-surface-900);
    border: 1px solid var(--color-border-subtle);
    border-radius: 999px;
    transition: color 0.15s ease, border-color 0.15s ease, background-color 0.15s ease;
}

.theme-toggle:hover {
    color: var(--color-ink);
    border-color: var(--color-ink-faint);
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
</style>
