import { describe, it, expect } from 'vitest';
import { formatRelativeTime, initialsOf } from '@/lib/questions';

const NOW = new Date('2026-09-18T12:00:00Z');
const ago = (seconds) => new Date(NOW.getTime() - seconds * 1000).toISOString();

describe('formatRelativeTime', () => {
  it('returns an empty string for no value', () => {
    expect(formatRelativeTime(null, 'en', NOW)).toBe('');
  });

  it('calls the last few seconds "now"', () => {
    expect(formatRelativeTime(ago(10), 'en', NOW)).toBe('now');
  });

  it('counts minutes, hours and days while they are the easier read', () => {
    expect(formatRelativeTime(ago(5 * 60), 'en', NOW)).toBe('5 minutes ago');
    expect(formatRelativeTime(ago(3 * 3600), 'en', NOW)).toBe('3 hours ago');
    expect(formatRelativeTime(ago(24 * 3600), 'en', NOW)).toBe('yesterday');
    expect(formatRelativeTime(ago(3 * 86400), 'en', NOW)).toBe('3 days ago');
  });

  it('switches to a date after a week, adding the year only when it differs', () => {
    expect(formatRelativeTime('2026-09-01T12:00:00Z', 'en', NOW)).toBe('Sep 1');
    expect(formatRelativeTime('2025-09-01T12:00:00Z', 'en', NOW)).toBe('Sep 1, 2025');
  });

  it('localises the wording', () => {
    expect(formatRelativeTime(ago(5 * 60), 'ru', NOW)).toBe('5 минут назад');
  });
});

describe('initialsOf', () => {
  it('takes the first letter of the first two words', () => {
    expect(initialsOf('kim creator')).toBe('KC');
    expect(initialsOf('Anna Maria Lopez')).toBe('AM');
    expect(initialsOf('Ada')).toBe('A');
  });

  it('falls back to a placeholder for a missing name', () => {
    expect(initialsOf('')).toBe('?');
    expect(initialsOf(null)).toBe('?');
  });
});
