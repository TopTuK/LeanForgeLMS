<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps({
  title: { type: String, default: '' },
  chapterTitle: { type: String, default: '' },
  parts: { type: Array, default: () => [] },
});

const { t } = useI18n();

const isEmpty = computed(() => props.parts.length === 0);
const displayTitle = computed(() => props.title.trim() || t('courses.lessonEditor.preview.untitled'));

function mediaPlaceholder(part) {
  if (part.needsReupload && part.fileName) {
    return t('courses.lessonEditor.parts.reupload', { fileName: part.fileName });
  }
  return t('courses.lessonEditor.preview.media_missing');
}
</script>

<template>
  <article class="lesson-preview">
    <header class="lesson-preview__header">
      <p
        v-if="chapterTitle"
        class="lesson-preview__breadcrumb"
      >
        {{ chapterTitle }}
      </p>
      <h2>{{ displayTitle }}</h2>
    </header>

    <p
      v-if="isEmpty"
      class="lesson-preview__empty"
    >
      {{ t('courses.lessonEditor.preview.empty') }}
    </p>

    <div
      v-else
      class="lesson-preview__parts"
    >
      <template
        v-for="part in parts"
        :key="part.id"
      >
        <!-- Instructor-authored TipTap HTML, same pattern as the student learn view. -->
        <!-- eslint-disable vue/no-v-html -->
        <div
          v-if="part.type === 'text'"
          class="lesson-preview__prose"
          v-html="part.html"
        />
        <!-- eslint-enable vue/no-v-html -->

        <div
          v-else-if="part.type === 'quiz'"
          class="lesson-preview__quiz"
        >
          <p class="lesson-preview__quiz-threshold">
            {{ t('courses.lessonEditor.preview.quiz_threshold', { percent: part.quizPassThreshold }) }}
          </p>
          <div
            v-for="(question, qIndex) in part.quizQuestions"
            :key="question.id"
            class="lesson-preview__quiz-question"
          >
            <p class="lesson-preview__quiz-question-text">
              {{ qIndex + 1 }}. {{ question.text || t('courses.lessonEditor.preview.quiz_untitled_question') }}
            </p>
            <ul class="lesson-preview__quiz-options">
              <li
                v-for="option in question.options"
                :key="option.id"
                :class="{ 'lesson-preview__quiz-option--correct': option.isCorrect }"
              >
                {{ option.text }}
              </li>
            </ul>
          </div>
        </div>

        <div
          v-else
          class="lesson-preview__media"
        >
          <template v-if="part.objectUrl">
            <img
              v-if="part.type === 'image'"
              :src="part.objectUrl"
              :alt="part.fileName || ''"
              class="lesson-preview__image"
            >
            <video
              v-else-if="part.type === 'video'"
              :src="part.objectUrl"
              class="lesson-preview__player"
              controls
              preload="metadata"
            />
            <audio
              v-else-if="part.type === 'audio'"
              :src="part.objectUrl"
              class="lesson-preview__player lesson-preview__player--audio"
              controls
              preload="metadata"
            />
          </template>
          <p
            v-else
            class="lesson-preview__missing"
          >
            {{ mediaPlaceholder(part) }}
          </p>
        </div>
      </template>
    </div>
  </article>
</template>

<style scoped>
.lesson-preview {
  min-width: 0;
  padding: 0.5rem 0 2rem;
  overflow-wrap: anywhere;
}

.lesson-preview__header {
  margin-bottom: 1.25rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--color-border-subtle);
}

.lesson-preview__breadcrumb {
  margin: 0 0 0.4rem;
  color: var(--color-ink-faint);
  font-size: 0.82rem;
  font-weight: 600;
}

.lesson-preview__header h2 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.6rem, 3vw, 2rem);
  font-weight: 800;
  letter-spacing: -0.03em;
}

.lesson-preview__empty,
.lesson-preview__missing {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
}

.lesson-preview__parts {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.lesson-preview__prose {
  color: var(--color-ink);
  font-size: 1rem;
  line-height: 1.7;
}

.lesson-preview__prose :deep(h2) {
  margin: 1.25rem 0 0.65rem;
  font-size: 1.35rem;
  font-weight: 800;
}

.lesson-preview__prose :deep(h3) {
  margin: 1.1rem 0 0.5rem;
  font-size: 1.1rem;
  font-weight: 700;
}

.lesson-preview__prose :deep(p) {
  margin: 0.55rem 0;
}

.lesson-preview__prose :deep(ul),
.lesson-preview__prose :deep(ol) {
  margin: 0.55rem 0;
  padding-left: 1.35rem;
}

.lesson-preview__prose :deep(blockquote) {
  margin: 0.85rem 0;
  padding: 0.35rem 0 0.35rem 0.95rem;
  border-left: 3px solid var(--color-accent-coral);
  color: var(--color-ink-muted);
}

.lesson-preview__prose :deep(code) {
  padding: 0.12rem 0.35rem;
  border-radius: 0.2rem;
  background: var(--color-surface-900);
  border: 1px solid var(--color-border-subtle);
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 0.88em;
}

.lesson-preview__prose :deep(a) {
  color: var(--color-accent-coral-dark);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.lesson-preview__prose :deep(img) {
  display: block;
  max-width: 100%;
  height: auto;
  margin: 0.85rem 0;
  border-radius: 0.55rem;
}

.lesson-preview__image {
  display: block;
  max-width: 100%;
  height: auto;
  border-radius: 0.55rem;
}

.lesson-preview__player {
  display: block;
  width: 100%;
  max-height: 22rem;
  border-radius: 0.55rem;
  background: var(--color-surface-900);
}

.lesson-preview__player--audio {
  height: 2.6rem;
}

.lesson-preview__missing {
  padding: 1.1rem 1.25rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.65rem;
  background: var(--color-surface-900);
}

.lesson-preview__quiz {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  padding: 1rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.65rem;
  background: var(--color-surface-900);
}

.lesson-preview__quiz-threshold {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 600;
}

.lesson-preview__quiz-question {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.lesson-preview__quiz-question-text {
  margin: 0;
  color: var(--color-ink);
  font-weight: 700;
}

.lesson-preview__quiz-options {
  margin: 0;
  padding-left: 1.25rem;
  color: var(--color-ink-muted);
  font-size: 0.92rem;
}

.lesson-preview__quiz-option--correct {
  color: var(--color-accent-coral-dark);
  font-weight: 700;
}
</style>
