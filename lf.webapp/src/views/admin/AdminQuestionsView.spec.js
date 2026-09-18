import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, within } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import AdminQuestionsView from '@/views/admin/AdminQuestionsView.vue';
import {
  deleteAdminQuestion,
  deleteAdminQuestionMessage,
  fetchAdminQuestionThread,
  fetchAdminQuestions,
  postAdminQuestionMessage,
} from '@/services/adminService';

vi.mock('@/services/adminService', () => ({
  fetchAdminQuestions: vi.fn(),
  fetchAdminQuestionThread: vi.fn(),
  postAdminQuestionMessage: vi.fn(),
  deleteAdminQuestionMessage: vi.fn(),
  deleteAdminQuestion: vi.fn(),
}));

function summary(overrides = {}) {
  return {
    id: 5,
    courseId: 7,
    courseTitle: 'Kotlin Basics',
    lessonId: 42,
    lessonTitle: 'Lesson 42',
    studentUserId: 100,
    studentName: 'Sasha Student',
    title: 'Why does this fail?',
    status: 'Open',
    createdAt: '2026-09-18T09:00:00Z',
    lastMessageAt: '2026-09-18T09:00:00Z',
    messageCount: 1,
    hasUnread: false,
    ...overrides,
  };
}

function thread(overrides = {}) {
  return {
    ...summary(),
    messages: [
      {
        id: 10,
        authorUserId: 100,
        authorName: 'Sasha Student',
        authorRole: 'Student',
        body: 'I am stuck on step two.',
        createdAt: '2026-09-18T09:00:00Z',
        isDeleted: false,
      },
    ],
    ...overrides,
  };
}

describe('AdminQuestionsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchAdminQuestions.mockResolvedValue({ items: [summary()], totalCount: 1 });
    fetchAdminQuestionThread.mockResolvedValue(thread());
  });

  it('lists every question across the platform', async () => {
    renderComponent(AdminQuestionsView);

    expect(await screen.findByText('Why does this fail?')).toBeInTheDocument();
    expect(screen.getByText('Sasha Student')).toBeInTheDocument();
    expect(screen.getByText(/Kotlin Basics/)).toBeInTheDocument();
  });

  it('applies the search and status filters', async () => {
    const user = userEvent.setup();
    renderComponent(AdminQuestionsView);
    await screen.findByText('Why does this fail?');

    await user.type(screen.getByLabelText('Search by subject, course or student'), 'deadlock');
    await user.selectOptions(screen.getByLabelText('Filter by status'), 'Answered');
    await user.click(screen.getByRole('button', { name: 'Apply' }));

    expect(fetchAdminQuestions).toHaveBeenLastCalledWith({
      status: 'Answered',
      search: 'deadlock',
      page: 1,
      pageSize: 20,
    });
  });

  it('opens a thread and replies to it as an admin', async () => {
    const user = userEvent.setup();
    postAdminQuestionMessage.mockResolvedValue(thread({
      messages: [
        ...thread().messages,
        {
          id: 11,
          authorUserId: 300,
          authorName: 'Ada Admin',
          authorRole: 'Admin',
          body: 'Handled by support.',
          createdAt: '2026-09-18T12:00:00Z',
          isDeleted: false,
        },
      ],
    }));
    renderComponent(AdminQuestionsView);

    await user.click(await screen.findByRole('button', { name: 'Open' }));
    await user.type(await screen.findByLabelText('Your reply'), 'Handled by support.');
    await user.click(screen.getByRole('button', { name: 'Send' }));

    expect(postAdminQuestionMessage).toHaveBeenCalledWith(5, 'Handled by support.');
    expect(await screen.findByText('Ada Admin')).toBeInTheDocument();
  });

  it('soft-deletes a message after confirmation', async () => {
    const user = userEvent.setup();
    deleteAdminQuestionMessage.mockResolvedValue(thread({
      messages: [{ ...thread().messages[0], body: null, isDeleted: true }],
    }));
    renderComponent(AdminQuestionsView);

    await user.click(await screen.findByRole('button', { name: 'Open' }));
    await user.click(await screen.findByRole('button', { name: 'Delete message' }));

    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }));

    expect(deleteAdminQuestionMessage).toHaveBeenCalledWith(5, 10);
    expect(await screen.findByText(/removed by an administrator/i)).toBeInTheDocument();
  });

  it('deletes a whole thread after confirmation', async () => {
    const user = userEvent.setup();
    deleteAdminQuestion.mockResolvedValue(undefined);
    renderComponent(AdminQuestionsView);

    await user.click(await screen.findByRole('button', { name: 'Open' }));
    await user.click(await screen.findByRole('button', { name: 'Delete the whole question' }));

    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }));

    expect(deleteAdminQuestion).toHaveBeenCalledWith(5);
  });

  // The admin panel is oversight, not participation: resolving a thread stays with its participants.
  it('does not offer the resolve control', async () => {
    const user = userEvent.setup();
    renderComponent(AdminQuestionsView);

    await user.click(await screen.findByRole('button', { name: 'Open' }));
    await screen.findByLabelText('Your reply');

    expect(screen.queryByRole('button', { name: 'Mark as resolved' })).toBeNull();
  });

  it('shows an empty state when nothing matches', async () => {
    fetchAdminQuestions.mockResolvedValue({ items: [], totalCount: 0 });
    renderComponent(AdminQuestionsView);

    expect(await screen.findByText(/no questions match these filters/i)).toBeInTheDocument();
  });

  it('shows an error when the list cannot be loaded', async () => {
    fetchAdminQuestions.mockRejectedValue(new Error('offline'));
    renderComponent(AdminQuestionsView);

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not load questions/i);
  });
});
