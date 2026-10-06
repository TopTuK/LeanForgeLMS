import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import api from '@/services/api';
import {
  fetchGroupMessages,
  fetchGroupUnreadCounts,
  markGroupChatRead,
  postGroupMessage,
  removeGroupMessage,
} from '@/services/groupChatService';

describe('groupChatService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    for (const m of Object.values(api)) m.mockResolvedValue({ data: 'RESULT' });
  });

  it.each([
    ['postGroupMessage', () => postGroupMessage(3, 'hi'), 'post', ['/groups/3/messages', { body: 'hi' }]],
    ['removeGroupMessage', () => removeGroupMessage(3, 9), 'delete', ['/groups/3/messages/9']],
    ['fetchGroupUnreadCounts', () => fetchGroupUnreadCounts(), 'get', ['/groups/unread']],
  ])('%s calls the right endpoint and unwraps data', async (_name, call, method, args) => {
    await expect(call()).resolves.toBe('RESULT');
    expect(api[method]).toHaveBeenCalledWith(...args);
  });

  it('fetchGroupMessages pages backwards from beforeId', async () => {
    await fetchGroupMessages(3);
    expect(api.get).toHaveBeenLastCalledWith('/groups/3/messages', { params: { beforeId: undefined, take: 30 } });

    await fetchGroupMessages(3, { beforeId: 40, take: 10 });
    expect(api.get).toHaveBeenLastCalledWith('/groups/3/messages', { params: { beforeId: 40, take: 10 } });
  });

  it('markGroupChatRead posts the last seen id', async () => {
    await markGroupChatRead(3, 41);
    expect(api.post).toHaveBeenCalledWith('/groups/3/messages/read', { lastSeenMessageId: 41 });
  });
});
