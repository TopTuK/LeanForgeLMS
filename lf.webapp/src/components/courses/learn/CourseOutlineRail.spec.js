import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import CourseOutlineRail from '@/components/courses/learn/CourseOutlineRail.vue';

const chapters = [
  {
    id: 1,
    title: 'Basics',
    lessons: [
      { id: 10, title: 'Intro', isCompleted: true },
      { id: 11, title: 'Setup', isCompleted: false },
    ],
  },
  {
    id: 2,
    title: 'Advanced',
    lessons: [{ id: 20, title: 'Deep dive', isCompleted: false }],
  },
];

describe('CourseOutlineRail', () => {
  it('renders every chapter heading and lesson button', () => {
    const { getByText, getAllByRole } = render(CourseOutlineRail, { props: { chapters } });
    expect(getByText('Basics')).toBeInTheDocument();
    expect(getByText('Advanced')).toBeInTheDocument();
    expect(getAllByRole('button')).toHaveLength(3);
  });

  it('marks the selected lesson as active and completed lessons as done', () => {
    const { getByRole } = render(CourseOutlineRail, {
      props: { chapters, selectedLessonId: 11 },
    });
    expect(getByRole('button', { name: 'Setup' })).toHaveClass('outline-rail__lesson--active');
    expect(getByRole('button', { name: 'Intro' })).toHaveClass('outline-rail__lesson--done');
  });

  it('emits select with the clicked lesson id', async () => {
    const { getByRole, emitted } = render(CourseOutlineRail, { props: { chapters } });
    await userEvent.click(getByRole('button', { name: 'Deep dive' }));
    expect(emitted().select).toEqual([[20]]);
  });

  it('exposes the title as the aside aria-label and heading', () => {
    const { getByRole } = render(CourseOutlineRail, {
      props: { chapters, title: 'Course outline' },
    });
    expect(getByRole('complementary', { name: 'Course outline' })).toBeInTheDocument();
    expect(getByRole('heading', { name: 'Course outline', level: 2 })).toBeInTheDocument();
  });
});
