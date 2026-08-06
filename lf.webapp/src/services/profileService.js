import api from '@/services/api';

export const fetchProfile = () => api.get('/Profile').then((r) => r.data);

export const updateProfile = (payload) => api.put('/Profile', payload).then((r) => r.data);
