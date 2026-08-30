import api from '@/services/api';

export const fetchUsers = ({ page = 1, pageSize = 20, search = '' } = {}) =>
  api.get('/admin/users', { params: { page, pageSize, search: search || undefined } }).then((r) => r.data);

export const updateUserInfo = (id, payload) => api.put(`/admin/users/${id}`, payload).then((r) => r.data);

export const updateUserRole = (id, role) => api.put(`/admin/users/${id}/role`, { role }).then((r) => r.data);

export const deleteUser = (id) => api.delete(`/admin/users/${id}`);

export const fetchAdminCategories = () => api.get('/admin/categories').then((r) => r.data);

export const createCategory = (name) => api.post('/admin/categories', { name }).then((r) => r.data);

export const deleteCategory = (id) => api.delete(`/admin/categories/${id}`);

export const fetchPromoCodes = ({ page = 1, pageSize = 50 } = {}) =>
  api.get('/admin/promo-codes', { params: { page, pageSize } }).then((r) => r.data);

export const createPromoCode = (payload) => api.post('/admin/promo-codes', payload).then((r) => r.data);

export const deactivatePromoCode = (id) => api.post(`/admin/promo-codes/${id}/deactivate`);
