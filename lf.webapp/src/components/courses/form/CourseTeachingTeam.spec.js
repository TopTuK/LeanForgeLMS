import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, within } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import CourseTeachingTeam from '@/components/courses/form/CourseTeachingTeam.vue';
import { assignCourseInstructor, fetchCourseInstructors, removeCourseInstructor } from '@/services/questionService';

vi.mock('@/services/questionService', () => ({
  fetchCourseInstructors: vi.fn(),
  assignCourseInstructor: vi.fn(),
  removeCourseInstructor: vi.fn(),
}));

const creator = {
  userId: 200,
  email: 'creator@lf.test',
  firstName: 'Kim',
  lastName: 'Creator',
  role: 'CourseCreator',
  assignedAt: '2026-09-01T09:00:00Z',
  isCreator: true,
};

const instructor = {
  userId: 201,
  email: 'assigned@lf.test',
  firstName: 'Ira',
  lastName: 'Assigned',
  role: 'Instructor',
  assignedAt: '2026-09-10T09:00:00Z',
  isCreator: false,
};

function renderTeam() {
  return renderComponent(CourseTeachingTeam, { props: { courseId: 7 } });
}

describe('CourseTeachingTeam', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchCourseInstructors.mockResolvedValue([creator, instructor]);
  });

  it('lists the team and labels the course author', async () => {
    renderTeam();

    expect(await screen.findByText('Kim Creator')).toBeInTheDocument();
    expect(screen.getByText('Ira Assigned')).toBeInTheDocument();
    expect(screen.getByText('Author')).toBeInTheDocument();
    expect(screen.getByText('Instructor')).toBeInTheDocument();
    expect(fetchCourseInstructors).toHaveBeenCalledWith(7);
  });

  // The creator is staff by authorship, not by assignment, so there is nothing to revoke.
  it('offers no remove control for the author', async () => {
    renderTeam();

    const authorRow = (await screen.findByText('Kim Creator')).closest('li');
    expect(within(authorRow).queryByRole('button', { name: 'Remove from the team' })).toBeNull();

    const instructorRow = screen.getByText('Ira Assigned').closest('li');
    expect(within(instructorRow).getByRole('button', { name: 'Remove from the team' })).toBeInTheDocument();
  });

  it('assigns an instructor by email and clears the field', async () => {
    const user = userEvent.setup();
    assignCourseInstructor.mockResolvedValue([creator, instructor]);
    renderTeam();

    await screen.findByText('Kim Creator');
    const field = screen.getByLabelText('Add an instructor by email');
    await user.type(field, 'assigned@lf.test');
    await user.click(screen.getByRole('button', { name: 'Add' }));

    expect(assignCourseInstructor).toHaveBeenCalledWith(7, 'assigned@lf.test');
    expect(field).toHaveValue('');
  });

  // The API's 409 body says exactly why (unknown user, wrong role, already assigned).
  it('surfaces the reason the server gives for a rejected assignment', async () => {
    const user = userEvent.setup();
    assignCourseInstructor.mockRejectedValue({
      response: { status: 409, data: 'This user is already assigned to the course.' },
    });
    renderTeam();

    await screen.findByText('Kim Creator');
    await user.type(screen.getByLabelText('Add an instructor by email'), 'assigned@lf.test');
    await user.click(screen.getByRole('button', { name: 'Add' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('This user is already assigned to the course.');
  });

  it('falls back to a generic message for other assignment failures', async () => {
    const user = userEvent.setup();
    assignCourseInstructor.mockRejectedValue({ response: { status: 500 } });
    renderTeam();

    await screen.findByText('Kim Creator');
    await user.type(screen.getByLabelText('Add an instructor by email'), 'someone@lf.test');
    await user.click(screen.getByRole('button', { name: 'Add' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not add this instructor/i);
  });

  it('removes an assigned instructor', async () => {
    const user = userEvent.setup();
    removeCourseInstructor.mockResolvedValue([creator]);
    renderTeam();

    const instructorRow = (await screen.findByText('Ira Assigned')).closest('li');
    await user.click(within(instructorRow).getByRole('button', { name: 'Remove from the team' }));

    expect(removeCourseInstructor).toHaveBeenCalledWith(7, 201);
    expect(screen.queryByText('Ira Assigned')).toBeNull();
  });

  it('shows an error when the team cannot be loaded', async () => {
    fetchCourseInstructors.mockRejectedValue(new Error('offline'));
    renderTeam();

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not load the teaching team/i);
  });
});
