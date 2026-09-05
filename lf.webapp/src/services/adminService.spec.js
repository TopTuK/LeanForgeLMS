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
  fetchPlatformSettings,
  updateStudentEnrollment,
  fetchPayments,
  downloadPaymentsCsv,
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

  describe('platform settings', () => {
    it('fetchPlatformSettings GETs the platform-settings route', async () => {
      api.get.mockResolvedValue({ data: { studentEnrollmentEnabled: false } });
      await fetchPlatformSettings();
      expect(api.get).toHaveBeenCalledWith('/admin/platform-settings');
    });

    it('updateStudentEnrollment PUTs the flag wrapped in an object', async () => {
      await updateStudentEnrollment(true);
      expect(api.put).toHaveBeenCalledWith('/admin/platform-settings/student-enrollment', { enabled: true });
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
});
