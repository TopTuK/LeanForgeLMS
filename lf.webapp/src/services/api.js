import axios from 'axios';

// The session JWT lives in an HttpOnly cookie the browser attaches automatically
// (withCredentials); page scripts never see the token.
const api = axios.create({
    baseURL: '/api',
    withCredentials: true,
});

// A 401 means the session cookie is missing or expired — send the user back to login.
// Callers that expect an anonymous 401 (the startup auth probe) pass `skipAuthRedirect`.
api.interceptors.response.use(
    (response) => response,
    (error) => {
        const status = error?.response?.status;
        const skip = error?.config?.skipAuthRedirect;
        if (status === 401 && !skip && !window.location.pathname.startsWith('/login')) {
            window.location.assign('/login');
        }
        return Promise.reject(error);
    },
);

export default api;
