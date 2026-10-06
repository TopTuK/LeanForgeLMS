import { describe, it, expect } from 'vitest';
import {
  canJoinLecture,
  fromLocalInputValue,
  groupLecturesByDay,
  lectureEnd,
  toLocalInputValue,
} from '@/lib/lectures';

const lecture = (overrides = {}) => ({
  id: 1,
  startsAt: '2026-10-07T10:00:00Z',
  durationMinutes: 90,
  meetingUrl: 'https://meet.example/abc',
  isCancelled: false,
  ...overrides,
});

describe('lectures lib', () => {
  it('computes the end from the duration', () => {
    expect(lectureEnd(lecture()).toISOString()).toBe('2026-10-07T11:30:00.000Z');
  });

  it('round-trips through the datetime-local input format', () => {
    const iso = '2026-10-07T10:00:00.000Z';
    const local = toLocalInputValue(iso);

    expect(local).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
    expect(fromLocalInputValue(local)).toBe(iso);
  });

  it.each([
    ['closed too early', '2026-10-07T09:40:00Z', false],
    ['open inside the 15 minute window', '2026-10-07T09:46:00Z', true],
    ['open while running', '2026-10-07T11:00:00Z', true],
    ['closed after the end', '2026-10-07T11:31:00Z', false],
  ])('join is %s', (_label, now, expected) => {
    expect(canJoinLecture(lecture(), new Date(now))).toBe(expected);
  });

  it('never offers join for a cancelled lecture or one without a link', () => {
    const during = new Date('2026-10-07T10:30:00Z');
    expect(canJoinLecture(lecture({ isCancelled: true }), during)).toBe(false);
    expect(canJoinLecture(lecture({ meetingUrl: null }), during)).toBe(false);
  });

  it('groups lectures by local day in chronological order', () => {
    const days = groupLecturesByDay([
      lecture({ id: 3, startsAt: '2026-10-09T12:00:00Z' }),
      lecture({ id: 1, startsAt: '2026-10-07T10:00:00Z' }),
      lecture({ id: 2, startsAt: '2026-10-07T12:00:00Z' }),
    ]);

    expect(days.map((d) => d.items.map((l) => l.id))).toEqual([[1, 2], [3]]);
  });
});
