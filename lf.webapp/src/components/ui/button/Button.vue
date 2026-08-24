<script setup>
import { Primitive } from 'reka-ui';
import { cn } from '@/lib/utils';
import { buttonVariants } from './variants';

const props = defineProps({
  variant: { type: String, default: 'default' },
  size: { type: String, default: 'default' },
  as: { type: String, default: 'button' },
  asChild: { type: Boolean, default: false },
  class: { type: [String, Object, Array], default: '' },
  type: { type: String, default: 'button' },
});
</script>

<template>
  <Primitive
    :as="as"
    :as-child="asChild"
    :type="as === 'button' ? type : undefined"
    :class="cn(buttonVariants({ variant, size }), props.class)"
  >
    <slot />
  </Primitive>
</template>
