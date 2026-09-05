import { describe, it, expect } from 'vitest';
import AuthorCard from '@/components/home/AuthorCard.vue';
import { renderComponent } from '@/test/renderComponent';

const props = {
  name: 'Sergey Sidorov',
  role: 'Founder',
  bio: 'Builds things.',
  highlights: ['Project management'],
  cta: 'Read full bio',
  href: 'https://s-sidorov.ru',
};

describe('home/AuthorCard', () => {
  it('renders the photo when provided, with the name as alt text', () => {
    const { getByRole, queryByText } = renderComponent(AuthorCard, {
      props: { ...props, photo: '/author-portrait.jpg' },
    });

    const img = getByRole('img', { name: 'Sergey Sidorov' });
    expect(img).toHaveAttribute('src', '/author-portrait.jpg');
    expect(queryByText('SS')).not.toBeInTheDocument();
  });

  it('falls back to initials when no photo is provided', () => {
    const { getByText, queryByRole } = renderComponent(AuthorCard, { props });

    expect(getByText('SS')).toBeInTheDocument();
    expect(queryByRole('img')).not.toBeInTheDocument();
  });

  it('links the CTA to the given href', () => {
    const { getByRole } = renderComponent(AuthorCard, { props });

    expect(getByRole('link', { name: /read full bio/i })).toHaveAttribute('href', 'https://s-sidorov.ru');
  });
});
