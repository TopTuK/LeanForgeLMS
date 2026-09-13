import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import HomeView from '@/views/HomeView.vue';
import { renderComponent } from '@/test/renderComponent';
import { fetchPublicNews } from '@/services/newsService';

vi.mock('@/services/newsService', () => ({
  fetchPublicNews: vi.fn(),
}));

describe('HomeView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchPublicNews.mockResolvedValue({ items: [], totalCount: 0 });
  });

  it('shows the latest public news when there is any', async () => {
    fetchPublicNews.mockResolvedValue({
      items: [{
        id: 1,
        title: 'Platform launch',
        html: '<p>We are live</p>',
        visibility: 'Public',
        publishedAt: '2026-09-10T10:00:00Z',
        images: [],
      }],
      totalCount: 1,
    });

    renderComponent(HomeView);

    expect(await screen.findByRole('heading', { name: /what's new at the school/i })).toBeInTheDocument();
    expect(screen.getByText('We are live')).toBeInTheDocument();
    expect(fetchPublicNews).toHaveBeenCalledWith({ page: 1, pageSize: 3 });
  });

  it('hides the news block when there is no public news', async () => {
    renderComponent(HomeView);

    await waitFor(() => expect(fetchPublicNews).toHaveBeenCalled());
    expect(screen.queryByRole('heading', { name: /what's new at the school/i })).toBeNull();
  });

  it('hides the news block when the feed fails to load', async () => {
    fetchPublicNews.mockRejectedValue(new Error('offline'));

    renderComponent(HomeView);

    await waitFor(() => expect(fetchPublicNews).toHaveBeenCalled());
    expect(screen.queryByRole('heading', { name: /what's new at the school/i })).toBeNull();
  });

  it('renders a single top-level heading and the main section headings', () => {
    const { getAllByRole, getByRole } = renderComponent(HomeView);

    const h1s = getAllByRole('heading', { level: 1 });
    expect(h1s).toHaveLength(1);
    expect(h1s[0]).toHaveTextContent(/advanced technology is indistinguishable from magic/i);
    expect(getByRole('heading', { level: 1 })).toHaveAccessibleName(/advanced technology/i);

    expect(getByRole('heading', { name: /built for people who run the work/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /self-paced, on one platform/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /^questions$/i })).toBeInTheDocument();
  });

  it('points the hero primary call to action at the audience section', () => {
    const { getByRole } = renderComponent(HomeView);

    expect(getByRole('link', { name: /find your direction/i })).toHaveAttribute('href', '#audience');
  });

  it('attributes the quote and links the independent school to Sergey Sidorov', () => {
    const { getByRole, getAllByRole, getByText } = renderComponent(HomeView);

    expect(getByText(/arthur c\. clarke/i)).toBeInTheDocument();

    const bylineLink = getByRole('link', { name: /independent school by sergey sidorov/i });
    expect(bylineLink).toHaveAttribute('href', 'https://s-sidorov.ru');

    const siteLinks = getAllByRole('link', { name: /read full bio/i });
    expect(siteLinks[0]).toHaveAttribute('href', 'https://s-sidorov.ru');
  });

  it('renders the self-paced learning steps', () => {
    const { getByRole } = renderComponent(HomeView);

    expect(getByRole('heading', { name: /work through chapters and lessons/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /track progress as you go/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /practise on real work/i })).toBeInTheDocument();
  });

  it('renders the audience cards for the roles this platform serves', () => {
    const { getByRole } = renderComponent(HomeView);

    expect(getByRole('heading', { name: 'Project managers' })).toBeInTheDocument();
    expect(getByRole('heading', { name: 'Product managers' })).toBeInTheDocument();
    expect(getByRole('heading', { name: 'Product team members' })).toBeInTheDocument();
  });

  it('describes the editorial landing images', () => {
    const { getByRole } = renderComponent(HomeView);

    expect(getByRole('img', { name: /shaping a workflow/i })).toBeInTheDocument();
    expect(getByRole('img', { name: /connecting a project card/i })).toBeInTheDocument();
    expect(getByRole('img', { name: /applying a self-paced lesson/i })).toBeInTheDocument();
    expect(getByRole('img', { name: /prepared learning workspace/i })).toBeInTheDocument();
  });

  it('expands a FAQ entry on click', async () => {
    const { getByRole } = renderComponent(HomeView);

    const trigger = getByRole('button', { name: /who is this for/i });
    expect(trigger).toHaveAttribute('aria-expanded', 'false');

    await userEvent.click(trigger);

    expect(trigger).toHaveAttribute('aria-expanded', 'true');
  });
});
