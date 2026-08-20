<script setup>
import { useI18n } from 'vue-i18n';
import { currentTheme, toggleTheme } from '@/theme/index.js';

const { t } = useI18n();
</script>

<template>
  <button
    type="button"
    class="theme-toggle"
    :aria-label="t(currentTheme === 'dark' ? 'nav.theme_light' : 'nav.theme_dark')"
    :title="t(currentTheme === 'dark' ? 'nav.theme_light' : 'nav.theme_dark')"
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
</template>

<style scoped>
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
</style>
