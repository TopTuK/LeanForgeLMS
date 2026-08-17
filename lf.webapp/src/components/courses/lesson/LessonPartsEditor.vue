<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useI18n } from 'vue-i18n';
import { useLessonPartStore } from '@/stores/lessonPartStore';
import LessonPartBlock from './LessonPartBlock.vue';
import LessonPartToolbox from './LessonPartToolbox.vue';
import LessonTextPart from './LessonTextPart.vue';
import LessonMediaPart from './LessonMediaPart.vue';

const props = defineProps({
  lessonId: { type: Number, required: true },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['error']);

const { t } = useI18n();
const partStore = useLessonPartStore();
const { revision } = storeToRefs(partStore);

const toolboxAnchor = ref(null);

const parts = computed(() => {
  revision.value;
  return partStore.partsFor(props.lessonId);
});

const isEmpty = computed(() => parts.value.length === 0);

function openToolbox(anchor) {
  if (props.disabled) return;
  if (toolboxAnchor.value === anchor) {
    closeToolbox();
    return;
  }
  toolboxAnchor.value = anchor;
}

function closeToolbox() {
  toolboxAnchor.value = null;
}

function addType({ type, index }) {
  partStore.addPart(props.lessonId, type, index);
  closeToolbox();
}

function onFile(part, file) {
  const result = partStore.setMediaFile(props.lessonId, part.id, file);
  if (!result.ok) emit('error', t(result.errorKey));
}

function onDocumentPointerDown(event) {
  if (toolboxAnchor.value == null) return;
  const target = event.target;
  if (!(target instanceof Element)) {
    closeToolbox();
    return;
  }
  if (target.closest('.part-toolbox, .part-block__plus, .parts-editor__add')) return;
  closeToolbox();
}

function onKeydown(event) {
  if (event.key === 'Escape') closeToolbox();
}

onMounted(() => {
  document.addEventListener('pointerdown', onDocumentPointerDown);
  window.addEventListener('keydown', onKeydown);
});

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocumentPointerDown);
  window.removeEventListener('keydown', onKeydown);
});
</script>

<template>
  <div class="parts-editor">
    <div
      v-if="isEmpty"
      class="parts-editor__empty"
    >
      <p>{{ t('courses.lessonEditor.parts.empty') }}</p>
      <button
        type="button"
        class="parts-editor__add"
        :disabled="disabled"
        @click="openToolbox('empty')"
      >
        + {{ t('courses.lessonEditor.parts.add') }}
      </button>
      <LessonPartToolbox
        v-if="toolboxAnchor === 'empty'"
        :insert-index="0"
        @select="addType"
      />
    </div>

    <template v-else>
      <LessonPartBlock
        v-for="(part, index) in parts"
        :key="part.id"
        :index="index"
        :total="parts.length"
        :insert-index="index + 1"
        :toolbox-open="toolboxAnchor === index"
        :disabled="disabled"
        @add="openToolbox(index)"
        @select-type="addType"
        @move-up="partStore.movePart(lessonId, part.id, 'up')"
        @move-down="partStore.movePart(lessonId, part.id, 'down')"
        @remove="partStore.removePart(lessonId, part.id)"
      >
        <LessonTextPart
          v-if="part.type === 'text'"
          :model-value="part.html"
          :disabled="disabled"
          @update:model-value="partStore.updateText(lessonId, part.id, $event)"
        />
        <LessonMediaPart
          v-else
          :type="part.type"
          :file-name="part.fileName"
          :object-url="part.objectUrl"
          :needs-reupload="part.needsReupload"
          :disabled="disabled"
          @file="onFile(part, $event)"
        />
      </LessonPartBlock>

      <div class="parts-editor__footer">
        <button
          type="button"
          class="parts-editor__add"
          :disabled="disabled"
          @click="openToolbox('footer')"
        >
          + {{ t('courses.lessonEditor.parts.add') }}
        </button>
        <LessonPartToolbox
          v-if="toolboxAnchor === 'footer'"
          :insert-index="parts.length"
          @select="addType"
        />
      </div>
    </template>
  </div>
</template>

<style scoped>
.parts-editor {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.parts-editor__empty {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.85rem;
  min-height: 10rem;
  padding: 1.35rem 1.25rem;
  border: 1px dashed var(--industrial-line-strong);
  border-radius: 0.35rem;
  background: var(--color-surface-950);
}

.parts-editor__empty p {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
}

.parts-editor__footer {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.55rem;
  padding-left: 3.05rem;
}

.parts-editor__add {
  padding: 0.45rem 0.15rem;
  border: 0;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  cursor: pointer;
}

.parts-editor__add:hover:not(:disabled) {
  color: var(--color-accent-coral-dark);
}

.parts-editor__add:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
