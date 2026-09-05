import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import api from '@/services/api';
import { createCheckout, fetchPaymentOrder } from '@/services/paymentService';

describe('paymentService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    for (const m of Object.values(api)) m.mockResolvedValue({ data: 'RESULT' });
  });

  it('createCheckout normalises an empty promo code to null', async () => {
    await createCheckout(5);
    expect(api.post).toHaveBeenCalledWith('/payments/checkout', { courseId: 5, promoCode: null });

    await createCheckout(5, 'SAVE10');
    expect(api.post).toHaveBeenLastCalledWith('/payments/checkout', { courseId: 5, promoCode: 'SAVE10' });
  });

  it('fetchPaymentOrder hits the order endpoint and unwraps data', async () => {
    await expect(fetchPaymentOrder(42)).resolves.toBe('RESULT');
    expect(api.get).toHaveBeenCalledWith('/payments/orders/42');
  });
});
