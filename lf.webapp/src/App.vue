<script setup>
import UnauthorizedLayout from '@/layout/UnauthorizedLayout.vue';
import AuthorizedLayout from '@/layout/AuthorizedLayout.vue';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
// Resolve the session from the HttpOnly cookie before choosing a layout, so an
// authenticated deep-link doesn't flash the logged-out shell first.
authStore.ensureInitialized();
</script>

<template>
  <template v-if="authStore.initialized">
    <UnauthorizedLayout v-if="!authStore.isAuthenticated" />
    <AuthorizedLayout v-else />
  </template>
</template>
