import { defineStore } from 'pinia';
import { ref } from 'vue';
import { uploadLessonMedia, uploadLessonFiles, replaceLessonParts, fetchLessonMediaObjectUrl } from '@/services/lessonPartService';

export const PART_TYPES = ['text', 'image', 'video', 'audio', 'quiz', 'files'];

export const DEFAULT_QUIZ_PASS_THRESHOLD = 60;

export function createBlankQuizOption() {
  return { id: crypto.randomUUID(), text: '', isCorrect: false };
}

export function createBlankQuizQuestion() {
  return {
    id: crypto.randomUUID(),
    text: '',
    questionType: 'single',
    options: [createBlankQuizOption(), createBlankQuizOption()],
  };
}

export const MEDIA_ACCEPT = {
  image: ['image/png', 'image/jpeg', 'image/jpg', 'image/webp', 'image/gif'],
  video: ['video/mp4', 'video/webm'],
  audio: ['audio/mpeg', 'audio/mp3', 'audio/wav', 'audio/wave', 'audio/x-wav', 'audio/ogg', 'audio/webm'],
};

export const MEDIA_ACCEPT_ATTR = {
  image: 'image/png,image/jpeg,image/webp,image/gif',
  video: 'video/mp4,video/webm',
  audio: 'audio/mpeg,audio/wav,audio/ogg,audio/webm',
};

function lessonKey(lessonId) {
  return String(lessonId);
}

function createPart(type, extras = {}) {
  return {
    id: crypto.randomUUID(),
    type,
    sortOrder: 0,
    html: '',
    fileName: null,
    mimeType: null,
    objectUrl: null,
    storageObjectId: null,
    uploading: false,
    uploadError: false,
    quizQuestions: [],
    quizPassThreshold: DEFAULT_QUIZ_PASS_THRESHOLD,
    files: [],
    ...extras,
  };
}

function serializePart(part) {
  if (part.type === 'quiz') {
    return {
      id: part.id,
      type: part.type,
      sortOrder: part.sortOrder,
      quizQuestions: part.quizQuestions ?? [],
      quizPassThreshold: part.quizPassThreshold ?? DEFAULT_QUIZ_PASS_THRESHOLD,
    };
  }

  if (part.type === 'files') {
    return {
      id: part.id,
      type: part.type,
      sortOrder: part.sortOrder,
      files: (part.files ?? []).map((f) => ({ fileName: f.fileName, storageObjectId: f.storageObjectId })),
    };
  }

  return {
    id: part.id,
    type: part.type,
    sortOrder: part.sortOrder,
    html: part.type === 'text' ? (part.html ?? '') : '',
    storageObjectId: part.type === 'text' ? null : (part.storageObjectId ?? null),
  };
}

function toApiPart(part) {
  if (part.type === 'quiz') {
    return {
      partType: 'quiz',
      html: null,
      storageObjectId: null,
      quizQuestions: (part.quizQuestions ?? []).map((q, qIndex) => ({
        text: q.text ?? '',
        questionType: q.questionType === 'multiple' ? 'MultipleChoice' : 'SingleChoice',
        sortOrder: qIndex,
        options: (q.options ?? []).map((o, oIndex) => ({
          text: o.text ?? '',
          isCorrect: !!o.isCorrect,
          sortOrder: oIndex,
        })),
      })),
      quizPassThresholdPercent: part.quizPassThreshold ?? DEFAULT_QUIZ_PASS_THRESHOLD,
    };
  }

  if (part.type === 'files') {
    return {
      partType: 'files',
      html: null,
      storageObjectId: null,
      files: (part.files ?? []).map((f) => ({ fileName: f.fileName, storageObjectId: f.storageObjectId })),
    };
  }

  return {
    partType: part.type,
    html: part.type === 'text' ? (part.html ?? '') : null,
    storageObjectId: part.type === 'text' ? null : (part.storageObjectId ?? null),
  };
}

function reindex(parts) {
  return parts.map((part, index) => ({ ...part, sortOrder: index }));
}

function signatureOf(parts) {
  return JSON.stringify((parts ?? []).map(serializePart));
}

export function isAcceptedFile(type, file) {
  const allowed = MEDIA_ACCEPT[type];
  if (!allowed || !file?.type) return false;
  return allowed.includes(file.type);
}

export const useLessonPartStore = defineStore('lessonParts', () => {
  const partsByLessonId = ref({});
  const savedByLessonId = ref({});
  const revision = ref(0);

  function bump() {
    revision.value += 1;
  }

  function cloneParts(parts) {
    return (parts ?? []).map((part) => ({ ...part }));
  }

  function referencedUrls(id) {
    const key = lessonKey(id);
    const urls = new Set();
    for (const part of [...(partsByLessonId.value[key] ?? []), ...(savedByLessonId.value[key] ?? [])]) {
      if (part.objectUrl?.startsWith('blob:')) urls.add(part.objectUrl);
    }
    return urls;
  }

  function revokeIfOrphaned(id, url) {
    if (!url?.startsWith('blob:')) return;
    if (!referencedUrls(id).has(url)) URL.revokeObjectURL(url);
  }

  function setParts(id, parts) {
    const key = lessonKey(id);
    partsByLessonId.value = {
      ...partsByLessonId.value,
      [key]: reindex(parts),
    };
    bump();
  }

  function commitSaved(id) {
    const key = lessonKey(id);
    savedByLessonId.value = {
      ...savedByLessonId.value,
      [key]: cloneParts(partsByLessonId.value[key] ?? []),
    };
    bump();
  }

  function partsFor(lessonId) {
    return partsByLessonId.value[lessonKey(lessonId)] ?? [];
  }

  function isDirty(lessonId) {
    const key = lessonKey(lessonId);
    return signatureOf(partsByLessonId.value[key]) !== signatureOf(savedByLessonId.value[key]);
  }

  function hasPendingUploads(lessonId) {
    return partsFor(lessonId).some((part) => part.uploading);
  }

  async function ensureLoaded(lessonId, apiContent = '', apiParts = [], mediaContext = {}) {
    const key = lessonKey(lessonId);
    if (partsByLessonId.value[key]) return;

    let seeded;
    if (Array.isArray(apiParts) && apiParts.length > 0) {
      seeded = apiParts.map((p) => createPart(String(p.partType).toLowerCase(), {
        id: p.id,
        sortOrder: p.sortOrder,
        html: p.html ?? '',
        storageObjectId: p.storageObjectId ?? null,
        // p.mediaUrl is an authenticated API route, not something a plain <img>/<video>
        // src can load directly — resolve it to a blob object URL below instead.
        objectUrl: null,
        quizQuestions: Array.isArray(p.quizQuestions) ? p.quizQuestions.map((q) => ({
          id: crypto.randomUUID(),
          text: q.text ?? '',
          questionType: q.questionType === 'MultipleChoice' ? 'multiple' : 'single',
          options: (q.options ?? []).map((o) => ({
            id: crypto.randomUUID(),
            text: o.text ?? '',
            isCorrect: !!o.isCorrect,
          })),
        })) : [],
        quizPassThreshold: p.quizPassThresholdPercent ?? DEFAULT_QUIZ_PASS_THRESHOLD,
        files: Array.isArray(p.files) ? p.files.map((f) => ({
          id: f.id,
          fileName: f.fileName,
          storageObjectId: f.storageObjectId,
          sizeBytes: f.sizeBytes,
          contentType: f.contentType,
          downloadUrl: f.downloadUrl,
        })) : [],
      }));
    } else {
      const html = typeof apiContent === 'string' ? apiContent.trim() : '';
      seeded = html ? [createPart('text', { html: apiContent, sortOrder: 0 })] : [];
    }

    partsByLessonId.value = {
      ...partsByLessonId.value,
      [key]: reindex(seeded),
    };
    commitSaved(lessonId);

    const { courseId, chapterId } = mediaContext;
    if (courseId == null || chapterId == null) return;

    const mediaParts = seeded.filter((part) => part.type !== 'text' && part.storageObjectId);
    await Promise.all(mediaParts.map(async (part) => {
      try {
        const objectUrl = await fetchLessonMediaObjectUrl(courseId, chapterId, lessonId, part.id);
        setParts(
          lessonId,
          partsFor(lessonId).map((item) => (item.id === part.id ? { ...item, objectUrl } : item)),
        );
      } catch {
        // Leave objectUrl null; the media block just shows its empty/dropzone state.
      }
    }));
  }

  function addPart(lessonId, type, index) {
    if (!PART_TYPES.includes(type)) return null;
    const current = [...partsFor(lessonId)];
    const extras = type === 'quiz' ? { quizQuestions: [createBlankQuizQuestion()] } : {};
    const part = createPart(type, extras);
    const insertAt = index == null ? current.length : Math.max(0, Math.min(index, current.length));
    current.splice(insertAt, 0, part);
    setParts(lessonId, current);
    return part;
  }

  function removePart(lessonId, partId) {
    const removed = partsFor(lessonId).find((part) => part.id === partId);
    setParts(lessonId, partsFor(lessonId).filter((part) => part.id !== partId));
    revokeIfOrphaned(lessonId, removed?.objectUrl);
  }

  function movePart(lessonId, partId, direction) {
    const current = [...partsFor(lessonId)];
    const from = current.findIndex((part) => part.id === partId);
    if (from < 0) return;
    const to = direction === 'up' ? from - 1 : from + 1;
    if (to < 0 || to >= current.length) return;
    const [part] = current.splice(from, 1);
    current.splice(to, 0, part);
    setParts(lessonId, current);
  }

  function reorderParts(lessonId, nextParts) {
    if (!Array.isArray(nextParts)) return;
    setParts(lessonId, nextParts);
  }

  function updateText(lessonId, partId, html) {
    setParts(
      lessonId,
      partsFor(lessonId).map((part) => (
        part.id === partId && part.type === 'text'
          ? { ...part, html }
          : part
      )),
    );
  }

  function updateQuiz(lessonId, partId, { quizQuestions, quizPassThreshold }) {
    setParts(
      lessonId,
      partsFor(lessonId).map((part) => (
        part.id === partId && part.type === 'quiz'
          ? { ...part, quizQuestions, quizPassThreshold }
          : part
      )),
    );
  }

  async function setMediaFile(lessonId, partId, file) {
    const current = partsFor(lessonId);
    const part = current.find((item) => item.id === partId);
    if (!part || part.type === 'text') return { ok: false, errorKey: 'courses.lessonEditor.parts.invalid_type' };
    if (!isAcceptedFile(part.type, file)) {
      return { ok: false, errorKey: 'courses.lessonEditor.parts.invalid_type' };
    }

    const previousUrl = part.objectUrl;
    const objectUrl = URL.createObjectURL(file);
    setParts(
      lessonId,
      current.map((item) => (
        item.id === partId
          ? { ...item, fileName: file.name, mimeType: file.type, objectUrl, uploading: true, uploadError: false }
          : item
      )),
    );
    revokeIfOrphaned(lessonId, previousUrl);

    try {
      const { storageObjectId } = await uploadLessonMedia(file);
      setParts(
        lessonId,
        partsFor(lessonId).map((item) => (
          item.id === partId ? { ...item, storageObjectId, uploading: false } : item
        )),
      );
      return { ok: true };
    } catch {
      setParts(
        lessonId,
        partsFor(lessonId).map((item) => (
          item.id === partId ? { ...item, uploading: false, uploadError: true } : item
        )),
      );
      return { ok: false, errorKey: 'courses.lessonEditor.parts.upload_error' };
    }
  }

  async function addFilesToPart(lessonId, partId, fileList) {
    const files = Array.from(fileList ?? []);
    if (files.length === 0) return { ok: true };

    const part = partsFor(lessonId).find((item) => item.id === partId);
    if (!part || part.type !== 'files') return { ok: false, errorKey: 'courses.lessonEditor.parts.invalid_type' };

    setParts(
      lessonId,
      partsFor(lessonId).map((item) => (item.id === partId ? { ...item, uploading: true, uploadError: false } : item)),
    );

    try {
      const uploaded = await uploadLessonFiles(files);
      setParts(
        lessonId,
        partsFor(lessonId).map((item) => (
          item.id === partId
            ? {
              ...item,
              uploading: false,
              files: [
                ...(item.files ?? []),
                ...uploaded.map((u) => ({
                  id: crypto.randomUUID(),
                  fileName: u.fileName,
                  storageObjectId: u.storageObjectId,
                  sizeBytes: u.sizeBytes,
                  contentType: u.contentType,
                  downloadUrl: null,
                })),
              ],
            }
            : item
        )),
      );
      return { ok: true };
    } catch {
      setParts(
        lessonId,
        partsFor(lessonId).map((item) => (item.id === partId ? { ...item, uploading: false, uploadError: true } : item)),
      );
      return { ok: false, errorKey: 'courses.lessonEditor.parts.upload_error' };
    }
  }

  function removeFileFromPart(lessonId, partId, fileId) {
    setParts(
      lessonId,
      partsFor(lessonId).map((item) => (
        item.id === partId
          ? { ...item, files: (item.files ?? []).filter((f) => f.id !== fileId) }
          : item
      )),
    );
  }

  async function commit(courseId, chapterId, lessonId) {
    const payload = partsFor(lessonId).map(toApiPart);
    await replaceLessonParts(courseId, chapterId, lessonId, payload);
    commitSaved(lessonId);
  }

  function discard(lessonId) {
    const key = lessonKey(lessonId);
    const previous = partsByLessonId.value[key] ?? [];
    const restored = cloneParts(savedByLessonId.value[key] ?? []);
    const restoredUrls = new Set(restored.map((part) => part.objectUrl).filter(Boolean));

    partsByLessonId.value = {
      ...partsByLessonId.value,
      [key]: restored,
    };
    bump();

    for (const part of previous) {
      if (part.objectUrl && !restoredUrls.has(part.objectUrl)) {
        revokeIfOrphaned(lessonId, part.objectUrl);
      }
    }
  }

  return {
    partsByLessonId,
    revision,
    partsFor,
    isDirty,
    hasPendingUploads,
    ensureLoaded,
    addPart,
    removePart,
    movePart,
    reorderParts,
    updateText,
    updateQuiz,
    setMediaFile,
    addFilesToPart,
    removeFileFromPart,
    commit,
    discard,
  };
});
