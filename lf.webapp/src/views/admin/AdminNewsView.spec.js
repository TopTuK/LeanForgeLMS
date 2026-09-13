import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor, within } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import AdminNewsView from '@/views/admin/AdminNewsView.vue';
import { fetchAdminNews, deleteNewsPost } from '@/services/adminService';

const routerPush = vi.fn();
vi.mock('vue-router', () => ({
  useRouter: () => ({ push: routerPush }),
}));

vi.mock('@/services/adminService', () => ({
  fetchAdminNews: vi.fn(),
  deleteNewsPost: vi.fn(),
}));

const publishedPost = {
  id: 5,
  title: 'Platform launch',
  html: '<p>We are live</p>',
  visibility: 'MembersOnly',
  isPublished: true,
  publishedAt: '2026-09-10T10:00:00Z',
  createdAt: '2026-09-09T10:00:00Z',
  updatedAt: '2026-09-10T10:00:00Z',
  images: [{ id: 1, storageObjectId: 11, url: '/api/news/5/images/1' }],
};

const draftPost = {
  ...publishedPost,
  id: 6,
  title: 'Upcoming course',
  visibility: 'Public',
  isPublished: false,
  publishedAt: null,
  images: [],
};

describe('AdminNewsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchAdminNews.mockResolvedValue({ items: [publishedPost, draftPost], totalCount: 2 });
    deleteNewsPost.mockResolvedValue({});
  });

  it('lists posts with their audience and status', async () => {
    renderComponent(AdminNewsView);

    const publishedRow = (await screen.findByText('Platform launch')).closest('tr');
    expect(within(publishedRow).getByText('Members only')).toBeInTheDocument();
    expect(within(publishedRow).getByText('Published')).toBeInTheDocument();

    const draftRow = screen.getByText('Upcoming course').closest('tr');
    expect(within(draftRow).getByText('Everyone')).toBeInTheDocument();
    expect(within(draftRow).getByText('Draft')).toBeInTheDocument();
  });

  it('opens the editor for a new post and for an existing one', async () => {
    const user = userEvent.setup();
    renderComponent(AdminNewsView);
    const row = (await screen.findByText('Platform launch')).closest('tr');

    await user.click(screen.getByRole('button', { name: /new post/i }));
    expect(routerPush).toHaveBeenCalledWith({ name: 'AdminNewsCreate' });

    await user.click(within(row).getByRole('button', { name: 'Edit' }));
    expect(routerPush).toHaveBeenCalledWith({ name: 'AdminNewsEdit', params: { id: 5 } });
  });

  it('deletes a post after confirmation and reloads the list', async () => {
    const user = userEvent.setup();
    renderComponent(AdminNewsView);
    const row = (await screen.findByText('Platform launch')).closest('tr');

    await user.click(within(row).getByRole('button', { name: 'Delete' }));
    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent('Platform launch');

    await user.click(within(dialog).getByRole('button', { name: 'Delete' }));

    expect(deleteNewsPost).toHaveBeenCalledWith(5);
    await waitFor(() => expect(fetchAdminNews).toHaveBeenCalledTimes(2));
  });

  it('shows an error when the list fails to load', async () => {
    fetchAdminNews.mockRejectedValue(new Error('offline'));

    renderComponent(AdminNewsView);

    expect(await screen.findByText(/could not load news posts/i)).toBeInTheDocument();
  });
});
