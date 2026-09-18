import { defineStore } from 'pinia';
import { ref } from 'vue';
import { fetchUnreadQuestionCount } from '@/services/questionService';

export const useQuestionStore = defineStore('question', () => {
  const unreadCount = ref(0);

  const refreshUnreadCount = async () => {
    try {
      unreadCount.value = await fetchUnreadQuestionCount();
    } catch {
      // The badge is a nicety: a failed probe keeps the last known count instead of breaking the header.
    }
  };

  return { unreadCount, refreshUnreadCount };
});
