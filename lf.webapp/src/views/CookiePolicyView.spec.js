import { describe, it, expect } from 'vitest';
import CookiePolicyView from '@/views/CookiePolicyView.vue';
import { renderComponent } from '@/test/renderComponent';

describe('CookiePolicyView', () => {
  it('renders the policy heading and generated illustration', () => {
    const { getAllByRole, getByRole } = renderComponent(CookiePolicyView);

    const h1s = getAllByRole('heading', { level: 1 });
    expect(h1s).toHaveLength(1);
    expect(h1s[0]).toHaveTextContent(/cookie policy/i);

    expect(getByRole('img', {
      name: 'Abstract browser panels, a security shield and small data tokens',
    })).toBeInTheDocument();
  });

  it('documents only production authentication cookies', () => {
    const { getByText, queryByText } = renderComponent(CookiePolicyView);

    expect(getByText('leanforge_api')).toBeInTheDocument();
    expect(getByText('LeanForgeTempCookie')).toBeInTheDocument();
    expect(getByText('Sign-in security cookies')).toBeInTheDocument();
    expect(getByText(/secure, HttpOnly cookie/i)).toBeInTheDocument();
    expect(queryByText(/\.AspNetCore/)).not.toBeInTheDocument();
    expect(queryByText(/development/i)).not.toBeInTheDocument();
  });

  it('presents browser preferences without internal storage keys', () => {
    const { getByRole, getByText, queryByText } = renderComponent(CookiePolicyView);

    expect(getByRole('heading', {
      name: /browser preferences/i,
    })).toBeInTheDocument();
    expect(getByText('Appearance')).toBeInTheDocument();
    expect(getByText('Language')).toBeInTheDocument();
    expect(getByText('Workspace layout')).toBeInTheDocument();
    expect(queryByText('leanforge-theme')).not.toBeInTheDocument();
    expect(getByRole('link', { name: 'Browser preferences' }))
      .toHaveAttribute('href', '#preferences');
  });

  it('states that tracking is absent and provides a contact address', () => {
    const { getByRole, getByText } = renderComponent(CookiePolicyView);

    expect(getByRole('heading', {
      name: /no tracking cookies/i,
    })).toBeInTheDocument();
    expect(getByText(/does not use browser analytics/i)).toBeInTheDocument();

    const email = getByRole('link', { name: 'support@pmi.moscow' });
    expect(email).toHaveAttribute('href', 'mailto:support@pmi.moscow');
  });
});
