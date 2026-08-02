import { defineStore } from 'pinia';
import { ref, computed, watch } from 'vue'; // Uncomment when needed
import { COOKIE_NAME } from '@/config';
import Cookies from 'js-cookie';
import { useRouter } from 'vue-router';

export const useAuthStore = defineStore('auth', () => {
    // State
    const hasCookie = ref(Boolean(Cookies.get(COOKIE_NAME)))

    // Getters
    const isAuthenticated = computed(() => {
        return hasCookie.value
    });

    const router = useRouter()

    watch(hasCookie, (newVal) => {
        // We need this because useAuth may be called before router is initialized
        if (!router) return; 

        if (router.currentRoute.value.meta.requireAuth && !newVal) {
            router.push({ name: 'Login' })
        }
    });

    // Actions
    const logout = () => {
        Cookies.remove(COOKIE_NAME)
        hasCookie.value = false
    };

    return {
        hasCookie, isAuthenticated, logout,
    }
});

