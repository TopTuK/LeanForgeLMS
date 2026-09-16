import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen } from '@testing-library/vue';

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: vi.fn() }),
  useRoute: () => ({ params: { enrollmentId: '12' } }),
}));

vi.mock('@/services/enrollmentService', () => ({
  fetchEnrollment: vi.fn(),
  completeLesson: vi.fn(),
  fetchEnrollmentLessonMediaObjectUrl: vi.fn(),
  fetchEnrollmentLessonPartFileObjectUrl: vi.fn(),
  submitQuizAttempt: vi.fn(),
}));

import { fetchEnrollment, fetchEnrollmentLessonMediaObjectUrl } from '@/services/enrollmentService';
import { renderComponent } from '@/test/renderComponent';
import CourseLearnView from '@/views/courses/CourseLearnView.vue';

const enrollment = {
  id: 12,
  courseId: 5,
  courseTitle: 'Async in C#',
  courseDescription: '<p>Course body</p>',
  enrolledAt: '2026-09-01T12:00:00Z',
  completedAt: null,
  status: 'Active',
  pricePaid: 0,
  isCourseUnavailable: false,
  chapters: [
    {
      id: 1,
      title: 'Chapter 1',
      sortOrder: 1,
      lessons: [{ id: 1, title: 'Lesson 1', content: '', sortOrder: 1, isCompleted: false, parts: [] }],
    },
  ],
};

describe('CourseLearnView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows the course when it is available', async () => {
    fetchEnrollment.mockResolvedValue({ ...enrollment });

    renderComponent(CourseLearnView);

    expect(await screen.findByRole('heading', { level: 1, name: 'Async in C#' })).toBeInTheDocument();
    expect(screen.queryByText('This course is temporarily unavailable')).not.toBeInTheDocument();
  });

  it('shows a temporarily-unavailable notice instead of content for an unpublished course', async () => {
    fetchEnrollment.mockResolvedValue({ ...enrollment, isCourseUnavailable: true, chapters: [] });

    renderComponent(CourseLearnView);

    expect(await screen.findByRole('heading', { name: 'This course is temporarily unavailable' })).toBeInTheDocument();
    expect(screen.getByText(/access will be restored soon/i)).toBeInTheDocument();
    expect(screen.queryByText('Async in C#')).not.toBeInTheDocument();
    expect(fetchEnrollmentLessonMediaObjectUrl).not.toHaveBeenCalled();
  });
});
