import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import api from '@/services/api';
import {
  fetchProfile,
  updateProfile,
  fetchAvatarObjectUrl,
  uploadAvatar,
  deleteAvatar,
} from '@/services/profileService';

describe('profileService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:avatar');
  });

  it('fetchProfile GETs /Profile and unwraps data', async () => {
    api.get.mockResolvedValue({ data: { firstName: 'Ada' } });
    await expect(fetchProfile()).resolves.toEqual({ firstName: 'Ada' });
    expect(api.get).toHaveBeenCalledWith('/Profile', undefined);
  });

  it('fetchProfile forwards a request config (used by the auth probe)', async () => {
    api.get.mockResolvedValue({ data: {} });
    await fetchProfile({ skipAuthRedirect: true });
    expect(api.get).toHaveBeenCalledWith('/Profile', { skipAuthRedirect: true });
  });

  it('updateProfile PUTs the payload to /Profile', async () => {
    api.put.mockResolvedValue({ data: { ok: true } });
    await expect(updateProfile({ description: 'hi' })).resolves.toEqual({ ok: true });
    expect(api.put).toHaveBeenCalledWith('/Profile', { description: 'hi' });
  });

  it('fetchAvatarObjectUrl blob-fetches the avatar and returns an object URL', async () => {
    const blob = new Blob(['x']);
    api.get.mockResolvedValue({ data: blob });

    await expect(fetchAvatarObjectUrl()).resolves.toBe('blob:avatar');
    expect(api.get).toHaveBeenCalledWith('/profile/avatar', { responseType: 'blob' });
    expect(URL.createObjectURL).toHaveBeenCalledWith(blob);
  });

  it('uploadAvatar POSTs multipart form data with the file', async () => {
    api.post.mockResolvedValue({ data: { avatarKey: 'k' } });
    const file = new File(['x'], 'me.png', { type: 'image/png' });

    await expect(uploadAvatar(file)).resolves.toEqual({ avatarKey: 'k' });

    const [url, body] = api.post.mock.calls[0];
    expect(url).toBe('/profile/avatar');
    expect(body).toBeInstanceOf(FormData);
    expect(body.get('file')).toBe(file);
  });

  it('deleteAvatar DELETEs /profile/avatar', async () => {
    api.delete.mockResolvedValue({ data: null });
    await deleteAvatar();
    expect(api.delete).toHaveBeenCalledWith('/profile/avatar');
  });
});
