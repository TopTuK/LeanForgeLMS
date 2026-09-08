import { describe, it, expect } from 'vitest';
import PublicFooter from '@/layout/PublicFooter.vue';
import FooterLayout from '@/layout/FooterLayout.vue';
import { renderComponent } from '@/test/renderComponent';

const RouterLinkStub = {
  props: ['to'],
  template: '<a href="#" :data-route-name="to.name"><slot /></a>',
};

describe.each([
  ['public footer', PublicFooter],
  ['authorized footer', FooterLayout],
])('%s cookie policy link', (_, component) => {
  it('points to the Cookies route', () => {
    const { getByRole } = renderComponent(component, {
      global: {
        stubs: { RouterLink: RouterLinkStub },
      },
    });

    expect(getByRole('link', { name: 'Cookie policy' }))
      .toHaveAttribute('data-route-name', 'Cookies');
  });
});
