import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor, within } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { createTestingPinia } from '@pinia/testing';

const routerPush = vi.fn();
const locationAssign = vi.fn();

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: routerPush }),
  useRoute: () => ({ params: { id: '5' } }),
}));

vi.mock('@/services/enrollmentService', () => ({
  fetchCoursePreview: vi.fn(),
  fetchCourseCoverImageObjectUrl: vi.fn(),
  fetchCoursePreviewLessonMediaObjectUrl: vi.fn(),
  fetchCoursePreviewLessonPartFileObjectUrl: vi.fn(),
  validatePromoCode: vi.fn(),
}));

vi.mock('@/services/paymentService', () => ({
  createCheckout: vi.fn(),
  fetchPaymentOrder: vi.fn(),
}));

vi.mock('@/services/platformService', () => ({
  fetchPlatformConfig: vi.fn().mockResolvedValue({ studentEnrollmentEnabled: true }),
}));

import { fetchCoursePreview, fetchCoursePreviewLessonPartFileObjectUrl } from '@/services/enrollmentService';
import { createCheckout } from '@/services/paymentService';
import { renderComponent } from '@/test/renderComponent';
import CourseDetailView from '@/views/courses/CourseDetailView.vue';

const paidPreview = {
  id: 5,
  title: 'Async in C#',
  shortIntroduction: 'Learn async',
  description: '<p>Course body</p>',
  categoryId: 1,
  categoryName: 'Backend',
  lessonCount: 3,
  coverType: 'Color',
  coverColor: 'Coral',
  isEnrolled: false,
  enrollmentId: null,
  enrollmentStatus: null,
  chapters: [],
  pricingType: 'Paid',
  price: 1990,
};

function renderDetail() {
  return renderComponent(CourseDetailView, {
    pinia: createTestingPinia({
      createSpy: vi.fn,
      stubActions: false,
    }),
  });
}

describe('CourseDetailView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    locationAssign.mockReset();
    vi.stubGlobal('location', { assign: locationAssign });
    fetchCoursePreview.mockResolvedValue({ ...paidPreview });
  });

  it('shows the awaiting-payment page instead of Continue when checkout is pending', async () => {
    fetchCoursePreview.mockResolvedValue({
      ...paidPreview,
      isEnrolled: true,
      enrollmentId: 9,
      enrollmentStatus: 'PendingPayment',
    });

    renderDetail();

    expect(await screen.findByRole('heading', {
      level: 1,
      name: /finish payment to unlock this course/i,
    })).toBeInTheDocument();
    expect(screen.getByText('Async in C#')).toBeInTheDocument();
    expect(screen.getByText(/access opens once payment is completed/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /complete payment/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^continue$/i })).not.toBeInTheDocument();
    expect(screen.getByRole('img', {
      name: /sealed linen course folder/i,
    })).toBeInTheDocument();
  });

  it('keeps Continue for an active enrollment', async () => {
    fetchCoursePreview.mockResolvedValue({
      ...paidPreview,
      isEnrolled: true,
      enrollmentId: 9,
      enrollmentStatus: 'Active',
    });

    renderDetail();

    expect(await screen.findByRole('button', { name: /^continue$/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /complete payment/i })).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 1, name: 'Async in C#' })).toBeInTheDocument();
  });

  it('shows a multi-image preview part as one row of images', async () => {
    fetchCoursePreview.mockResolvedValue({
      ...paidPreview,
      chapters: [{
        id: 1,
        title: 'Basics',
        sortOrder: 0,
        lessons: [{
          id: 3,
          title: 'Gallery lesson',
          sortOrder: 0,
          includeInPreview: true,
          content: null,
          parts: [{
            id: 8,
            partType: 'Image',
            sortOrder: 0,
            html: null,
            storageObjectId: null,
            mediaUrl: null,
            files: [81, 82, 83].map((id) => ({ id, fileName: `${id}.png`, storageObjectId: id })),
          }],
        }],
      }],
    });
    fetchCoursePreviewLessonPartFileObjectUrl.mockImplementation((courseId, lessonId, partId, fileId) =>
      Promise.resolve(`blob:${fileId}`));

    renderDetail();

    const lesson = (await screen.findByText('Gallery lesson')).closest('details');
    lesson.open = true;

    await waitFor(() => expect(within(lesson).getAllByRole('img')).toHaveLength(3));
    expect(fetchCoursePreviewLessonPartFileObjectUrl).toHaveBeenCalledTimes(3);
    expect(fetchCoursePreviewLessonPartFileObjectUrl).toHaveBeenCalledWith(5, 3, 8, 81);
  });

  it('redirects to Robokassa from the awaiting-payment CTA', async () => {
    fetchCoursePreview.mockResolvedValue({
      ...paidPreview,
      isEnrolled: true,
      enrollmentId: 9,
      enrollmentStatus: 'PendingPayment',
    });
    createCheckout.mockResolvedValue({
      enrollmentId: 9,
      paymentUrl: 'https://auth.robokassa.ru/Merchant/Index.aspx',
      status: 'PendingPayment',
    });

    const user = userEvent.setup();
    renderDetail();

    await user.click(await screen.findByRole('button', { name: /complete payment/i }));

    await waitFor(() => expect(createCheckout).toHaveBeenCalledWith(5, null));
    expect(locationAssign).toHaveBeenCalledWith('https://auth.robokassa.ru/Merchant/Index.aspx');
    expect(screen.getByRole('heading', {
      level: 1,
      name: /redirecting to the payment page/i,
    })).toBeInTheDocument();
  });
});
