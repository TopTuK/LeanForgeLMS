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

    expect(getByRole('heading', { name: /two programs to start/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /self-paced, on one platform/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /^questions$/i })).toBeInTheDocument();
  });

  it('points the hero primary call to action at the programs section', () => {
    const { getByRole } = renderComponent(HomeView);

    expect(getByRole('link', { name: /see the programs/i })).toHaveAttribute('href', '#programs');
  });

  it('renders both starting programs and filters them by category', async () => {
    const { getByRole, queryByRole } = renderComponent(HomeView);

    expect(getByRole('heading', { name: /introduction to llm/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /introduction to kanban/i })).toBeInTheDocument();

    await userEvent.click(getByRole('button', { name: /^AI$/ }));

    expect(getByRole('heading', { name: /introduction to llm/i })).toBeInTheDocument();
    expect(queryByRole('heading', { name: /introduction to kanban/i })).not.toBeInTheDocument();
  });

  it('expands a FAQ entry on click', async () => {
    const { getByRole } = renderComponent(HomeView);

    const trigger = getByRole('button', { name: /who is this school for/i });
    expect(trigger).toHaveAttribute('aria-expanded', 'false');

    await userEvent.click(trigger);

    expect(trigger).toHaveAttribute('aria-expanded', 'true');
  });
});
