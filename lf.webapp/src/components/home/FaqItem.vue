<script setup>
import { ref } from 'vue';

defineProps({
    question: { type: String, required: true },
    answer: { type: String, required: true },
});

const open = ref(false);
</script>

<template>
  <div class="border-b border-border-subtle">
    <button
      type="button"
      class="w-full flex items-center justify-between gap-4 py-5 text-left"
      :aria-expanded="open"
      @click="open = !open"
    >
      <span class="font-semibold text-ink text-base md:text-lg pr-4">
        {{ question }}
      </span>
      <span
        class="shrink-0 text-ink-muted text-xl leading-none transition-transform"
        :class="open ? 'rotate-45' : ''"
        aria-hidden="true"
      >+</span>
    </button>
    <div
      v-show="open"
      class="pb-5 pr-10 text-sm md:text-base text-ink-muted leading-relaxed"
    >
      {{ answer }}
    </div>
  </div>
</template>
