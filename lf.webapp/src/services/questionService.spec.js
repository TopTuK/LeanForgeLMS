import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

import api from '@/services/api';
import {
  askQuestion,
  assignCourseInstructor,
  closeQuestion,
  fetchCourseInstructors,
  fetchLessonQuestions,
  fetchQuestionThread,
  fetchQuestions,
  fetchUnreadQuestionCount,
  postQuestionMessage,
  removeCourseInstructor,
  reopenQuestion,
} from '@/services/questionService';

describe('questionService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.get.mockResolvedValue({ data: {} });
    api.post.mockResolvedValue({ data: {} });
    api.delete.mockResolvedValue({ data: {} });
  });

  it('fetchQuestions defaults to the student inbox and omits empty filters', async () => {
    api.get.mockResolvedValue({ data: { items: [], totalCount: 0 } });

    const result = await fetchQuestions();

    expect(api.get).toHaveBeenCalledWith('/questions', {
      params: { scope: 'student', courseId: undefined, status: undefined, page: 1, pageSize: 20 },
    });
    expect(result).toEqual({ items: [], totalCount: 0 });
  });

  it('fetchQuestions passes the staff scope and filters through', async () => {
    await fetchQuestions({ scope: 'staff', courseId: 7, status: 'Open', page: 3, pageSize: 5 });

    expect(api.get).toHaveBeenCalledWith('/questions', {
      params: { scope: 'staff', courseId: 7, status: 'Open', page: 3, pageSize: 5 },
    });
  });

  it('fetchLessonQuestions scopes the request to one lesson', async () => {
    await fetchLessonQuestions(42);

    expect(api.get).toHaveBeenCalledWith('/lessons/42/questions', { params: { page: 1, pageSize: 20 } });
  });

  it('fetchQuestionThread requests a single thread', async () => {
    api.get.mockResolvedValue({ data: { id: 5 } });

    expect(await fetchQuestionThread(5)).toEqual({ id: 5 });
    expect(api.get).toHaveBeenCalledWith('/questions/5');
  });

  it('askQuestion posts the lesson, subject and body', async () => {
    await askQuestion({ lessonId: 42, title: 'Subject', body: 'Body' });

    expect(api.post).toHaveBeenCalledWith('/questions', { lessonId: 42, title: 'Subject', body: 'Body' });
  });

  it('postQuestionMessage posts a reply to the thread', async () => {
    await postQuestionMessage(5, 'A reply');

    expect(api.post).toHaveBeenCalledWith('/questions/5/messages', { body: 'A reply' });
  });

  it('closeQuestion and reopenQuestion hit their own routes', async () => {
    await closeQuestion(5);
    await reopenQuestion(5);

    expect(api.post).toHaveBeenCalledWith('/questions/5/close');
    expect(api.post).toHaveBeenCalledWith('/questions/5/reopen');
  });

  it('fetchUnreadQuestionCount unwraps the count', async () => {
    api.get.mockResolvedValue({ data: { count: 4 } });

    expect(await fetchUnreadQuestionCount()).toBe(4);
    expect(api.get).toHaveBeenCalledWith('/questions/unread-count');
  });

  it('fetchCourseInstructors reads a course teaching team', async () => {
    api.get.mockResolvedValue({ data: [{ userId: 1 }] });

    expect(await fetchCourseInstructors(7)).toEqual([{ userId: 1 }]);
    expect(api.get).toHaveBeenCalledWith('/courses/7/instructors');
  });

  it('assignCourseInstructor posts the email', async () => {
    await assignCourseInstructor(7, 'instructor@lf.test');

    expect(api.post).toHaveBeenCalledWith('/courses/7/instructors', { email: 'instructor@lf.test' });
  });

  it('removeCourseInstructor deletes by user id', async () => {
    await removeCourseInstructor(7, 9);

    expect(api.delete).toHaveBeenCalledWith('/courses/7/instructors/9');
  });
});
