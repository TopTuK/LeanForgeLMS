import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import userEvent from '@testing-library/user-event';
import LoginView from '@/views/LoginView.vue';
import { renderComponent } from '@/test/renderComponent';

describe('LoginView', () => {
  let originalLocation;

  beforeEach(() => {
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
});
