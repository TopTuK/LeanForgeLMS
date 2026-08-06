import { defineStore } from 'pinia';
import { ref, computed, watch } from 'vue'; // Uncomment when needed
import { COOKIE_NAME } from '@/config';
import Cookies from 'js-cookie';
import { useRouter } from 'vue-router';
import api from '@/services/api';
import { fetchProfile } from '@/services/profileService';

export const useAuthStore = defineStore('auth', () => {
    // State
    const hasCookie = ref(Boolean(Cookies.get(COOKIE_NAME)))
    const user = ref(null) // { firstName, lastName, email }

    // Getters
    const isAuthenticated = computed(() => {
        return hasCookie.value
    });

    const router = useRouter()

    watch(hasCookie, (newVal) => {
        // We need this because useAuth may be called before router is initialized
        if (!router) return;

        if (router.currentRoute.value.meta.requiresAuth && !newVal) {
            router.push({ name: 'Login' })
        }

        if (!newVal) {
            user.value = null
        }
    });

    // Actions
    const fetchUser = async () => {
        if (!hasCookie.value) return;

        try {
            user.value = await fetchProfile()
        } catch {
            user.value = null
        }
    };

    const updateUser = (updated) => {
        user.value = { ...user.value, ...updated }
    };

    const logout = async () => {
        try {
            await api.get('/Auth/Logout')
        } catch {
            // Cookie may already be gone/expired - proceed with client-side cleanup regardless
        }

        Cookies.remove(COOKIE_NAME)
        hasCookie.value = false
        user.value = null
    };

    return {
        hasCookie, isAuthenticated, user, fetchUser, updateUser, logout,
    }
});

