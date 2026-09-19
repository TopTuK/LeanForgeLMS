import { describe, it, expect, beforeEach, vi } from 'vitest';
import { reactive } from 'vue';
import { screen, waitFor, within } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { createTestingPinia } from '@pinia/testing';
import { renderComponent } from '@/test/renderComponent';
import { useAuthStore } from '@/stores/authStore';
import QuestionsView from '@/views/QuestionsView.vue';
import {
  closeQuestion,
  fetchQuestionOverview,
  fetchQuestionThread,
  fetchQuestions,
  postQuestionMessage,
} from '@/services/questionService';

const routerPush = vi.fn();
const route = reactive({ params: {}, query: {} });

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: routerPush }),
  useRoute: () => route,
}));

vi.mock('@/services/questionService', () => ({
  fetchQuestions: vi.fn(),
  fetchQuestionOverview: vi.fn(),
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
    lessonTitle: 'Coroutines',
    studentUserId: 100,
    studentName: 'Sasha Student',
    title: 'Why does this suspend?',
    status: 'Answered',
    createdAt: '2026-09-18T09:00:00Z',
    lastMessageAt: '2026-09-18T10:00:00Z',
    messageCount: 2,
    hasUnread: true,
    lastMessagePreview: 'Try viewModelScope.',
    lastMessageAuthorRole: 'Instructor',
    studentEnrollmentId: 12,
    askedByViewer: true,
    ...overrides,
  };
}

function thread(overrides = {}) {
  return {
    ...summary({ hasUnread: false }),
    messages: [
      {
        id: 10,
        authorUserId: 100,
        authorName: 'Sasha Student',
        authorRole: 'Student',
        body: 'I do not understand coroutine scope.',
        createdAt: '2026-09-18T09:00:00Z',
        isDeleted: false,
        isMine: true,
      },
      {
        id: 11,
        authorUserId: 200,
        authorName: 'Kim Creator',
        authorRole: 'Instructor',
        body: 'Try viewModelScope.',
        createdAt: '2026-09-18T10:00:00Z',
        isDeleted: false,
        isMine: false,
      },
    ],
    ...overrides,
  };
}

function overview(overrides = {}) {
  return {
    total: 3,
    open: 1,
    answered: 1,
    closed: 1,
    unread: 1,
    courses: [{ courseId: 7, courseTitle: 'Kotlin Basics', count: 3 }],
    ...overrides,
  };
}

// RouterLink is stubbed without its slot by default; this stub keeps the link text queryable.
const RouterLinkStub = {
  props: ['to'],
  template: '<a :data-to="JSON.stringify(to)"><slot /></a>',
};

function renderView({ role = 'Student' } = {}) {
  const pinia = createTestingPinia({ createSpy: vi.fn, stubActions: false });
  useAuthStore(pinia).user = { role };

  return renderComponent(QuestionsView, {
    pinia,
    global: { stubs: { RouterLink: RouterLinkStub, RouterView: true } },
  });
}

describe('QuestionsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    route.params = {};
    route.query = {};
    fetchQuestions.mockResolvedValue({ items: [summary()], totalCount: 1 });
    fetchQuestionOverview.mockResolvedValue(overview());
    fetchQuestionThread.mockResolvedValue(thread());
  });

  it("loads the student's own inbox with where each question was asked and the latest reply", async () => {
    renderView();

    const row = (await screen.findByText('Why does this suspend?')).closest('button');
    expect(within(row).getByText('Kotlin Basics · Coroutines')).toBeInTheDocument();
    expect(within(row).getByText(/Try viewModelScope\./)).toBeInTheDocument();
    expect(within(row).getByText('Instructor:')).toBeInTheDocument();
    expect(within(row).getByText('New reply')).toBeInTheDocument();

    expect(fetchQuestions).toHaveBeenCalledWith(expect.objectContaining({ scope: 'student', page: 1 }));
    expect(fetchQuestionOverview).toHaveBeenCalledWith('student');
  });

  it('shows status filters with their counts and refetches when one is picked', async () => {
    const user = userEvent.setup();
    renderView();
    await screen.findByText('Why does this suspend?');

    const answered = screen.getByRole('button', { name: /Answered\s*1/ });
    expect(screen.getByRole('button', { name: /All\s*3/ })).toHaveAttribute('aria-pressed', 'true');

    await user.click(answered);

    expect(answered).toHaveAttribute('aria-pressed', 'true');
    await waitFor(() =>
      expect(fetchQuestions).toHaveBeenLastCalledWith(expect.objectContaining({ status: 'Answered', page: 1 })));
  });

  it('debounces the search before asking the server', async () => {
    const user = userEvent.setup();
    renderView();
    await screen.findByText('Why does this suspend?');
    fetchQuestions.mockClear();

    await user.type(screen.getByPlaceholderText('Search by subject, course or lesson'), 'scope');

    await waitFor(() => expect(fetchQuestions).toHaveBeenCalledTimes(1));
    expect(fetchQuestions).toHaveBeenCalledWith(expect.objectContaining({ search: 'scope' }));
  });

  it('offers a course filter only when questions span more than one course', async () => {
    const user = userEvent.setup();
    fetchQuestionOverview.mockResolvedValue(overview({
      courses: [
        { courseId: 7, courseTitle: 'Kotlin Basics', count: 2 },
        { courseId: 8, courseTitle: 'Rust Basics', count: 1 },
      ],
    }));
    renderView();

    const select = await screen.findByRole('combobox', { name: 'Course' });
    await user.selectOptions(select, '8');

    await waitFor(() =>
      expect(fetchQuestions).toHaveBeenLastCalledWith(expect.objectContaining({ courseId: 8 })));
  });

  it('hides the course filter for a single course', async () => {
    renderView();
    await screen.findByText('Why does this suspend?');

    expect(screen.queryByRole('combobox', { name: 'Course' })).toBeNull();
  });

  it('opens the conversation when a question is picked and records it in the URL', async () => {
    const user = userEvent.setup();
    renderView();

    await user.click(await screen.findByText('Why does this suspend?'));

    expect(fetchQuestionThread).toHaveBeenCalledWith(1);
    expect(await screen.findByText('I do not understand coroutine scope.')).toBeInTheDocument();
    expect(routerPush).toHaveBeenCalledWith({ name: 'Questions', params: { id: 1 }, query: {} });

    // Opening clears the unread state in the list without a refetch.
    const [row] = within(screen.getByRole('list', { name: 'Questions & answers' })).getAllByRole('button');
    expect(row).toHaveAttribute('aria-current', 'true');
    expect(within(row).queryByText('New reply')).toBeNull();
  });

  it('opens a deep-linked conversation straight away', async () => {
    route.params = { id: '1' };
    renderView();

    expect(await screen.findByText('I do not understand coroutine scope.')).toBeInTheDocument();
    expect(fetchQuestionThread).toHaveBeenCalledWith(1);
  });

  it('links back to the lesson the question was asked in', async () => {
    route.params = { id: '1' };
    renderView();

    const link = await screen.findByText('Open lesson');
    expect(JSON.parse(link.closest('a').dataset.to)).toEqual({
      name: 'CourseLearn',
      params: { enrollmentId: 12 },
      query: { lesson: 42 },
    });
  });

  it('sends a reply and moves the conversation to the top of the list', async () => {
    const user = userEvent.setup();
    fetchQuestions.mockResolvedValue({
      items: [summary({ id: 2, title: 'Older question' }), summary()],
      totalCount: 2,
    });
    postQuestionMessage.mockResolvedValue(thread({
      status: 'Open',
      lastMessagePreview: 'Still stuck.',
      lastMessageAuthorRole: 'Student',
      messages: [
        ...thread().messages,
        {
          id: 12,
          authorUserId: 100,
          authorName: 'Sasha Student',
          authorRole: 'Student',
          body: 'Still stuck.',
          createdAt: '2026-09-18T11:00:00Z',
          isDeleted: false,
          isMine: true,
        },
      ],
    }));
    renderView();

    await user.click(await screen.findByText('Why does this suspend?'));
    await user.type(await screen.findByLabelText('Your reply'), 'Still stuck.');
    await user.click(screen.getByRole('button', { name: 'Send' }));

    expect(postQuestionMessage).toHaveBeenCalledWith(1, 'Still stuck.');
    await waitFor(() => {
      const rows = within(screen.getByRole('list', { name: 'Questions & answers' })).getAllByRole('button');
      expect(rows[0]).toHaveTextContent('Why does this suspend?');
      expect(rows[0]).toHaveTextContent('You: Still stuck.');
    });
    expect(screen.getByLabelText('Your reply')).toHaveValue('');
  });

  it('marks a conversation resolved and offers to reopen it', async () => {
    const user = userEvent.setup();
    closeQuestion.mockResolvedValue(thread({ status: 'Closed' }));
    route.params = { id: '1' };
    renderView();

    await user.click(await screen.findByRole('button', { name: /Mark as resolved/ }));

    expect(closeQuestion).toHaveBeenCalledWith(1);
    expect(await screen.findByRole('button', { name: /Reopen/ })).toBeInTheDocument();
    expect(screen.queryByLabelText('Your reply')).toBeNull();
  });

  it('shows an invitation instead of empty panes when the student has never asked anything', async () => {
    fetchQuestions.mockResolvedValue({ items: [], totalCount: 0 });
    fetchQuestionOverview.mockResolvedValue(overview({ total: 0, open: 0, answered: 0, closed: 0, courses: [] }));
    renderView();

    expect(await screen.findByRole('heading', { name: 'No questions yet' })).toBeInTheDocument();
    expect(JSON.parse(screen.getByText('Go to my courses').closest('a').dataset.to)).toEqual({ name: 'CoursesActive' });
    expect(screen.queryByPlaceholderText('Search by subject, course or lesson')).toBeNull();
  });

  it('offers to clear filters when nothing matches them', async () => {
    const user = userEvent.setup();
    renderView();
    await screen.findByText('Why does this suspend?');

    fetchQuestions.mockResolvedValue({ items: [], totalCount: 0 });
    await user.click(screen.getByRole('button', { name: /Resolved\s*1/ }));

    expect(await screen.findByText('No questions match these filters.')).toBeInTheDocument();

    fetchQuestions.mockResolvedValue({ items: [summary()], totalCount: 1 });
    await user.click(screen.getByRole('button', { name: 'Clear filters' }));

    expect(await screen.findByText('Why does this suspend?')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /All\s*3/ })).toHaveAttribute('aria-pressed', 'true');
  });

  it('loads the next page on demand', async () => {
    const user = userEvent.setup();
    fetchQuestions
      .mockResolvedValueOnce({ items: [summary()], totalCount: 2 })
      .mockResolvedValueOnce({ items: [summary({ id: 2, title: 'Second page question' })], totalCount: 2 });
    renderView();

    await user.click(await screen.findByRole('button', { name: 'Show more' }));

    expect(await screen.findByText('Second page question')).toBeInTheDocument();
    expect(screen.getByText('Why does this suspend?')).toBeInTheDocument();
    expect(fetchQuestions).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 }));
    expect(screen.queryByRole('button', { name: 'Show more' })).toBeNull();
  });

  it('keeps a plain student on their own inbox with no incoming tab', async () => {
    renderView({ role: 'Student' });
    await screen.findByText('Why does this suspend?');

    expect(screen.queryByRole('tab', { name: 'Incoming' })).toBeNull();
  });

  it('lets teaching staff switch to the incoming inbox', async () => {
    const user = userEvent.setup();
    renderView({ role: 'Instructor' });
    await screen.findByText('Why does this suspend?');

    await user.click(screen.getByRole('tab', { name: 'Incoming' }));

    await waitFor(() =>
      expect(fetchQuestions).toHaveBeenLastCalledWith(expect.objectContaining({ scope: 'staff' })));
    expect(fetchQuestionOverview).toHaveBeenLastCalledWith('staff');
  });

  it('shows an error when the list cannot be loaded', async () => {
    fetchQuestions.mockRejectedValue(new Error('offline'));
    renderView();

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not load questions/i);
  });
});
