// Q&A timestamps carry a time of day, unlike news dates which are day-granular.
export function formatQuestionTimestamp(value, locale) {
  if (!value) return '';
  return new Intl.DateTimeFormat(locale, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value));
}

const RELATIVE_STEPS = [
  { unit: 'second', seconds: 60 },
  { unit: 'minute', seconds: 60 * 60 },
  { unit: 'hour', seconds: 60 * 60 * 24 },
  { unit: 'day', seconds: 60 * 60 * 24 * 7 },
];

// "5 minutes ago" for recent activity, then a plain date: past a week, relative wording stops being
// easier to read than the date itself. `now` is injectable so tests don't depend on the clock.
export function formatRelativeTime(value, locale, now = new Date()) {
  if (!value) return '';

  const date = new Date(value);
  const deltaSeconds = Math.round((date.getTime() - now.getTime()) / 1000);
  const abs = Math.abs(deltaSeconds);

  if (abs < 45) {
    return new Intl.RelativeTimeFormat(locale, { numeric: 'auto' }).format(0, 'second');
  }

  let divisor = 1;
  for (const step of RELATIVE_STEPS) {
    if (abs < step.seconds) {
      return new Intl.RelativeTimeFormat(locale, { numeric: 'auto' }).format(Math.round(deltaSeconds / divisor), step.unit);
    }
    divisor = step.seconds;
  }

  const sameYear = date.getFullYear() === now.getFullYear();
  return new Intl.DateTimeFormat(locale, {
    day: 'numeric',
    month: 'short',
    ...(sameYear ? {} : { year: 'numeric' }),
  }).format(date);
}

export function initialsOf(name) {
  const parts = String(name ?? '').trim().split(/\s+/).filter(Boolean);
  if (!parts.length) return '?';
  return parts.slice(0, 2).map((part) => part[0].toUpperCase()).join('');
}
