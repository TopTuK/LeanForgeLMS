<script setup>
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Image, Video, AudioLines } from 'lucide-vue-next';
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

const TypeIcon = computed(() => {
  if (props.type === 'image') return Image;
  if (props.type === 'video') return Video;
  return AudioLines;
});

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
      <span class="media-part__drop-icon">
        <component
          :is="TypeIcon"
          :size="22"
        />
      </span>
      <span class="media-part__drop-label">{{ uploadLabel }}</span>
    </button>
  </div>
</template>

<style scoped>
.media-part {
  min-height: 6rem;
  padding: 0.35rem;
}

.media-part__input {
  display: none;
}

.media-part__dropzone {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  width: 100%;
  min-height: 8rem;
  padding: 1.35rem 1.25rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.65rem;
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
  cursor: pointer;
  transition: border-color 0.12s ease, background-color 0.12s ease, color 0.12s ease;
}

.media-part__drop-icon {
  display: inline-grid;
  place-items: center;
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 0.65rem;
  background: var(--color-surface-900);
  color: var(--color-ink-muted);
}

.media-part__drop-label {
  font-size: 0.9rem;
  font-weight: 600;
  text-align: center;
}

.media-part__dropzone:hover:not(:disabled),
.media-part__dropzone--drag {
  border-color: var(--color-accent-coral);
  background: color-mix(in srgb, var(--color-accent-coral) 6%, var(--color-surface-950));
  color: var(--color-ink);
}

.media-part__dropzone:hover:not(:disabled) .media-part__drop-icon,
.media-part__dropzone--drag .media-part__drop-icon {
  background: color-mix(in srgb, var(--color-accent-coral) 14%, transparent);
  color: var(--color-accent-coral-dark);
}

.media-part__dropzone:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.media-part__preview {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  padding: 0.35rem;
}

.media-part__image {
  display: block;
  max-width: 100%;
  max-height: 22rem;
  margin: 0 auto;
  border-radius: 0.55rem;
}

.media-part__player {
  display: block;
  width: 100%;
  max-height: 22rem;
  border-radius: 0.55rem;
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
}

.media-part__replace {
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-accent-coral-dark);
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
}

.media-part__replace:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
