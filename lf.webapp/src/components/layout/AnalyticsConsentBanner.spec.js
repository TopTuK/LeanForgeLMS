import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';

vi.mock('vue-gtag', () => ({
  addGtag: vi.fn(),
  configure: vi.fn(),
  event: vi.fn(),
  optIn: vi.fn(),
  optOut: vi.fn(),
  set: vi.fn(),
}));

const STORAGE_KEY = 'leanforge-analytics-consent';

// analytics.js keeps consent in module state, so each test gets a fresh copy.
async function renderBanner() {
  vi.resetModules();
  const { default: AnalyticsConsentBanner } = await import('@/components/layout/AnalyticsConsentBanner.vue');
  const gtag = await import('vue-gtag');
  return { ...renderComponent(AnalyticsConsentBanner), gtag };
}

describe('AnalyticsConsentBanner', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.removeItem(STORAGE_KEY);
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('is not shown outside a production build', async () => {
    const { queryByRole } = await renderBanner();

    expect(queryByRole('region', { name: 'Analytics cookies' })).not.toBeInTheDocument();
  });

  describe('in a production build', () => {
    beforeEach(() => {
      vi.stubEnv('PROD', true);
    });

    it('asks for consent when no choice has been made', async () => {
      const { getByRole } = await renderBanner();

      expect(getByRole('region', { name: 'Analytics cookies' })).toBeInTheDocument();
      expect(getByRole('button', { name: 'Accept' })).toBeInTheDocument();
      expect(getByRole('button', { name: 'Decline' })).toBeInTheDocument();
    });

    it('stores an accept, loads analytics and hides itself', async () => {
      const user = userEvent.setup();
      const { getByRole, queryByRole, gtag } = await renderBanner();

      await user.click(getByRole('button', { name: 'Accept' }));

      expect(window.localStorage.getItem(STORAGE_KEY)).toBe('granted');
      expect(gtag.addGtag).toHaveBeenCalledTimes(1);
      expect(queryByRole('region', { name: 'Analytics cookies' })).not.toBeInTheDocument();
    });

    it('stores a decline without loading analytics and hides itself', async () => {
      const user = userEvent.setup();
      const { getByRole, queryByRole, gtag } = await renderBanner();

      await user.click(getByRole('button', { name: 'Decline' }));

      expect(window.localStorage.getItem(STORAGE_KEY)).toBe('denied');
      expect(gtag.addGtag).not.toHaveBeenCalled();
      expect(queryByRole('region', { name: 'Analytics cookies' })).not.toBeInTheDocument();
    });

    it('stays hidden once a choice exists', async () => {
      window.localStorage.setItem(STORAGE_KEY, 'denied');
      const { queryByRole } = await renderBanner();

      expect(queryByRole('region', { name: 'Analytics cookies' })).not.toBeInTheDocument();
    });
  });
});
