import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import api from '@/services/api';
import {
  uploadLessonMedia,
  uploadLessonFiles,
  replaceLessonParts,
  fetchLessonMediaObjectUrl,
} from '@/services/lessonPartService';

describe('lessonPartService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    for (const m of Object.values(api)) m.mockResolvedValue({ data: 'RESULT' });
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:lesson-media');
  });

  it('uploadLessonMedia POSTs a single-file form to /courses/lesson-media', async () => {
    const file = new File(['x'], 'clip.mp4', { type: 'video/mp4' });
    await expect(uploadLessonMedia(file)).resolves.toBe('RESULT');

    const [url, body] = api.post.mock.calls[0];
    expect(url).toBe('/courses/lesson-media');
    expect(body).toBeInstanceOf(FormData);
    expect(body.get('file')).toBe(file);
  });

  it('uploadLessonFiles appends every file under the "files" field', async () => {
    const a = new File(['a'], 'a.pdf');
    const b = new File(['b'], 'b.pdf');
    await uploadLessonFiles([a, b]);

    const [url, body] = api.post.mock.calls[0];
    expect(url).toBe('/courses/lesson-files');
    expect(body.getAll('files')).toEqual([a, b]);
  });

  it('replaceLessonParts PUTs the ordered parts to the lesson parts route', async () => {
    const parts = [{ partType: 'text', html: '<p>hi</p>' }];
    await replaceLessonParts(1, 2, 3, parts);
    expect(api.put).toHaveBeenCalledWith('/courses/1/chapters/2/lessons/3/parts', { parts });
  });

  it('fetchLessonMediaObjectUrl blob-fetches the part media and returns an object URL', async () => {
    const blob = new Blob(['m']);
    api.get.mockResolvedValue({ data: blob });

    await expect(fetchLessonMediaObjectUrl(1, 2, 3, 9)).resolves.toBe('blob:lesson-media');
    expect(api.get).toHaveBeenCalledWith(
      '/courses/1/chapters/2/lessons/3/parts/9/media',
      { responseType: 'blob' },
    );
    expect(URL.createObjectURL).toHaveBeenCalledWith(blob);
  });
});
