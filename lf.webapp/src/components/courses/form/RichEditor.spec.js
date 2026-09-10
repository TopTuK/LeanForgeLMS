import { describe, it, expect } from 'vitest';
import { waitFor } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import RichEditor from '@/components/courses/form/RichEditor.vue';
import { renderComponent } from '@/test/renderComponent';

async function renderEditor(props = {}) {
  const view = renderComponent(RichEditor, { props });
  await waitFor(() => {
    expect(view.getByRole('button', { name: 'Bold' })).toBeEnabled();
  });
  return view;
}

describe('RichEditor', () => {
  it('shows the formatting toolbar in compact mode without focusing the editor', async () => {
    const { getByRole } = await renderEditor({ compact: true });

    expect(getByRole('toolbar', { name: 'Formatting' })).toBeVisible();
    expect(getByRole('button', { name: 'Bold' })).toBeVisible();
  });

  it('exposes core formatting actions by accessible name', async () => {
    const { getByRole } = await renderEditor();

    for (const name of [
      'Bold',
      'Italic',
      'H1',
      'Align left',
      'Bullets',
      'Link',
      'Undo',
    ]) {
      expect(getByRole('button', { name })).toBeVisible();
    }
  });

  it('hides the image button when allowImage is false', async () => {
    const { queryByRole } = await renderEditor({ allowImage: false });

    expect(queryByRole('button', { name: 'Image' })).not.toBeInTheDocument();
  });

  it('shows the image button when allowImage is true', async () => {
    const { getByRole } = await renderEditor({ allowImage: true });

    expect(getByRole('button', { name: 'Image' })).toBeVisible();
  });

  it('disables formatting buttons when disabled', async () => {
    const { getByRole } = renderComponent(RichEditor, {
      props: { disabled: true, modelValue: '<p>Hi</p>' },
    });

    await waitFor(() => {
      expect(getByRole('toolbar', { name: 'Formatting' })).toBeInTheDocument();
    });

    expect(getByRole('button', { name: 'Bold' })).toBeDisabled();
    expect(getByRole('button', { name: 'Italic' })).toBeDisabled();
    expect(getByRole('button', { name: 'Link' })).toBeDisabled();
  });

  it('toggles Bold as pressed when the button is clicked', async () => {
    const user = userEvent.setup();
    const { getByRole } = await renderEditor({ modelValue: '' });

    const bold = getByRole('button', { name: 'Bold' });
    expect(bold).toHaveAttribute('aria-pressed', 'false');

    await user.click(bold);

    expect(bold).toHaveAttribute('aria-pressed', 'true');
  });
});
