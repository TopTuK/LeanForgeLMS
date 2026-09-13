import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor, within } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { createTestingPinia } from '@pinia/testing';
import { renderComponent } from '@/test/renderComponent';
import NotificationsView from '@/views/NotificationsView.vue';
import { fetchNotifications } from '@/services/newsService';
import { useNotificationStore } from '@/stores/notificationStore';

vi.mock('@/services/newsService', () => ({
  fetchNotifications: vi.fn(),
  fetchUnreadNotificationCount: vi.fn(),
  markNotificationsSeen: vi.fn(),
}));

function post(id, publishedAt, extra = {}) {
  return {
    id,
    title: `Post ${id}`,
    html: `<p>Body ${id}</p>`,
    visibility: 'Public',
    publishedAt,
    images: [],
    ...extra,
  };
}

function renderView() {
  const pinia = createTestingPinia({ createSpy: vi.fn });
  renderComponent(NotificationsView, { pinia });
  return useNotificationStore(pinia);
}

describe('NotificationsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the feed, tags posts published since the last visit and marks everything seen', async () => {
    fetchNotifications.mockResolvedValue({
      items: [
        post(2, '2026-09-12T10:00:00Z', { visibility: 'MembersOnly' }),
        post(1, '2026-09-01T10:00:00Z'),
      ],
      totalCount: 2,
      lastSeenAt: '2026-09-05T00:00:00Z',
    });

    const store = renderView();

    const newer = await screen.findByRole('article', { name: 'Post 2' });
    expect(within(newer).getByText('New')).toBeInTheDocument();
    expect(within(newer).getByText('Members only')).toBeInTheDocument();
    expect(within(newer).getByText('Body 2')).toBeInTheDocument();

    const older = screen.getByRole('article', { name: 'Post 1' });
    expect(within(older).queryByText('New')).toBeNull();

    await waitFor(() => expect(store.markAllSeen).toHaveBeenCalledTimes(1));
  });

  it('treats every post as new on the first visit', async () => {
    fetchNotifications.mockResolvedValue({ items: [post(1, '2026-09-01T10:00:00Z')], totalCount: 1, lastSeenAt: null });

    renderView();

    const article = await screen.findByRole('article', { name: 'Post 1' });
    expect(within(article).getByText('New')).toBeInTheDocument();
  });

  it('shows the empty state', async () => {
    fetchNotifications.mockResolvedValue({ items: [], totalCount: 0, lastSeenAt: null });

    renderView();

    expect(await screen.findByText(/nothing here yet/i)).toBeInTheDocument();
  });

  it('shows an error when the feed fails to load', async () => {
    fetchNotifications.mockRejectedValue(new Error('offline'));

    const store = renderView();

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not load notifications/i);
    expect(store.markAllSeen).not.toHaveBeenCalled();
  });

  it('loads the next page on demand without marking the feed seen twice', async () => {
    fetchNotifications
      .mockResolvedValueOnce({ items: [post(3, '2026-09-12T10:00:00Z')], totalCount: 2, lastSeenAt: null })
      .mockResolvedValueOnce({ items: [post(2, '2026-09-10T10:00:00Z')], totalCount: 2, lastSeenAt: '2026-09-13T00:00:00Z' });
    const user = userEvent.setup();

    const store = renderView();
    await screen.findByRole('article', { name: 'Post 3' });

    await user.click(screen.getByRole('button', { name: 'Show more' }));

    expect(await screen.findByRole('article', { name: 'Post 2' })).toBeInTheDocument();
    expect(fetchNotifications).toHaveBeenLastCalledWith({ page: 2, pageSize: 10 });
    expect(store.markAllSeen).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole('button', { name: 'Show more' })).toBeNull();
  });
});
