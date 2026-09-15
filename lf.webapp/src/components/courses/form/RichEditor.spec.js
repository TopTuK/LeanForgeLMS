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
      'Inline code',
      'Insert table',
      'Link',
      'Undo',
    ]) {
      expect(getByRole('button', { name })).toBeVisible();
    }
  });

  it('shows code-block controls only when enabled', async () => {
    const hidden = await renderEditor();
    expect(hidden.queryByRole('button', { name: 'Code block' })).not.toBeInTheDocument();
    hidden.unmount();

    const enabled = await renderEditor({ allowCodeBlock: true, modelValue: '<p>const answer = 42;</p>' });
    expect(enabled.getByRole('button', { name: 'Code block' })).toBeVisible();
    expect(enabled.getByRole('combobox', { name: 'Code language' })).toBeDisabled();
  });

  it('creates a language-aware multiline code block', async () => {
    const user = userEvent.setup();
    const { container, getByRole } = await renderEditor({
      allowCodeBlock: true,
      modelValue: '<p>const answer = 42;</p>',
    });

    await user.click(getByRole('button', { name: 'Code block' }));
    const language = getByRole('combobox', { name: 'Code language' });
    expect(language).toBeEnabled();

    await user.selectOptions(language, 'javascript');

    await waitFor(() => {
      expect(container.querySelector('pre code')).toHaveClass('language-javascript');
    });
  });

  it('creates and edits a table with accessible toolbar actions', async () => {
    const user = userEvent.setup();
    const { container, getByRole, queryByRole } = await renderEditor();

    expect(queryByRole('button', { name: 'Add row below' })).not.toBeInTheDocument();

    await user.click(getByRole('button', { name: 'Insert table' }));

    await waitFor(() => {
      expect(container.querySelectorAll('table tr')).toHaveLength(3);
      expect(container.querySelectorAll('table th')).toHaveLength(3);
    });

    await user.click(getByRole('button', { name: 'Add row below' }));
    expect(container.querySelectorAll('table tr')).toHaveLength(4);

    await user.click(getByRole('button', { name: 'Add column after' }));
    expect(container.querySelectorAll('table tr:first-child > *')).toHaveLength(4);

    await user.click(getByRole('button', { name: 'Delete row' }));
    expect(container.querySelectorAll('table tr')).toHaveLength(3);

    await user.click(getByRole('button', { name: 'Delete column' }));
    expect(container.querySelectorAll('table tr:first-child > *')).toHaveLength(3);

    await user.click(getByRole('button', { name: 'Delete table' }));
    expect(container.querySelector('table')).not.toBeInTheDocument();
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
