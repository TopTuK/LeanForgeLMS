import { vi } from 'vitest';
import { render } from '@testing-library/vue';
import { createTestingPinia } from '@pinia/testing';
import { i18n } from '@/i18n';

i18n.global.locale.value = 'en';

/**
 * Renders a component with the app's real i18n instance installed, plus an
 * optional testing Pinia and stubbed router-link. Extra options are forwarded
 * to Testing Library's `render`.
 */
export function renderComponent(component, { props, slots, attrs, pinia = false, global: globalOverrides = {}, ...rest } = {}) {
  const plugins = [i18n];
  if (pinia) {
    plugins.push(pinia === true ? createTestingPinia({ createSpy: vi.fn }) : pinia);
  }

  return render(component, {
    props,
    slots,
    attrs,
    global: {
      plugins,
      stubs: { RouterLink: true, RouterView: true },
      ...globalOverrides,
    },
    ...rest,
  });
}
