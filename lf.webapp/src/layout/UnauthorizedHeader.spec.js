import { describe, it, expect } from 'vitest';
import UnauthorizedHeader from '@/layout/UnauthorizedHeader.vue';
import { renderComponent } from '@/test/renderComponent';

describe('UnauthorizedHeader', () => {
  it('renders the section anchor navigation', () => {
    const { getAllByRole } = renderComponent(UnauthorizedHeader);

    const hrefs = getAllByRole('link')
      .map((a) => a.getAttribute('href'))
      .filter((href) => href?.startsWith('#'));

    expect(hrefs).toEqual(expect.arrayContaining(['#programs', '#approach', '#faq']));
  });

  it('renders the mobile menu toggle', () => {
    const { getByRole } = renderComponent(UnauthorizedHeader);

    expect(getByRole('button', { name: /toggle menu/i })).toBeInTheDocument();
  });
});
