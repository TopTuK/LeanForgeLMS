<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { formatQuestionTimestamp, formatRelativeTime } from '@/lib/questions';

const props = defineProps({
  question: { type: Object, required: true },
  selected: { type: Boolean, default: false },
});

const emit = defineEmits(['select']);

const { t, locale } = useI18n();

const previewAuthor = computed(() => {
  const role = props.question.lastMessageAuthorRole;
  // In your own threads "You:" reads better than your own name.
  if (role === 'Student') return props.question.askedByViewer ? t('questions.inbox.you') : props.question.studentName;
  return t(`questions.author_role.${String(role).toLowerCase()}`);
});

const preview = computed(() => props.question.lastMessagePreview ?? t('questions.message_deleted'));
</script>

<template>
  <li>
    <button
      type="button"
      class="inbox-item"
      :class="[`is-${String(question.status).toLowerCase()}`, { 'is-selected': selected, 'is-unread': question.hasUnread }]"
      :aria-current="selected ? 'true' : undefined"
      @click="emit('select', question)"
    >
      <span
        class="inbox-item__marker"
        aria-hidden="true"
      />

      <span class="inbox-item__body">
        <span class="inbox-item__top">
          <span class="inbox-item__context">{{ question.courseTitle }} · {{ question.lessonTitle }}</span>
          <time
            class="inbox-item__time"
            :datetime="question.lastMessageAt"
            :title="formatQuestionTimestamp(question.lastMessageAt, locale)"
          >{{ formatRelativeTime(question.lastMessageAt, locale) }}</time>
        </span>

        <span class="inbox-item__title">{{ question.title }}</span>

        <span class="inbox-item__preview">
          <span class="inbox-item__preview-author">{{ previewAuthor }}:</span>
          {{ preview }}
        </span>

        <span class="inbox-item__foot">
          <span class="inbox-item__status">{{ $t(`questions.inbox.status.${String(question.status).toLowerCase()}`) }}</span>
          <span
            v-if="question.hasUnread"
            class="inbox-item__unread"
          >{{ $t('questions.inbox.new_reply') }}</span>
        </span>
      </span>
    </button>
  </li>
</template>

<style scoped>
.inbox-item {
  position: relative;
  display: flex;
  gap: 0.75rem;
  width: 100%;
  border: 0;
  border-radius: calc(var(--radius-card) - 0.15rem);
  background: transparent;
  padding: 0.85rem 0.9rem;
  text-align: left;
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.inbox-item:hover {
  background: var(--qa-fill, color-mix(in srgb, var(--color-ink) 6%, transparent));
}

.inbox-item:focus-visible {
  outline: 2px solid var(--color-accent-coral);
  outline-offset: -2px;
}

.inbox-item.is-selected {
  background: var(--color-accent-soft);
}

/* The status marker: a quiet coloured rail rather than a badge, so a long list stays calm. */
.inbox-item__marker {
  flex: none;
  width: 3px;
  border-radius: 999px;
  background: var(--color-border-subtle);
}

.inbox-item.is-open .inbox-item__marker {
  background: var(--color-cover-amber);
}

.inbox-item.is-answered .inbox-item__marker {
  background: var(--color-cover-forest);
}

.inbox-item__body {
  display: flex;
  flex: 1;
  min-width: 0;
  flex-direction: column;
  gap: 0.2rem;
}

.inbox-item__top,
.inbox-item__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.inbox-item__context {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 0.75rem;
  color: var(--color-ink-muted);
}

.inbox-item__time {
  flex: none;
  font-size: 0.75rem;
  color: var(--color-ink-faint);
}

.inbox-item__title {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-weight: 500;
  color: var(--color-ink);
}

.inbox-item.is-unread .inbox-item__title {
  font-weight: 700;
}

.inbox-item__preview {
  display: -webkit-box;
  overflow: hidden;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  font-size: 0.85rem;
  line-height: 1.4;
  color: var(--color-ink-muted);
}

.inbox-item__preview-author {
  color: var(--color-ink);
}

.inbox-item__foot {
  margin-top: 0.15rem;
  justify-content: flex-start;
}

.inbox-item__status {
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: var(--color-ink-faint);
}

.inbox-item.is-open .inbox-item__status {
  color: var(--color-cover-amber);
}

.inbox-item.is-answered .inbox-item__status {
  color: var(--color-cover-forest);
}

.inbox-item__unread {
  border-radius: var(--radius-pill);
  background: var(--color-accent-coral);
  padding: 0.05rem 0.5rem;
  font-size: 0.7rem;
  font-weight: 600;
  color: #fff;
}
</style>
