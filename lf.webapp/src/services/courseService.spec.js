import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import api from '@/services/api';
import {
  fetchCategories,
  fetchCourses,
  fetchCourse,
  createCourse,
  uploadCourseCoverImage,
  fetchCourseCoverImageObjectUrl,
  addChapter,
  renameChapter,
  moveChapter,
  addLesson,
  updateLesson,
  moveLesson,
  removeLesson,
  publishCourse,
  enrollStudent,
} from '@/services/courseService';

describe('courseService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    for (const m of Object.values(api)) m.mockResolvedValue({ data: 'RESULT' });
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:cover');
  });

  it.each([
    ['fetchCategories', () => fetchCategories(), 'get', ['/courses/categories']],
    ['fetchCourse', () => fetchCourse(5), 'get', ['/courses/5']],
    ['createCourse', () => createCourse({ title: 'T' }), 'post', ['/courses', { title: 'T' }]],
    ['addChapter', () => addChapter(1, 'Ch'), 'post', ['/courses/1/chapters', { title: 'Ch' }]],
    ['renameChapter', () => renameChapter(1, 2, 'New'), 'put', ['/courses/1/chapters/2', { title: 'New' }]],
    ['moveChapter', () => moveChapter(1, 2, 'up'), 'post', ['/courses/1/chapters/2/move', { direction: 'up' }]],
    ['addLesson', () => addLesson(1, 2, { title: 'L' }), 'post', ['/courses/1/chapters/2/lessons', { title: 'L' }]],
    ['updateLesson', () => updateLesson(1, 2, 3, { title: 'L2' }), 'put', ['/courses/1/chapters/2/lessons/3', { title: 'L2' }]],
    ['moveLesson', () => moveLesson(1, 2, 3, 'down'), 'post', ['/courses/1/chapters/2/lessons/3/move', { direction: 'down' }]],
    ['removeLesson', () => removeLesson(1, 2, 3), 'delete', ['/courses/1/chapters/2/lessons/3']],
    ['publishCourse', () => publishCourse(5), 'post', ['/courses/5/publish']],
    ['enrollStudent', () => enrollStudent(5, 9), 'post', ['/courses/5/enrollments', { userId: 9 }]],
  ])('%s calls the right endpoint and unwraps data', async (_name, call, method, args) => {
    await expect(call()).resolves.toBe('RESULT');
    expect(api[method]).toHaveBeenCalledWith(...args);
  });

  it('fetchCourses sends default paging params', async () => {
    await fetchCourses();
    expect(api.get).toHaveBeenCalledWith('/courses', { params: { page: 1, pageSize: 20 } });
  });

  it('fetchCourses forwards explicit paging params', async () => {
    await fetchCourses({ page: 3, pageSize: 50 });
    expect(api.get).toHaveBeenCalledWith('/courses', { params: { page: 3, pageSize: 50 } });
  });

  it('uploadCourseCoverImage POSTs multipart form data with the file', async () => {
    const file = new File(['x'], 'cover.png', { type: 'image/png' });
    await uploadCourseCoverImage(file);

    const [url, body] = api.post.mock.calls[0];
    expect(url).toBe('/courses/cover-image');
    expect(body).toBeInstanceOf(FormData);
    expect(body.get('file')).toBe(file);
  });

  it('fetchCourseCoverImageObjectUrl blob-fetches and returns an object URL', async () => {
    const blob = new Blob(['img']);
    api.get.mockResolvedValue({ data: blob });

    await expect(fetchCourseCoverImageObjectUrl(5)).resolves.toBe('blob:cover');
    expect(api.get).toHaveBeenCalledWith('/courses/5/cover/image', { responseType: 'blob' });
    expect(URL.createObjectURL).toHaveBeenCalledWith(blob);
  });
});
