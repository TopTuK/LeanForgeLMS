import { sanitizeHtml } from '@/lib/sanitizeHtml';

// Drop-in replacement for `v-html` that runs the value through DOMPurify first.
// Registered globally as `v-safe-html` in main.js.
export const vSafeHtml = {
  mounted(el, binding) {
    el.innerHTML = sanitizeHtml(binding.value);
  },
  updated(el, binding) {
    if (binding.value === binding.oldValue) return;
    el.innerHTML = sanitizeHtml(binding.value);
  },
};
