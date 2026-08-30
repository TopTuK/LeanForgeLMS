import { describe, it, expect, beforeEach, vi } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';

const routerPush = vi.fn();

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: routerPush,
    currentRoute: { value: { meta: {} } },
  }),
}));

vi.mock('@/services/api', () => ({
  default: { get: vi.fn().mockResolvedValue({ data: {} }) },
}));

vi.mock('@/services/profileService', () => ({
  fetchProfile: vi.fn(),
  fetchAvatarObjectUrl: vi.fn(),
}));

import api from '@/services/api';
import { fetchProfile } from '@/services/profileService';
import { useAuthStore } from '@/stores/authStore';

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('derives isAuthenticated from the loaded user', () => {
    const store = useAuthStore();
    expect(store.isAuthenticated).toBe(false);

    store.user = { role: 'Student' };
    expect(store.isAuthenticated).toBe(true);
  });

  it.each([
    ['Admin', { isAdmin: true, canViewTeachingCourses: true, canCreateCourses: true }],
    ['CourseCreator', { isCourseCreator: true, canViewTeachingCourses: true, canCreateCourses: true }],
    ['Instructor', { isInstructor: true, canViewTeachingCourses: true, canCreateCourses: false }],
    ['Student', { isStudent: true, canViewTeachingCourses: false, canCreateCourses: false }],
  ])('resolves role getters for %s', (role, expected) => {
    const store = useAuthStore();
    store.user = { role };
    for (const [getter, value] of Object.entries(expected)) {
      expect(store[getter], getter).toBe(value);
    }
  });

  describe('ensureInitialized', () => {
    it('loads the user from the auth probe and marks itself done', async () => {
      fetchProfile.mockResolvedValueOnce({ firstName: 'Ada', role: 'Student' });
      const store = useAuthStore();

      await store.ensureInitialized();

      expect(fetchProfile).toHaveBeenCalledWith({ skipAuthRedirect: true });
      expect(store.user).toEqual({ firstName: 'Ada', role: 'Student' });
      expect(store.initialized).toBe(true);
    });

    it('leaves the user null when the probe 401s', async () => {
      fetchProfile.mockRejectedValueOnce({ response: { status: 401 } });
      const store = useAuthStore();

      await store.ensureInitialized();

      expect(store.user).toBeNull();
      expect(store.initialized).toBe(true);
    });

    it('runs the probe only once across repeated and concurrent calls', async () => {
      fetchProfile.mockResolvedValue({ role: 'Student' });
      const store = useAuthStore();

      await Promise.all([store.ensureInitialized(), store.ensureInitialized()]);
      await store.ensureInitialized();

      expect(fetchProfile).toHaveBeenCalledTimes(1);
    });
  });

  it('fetchUser refreshes the user and nulls it on failure', async () => {
    const store = useAuthStore();

    fetchProfile.mockResolvedValueOnce({ role: 'Admin' });
    await store.fetchUser();
    expect(store.user).toEqual({ role: 'Admin' });

    fetchProfile.mockRejectedValueOnce(new Error('boom'));
    await store.fetchUser();
    expect(store.user).toBeNull();
  });

  it('updateUser merges a partial patch into the current user', () => {
    const store = useAuthStore();
    store.user = { firstName: 'Ada', lastName: 'Lovelace', role: 'Student' };

    store.updateUser({ lastName: 'Byron', description: 'hi' });

    expect(store.user).toEqual({ firstName: 'Ada', lastName: 'Byron', role: 'Student', description: 'hi' });
  });

  it('logout calls the logout endpoint and clears state', async () => {
    const store = useAuthStore();
    store.user = { role: 'Student' };

    await store.logout();

    expect(api.get).toHaveBeenCalledWith('/Auth/Logout');
    expect(store.user).toBeNull();
  });

  it('logout still clears state when the request rejects', async () => {
    const store = useAuthStore();
    store.user = { role: 'Student' };
    api.get.mockRejectedValueOnce(new Error('network'));

    await store.logout();

    expect(store.user).toBeNull();
  });
});
