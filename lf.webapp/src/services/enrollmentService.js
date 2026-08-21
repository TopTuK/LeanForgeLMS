import api from '@/services/api';

export const fetchCatalog = ({ page = 1, pageSize = 20 } = {}) =>
  api.get('/enrollments/catalog', { params: { page, pageSize } }).then((r) => r.data);

export const enroll = (courseId) => api.post('/enrollments', { courseId }).then((r) => r.data);

export const fetchMyEnrollments = ({ status = 'active' } = {}) =>
  api.get('/enrollments/mine', { params: { status } }).then((r) => r.data);

export const fetchEnrollment = (id) => api.get(`/enrollments/${id}`).then((r) => r.data);

export const completeLesson = (enrollmentId, lessonId) =>
  api.post(`/enrollments/${enrollmentId}/lessons/${lessonId}/complete`).then((r) => r.data);

export const submitQuizAttempt = (enrollmentId, lessonId, partId, answers) =>
  api.post(`/enrollments/${enrollmentId}/lessons/${lessonId}/parts/${partId}/quiz/submit`, { answers }).then((r) => r.data);

export const fetchCourseCoverImageObjectUrl = (courseId) =>
  api.get(`/enrollments/courses/${courseId}/cover/image`, { responseType: 'blob' }).then((r) => URL.createObjectURL(r.data));

// Lesson media endpoints require auth, so a plain <img>/<video>/<audio> src can't hit them
// directly (no bearer header on a browser-initiated resource fetch) — blob-fetch instead.
export const fetchEnrollmentLessonMediaObjectUrl = (enrollmentId, lessonId, partId) =>
  api.get(`/enrollments/${enrollmentId}/lessons/${lessonId}/parts/${partId}/media`, { responseType: 'blob' })
    .then((r) => URL.createObjectURL(r.data));
