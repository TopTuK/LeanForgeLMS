import { describe, it, expect } from 'vitest';
import userEvent from '@testing-library/user-event';
import LessonImagesPart from '@/components/courses/lesson/LessonImagesPart.vue';
import { renderComponent } from '@/test/renderComponent';

function image(id, extras = {}) {
  return { id, fileName: `${id}.png`, objectUrl: `blob:${id}`, uploading: false, uploadError: false, ...extras };
}

describe('LessonImagesPart', () => {
  it('lets the author pick several images at once', async () => {
    const user = userEvent.setup();
    const { container, emitted } = renderComponent(LessonImagesPart, { props: { images: [] } });
    const input = container.querySelector('input[type="file"]');
    expect(input).toHaveAttribute('multiple');

    await user.upload(input, [
      new File(['a'], 'a.png', { type: 'image/png' }),
      new File(['b'], 'b.png', { type: 'image/png' }),
    ]);

    expect(emitted().files[0][0].map((file) => file.name)).toEqual(['a.png', 'b.png']);
  });

  it('shows every image in the row and emits remove for the chosen one', async () => {
    const user = userEvent.setup();
    const { getAllByRole, emitted } = renderComponent(LessonImagesPart, {
      props: { images: [image('a'), image('b')] },
    });

    expect(getAllByRole('img')).toHaveLength(2);

    await user.click(getAllByRole('button', { name: 'Remove image' })[1]);

    expect(emitted().remove[0]).toEqual(['b']);
  });

  it('disables adding more images while an upload is in flight', () => {
    const { getByRole } = renderComponent(LessonImagesPart, {
      props: { images: [image('a', { uploading: true })] },
    });

    expect(getByRole('button', { name: /uploading/i })).toBeDisabled();
    expect(getByRole('button', { name: 'Remove image' })).toBeDisabled();
  });

  it('flags an image whose upload failed', () => {
    const { getByText } = renderComponent(LessonImagesPart, {
      props: { images: [image('a', { uploadError: true })] },
    });

    expect(getByText('Upload failed')).toBeInTheDocument();
  });
});
