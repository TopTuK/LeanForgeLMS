<script setup>
import { computed, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { submitQuizAttempt } from '@/services/enrollmentService';

const props = defineProps({
  part: { type: Object, required: true },
  enrollmentId: { type: Number, required: true },
  lessonId: { type: Number, required: true },
});

const emit = defineEmits(['submitted']);

const { t } = useI18n();

const selected = reactive({});
const submitting = ref(false);
const error = ref('');
const result = ref(null);

function isSelected(questionId, optionId) {
  return (selected[questionId] ?? []).includes(optionId);
}

function toggleOption(question, optionId) {
  if (result.value) return;
  const current = selected[question.id] ?? [];
  if (question.questionType === 'MultipleChoice') {
    selected[question.id] = current.includes(optionId) ? current.filter((id) => id !== optionId) : [...current, optionId];
  } else {
    selected[question.id] = [optionId];
  }
}

const canSubmit = computed(() => props.part.quizQuestions.every((q) => (selected[q.id] ?? []).length > 0));

function resultFor(questionId) {
  return result.value?.questions.find((q) => q.questionId === questionId) ?? null;
}

function isCorrectOption(questionId, optionId) {
  return resultFor(questionId)?.correctOptionIds.includes(optionId) ?? false;
}

async function submit() {
  if (!canSubmit.value || submitting.value) return;

  submitting.value = true;
  error.value = '';
  try {
    const answers = props.part.quizQuestions.map((q) => ({ questionId: q.id, selectedOptionIds: selected[q.id] ?? [] }));
    const submission = await submitQuizAttempt(props.enrollmentId, props.lessonId, props.part.id, answers);
    result.value = submission.result;
    emit('submitted', submission.enrollment);
  } catch {
    error.value = t('courses.learn.quiz.submit_error');
  } finally {
    submitting.value = false;
  }
}

function retry() {
  result.value = null;
  for (const key of Object.keys(selected)) delete selected[key];
}
</script>

<template>
  <div class="quiz-take">
    <template v-if="!result">
      <div
        v-for="(question, qIndex) in part.quizQuestions"
        :key="question.id"
        class="quiz-take__question"
      >
        <p class="quiz-take__question-text">
          {{ qIndex + 1 }}. {{ question.text }}
        </p>
        <label
          v-for="option in question.options"
          :key="option.id"
          class="quiz-take__option"
          :class="{ 'quiz-take__option--selected': isSelected(question.id, option.id) }"
        >
          <input
            :type="question.questionType === 'MultipleChoice' ? 'checkbox' : 'radio'"
            :name="`question-${question.id}`"
            :checked="isSelected(question.id, option.id)"
            @change="toggleOption(question, option.id)"
          >
          <span>{{ option.text }}</span>
        </label>
      </div>

      <p
        v-if="error"
        class="quiz-take__error"
        role="alert"
      >
        {{ error }}
      </p>

      <button
        type="button"
        class="quiz-take__submit"
        :disabled="!canSubmit || submitting"
        @click="submit"
      >
        {{ submitting ? t('courses.learn.quiz.submitting') : t('courses.learn.quiz.submit') }}
      </button>
    </template>

    <template v-else>
      <div
        class="quiz-take__banner"
        :class="result.passed ? 'quiz-take__banner--passed' : 'quiz-take__banner--failed'"
      >
        <p class="quiz-take__score">
          {{ t('courses.learn.quiz.score_label', { percent: result.scorePercent }) }}
        </p>
        <p>{{ result.passed ? t('courses.learn.quiz.passed') : t('courses.learn.quiz.failed') }}</p>
      </div>

      <div
        v-for="(question, qIndex) in part.quizQuestions"
        :key="question.id"
        class="quiz-take__question"
      >
        <p class="quiz-take__question-text">
          {{ qIndex + 1 }}. {{ question.text }}
          <span
            class="quiz-take__mark"
            :class="resultFor(question.id)?.isCorrect ? 'quiz-take__mark--correct' : 'quiz-take__mark--incorrect'"
          >
            {{ resultFor(question.id)?.isCorrect ? t('courses.learn.quiz.correct') : t('courses.learn.quiz.incorrect') }}
          </span>
        </p>
        <div
          v-for="option in question.options"
          :key="option.id"
          class="quiz-take__option quiz-take__option--result"
          :class="{
            'quiz-take__option--was-selected': isSelected(question.id, option.id),
            'quiz-take__option--correct': isCorrectOption(question.id, option.id),
          }"
        >
          {{ option.text }}
        </div>
      </div>

      <button
        type="button"
        class="quiz-take__retry"
        @click="retry"
      >
        {{ t('courses.learn.quiz.retry') }}
      </button>
    </template>
  </div>
</template>

<style scoped>
.quiz-take {
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
  padding: 1.25rem;
  border: 1px solid var(--industrial-line);
  border-radius: 0.35rem;
  background: var(--color-surface-900);
}

.quiz-take__question {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
}

.quiz-take__question-text {
  margin: 0;
  color: var(--color-ink);
  font-weight: 700;
}

.quiz-take__option {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.55rem 0.75rem;
  border: 1px solid var(--industrial-line);
  border-radius: 0.3rem;
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
  font-size: 0.92rem;
  cursor: pointer;
  transition: border-color 0.15s ease, color 0.15s ease;
}

.quiz-take__option input {
  accent-color: var(--color-accent-coral);
}

.quiz-take__option--selected {
  border-color: var(--color-accent-coral);
  color: var(--color-ink);
}

.quiz-take__option--result {
  cursor: default;
}

.quiz-take__option--was-selected {
  border-color: var(--color-ink-faint);
  color: var(--color-ink);
}

.quiz-take__option--correct {
  border-color: var(--color-accent-coral);
  background: var(--industrial-accent-wash);
  color: var(--color-ink);
  font-weight: 700;
}

.quiz-take__error {
  margin: 0;
  color: var(--color-accent-coral-dark);
  font-size: 0.85rem;
  font-weight: 600;
}

.quiz-take__submit,
.quiz-take__retry {
  align-self: flex-start;
  padding: 0.65rem 1.3rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 999px;
  background: var(--color-accent-coral);
  color: #ffffff;
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.quiz-take__submit:hover:not(:disabled),
.quiz-take__retry:hover {
  transform: translateY(-1px);
  box-shadow: 0 8px 20px rgba(236, 104, 60, 0.22);
}

.quiz-take__submit:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.quiz-take__banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.9rem 1.1rem;
  border-radius: 0.3rem;
  border: 1px solid var(--industrial-line-strong);
}

.quiz-take__banner p {
  margin: 0;
}

.quiz-take__banner--passed {
  background: color-mix(in srgb, var(--color-accent-coral) 14%, var(--color-surface-950));
  border-color: var(--color-accent-coral);
  color: var(--color-ink);
}

.quiz-take__banner--failed {
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
}

.quiz-take__score {
  font-weight: 800;
  font-size: 1.05rem;
}

.quiz-take__mark {
  margin-left: 0.5rem;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.03em;
  text-transform: uppercase;
}

.quiz-take__mark--correct {
  color: var(--color-accent-coral-dark);
}

.quiz-take__mark--incorrect {
  color: var(--color-ink-faint);
}
</style>
