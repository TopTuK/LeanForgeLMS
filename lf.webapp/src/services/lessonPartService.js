import api from '@/services/api';

export const uploadLessonMedia = (file) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post('/courses/lesson-media', formData).then((r) => r.data);
};

export const replaceLessonParts = (courseId, chapterId, lessonId, parts) =>
  api.put(`/courses/${courseId}/chapters/${chapterId}/lessons/${lessonId}/parts`, { parts }).then((r) => r.data);
