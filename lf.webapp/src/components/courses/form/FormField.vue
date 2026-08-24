<script setup>
import { computed, useId } from 'vue';

const props = defineProps({
  modelValue: { type: String, default: '' },
  label: { type: String, required: true },
  index: { type: String, default: '' },
  type: { type: String, default: 'text' },
  rows: { type: Number, default: 4 },
  placeholder: { type: String, default: '' },
  disabled: { type: Boolean, default: false },
  required: { type: Boolean, default: false },
});

const emit = defineEmits(['update:modelValue']);

const fieldId = useId();
const isTextarea = computed(() => props.type === 'textarea');

function onInput(event) {
  emit('update:modelValue', event.target.value);
}
</script>

<template>
  <div class="form-field">
    <div class="form-field__meta">
      <label
        class="form-field__label"
        :for="fieldId"
      >
        {{ label }}
      </label>
      <span
        v-if="index"
        class="form-field__index"
        aria-hidden="true"
      >{{ index }}</span>
    </div>
    <textarea
      v-if="isTextarea"
      :id="fieldId"
      class="form-field__control form-field__control--area"
      :value="modelValue"
      :rows="rows"
      :placeholder="placeholder"
      :disabled="disabled"
      :required="required"
      @input="onInput"
    />
    <input
      v-else
      :id="fieldId"
      class="form-field__control"
      :type="type"
      :value="modelValue"
      :placeholder="placeholder"
      :disabled="disabled"
      :required="required"
      @input="onInput"
    >
  </div>
</template>

<style scoped>
.form-field {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-field__meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.form-field__label {
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 600;
  letter-spacing: 0.04em;
}

.form-field__index {
  color: var(--color-ink-faint);
  font-family: var(--font-mono);
  font-size: 0.65rem;
  font-weight: 500;
  letter-spacing: 0.08em;
}

.form-field__control {
  width: 100%;
  padding: 0.85rem 1rem;
  color: var(--color-ink);
  background: var(--color-card);
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.5rem;
  font-family: inherit;
  font-size: 0.95rem;
  line-height: 1.45;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.form-field__control--area {
  resize: vertical;
  min-height: 6rem;
}

.form-field__control:disabled {
  color: var(--color-ink-faint);
  background: var(--color-surface-900);
  cursor: not-allowed;
}

.form-field__control:focus {
  outline: none;
  border-color: var(--color-accent-coral);
  box-shadow: 0 0 0 3px var(--industrial-accent-wash);
}

.form-field__control::placeholder {
  color: var(--color-ink-faint);
}
</style>
