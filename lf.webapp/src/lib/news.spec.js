import { describe, it, expect } from 'vitest';
import { formatNewsDate, htmlToText } from '@/lib/news';

describe('htmlToText', () => {
  it('returns an empty string for empty input', () => {
    expect(htmlToText('')).toBe('');
    expect(htmlToText(null)).toBe('');
  });

  it('keeps words from adjacent blocks apart', () => {
    expect(htmlToText('<h2>Title</h2><p>One</p><p>Two<br>Three</p>')).toBe('Title One Two Three');
  });

  it('drops markup and script content', () => {
    expect(htmlToText('<p><strong>Bold</strong> text</p><script>alert(1)</script>')).toBe('Bold text');
  });
});

describe('formatNewsDate', () => {
  it('formats a date in the given locale', () => {
    const formatted = formatNewsDate('2026-09-10T10:00:00Z', 'en');

    expect(formatted).toContain('September');
    expect(formatted).toContain('2026');
  });

  it('returns an empty string for a missing date', () => {
    expect(formatNewsDate(null, 'en')).toBe('');
  });
});
