<script setup>
import { onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { MessageCircleQuestion } from 'lucide-vue-next';
import {
  askQuestion,
  fetchLessonQuestions,
  fetchQuestionThread,
  postQuestionMessage,
} from '@/services/questionService';
import { useQuestionStore } from '@/stores/questionStore';
import QuestionStatusBadge from '@/components/questions/QuestionStatusBadge.vue';
import QuestionThread from '@/components/questions/QuestionThread.vue';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';

const props = defineProps({
  lessonId: { type: Number, required: true },
});

const { t } = useI18n();
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
      <h3 class="lesson-questions__title">
        <MessageCircleQuestion :size="18" />
        {{ $t('questions.lesson_panel_title') }}
      </h3>
      <Button
        v-if="!composerOpen"
        size="sm"
        variant="outline"
        @click="composerOpen = true"
      >
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
      <label
        class="lesson-questions__label"
        for="lesson-question-title"
      >{{ $t('questions.title_label') }}</label>
      <Input
        id="lesson-question-title"
        v-model="title"
        :placeholder="$t('questions.title_placeholder')"
      />

      <label
        class="lesson-questions__label"
        for="lesson-question-body"
      >{{ $t('questions.body_label') }}</label>
      <Textarea
        id="lesson-question-body"
        v-model="body"
        :rows="4"
        :placeholder="$t('questions.body_placeholder')"
      />

      <div class="lesson-questions__actions">
        <Button
          type="submit"
          :disabled="!title.trim() || !body.trim() || submitting"
        >
          {{ submitting ? $t('questions.sending') : $t('questions.ask_action') }}
        </Button>
        <Button
          type="button"
          variant="ghost"
          @click="composerOpen = false"
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
    <p
      v-else-if="!questions.length && !composerOpen"
      class="lesson-questions__hint"
    >
      {{ $t('questions.empty_lesson') }}
    </p>

    <ul
      v-if="questions.length"
      class="lesson-questions__list"
    >
      <li
        v-for="question in questions"
        :key="question.id"
      >
        <button
          type="button"
          class="lesson-questions__item"
          :class="{ 'is-selected': thread?.id === question.id }"
          @click="openThread(question)"
        >
          <span class="lesson-questions__item-title">{{ question.title }}</span>
          <QuestionStatusBadge :status="question.status" />
        </button>
      </li>
    </ul>

    <QuestionThread
      v-if="thread"
      :thread="thread"
      :submitting="replying"
      :error-message="threadError"
      @reply="reply"
    />
  </section>
</template>

<style scoped>
.lesson-questions {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-top: clamp(1.5rem, 4vw, 2.5rem);
  border-top: 1px solid var(--color-border-subtle);
  padding-top: clamp(1rem, 3vw, 1.5rem);
}

.lesson-questions__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.lesson-questions__title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-ink);
}

.lesson-questions__composer {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.lesson-questions__label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-ink);
}

.lesson-questions__actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.25rem;
}

.lesson-questions__list {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.lesson-questions__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  width: 100%;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-card);
  padding: 0.5rem 0.75rem;
  text-align: left;
  cursor: pointer;
}

.lesson-questions__item.is-selected {
  border-color: var(--color-accent-coral);
  background: var(--color-accent-soft);
}

.lesson-questions__item-title {
  font-weight: 600;
  color: var(--color-ink);
}

.lesson-questions__hint {
  font-size: 0.85rem;
  color: var(--color-ink-muted);
}

.lesson-questions__error {
  border: 1px solid var(--color-accent-coral);
  border-radius: var(--radius-card);
  background: var(--color-accent-soft);
  padding: 0.5rem 0.75rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-accent-coral);
}
</style>
