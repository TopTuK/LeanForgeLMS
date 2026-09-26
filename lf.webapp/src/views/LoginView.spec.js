import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import LoginView from '@/views/LoginView.vue';
import { renderComponent } from '@/test/renderComponent';

const route = { query: {} };
vi.mock('vue-router', () => ({ useRoute: () => route }));

describe('LoginView', () => {
  let originalLocation;

  beforeEach(() => {
    route.query = {};
    originalLocation = window.location;
    Object.defineProperty(window, 'location', { configurable: true, value: { href: '' } });
  });

  afterEach(() => {
    Object.defineProperty(window, 'location', { configurable: true, value: originalLocation });
  });

  it('offers PMI, Google and Yandex as sign-in providers', () => {
    const { getByRole } = renderComponent(LoginView);

    expect(getByRole('button', { name: /Continue with PMI Club/i })).toBeInTheDocument();
    expect(getByRole('button', { name: /Continue with Google/i })).toBeInTheDocument();
    expect(getByRole('button', { name: /Continue with Yandex/i })).toBeInTheDocument();
  });

  it('redirects to the Yandex sign-in endpoint when its card is clicked', async () => {
    const user = userEvent.setup();
    const { getByRole } = renderComponent(LoginView);

    await user.click(getByRole('button', { name: /Continue with Yandex/i }));

    expect(window.location.href).toBe('/api/Auth/SignInYandex');
  });

  it.each([
    [/your VK account/i, 'vkid'],
    [/Continue with Mail\.ru/i, 'mail_ru'],
    [/Continue with OK/i, 'ok_ru'],
  ])('redirects %s to the VK ID sign-in endpoint with provider %s', async (name, provider) => {
    const user = userEvent.setup();
    const { getByRole } = renderComponent(LoginView);

    await user.click(getByRole('button', { name }));

    expect(window.location.href).toBe(`/api/Auth/SignInVk?provider=${provider}`);
  });

  it('explains a sign-in rejected for a missing email', () => {
    route.query = { error: 'email_required' };
    const { getByRole } = renderComponent(LoginView);

    expect(getByRole('alert')).toHaveTextContent(/could not get an email address/i);
  });

  it('shows no error on a plain visit', () => {
    const { queryByRole } = renderComponent(LoginView);

    expect(queryByRole('alert')).not.toBeInTheDocument();
  });
});
