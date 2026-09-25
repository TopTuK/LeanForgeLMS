import { defineStore } from 'pinia';
import { ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import api from '@/services/api';
import { fetchProfile, fetchAvatarObjectUrl, updatePreferredLanguage } from '@/services/profileService';
import { currentLocale, isSupportedLocale, setLocale } from '@/i18n/index.js';
import { setUserRole, track, trackLoginCompleted } from '@/lib/analytics';

export const useAuthStore = defineStore('auth', () => {
    // State
    const user = ref(null); // { firstName, lastName, email, role, description }
    const avatarUrl = ref(null); // object URL for the current user's avatar image
    const initialized = ref(false);

    let initPromise = null;

    // Getters
    const isAuthenticated = computed(() => user.value !== null);

    const isAdmin = computed(() => user.value?.role === 'Admin');
    const isStudent = computed(() => user.value?.role === 'Student');
    const isInstructor = computed(() => user.value?.role === 'Instructor');
    const isCourseCreator = computed(() => user.value?.role === 'CourseCreator');
    const canViewTeachingCourses = computed(() => isInstructor.value || isCourseCreator.value || isAdmin.value);
    const canCreateCourses = computed(() => isCourseCreator.value || isAdmin.value);

    const router = useRouter();

    watch(isAuthenticated, (authed) => {
        if (!router) return;
        if (!authed && router.currentRoute.value.meta.requiresAuth) {
            router.push({ name: 'Login' });
        }
    });

    watch(() => user.value?.role, setUserRole, { immediate: true });

    // Actions

    const saveLanguage = async (language) => {
        try {
            const updated = await updatePreferredLanguage(language);
            if (user.value) user.value = { ...user.value, preferredLanguage: updated.preferredLanguage };
        } catch {
            // Best effort: the UI already switched; emails just keep the previous language.
        }
    };

    // The server copy follows the user across devices; a user who never chose one gets whatever
    // they are currently looking at, so emails match the UI from their first enrollment on.
    const syncLocaleWithProfile = () => {
        const preferred = user.value?.preferredLanguage;
        if (isSupportedLocale(preferred)) {
            setLocale(preferred);
        } else if (user.value) {
            saveLanguage(currentLocale());
        }
    };

    const changeLocale = (language) => {
        setLocale(language);
        if (isAuthenticated.value && user.value.preferredLanguage !== language) saveLanguage(language);
    };

    // Resolves auth state from the HttpOnly session cookie exactly once per app load:
    // the SPA can't read the cookie, so it asks the API who it is.
    const ensureInitialized = () => {
        if (initialized.value) return Promise.resolve();
        if (initPromise) return initPromise;

        initPromise = (async () => {
            try {
                user.value = await fetchProfile({ skipAuthRedirect: true });
                trackLoginCompleted();
                syncLocaleWithProfile();
            } catch {
                user.value = null;
            } finally {
                initialized.value = true;
                initPromise = null;
            }
        })();

        return initPromise;
    };

    const fetchUser = async () => {
        try {
            user.value = await fetchProfile({ skipAuthRedirect: true });
        } catch {
            user.value = null;
        }
    };

    const refreshAvatar = async () => {
        if (avatarUrl.value) URL.revokeObjectURL(avatarUrl.value);
        avatarUrl.value = null;

        if (!isAuthenticated.value) return;

        try {
            avatarUrl.value = await fetchAvatarObjectUrl();
        } catch {
            avatarUrl.value = null;
        }
    };

    const updateUser = (updated) => {
        user.value = { ...user.value, ...updated };
    };

    const clear = () => {
        user.value = null;
        if (avatarUrl.value) URL.revokeObjectURL(avatarUrl.value);
        avatarUrl.value = null;
    };

    const logout = async () => {
        try {
            await api.get('/Auth/Logout');
        } catch {
            // Session may already be gone — proceed with client-side cleanup regardless.
        }

        track('logout');
        clear();
    };

    return {
        isAuthenticated, isAdmin, isStudent, isInstructor, isCourseCreator,
        canViewTeachingCourses, canCreateCourses,
        user, avatarUrl, initialized,
        ensureInitialized, fetchUser, refreshAvatar, updateUser, clear, logout, changeLocale,
    };
});
