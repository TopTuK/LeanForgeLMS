const BLOCK_END = /<\/(p|h[1-6]|li|blockquote|pre)>|<br\s*\/?>/gi;

// Plain-text excerpt for news cards. DOMParser neither runs scripts nor loads images, and the
// result is rendered as text, so this is safe on any input. Block boundaries become spaces so
// "<p>One</p><p>Two</p>" reads "One Two", not "OneTwo".
export function htmlToText(html) {
  if (!html) return '';
  const doc = new DOMParser().parseFromString(html.replace(BLOCK_END, '$& '), 'text/html');
  doc.querySelectorAll('script, style').forEach((node) => node.remove());
  return (doc.body.textContent ?? '').replace(/\s+/g, ' ').trim();
}

export function formatNewsDate(value, locale) {
  if (!value) return '';
  return new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'long', year: 'numeric' }).format(new Date(value));
}
