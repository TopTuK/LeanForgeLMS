import { describe, it, expect, beforeEach, vi } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { usePlatformStore } from '@/stores/platformStore';
import { fetchPlatformConfig } from '@/services/platformService';

vi.mock('@/services/platformService', () => ({
  fetchPlatformConfig: vi.fn(),
}));

describe('usePlatformStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('defaults studentEnrollmentEnabled to false before loading', () => {
    const store = usePlatformStore();
    expect(store.studentEnrollmentEnabled).toBe(false);
    expect(store.loaded).toBe(false);
  });

  it('ensureLoaded fetches once and sets the flag', async () => {
    fetchPlatformConfig.mockResolvedValue({ studentEnrollmentEnabled: true });
    const store = usePlatformStore();

    await store.ensureLoaded();
    await store.ensureLoaded();

    expect(fetchPlatformConfig).toHaveBeenCalledTimes(1);
    expect(store.studentEnrollmentEnabled).toBe(true);
    expect(store.loaded).toBe(true);
  });

  it('ensureLoaded keeps the flag false when the request fails', async () => {
    fetchPlatformConfig.mockRejectedValue(new Error('offline'));
    const store = usePlatformStore();

    await store.ensureLoaded();

    expect(store.studentEnrollmentEnabled).toBe(false);
    expect(store.loaded).toBe(true);
  });

  it('refresh re-fetches the config', async () => {
    fetchPlatformConfig.mockResolvedValueOnce({ studentEnrollmentEnabled: false });
    const store = usePlatformStore();
    await store.ensureLoaded();

    fetchPlatformConfig.mockResolvedValueOnce({ studentEnrollmentEnabled: true });
    await store.refresh();

    expect(fetchPlatformConfig).toHaveBeenCalledTimes(2);
    expect(store.studentEnrollmentEnabled).toBe(true);
  });
});
