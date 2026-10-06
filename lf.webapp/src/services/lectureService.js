import api from '@/services/api';

export const fetchCourseLectures = (courseId) => api.get(`/courses/${courseId}/lectures`).then((r) => r.data);

export const scheduleLecture = (courseId, lecture) =>
  api.post(`/courses/${courseId}/lectures`, lecture).then((r) => r.data);

export const updateLecture = (id, lecture) => api.put(`/lectures/${id}`, lecture).then((r) => r.data);

export const cancelLecture = (id) => api.post(`/lectures/${id}/cancel`).then((r) => r.data);

export const removeLecture = (id) => api.delete(`/lectures/${id}`);

export const fetchMySchedule = ({ from = null, to = null } = {}) =>
  api
    .get('/lectures/mine', { params: { from: from || undefined, to: to || undefined } })
    .then((r) => r.data);
