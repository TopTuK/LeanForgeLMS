import { describe, it, expect, beforeEach, vi } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useNotificationStore } from '@/stores/notificationStore';
import { fetchUnreadNotificationCount, markNotificationsSeen } from '@/services/newsService';

vi.mock('@/services/newsService', () => ({
  fetchUnreadNotificationCount: vi.fn(),
  markNotificationsSeen: vi.fn(),
}));

describe('useNotificationStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('starts with no unread notifications', () => {
    expect(useNotificationStore().unreadCount).toBe(0);
  });

  it('refreshUnreadCount stores the server count', async () => {
    fetchUnreadNotificationCount.mockResolvedValue(3);
    const store = useNotificationStore();

    await store.refreshUnreadCount();

    expect(store.unreadCount).toBe(3);
  });

  it('refreshUnreadCount keeps the last known count when the probe fails', async () => {
    fetchUnreadNotificationCount.mockResolvedValueOnce(2).mockRejectedValueOnce(new Error('offline'));
    const store = useNotificationStore();

    await store.refreshUnreadCount();
    await store.refreshUnreadCount();

    expect(store.unreadCount).toBe(2);
  });

  it('markAllSeen clears the badge once the server confirms', async () => {
    fetchUnreadNotificationCount.mockResolvedValue(5);
    markNotificationsSeen.mockResolvedValue(undefined);
    const store = useNotificationStore();
    await store.refreshUnreadCount();

    await store.markAllSeen();

    expect(markNotificationsSeen).toHaveBeenCalledTimes(1);
    expect(store.unreadCount).toBe(0);
  });

  it('markAllSeen keeps the badge when the request fails', async () => {
    fetchUnreadNotificationCount.mockResolvedValue(5);
    markNotificationsSeen.mockRejectedValue(new Error('offline'));
    const store = useNotificationStore();
    await store.refreshUnreadCount();

    await store.markAllSeen();

    expect(store.unreadCount).toBe(5);
  });
});
