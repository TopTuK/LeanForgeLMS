<script setup>
import { nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import StudioButton from './StudioButton.vue';

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  title: { type: String, required: true },
  label: { type: String, default: '' },
  placeholder: { type: String, default: '' },
  confirmLabel: { type: String, default: 'Create' },
  cancelLabel: { type: String, default: 'Cancel' },
  initialValue: { type: String, default: '' },
});

const emit = defineEmits(['update:modelValue', 'confirm', 'cancel']);

const value = ref('');
const inputRef = ref(null);
const submitting = ref(false);

function close() {
  emit('update:modelValue', false);
  emit('cancel');
}

function confirm() {
  const trimmed = value.value.trim();
  if (!trimmed || submitting.value) return;
  submitting.value = true;
  emit('confirm', trimmed);
  emit('update:modelValue', false);
  submitting.value = false;
}

function onKeydown(event) {
  if (!props.modelValue) return;
  if (event.key === 'Escape') {
    event.preventDefault();
    close();
  }
}

watch(
  () => props.modelValue,
  async (open) => {
    document.documentElement.classList.toggle('is-modal-open', open);
    if (open) {
      value.value = props.initialValue;
      await nextTick();
      inputRef.value?.focus();
      inputRef.value?.select();
    }
  },
);

onMounted(() => window.addEventListener('keydown', onKeydown));
onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown);
  document.documentElement.classList.remove('is-modal-open');
});
</script>

<template>
  <Teleport to="body">
    <div
      v-if="modelValue"
      class="studio-prompt"
      role="dialog"
      aria-modal="true"
      :aria-label="title"
    >
      <button
        type="button"
        class="studio-prompt__backdrop"
        :aria-label="cancelLabel"
        @click="close"
      />
      <div class="studio-prompt__panel">
        <h2 class="studio-prompt__title">
          {{ title }}
        </h2>
        <label class="studio-prompt__field">
          <span
            v-if="label"
            class="studio-prompt__label"
          >{{ label }}</span>
          <input
            ref="inputRef"
            v-model="value"
            type="text"
            class="studio-prompt__input"
            :placeholder="placeholder"
            @keydown.enter.prevent="confirm"
          >
        </label>
        <div class="studio-prompt__actions">
          <StudioButton
            variant="ghost"
            @click="close"
          >
            {{ cancelLabel }}
          </StudioButton>
          <StudioButton
            variant="primary"
            :disabled="!value.trim()"
            @click="confirm"
          >
            {{ confirmLabel }}
          </StudioButton>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.studio-prompt {
  position: fixed;
  inset: 0;
  z-index: 80;
  display: grid;
  place-items: center;
  padding: 1.25rem;
}

.studio-prompt__backdrop {
  position: absolute;
  inset: 0;
  border: 0;
  background: rgb(15 23 42 / 0.35);
  cursor: pointer;
}

.studio-prompt__panel {
  position: relative;
  z-index: 1;
  width: min(26rem, 100%);
  padding: 1.35rem 1.35rem 1.15rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.75rem;
  background: var(--color-surface-950);
  box-shadow: 0 24px 48px -28px rgb(15 23 42 / 0.45);
}

.studio-prompt__title {
  margin: 0 0 0.85rem;
  color: var(--color-ink);
  font-size: 1.05rem;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.studio-prompt__field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.studio-prompt__label {
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 600;
}

.studio-prompt__input {
  width: 100%;
  padding: 0.65rem 0.75rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.5rem;
  background: var(--color-surface-900);
  color: var(--color-ink);
  font: inherit;
  font-size: 0.95rem;
}

.studio-prompt__input:focus {
  outline: 2px solid color-mix(in srgb, var(--color-accent-coral) 35%, transparent);
  outline-offset: 0;
  border-color: transparent;
}

.studio-prompt__actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  margin-top: 1.25rem;
}
</style>
