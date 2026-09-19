<script setup>
import { computed, nextTick, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ArrowLeft, ArrowUpRight, CheckCircle2, RotateCcw, SendHorizontal } from 'lucide-vue-next';
import { formatQuestionTimestamp, formatRelativeTime, initialsOf } from '@/lib/questions';

const props = defineProps({
  thread: { type: Object, required: true },
  submitting: { type: Boolean, default: false },
  errorMessage: { type: String, default: '' },
  showBack: { type: Boolean, default: false },
});

const emit = defineEmits(['reply', 'close', 'reopen', 'back']);

const { t, locale } = useI18n();

const draft = ref('');
const messagesEl = ref(null);
const composerEl = ref(null);

const isClosed = computed(() => props.thread.status === 'Closed');
const isOwnThread = computed(() => props.thread.askedByViewer);

// Staff cannot open another student's enrollment, so the lesson link only exists in your own threads.
const lessonLink = computed(() => (isOwnThread.value && props.thread.studentEnrollmentId
  ? {
    name: 'CourseLearn',
    params: { enrollmentId: props.thread.studentEnrollmentId },
    query: { lesson: props.thread.lessonId },
  }
  : null));

const statusKey = computed(() => `questions.inbox.status.${String(props.thread.status).toLowerCase()}`);

function isOwn(message) {
  return message.isMine;
}

function authorLabel(message) {
  if (isOwn(message)) return t('questions.inbox.you');
  return message.authorName;
}

function roleLabel(message) {
  return t(`questions.author_role.${String(message.authorRole).toLowerCase()}`);
}

function autoGrow() {
  const el = composerEl.value;
  if (!el) return;
  el.style.height = 'auto';
  el.style.height = `${Math.min(el.scrollHeight, 240)}px`;
}

function submit() {
  const body = draft.value.trim();
  if (!body || props.submitting) return;

  emit('reply', body, () => {
    draft.value = '';
    nextTick(autoGrow);
  });
}

function onKeydown(event) {
  if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
    event.preventDefault();
    submit();
  }
}

// Newest message sits at the bottom, so every new thread or reply scrolls it into view.
watch(
  () => [props.thread.id, props.thread.messages.length],
  async () => {
    await nextTick();
    if (messagesEl.value) messagesEl.value.scrollTop = messagesEl.value.scrollHeight;
  },
  { immediate: true },
);

watch(() => props.thread.id, () => {
  draft.value = '';
});
</script>

<template>
  <section
    class="conversation"
    :aria-labelledby="`conversation-title-${thread.id}`"
  >
    <header class="conversation__header">
      <button
        v-if="showBack"
        type="button"
        class="conversation__icon-button"
        :aria-label="$t('questions.inbox.back')"
        @click="emit('back')"
      >
        <ArrowLeft :size="18" />
      </button>

      <div class="conversation__heading">
        <p class="conversation__context">
          {{ thread.courseTitle }} <span aria-hidden="true">›</span> {{ thread.lessonTitle }}
        </p>
        <h2
          :id="`conversation-title-${thread.id}`"
          class="conversation__title"
        >
          {{ thread.title }}
        </h2>
        <div class="conversation__meta">
          <span
            class="conversation__status"
            :class="`is-${String(thread.status).toLowerCase()}`"
          >{{ $t(statusKey) }}</span>
          <span
            v-if="!isOwnThread"
            class="conversation__student"
          >{{ thread.studentName }}</span>
        </div>
      </div>

      <div class="conversation__actions">
        <RouterLink
          v-if="lessonLink"
          :to="lessonLink"
          class="conversation__link"
        >
          {{ $t('questions.inbox.open_lesson') }}
          <ArrowUpRight :size="15" />
        </RouterLink>
        <button
          v-if="!isClosed"
          type="button"
          class="conversation__ghost"
          @click="emit('close')"
        >
          <CheckCircle2 :size="15" />
          {{ $t('questions.close_thread') }}
        </button>
      </div>
    </header>

    <ol
      ref="messagesEl"
      class="conversation__messages"
    >
      <li
        v-for="message in thread.messages"
        :key="message.id"
        class="message"
        :class="{ 'is-own': isOwn(message), 'is-staff': message.authorRole !== 'Student' }"
      >
        <span
          v-if="!isOwn(message)"
          class="message__avatar"
          aria-hidden="true"
        >{{ initialsOf(message.authorName) }}</span>

        <div class="message__column">
          <p class="message__author">
            <span class="message__name">{{ authorLabel(message) }}</span>
            <span
              v-if="!isOwn(message)"
              class="message__role"
            >{{ roleLabel(message) }}</span>
          </p>

          <!-- Bodies are plain text end to end; v-text keeps template whitespace out of pre-wrap. -->
          <p
            v-if="message.isDeleted"
            class="message__bubble is-deleted"
          >
            {{ $t('questions.message_deleted') }}
          </p>
          <p
            v-else
            class="message__bubble"
            v-text="message.body"
          />

          <time
            class="message__time"
            :datetime="message.createdAt"
            :title="formatQuestionTimestamp(message.createdAt, locale)"
          >{{ formatRelativeTime(message.createdAt, locale) }}</time>
        </div>
      </li>
    </ol>

    <footer class="conversation__footer">
      <p
        v-if="errorMessage"
        class="conversation__error"
        role="alert"
      >
        {{ errorMessage }}
      </p>

      <div
        v-if="isClosed"
        class="conversation__closed"
      >
        <span>{{ $t('questions.inbox.resolved_hint') }}</span>
        <button
          type="button"
          class="conversation__ghost"
          @click="emit('reopen')"
        >
          <RotateCcw :size="15" />
          {{ $t('questions.reopen_thread') }}
        </button>
      </div>

      <form
        v-else
        class="composer"
        @submit.prevent="submit"
      >
        <label
          class="sr-only"
          :for="`conversation-reply-${thread.id}`"
        >{{ $t('questions.reply_label') }}</label>
        <textarea
          :id="`conversation-reply-${thread.id}`"
          ref="composerEl"
          v-model="draft"
          class="composer__input"
          rows="1"
          :placeholder="$t('questions.reply_placeholder')"
          @input="autoGrow"
          @keydown="onKeydown"
        />
        <button
          type="submit"
          class="composer__send"
          :disabled="!draft.trim() || submitting"
          :aria-label="submitting ? $t('questions.sending') : $t('questions.send')"
        >
          <SendHorizontal :size="18" />
        </button>
      </form>
      <p
        v-if="!isClosed"
        class="composer__hint"
      >
        {{ $t('questions.inbox.send_hint') }}
      </p>
    </footer>
  </section>
</template>

<style scoped>
.conversation {
  display: flex;
  height: 100%;
  min-height: 0;
  flex-direction: column;
}

.conversation__header {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  border-bottom: 1px solid var(--color-border-subtle);
  padding: 1.25rem 1.5rem 1rem;
}

.conversation__heading {
  flex: 1;
  min-width: 0;
}

.conversation__context {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 0.8rem;
  color: var(--color-ink-muted);
}

.conversation__title {
  margin-top: 0.2rem;
  font-family: var(--font-display);
  font-size: 1.15rem;
  font-weight: 600;
  line-height: 1.3;
  letter-spacing: -0.01em;
  color: var(--color-ink);
}

.conversation__meta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-top: 0.5rem;
  font-size: 0.8rem;
  color: var(--color-ink-muted);
}

.conversation__status {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-weight: 600;
}

.conversation__status::before {
  content: '';
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 999px;
  background: var(--color-ink-faint);
}

.conversation__status.is-open::before {
  background: var(--color-cover-amber);
}

.conversation__status.is-answered::before {
  background: var(--color-cover-forest);
}

.conversation__actions {
  display: flex;
  flex: none;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: 0.25rem;
}

.conversation__link,
.conversation__ghost,
.conversation__icon-button {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  border: 0;
  border-radius: var(--radius-pill);
  background: transparent;
  padding: 0.4rem 0.75rem;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-ink-muted);
  cursor: pointer;
  text-decoration: none;
  transition: background-color 0.15s ease, color 0.15s ease;
}

.conversation__icon-button {
  margin-left: -0.5rem;
  padding: 0.45rem;
}

.conversation__link {
  color: var(--color-accent-coral);
}

.conversation__link:hover,
.conversation__ghost:hover,
.conversation__icon-button:hover {
  background: var(--qa-fill, color-mix(in srgb, var(--color-ink) 6%, transparent));
  color: var(--color-ink);
}

.conversation__messages {
  display: flex;
  flex: 1;
  min-height: 0;
  flex-direction: column;
  gap: 1.25rem;
  margin: 0;
  overflow-y: auto;
  padding: 1.5rem;
  list-style: none;
}

.message {
  display: flex;
  align-items: flex-start;
  gap: 0.65rem;
  max-width: min(36rem, 88%);
}

.message.is-own {
  flex-direction: row-reverse;
  align-self: flex-end;
}

.message__avatar {
  display: grid;
  flex: none;
  width: 2rem;
  height: 2rem;
  place-items: center;
  border-radius: 999px;
  background: var(--qa-fill, color-mix(in srgb, var(--color-ink) 6%, transparent));
  font-size: 0.7rem;
  font-weight: 700;
  color: var(--color-ink-muted);
}

.message.is-staff .message__avatar {
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
}

.message__column {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 0.3rem;
}

.message.is-own .message__column {
  align-items: flex-end;
}

.message__author {
  display: flex;
  align-items: baseline;
  gap: 0.4rem;
  font-size: 0.78rem;
}

.message__name {
  font-weight: 600;
  color: var(--color-ink);
}

.message__role {
  color: var(--color-ink-faint);
}

.message__bubble {
  border-radius: 1rem;
  border-top-left-radius: 0.3rem;
  background: var(--qa-fill, color-mix(in srgb, var(--color-ink) 6%, transparent));
  padding: 0.65rem 0.9rem;
  line-height: 1.5;
  color: var(--color-ink);
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.message.is-own .message__bubble {
  border-top-left-radius: 1rem;
  border-top-right-radius: 0.3rem;
  background: var(--color-accent-coral);
  color: #fff;
}

.message__bubble.is-deleted,
.message.is-own .message__bubble.is-deleted {
  background: transparent;
  border: 1px dashed var(--color-border-subtle);
  font-style: italic;
  color: var(--color-ink-muted);
}

.message__time {
  font-size: 0.72rem;
  color: var(--color-ink-faint);
}

.conversation__footer {
  border-top: 1px solid var(--color-border-subtle);
  padding: 0.9rem 1.5rem 1.1rem;
}

.conversation__error {
  margin-bottom: 0.6rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-accent-coral);
}

.conversation__closed {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  font-size: 0.85rem;
  color: var(--color-ink-muted);
}

.composer {
  display: flex;
  align-items: flex-end;
  gap: 0.5rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 1.25rem;
  background: var(--color-card);
  padding: 0.4rem 0.4rem 0.4rem 1rem;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.composer:focus-within {
  border-color: var(--color-accent-coral);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-accent-coral) 15%, transparent);
}

.composer__input {
  flex: 1;
  min-height: 2.25rem;
  max-height: 15rem;
  resize: none;
  border: 0;
  background: transparent;
  padding: 0.5rem 0;
  font: inherit;
  line-height: 1.45;
  color: var(--color-ink);
  outline: none;
}

.composer__input::placeholder {
  color: var(--color-ink-faint);
}

.composer__send {
  display: grid;
  flex: none;
  width: 2.25rem;
  height: 2.25rem;
  place-items: center;
  border: 0;
  border-radius: 999px;
  background: var(--color-accent-coral);
  color: #fff;
  cursor: pointer;
  transition: opacity 0.15s ease, transform 0.15s ease;
}

.composer__send:hover:not(:disabled) {
  transform: translateY(-1px);
}

.composer__send:disabled {
  cursor: not-allowed;
  opacity: 0.35;
}

.composer__hint {
  margin-top: 0.4rem;
  padding-left: 1rem;
  font-size: 0.72rem;
  color: var(--color-ink-faint);
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

@media (max-width: 40rem) {
  .conversation__header,
  .conversation__messages,
  .conversation__footer {
    padding-left: 1rem;
    padding-right: 1rem;
  }

  .conversation__header {
    flex-wrap: wrap;
  }

  .conversation__actions {
    width: 100%;
    justify-content: flex-start;
  }
}
</style>
