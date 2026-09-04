import { describe, it, expect } from 'vitest';
import userEvent from '@testing-library/user-event';
import HomeView from '@/views/HomeView.vue';
import { renderComponent } from '@/test/renderComponent';

describe('HomeView', () => {
  it('renders a single top-level heading and the main section headings', () => {
    const { getAllByRole, getByRole } = renderComponent(HomeView);

    const h1s = getAllByRole('heading', { level: 1 });
    expect(h1s).toHaveLength(1);
    expect(h1s[0]).toHaveTextContent(/skill set/i);

    expect(getByRole('heading', { name: /built for people who run the work/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /self-paced, on one platform/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /^questions$/i })).toBeInTheDocument();
  });

  it('points the hero primary call to action at the audience section', () => {
    const { getByRole } = renderComponent(HomeView);

    expect(getByRole('link', { name: /see who it's for/i })).toHaveAttribute('href', '#audience');
  });

  it('states this is Sergey Sidorov\'s personal project and links to his site', () => {
    const { getByRole, getAllByRole } = renderComponent(HomeView);

    const bylineLink = getByRole('link', { name: /personal project by sergey sidorov/i });
    expect(bylineLink).toHaveAttribute('href', 'https://s-sidorov.ru');

    const siteLinks = getAllByRole('link', { name: /read full bio/i });
    expect(siteLinks[0]).toHaveAttribute('href', 'https://s-sidorov.ru');
  });

  it('renders the audience cards for the roles this platform serves', () => {
    const { getByRole } = renderComponent(HomeView);

    expect(getByRole('heading', { name: 'Project managers' })).toBeInTheDocument();
    expect(getByRole('heading', { name: 'Product managers' })).toBeInTheDocument();
    expect(getByRole('heading', { name: 'Product team members' })).toBeInTheDocument();
  });

  it('expands a FAQ entry on click', async () => {
    const { getByRole } = renderComponent(HomeView);

    const trigger = getByRole('button', { name: /who is this for/i });
    expect(trigger).toHaveAttribute('aria-expanded', 'false');

    await userEvent.click(trigger);

    expect(trigger).toHaveAttribute('aria-expanded', 'true');
  });
});
