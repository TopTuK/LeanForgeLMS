import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import QuestionInboxItem from '@/components/questions/inbox/QuestionInboxItem.vue';

function question(overrides = {}) {
  return {
    id: 1,
    courseTitle: 'Kotlin Basics',
    lessonTitle: 'Coroutines',
    studentName: 'Sasha Student',
    title: 'Why does this suspend?',
    status: 'Answered',
    lastMessageAt: '2026-09-18T10:00:00Z',
    hasUnread: false,
    lastMessagePreview: 'Try viewModelScope.',
    lastMessageAuthorRole: 'Instructor',
    askedByViewer: true,
    ...overrides,
  };
}

function renderItem(props = {}) {
  return renderComponent(QuestionInboxItem, { props: { question: question(), ...props } });
}

describe('QuestionInboxItem', () => {
  it('shows the subject, where it was asked, the latest message and the status', () => {
    renderItem();

    const row = screen.getByRole('button');
    expect(row).toHaveTextContent('Why does this suspend?');
    expect(row).toHaveTextContent('Kotlin Basics · Coroutines');
    expect(row).toHaveTextContent('Instructor: Try viewModelScope.');
    expect(row).toHaveTextContent('Answered');
  });

  it('calls your own last message "You"', () => {
    renderItem({ question: question({ lastMessageAuthorRole: 'Student', lastMessagePreview: 'Still stuck.' }) });

    expect(screen.getByRole('button')).toHaveTextContent('You: Still stuck.');
  });

  it("names the student in someone else's thread", () => {
    renderItem({
      question: question({ askedByViewer: false, lastMessageAuthorRole: 'Student', lastMessagePreview: 'Help' }),
    });

    expect(screen.getByRole('button')).toHaveTextContent('Sasha Student: Help');
  });

  it('flags an unread reply', () => {
    renderItem({ question: question({ hasUnread: true }) });

    expect(screen.getByText('New reply')).toBeInTheDocument();
  });

  it('describes an open question as awaiting a reply', () => {
    renderItem({ question: question({ status: 'Open' }) });

    expect(screen.getByText('Awaiting reply')).toBeInTheDocument();
  });

  it('falls back to a removal notice when the latest message was deleted', () => {
    renderItem({ question: question({ lastMessagePreview: null }) });

    expect(screen.getByRole('button')).toHaveTextContent(/removed by an administrator/i);
  });

  it('marks the selected row and emits select on click', async () => {
    const user = userEvent.setup();
    const { emitted } = renderItem({ selected: true });

    expect(screen.getByRole('button')).toHaveAttribute('aria-current', 'true');

    await user.click(screen.getByRole('button'));
    expect(emitted().select[0][0].id).toBe(1);
  });
});
