import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from "@/stores/authStore";
import { storeToRefs } from "pinia";
import { i18n } from '@/i18n';

const HomeView = () => import('@/views/HomeView.vue');
const LoginView = () => import('@/views/LoginView.vue');
const CoursesView = () => import('@/views/CoursesView.vue');
const AvailableCoursesView = () => import('@/views/courses/AvailableCoursesView.vue');
const CourseDetailView = () => import('@/views/courses/CourseDetailView.vue');
const ActiveCoursesView = () => import('@/views/courses/ActiveCoursesView.vue');
const FinishedCoursesView = () => import('@/views/courses/FinishedCoursesView.vue');
const TeachingCoursesView = () => import('@/views/courses/TeachingCoursesView.vue');
const CreateCourseView = () => import('@/views/courses/CreateCourseView.vue');
const CourseEditorView = () => import('@/views/courses/CourseEditorView.vue');
const LessonEditorView = () => import('@/views/courses/LessonEditorView.vue');
const CourseLearnView = () => import('@/views/courses/CourseLearnView.vue');
const PaymentResultView = () => import('@/views/payments/PaymentResultView.vue');
const EventsView = () => import('@/views/EventsView.vue');
const CertificatesView = () => import('@/views/CertificatesView.vue');
const ProfileView = () => import('@/views/ProfileView.vue');
const AdminLayout = () => import('@/layout/AdminLayout.vue');
const AdminUsersView = () => import('@/views/admin/AdminUsersView.vue');
const AdminCoursesView = () => import('@/views/admin/AdminCoursesView.vue');
const AdminCategoriesView = () => import('@/views/admin/AdminCategoriesView.vue');
const AdminPromoCodesView = () => import('@/views/admin/AdminPromoCodesView.vue');
const AdminSettingsView = () => import('@/views/admin/AdminSettingsView.vue');
const AdminPaymentsView = () => import('@/views/admin/AdminPaymentsView.vue');

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
        component: CoursesView,
        meta: {
            requiresAuth: true,
        },
        children: [
            { path: '', name: 'Courses', redirect: { name: 'CoursesAvailable' } },
            {
                path: 'available',
                name: 'CoursesAvailable',
                component: AvailableCoursesView,
                meta: {
                    title: 'courses_available_view_title',
                    requiresAuth: true,
                }
            },
            {
                path: 'preview/:id',
                name: 'CourseDetail',
                component: CourseDetailView,
                meta: {
                    title: 'courses_detail_view_title',
                    requiresAuth: true,
                }
            },
            {
                path: 'active',
                name: 'CoursesActive',
                component: ActiveCoursesView,
                meta: {
                    title: 'courses_active_view_title',
                    requiresAuth: true,
                }
            },
            {
                path: 'finished',
                name: 'CoursesFinished',
                component: FinishedCoursesView,
                meta: {
                    title: 'courses_finished_view_title',
                    requiresAuth: true,
                }
            },
            {
                path: 'teaching',
                name: 'CoursesTeaching',
                component: TeachingCoursesView,
                meta: {
                    title: 'courses_teaching_view_title',
                    requiresAuth: true,
                    roles: ['Instructor', 'CourseCreator', 'Admin'],
                }
            },
            {
                path: 'create',
                name: 'CoursesCreate',
                component: CreateCourseView,
                meta: {
                    title: 'courses_create_view_title',
                    requiresAuth: true,
                    roles: ['CourseCreator', 'Admin'],
                }
            },
            {
                path: 'edit/:id',
                name: 'CourseEdit',
                component: CourseEditorView,
                meta: {
                    title: 'courses_edit_view_title',
                    requiresAuth: true,
                    roles: ['CourseCreator', 'Admin'],
                }
            },
            {
                path: 'edit/:courseId/chapters/:chapterId/lessons/:lessonId',
                name: 'LessonEdit',
                component: LessonEditorView,
                meta: {
                    title: 'courses_lesson_edit_view_title',
                    requiresAuth: true,
                    roles: ['CourseCreator', 'Admin'],
                }
            },
            {
                path: 'learn/:enrollmentId',
                name: 'CourseLearn',
                component: CourseLearnView,
                meta: {
                    title: 'courses_learn_view_title',
                    requiresAuth: true,
                }
            },
        ],
    },
    {
        path: '/payments/success',
        name: 'PaymentSuccess',
        component: PaymentResultView,
        props: { outcome: 'success' },
        meta: {
            title: 'payment_result_view_title',
            requiresAuth: true,
        }
    },
    {
        path: '/payments/fail',
        name: 'PaymentFail',
        component: PaymentResultView,
        props: { outcome: 'fail' },
        meta: {
            title: 'payment_result_view_title',
            requiresAuth: true,
        }
    },
    {
        path: '/events',
        name: 'Events',
        component: EventsView,
        meta: {
            title: 'events_view_title',
            requiresAuth: true,
        }
    },
    {
        path: '/certificates',
        name: 'Certificates',
        component: CertificatesView,
        meta: {
            title: 'certificates_view_title',
            requiresAuth: true,
        }
    },
    {
        path: '/profile',
        name: 'Profile',
        component: ProfileView,
        meta: {
            title: 'profile_view_title',
            requiresAuth: true,
        }
    },
    {
        path: '/admin',
        component: AdminLayout,
        meta: {
            requiresAuth: true,
            roles: ['Admin'],
        },
        children: [
            { path: '', redirect: { name: 'AdminUsers' } },
            {
                path: 'users',
                name: 'AdminUsers',
                component: AdminUsersView,
                meta: {
                    title: 'admin_users_view_title',
                    requiresAuth: true,
                    roles: ['Admin'],
                }
            },
            {
                path: 'courses',
                name: 'AdminCourses',
                component: AdminCoursesView,
                meta: {
                    title: 'admin_courses_view_title',
                    requiresAuth: true,
                    roles: ['Admin'],
                }
            },
            {
                path: 'categories',
                name: 'AdminCategories',
                component: AdminCategoriesView,
                meta: {
                    title: 'admin_categories_view_title',
                    requiresAuth: true,
                    roles: ['Admin'],
                }
            },
            {
                path: 'promo-codes',
                name: 'AdminPromoCodes',
                component: AdminPromoCodesView,
                meta: {
                    title: 'admin_promo_codes_view_title',
                    requiresAuth: true,
                    roles: ['Admin'],
                }
            },
            {
                path: 'payments',
                name: 'AdminPayments',
                component: AdminPaymentsView,
                meta: {
                    title: 'admin_payments_view_title',
                    requiresAuth: true,
                    roles: ['Admin'],
                }
            },
            {
                path: 'settings',
                name: 'AdminSettings',
                component: AdminSettingsView,
                meta: {
                    title: 'admin_settings_view_title',
                    requiresAuth: true,
                    roles: ['Admin'],
                }
            },
        ],
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
    // The session lives in an HttpOnly cookie, so auth state comes from a one-time API probe.
    await authStore.ensureInitialized()

    const { isAuthenticated } = storeToRefs(authStore)
    console.log('Router::beforeEach: isAuthenticated=', isAuthenticated.value)

    if (isAuthenticated.value) {
        if (!to.meta.requiresAuth) {
            console.log('Router::beforeEach: route does not require auth. Redirecting to Courses')
            return { name: 'Courses' }
        }

        if (to.meta.roles?.length) {
            if (!authStore.user) await authStore.fetchUser()

            if (!authStore.user || !to.meta.roles.includes(authStore.user.role)) {
                console.log('Router::beforeEach: route requires role(s):', to.meta.roles, '. Redirecting to Courses')
                return { name: 'Courses' }
            }
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

router.afterEach((to) => {
    if (to.meta.title) {
        document.title = i18n.global.t(to.meta.title);
    }
});

export default router;