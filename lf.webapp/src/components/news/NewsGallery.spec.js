import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import NewsGallery from '@/components/news/NewsGallery.vue';
import { renderComponent } from '@/test/renderComponent';

const images = [
  { id: 1, url: '/api/news/9/images/1' },
  { id: 2, url: '/api/news/9/images/2' },
];

describe('NewsGallery', () => {
  it('renders one lazy-loaded thumbnail per image', () => {
    renderComponent(NewsGallery, { props: { images, title: 'Launch' } });

    const thumbnails = screen.getAllByRole('img');
    expect(thumbnails).toHaveLength(2);
    expect(thumbnails[0]).toHaveAttribute('src', '/api/news/9/images/1');
    expect(thumbnails[0]).toHaveAttribute('loading', 'lazy');
    expect(thumbnails[1]).toHaveAccessibleName('Launch — image 2');
  });

  it('opens the lightbox on the clicked image and pages through the gallery', async () => {
    const user = userEvent.setup();
    renderComponent(NewsGallery, { props: { images, title: 'Launch' } });

    await user.click(screen.getByRole('button', { name: 'Open image 2 of 2' }));

    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent('2 / 2');

    await user.click(screen.getByRole('button', { name: 'Next image' }));
    expect(dialog).toHaveTextContent('1 / 2');

    await user.click(screen.getByRole('button', { name: 'Previous image' }));
    expect(dialog).toHaveTextContent('2 / 2');
  });

  it('renders nothing for an empty gallery', () => {
    renderComponent(NewsGallery, { props: { images: [], title: 'Launch' } });

    expect(screen.queryByRole('img')).toBeNull();
  });
});
