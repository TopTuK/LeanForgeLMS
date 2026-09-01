import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/vue';

const routerPush = vi.fn();
const routerReplace = vi.fn();
let routeQuery = {};

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: routerPush, replace: routerReplace }),
  useRoute: () => ({ query: routeQuery }),
}));

vi.mock('@/services/paymentService', () => ({
  fetchPaymentOrder: vi.fn(),
}));

import { fetchPaymentOrder } from '@/services/paymentService';
import { renderComponent } from '@/test/renderComponent';
import PaymentResultView from '@/views/payments/PaymentResultView.vue';

describe('PaymentResultView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    routeQuery = { InvId: '42' };
  });

  it('shows the failure message without polling on the fail outcome', () => {
    renderComponent(PaymentResultView, { props: { outcome: 'fail' } });

    expect(screen.getByText('Payment was not completed')).toBeInTheDocument();
    expect(fetchPaymentOrder).not.toHaveBeenCalled();
  });

  it('redirects to the course once the order reports Paid', async () => {
    fetchPaymentOrder.mockResolvedValue({ id: 42, enrollmentId: 7, status: 'Paid' });

    renderComponent(PaymentResultView, { props: { outcome: 'success' } });

    await waitFor(() => expect(routerReplace).toHaveBeenCalledWith({
      name: 'CourseLearn',
      params: { enrollmentId: 7 },
    }));
  });

  it('shows an error when there is no InvId', async () => {
    routeQuery = {};

    renderComponent(PaymentResultView, { props: { outcome: 'success' } });

    await waitFor(() => expect(screen.getByText(/could not confirm this payment/i)).toBeInTheDocument());
    expect(fetchPaymentOrder).not.toHaveBeenCalled();
  });
});
