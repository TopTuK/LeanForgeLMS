import api from '@/services/api';

// Courses the user created or was assigned to teach (fetchCourses only returns owned ones).
export const fetchTeachingCourses = () => api.get('/teaching/courses').then((r) => r.data);
