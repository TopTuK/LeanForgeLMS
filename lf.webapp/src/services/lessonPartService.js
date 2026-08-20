import api from '@/services/api';

export const uploadLessonMedia = (file) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post('/courses/lesson-media', formData).then((r) => r.data);
};

export const replaceLessonParts = (courseId, chapterId, lessonId, parts) =>
  api.put(`/courses/${courseId}/chapters/${chapterId}/lessons/${lessonId}/parts`, { parts }).then((r) => r.data);

// Lesson media endpoints require auth, so a plain <img>/<video>/<audio> src can't hit them
// directly (no bearer header on a browser-initiated resource fetch) — blob-fetch instead.
export const fetchLessonMediaObjectUrl = (courseId, chapterId, lessonId, partId) =>
  api.get(`/courses/${courseId}/chapters/${chapterId}/lessons/${lessonId}/parts/${partId}/media`, { responseType: 'blob' })
    .then((r) => URL.createObjectURL(r.data));
