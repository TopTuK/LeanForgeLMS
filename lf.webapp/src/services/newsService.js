import api from '@/services/api';

// Public news is readable while signed out, so these calls must never bounce a visitor to /login.
export const fetchPublicNews = ({ page = 1, pageSize = 10 } = {}) =>
  api.get('/news', { params: { page, pageSize }, skipAuthRedirect: true }).then((r) => r.data);

export const fetchPublicNewsPost = (id) =>
  api.get(`/news/${id}`, { skipAuthRedirect: true }).then((r) => r.data);

export const fetchNotifications = ({ page = 1, pageSize = 10 } = {}) =>
  api.get('/notifications', { params: { page, pageSize } }).then((r) => r.data);

// A background probe for the header badge — an expired session is handled by the pages the
// user actually opens, not by this call yanking them to /login.
export const fetchUnreadNotificationCount = () =>
  api.get('/notifications/unread-count', { skipAuthRedirect: true }).then((r) => r.data.count);

export const markNotificationsSeen = () => api.post('/notifications/mark-seen');
