import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import LessonQuestionsPanel from '@/components/courses/lesson/LessonQuestionsPanel.vue';
import { askQuestion, fetchLessonQuestions, fetchQuestionThread } from '@/services/questionService';

vi.mock('@/services/questionService', () => ({
  fetchLessonQuestions: vi.fn(),
  fetchQuestionThread: vi.fn(),
  askQuestion: vi.fn(),
  postQuestionMessage: vi.fn(),
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

function thread() {
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
  };
}

function renderPanel() {
  return renderComponent(LessonQuestionsPanel, { props: { lessonId: 42 }, pinia: true });
}

describe('LessonQuestionsPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchLessonQuestions.mockResolvedValue({ items: [], totalCount: 0 });
    fetchQuestionThread.mockResolvedValue(thread());
    askQuestion.mockResolvedValue(thread());
  });

  it('loads the questions for its lesson', async () => {
    renderPanel();

    expect(await screen.findByText(/no questions on this lesson yet/i)).toBeInTheDocument();
    expect(fetchLessonQuestions).toHaveBeenCalledWith(42);
  });

  it('lists existing questions and opens one', async () => {
    const user = userEvent.setup();
    fetchLessonQuestions.mockResolvedValue({ items: [summary()], totalCount: 1 });
    renderPanel();

    await user.click(await screen.findByText('Why does this fail?'));

    expect(fetchQuestionThread).toHaveBeenCalledWith(1);
    expect(await screen.findByText('I am stuck on step two.')).toBeInTheDocument();
  });

  it('asks a question through the composer', async () => {
    const user = userEvent.setup();
    renderPanel();

    await user.click(await screen.findByRole('button', { name: 'Ask a question' }));
    await user.type(screen.getByLabelText('Subject'), 'Why does this fail?');
    await user.type(screen.getByLabelText('Your question'), 'I am stuck on step two.');
    await user.click(screen.getByRole('button', { name: 'Ask a question' }));

    expect(askQuestion).toHaveBeenCalledWith({
      lessonId: 42,
      title: 'Why does this fail?',
      body: 'I am stuck on step two.',
    });
  });

  it('keeps the submit button disabled until both fields are filled', async () => {
    const user = userEvent.setup();
    renderPanel();

    await user.click(await screen.findByRole('button', { name: 'Ask a question' }));
    await user.type(screen.getByLabelText('Subject'), 'Only a subject');

    expect(screen.getByRole('button', { name: 'Ask a question' })).toBeDisabled();
  });

  // A 403 here means the viewer is not enrolled, which deserves a specific explanation.
  it('explains the enrollment requirement when the API answers 403', async () => {
    const user = userEvent.setup();
    askQuestion.mockRejectedValue({ response: { status: 403 } });
    renderPanel();

    await user.click(await screen.findByRole('button', { name: 'Ask a question' }));
    await user.type(screen.getByLabelText('Subject'), 'Why does this fail?');
    await user.type(screen.getByLabelText('Your question'), 'I am stuck.');
    await user.click(screen.getByRole('button', { name: 'Ask a question' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/only students enrolled in this course/i);
  });

  it('falls back to a generic error for any other failure', async () => {
    const user = userEvent.setup();
    askQuestion.mockRejectedValue({ response: { status: 500 } });
    renderPanel();

    await user.click(await screen.findByRole('button', { name: 'Ask a question' }));
    await user.type(screen.getByLabelText('Subject'), 'Why does this fail?');
    await user.type(screen.getByLabelText('Your question'), 'I am stuck.');
    await user.click(screen.getByRole('button', { name: 'Ask a question' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not send your message/i);
  });

  it('shows an error when the list cannot be loaded', async () => {
    fetchLessonQuestions.mockRejectedValue(new Error('offline'));
    renderPanel();

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not load questions/i);
  });
});
