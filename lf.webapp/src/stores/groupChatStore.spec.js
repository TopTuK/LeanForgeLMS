import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useGroupChatStore } from '@/stores/groupChatStore';
import { fetchGroupUnreadCounts } from '@/services/groupChatService';

vi.mock('@/services/groupChatService', () => ({
  fetchGroupUnreadCounts: vi.fn(),
}));

describe('groupChatStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('totals unread counts across groups and clears one group', async () => {
    fetchGroupUnreadCounts.mockResolvedValue([{ groupId: 1, count: 2 }, { groupId: 5, count: 3 }]);
    const store = useGroupChatStore();

    await store.refreshUnreadCounts();
    expect(store.totalUnread).toBe(5);

    store.clearGroup(1);
    expect(store.totalUnread).toBe(3);
    expect(store.unreadByGroup[1]).toBeUndefined();
  });

  it('keeps the last known counts when the probe fails', async () => {
    fetchGroupUnreadCounts.mockResolvedValueOnce([{ groupId: 1, count: 2 }]).mockRejectedValueOnce(new Error('down'));
    const store = useGroupChatStore();

    await store.refreshUnreadCounts();
    await store.refreshUnreadCounts();

    expect(store.totalUnread).toBe(2);
  });
});
