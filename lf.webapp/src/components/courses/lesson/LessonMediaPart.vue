<script setup>
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { MEDIA_ACCEPT_ATTR } from '@/stores/lessonPartStore';

const props = defineProps({
  type: {
    type: String,
    required: true,
    validator: (value) => ['image', 'video', 'audio'].includes(value),
  },
  fileName: { type: String, default: null },
  objectUrl: { type: String, default: null },
  uploading: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['file']);

const { t } = useI18n();
const dragging = ref(false);
const inputRef = ref(null);

const accept = computed(() => MEDIA_ACCEPT_ATTR[props.type] ?? '');
const hasPreview = computed(() => Boolean(props.objectUrl));

const uploadLabel = computed(() => {
  if (props.type === 'image') return t('courses.lessonEditor.parts.upload_image');
  if (props.type === 'video') return t('courses.lessonEditor.parts.upload_video');
  return t('courses.lessonEditor.parts.upload_audio');
});

function openPicker() {
  if (props.disabled || props.uploading) return;
  inputRef.value?.click();
}

function onFile(file) {
  if (!file || props.disabled || props.uploading) return;
  emit('file', file);
}

function onInputChange(event) {
  const file = event.target.files?.[0];
  event.target.value = '';
  onFile(file);
}

function onDrop(event) {
  event.preventDefault();
  dragging.value = false;
  onFile(event.dataTransfer?.files?.[0]);
}

function onDragOver(event) {
  event.preventDefault();
  if (!props.disabled) dragging.value = true;
}

function onDragLeave() {
  dragging.value = false;
}
</script>

<template>
  <div class="media-part">
    <input
      ref="inputRef"
      type="file"
      class="media-part__input"
      :accept="accept"
      :disabled="disabled"
      @change="onInputChange"
    >

    <div
      v-if="hasPreview"
      class="media-part__preview"
    >
      <img
        v-if="type === 'image'"
        :src="objectUrl"
        :alt="fileName || ''"
        class="media-part__image"
      >
      <video
        v-else-if="type === 'video'"
        :src="objectUrl"
        class="media-part__player"
        controls
        preload="metadata"
      />
      <audio
        v-else
        :src="objectUrl"
        class="media-part__player media-part__player--audio"
        controls
        preload="metadata"
      />
      <div class="media-part__meta">
        <span class="media-part__name">{{ fileName }}</span>
        <span
          v-if="uploading"
          class="media-part__status"
        >
          {{ t('courses.lessonEditor.parts.uploading') }}
        </span>
        <button
          type="button"
          class="media-part__replace"
          :disabled="disabled || uploading"
          @click="openPicker"
        >
          {{ t('courses.lessonEditor.parts.replace') }}
        </button>
      </div>
    </div>

    <button
      v-else
      type="button"
      class="media-part__dropzone"
      :class="{ 'media-part__dropzone--drag': dragging }"
      :disabled="disabled"
      @click="openPicker"
      @dragover="onDragOver"
      @dragleave="onDragLeave"
      @drop="onDrop"
    >
      <span>{{ uploadLabel }}</span>
    </button>
  </div>
</template>

<style scoped>
.media-part {
  min-height: 8rem;
}

.media-part__input {
  display: none;
}

.media-part__dropzone {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  min-height: 8.5rem;
  padding: 1.25rem 1.5rem;
  border: 1px dashed var(--industrial-line-strong);
  border-radius: 0.25rem;
  background:
    linear-gradient(
      180deg,
      color-mix(in srgb, var(--industrial-panel) 70%, transparent) 0%,
      var(--color-surface-950) 100%
    );
  color: var(--color-ink-muted);
  font-size: 0.92rem;
  font-weight: 600;
  text-align: center;
  cursor: pointer;
}

.media-part__dropzone:hover:not(:disabled),
.media-part__dropzone--drag {
  border-color: var(--color-accent-coral);
  color: var(--color-ink);
}

.media-part__dropzone:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.media-part__preview {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 0.85rem;
}

.media-part__image {
  display: block;
  max-width: 100%;
  max-height: 22rem;
  margin: 0 auto;
  border: 1px solid var(--industrial-line);
  border-radius: 0.25rem;
}

.media-part__player {
  display: block;
  width: 100%;
  max-height: 22rem;
  background: var(--color-surface-900);
}

.media-part__player--audio {
  height: 2.6rem;
}

.media-part__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.65rem;
}

.media-part__name {
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 600;
}

.media-part__status {
  color: var(--color-accent-coral-dark);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.media-part__replace {
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-accent-coral-dark);
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.media-part__replace:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
