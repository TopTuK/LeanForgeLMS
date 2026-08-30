import { describe, it, expect, beforeEach, vi } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import {
  isAcceptedFile,
  createBlankQuizOption,
  createBlankQuizQuestion,
  DEFAULT_QUIZ_PASS_THRESHOLD,
  PART_TYPES,
  useLessonPartStore,
} from '@/stores/lessonPartStore';
import { replaceLessonParts, uploadLessonMedia, uploadLessonFiles } from '@/services/lessonPartService';

vi.mock('@/services/lessonPartService', () => ({
  uploadLessonMedia: vi.fn(),
  uploadLessonFiles: vi.fn(),
  replaceLessonParts: vi.fn().mockResolvedValue(undefined),
  fetchLessonMediaObjectUrl: vi.fn(),
}));

describe('isAcceptedFile', () => {
  it('accepts a mime type listed for the part type', () => {
    expect(isAcceptedFile('image', { type: 'image/png' })).toBe(true);
    expect(isAcceptedFile('video', { type: 'video/mp4' })).toBe(true);
  });

  it('rejects a mime type not listed for the part type', () => {
    expect(isAcceptedFile('image', { type: 'application/pdf' })).toBe(false);
    expect(isAcceptedFile('audio', { type: 'video/mp4' })).toBe(false);
  });

  it('rejects unknown part types and files without a type', () => {
    expect(isAcceptedFile('text', { type: 'image/png' })).toBe(false);
    expect(isAcceptedFile('image', {})).toBe(false);
    expect(isAcceptedFile('image', null)).toBe(false);
  });
});

describe('quiz factory helpers', () => {
  it('creates a blank option with a unique id and default flags', () => {
    const a = createBlankQuizOption();
    const b = createBlankQuizOption();
    expect(a).toMatchObject({ text: '', isCorrect: false });
    expect(a.id).toEqual(expect.any(String));
    expect(a.id).not.toBe(b.id);
  });

  it('creates a single-choice question seeded with two options', () => {
    const q = createBlankQuizQuestion();
    expect(q).toMatchObject({ text: '', questionType: 'single' });
    expect(q.options).toHaveLength(2);
    expect(new Set(q.options.map((o) => o.id)).size).toBe(2);
  });
});

describe('useLessonPartStore', () => {
  const lessonId = 42;

  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('adds parts, reindexes sortOrder, and rejects unknown types', () => {
    const store = useLessonPartStore();

    expect(store.addPart(lessonId, 'not-a-type')).toBeNull();

    store.addPart(lessonId, 'text');
    store.addPart(lessonId, 'quiz');

    const parts = store.partsFor(lessonId);
    expect(parts.map((p) => p.type)).toEqual(['text', 'quiz']);
    expect(parts.map((p) => p.sortOrder)).toEqual([0, 1]);
    // a quiz part is seeded with one blank question
    expect(parts[1].quizQuestions).toHaveLength(1);
  });

  it('inserts a part at a given index', () => {
    const store = useLessonPartStore();
    store.addPart(lessonId, 'text');
    store.addPart(lessonId, 'image');
    store.addPart(lessonId, 'quiz', 1);

    expect(store.partsFor(lessonId).map((p) => p.type)).toEqual(['text', 'quiz', 'image']);
  });

  it('removes a part by id', () => {
    const store = useLessonPartStore();
    const a = store.addPart(lessonId, 'text');
    store.addPart(lessonId, 'image');

    store.removePart(lessonId, a.id);

    expect(store.partsFor(lessonId).map((p) => p.type)).toEqual(['image']);
  });

  it('moves a part up and clamps at the edges', () => {
    const store = useLessonPartStore();
    store.addPart(lessonId, 'text');
    const b = store.addPart(lessonId, 'image');

    store.movePart(lessonId, b.id, 'up');
    expect(store.partsFor(lessonId).map((p) => p.type)).toEqual(['image', 'text']);

    store.movePart(lessonId, b.id, 'up'); // already first, no-op
    expect(store.partsFor(lessonId).map((p) => p.type)).toEqual(['image', 'text']);
  });

  it('tracks dirty state against the last committed snapshot', async () => {
    const store = useLessonPartStore();
    await store.ensureLoaded(lessonId, '', []);
    expect(store.isDirty(lessonId)).toBe(false);

    store.addPart(lessonId, 'text');
    expect(store.isDirty(lessonId)).toBe(true);

    await store.commit(1, 2, lessonId);
    expect(store.isDirty(lessonId)).toBe(false);
  });

  it('maps parts to the API DTO shape on commit', async () => {
    const store = useLessonPartStore();
    await store.ensureLoaded(lessonId, '', []);

    const textPart = store.addPart(lessonId, 'text');
    store.updateText(lessonId, textPart.id, '<p>hi</p>');

    const quizPart = store.addPart(lessonId, 'quiz');
    store.updateQuiz(lessonId, quizPart.id, {
      quizQuestions: [{
        text: 'Q1',
        questionType: 'multiple',
        options: [
          { text: 'a', isCorrect: true },
          { text: 'b', isCorrect: false },
        ],
      }],
      quizPassThreshold: 75,
    });

    await store.commit(10, 20, lessonId);

    expect(replaceLessonParts).toHaveBeenCalledWith(10, 20, lessonId, expect.any(Array));
    const payload = replaceLessonParts.mock.calls[0][3];
    expect(payload[0]).toMatchObject({ partType: 'text', html: '<p>hi</p>', storageObjectId: null });
    expect(payload[1]).toMatchObject({
      partType: 'quiz',
      quizPassThresholdPercent: 75,
      quizQuestions: [expect.objectContaining({
        text: 'Q1',
        questionType: 'MultipleChoice',
        sortOrder: 0,
      })],
    });
  });

  it('exposes the supported part types and default threshold', () => {
    expect(PART_TYPES).toContain('quiz');
    expect(DEFAULT_QUIZ_PASS_THRESHOLD).toBe(60);
  });

  describe('media & file uploads', () => {
    it('setMediaFile rejects a file whose type is not accepted for the part', async () => {
      const store = useLessonPartStore();
      const part = store.addPart(lessonId, 'image');

      const result = await store.setMediaFile(lessonId, part.id, { name: 'a.pdf', type: 'application/pdf' });

      expect(result).toEqual({ ok: false, errorKey: 'courses.lessonEditor.parts.invalid_type' });
      expect(uploadLessonMedia).not.toHaveBeenCalled();
    });

    it('setMediaFile uploads an accepted file and stores the returned storageObjectId', async () => {
      uploadLessonMedia.mockResolvedValueOnce({ storageObjectId: 'obj-1' });
      const store = useLessonPartStore();
      const part = store.addPart(lessonId, 'image');
      const file = new File(['x'], 'a.png', { type: 'image/png' });

      const result = await store.setMediaFile(lessonId, part.id, file);

      expect(result).toEqual({ ok: true });
      const stored = store.partsFor(lessonId)[0];
      expect(stored).toMatchObject({ fileName: 'a.png', storageObjectId: 'obj-1', uploading: false });
    });

    it('setMediaFile flags an upload error when the request fails', async () => {
      uploadLessonMedia.mockRejectedValueOnce(new Error('boom'));
      const store = useLessonPartStore();
      const part = store.addPart(lessonId, 'image');

      const result = await store.setMediaFile(lessonId, part.id, new File(['x'], 'a.png', { type: 'image/png' }));

      expect(result).toEqual({ ok: false, errorKey: 'courses.lessonEditor.parts.upload_error' });
      expect(store.partsFor(lessonId)[0]).toMatchObject({ uploading: false, uploadError: true });
    });

    it('addFilesToPart appends the uploaded descriptors to a files part', async () => {
      uploadLessonFiles.mockResolvedValueOnce([
        { fileName: 'a.pdf', storageObjectId: 's1', sizeBytes: 10, contentType: 'application/pdf' },
      ]);
      const store = useLessonPartStore();
      const part = store.addPart(lessonId, 'files');

      const result = await store.addFilesToPart(lessonId, part.id, [new File(['a'], 'a.pdf')]);

      expect(result).toEqual({ ok: true });
      expect(store.partsFor(lessonId)[0].files).toEqual([
        expect.objectContaining({ fileName: 'a.pdf', storageObjectId: 's1', sizeBytes: 10 }),
      ]);
    });

    it('addFilesToPart rejects a non-files part', async () => {
      const store = useLessonPartStore();
      const part = store.addPart(lessonId, 'text');

      const result = await store.addFilesToPart(lessonId, part.id, [new File(['a'], 'a.pdf')]);

      expect(result).toEqual({ ok: false, errorKey: 'courses.lessonEditor.parts.invalid_type' });
    });

    it('removeFileFromPart drops the file with the given id', async () => {
      uploadLessonFiles.mockResolvedValueOnce([
        { fileName: 'a.pdf', storageObjectId: 's1' },
        { fileName: 'b.pdf', storageObjectId: 's2' },
      ]);
      const store = useLessonPartStore();
      const part = store.addPart(lessonId, 'files');
      await store.addFilesToPart(lessonId, part.id, [new File(['a'], 'a.pdf'), new File(['b'], 'b.pdf')]);

      const firstFileId = store.partsFor(lessonId)[0].files[0].id;
      store.removeFileFromPart(lessonId, part.id, firstFileId);

      expect(store.partsFor(lessonId)[0].files.map((f) => f.fileName)).toEqual(['b.pdf']);
    });

    it('discard restores the last committed snapshot', async () => {
      const store = useLessonPartStore();
      await store.ensureLoaded(lessonId, '', []);
      store.addPart(lessonId, 'text');
      await store.commit(1, 2, lessonId);

      store.addPart(lessonId, 'quiz');
      expect(store.partsFor(lessonId)).toHaveLength(2);

      store.discard(lessonId);

      expect(store.partsFor(lessonId).map((p) => p.type)).toEqual(['text']);
      expect(store.isDirty(lessonId)).toBe(false);
    });
  });
});
