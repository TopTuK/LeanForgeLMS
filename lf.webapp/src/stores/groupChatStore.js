import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { fetchGroupUnreadCounts } from '@/services/groupChatService';

export const useGroupChatStore = defineStore('groupChat', () => {
  const unreadByGroup = ref({});

  const totalUnread = computed(() => Object.values(unreadByGroup.value).reduce((sum, count) => sum + count, 0));

  const refreshUnreadCounts = async () => {
    try {
      const counts = await fetchGroupUnreadCounts();
      unreadByGroup.value = Object.fromEntries(counts.map((c) => [c.groupId, c.count]));
    } catch {
      // The badge is a nicety: a failed probe keeps the last known counts instead of breaking the header.
    }
  };

  const clearGroup = (groupId) => {
    if (!unreadByGroup.value[groupId]) return;
    const next = { ...unreadByGroup.value };
    delete next[groupId];
    unreadByGroup.value = next;
  };

  return { unreadByGroup, totalUnread, refreshUnreadCounts, clearGroup };
});
