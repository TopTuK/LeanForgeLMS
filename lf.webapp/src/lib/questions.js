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
