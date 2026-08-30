import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import api from '@/services/api';

// Invoke the response interceptor's rejection handler directly.
const rejectionHandler = api.interceptors.response.handlers[0].rejected;

describe('api instance', () => {
  let assign;

  beforeEach(() => {
    assign = vi.fn();
    vi.stubGlobal('location', { pathname: '/courses', assign });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('is configured with the /api base URL and credentials', () => {
    expect(api.defaults.baseURL).toBe('/api');
    expect(api.defaults.withCredentials).toBe(true);
  });

  it('does not attach an Authorization header (token lives in an HttpOnly cookie)', () => {
    expect(api.interceptors.request.handlers.filter(Boolean)).toHaveLength(0);
  });

  it('redirects to /login on a 401', async () => {
    await expect(rejectionHandler({ response: { status: 401 }, config: {} })).rejects.toBeDefined();
    expect(assign).toHaveBeenCalledWith('/login');
  });

  it('does not redirect when the caller opted out with skipAuthRedirect', async () => {
    await expect(
      rejectionHandler({ response: { status: 401 }, config: { skipAuthRedirect: true } }),
    ).rejects.toBeDefined();
    expect(assign).not.toHaveBeenCalled();
  });

  it('does not redirect on non-401 errors', async () => {
    await expect(rejectionHandler({ response: { status: 500 }, config: {} })).rejects.toBeDefined();
    expect(assign).not.toHaveBeenCalled();
  });

  it('does not redirect when already on the login page', async () => {
    vi.stubGlobal('location', { pathname: '/login', assign });
    await expect(rejectionHandler({ response: { status: 401 }, config: {} })).rejects.toBeDefined();
    expect(assign).not.toHaveBeenCalled();
  });
});
