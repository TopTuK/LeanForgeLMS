<script setup>
import { onMounted } from 'vue';
import BaseLayout from './BaseLayout.vue';
import AuthorizedHeader from './AuthorizedHeader.vue';
import { useAuthStore } from '@/stores/authStore';
import { useNotificationStore } from '@/stores/notificationStore';

const authStore = useAuthStore();
const notificationStore = useNotificationStore();

onMounted(async () => {
  notificationStore.refreshUnreadCount();
  await authStore.fetchUser();
  authStore.refreshAvatar();
});
</script>

<template>
  <BaseLayout>
    <template #header>
      <AuthorizedHeader />
    </template>

    <template #default>
      <router-view />
    </template>
  </BaseLayout>
</template>
