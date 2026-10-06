import { describe, it, expect, beforeEach, vi } from 'vitest';
import { reactive } from 'vue';
import { screen, waitFor } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { createTestingPinia } from '@pinia/testing';
import { renderComponent } from '@/test/renderComponent';
import GroupChatView from '@/views/groups/GroupChatView.vue';
import { fetchGroup } from '@/services/groupService';
import { fetchGroupMessages, postGroupMessage } from '@/services/groupChatService';
import { joinGroupChat, onGroupChatEvent } from '@/services/groupChatHub';

const route = reactive({ params: { groupId: '3' }, query: {} });

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: vi.fn() }),
  useRoute: () => route,
}));

vi.mock('@/services/groupService', () => ({
  fetchGroup: vi.fn(),
}));

vi.mock('@/services/groupChatService', () => ({
  fetchGroupMessages: vi.fn(),
  postGroupMessage: vi.fn(),
  removeGroupMessage: vi.fn(),
  markGroupChatRead: vi.fn().mockResolvedValue(undefined),
  fetchGroupUnreadCounts: vi.fn().mockResolvedValue([]),
}));

const handlers = {};
vi.mock('@/services/groupChatHub', () => ({
  GroupChatEvents: { messagePosted: 'messagePosted', messageDeleted: 'messageDeleted' },
  joinGroupChat: vi.fn().mockResolvedValue(undefined),
  leaveGroupChat: vi.fn().mockResolvedValue(undefined),
  onGroupChatEvent: vi.fn((name, handler) => {
    handlers[name] = handler;
    return () => {};
  }),
}));

const VIEWER = 100;

function message(overrides = {}) {
  return {
    id: 1,
    groupId: 3,
    authorUserId: 200,
    authorName: 'Kim Creator',
    authorIsStaff: true,
    body: 'Welcome!',
    sentAt: '2026-10-06T10:00:00Z',
    isDeleted: false,
    isMine: false,
    ...overrides,
  };
}

function render() {
  return renderComponent(GroupChatView, {
    pinia: createTestingPinia({ createSpy: vi.fn, stubActions: false }),
  });
}

describe('GroupChatView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Handlers from the previous test belong to an unmounted component.
    Object.keys(handlers).forEach((name) => delete handlers[name]);
    fetchGroup.mockResolvedValue({
      id: 3,
      courseId: 7,
      courseTitle: 'Kotlin Basics',
      name: 'Stream A',
      canManage: false,
      members: [{ userId: VIEWER, firstName: 'Sasha', lastName: 'Student' }],
    });
    fetchGroupMessages.mockResolvedValue({ items: [message()], hasMore: false, viewerUserId: VIEWER });
  });

  it('loads history, marks staff authors and subscribes to live updates', async () => {
    render();

    expect(await screen.findByText('Welcome!')).toBeInTheDocument();
    expect(screen.getByText('Instructor')).toBeInTheDocument();
    await waitFor(() => expect(joinGroupChat).toHaveBeenCalledWith(3));
    expect(onGroupChatEvent).toHaveBeenCalledWith('messagePosted', expect.any(Function));
  });

  it('appends pushed messages and recognises the viewer as their author', async () => {
    const { container } = render();
    await screen.findByText('Welcome!');
    await waitFor(() => expect(handlers.messagePosted).toBeDefined());

    await handlers.messagePosted(message({ id: 2, authorUserId: VIEWER, authorIsStaff: false, body: 'Hi from my other tab' }));

    expect(await screen.findByText('Hi from my other tab')).toBeInTheDocument();
    expect(container.querySelectorAll('.chat-message.is-mine')).toHaveLength(1);
  });

  it('ignores pushes for other groups', async () => {
    render();
    await screen.findByText('Welcome!');
    await waitFor(() => expect(handlers.messagePosted).toBeDefined());

    await handlers.messagePosted(message({ id: 9, groupId: 4, body: 'Elsewhere' }));

    expect(screen.queryByText('Elsewhere')).not.toBeInTheDocument();
  });

  it('does not duplicate a sent message when its push arrives too', async () => {
    const sent = message({ id: 2, authorUserId: VIEWER, authorIsStaff: false, body: 'Hello', isMine: true });
    postGroupMessage.mockResolvedValue(sent);
    render();
    await screen.findByText('Welcome!');

    await userEvent.type(screen.getByPlaceholderText('Write a message…'), 'Hello');
    await userEvent.click(screen.getByRole('button', { name: 'Send' }));
    await screen.findByText('Hello');
    await handlers.messagePosted({ ...sent, isMine: false });

    expect(postGroupMessage).toHaveBeenCalledWith(3, 'Hello');
    expect(screen.getAllByText('Hello')).toHaveLength(1);
  });

  it('shows a dedicated message when the viewer has no access', async () => {
    fetchGroup.mockRejectedValue({ response: { status: 403 } });
    render();

    expect(await screen.findByRole('alert')).toHaveTextContent("You do not have access to this group's chat.");
    expect(joinGroupChat).not.toHaveBeenCalled();
  });
});
