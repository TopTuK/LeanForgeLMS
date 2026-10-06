<script setup>
import { onMounted } from 'vue';
import BaseLayout from './BaseLayout.vue';
import AuthorizedHeader from './AuthorizedHeader.vue';
import { useAuthStore } from '@/stores/authStore';
import { useNotificationStore } from '@/stores/notificationStore';
import { useQuestionStore } from '@/stores/questionStore';
import { useGroupChatStore } from '@/stores/groupChatStore';

const authStore = useAuthStore();
const notificationStore = useNotificationStore();
const questionStore = useQuestionStore();
const groupChatStore = useGroupChatStore();

onMounted(async () => {
  notificationStore.refreshUnreadCount();
  questionStore.refreshUnreadCount();
  groupChatStore.refreshUnreadCounts();
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
