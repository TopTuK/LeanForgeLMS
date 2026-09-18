<script setup>
import { useI18n } from 'vue-i18n';
import QuestionStatusBadge from '@/components/questions/QuestionStatusBadge.vue';
import { formatQuestionTimestamp } from '@/lib/questions';

defineProps({
  question: { type: Object, required: true },
  selected: { type: Boolean, default: false },
});

const emit = defineEmits(['select']);

const { locale } = useI18n();
</script>

<template>
  <li>
    <button
      type="button"
      class="question-item"
      :class="{ 'is-selected': selected, 'has-unread': question.hasUnread }"
      :aria-current="selected ? 'true' : undefined"
      @click="emit('select', question)"
    >
      <span class="question-item__head">
        <span class="question-item__title">{{ question.title }}</span>
        <QuestionStatusBadge :status="question.status" />
      </span>

      <span class="question-item__meta">
        {{ question.courseTitle }} &middot; {{ question.lessonTitle }}
      </span>

      <span class="question-item__foot">
        <span class="question-item__author">{{ question.studentName }}</span>
        <time :datetime="question.lastMessageAt">{{ formatQuestionTimestamp(question.lastMessageAt, locale) }}</time>
      </span>

      <!-- The dot is decorative; the unread state is announced through the label below it. -->
      <span
        v-if="question.hasUnread"
        class="question-item__dot"
        aria-hidden="true"
      />
      <span
        v-if="question.hasUnread"
        class="sr-only"
      >{{ $t('questions.unread_marker') }}</span>
    </button>
  </li>
</template>

<style scoped>
.question-item {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  width: 100%;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-card);
  padding: 0.75rem 1rem;
  text-align: left;
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease;
}

.question-item:hover {
  border-color: color-mix(in srgb, var(--color-accent-coral) 45%, transparent);
}

.question-item.is-selected {
  border-color: var(--color-accent-coral);
  background: var(--color-accent-soft);
}

.question-item__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.question-item__title {
  font-weight: 600;
  color: var(--color-ink);
}

.question-item.has-unread .question-item__title {
  font-weight: 700;
}

.question-item__meta,
.question-item__foot {
  font-size: 0.8rem;
  color: var(--color-ink-muted);
}

.question-item__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.question-item__dot {
  position: absolute;
  top: 0.6rem;
  left: -0.3rem;
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 999px;
  background: var(--color-accent-coral);
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}
</style>
