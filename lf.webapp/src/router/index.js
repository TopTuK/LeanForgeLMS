import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from "@/stores/authStore";
import { storeToRefs } from "pinia";

const HomeView = () => import('@/views/HomeView.vue');
const LoginView = () => import('@/views/LoginView.vue');
const CoursesView = () => import('@/views/CoursesView.vue');

const routes = [
    {
        path: '/',
        name: 'Home',
        component: HomeView,
        meta: {
            title: 'home_view_title',
            requiresAuth: false,
        }
    },
    {
        path: '/login',
        name: 'Login',
        component: LoginView,
        meta: {
            title: 'login_view_title',
            requiresAuth: false,
        }
    },
    {
        path: '/courses',
        name: 'Courses',
        component: CoursesView,
        meta: {
            title: 'courses_view_title',
            requiresAuth: true,
        }
    },
];

const router = createRouter({
    //history: createWebHashHistory(),
    history: createWebHistory(),
    routes,
    scrollBehavior(to, from, savedPosition) {
        return savedPosition || { top: 0 };
    },
});

router.beforeEach(async (to, from) => {
    console.log(`Router::beforeEach: from: ${from.name} -> to: ${to.name}`);  

    const authStore = useAuthStore()
    const { isAuthenticated } = storeToRefs(authStore)
    console.log('Router::beforeEach: isAuthenticated=', isAuthenticated.value)

    if (isAuthenticated.value) {
        if (!to.meta.requiresAuth) {
            console.log('Router::beforeEach: route does not require auth. Redirecting to Courses')
            return { name: 'Courses' }
        }
    }
    else {
        // Allow access to routes that don't require auth
        if (to.meta.requiresAuth) {
            console.log('Router::beforeEach: route requires auth. Block navigation')
            return {
                name: 'Login',
                query: { redirectTo: encodeURIComponent(to.fullPath) }
            }
        }
    }
});

export default router;