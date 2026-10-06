import { describe, it, expect, beforeEach, vi } from 'vitest';

const connection = {
  on: vi.fn(),
  off: vi.fn(),
  onreconnected: vi.fn(),
  start: vi.fn(),
  invoke: vi.fn(),
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

const hubCalls = () => connection.invoke.mock.calls.map(([method, id]) => `${method}:${id}`);

describe('groupChatHub', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    connection.state = 'Disconnected';
    connection.start.mockImplementation(async () => {
      connection.state = 'Connected';
    });
    connection.invoke.mockResolvedValue(undefined);
  });

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

  it('starts the connection, joins, and leaves when the only owner releases', async () => {
    const chat = joinGroupChat(3);
    await chat.ready;
    await chat.leave();

    expect(connection.start).toHaveBeenCalled();
    expect(hubCalls()).toEqual(['JoinGroup:3', 'LeaveGroup:3']);
  });

  it('keeps the group joined while another owner holds it, and ignores repeated releases', async () => {
    const older = joinGroupChat(4);
    const newer = joinGroupChat(4);
    await Promise.all([older.ready, newer.ready]);

    await older.leave();
    await older.leave();
    expect(hubCalls()).toEqual(['JoinGroup:4']);

    await newer.leave();
    expect(hubCalls()).toEqual(['JoinGroup:4', 'LeaveGroup:4']);
  });

  it('never subscribes for a caller that released before its join ran', async () => {
    const chat = joinGroupChat(5);
    const left = chat.leave();
    await chat.ready;
    await left;

    expect(hubCalls()).toEqual([]);
  });

  it('runs a leave only after an in-flight join completes', async () => {
    let finishJoin;
    connection.invoke.mockImplementationOnce(() => new Promise((resolve) => { finishJoin = resolve; }));

    const chat = joinGroupChat(6);
    await vi.waitFor(() => expect(hubCalls()).toEqual(['JoinGroup:6']));
    const left = chat.leave();

    finishJoin();
    await left;
    expect(hubCalls()).toEqual(['JoinGroup:6', 'LeaveGroup:6']);
  });
});
