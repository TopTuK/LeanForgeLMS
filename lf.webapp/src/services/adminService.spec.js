import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import api from '@/services/api';
import {
  fetchUsers,
  updateUserInfo,
  updateUserRole,
  deleteUser,
  fetchAdminCategories,
  createCategory,
  deleteCategory,
  fetchPromoCodes,
  createPromoCode,
  deactivatePromoCode,
  fetchPayments,
  downloadPaymentsCsv,
  fetchAdminCourses,
  fetchCourseEnrollments,
  enrollStudent,
  removeEnrollment,
  deleteCourse,
  unpublishCourse,
  fetchAdminNews,
  fetchAdminNewsPost,
  createNewsPost,
  updateNewsPost,
  deleteNewsPost,
  uploadNewsImage,
} from '@/services/adminService';

describe('adminService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    for (const m of Object.values(api)) m.mockResolvedValue({ data: undefined });
  });

  describe('users', () => {
    it('fetchUsers sends default paging and omits an empty search', async () => {
      api.get.mockResolvedValue({ data: { items: [] } });
      await fetchUsers();
      expect(api.get).toHaveBeenCalledWith('/admin/users', {
        params: { page: 1, pageSize: 20, search: undefined },
      });
    });

    it('fetchUsers forwards an explicit search term', async () => {
      await fetchUsers({ page: 2, pageSize: 5, search: 'ada' });
      expect(api.get).toHaveBeenCalledWith('/admin/users', {
        params: { page: 2, pageSize: 5, search: 'ada' },
      });
    });

    it('updateUserInfo PUTs the payload to the user route', async () => {
      await updateUserInfo(9, { firstName: 'A' });
      expect(api.put).toHaveBeenCalledWith('/admin/users/9', { firstName: 'A' });
    });

    it('updateUserRole PUTs the role wrapped in an object', async () => {
      await updateUserRole(9, 'Admin');
      expect(api.put).toHaveBeenCalledWith('/admin/users/9/role', { role: 'Admin' });
    });

    it('deleteUser DELETEs the user route', async () => {
      await deleteUser(9);
      expect(api.delete).toHaveBeenCalledWith('/admin/users/9');
    });
  });

  describe('categories', () => {
    it('fetchAdminCategories GETs the categories route', async () => {
      api.get.mockResolvedValue({ data: [] });
      await fetchAdminCategories();
      expect(api.get).toHaveBeenCalledWith('/admin/categories');
    });

    it('createCategory POSTs the name wrapped in an object', async () => {
      await createCategory('Backend');
      expect(api.post).toHaveBeenCalledWith('/admin/categories', { name: 'Backend' });
    });

    it('deleteCategory DELETEs the category route', async () => {
      await deleteCategory(3);
      expect(api.delete).toHaveBeenCalledWith('/admin/categories/3');
    });
  });

  describe('promo codes', () => {
    it('fetchPromoCodes sends default paging', async () => {
      api.get.mockResolvedValue({ data: { items: [] } });
      await fetchPromoCodes();
      expect(api.get).toHaveBeenCalledWith('/admin/promo-codes', { params: { page: 1, pageSize: 50 } });
    });

    it('createPromoCode POSTs the payload', async () => {
      await createPromoCode({ code: 'SAVE10', discountPercent: 10 });
      expect(api.post).toHaveBeenCalledWith('/admin/promo-codes', { code: 'SAVE10', discountPercent: 10 });
    });

    it('deactivatePromoCode POSTs to the deactivate route', async () => {
      await deactivatePromoCode(7);
      expect(api.post).toHaveBeenCalledWith('/admin/promo-codes/7/deactivate');
    });
  });

  describe('courses', () => {
    it('fetchAdminCourses sends default paging', async () => {
      api.get.mockResolvedValue({ data: { items: [] } });
      await fetchAdminCourses();
      expect(api.get).toHaveBeenCalledWith('/admin/courses', { params: { page: 1, pageSize: 20 } });
    });

    it('fetchCourseEnrollments GETs the course roster route', async () => {
      api.get.mockResolvedValue({ data: { items: [] } });
      await fetchCourseEnrollments(5, { page: 2, pageSize: 100 });
      expect(api.get).toHaveBeenCalledWith('/admin/courses/5/enrollments', {
        params: { page: 2, pageSize: 100 },
      });
    });

    it('enrollStudent POSTs to the admin course route, not the authoring one', async () => {
      await enrollStudent(5, 9);
      expect(api.post).toHaveBeenCalledWith('/admin/courses/5/enrollments', { userId: 9 });
    });

    it('removeEnrollment DELETEs the enrollment route', async () => {
      await removeEnrollment(5, 10);
      expect(api.delete).toHaveBeenCalledWith('/admin/courses/5/enrollments/10');
    });

    it('deleteCourse omits force by default', async () => {
      await deleteCourse(5);
      expect(api.delete).toHaveBeenCalledWith('/admin/courses/5', { params: { force: undefined } });
    });

    it('deleteCourse forwards force when the admin acknowledged paid students', async () => {
      await deleteCourse(5, { force: true });
      expect(api.delete).toHaveBeenCalledWith('/admin/courses/5', { params: { force: true } });
    });

    it('unpublishCourse posts to the unpublish endpoint', async () => {
      await unpublishCourse(5);
      expect(api.post).toHaveBeenCalledWith('/admin/courses/5/unpublish');
    });
  });

  describe('payments', () => {
    it('fetchPayments sends default paging and omits empty dates', async () => {
      api.get.mockResolvedValue({ data: { items: [] } });
      await fetchPayments();
      expect(api.get).toHaveBeenCalledWith('/admin/payments', {
        params: { page: 1, pageSize: 20, from: undefined, to: undefined },
      });
    });

    it('downloadPaymentsCsv requests a blob and triggers a download', async () => {
      const clickSpy = vi.fn();
      const anchor = { href: '', download: '', click: clickSpy, remove: vi.fn() };
      vi.spyOn(document, 'createElement').mockReturnValue(anchor);
      vi.spyOn(document.body, 'appendChild').mockImplementation(() => {});
      api.get.mockResolvedValue({
        data: new Blob(['a;b']),
        headers: { 'content-disposition': 'attachment; filename="course-payments-2026-09-06.csv"' },
      });

      await downloadPaymentsCsv({ from: '2026-01-01' });

      expect(api.get).toHaveBeenCalledWith('/admin/payments/report.csv', {
        params: { from: '2026-01-01', to: undefined },
        responseType: 'blob',
      });
      expect(anchor.download).toBe('course-payments-2026-09-06.csv');
      expect(clickSpy).toHaveBeenCalled();
      document.createElement.mockRestore();
      document.body.appendChild.mockRestore();
    });
  });

  describe('news', () => {
    it('fetchAdminNews sends default paging', async () => {
      api.get.mockResolvedValue({ data: { items: [], totalCount: 0 } });

      const result = await fetchAdminNews();

      expect(api.get).toHaveBeenCalledWith('/admin/news', { params: { page: 1, pageSize: 20 } });
      expect(result).toEqual({ items: [], totalCount: 0 });
    });

    it('fetchAdminNewsPost requests one post', async () => {
      await fetchAdminNewsPost(7);
      expect(api.get).toHaveBeenCalledWith('/admin/news/7');
    });

    it('createNewsPost and updateNewsPost send the payload', async () => {
      const payload = { title: 'Launch', html: '<p>Hi</p>', visibility: 'Public', isPublished: true, imageStorageObjectIds: [3, 1] };

      await createNewsPost(payload);
      await updateNewsPost(7, payload);

      expect(api.post).toHaveBeenCalledWith('/admin/news', payload);
      expect(api.put).toHaveBeenCalledWith('/admin/news/7', payload);
    });

    it('deleteNewsPost deletes by id', async () => {
      await deleteNewsPost(7);
      expect(api.delete).toHaveBeenCalledWith('/admin/news/7');
    });

    it('uploadNewsImage posts the file as multipart form data', async () => {
      api.post.mockResolvedValue({ data: { storageObjectId: 11 } });
      const file = new File(['x'], 'a.png', { type: 'image/png' });

      const result = await uploadNewsImage(file);

      const [url, body] = api.post.mock.calls[0];
      expect(url).toBe('/admin/news/images');
      expect(body).toBeInstanceOf(FormData);
      expect(body.get('file')).toBe(file);
      expect(result).toEqual({ storageObjectId: 11 });
    });
  });
});
