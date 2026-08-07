import api from '@/services/api';

export const fetchProfile = () => api.get('/Profile').then((r) => r.data);

export const updateProfile = (payload) => api.put('/Profile', payload).then((r) => r.data);

// The avatar route requires the same JWT bearer auth as every other API call, so it can't be
// used directly as an <img src>; fetch it as a blob and hand the caller an object URL instead.
export const fetchAvatarObjectUrl = () =>
  api.get('/profile/avatar', { responseType: 'blob' }).then((r) => URL.createObjectURL(r.data));

export const uploadAvatar = (file) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post('/profile/avatar', formData).then((r) => r.data);
};

export const deleteAvatar = () => api.delete('/profile/avatar').then((r) => r.data);
