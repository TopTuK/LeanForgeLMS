<script setup>
import { computed } from 'vue';
import { availableLocales, i18n } from '@/i18n/index.js';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();

const currentLocale = computed(() => i18n.global.locale.value);
const nextLocale = computed(() => availableLocales.find((locale) => locale.code !== currentLocale.value));

function toggleLocale() {
    if (nextLocale.value) authStore.changeLocale(nextLocale.value.code);
}
</script>

<template>
  <button
    type="button"
    class="text-sm font-medium text-ink-muted hover:text-ink transition"
    @click="toggleLocale"
  >
    {{ nextLocale?.code.toUpperCase() }}
  </button>
</template>
