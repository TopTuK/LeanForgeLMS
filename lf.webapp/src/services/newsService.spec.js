import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import api from '@/services/api';
import {
  fetchPublicNews,
  fetchPublicNewsPost,
  fetchNotifications,
  fetchUnreadNotificationCount,
  markNotificationsSeen,
} from '@/services/newsService';

describe('newsService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.get.mockResolvedValue({ data: {} });
    api.post.mockResolvedValue({ data: undefined });
  });

  it('fetchPublicNews sends paging and never redirects an anonymous visitor to login', async () => {
    api.get.mockResolvedValue({ data: { items: [], totalCount: 0 } });

    const result = await fetchPublicNews({ page: 2, pageSize: 3 });

    expect(api.get).toHaveBeenCalledWith('/news', { params: { page: 2, pageSize: 3 }, skipAuthRedirect: true });
    expect(result).toEqual({ items: [], totalCount: 0 });
  });

  it('fetchPublicNews defaults to the first page', async () => {
    await fetchPublicNews();

    expect(api.get).toHaveBeenCalledWith('/news', { params: { page: 1, pageSize: 10 }, skipAuthRedirect: true });
  });

  it('fetchPublicNewsPost requests a single post anonymously', async () => {
    api.get.mockResolvedValue({ data: { id: 5 } });

    const result = await fetchPublicNewsPost(5);

    expect(api.get).toHaveBeenCalledWith('/news/5', { skipAuthRedirect: true });
    expect(result).toEqual({ id: 5 });
  });

  it('fetchNotifications requests the authenticated feed', async () => {
    await fetchNotifications({ page: 3 });

    expect(api.get).toHaveBeenCalledWith('/notifications', { params: { page: 3, pageSize: 10 } });
  });

  it('fetchUnreadNotificationCount unwraps the count without redirecting', async () => {
    api.get.mockResolvedValue({ data: { count: 4 } });

    const count = await fetchUnreadNotificationCount();

    expect(api.get).toHaveBeenCalledWith('/notifications/unread-count', { skipAuthRedirect: true });
    expect(count).toBe(4);
  });

  it('markNotificationsSeen posts to mark-seen', async () => {
    await markNotificationsSeen();

    expect(api.post).toHaveBeenCalledWith('/notifications/mark-seen');
  });
});
