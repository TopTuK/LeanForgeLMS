import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import api from '@/services/api';
import {
  cancelLecture,
  fetchCourseLectures,
  fetchMySchedule,
  scheduleLecture,
  updateLecture,
} from '@/services/lectureService';

describe('lectureService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    for (const m of Object.values(api)) m.mockResolvedValue({ data: 'RESULT' });
  });

  it.each([
    ['fetchCourseLectures', () => fetchCourseLectures(7), 'get', ['/courses/7/lectures']],
    ['scheduleLecture', () => scheduleLecture(7, { title: 'L' }), 'post', ['/courses/7/lectures', { title: 'L' }]],
    ['updateLecture', () => updateLecture(4, { title: 'L' }), 'put', ['/lectures/4', { title: 'L' }]],
    ['cancelLecture', () => cancelLecture(4), 'post', ['/lectures/4/cancel']],
  ])('%s calls the right endpoint and unwraps data', async (_name, call, method, args) => {
    await expect(call()).resolves.toBe('RESULT');
    expect(api[method]).toHaveBeenCalledWith(...args);
  });

  it('fetchMySchedule omits an unset range so the server applies its default window', async () => {
    await fetchMySchedule();
    expect(api.get).toHaveBeenCalledWith('/lectures/mine', { params: { from: undefined, to: undefined } });
  });
});
