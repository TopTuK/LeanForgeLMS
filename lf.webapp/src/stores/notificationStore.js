import { defineStore } from 'pinia';
import { ref } from 'vue';
import { fetchUnreadNotificationCount, markNotificationsSeen } from '@/services/newsService';

export const useNotificationStore = defineStore('notification', () => {
  const unreadCount = ref(0);

  const refreshUnreadCount = async () => {
    try {
      unreadCount.value = await fetchUnreadNotificationCount();
    } catch {
      // The badge is a nicety: a failed probe keeps the last known count instead of breaking the header.
    }
  };

  const markAllSeen = async () => {
    try {
      await markNotificationsSeen();
      unreadCount.value = 0;
    } catch {
      // Leave the badge as is — the next visit to Notifications retries.
    }
  };

  return { unreadCount, refreshUnreadCount, markAllSeen };
});
