import api from '@/services/api';

export const fetchCatalog = ({ page = 1, pageSize = 20 } = {}) =>
  api.get('/enrollments/catalog', { params: { page, pageSize } }).then((r) => r.data);

export const fetchCoursePreview = (courseId) =>
  api.get(`/enrollments/catalog/${courseId}`).then((r) => r.data);

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

// Same auth constraint as the media blob-fetch above — a Files part's attachment can't be linked
// with a plain <a href>, so this resolves the download to a blob object URL on demand instead.
export const fetchEnrollmentLessonPartFileObjectUrl = (enrollmentId, lessonId, partId, fileId) =>
  api.get(`/enrollments/${enrollmentId}/lessons/${lessonId}/parts/${partId}/files/${fileId}/media`, { responseType: 'blob' })
    .then((r) => URL.createObjectURL(r.data));

// Preview-lesson equivalents of the two helpers above, used on the course details page before
// enrollment exists — same blob-fetch constraint (auth header can't reach a plain <img>/<a> src).
export const fetchCoursePreviewLessonMediaObjectUrl = (courseId, lessonId, partId) =>
  api.get(`/enrollments/catalog/${courseId}/lessons/${lessonId}/parts/${partId}/media`, { responseType: 'blob' })
    .then((r) => URL.createObjectURL(r.data));

export const fetchCoursePreviewLessonPartFileObjectUrl = (courseId, lessonId, partId, fileId) =>
  api.get(`/enrollments/catalog/${courseId}/lessons/${lessonId}/parts/${partId}/files/${fileId}/media`, { responseType: 'blob' })
    .then((r) => URL.createObjectURL(r.data));
