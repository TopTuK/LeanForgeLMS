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

export const fetchPlatformSettings = () => api.get('/admin/platform-settings').then((r) => r.data);

export const updateStudentEnrollment = (enabled) =>
  api.put('/admin/platform-settings/student-enrollment', { enabled }).then((r) => r.data);

export const fetchPayments = ({ page = 1, pageSize = 20, from, to } = {}) =>
  api
    .get('/admin/payments', { params: { page, pageSize, from: from || undefined, to: to || undefined } })
    .then((r) => r.data);

export const downloadPaymentsCsv = async ({ from, to } = {}) => {
  const response = await api.get('/admin/payments/report.csv', {
    params: { from: from || undefined, to: to || undefined },
    responseType: 'blob',
  });

  const disposition = response.headers['content-disposition'] ?? '';
  const match = /filename="?([^"]+)"?/.exec(disposition);
  const fileName = match?.[1] ?? 'course-payments.csv';

  const objectUrl = URL.createObjectURL(response.data);
  try {
    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
  } finally {
    URL.revokeObjectURL(objectUrl);
  }
};
