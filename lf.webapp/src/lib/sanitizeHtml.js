import DOMPurify from 'dompurify';

// Mirrors the server-side allow-list (LF.Application GanssHtmlSanitizer) so the two agree on
// what "safe rich text" means. This is the render-time backstop: it also neutralises rows that
// were stored before server-side sanitisation existed.
const CONFIG = {
  ALLOWED_TAGS: [
    'p', 'br', 'span',
    'h1', 'h2', 'h3',
    'strong', 'b', 'em', 'i', 'u', 's', 'mark', 'sub', 'sup',
    'ul', 'ol', 'li',
    'blockquote', 'pre', 'code', 'hr',
    'table', 'thead', 'tbody', 'tfoot', 'tr', 'th', 'td',
    'a', 'img',
  ],
  ALLOWED_ATTR: ['href', 'target', 'rel', 'title', 'src', 'alt', 'style', 'class'],
  ALLOW_DATA_ATTR: false,
  // http(s), mailto, protocol-relative, and fragment/relative links only — blocks javascript:.
  ALLOWED_URI_REGEXP: /^(?:(?:https?|mailto):|[^a-z]|[a-z+.-]+(?:[^a-z+.:-]|$))/i,
};

let hookInstalled = false;

function ensureHook() {
  if (hookInstalled) return;
  DOMPurify.addHook('afterSanitizeAttributes', (node) => {
    if (node.hasAttribute('class')) {
      const classes = node.getAttribute('class').split(/\s+/);
      const languageClass = classes.find((value) => /^language-[a-z0-9+#.-]{1,32}$/.test(value));
      if (node.tagName === 'CODE' && languageClass) {
        node.setAttribute('class', languageClass);
      } else {
        node.removeAttribute('class');
      }
    }

    if (node.tagName === 'A' && node.hasAttribute('href')) {
      node.setAttribute('target', '_blank');
      node.setAttribute('rel', 'noopener noreferrer');
    }
  });
  hookInstalled = true;
}

export function sanitizeHtml(html) {
  if (!html) return '';
  ensureHook();
  return DOMPurify.sanitize(String(html), CONFIG);
}
