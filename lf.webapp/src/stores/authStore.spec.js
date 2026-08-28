import { describe, it, expect, beforeEach, vi } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';

const routerPush = vi.fn();

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: routerPush,
    currentRoute: { value: { meta: {} } },
  }),
}));

vi.mock('js-cookie', () => ({
  default: { get: vi.fn(() => undefined), remove: vi.fn() },
}));

vi.mock('@/services/api', () => ({
  default: { get: vi.fn().mockResolvedValue({ data: {} }) },
}));

vi.mock('@/services/profileService', () => ({
  fetchProfile: vi.fn(),
  fetchAvatarObjectUrl: vi.fn(),
}));

import Cookies from 'js-cookie';
import api from '@/services/api';
import { fetchProfile } from '@/services/profileService';
import { useAuthStore } from '@/stores/authStore';

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    Cookies.get.mockReturnValue(undefined);
  });

  it('derives isAuthenticated from the cookie presence', () => {
    const store = useAuthStore();
    expect(store.isAuthenticated).toBe(false);

    store.hasCookie = true;
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

  it('fetchUser is a no-op without a cookie', async () => {
    const store = useAuthStore();
    await store.fetchUser();
    expect(fetchProfile).not.toHaveBeenCalled();
    expect(store.user).toBeNull();
  });

  it('fetchUser populates the user on success and nulls it on failure', async () => {
    const store = useAuthStore();
    store.hasCookie = true;

    fetchProfile.mockResolvedValueOnce({ firstName: 'Ada', role: 'Student' });
    await store.fetchUser();
    expect(store.user).toEqual({ firstName: 'Ada', role: 'Student' });

    fetchProfile.mockRejectedValueOnce(new Error('401'));
    await store.fetchUser();
    expect(store.user).toBeNull();
  });

  it('updateUser merges a partial patch into the current user', () => {
    const store = useAuthStore();
    store.user = { firstName: 'Ada', lastName: 'Lovelace', role: 'Student' };

    store.updateUser({ lastName: 'Byron', description: 'hi' });

    expect(store.user).toEqual({
      firstName: 'Ada',
      lastName: 'Byron',
      role: 'Student',
      description: 'hi',
    });
  });

  it('logout clears client state and calls the logout endpoint', async () => {
    const store = useAuthStore();
    store.hasCookie = true;
    store.user = { role: 'Student' };

    await store.logout();

    expect(api.get).toHaveBeenCalledWith('/Auth/Logout');
    expect(Cookies.remove).toHaveBeenCalled();
    expect(store.hasCookie).toBe(false);
    expect(store.user).toBeNull();
  });

  it('logout still clears client state when the request rejects', async () => {
    const store = useAuthStore();
    store.hasCookie = true;
    store.user = { role: 'Student' };
    api.get.mockRejectedValueOnce(new Error('network'));

    await store.logout();

    expect(store.hasCookie).toBe(false);
    expect(store.user).toBeNull();
  });
});
