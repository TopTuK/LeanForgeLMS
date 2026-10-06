import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

// One shared connection for the whole SPA. It authenticates with the same HttpOnly session cookie as
// the REST API (same-origin handshake), so no token plumbing is needed here.
const HUB_PATH = '/hubs/group-chat';

let connection = null;
let starting = null;
const joinedGroups = new Set();

function getConnection() {
  if (connection) return connection;

  connection = new HubConnectionBuilder()
    .withUrl(HUB_PATH, { withCredentials: true })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();

  // Group subscriptions are per connection id, which changes on reconnect.
  connection.onreconnected(() => Promise.all([...joinedGroups].map((id) => connection.invoke('JoinGroup', id))));

  return connection;
}

async function ensureStarted() {
  const conn = getConnection();
  if (conn.state === HubConnectionState.Connected) return conn;
  if (!starting) {
    starting = conn.start().finally(() => {
      starting = null;
    });
  }
  await starting;
  return conn;
}

export async function joinGroupChat(groupId) {
  const conn = await ensureStarted();
  await conn.invoke('JoinGroup', groupId);
  joinedGroups.add(groupId);
}

export async function leaveGroupChat(groupId) {
  joinedGroups.delete(groupId);
  if (connection?.state === HubConnectionState.Connected) {
    await connection.invoke('LeaveGroup', groupId);
  }
}

// Returns an unsubscribe function. The handler's return value is swallowed: the SignalR client treats
// any returned value (an async handler's Promise included) as a reply to a server-to-client call and
// logs an error for these fire-and-forget events.
export function onGroupChatEvent(eventName, handler) {
  const conn = getConnection();
  const listener = (...args) => {
    handler(...args);
  };
  conn.on(eventName, listener);
  return () => conn.off(eventName, listener);
}

export const GroupChatEvents = Object.freeze({
  messagePosted: 'messagePosted',
  messageDeleted: 'messageDeleted',
});
