import { describe, it, expect } from 'vitest';
import OfferView from '@/views/OfferView.vue';
import { renderComponent } from '@/test/renderComponent';

describe('OfferView', () => {
  it('renders a single document heading and the main articles', () => {
    const { getAllByRole, getByRole } = renderComponent(OfferView);

    const h1s = getAllByRole('heading', { level: 1 });
    expect(h1s).toHaveLength(1);
    expect(h1s[0]).toHaveTextContent(/public offer/i);

    expect(getByRole('heading', { name: /definitions/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /price and payment/i })).toBeInTheDocument();
    expect(getByRole('heading', { name: /cancellation and refunds/i })).toBeInTheDocument();
  });

  it('identifies the school, the provider and the payment method', () => {
    const { getByText, getAllByText } = renderComponent(OfferView);

    expect(getByText(/Sergey Sidorov/i)).toBeInTheDocument();
    expect(getAllByText(/High Managers School/).length).toBeGreaterThan(0);
    expect(getByText(/lms\.s-sidorov\.ru/)).toBeInTheDocument();
    expect(getAllByText(/Robokassa/).length).toBeGreaterThan(0);
  });

  it('states that this is not licensed education and that Russian text prevails', () => {
    const { getByText } = renderComponent(OfferView);

    expect(getByText(/does not carry out licensed educational activity/i)).toBeInTheDocument();
    expect(getByText(/russian-language text is legally binding/i)).toBeInTheDocument();
  });

  it('points claims to the support email', () => {
    const { getByText } = renderComponent(OfferView);

    expect(getByText(/support@pmi\.moscow/)).toBeInTheDocument();
  });
});
