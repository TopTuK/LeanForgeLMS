import api from '@/services/api';

// Public runtime config the SPA reads before deciding whether to show the enroll CTA.
export const fetchPlatformConfig = () =>
  api.get('/platform/config', { skipAuthRedirect: true }).then((r) => r.data);
