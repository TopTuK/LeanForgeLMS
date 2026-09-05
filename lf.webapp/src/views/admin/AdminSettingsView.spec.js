import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import AdminSettingsView from '@/views/admin/AdminSettingsView.vue';
import { fetchPlatformSettings, updateStudentEnrollment } from '@/services/adminService';

vi.mock('@/services/adminService', () => ({
  fetchPlatformSettings: vi.fn(),
  updateStudentEnrollment: vi.fn(),
}));

describe('AdminSettingsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchPlatformSettings.mockResolvedValue({ studentEnrollmentEnabled: false, updatedAt: '2026-09-01T10:00:00Z' });
    updateStudentEnrollment.mockResolvedValue({ studentEnrollmentEnabled: true, updatedAt: '2026-09-06T10:00:00Z' });
  });

  it('renders the fetched toggle state', async () => {
    renderComponent(AdminSettingsView);

    const checkbox = await screen.findByRole('checkbox');
    expect(checkbox.checked).toBe(false);
  });

  it('toggling the checkbox updates its state', async () => {
    const user = userEvent.setup();
    renderComponent(AdminSettingsView);

    const checkbox = await screen.findByRole('checkbox');
    await user.click(checkbox);

    expect(checkbox.checked).toBe(true);
  });

  it('saves the current value', async () => {
    const user = userEvent.setup();
    renderComponent(AdminSettingsView);

    const saveButton = await screen.findByRole('button', { name: /save/i });
    await user.click(saveButton);

    await waitFor(() => expect(updateStudentEnrollment).toHaveBeenCalledWith(false));
  });
});
