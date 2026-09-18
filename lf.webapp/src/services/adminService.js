import api from '@/services/api';

export const fetchUsers = ({ page = 1, pageSize = 20, search = '' } = {}) =>
  api.get('/admin/users', { params: { page, pageSize, search: search || undefined } }).then((r) => r.data);

export const updateUserInfo = (id, payload) => api.put(`/admin/users/${id}`, payload).then((r) => r.data);

export const updateUserRole = (id, role) => api.put(`/admin/users/${id}/role`, { role }).then((r) => r.data);

export const deleteUser = (id) => api.delete(`/admin/users/${id}`);

export const fetchAdminCourses = ({ page = 1, pageSize = 20 } = {}) =>
  api.get('/admin/courses', { params: { page, pageSize } }).then((r) => r.data);

export const fetchCourseEnrollments = (courseId, { page = 1, pageSize = 20 } = {}) =>
  api.get(`/admin/courses/${courseId}/enrollments`, { params: { page, pageSize } }).then((r) => r.data);

export const enrollStudent = (courseId, userId) =>
  api.post(`/admin/courses/${courseId}/enrollments`, { userId }).then((r) => r.data);

export const removeEnrollment = (courseId, enrollmentId) =>
  api.delete(`/admin/courses/${courseId}/enrollments/${enrollmentId}`).then((r) => r.data);

// force acknowledges that paid students lose access without a refund; without it the API
// refuses a course anyone has paid for.
export const unpublishCourse = (courseId) =>
  api.post(`/admin/courses/${courseId}/unpublish`).then((r) => r.data);

export const deleteCourse = (courseId, { force = false } = {}) =>
  api.delete(`/admin/courses/${courseId}`, { params: { force: force || undefined } }).then((r) => r.data);

export const fetchAdminCategories = () => api.get('/admin/categories').then((r) => r.data);

export const createCategory = (name) => api.post('/admin/categories', { name }).then((r) => r.data);

export const deleteCategory = (id) => api.delete(`/admin/categories/${id}`);

export const fetchPromoCodes = ({ page = 1, pageSize = 50 } = {}) =>
  api.get('/admin/promo-codes', { params: { page, pageSize } }).then((r) => r.data);

export const createPromoCode = (payload) => api.post('/admin/promo-codes', payload).then((r) => r.data);

export const deactivatePromoCode = (id) => api.post(`/admin/promo-codes/${id}/deactivate`);

export const fetchPayments = ({ page = 1, pageSize = 20, from, to } = {}) =>
  api
    .get('/admin/payments', { params: { page, pageSize, from: from || undefined, to: to || undefined } })
    .then((r) => r.data);

export const fetchAdminNews = ({ page = 1, pageSize = 20 } = {}) =>
  api.get('/admin/news', { params: { page, pageSize } }).then((r) => r.data);

export const fetchAdminNewsPost = (id) => api.get(`/admin/news/${id}`).then((r) => r.data);

export const createNewsPost = (payload) => api.post('/admin/news', payload).then((r) => r.data);

export const updateNewsPost = (id, payload) => api.put(`/admin/news/${id}`, payload).then((r) => r.data);

export const deleteNewsPost = (id) => api.delete(`/admin/news/${id}`);

export const uploadNewsImage = (file) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post('/admin/news/images', formData).then((r) => r.data);
};

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

export const fetchAdminQuestions = ({ courseId = null, status = null, search = null, page = 1, pageSize = 20 } = {}) =>
  api
    .get('/admin/questions', {
      params: {
        courseId: courseId || undefined,
        status: status || undefined,
        search: search || undefined,
        page,
        pageSize,
      },
    })
    .then((r) => r.data);

export const fetchAdminQuestionThread = (id) => api.get(`/admin/questions/${id}`).then((r) => r.data);

export const postAdminQuestionMessage = (id, body) =>
  api.post(`/admin/questions/${id}/messages`, { body }).then((r) => r.data);

export const deleteAdminQuestionMessage = (id, messageId) =>
  api.delete(`/admin/questions/${id}/messages/${messageId}`).then((r) => r.data);

export const deleteAdminQuestion = (id) => api.delete(`/admin/questions/${id}`);
