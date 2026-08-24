<script setup>
import { cn } from '@/lib/utils';

const props = defineProps({
  modelValue: { type: [String, Number, Object], default: '' },
  options: { type: Array, default: () => [] },
  placeholder: { type: String, default: '' },
  class: { type: [String, Object, Array], default: '' },
  valueKey: { type: String, default: 'value' },
  labelKey: { type: String, default: 'label' },
});

const emit = defineEmits(['update:modelValue']);

function optionValue(option) {
  return typeof option === 'object' ? option[props.valueKey] : option;
}

function optionLabel(option) {
  return typeof option === 'object' ? option[props.labelKey] : option;
}
</script>

<template>
  <select
    :value="typeof modelValue === 'object' && modelValue ? modelValue[valueKey] : modelValue"
    :class="cn(
      'flex h-10 w-full rounded-md border border-border-subtle bg-card px-3 text-sm text-ink focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
      props.class,
    )"
    @change="emit('update:modelValue', $event.target.value)"
  >
    <option
      v-if="placeholder"
      value=""
      disabled
    >
      {{ placeholder }}
    </option>
    <option
      v-for="option in options"
      :key="optionValue(option)"
      :value="optionValue(option)"
    >
      {{ optionLabel(option) }}
    </option>
  </select>
</template>
