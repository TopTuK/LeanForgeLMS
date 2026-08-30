import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import api from '@/services/api';
import {
  fetchCatalog,
  fetchCoursePreview,
  enroll,
  validatePromoCode,
  fetchMyEnrollments,
  fetchEnrollment,
  completeLesson,
  submitQuizAttempt,
  fetchCourseCoverImageObjectUrl,
  fetchEnrollmentLessonMediaObjectUrl,
  fetchEnrollmentLessonPartFileObjectUrl,
  fetchCoursePreviewLessonMediaObjectUrl,
  fetchCoursePreviewLessonPartFileObjectUrl,
} from '@/services/enrollmentService';

describe('enrollmentService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    for (const m of Object.values(api)) m.mockResolvedValue({ data: 'RESULT' });
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:media');
  });

  it.each([
    ['fetchCoursePreview', () => fetchCoursePreview(5), 'get', ['/enrollments/catalog/5']],
    ['fetchEnrollment', () => fetchEnrollment(7), 'get', ['/enrollments/7']],
    ['completeLesson', () => completeLesson(7, 3), 'post', ['/enrollments/7/lessons/3/complete']],
    ['submitQuizAttempt', () => submitQuizAttempt(7, 3, 9, [{ questionId: 1, selectedOptionIds: [2] }]), 'post',
      ['/enrollments/7/lessons/3/parts/9/quiz/submit', { answers: [{ questionId: 1, selectedOptionIds: [2] }] }]],
  ])('%s calls the right endpoint and unwraps data', async (_name, call, method, args) => {
    await expect(call()).resolves.toBe('RESULT');
    expect(api[method]).toHaveBeenCalledWith(...args);
  });

  it('fetchCatalog sends default paging params', async () => {
    await fetchCatalog();
    expect(api.get).toHaveBeenCalledWith('/enrollments/catalog', { params: { page: 1, pageSize: 20 } });
  });

  it('fetchMyEnrollments defaults to the active status and forwards overrides', async () => {
    await fetchMyEnrollments();
    expect(api.get).toHaveBeenCalledWith('/enrollments/mine', { params: { status: 'active' } });

    await fetchMyEnrollments({ status: 'finished' });
    expect(api.get).toHaveBeenLastCalledWith('/enrollments/mine', { params: { status: 'finished' } });
  });

  it('enroll normalises an empty promo code to null', async () => {
    await enroll(5);
    expect(api.post).toHaveBeenCalledWith('/enrollments', { courseId: 5, promoCode: null });

    await enroll(5, 'SAVE10');
    expect(api.post).toHaveBeenLastCalledWith('/enrollments', { courseId: 5, promoCode: 'SAVE10' });
  });

  it('validatePromoCode passes the code and course as query params', async () => {
    await validatePromoCode('SAVE10', 5);
    expect(api.get).toHaveBeenCalledWith('/enrollments/promo-codes/validate', {
      params: { code: 'SAVE10', courseId: 5 },
    });
  });

  it.each([
    ['fetchCourseCoverImageObjectUrl', () => fetchCourseCoverImageObjectUrl(5), '/enrollments/courses/5/cover/image'],
    ['fetchEnrollmentLessonMediaObjectUrl', () => fetchEnrollmentLessonMediaObjectUrl(7, 3, 9),
      '/enrollments/7/lessons/3/parts/9/media'],
    ['fetchEnrollmentLessonPartFileObjectUrl', () => fetchEnrollmentLessonPartFileObjectUrl(7, 3, 9, 4),
      '/enrollments/7/lessons/3/parts/9/files/4/media'],
    ['fetchCoursePreviewLessonMediaObjectUrl', () => fetchCoursePreviewLessonMediaObjectUrl(5, 3, 9),
      '/enrollments/catalog/5/lessons/3/parts/9/media'],
    ['fetchCoursePreviewLessonPartFileObjectUrl', () => fetchCoursePreviewLessonPartFileObjectUrl(5, 3, 9, 4),
      '/enrollments/catalog/5/lessons/3/parts/9/files/4/media'],
  ])('%s blob-fetches %s and returns an object URL', async (_name, call, url) => {
    const blob = new Blob(['b']);
    api.get.mockResolvedValue({ data: blob });

    await expect(call()).resolves.toBe('blob:media');
    expect(api.get).toHaveBeenCalledWith(url, { responseType: 'blob' });
    expect(URL.createObjectURL).toHaveBeenCalledWith(blob);
  });
});
