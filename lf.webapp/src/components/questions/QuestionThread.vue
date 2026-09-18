<script setup>
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Trash2 } from 'lucide-vue-next';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import QuestionStatusBadge from '@/components/questions/QuestionStatusBadge.vue';
import { formatQuestionTimestamp } from '@/lib/questions';

const props = defineProps({
  thread: { type: Object, required: true },
  // Admins get delete controls; everyone else just reads and replies.
  canDelete: { type: Boolean, default: false },
  canChangeStatus: { type: Boolean, default: true },
  submitting: { type: Boolean, default: false },
  errorMessage: { type: String, default: '' },
});

const emit = defineEmits(['reply', 'close', 'reopen', 'delete-message']);

const { t, locale } = useI18n();

const draft = ref('');

const isClosed = computed(() => props.thread.status === 'Closed');

function submitReply() {
  const body = draft.value.trim();
  if (!body || props.submitting) return;

  emit('reply', body, () => {
    draft.value = '';
  });
}

function authorLabel(message) {
  return t(`questions.author_role.${String(message.authorRole).toLowerCase()}`);
}

function timestamp(value) {
  return formatQuestionTimestamp(value, locale.value);
}
</script>

<template>
  <section
    class="question-thread"
    :aria-labelledby="`question-thread-${thread.id}`"
  >
    <header class="question-thread__header">
      <div>
        <h2
          :id="`question-thread-${thread.id}`"
          class="question-thread__title font-display"
        >
          {{ thread.title }}
        </h2>
        <p class="question-thread__meta">
          {{ thread.courseTitle }} &middot; {{ thread.lessonTitle }} &middot; {{ thread.studentName }}
        </p>
      </div>
      <QuestionStatusBadge :status="thread.status" />
    </header>

    <ol class="question-thread__messages">
      <li
        v-for="message in thread.messages"
        :key="message.id"
        class="question-thread__message"
        :class="{ 'is-staff': message.authorRole !== 'Student', 'is-deleted': message.isDeleted }"
      >
        <div class="question-thread__message-head">
          <span class="question-thread__author">{{ message.authorName }}</span>
          <span class="mono-label question-thread__role">{{ authorLabel(message) }}</span>
          <time
            class="question-thread__time"
            :datetime="message.createdAt"
          >{{ timestamp(message.createdAt) }}</time>

          <Button
            v-if="canDelete && !message.isDeleted"
            variant="ghost"
            size="sm"
            :aria-label="$t('questions.delete_message')"
            @click="emit('delete-message', message)"
          >
            <Trash2 class="size-4" />
          </Button>
        </div>

        <!-- Bodies are plain text end to end, so they are rendered as text, never as markup. -->
        <p
          v-if="message.isDeleted"
          class="question-thread__body question-thread__body--deleted"
        >
          {{ $t('questions.message_deleted') }}
        </p>
        <!-- v-text, not interpolation: the body renders with white-space: pre-wrap, so template
             indentation inside the tag would show up as literal leading whitespace. -->
        <p
          v-else
          class="question-thread__body"
          v-text="message.body"
        />
      </li>
    </ol>

    <p
      v-if="errorMessage"
      class="question-thread__error"
      role="alert"
    >
      {{ errorMessage }}
    </p>

    <form
      v-if="!isClosed"
      class="question-thread__composer"
      @submit.prevent="submitReply"
    >
      <label
        class="question-thread__label"
        :for="`question-reply-${thread.id}`"
      >{{ $t('questions.reply_label') }}</label>
      <Textarea
        :id="`question-reply-${thread.id}`"
        v-model="draft"
        :rows="4"
        :placeholder="$t('questions.reply_placeholder')"
      />
      <div class="question-thread__actions">
        <Button
          type="submit"
          :disabled="!draft.trim() || submitting"
        >
          {{ submitting ? $t('questions.sending') : $t('questions.send') }}
        </Button>
        <Button
          v-if="canChangeStatus"
          type="button"
          variant="outline"
          @click="emit('close')"
        >
          {{ $t('questions.close_thread') }}
        </Button>
      </div>
    </form>

    <div
      v-else
      class="question-thread__actions"
    >
      <p class="question-thread__closed-hint">
        {{ $t('questions.closed_hint') }}
      </p>
      <Button
        v-if="canChangeStatus"
        type="button"
        variant="outline"
        @click="emit('reopen')"
      >
        {{ $t('questions.reopen_thread') }}
      </Button>
    </div>
  </section>
</template>

<style scoped>
.question-thread {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-card);
  padding: clamp(1rem, 3vw, 1.5rem);
}

.question-thread__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.question-thread__title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--color-ink);
}

.question-thread__meta {
  margin-top: 0.25rem;
  font-size: 0.85rem;
  color: var(--color-ink-muted);
}

.question-thread__messages {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.question-thread__message {
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  padding: 0.75rem 1rem;
  background: var(--color-surface-950);
}

/* Staff replies are tinted so a long thread reads as a conversation at a glance. */
.question-thread__message.is-staff {
  border-color: color-mix(in srgb, var(--color-accent-coral) 35%, transparent);
  background: var(--color-accent-soft);
}

.question-thread__message-head {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.question-thread__author {
  font-weight: 600;
  color: var(--color-ink);
}

.question-thread__role,
.question-thread__time {
  font-size: 0.75rem;
  color: var(--color-ink-muted);
}

.question-thread__time {
  margin-left: auto;
}

.question-thread__body {
  margin-top: 0.5rem;
  color: var(--color-ink);
  /* Bodies are plain text; newlines the author typed are preserved. */
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.question-thread__body--deleted {
  color: var(--color-ink-muted);
  font-style: italic;
}

.question-thread__composer {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.question-thread__label {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-ink);
}

.question-thread__actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.question-thread__closed-hint {
  font-size: 0.85rem;
  color: var(--color-ink-muted);
}

.question-thread__error {
  border: 1px solid var(--color-accent-coral);
  border-radius: var(--radius-card);
  background: var(--color-accent-soft);
  padding: 0.5rem 0.75rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-accent-coral);
}
</style>
