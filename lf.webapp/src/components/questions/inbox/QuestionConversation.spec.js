import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import QuestionConversation from '@/components/questions/inbox/QuestionConversation.vue';

const RouterLinkStub = {
  props: ['to'],
  template: '<a :data-to="JSON.stringify(to)"><slot /></a>',
};

function thread(overrides = {}) {
  return {
    id: 1,
    courseTitle: 'Kotlin Basics',
    lessonId: 42,
    lessonTitle: 'Coroutines',
    studentUserId: 100,
    studentName: 'Sasha Student',
    title: 'Why does this suspend?',
    status: 'Answered',
    studentEnrollmentId: 12,
    askedByViewer: true,
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

function renderConversation(props = {}) {
  return renderComponent(QuestionConversation, {
    props: { thread: thread(), ...props },
    global: { stubs: { RouterLink: RouterLinkStub } },
  });
}

describe('QuestionConversation', () => {
  it('shows where the question was asked and the whole conversation', () => {
    renderConversation();

    expect(screen.getByRole('heading', { name: 'Why does this suspend?' })).toBeInTheDocument();
    expect(screen.getByText(/Kotlin Basics/)).toHaveTextContent('Kotlin Basics › Coroutines');
    expect(screen.getByText('I do not understand coroutine scope.')).toBeInTheDocument();
    expect(screen.getByText('Try viewModelScope.')).toBeInTheDocument();
  });

  it('labels your own messages "You" and names the instructor with their role', () => {
    renderConversation();

    expect(screen.getByText('You')).toBeInTheDocument();
    expect(screen.getByText('Kim Creator')).toBeInTheDocument();
    expect(screen.getByText('Instructor')).toBeInTheDocument();
  });

  it('links to the lesson in your own threads', () => {
    renderConversation();

    const link = screen.getByText('Open lesson').closest('a');
    expect(JSON.parse(link.dataset.to)).toEqual({
      name: 'CourseLearn',
      params: { enrollmentId: 12 },
      query: { lesson: 42 },
    });
  });

  // Staff can't open another student's enrollment, so the link would only lead to a 403.
  it("offers no lesson link in someone else's thread", () => {
    renderConversation({ thread: thread({ askedByViewer: false }) });

    expect(screen.queryByText('Open lesson')).toBeNull();
    expect(screen.getByText('Sasha Student', { selector: '.conversation__student' })).toBeInTheDocument();
  });

  it('renders message bodies as text, never as markup', () => {
    renderConversation({
      thread: thread({ messages: [{ ...thread().messages[0], body: '<img src=x onerror="alert(1)">' }] }),
    });

    expect(screen.getByText('<img src=x onerror="alert(1)">')).toBeInTheDocument();
    expect(document.querySelector('img')).toBeNull();
  });

  it('shows a removed message as removed', () => {
    renderConversation({
      thread: thread({ messages: [{ ...thread().messages[1], body: null, isDeleted: true }] }),
    });

    expect(screen.getByText(/removed by an administrator/i)).toBeInTheDocument();
  });

  it('sends a reply with the button and clears the box once the caller confirms', async () => {
    const user = userEvent.setup();
    const { emitted } = renderConversation();

    await user.type(screen.getByLabelText('Your reply'), 'Still stuck.');
    await user.click(screen.getByRole('button', { name: 'Send' }));

    const [body, onSuccess] = emitted().reply[0];
    expect(body).toBe('Still stuck.');

    onSuccess();
    await Promise.resolve();
    expect(screen.getByLabelText('Your reply')).toHaveValue('');
  });

  it('sends with Ctrl+Enter', async () => {
    const user = userEvent.setup();
    const { emitted } = renderConversation();

    await user.type(screen.getByLabelText('Your reply'), 'Quick follow-up');
    await user.keyboard('{Control>}{Enter}{/Control}');

    expect(emitted().reply[0][0]).toBe('Quick follow-up');
  });

  it('keeps Send disabled for an empty or blank reply', async () => {
    const user = userEvent.setup();
    const { emitted } = renderConversation();

    await user.type(screen.getByLabelText('Your reply'), '   ');

    expect(screen.getByRole('button', { name: 'Send' })).toBeDisabled();
    expect(emitted().reply).toBeUndefined();
  });

  it('emits close from the resolve action', async () => {
    const user = userEvent.setup();
    const { emitted } = renderConversation();

    await user.click(screen.getByRole('button', { name: /Mark as resolved/ }));

    expect(emitted().close).toHaveLength(1);
  });

  it('swaps the composer for a reopen action once resolved', async () => {
    const user = userEvent.setup();
    const { emitted } = renderConversation({ thread: thread({ status: 'Closed' }) });

    expect(screen.queryByLabelText('Your reply')).toBeNull();
    expect(screen.getByText('This question is resolved.')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /Reopen/ }));
    expect(emitted().reopen).toHaveLength(1);
  });

  it('offers a back action on request', async () => {
    const user = userEvent.setup();
    const { emitted } = renderConversation({ showBack: true });

    await user.click(screen.getByRole('button', { name: 'Back to all questions' }));

    expect(emitted().back).toHaveLength(1);
  });

  it('shows the error passed in by the caller', () => {
    renderConversation({ errorMessage: 'Could not send your message. Please try again.' });

    expect(screen.getByRole('alert')).toHaveTextContent(/could not send your message/i);
  });
});
