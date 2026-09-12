import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor, within } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import AdminCoursesView from '@/views/admin/AdminCoursesView.vue';
import {
  fetchAdminCourses,
  fetchCourseEnrollments,
  enrollStudent,
  removeEnrollment,
  deleteCourse,
  fetchUsers,
} from '@/services/adminService';

const routerPush = vi.fn();
vi.mock('vue-router', () => ({
  useRouter: () => ({ push: routerPush }),
}));

vi.mock('@/services/adminService', () => ({
  fetchAdminCourses: vi.fn(),
  fetchCourseEnrollments: vi.fn(),
  enrollStudent: vi.fn(),
  removeEnrollment: vi.fn(),
  deleteCourse: vi.fn(),
  fetchUsers: vi.fn(),
}));

const managedCourse = {
  id: 5,
  title: 'Async in C#',
  categoryName: 'Backend',
  createdByUserId: 3,
  authorFirstName: 'Ada',
  authorLastName: 'Lovelace',
  authorEmail: 'ada@pmi.moscow',
  pricingType: 'Paid',
  price: 1990,
  enrollmentMode: 'Managed',
  isPublished: true,
  createdAt: '2026-09-01T12:00:00Z',
};

function enrollment({ id = 10, userId = 7, isPaid = false, pricePaid = 0 } = {}) {
  return {
    id,
    userId,
    studentFirstName: 'Ann',
    studentLastName: 'Lee',
    studentEmail: 'ann@pmi.moscow',
    status: 'Active',
    pricePaid,
    isPaid,
    enrolledAt: '2026-09-02T12:00:00Z',
    completedAt: null,
    totalLessonCount: 4,
    completedLessonCount: 1,
    progressPercent: 25,
  };
}

describe('AdminCoursesView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchAdminCourses.mockResolvedValue({ items: [managedCourse], totalCount: 1 });
    fetchCourseEnrollments.mockResolvedValue({ items: [enrollment()], totalCount: 1 });
    fetchUsers.mockResolvedValue({ items: [], totalCount: 0 });
    enrollStudent.mockResolvedValue({});
    removeEnrollment.mockResolvedValue({});
    deleteCourse.mockResolvedValue({});
  });

  it('renders courses and labels a managed course as private', async () => {
    renderComponent(AdminCoursesView);

    expect(await screen.findByText('Async in C#')).toBeInTheDocument();
    expect(screen.getByText('Private')).toBeInTheDocument();
  });

  // The admin list mixes every author's courses, so the owner has to be visible.
  it('shows who owns each course', async () => {
    renderComponent(AdminCoursesView);

    expect(await screen.findByText('Ada Lovelace')).toBeInTheDocument();
    expect(screen.getByText('ada@pmi.moscow')).toBeInTheDocument();
  });

  it('opens the course editor for a course the admin does not own', async () => {
    const user = userEvent.setup();
    renderComponent(AdminCoursesView);
    await screen.findByText('Async in C#');

    await user.click(screen.getByRole('button', { name: 'Edit' }));

    expect(routerPush).toHaveBeenCalledWith({ name: 'CourseEdit', params: { id: 5 } });
  });

  it('lists the roster when opening the students dialog', async () => {
    const user = userEvent.setup();
    renderComponent(AdminCoursesView);
    await screen.findByText('Async in C#');

    await user.click(screen.getByRole('button', { name: 'Students' }));

    expect(await screen.findByText('Ann Lee')).toBeInTheDocument();
    expect(fetchCourseEnrollments).toHaveBeenCalledWith(5, { page: 1, pageSize: 100 });
  });

  it('enrolls a student picked from the search results', async () => {
    const user = userEvent.setup();
    fetchUsers.mockResolvedValue({
      items: [{ id: 9, firstName: 'Grace', lastName: 'H', email: 'grace@pmi.moscow' }],
      totalCount: 1,
    });
    renderComponent(AdminCoursesView);
    await screen.findByText('Async in C#');
    await user.click(screen.getByRole('button', { name: 'Students' }));
    await screen.findByText('Ann Lee');

    await user.type(screen.getByPlaceholderText('Search by name or email'), 'grace');
    await user.click(await screen.findByRole('button', { name: 'Enroll' }));

    await waitFor(() => expect(enrollStudent).toHaveBeenCalledWith(5, 9));
  });

  // The paid warning is the whole point of the confirmation — it must name the amount.
  it('warns with the amount paid before removing a paying student', async () => {
    const user = userEvent.setup();
    fetchCourseEnrollments.mockResolvedValue({
      items: [enrollment({ isPaid: true, pricePaid: 1990 })],
      totalCount: 1,
    });
    renderComponent(AdminCoursesView);
    await screen.findByText('Async in C#');
    await user.click(screen.getByRole('button', { name: 'Students' }));
    await screen.findByText('Ann Lee');

    await user.click(screen.getByRole('button', { name: 'Remove' }));

    expect(await screen.findByText(/Ann Lee paid 1990 ₽/)).toBeInTheDocument();
    expect(screen.getByText(/no refund is issued/)).toBeInTheDocument();
  });

  it('removes a student after confirming', async () => {
    const user = userEvent.setup();
    renderComponent(AdminCoursesView);
    await screen.findByText('Async in C#');
    await user.click(screen.getByRole('button', { name: 'Students' }));
    await screen.findByText('Ann Lee');

    await user.click(screen.getByRole('button', { name: 'Remove' }));
    const dialog = await screen.findByText('Remove student');
    await user.click(within(dialog.closest('[role="dialog"]')).getByRole('button', { name: 'Remove' }));

    await waitFor(() => expect(removeEnrollment).toHaveBeenCalledWith(5, 10));
  });

  it('blocks deleting a course with paid students until the admin acknowledges', async () => {
    const user = userEvent.setup();
    fetchCourseEnrollments.mockResolvedValue({
      items: [enrollment({ isPaid: true, pricePaid: 1990 })],
      totalCount: 1,
    });
    renderComponent(AdminCoursesView);
    await screen.findByText('Async in C#');

    await user.click(screen.getByRole('button', { name: 'Delete' }));
    await screen.findByText('Delete course');

    const confirm = screen.getAllByRole('button', { name: 'Delete' })
      .find((b) => b.closest('[role="dialog"]'));
    expect(confirm).toBeDisabled();

    await user.click(screen.getByRole('checkbox'));
    expect(confirm).not.toBeDisabled();

    await user.click(confirm);
    await waitFor(() => expect(deleteCourse).toHaveBeenCalledWith(5, { force: true }));
  });

  it('deletes a course with no paying students without an acknowledgement', async () => {
    const user = userEvent.setup();
    renderComponent(AdminCoursesView);
    await screen.findByText('Async in C#');

    await user.click(screen.getByRole('button', { name: 'Delete' }));
    await screen.findByText('Delete course');

    const confirm = screen.getAllByRole('button', { name: 'Delete' })
      .find((b) => b.closest('[role="dialog"]'));
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();

    await user.click(confirm);
    await waitFor(() => expect(deleteCourse).toHaveBeenCalledWith(5, { force: false }));
  });

  it('surfaces the server reason when a delete is refused', async () => {
    const user = userEvent.setup();
    deleteCourse.mockRejectedValue({
      response: { status: 409, data: '2 student(s) have paid for this course.' },
    });
    renderComponent(AdminCoursesView);
    await screen.findByText('Async in C#');

    await user.click(screen.getByRole('button', { name: 'Delete' }));
    await screen.findByText('Delete course');
    const confirm = screen.getAllByRole('button', { name: 'Delete' })
      .find((b) => b.closest('[role="dialog"]'));
    await user.click(confirm);

    expect(await screen.findByText('2 student(s) have paid for this course.')).toBeInTheDocument();
  });
});
