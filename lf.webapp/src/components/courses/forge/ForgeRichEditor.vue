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
      class: 'forge-rich-editor__prose',
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
    mark: 'bold',
    action: (chain) => chain.toggleBold(),
  },
  {
    id: 'italic',
    labelKey: 'courses.lessonEditor.toolbar.italic',
    mark: 'italic',
    action: (chain) => chain.toggleItalic(),
  },
  {
    id: 'underline',
    labelKey: 'courses.lessonEditor.toolbar.underline',
    mark: 'underline',
    action: (chain) => chain.toggleUnderline(),
  },
  {
    id: 'strike',
    labelKey: 'courses.lessonEditor.toolbar.strike',
    mark: 'strike',
    action: (chain) => chain.toggleStrike(),
  },
];
</script>

<template>
  <div
    class="forge-rich-editor"
    :class="{ 'forge-rich-editor--disabled': disabled }"
  >
    <div
      class="forge-rich-editor__toolbar"
      role="toolbar"
      :aria-label="$t('courses.lessonEditor.toolbar.label')"
    >
      <div class="forge-rich-editor__group">
        <va-button
          v-for="tool in tools"
          :key="tool.id"
          size="small"
          preset="secondary"
          border-color="transparent"
          :color="isActive(tool.mark) ? 'primary' : undefined"
          :disabled="disabled || !editor"
          :aria-label="$t(tool.labelKey)"
          :title="$t(tool.labelKey)"
          @click="run(tool.action)"
        >
          {{ $t(tool.labelKey) }}
        </va-button>
      </div>

      <span
        class="forge-rich-editor__sep"
        aria-hidden="true"
      />

      <div class="forge-rich-editor__group">
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :color="isActive('heading', { level: 2 }) ? 'primary' : undefined"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.h2')"
          :title="$t('courses.lessonEditor.toolbar.h2')"
          @click="run((chain) => chain.toggleHeading({ level: 2 }))"
        >
          {{ $t('courses.lessonEditor.toolbar.h2') }}
        </va-button>
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :color="isActive('heading', { level: 3 }) ? 'primary' : undefined"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.h3')"
          :title="$t('courses.lessonEditor.toolbar.h3')"
          @click="run((chain) => chain.toggleHeading({ level: 3 }))"
        >
          {{ $t('courses.lessonEditor.toolbar.h3') }}
        </va-button>
      </div>

      <span
        class="forge-rich-editor__sep"
        aria-hidden="true"
      />

      <div class="forge-rich-editor__group">
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :color="isActive('bulletList') ? 'primary' : undefined"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.bullet_list')"
          :title="$t('courses.lessonEditor.toolbar.bullet_list')"
          @click="run((chain) => chain.toggleBulletList())"
        >
          {{ $t('courses.lessonEditor.toolbar.bullet_list') }}
        </va-button>
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :color="isActive('orderedList') ? 'primary' : undefined"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.ordered_list')"
          :title="$t('courses.lessonEditor.toolbar.ordered_list')"
          @click="run((chain) => chain.toggleOrderedList())"
        >
          {{ $t('courses.lessonEditor.toolbar.ordered_list') }}
        </va-button>
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :color="isActive('blockquote') ? 'primary' : undefined"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.blockquote')"
          :title="$t('courses.lessonEditor.toolbar.blockquote')"
          @click="run((chain) => chain.toggleBlockquote())"
        >
          {{ $t('courses.lessonEditor.toolbar.blockquote') }}
        </va-button>
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :color="isActive('code') ? 'primary' : undefined"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.code')"
          :title="$t('courses.lessonEditor.toolbar.code')"
          @click="run((chain) => chain.toggleCode())"
        >
          {{ $t('courses.lessonEditor.toolbar.code') }}
        </va-button>
      </div>

      <span
        class="forge-rich-editor__sep"
        aria-hidden="true"
      />

      <div class="forge-rich-editor__group">
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :color="isActive('link') ? 'primary' : undefined"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.link')"
          :title="$t('courses.lessonEditor.toolbar.link')"
          @click="setLink"
        >
          {{ $t('courses.lessonEditor.toolbar.link') }}
        </va-button>
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.image')"
          :title="$t('courses.lessonEditor.toolbar.image')"
          @click="setImage"
        >
          {{ $t('courses.lessonEditor.toolbar.image') }}
        </va-button>
      </div>

      <span
        class="forge-rich-editor__sep"
        aria-hidden="true"
      />

      <div class="forge-rich-editor__group">
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :disabled="disabled || !editor || !canUndo"
          :aria-label="$t('courses.lessonEditor.toolbar.undo')"
          :title="$t('courses.lessonEditor.toolbar.undo')"
          @click="run((chain) => chain.undo())"
        >
          {{ $t('courses.lessonEditor.toolbar.undo') }}
        </va-button>
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :disabled="disabled || !editor || !canRedo"
          :aria-label="$t('courses.lessonEditor.toolbar.redo')"
          :title="$t('courses.lessonEditor.toolbar.redo')"
          @click="run((chain) => chain.redo())"
        >
          {{ $t('courses.lessonEditor.toolbar.redo') }}
        </va-button>
        <va-button
          size="small"
          preset="secondary"
          border-color="transparent"
          :disabled="disabled || !editor"
          :aria-label="$t('courses.lessonEditor.toolbar.clear')"
          :title="$t('courses.lessonEditor.toolbar.clear')"
          @click="clearFormatting"
        >
          {{ $t('courses.lessonEditor.toolbar.clear') }}
        </va-button>
      </div>
    </div>

    <EditorContent
      class="forge-rich-editor__surface"
      :editor="editor"
    />
  </div>
</template>

<style scoped>
.forge-rich-editor {
  display: flex;
  flex-direction: column;
  min-height: 22rem;
  border: 1px solid var(--industrial-line-strong);
  border-radius: 0.35rem;
  background: var(--color-surface-950);
  overflow: hidden;
}

.forge-rich-editor--disabled {
  opacity: 0.72;
}

.forge-rich-editor__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem 0.5rem;
  padding: 0.55rem 0.65rem;
  border-bottom: 1px solid var(--industrial-line);
  background:
    linear-gradient(
      180deg,
      color-mix(in srgb, var(--industrial-panel) 88%, transparent) 0%,
      var(--color-surface-900) 100%
    );
}

.forge-rich-editor__group {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 0.25rem;
}

.forge-rich-editor__sep {
  width: 1px;
  height: 1.4rem;
  background: var(--industrial-line-strong);
  margin: 0 0.15rem;
}

.forge-rich-editor__surface {
  flex: 1;
  min-height: 18rem;
}

.forge-rich-editor__surface :deep(.tiptap),
.forge-rich-editor__surface :deep(.ProseMirror) {
  min-height: 18rem;
  padding: 1.15rem 1.25rem 1.5rem;
  color: var(--color-ink);
  font-size: 1rem;
  line-height: 1.65;
  outline: none;
}

.forge-rich-editor__surface :deep(.ProseMirror:focus) {
  box-shadow: inset 0 0 0 2px var(--industrial-accent-wash);
}

.forge-rich-editor__surface :deep(.ProseMirror p.is-editor-empty:first-child::before) {
  content: attr(data-placeholder);
  float: left;
  height: 0;
  color: var(--color-ink-faint);
  pointer-events: none;
}

.forge-rich-editor__surface :deep(.ProseMirror h2) {
  margin: 1.25rem 0 0.65rem;
  font-size: 1.45rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  color: var(--color-ink);
}

.forge-rich-editor__surface :deep(.ProseMirror h3) {
  margin: 1.1rem 0 0.5rem;
  font-size: 1.15rem;
  font-weight: 700;
  color: var(--color-ink);
}

.forge-rich-editor__surface :deep(.ProseMirror p) {
  margin: 0.55rem 0;
}

.forge-rich-editor__surface :deep(.ProseMirror ul),
.forge-rich-editor__surface :deep(.ProseMirror ol) {
  margin: 0.55rem 0;
  padding-left: 1.35rem;
}

.forge-rich-editor__surface :deep(.ProseMirror blockquote) {
  margin: 0.85rem 0;
  padding: 0.35rem 0 0.35rem 0.95rem;
  border-left: 3px solid var(--color-accent-coral);
  color: var(--color-ink-muted);
}

.forge-rich-editor__surface :deep(.ProseMirror code) {
  padding: 0.12rem 0.35rem;
  border-radius: 0.2rem;
  background: var(--color-surface-900);
  border: 1px solid var(--industrial-line);
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 0.88em;
}

.forge-rich-editor__surface :deep(.ProseMirror a) {
  color: var(--color-accent-coral-dark);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.forge-rich-editor__surface :deep(.ProseMirror img) {
  display: block;
  max-width: 100%;
  height: auto;
  margin: 0.85rem 0;
  border: 1px solid var(--industrial-line);
  border-radius: 0.25rem;
}

.forge-rich-editor__toolbar :deep(.va-button) {
  min-width: auto;
  text-transform: none;
  font-weight: 600;
  letter-spacing: 0.02em;
}
</style>
