import { describe, it, expect, vi } from 'vitest';

const connection = {
  on: vi.fn(),
  off: vi.fn(),
  onreconnected: vi.fn(),
  start: vi.fn().mockResolvedValue(undefined),
  invoke: vi.fn().mockResolvedValue(undefined),
  state: 'Disconnected',
};

vi.mock('@microsoft/signalr', () => {
  class HubConnectionBuilder {
    withUrl() { return this; }
    withAutomaticReconnect() { return this; }
    configureLogging() { return this; }
    build() { return connection; }
  }
  return {
    HubConnectionBuilder,
    HubConnectionState: { Connected: 'Connected' },
    LogLevel: { Warning: 3 },
  };
});

import { joinGroupChat, onGroupChatEvent } from '@/services/groupChatHub';

describe('groupChatHub', () => {
  // SignalR logs an error whenever a client handler returns a value, which an async handler always does.
  it('registers a listener that swallows the handler result, and unsubscribes it', () => {
    const handler = vi.fn(async () => 'ignored');

    const off = onGroupChatEvent('messagePosted', handler);
    const listener = connection.on.mock.calls[0][1];

    expect(listener({ id: 1 })).toBeUndefined();
    expect(handler).toHaveBeenCalledWith({ id: 1 });

    off();
    expect(connection.off).toHaveBeenCalledWith('messagePosted', listener);
  });

  it('starts the connection before joining a group', async () => {
    await joinGroupChat(3);

    expect(connection.start).toHaveBeenCalled();
    expect(connection.invoke).toHaveBeenCalledWith('JoinGroup', 3);
  });
});
