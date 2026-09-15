<script setup>
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ImagePlus, X } from 'lucide-vue-next';
import { MEDIA_ACCEPT_ATTR } from '@/stores/lessonPartStore';
import LessonImageRow from './LessonImageRow.vue';

const props = defineProps({
  images: { type: Array, default: () => [] },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['files', 'remove']);

const { t } = useI18n();
const dragging = ref(false);
const inputRef = ref(null);

const isUploading = computed(() => props.images.some((image) => image.uploading));

const rowImages = computed(() => props.images.map((image) => ({
  key: image.id,
  id: image.id,
  src: image.objectUrl,
  alt: image.fileName || '',
  uploading: image.uploading,
  uploadError: image.uploadError,
})));

const dropLabel = computed(() => {
  if (isUploading.value) return t('courses.lessonEditor.parts.uploading');
  if (props.images.length > 0) return t('courses.lessonEditor.parts.images.add_more');
  return t('courses.lessonEditor.parts.images.upload');
});

function openPicker() {
  if (props.disabled || isUploading.value) return;
  inputRef.value?.click();
}

function onFiles(fileList) {
  if (!fileList || fileList.length === 0 || props.disabled || isUploading.value) return;
  // Copy before the input is reset, which empties the live FileList.
  emit('files', Array.from(fileList));
}

function onInputChange(event) {
  onFiles(event.target.files);
  event.target.value = '';
}

function onDrop(event) {
  event.preventDefault();
  dragging.value = false;
  onFiles(event.dataTransfer?.files);
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
  <div class="images-part">
    <input
      ref="inputRef"
      type="file"
      class="images-part__input"
      multiple
      :accept="MEDIA_ACCEPT_ATTR.image"
      :disabled="disabled"
      @change="onInputChange"
    >

    <LessonImageRow
      v-if="images.length > 0"
      :images="rowImages"
    >
      <template #item="{ image }">
        <span
          v-if="image.uploading"
          class="images-part__status"
        >
          {{ t('courses.lessonEditor.parts.uploading') }}
        </span>
        <span
          v-else-if="image.uploadError"
          class="images-part__status images-part__status--error"
        >
          {{ t('courses.lessonEditor.parts.images.upload_failed') }}
        </span>
        <button
          type="button"
          class="images-part__remove"
          :disabled="disabled || image.uploading"
          :aria-label="t('courses.lessonEditor.parts.images.remove')"
          :title="t('courses.lessonEditor.parts.images.remove')"
          @click="emit('remove', image.id)"
        >
          <X
            :size="14"
            aria-hidden="true"
          />
        </button>
      </template>
    </LessonImageRow>

    <button
      type="button"
      class="images-part__dropzone"
      :class="{
        'images-part__dropzone--drag': dragging,
        'images-part__dropzone--compact': images.length > 0,
      }"
      :disabled="disabled || isUploading"
      @click="openPicker"
      @dragover="onDragOver"
      @dragleave="onDragLeave"
      @drop="onDrop"
    >
      <span class="images-part__drop-icon">
        <ImagePlus
          :size="20"
          aria-hidden="true"
        />
      </span>
      <span class="images-part__drop-label">{{ dropLabel }}</span>
    </button>
  </div>
</template>

<style scoped>
.images-part {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  min-height: 6rem;
  padding: 0.35rem;
}

.images-part__input {
  display: none;
}

.images-part__status {
  position: absolute;
  bottom: 0.45rem;
  left: 0.45rem;
  padding: 0.2rem 0.45rem;
  border-radius: 0.35rem;
  background: rgb(15 23 42 / 0.72);
  color: white;
  font-size: 0.74rem;
  font-weight: 700;
}

.images-part__status--error {
  background: var(--color-accent-coral-dark);
}

.images-part__remove {
  position: absolute;
  top: 0.4rem;
  right: 0.4rem;
  display: inline-grid;
  place-items: center;
  width: 1.7rem;
  height: 1.7rem;
  padding: 0;
  border: 0;
  border-radius: 0.4rem;
  background: rgb(15 23 42 / 0.62);
  color: white;
  cursor: pointer;
}

.images-part__remove:hover:not(:disabled) {
  background: rgb(15 23 42 / 0.85);
}

.images-part__remove:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.images-part__dropzone {
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

.images-part__dropzone--compact {
  flex-direction: row;
  min-height: 0;
  padding: 0.65rem 1rem;
}

.images-part__drop-icon {
  display: inline-grid;
  place-items: center;
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.6rem;
  background: var(--color-surface-900);
  color: var(--color-ink-muted);
}

.images-part__drop-label {
  font-size: 0.88rem;
  font-weight: 600;
  text-align: center;
}

.images-part__dropzone:hover:not(:disabled),
.images-part__dropzone--drag {
  border-color: var(--color-accent-coral);
  background: color-mix(in srgb, var(--color-accent-coral) 6%, var(--color-surface-950));
  color: var(--color-ink);
}

.images-part__dropzone:hover:not(:disabled) .images-part__drop-icon,
.images-part__dropzone--drag .images-part__drop-icon {
  background: color-mix(in srgb, var(--color-accent-coral) 14%, transparent);
  color: var(--color-accent-coral-dark);
}

.images-part__dropzone:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
