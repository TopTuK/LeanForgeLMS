<script setup>
import { ref, useId } from 'vue';

defineProps({
  index: { type: String, default: '' },
  question: { type: String, required: true },
  answer: { type: String, required: true },
});

const open = ref(false);
const panelId = `faq-panel-${useId()}`;
</script>

<template>
  <div class="faq-item">
    <button
      type="button"
      class="faq-item__trigger"
      :aria-expanded="open"
      :aria-controls="panelId"
      @click="open = !open"
    >
      <span
        v-if="index"
        class="faq-item__index mono-label"
        aria-hidden="true"
      >{{ index }}</span>
      <span class="faq-item__question">{{ question }}</span>
      <span
        class="faq-item__glyph"
        :class="open ? 'is-open' : ''"
        aria-hidden="true"
      >+</span>
    </button>

    <div
      v-show="open"
      :id="panelId"
      class="faq-item__answer"
    >
      {{ answer }}
    </div>
  </div>
</template>

<style scoped>
.faq-item {
  border-bottom: 1px solid var(--color-border-subtle);
}

.faq-item__trigger {
  display: grid;
  grid-template-columns: auto 1fr auto;
  align-items: baseline;
  gap: 1rem;
  width: 100%;
  padding: 1.35rem 0;
  text-align: left;
  cursor: pointer;
}

.faq-item__index {
  color: var(--color-accent-coral);
}

.faq-item__question {
  color: var(--color-ink);
  font-size: 1rem;
  font-weight: 600;
  line-height: 1.4;
}

.faq-item__glyph {
  color: var(--color-ink-muted);
  font-size: 1.35rem;
  line-height: 1;
  transition: transform 0.15s ease;
}

.faq-item__glyph.is-open {
  transform: rotate(45deg);
}

.faq-item__answer {
  padding: 0 2.5rem 1.35rem 2.5rem;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
  line-height: 1.7;
}

@media (max-width: 640px) {
  .faq-item__answer {
    padding-left: 0;
  }
}
</style>
