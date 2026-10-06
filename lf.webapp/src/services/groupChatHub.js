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

// Several views (or an old and a new instance of the same view during navigation) can want the same
// group. Each caller owns a token; the group stays joined while any token is held. Join/leave for one
// group run strictly in order, so a leave can't overtake an in-flight join and strand the subscription.
const owners = new Map();
const queues = new Map();

function enqueue(groupId, operation) {
  const next = (queues.get(groupId) ?? Promise.resolve()).catch(() => {}).then(operation);
  queues.set(groupId, next);
  return next;
}

// Returns { ready, leave }: `ready` settles once subscribed (rejects if the hub refuses), and `leave`
// releases only this caller's ownership.
export function joinGroupChat(groupId) {
  const token = Symbol(`group-chat:${groupId}`);
  if (!owners.has(groupId)) owners.set(groupId, new Set());
  owners.get(groupId).add(token);

  const ready = enqueue(groupId, async () => {
    // Released before this join got its turn: don't subscribe on behalf of a view that is gone.
    if (!owners.get(groupId)?.has(token) || joinedGroups.has(groupId)) return;
    const conn = await ensureStarted();
    await conn.invoke('JoinGroup', groupId);
    joinedGroups.add(groupId);
  });

  const leave = () => {
    const tokens = owners.get(groupId);
    if (!tokens?.delete(token) || tokens.size > 0) return Promise.resolve();
    owners.delete(groupId);

    return enqueue(groupId, async () => {
      // Re-acquired by another caller while this leave was queued.
      if (owners.has(groupId) || !joinedGroups.delete(groupId)) return;
      if (connection?.state === HubConnectionState.Connected) {
        await connection.invoke('LeaveGroup', groupId);
      }
    });
  };

  return { ready, leave };
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
