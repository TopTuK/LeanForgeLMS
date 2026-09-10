import { describe, it, expect } from 'vitest';
import ContactView from '@/views/ContactView.vue';
import { renderComponent } from '@/test/renderComponent';

describe('ContactView', () => {
  it('renders the administrator name and support channels', () => {
    const { getByText, getByRole } = renderComponent(ContactView);

    expect(getByText('Sergey Sidorov')).toBeInTheDocument();

    const email = getByRole('link', { name: 'support@pmi.moscow' });
    expect(email).toHaveAttribute('href', 'mailto:support@pmi.moscow');

    const telegram = getByRole('link', { name: '@TopTuk88' });
    expect(telegram).toHaveAttribute('href', 'https://t.me/TopTuk88');
    expect(telegram).toHaveAttribute('target', '_blank');
  });

  it('renders the INN and tax status note', () => {
    const { getByText } = renderComponent(ContactView);

    expect(getByText('773371597190')).toBeInTheDocument();
    expect(getByText(/Professional income tax/i)).toBeInTheDocument();
  });

  it('renders the contact still-life image', () => {
    const { getByRole } = renderComponent(ContactView);

    expect(getByRole('img', {
      name: 'A quiet desk with a notebook, envelopes and a phone by the window',
    })).toBeInTheDocument();
  });
});
