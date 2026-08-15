import api from '@/services/api';

export const fetchCatalog = ({ page = 1, pageSize = 20 } = {}) =>
  api.get('/enrollments/catalog', { params: { page, pageSize } }).then((r) => r.data);

export const enroll = (courseId) => api.post('/enrollments', { courseId }).then((r) => r.data);

export const fetchMyEnrollments = ({ status = 'active' } = {}) =>
  api.get('/enrollments/mine', { params: { status } }).then((r) => r.data);

export const fetchEnrollment = (id) => api.get(`/enrollments/${id}`).then((r) => r.data);

export const completeLesson = (enrollmentId, lessonId) =>
  api.post(`/enrollments/${enrollmentId}/lessons/${lessonId}/complete`).then((r) => r.data);
