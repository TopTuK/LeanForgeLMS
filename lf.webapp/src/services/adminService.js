import api from '@/services/api';

export const fetchUsers = ({ page = 1, pageSize = 20, search = '' } = {}) =>
  api.get('/admin/users', { params: { page, pageSize, search: search || undefined } }).then((r) => r.data);

export const updateUserInfo = (id, payload) => api.put(`/admin/users/${id}`, payload).then((r) => r.data);

export const updateUserRole = (id, role) => api.put(`/admin/users/${id}/role`, { role }).then((r) => r.data);

export const deleteUser = (id) => api.delete(`/admin/users/${id}`);
