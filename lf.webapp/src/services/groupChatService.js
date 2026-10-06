import api from '@/services/api';

export const fetchGroupMessages = (groupId, { beforeId = null, take = 30 } = {}) =>
  api
    .get(`/groups/${groupId}/messages`, { params: { beforeId: beforeId || undefined, take } })
    .then((r) => r.data);

export const postGroupMessage = (groupId, body) =>
  api.post(`/groups/${groupId}/messages`, { body }).then((r) => r.data);

export const removeGroupMessage = (groupId, messageId) =>
  api.delete(`/groups/${groupId}/messages/${messageId}`).then((r) => r.data);

export const markGroupChatRead = (groupId, lastSeenMessageId) =>
  api.post(`/groups/${groupId}/messages/read`, { lastSeenMessageId });

export const fetchGroupUnreadCounts = () => api.get('/groups/unread').then((r) => r.data);
