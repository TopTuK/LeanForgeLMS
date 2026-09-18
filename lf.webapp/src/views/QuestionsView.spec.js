import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { createTestingPinia } from '@pinia/testing';
import { renderComponent } from '@/test/renderComponent';
import { useAuthStore } from '@/stores/authStore';
import QuestionsView from '@/views/QuestionsView.vue';
import {
  closeQuestion,
  fetchQuestionThread,
  fetchQuestions,
  postQuestionMessage,
} from '@/services/questionService';

vi.mock('vue-router', () => ({ useRouter: () => ({ push: vi.fn() }) }));

vi.mock('@/services/questionService', () => ({
  fetchQuestions: vi.fn(),
  fetchQuestionThread: vi.fn(),
  postQuestionMessage: vi.fn(),
  closeQuestion: vi.fn(),
  reopenQuestion: vi.fn(),
  fetchUnreadQuestionCount: vi.fn(),
}));

function summary(overrides = {}) {
  return {
    id: 1,
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

function renderView({ role = 'Student' } = {}) {
  const pinia = createTestingPinia({ createSpy: vi.fn, stubActions: false });
  const authStore = useAuthStore(pinia);
  authStore.user = { id: 100, role };

  renderComponent(QuestionsView, { pinia });
  return pinia;
}

describe('QuestionsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchQuestions.mockResolvedValue({ items: [summary()], totalCount: 1 });
    fetchQuestionThread.mockResolvedValue(thread());
  });

  it('loads the student inbox on mount', async () => {
    renderView();

    expect(await screen.findByText('Why does this fail?')).toBeInTheDocument();
    expect(fetchQuestions).toHaveBeenCalledWith({ scope: 'student', page: 1, pageSize: 20 });
  });

  // A plain student has no incoming queue, so the tabs are pointless for them.
  it('hides the incoming tab from a plain student', async () => {
    renderView({ role: 'Student' });

    await screen.findByText('Why does this fail?');
    expect(screen.queryByRole('tab', { name: 'Incoming' })).toBeNull();
  });

  it('offers the incoming tab to teaching staff and switches scope', async () => {
    const user = userEvent.setup();
    renderView({ role: 'Instructor' });

    await screen.findByText('Why does this fail?');
    await user.click(screen.getByRole('tab', { name: 'Incoming' }));

    await waitFor(() =>
      expect(fetchQuestions).toHaveBeenLastCalledWith({ scope: 'staff', page: 1, pageSize: 20 }));
  });

  it('opens a thread when a question is picked', async () => {
    const user = userEvent.setup();
    renderView();

    await user.click(await screen.findByText('Why does this fail?'));

    expect(fetchQuestionThread).toHaveBeenCalledWith(1);
    expect(await screen.findByText('I am stuck on step two.')).toBeInTheDocument();
  });

  it('sends a reply and shows the updated thread', async () => {
    const user = userEvent.setup();
    postQuestionMessage.mockResolvedValue(thread({
      status: 'Open',
      messages: [
        ...thread().messages,
        {
          id: 11,
          authorUserId: 100,
          authorName: 'Sasha Student',
          authorRole: 'Student',
          body: 'Still stuck.',
          createdAt: '2026-09-18T11:00:00Z',
          isDeleted: false,
        },
      ],
    }));
    renderView();

    await user.click(await screen.findByText('Why does this fail?'));
    await user.type(await screen.findByLabelText('Your reply'), 'Still stuck.');
    await user.click(screen.getByRole('button', { name: 'Send' }));

    expect(postQuestionMessage).toHaveBeenCalledWith(1, 'Still stuck.');
    expect(await screen.findByText('Still stuck.')).toBeInTheDocument();
  });

  it('closes a thread from the composer', async () => {
    const user = userEvent.setup();
    closeQuestion.mockResolvedValue(thread({ status: 'Closed' }));
    renderView();

    await user.click(await screen.findByText('Why does this fail?'));
    await user.click(await screen.findByRole('button', { name: 'Mark as resolved' }));

    expect(closeQuestion).toHaveBeenCalledWith(1);
    expect(await screen.findByRole('button', { name: 'Reopen' })).toBeInTheDocument();
  });

  it('shows an empty state when the student has asked nothing', async () => {
    fetchQuestions.mockResolvedValue({ items: [], totalCount: 0 });
    renderView();

    expect(await screen.findByText(/you have not asked anything yet/i)).toBeInTheDocument();
  });

  it('shows an error when the list cannot be loaded', async () => {
    fetchQuestions.mockRejectedValue(new Error('offline'));
    renderView();

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not load questions/i);
  });

  it('shows an error when sending a reply fails', async () => {
    const user = userEvent.setup();
    postQuestionMessage.mockRejectedValue(new Error('offline'));
    renderView();

    await user.click(await screen.findByText('Why does this fail?'));
    await user.type(await screen.findByLabelText('Your reply'), 'Still stuck.');
    await user.click(screen.getByRole('button', { name: 'Send' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not send your message/i);
  });
});
