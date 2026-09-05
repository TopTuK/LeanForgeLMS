import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import AdminPaymentsView from '@/views/admin/AdminPaymentsView.vue';
import { fetchPayments, downloadPaymentsCsv } from '@/services/adminService';

vi.mock('@/services/adminService', () => ({
  fetchPayments: vi.fn(),
  downloadPaymentsCsv: vi.fn(),
}));

const sampleRow = {
  id: 1,
  paymentOrderId: 50,
  paidAt: '2026-09-05T12:00:00Z',
  studentName: 'Ann Lee',
  studentEmail: 'ann@pmi.moscow',
  courseTitle: 'Async in C#',
  amount: 1990,
  promoCode: null,
  provider: 'Robokassa',
  providerOperationId: 'op-50',
};

describe('AdminPaymentsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchPayments.mockResolvedValue({ items: [sampleRow], totalCount: 1, totalAmount: 1990 });
    downloadPaymentsCsv.mockResolvedValue(undefined);
  });

  it('renders payment rows from the service', async () => {
    renderComponent(AdminPaymentsView);

    expect(await screen.findByText('Ann Lee')).toBeInTheDocument();
    expect(screen.getByText('Async in C#')).toBeInTheDocument();
    expect(screen.getByText('op-50')).toBeInTheDocument();
  });

  it('downloads the CSV report', async () => {
    const user = userEvent.setup();
    renderComponent(AdminPaymentsView);

    await screen.findByText('Ann Lee');
    await user.click(screen.getByRole('button', { name: /download csv/i }));

    await waitFor(() => expect(downloadPaymentsCsv).toHaveBeenCalled());
  });
});
