import { describe, it, expect } from 'vitest';
import { screen, within } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import QuestionThread from '@/components/questions/QuestionThread.vue';

function thread(overrides = {}) {
  return {
    id: 1,
    courseTitle: 'Kotlin Basics',
    lessonTitle: 'Lesson 42',
    studentName: 'Sasha Student',
    title: 'Why does this fail?',
    status: 'Open',
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

function renderThread(props = {}) {
  return renderComponent(QuestionThread, { props: { thread: thread(), ...props } });
}

describe('QuestionThread', () => {
  it('renders the subject, context and every message', () => {
    renderThread({
      thread: thread({
        messages: [
          ...thread().messages,
          {
            id: 11,
            authorUserId: 200,
            authorName: 'Kim Creator',
            authorRole: 'Instructor',
            body: 'Try restarting the exercise.',
            createdAt: '2026-09-18T10:00:00Z',
            isDeleted: false,
          },
        ],
      }),
    });

    expect(screen.getByRole('heading', { name: 'Why does this fail?' })).toBeInTheDocument();
    expect(screen.getByText(/Kotlin Basics/)).toBeInTheDocument();
    expect(screen.getByText('I am stuck on step two.')).toBeInTheDocument();
    expect(screen.getByText('Try restarting the exercise.')).toBeInTheDocument();
    expect(screen.getByText('Kim Creator')).toBeInTheDocument();
  });

  // Plain-text bodies must never be interpreted as markup.
  it('renders a message body containing HTML as literal text', () => {
    renderThread({
      thread: thread({
        messages: [{ ...thread().messages[0], body: '<img src=x onerror="alert(1)">' }],
      }),
    });

    expect(screen.getByText('<img src=x onerror="alert(1)">')).toBeInTheDocument();
    expect(document.querySelector('img')).toBeNull();
  });

  it('withholds the body of a deleted message but keeps it in the thread', () => {
    renderThread({
      thread: thread({
        messages: [{ ...thread().messages[0], body: null, isDeleted: true }],
      }),
    });

    expect(screen.getByText(/removed by an administrator/i)).toBeInTheDocument();
    expect(screen.queryByText('I am stuck on step two.')).toBeNull();
  });

  it('emits the reply and clears the box once the caller confirms', async () => {
    const user = userEvent.setup();
    const { emitted } = renderThread();

    await user.type(screen.getByLabelText('Your reply'), 'Here is more detail.');
    await user.click(screen.getByRole('button', { name: 'Send' }));

    const [body, onSuccess] = emitted().reply[0];
    expect(body).toBe('Here is more detail.');

    onSuccess();
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(screen.getByLabelText('Your reply')).toHaveValue('');
  });

  it('will not send an empty or whitespace-only reply', async () => {
    const user = userEvent.setup();
    const { emitted } = renderThread();

    await user.type(screen.getByLabelText('Your reply'), '   ');

    expect(screen.getByRole('button', { name: 'Send' })).toBeDisabled();
    expect(emitted().reply).toBeUndefined();
  });

  it('replaces the composer with a reopen action once the thread is closed', () => {
    renderThread({ thread: thread({ status: 'Closed' }) });

    expect(screen.queryByLabelText('Your reply')).toBeNull();
    expect(screen.getByRole('button', { name: 'Reopen' })).toBeInTheDocument();
  });

  it('hides delete controls unless the viewer may delete', () => {
    renderThread();

    expect(screen.queryByRole('button', { name: 'Delete message' })).toBeNull();
  });

  it('emits delete-message with the message when the viewer may delete', async () => {
    const user = userEvent.setup();
    const { emitted } = renderThread({ canDelete: true });

    await user.click(screen.getByRole('button', { name: 'Delete message' }));

    expect(emitted()['delete-message'][0][0].id).toBe(10);
  });

  it('hides the status controls when the viewer may not change status', () => {
    renderThread({ canChangeStatus: false });

    expect(screen.queryByRole('button', { name: 'Mark as resolved' })).toBeNull();
  });

  it('shows the error message passed in by the caller', () => {
    renderThread({ errorMessage: 'Could not send your message. Please try again.' });

    expect(within(screen.getByRole('alert')).getByText(/could not send your message/i)).toBeInTheDocument();
  });
});
