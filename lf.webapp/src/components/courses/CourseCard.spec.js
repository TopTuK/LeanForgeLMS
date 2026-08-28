import { describe, it, expect } from 'vitest';
import userEvent from '@testing-library/user-event';
import CourseCard from '@/components/courses/CourseCard.vue';
import { renderComponent } from '@/test/renderComponent';

const base = {
  title: 'Intro to Lean',
  description: 'A short course',
  category: 'Management',
};

describe('CourseCard', () => {
  it('renders the available state with a view-details action', async () => {
    const { getByRole, emitted } = renderComponent(CourseCard, {
      props: { ...base, status: 'available' },
    });
    const button = getByRole('button', { name: /view details/i });
    await userEvent.click(button);
    expect(emitted()['view-details']).toHaveLength(1);
  });

  it('renders progress and a continue action in the active state', async () => {
    const { getByText, getByRole, emitted } = renderComponent(CourseCard, {
      props: { ...base, status: 'active', progress: 50 },
    });
    expect(getByText('50% complete')).toBeInTheDocument();
    await userEvent.click(getByRole('button', { name: /continue/i }));
    expect(emitted().continue).toHaveLength(1);
  });

  it('renders the completion date and a review action in the finished state', async () => {
    const { getByText, getByRole, emitted } = renderComponent(CourseCard, {
      props: { ...base, status: 'finished', completedOn: '2024-03-01' },
    });
    expect(getByText('Completed 2024-03-01')).toBeInTheDocument();
    await userEvent.click(getByRole('button', { name: /review/i }));
    expect(emitted().continue).toHaveLength(1);
  });

  it('renders the student count, a manage action and a status badge in the teaching state', async () => {
    const { getByText, getByRole, emitted } = renderComponent(CourseCard, {
      props: { ...base, status: 'teaching', studentsCount: 3, isPublished: false },
    });
    expect(getByText('3 students')).toBeInTheDocument();
    expect(getByText('Draft')).toBeInTheDocument();
    await userEvent.click(getByRole('button', { name: /manage/i }));
    expect(emitted().manage).toHaveLength(1);
  });

  it('paints a colour cover from coverColor', () => {
    const { container } = renderComponent(CourseCard, {
      props: { ...base, status: 'available', coverType: 'Color', coverColor: 'Amber' },
    });
    expect(container.querySelector('.course-card__cover')).toHaveStyle({
      backgroundColor: 'var(--color-cover-amber)',
    });
  });

  it('renders a cover image when coverType is Image', () => {
    const { container } = renderComponent(CourseCard, {
      props: { ...base, status: 'available', coverType: 'Image', coverImageUrl: 'blob:cover' },
    });
    const img = container.querySelector('img.course-card__cover-image');
    expect(img).toHaveAttribute('src', 'blob:cover');
  });
});
