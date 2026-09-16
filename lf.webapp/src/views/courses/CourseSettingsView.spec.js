import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';

const routerPush = vi.fn();

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: routerPush }),
  useRoute: () => ({ params: { id: '5' } }),
}));

vi.mock('@/services/courseService', () => ({
  fetchCourse: vi.fn(),
  fetchCategories: vi.fn(),
  fetchCourseCoverImageObjectUrl: vi.fn(),
  updateCourse: vi.fn(),
  uploadCourseCoverImage: vi.fn(),
}));

import { fetchCategories, fetchCourse, fetchCourseCoverImageObjectUrl, updateCourse } from '@/services/courseService';
import { renderComponent } from '@/test/renderComponent';
import CourseSettingsView from '@/views/courses/CourseSettingsView.vue';

const draftCourse = {
  id: 5,
  title: 'Async in C#',
  shortIntroduction: 'Learn async',
  description: '<p>Course body</p>',
  coverType: 'Color',
  coverColor: 'Ocean',
  coverImageUrl: null,
  isPublished: false,
  categoryId: 1,
  categoryName: 'Backend',
  chapters: [],
  pricingType: 'Free',
  price: null,
  enrollmentMode: 'Open',
};

describe('CourseSettingsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchCategories.mockResolvedValue([
      { id: 1, name: 'Backend', isDefault: true },
      { id: 2, name: 'Frontend', isDefault: false },
    ]);
    updateCourse.mockResolvedValue({ ...draftCourse });
  });

  it('prefills the form from the course', async () => {
    fetchCourse.mockResolvedValue({ ...draftCourse });

    renderComponent(CourseSettingsView);

    expect(await screen.findByLabelText(/^Title/)).toHaveValue('Async in C#');
    expect(screen.getByLabelText(/^Short introduction/)).toHaveValue('Learn async');
    expect(screen.getByRole('option', { name: 'Backend' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('option', { name: 'Ocean' })).toHaveAttribute('aria-selected', 'true');
  });

  it('saves the edited details and returns to the editor', async () => {
    fetchCourse.mockResolvedValue({ ...draftCourse });
    const user = userEvent.setup();

    renderComponent(CourseSettingsView);

    const title = await screen.findByLabelText(/^Title/);
    await user.clear(title);
    await user.type(title, 'Async in depth');
    await user.click(screen.getByRole('option', { name: 'Frontend' }));
    await user.click(screen.getByRole('option', { name: 'Paid' }));
    await user.type(screen.getByRole('spinbutton'), '1990');
    await user.click(screen.getByRole('option', { name: 'Managed' }));
    await user.click(screen.getByRole('button', { name: 'Save changes' }));

    await waitFor(() => expect(updateCourse).toHaveBeenCalledTimes(1));
    expect(updateCourse).toHaveBeenCalledWith(5, expect.objectContaining({
      title: 'Async in depth',
      categoryId: 2,
      pricingType: 'Paid',
      price: 1990,
      enrollmentMode: 'Managed',
      coverType: 'Color',
      coverColor: 'Ocean',
      coverImageStorageObjectId: null,
    }));
    expect(routerPush).toHaveBeenCalledWith({ name: 'CourseEdit', params: { id: 5 } });
  });

  it('keeps the current image cover without requiring a new upload', async () => {
    fetchCourse.mockResolvedValue({ ...draftCourse, coverType: 'Image', coverColor: null, coverImageUrl: '/api/courses/5/cover/image' });
    fetchCourseCoverImageObjectUrl.mockResolvedValue('blob:cover');
    const user = userEvent.setup();

    renderComponent(CourseSettingsView);

    await user.click(await screen.findByRole('button', { name: 'Save changes' }));

    await waitFor(() => expect(updateCourse).toHaveBeenCalledTimes(1));
    expect(updateCourse).toHaveBeenCalledWith(5, expect.objectContaining({
      coverType: 'Image',
      coverColor: null,
      coverImageStorageObjectId: null,
    }));
  });

  it('shows a conflict message when the course was published meanwhile', async () => {
    fetchCourse.mockResolvedValue({ ...draftCourse });
    updateCourse.mockRejectedValue({ response: { status: 409 } });
    const user = userEvent.setup();

    renderComponent(CourseSettingsView);

    await user.click(await screen.findByRole('button', { name: 'Save changes' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('This course has been published');
    expect(routerPush).not.toHaveBeenCalled();
  });

  it('locks a published course instead of showing the form', async () => {
    fetchCourse.mockResolvedValue({ ...draftCourse, isPublished: true });

    renderComponent(CourseSettingsView);

    expect(await screen.findByText('This course is published, so its details can no longer be changed.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save changes' })).not.toBeInTheDocument();
  });
});
