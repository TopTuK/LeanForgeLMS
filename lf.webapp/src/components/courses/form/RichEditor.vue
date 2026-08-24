<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { useEditor, EditorContent } from '@tiptap/vue-3';
import StarterKit from '@tiptap/starter-kit';
import Link from '@tiptap/extension-link';
import Image from '@tiptap/extension-image';
import Placeholder from '@tiptap/extension-placeholder';
import Underline from '@tiptap/extension-underline';
import { useI18n } from 'vue-i18n';

const props = defineProps({
  modelValue: { type: String, default: '' },
  placeholder: { type: String, default: '' },
  disabled: { type: Boolean, default: false },
  compact: { type: Boolean, default: false },
  allowImage: { type: Boolean, default: true },
});

const emit = defineEmits(['update:modelValue']);

const { t } = useI18n();
const editorTick = ref(0);

const editor = useEditor({
  content: props.modelValue || '',
  editable: !props.disabled,
  extensions: [
    StarterKit.configure({
      heading: { levels: [2, 3] },
    }),
    Underline,
    Link.configure({
      openOnClick: false,
      HTMLAttributes: { rel: 'noopener noreferrer', target: '_blank' },
    }),
    Image.configure({ allowBase64: false }),
    Placeholder.configure({
      placeholder: props.placeholder || t('courses.lessonEditor.placeholder'),
    }),
  ],
  editorProps: {
    attributes: {
      class: 'rich-editor__prose',
    },
  },
  onUpdate: ({ editor: ed }) => {
    const html = ed.getHTML();
    emit('update:modelValue', html === '<p></p>' ? '' : html);
  },
  onSelectionUpdate: () => {
    editorTick.value += 1;
  },
  onTransaction: () => {
    editorTick.value += 1;
  },
});

watch(
  () => props.modelValue,
  (value) => {
    if (!editor.value) return;
    const current = editor.value.getHTML();
    const currentNormalized = current === '<p></p>' ? '' : current;
    const next = value || '';
    if (next !== currentNormalized) {
      editor.value.commands.setContent(next, { emitUpdate: false });
    }
  },
);

watch(
  () => props.disabled,
  (disabled) => {
    editor.value?.setEditable(!disabled);
  },
);

onBeforeUnmount(() => {
  editor.value?.destroy();
});

const canUndo = computed(() => {
  editorTick.value;
  return editor.value?.can().undo() ?? false;
});
const canRedo = computed(() => {
  editorTick.value;
  return editor.value?.can().redo() ?? false;
});

function isActive(name, attrs = {}) {
  editorTick.value;
  return editor.value?.isActive(name, attrs) ?? false;
}

function run(command) {
  if (!editor.value || props.disabled) return;
  command(editor.value.chain().focus()).run();
}

function setLink() {
  if (!editor.value || props.disabled) return;
  const previous = editor.value.getAttributes('link').href ?? '';
  const url = window.prompt(t('courses.lessonEditor.link_prompt'), previous);
  if (url === null) return;
  if (url.trim() === '') {
    editor.value.chain().focus().extendMarkRange('link').unsetLink().run();
    return;
  }
  editor.value
    .chain()
    .focus()
    .extendMarkRange('link')
    .setLink({ href: url.trim() })
    .run();
}

function setImage() {
  if (!editor.value || props.disabled) return;
  const url = window.prompt(t('courses.lessonEditor.image_prompt'));
  if (!url?.trim()) return;
  editor.value.chain().focus().setImage({ src: url.trim() }).run();
}

function clearFormatting() {
  if (!editor.value || props.disabled) return;
  editor.value.chain().focus().clearNodes().unsetAllMarks().run();
}

const tools = [
  {
    id: 'bold',
    labelKey: 'courses.lessonEditor.toolbar.bold',
    icon: 'format_bold',
    mark: 'bold',
    action: (chain) => chain.toggleBold(),
  },
  {
    id: 'italic',
    labelKey: 'courses.lessonEditor.toolbar.italic',
    icon: 'format_italic',
    mark: 'italic',
    action: (chain) => chain.toggleItalic(),
  },
  {
    id: 'underline',
    labelKey: 'courses.lessonEditor.toolbar.underline',
    icon: 'format_underlined',
    mark: 'underline',
    action: (chain) => chain.toggleUnderline(),
  },
  {
    id: 'strike',
    labelKey: 'courses.lessonEditor.toolbar.strike',
    icon: 'strikethrough_s',
    mark: 'strike',
    action: (chain) => chain.toggleStrike(),
  },
];
</script>

<template>
  <div
    class="rich-editor"
    :class="{
      'rich-editor--disabled': disabled,
      'rich-editor--compact': compact,
    }"
  >
    <div
      class="rich-editor__toolbar"
      role="toolbar"
      :aria-label="$t('courses.lessonEditor.toolbar.label')"
    >
      <div class="rich-editor__group">
        <button
          v-for="tool in tools"
          :key="tool.id"
          type="button"
          class="rich-editor__btn"
          :class="{ 'is-active': isActive(tool.mark) }"
          :disabled="disabled || !editor"
          :aria-label="$t(tool.labelKey)"
          :title="$t(tool.labelKey)"
          @click="run(tool.action)"
        >
          {{ tool.id.slice(0, 1).toUpperCase() }}
        </button>
      </div>

      <span
        class="rich-editor__sep"
        aria-hidden="true"
      />

      <div class="rich-editor__group">
        <button
          type="button"
          class="rich-editor__btn rich-editor__heading"
          :class="{ 'is-active': isActive('heading', { level: 2 }) }"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.h2')"
          :title="$t('courses.lessonEditor.toolbar.h2')"
          @click="run((chain) => chain.toggleHeading({ level: 2 }))"
        >
          {{ $t('courses.lessonEditor.toolbar.h2') }}
        </button>
        <button
          type="button"
          class="rich-editor__btn rich-editor__heading"
          :class="{ 'is-active': isActive('heading', { level: 3 }) }"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.h3')"
          :title="$t('courses.lessonEditor.toolbar.h3')"
          @click="run((chain) => chain.toggleHeading({ level: 3 }))"
        >
          {{ $t('courses.lessonEditor.toolbar.h3') }}
        </button>
      </div>

      <span
        class="rich-editor__sep"
        aria-hidden="true"
      />

      <div class="rich-editor__group">
        <button
          type="button"
          class="rich-editor__btn"
          :class="{ 'is-active': isActive('bulletList') }"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.bullet_list')"
          :title="$t('courses.lessonEditor.toolbar.bullet_list')"
          @click="run((chain) => chain.toggleBulletList())"
        >
          •
        </button>
        <button
          type="button"
          class="rich-editor__btn"
          :class="{ 'is-active': isActive('orderedList') }"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.ordered_list')"
          :title="$t('courses.lessonEditor.toolbar.ordered_list')"
          @click="run((chain) => chain.toggleOrderedList())"
        >
          1.
        </button>
        <button
          type="button"
          class="rich-editor__btn"
          :class="{ 'is-active': isActive('blockquote') }"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.blockquote')"
          :title="$t('courses.lessonEditor.toolbar.blockquote')"
          @click="run((chain) => chain.toggleBlockquote())"
        >
          “
        </button>
        <button
          type="button"
          class="rich-editor__btn"
          :class="{ 'is-active': isActive('code') }"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.code')"
          :title="$t('courses.lessonEditor.toolbar.code')"
          @click="run((chain) => chain.toggleCode())"
        >
          &lt;/&gt;
        </button>
      </div>

      <span
        class="rich-editor__sep"
        aria-hidden="true"
      />

      <div class="rich-editor__group">
        <button
          type="button"
          class="rich-editor__btn"
          :class="{ 'is-active': isActive('link') }"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.link')"
          :title="$t('courses.lessonEditor.toolbar.link')"
          @click="setLink"
        >
          ↗
        </button>
        <button
          v-if="allowImage"
          type="button"
          class="rich-editor__btn"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.image')"
          :title="$t('courses.lessonEditor.toolbar.image')"
          @click="setImage"
        >
          ▣
        </button>
      </div>

      <span
        class="rich-editor__sep"
        aria-hidden="true"
      />

      <div class="rich-editor__group">
        <button
          type="button"
          class="rich-editor__btn"
          :disabled="disabled || !editor || !canUndo"
          :aria-label="$t('courses.lessonEditor.toolbar.undo')"
          :title="$t('courses.lessonEditor.toolbar.undo')"
          @click="run((chain) => chain.undo())"
        >
          ↺
        </button>
        <button
          type="button"
          class="rich-editor__btn"
          :disabled="disabled || !editor || !canRedo"
          :aria-label="$t('courses.lessonEditor.toolbar.redo')"
          :title="$t('courses.lessonEditor.toolbar.redo')"
          @click="run((chain) => chain.redo())"
        >
          ↻
        </button>
        <button
          type="button"
          class="rich-editor__btn"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.clear')"
          :title="$t('courses.lessonEditor.toolbar.clear')"
          @click="clearFormatting"
        >
          ×
        </button>
      </div>
    </div>

    <EditorContent
      class="rich-editor__surface"
      :editor="editor"
    />
  </div>
</template>

<style scoped>
.rich-editor {
  display: flex;
  flex-direction: column;
  min-height: 22rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.65rem;
  background: var(--color-surface-950);
  overflow: hidden;
}

.rich-editor--disabled {
  opacity: 0.72;
}

.rich-editor--compact {
  min-height: 0;
  border: 0;
  border-radius: 0.55rem;
  background: transparent;
  overflow: visible;
}

.rich-editor--compact .rich-editor__toolbar {
  position: sticky;
  top: 3.5rem;
  z-index: 4;
  margin: 0 0 0.15rem;
  padding: 0.3rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.55rem;
  background: var(--color-surface-950);
  box-shadow: 0 8px 20px -16px rgb(15 23 42 / 0.35);
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.12s ease;
}

.rich-editor--compact:focus-within .rich-editor__toolbar {
  opacity: 1;
  pointer-events: auto;
}

.rich-editor--compact .rich-editor__surface,
.rich-editor--compact .rich-editor__surface :deep(.tiptap),
.rich-editor--compact .rich-editor__surface :deep(.ProseMirror) {
  min-height: 4.5rem;
}

.rich-editor--compact .rich-editor__surface :deep(.tiptap),
.rich-editor--compact .rich-editor__surface :deep(.ProseMirror) {
  padding: 0.55rem 0.65rem 0.85rem;
}

.rich-editor--compact .rich-editor__surface :deep(.ProseMirror:focus) {
  box-shadow: none;
}

.rich-editor__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem 0.5rem;
  padding: 0.55rem 0.65rem;
  border-bottom: 1px solid var(--color-border-subtle);
  background: var(--color-surface-900);
}

.rich-editor__group {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 0.25rem;
}

.rich-editor__sep {
  width: 1px;
  height: 1.4rem;
  background: var(--color-border-subtle);
  margin: 0 0.15rem;
}

.rich-editor__surface {
  flex: 1;
  min-height: 18rem;
}

.rich-editor__surface :deep(.tiptap),
.rich-editor__surface :deep(.ProseMirror) {
  min-height: 18rem;
  padding: 1.15rem 1.25rem 1.5rem;
  color: var(--color-ink);
  font-size: 1rem;
  line-height: 1.65;
  outline: none;
  overflow-wrap: anywhere;
}

.rich-editor__surface :deep(.ProseMirror:focus) {
  box-shadow: inset 0 0 0 2px color-mix(in srgb, var(--color-accent-coral) 16%, transparent);
}

.rich-editor__surface :deep(.ProseMirror p.is-editor-empty:first-child::before) {
  content: attr(data-placeholder);
  float: left;
  height: 0;
  color: var(--color-ink-faint);
  pointer-events: none;
}

.rich-editor__surface :deep(.ProseMirror h2) {
  margin: 1.25rem 0 0.65rem;
  font-size: 1.45rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  color: var(--color-ink);
}

.rich-editor__surface :deep(.ProseMirror h3) {
  margin: 1.1rem 0 0.5rem;
  font-size: 1.15rem;
  font-weight: 700;
  color: var(--color-ink);
}

.rich-editor__surface :deep(.ProseMirror p) {
  margin: 0.55rem 0;
}

.rich-editor__surface :deep(.ProseMirror ul),
.rich-editor__surface :deep(.ProseMirror ol) {
  margin: 0.55rem 0;
  padding-left: 1.35rem;
}

.rich-editor__surface :deep(.ProseMirror blockquote) {
  margin: 0.85rem 0;
  padding: 0.35rem 0 0.35rem 0.95rem;
  border-left: 3px solid var(--color-accent-coral);
  color: var(--color-ink-muted);
}

.rich-editor__surface :deep(.ProseMirror code) {
  padding: 0.12rem 0.35rem;
  border-radius: 0.2rem;
  background: var(--color-surface-900);
  border: 1px solid var(--color-border-subtle);
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 0.88em;
}

.rich-editor__surface :deep(.ProseMirror a) {
  color: var(--color-accent-coral-dark);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.rich-editor__surface :deep(.ProseMirror img) {
  display: block;
  max-width: 100%;
  height: auto;
  margin: 0.85rem 0;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.45rem;
}

.rich-editor__btn {
  min-width: 2rem;
  height: 2rem;
  padding: 0 0.4rem;
  border: 0;
  border-radius: 0.35rem;
  background: transparent;
  color: var(--color-ink-muted);
  font-family: inherit;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
}

.rich-editor__btn:hover:not(:disabled) {
  background: var(--color-surface-800);
  color: var(--color-ink);
}

.rich-editor__btn.is-active {
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
}

.rich-editor__btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.rich-editor__heading {
  min-width: 2.1rem;
}
</style>
