<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useI18n } from 'vue-i18n';
import draggable from 'vuedraggable';
import { Plus } from 'lucide-vue-next';
import { useLessonPartStore } from '@/stores/lessonPartStore';
import LessonPartBlock from './LessonPartBlock.vue';
import LessonPartToolbox from './LessonPartToolbox.vue';
import LessonTextPart from './LessonTextPart.vue';
import LessonMediaPart from './LessonMediaPart.vue';
import LessonQuizPart from './LessonQuizPart.vue';

const props = defineProps({
  lessonId: { type: Number, required: true },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['error']);

const { t } = useI18n();
const partStore = useLessonPartStore();
const { revision } = storeToRefs(partStore);

const toolboxAnchor = ref(null);
const slashInsertIndex = ref(0);

const parts = computed({
  get() {
    revision.value;
    return partStore.partsFor(props.lessonId);
  },
  set(next) {
    partStore.reorderParts(props.lessonId, next);
  },
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

async function onFile(part, file) {
  const result = await partStore.setMediaFile(props.lessonId, part.id, file);
  if (!result.ok) emit('error', t(result.errorKey));
}

function onSlashRequest(part, index) {
  partStore.removePart(props.lessonId, part.id);
  slashInsertIndex.value = index;
  requestAnimationFrame(() => {
    if (partStore.partsFor(props.lessonId).length === 0) {
      toolboxAnchor.value = 'empty';
      return;
    }
    toolboxAnchor.value = 'slash';
  });
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
      <p class="parts-editor__hint">
        {{ t('courses.lessonEditor.parts.slash_hint') }}
      </p>
      <button
        type="button"
        class="parts-editor__add"
        :disabled="disabled"
        @click="openToolbox('empty')"
      >
        <Plus :size="16" />
        {{ t('courses.lessonEditor.parts.add') }}
      </button>
      <LessonPartToolbox
        v-if="toolboxAnchor === 'empty'"
        :insert-index="0"
        @select="addType"
      />
    </div>

    <template v-else>
      <draggable
        v-model="parts"
        item-key="id"
        handle=".part-block-drag"
        :disabled="disabled"
        class="parts-editor__list"
      >
        <template #item="{ element: part, index }">
          <LessonPartBlock
            :index="index"
            :total="parts.length"
            :insert-index="index + 1"
            :toolbox-open="toolboxAnchor === index"
            :disabled="disabled"
            @add="openToolbox(index)"
            @select-type="addType"
            @remove="partStore.removePart(lessonId, part.id)"
          >
            <LessonTextPart
              v-if="part.type === 'text'"
              :model-value="part.html"
              :disabled="disabled"
              @update:model-value="partStore.updateText(lessonId, part.id, $event)"
              @slash="onSlashRequest(part, index)"
            />
            <LessonQuizPart
              v-else-if="part.type === 'quiz'"
              :model-value="{ quizQuestions: part.quizQuestions, quizPassThreshold: part.quizPassThreshold }"
              :disabled="disabled"
              @update:model-value="partStore.updateQuiz(lessonId, part.id, $event)"
            />
            <LessonMediaPart
              v-else
              :type="part.type"
              :file-name="part.fileName"
              :object-url="part.objectUrl"
              :uploading="part.uploading"
              :disabled="disabled"
              @file="onFile(part, $event)"
            />
          </LessonPartBlock>
        </template>
      </draggable>

      <div class="parts-editor__footer">
        <button
          type="button"
          class="parts-editor__add"
          :disabled="disabled"
          @click="openToolbox('footer')"
        >
          <Plus :size="16" />
          {{ t('courses.lessonEditor.parts.add') }}
        </button>
        <LessonPartToolbox
          v-if="toolboxAnchor === 'footer'"
          :insert-index="parts.length"
          @select="addType"
        />
        <div
          v-if="toolboxAnchor === 'slash'"
          class="parts-editor__slash"
        >
          <LessonPartToolbox
            :insert-index="slashInsertIndex"
            @select="addType"
          />
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.parts-editor {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.parts-editor__list {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.parts-editor__empty {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.55rem;
  min-height: 9rem;
  padding: 1.75rem 1.25rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.75rem;
  background: var(--color-surface-900);
}

.parts-editor__empty p {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
}

.parts-editor__hint {
  color: var(--color-ink-faint) !important;
  font-size: 0.85rem !important;
}

.parts-editor__footer {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.45rem;
  padding: 0.35rem 0 0 2.45rem;
}

.parts-editor__slash {
  width: 100%;
}

.parts-editor__add {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.4rem 0.55rem;
  border: 0;
  border-radius: 0.45rem;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
}

.parts-editor__add:hover:not(:disabled) {
  background: var(--color-surface-900);
  color: var(--color-ink);
}

.parts-editor__add:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
