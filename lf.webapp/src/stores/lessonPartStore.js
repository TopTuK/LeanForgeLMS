import { defineStore } from 'pinia';
import { ref } from 'vue';
import { uploadLessonMedia, replaceLessonParts, fetchLessonMediaObjectUrl } from '@/services/lessonPartService';

export const PART_TYPES = ['text', 'image', 'video', 'audio'];

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
    ...extras,
  };
}

function serializePart(part) {
  return {
    id: part.id,
    type: part.type,
    sortOrder: part.sortOrder,
    html: part.type === 'text' ? (part.html ?? '') : '',
    storageObjectId: part.type === 'text' ? null : (part.storageObjectId ?? null),
  };
}

function toApiPart(part) {
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
    const part = createPart(type);
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
    updateText,
    setMediaFile,
    commit,
    discard,
  };
});
