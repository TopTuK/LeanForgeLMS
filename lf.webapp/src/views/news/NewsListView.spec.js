import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import NewsListView from '@/views/news/NewsListView.vue';
import { fetchPublicNews } from '@/services/newsService';

vi.mock('@/services/newsService', () => ({
  fetchPublicNews: vi.fn(),
}));

function post(id) {
  return {
    id,
    title: `Post ${id}`,
    html: `<p>Summary of post ${id}</p>`,
    visibility: 'Public',
    publishedAt: '2026-09-10T10:00:00Z',
    images: [],
  };
}

describe('NewsListView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders a card per public post', async () => {
    fetchPublicNews.mockResolvedValue({ items: [post(1), post(2)], totalCount: 2 });

    renderComponent(NewsListView, { pinia: true });

    expect(await screen.findByText('Summary of post 1')).toBeInTheDocument();
    expect(screen.getByText('Summary of post 2')).toBeInTheDocument();
    expect(fetchPublicNews).toHaveBeenCalledWith({ page: 1, pageSize: 9 });
    expect(screen.queryByRole('button', { name: 'Show more' })).toBeNull();
  });

  it('appends the next page when asked', async () => {
    fetchPublicNews
      .mockResolvedValueOnce({ items: [post(1)], totalCount: 2 })
      .mockResolvedValueOnce({ items: [post(2)], totalCount: 2 });
    const user = userEvent.setup();

    renderComponent(NewsListView, { pinia: true });
    await screen.findByText('Summary of post 1');

    await user.click(screen.getByRole('button', { name: 'Show more' }));

    expect(await screen.findByText('Summary of post 2')).toBeInTheDocument();
    expect(screen.getByText('Summary of post 1')).toBeInTheDocument();
    expect(fetchPublicNews).toHaveBeenLastCalledWith({ page: 2, pageSize: 9 });
  });

  it('shows the empty state', async () => {
    fetchPublicNews.mockResolvedValue({ items: [], totalCount: 0 });

    renderComponent(NewsListView, { pinia: true });

    expect(await screen.findByText(/no news yet/i)).toBeInTheDocument();
  });

  it('shows an error when loading fails', async () => {
    fetchPublicNews.mockRejectedValue(new Error('offline'));

    renderComponent(NewsListView, { pinia: true });

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not load news/i);
  });
});
