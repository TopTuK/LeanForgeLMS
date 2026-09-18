import { describe, it, expect, beforeEach, vi } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useQuestionStore } from '@/stores/questionStore';
import { fetchUnreadQuestionCount } from '@/services/questionService';

vi.mock('@/services/questionService', () => ({
  fetchUnreadQuestionCount: vi.fn(),
}));

describe('useQuestionStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('starts with no unread questions', () => {
    expect(useQuestionStore().unreadCount).toBe(0);
  });

  it('refreshUnreadCount stores the server count', async () => {
    fetchUnreadQuestionCount.mockResolvedValue(3);
    const store = useQuestionStore();

    await store.refreshUnreadCount();

    expect(store.unreadCount).toBe(3);
  });

  it('refreshUnreadCount keeps the last known count when the probe fails', async () => {
    fetchUnreadQuestionCount.mockResolvedValueOnce(2).mockRejectedValueOnce(new Error('offline'));
    const store = useQuestionStore();

    await store.refreshUnreadCount();
    await store.refreshUnreadCount();

    expect(store.unreadCount).toBe(2);
  });
});
