<script setup>
import { computed } from 'vue';
import { availableLocales, setLocale, i18n } from '@/i18n/index.js';

const currentLocale = computed(() => i18n.global.locale.value);
const nextLocale = computed(() => availableLocales.find((locale) => locale.code !== currentLocale.value));

function toggleLocale() {
    if (nextLocale.value) setLocale(nextLocale.value.code);
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
