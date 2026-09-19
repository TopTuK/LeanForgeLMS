<script setup>
import { onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  ArrowRight,
  CircleHelp,
  MessageCircleQuestion,
  MessageSquareText,
  Plus,
  Send,
  X,
} from 'lucide-vue-next';
import {
  askQuestion,
  fetchLessonQuestions,
  fetchQuestionThread,
  postQuestionMessage,
} from '@/services/questionService';
import { useQuestionStore } from '@/stores/questionStore';
import QuestionStatusBadge from '@/components/questions/QuestionStatusBadge.vue';
import QuestionThread from '@/components/questions/QuestionThread.vue';
import { formatQuestionTimestamp } from '@/lib/questions';
import { track } from '@/lib/analytics';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';

const props = defineProps({
  lessonId: { type: Number, required: true },
});

const { t, locale } = useI18n();
const questionStore = useQuestionStore();

const questions = ref([]);
const loading = ref(true);
const errorMessage = ref('');

const composerOpen = ref(false);
const title = ref('');
const body = ref('');
const submitting = ref(false);

const thread = ref(null);
const threadError = ref('');
const replying = ref(false);

async function load() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchLessonQuestions(props.lessonId);
    questions.value = result.items;
  } catch {
    errorMessage.value = t('questions.load_error');
  } finally {
    loading.value = false;
  }
}

async function submitQuestion() {
  if (!title.value.trim() || !body.value.trim() || submitting.value) return;

  submitting.value = true;
  errorMessage.value = '';
  try {
    thread.value = await askQuestion({ lessonId: props.lessonId, title: title.value.trim(), body: body.value.trim() });
    track('question_ask', { lesson_id: props.lessonId });
    title.value = '';
    body.value = '';
    composerOpen.value = false;
    await load();
  } catch (err) {
    // 403 is the common case here: the student is previewing a course they aren't enrolled in.
    errorMessage.value = err?.response?.status === 403 ? t('questions.not_enrolled') : t('questions.send_error');
  } finally {
    submitting.value = false;
  }
}

async function openThread(question) {
  threadError.value = '';
  try {
    thread.value = await fetchQuestionThread(question.id);
    await Promise.all([questionStore.refreshUnreadCount(), load()]);
  } catch {
    threadError.value = t('questions.load_error');
  }
}

async function reply(text, onSuccess) {
  replying.value = true;
  threadError.value = '';
  try {
    thread.value = await postQuestionMessage(thread.value.id, text);
    onSuccess();
    await load();
  } catch {
    threadError.value = t('questions.send_error');
  } finally {
    replying.value = false;
  }
}

function closeComposer() {
  composerOpen.value = false;
}

function timestamp(value) {
  return formatQuestionTimestamp(value, locale.value);
}

// Moving to another lesson resets the panel rather than showing the previous lesson's threads.
watch(
  () => props.lessonId,
  () => {
    thread.value = null;
    composerOpen.value = false;
    load();
  },
);

onMounted(load);
</script>

<template>
  <section class="lesson-questions">
    <header class="lesson-questions__header">
      <div class="lesson-questions__heading">
        <span
          class="lesson-questions__icon"
          aria-hidden="true"
        >
          <MessageCircleQuestion :size="20" />
        </span>
        <div>
          <h3 class="lesson-questions__title">
            {{ $t('questions.lesson_panel_title') }}
          </h3>
          <p class="lesson-questions__subtitle">
            {{ $t('questions.lesson_panel_subtitle') }}
          </p>
        </div>
      </div>
      <Button
        v-if="!composerOpen"
        size="sm"
        class="lesson-questions__ask-button"
        @click="composerOpen = true"
      >
        <Plus :size="15" />
        {{ $t('questions.ask_action') }}
      </Button>
    </header>

    <p
      v-if="errorMessage"
      class="lesson-questions__error"
      role="alert"
    >
      {{ errorMessage }}
    </p>

    <form
      v-if="composerOpen"
      class="lesson-questions__composer"
      @submit.prevent="submitQuestion"
    >
      <div class="lesson-questions__composer-header">
        <div>
          <h4>{{ $t('questions.composer_title') }}</h4>
          <p>{{ $t('questions.composer_hint') }}</p>
        </div>
        <button
          type="button"
          class="lesson-questions__close"
          :aria-label="$t('questions.cancel')"
          @click="closeComposer"
        >
          <X :size="17" />
        </button>
      </div>

      <div class="lesson-questions__field">
        <label
          class="lesson-questions__label"
          for="lesson-question-title"
        >{{ $t('questions.title_label') }}</label>
        <Input
          id="lesson-question-title"
          v-model="title"
          class="lesson-questions__input"
          :placeholder="$t('questions.title_placeholder')"
        />
      </div>

      <div class="lesson-questions__field">
        <label
          class="lesson-questions__label"
          for="lesson-question-body"
        >{{ $t('questions.body_label') }}</label>
        <Textarea
          id="lesson-question-body"
          v-model="body"
          class="lesson-questions__textarea"
          :rows="5"
          :placeholder="$t('questions.body_placeholder')"
        />
      </div>

      <div class="lesson-questions__actions">
        <Button
          type="submit"
          :disabled="!title.trim() || !body.trim() || submitting"
        >
          <Send :size="15" />
          {{ submitting ? $t('questions.sending') : $t('questions.ask_action') }}
        </Button>
        <Button
          type="button"
          variant="ghost"
          @click="closeComposer"
        >
          {{ $t('questions.cancel') }}
        </Button>
      </div>
    </form>

    <p
      v-if="loading"
      class="lesson-questions__hint"
    >
      {{ $t('questions.loading') }}
    </p>
    <div
      v-else-if="!questions.length && !composerOpen"
      class="lesson-questions__empty"
    >
      <CircleHelp :size="22" />
      <div>
        <p class="lesson-questions__empty-title">
          {{ $t('questions.empty_lesson') }}
        </p>
        <p class="lesson-questions__empty-hint">
          {{ $t('questions.empty_lesson_hint') }}
        </p>
      </div>
    </div>

    <div v-if="questions.length">
      <p class="lesson-questions__list-label">
        {{ $t('questions.lesson_history') }}
      </p>
      <ul class="lesson-questions__list">
        <li
          v-for="question in questions"
          :key="question.id"
        >
          <button
            type="button"
            class="lesson-questions__item"
            :class="{ 'is-selected': thread?.id === question.id }"
            :aria-current="thread?.id === question.id ? 'true' : undefined"
            @click="openThread(question)"
          >
            <span class="lesson-questions__item-main">
              <span class="lesson-questions__item-title">{{ question.title }}</span>
              <span class="lesson-questions__item-meta">
                <MessageSquareText :size="13" />
                {{ question.messageCount }}
                <span aria-hidden="true">&middot;</span>
                <time :datetime="question.lastMessageAt">{{ timestamp(question.lastMessageAt) }}</time>
              </span>
            </span>
            <span class="lesson-questions__item-end">
              <QuestionStatusBadge :status="question.status" />
              <ArrowRight :size="15" />
            </span>
          </button>
        </li>
      </ul>
    </div>

    <div
      v-if="thread"
      class="lesson-questions__thread"
    >
      <p class="lesson-questions__list-label">
        {{ $t('questions.conversation') }}
      </p>
      <QuestionThread
        :thread="thread"
        :submitting="replying"
        :error-message="threadError"
        :can-change-status="false"
        @reply="reply"
      />
    </div>
  </section>
</template>

<style scoped>
.lesson-questions {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-top: clamp(2rem, 5vw, 3rem);
  border-top: 1px solid var(--color-border-subtle);
  padding-top: clamp(1.25rem, 3vw, 1.75rem);
}

.lesson-questions__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.lesson-questions__heading {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  min-width: 0;
}

.lesson-questions__icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.35rem;
  height: 2.35rem;
  flex: 0 0 auto;
  border: 1px solid color-mix(in srgb, var(--color-accent-coral) 28%, var(--color-border-subtle));
  border-radius: 0.65rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral-dark);
}

.lesson-questions__title {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--color-ink);
}

.lesson-questions__subtitle {
  max-width: 30rem;
  margin: 0.2rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  line-height: 1.45;
}

.lesson-questions__ask-button {
  flex: 0 0 auto;
}

.lesson-questions__composer {
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
  padding: clamp(1rem, 3vw, 1.35rem);
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.75rem;
  background: var(--color-card);
}

.lesson-questions__composer-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.lesson-questions__composer-header h4 {
  margin: 0;
  color: var(--color-ink);
  font-size: 0.95rem;
  font-weight: 700;
}

.lesson-questions__composer-header p {
  margin: 0.15rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  line-height: 1.45;
}

.lesson-questions__close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  flex: 0 0 auto;
  padding: 0;
  border: 0;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink-muted);
  cursor: pointer;
}

.lesson-questions__close:hover {
  background: var(--color-surface-900);
  color: var(--color-ink);
}

.lesson-questions__field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.lesson-questions__label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-ink);
}

.lesson-questions__input,
.lesson-questions__textarea {
  background: var(--color-surface-950);
}

.lesson-questions__textarea {
  resize: vertical;
}

.lesson-questions__actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.1rem;
}

.lesson-questions__list-label {
  margin: 0 0 0.5rem;
  color: var(--color-ink-muted);
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.lesson-questions__list {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.lesson-questions__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  width: 100%;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.6rem;
  background: transparent;
  padding: 0.7rem 0.8rem;
  text-align: left;
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease;
}

.lesson-questions__item:hover {
  border-color: color-mix(in srgb, var(--color-accent-coral) 42%, var(--color-border-subtle));
  background: var(--color-surface-900);
}

.lesson-questions__item.is-selected {
  border-color: var(--color-accent-coral);
  background: var(--color-accent-soft);
}

.lesson-questions__item-main {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
}

.lesson-questions__item-title {
  color: var(--color-ink);
  font-size: 0.88rem;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.lesson-questions__item-meta {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  color: var(--color-ink-muted);
  font-size: 0.72rem;
}

.lesson-questions__item-end {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 0 0 auto;
  color: var(--color-ink);
}

.lesson-questions__hint {
  margin: 0;
  font-size: 0.85rem;
  color: var(--color-ink-muted);
}

.lesson-questions__empty {
  display: flex;
  align-items: flex-start;
  gap: 0.7rem;
  padding: 0.85rem 1rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.65rem;
  color: var(--color-ink-muted);
}

.lesson-questions__empty svg {
  flex: 0 0 auto;
  margin-top: 0.05rem;
  color: var(--color-ink-faint);
}

.lesson-questions__empty-title {
  margin: 0;
  color: var(--color-ink);
  font-size: 0.85rem;
  font-weight: 600;
}

.lesson-questions__empty-hint {
  margin: 0.15rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  line-height: 1.4;
}

.lesson-questions__error {
  margin: 0;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.6rem;
  background: var(--color-accent-soft);
  padding: 0.5rem 0.75rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-accent-coral);
}

.lesson-questions__thread {
  margin-top: 0.25rem;
}

@media (max-width: 39rem) {
  .lesson-questions__header {
    display: grid;
  }

  .lesson-questions__ask-button {
    width: 100%;
  }

  .lesson-questions__item-end svg {
    display: none;
  }
}
</style>
