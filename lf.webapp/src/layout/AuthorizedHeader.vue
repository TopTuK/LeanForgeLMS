<script setup>
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import { Menu } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/authStore';
import ThemeToggleButton from '@/components/layout/ThemeToggleButton.vue';
import LocaleToggleButton from '@/components/layout/LocaleToggleButton.vue';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuItem,
  DropdownMenuSeparator,
} from '@/components/ui/dropdown-menu';
import { Sheet } from '@/components/ui/sheet';
import logo from '@/assets/logo.svg';

const router = useRouter();
const authStore = useAuthStore();
const mobileOpen = ref(false);

const displayName = computed(() => authStore.user?.firstName ?? '');

function closeMobile() {
  mobileOpen.value = false;
}

async function onLogout() {
  closeMobile();
  await authStore.logout();
  router.push({ name: 'Home' });
}

function go(name) {
  closeMobile();
  router.push({ name });
}
</script>

<template>
  <div class="app-header">
    <div class="container mx-auto flex h-[4.5rem] items-center justify-between px-6">
      <router-link
        :to="{ name: 'Courses' }"
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
        <router-link
          :to="{ name: 'Courses' }"
          class="text-sm font-medium text-ink-muted transition hover:text-ink"
        >
          {{ $t('nav.courses') }}
        </router-link>
        <router-link
          v-if="authStore.isAdmin"
          :to="{ name: 'AdminUsers' }"
          class="text-sm font-medium text-ink-muted transition hover:text-ink"
        >
          {{ $t('nav.administration') }}
        </router-link>
      </nav>

      <div class="hidden items-center gap-3 md:flex">
        <ThemeToggleButton />
        <LocaleToggleButton />
        <DropdownMenu>
          <template #trigger>
            <button
              type="button"
              class="app-header__account"
            >
              <img
                v-if="authStore.avatarUrl"
                :src="authStore.avatarUrl"
                alt=""
                class="app-header__avatar"
              >
              <span>{{ displayName || $t('nav.profile') }}</span>
            </button>
          </template>
          <DropdownMenuItem @click="router.push({ name: 'Profile' })">
            {{ $t('nav.profile') }}
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem
            destructive
            @click="onLogout"
          >
            {{ $t('nav.logout') }}
          </DropdownMenuItem>
        </DropdownMenu>
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
        <button
          type="button"
          class="rounded-md px-2 py-2 text-left text-sm font-medium text-ink-muted hover:bg-surface-900 hover:text-ink"
          @click="go('Courses')"
        >
          {{ $t('nav.courses') }}
        </button>
        <button
          type="button"
          class="rounded-md px-2 py-2 text-left text-sm font-medium text-ink-muted hover:bg-surface-900 hover:text-ink"
          @click="go('Profile')"
        >
          {{ displayName || $t('nav.profile') }}
        </button>
        <button
          v-if="authStore.isAdmin"
          type="button"
          class="rounded-md px-2 py-2 text-left text-sm font-medium text-ink-muted hover:bg-surface-900 hover:text-ink"
          @click="go('AdminUsers')"
        >
          {{ $t('nav.administration') }}
        </button>
      </nav>
      <div class="mt-6 flex items-center justify-between">
        <div class="flex items-center gap-3">
          <ThemeToggleButton />
          <LocaleToggleButton />
        </div>
        <Button
          variant="outline"
          size="sm"
          @click="onLogout"
        >
          {{ $t('nav.logout') }}
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

.app-header__account {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.35rem 0.65rem 0.35rem 0.35rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 999px;
  background: var(--color-card);
  color: var(--color-ink);
  font-size: 0.875rem;
  font-weight: 500;
}

.app-header__avatar {
  width: 1.75rem;
  height: 1.75rem;
  border-radius: 999px;
  object-fit: cover;
}
</style>
