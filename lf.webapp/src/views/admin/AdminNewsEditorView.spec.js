/* eslint-disable vue/one-component-per-file -- the two module stubs below are test doubles, not app components */
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { defineComponent, h } from 'vue';
import { screen, waitFor } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import AdminNewsEditorView from '@/views/admin/AdminNewsEditorView.vue';
import {
  createNewsPost,
  fetchAdminNewsPost,
  updateNewsPost,
  uploadNewsImage,
} from '@/services/adminService';

const mocks = vi.hoisted(() => ({
  route: { params: {} },
  push: vi.fn(),
}));

vi.mock('vue-router', () => ({
  useRoute: () => mocks.route,
  useRouter: () => ({ push: mocks.push }),
}));

vi.mock('@/services/adminService', () => ({
  createNewsPost: vi.fn(),
  fetchAdminNewsPost: vi.fn(),
  updateNewsPost: vi.fn(),
  uploadNewsImage: vi.fn(),
}));

// vuedraggable drag behaviour is untestable in jsdom; render the item slot inline.
vi.mock('vuedraggable', () => ({
  default: defineComponent({
    name: 'DraggableStub',
    props: { modelValue: { type: Array, default: () => [] } },
    setup(props, { slots }) {
      return () => h('div', (props.modelValue ?? []).map((element, index) => slots.item?.({ element, index })));
    },
  }),
}));

// TipTap is covered by RichEditor.spec; a textarea keeps these tests about the editor page itself.
vi.mock('@/components/courses/form/RichEditor.vue', () => ({
  default: defineComponent({
    name: 'RichEditorStub',
    props: { modelValue: { type: String, default: '' } },
    emits: ['update:modelValue'],
    setup(props, { emit }) {
      return () => h('textarea', {
        'aria-label': 'Body',
        value: props.modelValue,
        onInput: (event) => emit('update:modelValue', event.target.value),
      });
    },
  }),
}));

function image(name, type = 'image/png') {
  return new File(['x'], name, { type });
}

describe('AdminNewsEditorView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.route.params = {};
    createNewsPost.mockResolvedValue({ id: 1 });
    updateNewsPost.mockResolvedValue({ id: 7 });
  });

  it('creates a post with its uploaded images in the chosen order', async () => {
    uploadNewsImage
      .mockResolvedValueOnce({ storageObjectId: 11 })
      .mockResolvedValueOnce({ storageObjectId: 12 });
    const user = userEvent.setup();
    renderComponent(AdminNewsEditorView);

    await user.type(screen.getByLabelText('Title'), 'Launch');
    await user.type(screen.getByLabelText('Body'), 'We are live');
    await user.click(screen.getByRole('radio', { name: /members only/i }));
    await user.upload(screen.getByLabelText('Add images'), [image('a.png'), image('b.png')]);

    await waitFor(() => expect(screen.queryByText('Uploading…')).toBeNull());
    expect(screen.getAllByTestId('news-image-tile')).toHaveLength(2);

    await user.click(screen.getByRole('button', { name: 'Move image 2 earlier' }));
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(createNewsPost).toHaveBeenCalledWith({
      title: 'Launch',
      html: 'We are live',
      visibility: 'MembersOnly',
      isPublished: true,
      imageStorageObjectIds: [12, 11],
    });
    expect(mocks.push).toHaveBeenCalledWith({ name: 'AdminNews' });
  });

  it('loads an existing post and saves the edits', async () => {
    mocks.route.params = { id: '7' };
    fetchAdminNewsPost.mockResolvedValue({
      id: 7,
      title: 'Old title',
      html: '<p>Old body</p>',
      visibility: 'Public',
      isPublished: false,
      images: [{ id: 1, storageObjectId: 21, url: '/api/news/7/images/1' }],
    });
    const user = userEvent.setup();
    renderComponent(AdminNewsEditorView);

    const title = await screen.findByDisplayValue('Old title');
    expect(fetchAdminNewsPost).toHaveBeenCalledWith(7);
    expect(screen.getByRole('heading', { name: 'Edit news post' })).toBeInTheDocument();

    await user.clear(title);
    await user.type(title, 'New title');
    await user.click(screen.getByRole('button', { name: 'Remove image 1' }));
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(updateNewsPost).toHaveBeenCalledWith(7, {
      title: 'New title',
      html: '<p>Old body</p>',
      visibility: 'Public',
      isPublished: false,
      imageStorageObjectIds: [],
    });
  });

  it('rejects unsupported files without uploading them', async () => {
    const user = userEvent.setup({ applyAccept: false });
    renderComponent(AdminNewsEditorView);

    await user.upload(screen.getByLabelText('Add images'), image('anim.gif', 'image/gif'));

    expect(screen.getByText(/anim\.gif: only png, jpeg or webp/i)).toBeInTheDocument();
    expect(uploadNewsImage).not.toHaveBeenCalled();
  });

  it('asks for a title before saving', async () => {
    const user = userEvent.setup();
    renderComponent(AdminNewsEditorView);

    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Enter a title.');
    expect(createNewsPost).not.toHaveBeenCalled();
  });

  it('blocks saving while an image upload failed', async () => {
    uploadNewsImage.mockRejectedValue(new Error('minio down'));
    const user = userEvent.setup();
    renderComponent(AdminNewsEditorView);

    await user.type(screen.getByLabelText('Title'), 'Launch');
    await user.type(screen.getByLabelText('Body'), 'Body');
    await user.upload(screen.getByLabelText('Add images'), image('a.png'));
    expect(await screen.findByText('Upload failed')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(screen.getByRole('alert')).toHaveTextContent(/failed to upload/i);
    expect(createNewsPost).not.toHaveBeenCalled();
  });

  it('shows the server validation message', async () => {
    createNewsPost.mockRejectedValue({
      response: { status: 400, data: { errors: { imageStorageObjectIds: ['Unknown news image id(s): 9.'] } } },
    });
    const user = userEvent.setup();
    renderComponent(AdminNewsEditorView);

    await user.type(screen.getByLabelText('Title'), 'Launch');
    await user.type(screen.getByLabelText('Body'), 'Body');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Unknown news image id(s): 9.');
    expect(mocks.push).not.toHaveBeenCalled();
  });
});
