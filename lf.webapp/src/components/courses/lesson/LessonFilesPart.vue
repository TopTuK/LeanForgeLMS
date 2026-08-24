<script setup>
import { ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Paperclip, X } from 'lucide-vue-next';

const props = defineProps({
  files: { type: Array, default: () => [] },
  uploading: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['files', 'remove']);

const { t } = useI18n();
const dragging = ref(false);
const inputRef = ref(null);

function formatSize(bytes) {
  if (!Number.isFinite(bytes) || bytes < 0) return '';
  if (bytes < 1024) return `${bytes} B`;
  const units = ['KB', 'MB', 'GB'];
  let value = bytes / 1024;
  let unitIndex = 0;
  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }
  return `${value.toFixed(value >= 10 ? 0 : 1)} ${units[unitIndex]}`;
}

function openPicker() {
  if (props.disabled || props.uploading) return;
  inputRef.value?.click();
}

function onFiles(fileList) {
  if (!fileList || fileList.length === 0 || props.disabled || props.uploading) return;
  emit('files', fileList);
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
  <div class="files-part">
    <input
      ref="inputRef"
      type="file"
      class="files-part__input"
      multiple
      :disabled="disabled"
      @change="onInputChange"
    >

    <ul
      v-if="files.length > 0"
      class="files-part__list"
    >
      <li
        v-for="file in files"
        :key="file.id"
        class="files-part__item"
      >
        <Paperclip
          :size="16"
          class="files-part__item-icon"
        />
        <span class="files-part__item-name">{{ file.fileName }}</span>
        <span
          v-if="file.sizeBytes != null"
          class="files-part__item-size"
        >{{ formatSize(file.sizeBytes) }}</span>
        <button
          type="button"
          class="files-part__item-remove"
          :disabled="disabled"
          :aria-label="t('courses.lessonEditor.parts.files.remove')"
          @click="emit('remove', file.id)"
        >
          <X :size="14" />
        </button>
      </li>
    </ul>

    <button
      type="button"
      class="files-part__dropzone"
      :class="{ 'files-part__dropzone--drag': dragging }"
      :disabled="disabled || uploading"
      @click="openPicker"
      @dragover="onDragOver"
      @dragleave="onDragLeave"
      @drop="onDrop"
    >
      <span class="files-part__drop-icon">
        <Paperclip :size="20" />
      </span>
      <span class="files-part__drop-label">
        {{ uploading ? t('courses.lessonEditor.parts.uploading') : t('courses.lessonEditor.parts.files.upload') }}
      </span>
    </button>
  </div>
</template>

<style scoped>
.files-part {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  min-height: 4rem;
  padding: 0.35rem;
}

.files-part__input {
  display: none;
}

.files-part__list {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.files-part__item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.65rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.55rem;
  background: var(--color-surface-950);
}

.files-part__item-icon {
  flex: none;
  color: var(--color-ink-muted);
}

.files-part__item-name {
  overflow: hidden;
  flex: 1 1 auto;
  min-width: 0;
  color: var(--color-ink);
  font-size: 0.85rem;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.files-part__item-size {
  flex: none;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
}

.files-part__item-remove {
  display: inline-grid;
  flex: none;
  place-items: center;
  width: 1.6rem;
  height: 1.6rem;
  border: 0;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink-muted);
  cursor: pointer;
}

.files-part__item-remove:hover:not(:disabled) {
  background: var(--color-surface-900);
  color: var(--color-ink);
}

.files-part__item-remove:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.files-part__dropzone {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  width: 100%;
  min-height: 6rem;
  padding: 1.1rem 1.25rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.65rem;
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
  cursor: pointer;
  transition: border-color 0.12s ease, background-color 0.12s ease, color 0.12s ease;
}

.files-part__drop-icon {
  display: inline-grid;
  place-items: center;
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.6rem;
  background: var(--color-surface-900);
  color: var(--color-ink-muted);
}

.files-part__drop-label {
  font-size: 0.88rem;
  font-weight: 600;
  text-align: center;
}

.files-part__dropzone:hover:not(:disabled),
.files-part__dropzone--drag {
  border-color: var(--color-accent-coral);
  background: color-mix(in srgb, var(--color-accent-coral) 6%, var(--color-surface-950));
  color: var(--color-ink);
}

.files-part__dropzone:hover:not(:disabled) .files-part__drop-icon,
.files-part__dropzone--drag .files-part__drop-icon {
  background: color-mix(in srgb, var(--color-accent-coral) 14%, transparent);
  color: var(--color-accent-coral-dark);
}

.files-part__dropzone:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
