import { describe, it, expect, beforeEach, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import LearnerQuizPart from '@/components/courses/lesson/LearnerQuizPart.vue';
import { renderComponent } from '@/test/renderComponent';
import { submitQuizAttempt } from '@/services/enrollmentService';

vi.mock('@/services/enrollmentService', () => ({
  submitQuizAttempt: vi.fn(),
}));

const part = {
  id: 100,
  quizQuestions: [
    {
      id: 1,
      text: 'Pick one',
      questionType: 'SingleChoice',
      options: [
        { id: 11, text: 'Alpha' },
        { id: 12, text: 'Beta' },
      ],
    },
    {
      id: 2,
      text: 'Pick many',
      questionType: 'MultipleChoice',
      options: [
        { id: 21, text: 'One' },
        { id: 22, text: 'Two' },
      ],
    },
  ],
};

const props = { part, enrollmentId: 7, lessonId: 3 };

function renderQuiz() {
  return renderComponent(LearnerQuizPart, { props });
}

describe('LearnerQuizPart', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders radios for single-choice and checkboxes for multiple-choice questions', () => {
    const { getByRole } = renderQuiz();
    expect(getByRole('radio', { name: 'Alpha' })).toBeInTheDocument();
    expect(getByRole('checkbox', { name: 'One' })).toBeInTheDocument();
  });

  it('keeps submit disabled until every question is answered', async () => {
    const { getByRole } = renderQuiz();
    const submit = getByRole('button', { name: 'Submit answers' });
    expect(submit).toBeDisabled();

    await userEvent.click(getByRole('radio', { name: 'Alpha' }));
    expect(submit).toBeDisabled();

    await userEvent.click(getByRole('checkbox', { name: 'One' }));
    expect(submit).toBeEnabled();
  });

  it('submits answers, emits the returned enrollment and shows the result banner', async () => {
    submitQuizAttempt.mockResolvedValueOnce({
      enrollment: { id: 7, progressPercent: 100 },
      result: {
        passed: true,
        scorePercent: 100,
        questions: [
          { questionId: 1, isCorrect: true, correctOptionIds: [11] },
          { questionId: 2, isCorrect: true, correctOptionIds: [21] },
        ],
      },
    });

    const { getByRole, getByText, emitted } = renderQuiz();
    await userEvent.click(getByRole('radio', { name: 'Alpha' }));
    await userEvent.click(getByRole('checkbox', { name: 'One' }));
    await userEvent.click(getByRole('button', { name: 'Submit answers' }));

    expect(submitQuizAttempt).toHaveBeenCalledWith(7, 3, 100, [
      { questionId: 1, selectedOptionIds: [11] },
      { questionId: 2, selectedOptionIds: [21] },
    ]);
    expect(emitted().submitted).toEqual([[{ id: 7, progressPercent: 100 }]]);
    expect(getByText('You passed!')).toBeInTheDocument();
    expect(getByText('Score: 100%')).toBeInTheDocument();
  });

  it('shows an error message when submission fails', async () => {
    submitQuizAttempt.mockRejectedValueOnce(new Error('boom'));

    const { getByRole, findByRole } = renderQuiz();
    await userEvent.click(getByRole('radio', { name: 'Alpha' }));
    await userEvent.click(getByRole('checkbox', { name: 'One' }));
    await userEvent.click(getByRole('button', { name: 'Submit answers' }));

    const alert = await findByRole('alert');
    expect(alert).toHaveTextContent('Could not submit your answers. Please try again.');
  });

  it('returns to the question form when retry is clicked', async () => {
    submitQuizAttempt.mockResolvedValueOnce({
      enrollment: {},
      result: { passed: false, scorePercent: 0, questions: [] },
    });

    const { getByRole } = renderQuiz();
    await userEvent.click(getByRole('radio', { name: 'Alpha' }));
    await userEvent.click(getByRole('checkbox', { name: 'One' }));
    await userEvent.click(getByRole('button', { name: 'Submit answers' }));

    await userEvent.click(getByRole('button', { name: 'Try again' }));

    expect(getByRole('button', { name: 'Submit answers' })).toBeDisabled();
    expect(getByRole('radio', { name: 'Alpha' })).not.toBeChecked();
  });
});
