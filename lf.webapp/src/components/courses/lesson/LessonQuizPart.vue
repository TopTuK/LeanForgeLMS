<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import draggable from 'vuedraggable';
import { createBlankQuizOption, createBlankQuizQuestion, DEFAULT_QUIZ_PASS_THRESHOLD } from '@/stores/lessonPartStore';

const props = defineProps({
  modelValue: { type: Object, required: true },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['update:modelValue']);

const { t } = useI18n();

const questions = computed(() => props.modelValue.quizQuestions ?? []);
const passThreshold = computed(() => props.modelValue.quizPassThreshold ?? DEFAULT_QUIZ_PASS_THRESHOLD);

function emitUpdate(partial) {
  emit('update:modelValue', {
    quizQuestions: questions.value,
    quizPassThreshold: passThreshold.value,
    ...partial,
  });
}

function updateThreshold(event) {
  const value = Math.min(100, Math.max(1, Number(event.target.value) || DEFAULT_QUIZ_PASS_THRESHOLD));
  emitUpdate({ quizPassThreshold: value });
}

function setQuestions(newQuestions) {
  emitUpdate({ quizQuestions: newQuestions });
}

function updateQuestion(qIndex, patch) {
  setQuestions(questions.value.map((q, i) => (i === qIndex ? { ...q, ...patch } : q)));
}

function updateQuestionType(qIndex, questionType) {
  const question = questions.value[qIndex];
  const options = questionType === 'single'
    ? question.options.map((o, i) => ({ ...o, isCorrect: i === question.options.findIndex((x) => x.isCorrect) }))
    : question.options;
  updateQuestion(qIndex, { questionType, options });
}

function addQuestion() {
  setQuestions([...questions.value, createBlankQuizQuestion()]);
}

function removeQuestion(qIndex) {
  setQuestions(questions.value.filter((_, i) => i !== qIndex));
}

function setOptions(qIndex, newOptions) {
  updateQuestion(qIndex, { options: newOptions });
}

function updateOptionText(qIndex, oIndex, text) {
  const question = questions.value[qIndex];
  setOptions(qIndex, question.options.map((o, i) => (i === oIndex ? { ...o, text } : o)));
}

function toggleOptionCorrect(qIndex, oIndex) {
  const question = questions.value[qIndex];
  const options = question.questionType === 'single'
    ? question.options.map((o, i) => ({ ...o, isCorrect: i === oIndex }))
    : question.options.map((o, i) => (i === oIndex ? { ...o, isCorrect: !o.isCorrect } : o));
  setOptions(qIndex, options);
}

function addOption(qIndex) {
  setOptions(qIndex, [...questions.value[qIndex].options, createBlankQuizOption()]);
}

function removeOption(qIndex, oIndex) {
  setOptions(qIndex, questions.value[qIndex].options.filter((_, i) => i !== oIndex));
}
</script>

<template>
  <div class="quiz-part">
    <div class="quiz-part__threshold">
      <span>{{ t('courses.lessonEditor.parts.quiz.pass_threshold') }}</span>
      <div class="quiz-part__threshold-input">
        <input
          type="number"
          min="1"
          max="100"
          :value="passThreshold"
          :disabled="disabled"
          :aria-label="t('courses.lessonEditor.parts.quiz.pass_threshold')"
          @change="updateThreshold"
        >
        <span>%</span>
      </div>
    </div>

    <draggable
      :model-value="questions"
      item-key="id"
      handle=".quiz-question__handle"
      :disabled="disabled"
      class="quiz-part__questions"
      @update:model-value="setQuestions"
    >
      <template #item="{ element: question, index: qIndex }">
        <div class="quiz-question">
          <div class="quiz-question__header">
            <button
              type="button"
              class="quiz-question__handle"
              :disabled="disabled"
              :aria-label="t('courses.lessonEditor.parts.quiz.drag_question')"
            >
              ⠿
            </button>
            <span class="quiz-question__index">{{ t('courses.lessonEditor.parts.quiz.question_label', { number: qIndex + 1 }) }}</span>
            <div
              class="quiz-question__type"
              role="radiogroup"
            >
              <button
                type="button"
                class="quiz-question__type-option"
                :class="{ 'quiz-question__type-option--active': question.questionType === 'single' }"
                :disabled="disabled"
                @click="updateQuestionType(qIndex, 'single')"
              >
                {{ t('courses.lessonEditor.parts.quiz.question_type_single') }}
              </button>
              <button
                type="button"
                class="quiz-question__type-option"
                :class="{ 'quiz-question__type-option--active': question.questionType === 'multiple' }"
                :disabled="disabled"
                @click="updateQuestionType(qIndex, 'multiple')"
              >
                {{ t('courses.lessonEditor.parts.quiz.question_type_multiple') }}
              </button>
            </div>
            <button
              type="button"
              class="quiz-question__remove"
              :disabled="disabled || questions.length <= 1"
              :title="t('courses.lessonEditor.parts.quiz.remove_question')"
              :aria-label="t('courses.lessonEditor.parts.quiz.remove_question')"
              @click="removeQuestion(qIndex)"
            >
              ×
            </button>
          </div>

          <input
            type="text"
            class="quiz-question__text"
            :placeholder="t('courses.lessonEditor.parts.quiz.question_placeholder')"
            :value="question.text"
            :disabled="disabled"
            @input="updateQuestion(qIndex, { text: $event.target.value })"
          >

          <draggable
            :model-value="question.options"
            item-key="id"
            handle=".quiz-option__handle"
            :disabled="disabled"
            class="quiz-question__options"
            @update:model-value="(v) => setOptions(qIndex, v)"
          >
            <template #item="{ element: option, index: oIndex }">
              <div class="quiz-option">
                <button
                  type="button"
                  class="quiz-option__handle"
                  :disabled="disabled"
                  :aria-label="t('courses.lessonEditor.parts.quiz.drag_option')"
                >
                  ⠿
                </button>
                <button
                  type="button"
                  class="quiz-option__correct"
                  :class="{
                    'quiz-option__correct--active': option.isCorrect,
                    'quiz-option__correct--round': question.questionType === 'single',
                  }"
                  :disabled="disabled"
                  :title="t('courses.lessonEditor.parts.quiz.correct_answer')"
                  :aria-pressed="option.isCorrect"
                  @click="toggleOptionCorrect(qIndex, oIndex)"
                >
                  <span v-if="option.isCorrect">✓</span>
                </button>
                <input
                  type="text"
                  class="quiz-option__text"
                  :placeholder="t('courses.lessonEditor.parts.quiz.option_placeholder')"
                  :value="option.text"
                  :disabled="disabled"
                  @input="updateOptionText(qIndex, oIndex, $event.target.value)"
                >
                <button
                  type="button"
                  class="quiz-option__remove"
                  :disabled="disabled || question.options.length <= 2"
                  :title="t('courses.lessonEditor.parts.quiz.remove_option')"
                  :aria-label="t('courses.lessonEditor.parts.quiz.remove_option')"
                  @click="removeOption(qIndex, oIndex)"
                >
                  ×
                </button>
              </div>
            </template>
          </draggable>

          <button
            type="button"
            class="quiz-part__ghost-btn"
            :disabled="disabled"
            @click="addOption(qIndex)"
          >
            + {{ t('courses.lessonEditor.parts.quiz.add_option') }}
          </button>
        </div>
      </template>
    </draggable>

    <button
      type="button"
      class="quiz-part__ghost-btn quiz-part__add-question"
      :disabled="disabled"
      @click="addQuestion"
    >
      + {{ t('courses.lessonEditor.parts.quiz.add_question') }}
    </button>
  </div>
</template>

<style scoped>
.quiz-part {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  padding: 0.65rem 0.5rem 0.85rem;
}

.quiz-part__threshold {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding-bottom: 0.65rem;
  border-bottom: 1px solid var(--color-border-subtle);
  color: var(--color-ink-muted);
  font-size: 0.85rem;
  font-weight: 600;
}

.quiz-part__threshold-input {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  color: var(--color-ink);
  font-size: 0.9rem;
  font-weight: 700;
}

.quiz-part__threshold-input input {
  width: 4rem;
  padding: 0.35rem 0.5rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.45rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  font-size: 0.9rem;
  text-align: right;
}

.quiz-part__questions {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}

.quiz-question {
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
  padding: 0.85rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.65rem;
  background: var(--color-surface-950);
}

.quiz-question__header {
  display: flex;
  align-items: center;
  gap: 0.55rem;
}

.quiz-question__handle,
.quiz-option__handle {
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-ink-faint);
  font-size: 1rem;
  line-height: 1;
  cursor: grab;
}

.quiz-question__handle:disabled,
.quiz-option__handle:disabled {
  cursor: not-allowed;
  opacity: 0.4;
}

.quiz-question__index {
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 700;
}

.quiz-question__type {
  display: flex;
  gap: 0.25rem;
  margin-left: auto;
  padding: 0.15rem;
  border-radius: 0.5rem;
  background: var(--color-surface-900);
}

.quiz-question__type-option {
  padding: 0.3rem 0.55rem;
  border: 0;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 0.74rem;
  font-weight: 700;
  cursor: pointer;
}

.quiz-question__type-option--active {
  background: var(--color-surface-950);
  color: var(--color-ink);
  box-shadow: 0 1px 2px rgb(15 23 42 / 0.06);
}

.quiz-question__remove,
.quiz-option__remove {
  width: 1.5rem;
  height: 1.5rem;
  flex-shrink: 0;
  padding: 0;
  border: 0;
  border-radius: 0.35rem;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  line-height: 1;
  cursor: pointer;
}

.quiz-question__remove:hover:not(:disabled),
.quiz-option__remove:hover:not(:disabled) {
  background: color-mix(in srgb, #b33a2b 10%, transparent);
  color: #b33a2b;
}

.quiz-question__remove:disabled,
.quiz-option__remove:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.quiz-question__text {
  width: 100%;
  padding: 0.55rem 0.65rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.45rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  font-size: 0.92rem;
  font-weight: 600;
}

.quiz-question__options {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.quiz-option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.quiz-option__correct {
  width: 1.35rem;
  height: 1.35rem;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: 1.5px solid var(--color-ink-faint);
  border-radius: 0.3rem;
  background: transparent;
  color: #ffffff;
  font-size: 0.75rem;
  line-height: 1;
  cursor: pointer;
}

.quiz-option__correct--round {
  border-radius: 999px;
}

.quiz-option__correct--active {
  border-color: var(--color-accent-coral);
  background: var(--color-accent-coral);
}

.quiz-option__correct:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.quiz-option__text {
  flex: 1;
  min-width: 0;
  padding: 0.4rem 0.6rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.45rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  font-size: 0.88rem;
}

.quiz-part__ghost-btn {
  align-self: flex-start;
  padding: 0.35rem 0.45rem;
  border: 0;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
}

.quiz-part__ghost-btn:hover:not(:disabled) {
  background: var(--color-surface-900);
  color: var(--color-ink);
}

.quiz-part__ghost-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.quiz-part__add-question {
  margin-top: 0.15rem;
  padding-top: 0.55rem;
  border-top: 1px solid var(--color-border-subtle);
  border-radius: 0;
}
</style>
