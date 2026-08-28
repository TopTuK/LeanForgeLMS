import { describe, it, expect } from 'vitest';
import { defineComponent, h } from 'vue';
import userEvent from '@testing-library/user-event';
import LessonQuizPart from '@/components/courses/lesson/LessonQuizPart.vue';
import { renderComponent } from '@/test/renderComponent';

// vuedraggable drag behaviour is untestable in jsdom; render the item slot inline.
vi.mock('vuedraggable', () => ({
  default: defineComponent({
    name: 'DraggableStub',
    props: { modelValue: { type: Array, default: () => [] } },
    setup(props, { slots }) {
      return () => h(
        'div',
        (props.modelValue ?? []).map((element, index) => slots.item?.({ element, index })),
      );
    },
  }),
}));

function makeModel() {
  return {
    quizPassThreshold: 60,
    quizQuestions: [
      {
        id: 'q1',
        text: 'Question one',
        questionType: 'single',
        options: [
          { id: 'o1', text: 'A', isCorrect: true },
          { id: 'o2', text: 'B', isCorrect: false },
        ],
      },
    ],
  };
}

function lastEmit(emitted) {
  return emitted()['update:modelValue'].at(-1)[0];
}

describe('LessonQuizPart', () => {
  it('clamps the pass threshold to the 1-100 range', async () => {
    const { getByLabelText, emitted } = renderComponent(LessonQuizPart, {
      props: { modelValue: makeModel() },
    });
    const input = getByLabelText('Pass threshold');

    await userEvent.clear(input);
    await userEvent.type(input, '150');
    input.dispatchEvent(new Event('change'));
    expect(lastEmit(emitted).quizPassThreshold).toBe(100);
  });

  it('appends a blank question when "Add question" is clicked', async () => {
    const { getByRole, emitted } = renderComponent(LessonQuizPart, {
      props: { modelValue: makeModel() },
    });

    await userEvent.click(getByRole('button', { name: /add question/i }));

    const next = lastEmit(emitted);
    expect(next.quizQuestions).toHaveLength(2);
    expect(next.quizQuestions[1]).toMatchObject({ text: '', questionType: 'single' });
  });

  it('removes a question', async () => {
    const model = makeModel();
    model.quizQuestions.push({
      id: 'q2', text: 'Question two', questionType: 'single',
      options: [{ id: 'o3', text: 'C', isCorrect: true }, { id: 'o4', text: 'D', isCorrect: false }],
    });

    const { getAllByRole, emitted } = renderComponent(LessonQuizPart, { props: { modelValue: model } });
    await userEvent.click(getAllByRole('button', { name: 'Remove question' })[0]);

    expect(lastEmit(emitted).quizQuestions).toHaveLength(1);
    expect(lastEmit(emitted).quizQuestions[0].id).toBe('q2');
  });

  it('keeps exactly one correct option for single-choice questions', async () => {
    const { getByRole, emitted } = renderComponent(LessonQuizPart, {
      props: { modelValue: makeModel() },
    });

    // only the not-yet-correct option ("B") exposes the "mark as correct" affordance;
    // the already-correct option ("A") renders a checkmark instead
    await userEvent.click(getByRole('button', { name: 'Mark as correct answer' }));

    const options = lastEmit(emitted).quizQuestions[0].options;
    expect(options.map((o) => o.isCorrect)).toEqual([false, true]);
  });

  it('collapses multiple correct answers to one when switching a question to single choice', async () => {
    const model = makeModel();
    model.quizQuestions[0].questionType = 'multiple';
    model.quizQuestions[0].options = [
      { id: 'o1', text: 'A', isCorrect: true },
      { id: 'o2', text: 'B', isCorrect: true },
      { id: 'o3', text: 'C', isCorrect: false },
    ];

    const { getByRole, emitted } = renderComponent(LessonQuizPart, { props: { modelValue: model } });
    await userEvent.click(getByRole('button', { name: 'Single choice' }));

    const options = lastEmit(emitted).quizQuestions[0].options;
    expect(options.map((o) => o.isCorrect)).toEqual([true, false, false]);
  });
});
