<script setup>
import { ref } from 'vue';
import { Menu } from 'lucide-vue-next';
import ThemeToggleButton from '@/components/layout/ThemeToggleButton.vue';
import LocaleToggleButton from '@/components/layout/LocaleToggleButton.vue';
import { Button } from '@/components/ui/button';
import { Sheet } from '@/components/ui/sheet';
import logo from '@/assets/logo.svg';

const navLinks = [
  { href: '#courses', labelKey: 'nav.courses' },
  { href: '#faq', labelKey: 'nav.faq' },
  { href: '#contacts', labelKey: 'nav.contacts' },
];

const mobileOpen = ref(false);

function closeMobile() {
  mobileOpen.value = false;
}
</script>

<template>
  <div class="app-header">
    <div class="container mx-auto flex h-[4.5rem] items-center justify-between px-6">
      <router-link
        :to="{ name: 'Home' }"
        class="app-header__brand"
        @click="closeMobile"
      >
        <img
          :src="logo"
          alt=""
          class="size-8"
        >
        <span class="font-display text-[0.95rem] font-semibold leading-tight tracking-tight text-ink sm:text-base">
          <span class="sm:hidden">{{ $t('common.brand_short') }}</span>
          <span class="hidden sm:inline">{{ $t('common.brand_name') }}</span>
        </span>
      </router-link>

      <nav class="hidden items-center gap-8 md:flex">
        <a
          v-for="link in navLinks"
          :key="link.href"
          :href="link.href"
          class="text-sm font-medium text-ink-muted transition hover:text-ink"
        >
          {{ $t(link.labelKey) }}
        </a>
      </nav>

      <div class="hidden items-center gap-3 md:flex">
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
        class="text-ink md:hidden"
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
  background: color-mix(in srgb, var(--color-surface-950) 88%, transparent);
  border-bottom: 1px solid var(--color-border-subtle);
  backdrop-filter: blur(12px);
}

.app-header__brand {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  min-width: 0;
}
</style>
