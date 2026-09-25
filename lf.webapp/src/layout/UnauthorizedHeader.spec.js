import { describe, it, expect } from 'vitest';
import UnauthorizedHeader from '@/layout/UnauthorizedHeader.vue';
import { renderComponent } from '@/test/renderComponent';

describe('UnauthorizedHeader', () => {
  it('renders the section anchor navigation', () => {
    const { getAllByRole } = renderComponent(UnauthorizedHeader, { pinia: true });

    const hrefs = getAllByRole('link')
      .map((a) => a.getAttribute('href'))
      .filter((href) => href?.startsWith('#'));

    expect(hrefs).toEqual(expect.arrayContaining(['#audience', '#approach', '#faq']));
  });

  it('renders the mobile menu toggle', () => {
    const { getByRole } = renderComponent(UnauthorizedHeader, { pinia: true });

    expect(getByRole('button', { name: /toggle menu/i })).toBeInTheDocument();
  });
});
