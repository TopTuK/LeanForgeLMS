import { sanitizeHtml } from '@/lib/sanitizeHtml';
import { highlightCodeBlocks } from '@/lib/codeHighlight';

// Drop-in replacement for `v-html` that runs the value through DOMPurify first.
// Registered globally as `v-safe-html` in main.js.
function renderSafeHtml(el, value) {
  el.innerHTML = sanitizeHtml(value);
  highlightCodeBlocks(el);
}

export const vSafeHtml = {
  mounted(el, binding) {
    renderSafeHtml(el, binding.value);
  },
  updated(el, binding) {
    if (binding.value === binding.oldValue) return;
    renderSafeHtml(el, binding.value);
  },
};
