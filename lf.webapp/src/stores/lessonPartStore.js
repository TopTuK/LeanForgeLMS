import { defineStore } from 'pinia';
import { ref } from 'vue';

const STORAGE_KEY = 'leanforge-lesson-parts';

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
    needsReupload: false,
    ...extras,
  };
}

function serializePart(part) {
  return {
    id: part.id,
    type: part.type,
    sortOrder: part.sortOrder,
    html: part.type === 'text' ? (part.html ?? '') : '',
    fileName: part.fileName ?? null,
    mimeType: part.mimeType ?? null,
  };
}

function deserializePart(saved) {
  const isMedia = saved.type !== 'text';
  return createPart(saved.type, {
    id: saved.id,
    sortOrder: saved.sortOrder ?? 0,
    html: saved.html ?? '',
    fileName: saved.fileName ?? null,
    mimeType: saved.mimeType ?? null,
    objectUrl: null,
    needsReupload: isMedia && Boolean(saved.fileName),
  });
}

function reindex(parts) {
  return parts.map((part, index) => ({ ...part, sortOrder: index }));
}

function signatureOf(parts) {
  return JSON.stringify((parts ?? []).map(serializePart));
}

function readStorage() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return {};
    const parsed = JSON.parse(raw);
    return parsed && typeof parsed === 'object' ? parsed : {};
  } catch {
    return {};
  }
}

function writeStorage(all) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(all));
  } catch {
    // Quota or private-mode failures should not break editing.
  }
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

  function persist(id) {
    const key = lessonKey(id);
    const all = readStorage();
    all[key] = (partsByLessonId.value[key] ?? []).map(serializePart);
    writeStorage(all);
  }

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
      if (part.objectUrl) urls.add(part.objectUrl);
    }
    return urls;
  }

  function revokeIfOrphaned(id, url) {
    if (!url) return;
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

  function ensureLoaded(lessonId, apiContent = '') {
    const key = lessonKey(lessonId);
    if (partsByLessonId.value[key]) return;

    const stored = readStorage()[key];
    if (Array.isArray(stored)) {
      partsByLessonId.value = {
        ...partsByLessonId.value,
        [key]: reindex(
          stored
            .filter((item) => item && PART_TYPES.includes(item.type))
            .map(deserializePart),
        ),
      };
      commitSaved(lessonId);
      return;
    }

    const html = typeof apiContent === 'string' ? apiContent.trim() : '';
    const seeded = html
      ? [createPart('text', { html: apiContent, sortOrder: 0 })]
      : [];

    partsByLessonId.value = {
      ...partsByLessonId.value,
      [key]: seeded,
    };
    persist(lessonId);
    commitSaved(lessonId);
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

  function setMediaFile(lessonId, partId, file) {
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
          ? {
              ...item,
              fileName: file.name,
              mimeType: file.type,
              objectUrl,
              needsReupload: false,
            }
          : item
      )),
    );
    revokeIfOrphaned(lessonId, previousUrl);
    return { ok: true };
  }

  function commit(lessonId) {
    persist(lessonId);
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
