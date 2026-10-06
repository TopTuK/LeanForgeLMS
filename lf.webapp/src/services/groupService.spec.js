import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import api from '@/services/api';
import {
  addGroupMembers,
  createGroup,
  fetchCourseGroups,
  fetchEligibleStudents,
  fetchGroup,
  fetchMyGroups,
  removeGroupMember,
  updateGroup,
} from '@/services/groupService';
import { fetchTeachingCourses } from '@/services/teachingService';

describe('groupService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    for (const m of Object.values(api)) m.mockResolvedValue({ data: 'RESULT' });
  });

  it.each([
    ['fetchCourseGroups', () => fetchCourseGroups(7), 'get', ['/courses/7/groups']],
    ['createGroup', () => createGroup(7, { name: 'A' }), 'post', ['/courses/7/groups', { name: 'A', description: null }]],
    ['fetchEligibleStudents', () => fetchEligibleStudents(7), 'get', ['/courses/7/groups/eligible-students']],
    ['fetchMyGroups', () => fetchMyGroups(), 'get', ['/groups/mine']],
    ['fetchGroup', () => fetchGroup(3), 'get', ['/groups/3']],
    ['updateGroup', () => updateGroup(3, { name: 'B', description: 'd' }), 'put', ['/groups/3', { name: 'B', description: 'd' }]],
    ['addGroupMembers', () => addGroupMembers(3, [1, 2]), 'post', ['/groups/3/members', { userIds: [1, 2] }]],
    ['removeGroupMember', () => removeGroupMember(3, 1), 'delete', ['/groups/3/members/1']],
    ['fetchTeachingCourses', () => fetchTeachingCourses(), 'get', ['/teaching/courses']],
  ])('%s calls the right endpoint and unwraps data', async (_name, call, method, args) => {
    await expect(call()).resolves.toBe('RESULT');
    expect(api[method]).toHaveBeenCalledWith(...args);
  });
});
