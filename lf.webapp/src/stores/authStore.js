import { defineStore } from 'pinia';
import { ref, computed, watch } from 'vue'; // Uncomment when needed
import { COOKIE_NAME } from '@/config';
import Cookies from 'js-cookie';
import { useRouter } from 'vue-router';
import api from '@/services/api';
import { fetchProfile, fetchAvatarObjectUrl } from '@/services/profileService';

export const useAuthStore = defineStore('auth', () => {
    // State
    const hasCookie = ref(Boolean(Cookies.get(COOKIE_NAME)))
    const user = ref(null) // { firstName, lastName, email, role }
    const avatarUrl = ref(null) // object URL for the current user's avatar image

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

    const refreshAvatar = async () => {
        if (avatarUrl.value) URL.revokeObjectURL(avatarUrl.value);
        avatarUrl.value = null;

        if (!hasCookie.value) return;

        try {
            avatarUrl.value = await fetchAvatarObjectUrl()
        } catch {
            avatarUrl.value = null
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

        if (avatarUrl.value) URL.revokeObjectURL(avatarUrl.value);
        avatarUrl.value = null
    };

    return {
        hasCookie, isAuthenticated, user, avatarUrl, fetchUser, refreshAvatar, updateUser, logout,
    }
});

