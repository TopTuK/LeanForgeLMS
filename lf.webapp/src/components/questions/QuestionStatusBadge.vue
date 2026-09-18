<script setup>
import { computed } from 'vue';
import { Badge } from '@/components/ui/badge';

const props = defineProps({
  status: { type: String, required: true },
});

// Open is the one that wants attention, so it gets the accent; Closed recedes.
const VARIANTS = { Open: 'coral', Answered: 'default', Closed: 'muted' };

const variant = computed(() => VARIANTS[props.status] ?? 'default');
const labelKey = computed(() => `questions.status.${String(props.status).toLowerCase()}`);
</script>

<template>
  <Badge :variant="variant">
    {{ $t(labelKey) }}
  </Badge>
</template>
