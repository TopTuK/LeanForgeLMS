import api from '@/services/api';

export const fetchCourseGroups = (courseId) => api.get(`/courses/${courseId}/groups`).then((r) => r.data);

export const createGroup = (courseId, { name, description = null }) =>
  api.post(`/courses/${courseId}/groups`, { name, description }).then((r) => r.data);

export const fetchEligibleStudents = (courseId) =>
  api.get(`/courses/${courseId}/groups/eligible-students`).then((r) => r.data);

export const fetchMyGroups = () => api.get('/groups/mine').then((r) => r.data);

export const fetchGroup = (id) => api.get(`/groups/${id}`).then((r) => r.data);

export const updateGroup = (id, { name, description = null }) =>
  api.put(`/groups/${id}`, { name, description }).then((r) => r.data);

export const removeGroup = (id) => api.delete(`/groups/${id}`);

export const addGroupMembers = (id, userIds) => api.post(`/groups/${id}/members`, { userIds }).then((r) => r.data);

export const removeGroupMember = (id, userId) => api.delete(`/groups/${id}/members/${userId}`).then((r) => r.data);
