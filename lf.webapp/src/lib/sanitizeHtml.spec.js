import { describe, it, expect } from 'vitest';
import { sanitizeHtml } from '@/lib/sanitizeHtml';

describe('sanitizeHtml', () => {
  it('returns an empty string for empty input', () => {
    expect(sanitizeHtml('')).toBe('');
    expect(sanitizeHtml(null)).toBe('');
    expect(sanitizeHtml(undefined)).toBe('');
  });

  it('keeps allowed formatting markup', () => {
    const html = '<h2>Title</h2><p><strong>bold</strong> and <em>italic</em></p><ul><li>one</li></ul>';
    expect(sanitizeHtml(html)).toBe(html);
  });

  it('strips <script> tags', () => {
    const out = sanitizeHtml('<p>hi</p><script>alert(1)</script>');
    expect(out).toBe('<p>hi</p>');
    expect(out).not.toContain('script');
  });

  it('strips inline event handlers', () => {
    const out = sanitizeHtml('<img src="x" onerror="alert(1)">');
    expect(out).not.toContain('onerror');
  });

  it('drops javascript: hrefs but keeps the link element', () => {
    const out = sanitizeHtml('<a href="javascript:alert(1)">x</a>');
    expect(out).not.toContain('javascript:');
  });

  it('removes disallowed elements like iframe and style', () => {
    const out = sanitizeHtml('<iframe src="https://evil.test"></iframe><style>*{}</style><p>ok</p>');
    expect(out).toBe('<p>ok</p>');
  });

  it('forces safe rel/target on links', () => {
    const out = sanitizeHtml('<a href="https://example.com">x</a>');
    expect(out).toContain('rel="noopener noreferrer"');
    expect(out).toContain('target="_blank"');
  });

  it('keeps http(s) images', () => {
    const out = sanitizeHtml('<img src="https://cdn.test/a.png" alt="a">');
    expect(out).toContain('src="https://cdn.test/a.png"');
  });
});
