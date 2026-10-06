import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { screen } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { renderComponent } from '@/test/renderComponent';
import ScheduleView from '@/views/ScheduleView.vue';
import { fetchMySchedule } from '@/services/lectureService';

vi.mock('@/services/lectureService', () => ({
  fetchMySchedule: vi.fn(),
}));

const NOW = new Date('2026-10-07T09:50:00Z');

function lecture(overrides = {}) {
  return {
    id: 1,
    courseId: 7,
    courseTitle: 'Kotlin Basics',
    title: 'Coroutines',
    description: null,
    startsAt: '2026-10-07T10:00:00Z',
    durationMinutes: 90,
    meetingUrl: 'https://meet.example/abc',
    isCancelled: false,
    groups: [{ id: 3, name: 'Stream A' }],
    isTeaching: false,
    ...overrides,
  };
}

describe('ScheduleView', () => {
  beforeEach(() => {
    vi.useFakeTimers({ now: NOW, toFake: ['Date'] });
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('offers Join for a lecture about to start and a hint for later ones', async () => {
    fetchMySchedule.mockResolvedValue([
      lecture(),
      lecture({ id: 2, title: 'Flows', startsAt: '2026-10-08T10:00:00Z' }),
    ]);
    renderComponent(ScheduleView);

    const join = await screen.findByRole('link', { name: /Join/ });
    expect(join).toHaveAttribute('href', 'https://meet.example/abc');
    expect(screen.getByText('Flows')).toBeInTheDocument();
    expect(screen.getByText('The link opens 15 minutes before the start')).toBeInTheDocument();
  });

  it('marks cancelled lectures and never offers to join them', async () => {
    fetchMySchedule.mockResolvedValue([lecture({ isCancelled: true, meetingUrl: null })]);
    renderComponent(ScheduleView);

    expect(await screen.findByText('Cancelled')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /Join/ })).not.toBeInTheDocument();
  });

  it('moves finished lectures to the Past tab', async () => {
    fetchMySchedule.mockResolvedValue([lecture({ title: 'Yesterday', startsAt: '2026-10-06T10:00:00Z' })]);
    renderComponent(ScheduleView);

    expect(await screen.findByText('No upcoming lectures.')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('tab', { name: 'Past' }));
    expect(screen.getByText('Yesterday')).toBeInTheDocument();
  });
});
