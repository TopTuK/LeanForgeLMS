import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('vue-gtag', () => ({
  addGtag: vi.fn(),
  configure: vi.fn(),
  event: vi.fn(),
  optIn: vi.fn(),
  optOut: vi.fn(),
  set: vi.fn(),
}));

const STORAGE_KEY = 'leanforge-analytics-consent';

async function loadModules() {
  vi.resetModules();
  const gtag = await import('vue-gtag');
  const analytics = await import('@/lib/analytics');
  return { gtag, analytics };
}

describe('analytics', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.removeItem(STORAGE_KEY);
    window.sessionStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  describe('outside a production build', () => {
    it('never configures gtag nor sends events, even with consent', async () => {
      window.localStorage.setItem(STORAGE_KEY, 'granted');
      const { gtag, analytics } = await loadModules();

      analytics.installAnalytics({});
      analytics.track('enroll_course');

      expect(gtag.configure).not.toHaveBeenCalled();
      expect(gtag.addGtag).not.toHaveBeenCalled();
      expect(gtag.event).not.toHaveBeenCalled();
    });
  });

  describe('in a production build', () => {
    beforeEach(() => {
      vi.stubEnv('PROD', true);
    });

    it('configures gtag in manual mode but does not load it before consent', async () => {
      const { gtag, analytics } = await loadModules();

      analytics.installAnalytics({});
      analytics.track('enroll_course');

      expect(gtag.configure).toHaveBeenCalledWith(expect.objectContaining({
        tagId: 'G-KZGF9HH7RL',
        initMode: 'manual',
      }));
      expect(gtag.addGtag).not.toHaveBeenCalled();
      expect(gtag.event).not.toHaveBeenCalled();
    });

    it('loads gtag on install when consent was granted earlier', async () => {
      window.localStorage.setItem(STORAGE_KEY, 'granted');
      const { gtag, analytics } = await loadModules();

      analytics.installAnalytics({});

      expect(gtag.addGtag).toHaveBeenCalledTimes(1);
    });

    it('persists a grant, loads gtag once and starts sending events', async () => {
      const { gtag, analytics } = await loadModules();
      analytics.installAnalytics({});

      analytics.grantConsent();
      analytics.grantConsent();
      analytics.track('lesson_complete', { lesson_id: 7 });

      expect(window.localStorage.getItem(STORAGE_KEY)).toBe('granted');
      expect(gtag.addGtag).toHaveBeenCalledTimes(1);
      expect(gtag.event).toHaveBeenCalledWith('lesson_complete', { lesson_id: 7 });
    });

    it('persists a denial and opts out if gtag was already loaded', async () => {
      window.localStorage.setItem(STORAGE_KEY, 'granted');
      const { gtag, analytics } = await loadModules();
      analytics.installAnalytics({});

      analytics.denyConsent();
      analytics.track('logout');

      expect(window.localStorage.getItem(STORAGE_KEY)).toBe('denied');
      expect(gtag.optOut).toHaveBeenCalled();
      expect(gtag.event).not.toHaveBeenCalled();
    });

    it('forgets the choice on reset', async () => {
      window.localStorage.setItem(STORAGE_KEY, 'denied');
      const { analytics } = await loadModules();
      const { consent } = analytics.useAnalyticsConsent();

      analytics.resetConsent();

      expect(consent.value).toBeNull();
      expect(window.localStorage.getItem(STORAGE_KEY)).toBeNull();
    });

    it('reports a sign-in only after the session is established', async () => {
      window.localStorage.setItem(STORAGE_KEY, 'granted');
      const { gtag, analytics } = await loadModules();

      analytics.trackLoginStarted('google');
      analytics.trackLoginCompleted();
      analytics.trackLoginCompleted();

      expect(gtag.event).toHaveBeenCalledWith('login_start', { method: 'google' });
      expect(gtag.event).toHaveBeenCalledWith('login', { method: 'google' });
      expect(gtag.event.mock.calls.filter(([name]) => name === 'login')).toHaveLength(1);
    });

    it('sends the role user property only once gtag is loaded', async () => {
      const { gtag, analytics } = await loadModules();
      analytics.installAnalytics({});

      analytics.setUserRole('Student');
      expect(gtag.set).not.toHaveBeenCalled();

      analytics.grantConsent();
      expect(gtag.set).toHaveBeenCalledWith('user_properties', { role: 'Student' });
    });
  });

  describe('pageViewParams', () => {
    it('drops the query string except campaign parameters', async () => {
      const { analytics } = await loadModules();

      const params = analytics.pageViewParams({
        name: 'PaymentSuccess',
        path: '/payments/success',
        query: { InvId: '42', OutSum: '1000', SignatureValue: 'abc', utm_source: 'tg' },
      });

      expect(params.page_path).toBe('/payments/success');
      expect(params.page_title).toBe('PaymentSuccess');
      expect(params.page_location).toBe(`${window.location.origin}/payments/success?utm_source=tg`);
    });
  });
});
