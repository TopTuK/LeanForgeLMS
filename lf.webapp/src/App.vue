<script setup>
import UnauthorizedLayout from '@/layout/UnauthorizedLayout.vue';
import AuthorizedLayout from '@/layout/AuthorizedLayout.vue';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
</script>

<template>
  <UnauthorizedLayout v-if="!authStore.isAuthenticated" />
  <AuthorizedLayout v-else />
</template>
