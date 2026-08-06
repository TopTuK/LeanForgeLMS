import axios from 'axios';
import Cookies from 'js-cookie';
import { COOKIE_NAME } from '@/config';

const api = axios.create({
    baseURL: '/api',
    withCredentials: true,
});

api.interceptors.request.use((config) => {
    const token = Cookies.get(COOKIE_NAME);
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
});

export default api;
