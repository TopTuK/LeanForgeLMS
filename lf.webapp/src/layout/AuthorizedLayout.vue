<script setup>
import { onMounted } from 'vue';
import BaseLayout from './BaseLayout.vue';
import AuthorizedHeader from './AuthorizedHeader.vue';
import { useAuthStore } from '@/stores/authStore';
import { useNotificationStore } from '@/stores/notificationStore';
import { useQuestionStore } from '@/stores/questionStore';

const authStore = useAuthStore();
const notificationStore = useNotificationStore();
const questionStore = useQuestionStore();

onMounted(async () => {
  notificationStore.refreshUnreadCount();
  questionStore.refreshUnreadCount();
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
