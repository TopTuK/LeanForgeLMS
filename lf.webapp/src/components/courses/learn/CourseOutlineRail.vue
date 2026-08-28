<script setup>
defineProps({
  chapters: { type: Array, required: true },
  selectedLessonId: { type: Number, default: null },
  title: { type: String, default: '' },
});

const emit = defineEmits(['select']);

function onSelect(lessonId) {
  emit('select', lessonId);
}
</script>

<template>
  <aside
    class="outline-rail"
    :aria-label="title || undefined"
  >
    <h2
      v-if="title"
      class="outline-rail__heading"
    >
      {{ title }}
    </h2>

    <div
      v-for="chapter in chapters"
      :key="chapter.id"
      class="outline-rail__chapter"
    >
      <h3 class="outline-rail__chapter-title">
        {{ chapter.title }}
      </h3>
      <button
        v-for="lesson in chapter.lessons"
        :key="lesson.id"
        type="button"
        class="outline-rail__lesson"
        :class="{
          'outline-rail__lesson--active': lesson.id === selectedLessonId,
          'outline-rail__lesson--done': lesson.isCompleted,
        }"
        @click="onSelect(lesson.id)"
      >
        <span
          class="outline-rail__dot"
          aria-hidden="true"
        />
        <span class="outline-rail__lesson-title">{{ lesson.title }}</span>
      </button>
    </div>
  </aside>
</template>

<style scoped>
.outline-rail {
  display: flex;
  flex-direction: column;
  gap: 1.15rem;
  height: 100%;
  padding: 1rem 0.85rem;
  overflow-y: auto;
}

.outline-rail__heading {
  margin: 0 0 0.15rem;
  padding: 0 0.35rem;
  color: var(--color-ink);
  font-size: 0.92rem;
  font-weight: 700;
  letter-spacing: -0.01em;
}

.outline-rail__chapter-title {
  margin: 0 0 0.35rem;
  padding: 0 0.35rem;
  color: var(--color-ink-muted);
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.outline-rail__lesson {
  display: flex;
  align-items: flex-start;
  gap: 0.55rem;
  width: 100%;
  padding: 0.45rem 0.45rem;
  border: 0;
  border-radius: 0.4rem;
  background: transparent;
  color: var(--color-ink-muted);
  font: inherit;
  font-size: 0.86rem;
  line-height: 1.35;
  text-align: left;
  cursor: pointer;
}

.outline-rail__lesson:hover {
  background: var(--color-surface-900);
  color: var(--color-ink);
}

.outline-rail__lesson--active {
  background: var(--color-accent-soft);
  color: var(--color-ink);
  font-weight: 600;
}

.outline-rail__dot {
  width: 0.5rem;
  height: 0.5rem;
  margin-top: 0.35rem;
  flex-shrink: 0;
  border-radius: 999px;
  border: 1.5px solid var(--color-ink-faint);
}

.outline-rail__lesson--done .outline-rail__dot {
  background: var(--color-accent-coral);
  border-color: var(--color-accent-coral);
}

.outline-rail__lesson-title {
  overflow-wrap: anywhere;
}
</style>
