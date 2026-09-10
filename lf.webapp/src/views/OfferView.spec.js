import { describe, it, expect } from 'vitest';
import OfferView from '@/views/OfferView.vue';
import { renderComponent } from '@/test/renderComponent';

describe('OfferView', () => {
  it('renders the Russian public offer with a single document heading', () => {
    const { getAllByRole } = renderComponent(OfferView);

    const h1s = getAllByRole('heading', { level: 1 });
    expect(h1s).toHaveLength(1);
    expect(h1s[0]).toHaveTextContent(/Публичная оферта/i);
  });

  it('renders the main sections of the offer', () => {
    const { getByRole } = renderComponent(OfferView);

    expect(getByRole('heading', { name: /Общие положения/ })).toBeInTheDocument();
    expect(getByRole('heading', { name: /Предмет Договора/ })).toBeInTheDocument();
    expect(getByRole('heading', { name: /Цена и порядок расчётов/ })).toBeInTheDocument();
    expect(getByRole('heading', { name: /Реквизиты Исполнителя/ })).toBeInTheDocument();
  });

  it('identifies the provider, the site and the contact details', () => {
    const { getByText } = renderComponent(OfferView);

    expect(getByText(/Школа Сильных Менеджеров/)).toBeInTheDocument();
    expect(getByText(/Сидоров Сергей Александрович/)).toBeInTheDocument();
    expect(getByText(/773371597190/)).toBeInTheDocument();
    expect(getByText(/sergey\.sidorov@pmi\.moscow/)).toBeInTheDocument();
  });

  it('states that Russian law governs the agreement', () => {
    const { getAllByText } = renderComponent(OfferView);

    expect(getAllByText(/законодательством Российской Федерации/).length).toBeGreaterThan(0);
  });
});
