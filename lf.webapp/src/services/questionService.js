import api from '@/services/api';

export const fetchQuestions = ({ scope = 'student', courseId = null, status = null, page = 1, pageSize = 20 } = {}) =>
  api
    .get('/questions', {
      params: {
        scope,
        courseId: courseId || undefined,
        status: status || undefined,
        page,
        pageSize,
      },
    })
    .then((r) => r.data);

export const fetchLessonQuestions = (lessonId, { page = 1, pageSize = 20 } = {}) =>
  api.get(`/lessons/${lessonId}/questions`, { params: { page, pageSize } }).then((r) => r.data);

export const fetchQuestionThread = (id) => api.get(`/questions/${id}`).then((r) => r.data);

export const askQuestion = ({ lessonId, title, body }) =>
  api.post('/questions', { lessonId, title, body }).then((r) => r.data);

export const postQuestionMessage = (id, body) => api.post(`/questions/${id}/messages`, { body }).then((r) => r.data);

export const closeQuestion = (id) => api.post(`/questions/${id}/close`).then((r) => r.data);

export const reopenQuestion = (id) => api.post(`/questions/${id}/reopen`).then((r) => r.data);

export const fetchUnreadQuestionCount = () => api.get('/questions/unread-count').then((r) => r.data.count);

export const fetchCourseInstructors = (courseId) => api.get(`/courses/${courseId}/instructors`).then((r) => r.data);

export const assignCourseInstructor = (courseId, email) =>
  api.post(`/courses/${courseId}/instructors`, { email }).then((r) => r.data);

export const removeCourseInstructor = (courseId, userId) =>
  api.delete(`/courses/${courseId}/instructors/${userId}`).then((r) => r.data);
