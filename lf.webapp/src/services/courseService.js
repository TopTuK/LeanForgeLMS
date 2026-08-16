import api from '@/services/api';

export const fetchCategories = () => api.get('/courses/categories').then((r) => r.data);

export const fetchCourses = ({ page = 1, pageSize = 20 } = {}) =>
  api.get('/courses', { params: { page, pageSize } }).then((r) => r.data);

export const fetchCourse = (id) => api.get(`/courses/${id}`).then((r) => r.data);

export const createCourse = (payload) => api.post('/courses', payload).then((r) => r.data);

export const uploadCourseCoverImage = (file) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post('/courses/cover-image', formData).then((r) => r.data);
};

export const fetchCourseCoverImageObjectUrl = (courseId) =>
  api.get(`/courses/${courseId}/cover/image`, { responseType: 'blob' }).then((r) => URL.createObjectURL(r.data));

export const addChapter = (courseId, title) =>
  api.post(`/courses/${courseId}/chapters`, { title }).then((r) => r.data);

export const renameChapter = (courseId, chapterId, title) =>
  api.put(`/courses/${courseId}/chapters/${chapterId}`, { title }).then((r) => r.data);

export const moveChapter = (courseId, chapterId, direction) =>
  api.post(`/courses/${courseId}/chapters/${chapterId}/move`, { direction }).then((r) => r.data);

export const addLesson = (courseId, chapterId, payload) =>
  api.post(`/courses/${courseId}/chapters/${chapterId}/lessons`, payload).then((r) => r.data);

export const updateLesson = (courseId, chapterId, lessonId, payload) =>
  api.put(`/courses/${courseId}/chapters/${chapterId}/lessons/${lessonId}`, payload).then((r) => r.data);

export const moveLesson = (courseId, chapterId, lessonId, direction) =>
  api.post(`/courses/${courseId}/chapters/${chapterId}/lessons/${lessonId}/move`, { direction }).then((r) => r.data);

export const removeLesson = (courseId, chapterId, lessonId) =>
  api.delete(`/courses/${courseId}/chapters/${chapterId}/lessons/${lessonId}`).then((r) => r.data);

export const publishCourse = (courseId) => api.post(`/courses/${courseId}/publish`).then((r) => r.data);
