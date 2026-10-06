// Lecture times travel as UTC ISO strings; the UI shows and edits them in the viewer's local time.

export function formatLectureDay(value, locale) {
  return new Intl.DateTimeFormat(locale, { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date(value));
}

export function formatLectureTime(value, locale) {
  return new Intl.DateTimeFormat(locale, { hour: '2-digit', minute: '2-digit' }).format(new Date(value));
}

export function formatLectureDateTime(value, locale) {
  return new Intl.DateTimeFormat(locale, {
    day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  }).format(new Date(value));
}

export function lectureEnd(lecture) {
  return new Date(new Date(lecture.startsAt).getTime() + lecture.durationMinutes * 60_000);
}

// <input type="datetime-local"> wants "YYYY-MM-DDTHH:mm" in local time, with no zone suffix.
export function toLocalInputValue(value) {
  const date = new Date(value);
  const pad = (n) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

// Null for an empty or partially typed value, which would otherwise make toISOString() throw.
export function fromLocalInputValue(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

// Join opens shortly before the start so students can get in early, and closes when the lecture ends.
export const JOIN_WINDOW_MINUTES = 15;

export function canJoinLecture(lecture, now = new Date()) {
  if (lecture.isCancelled || !lecture.meetingUrl) return false;
  const opensAt = new Date(lecture.startsAt).getTime() - JOIN_WINDOW_MINUTES * 60_000;
  return now.getTime() >= opensAt && now < lectureEnd(lecture);
}

// Groups lectures into [{ day: 'YYYY-MM-DD' (local), items: [...] }] in chronological order.
export function groupLecturesByDay(lectures) {
  const byDay = new Map();
  for (const lecture of [...lectures].sort((a, b) => new Date(a.startsAt) - new Date(b.startsAt))) {
    const key = toLocalInputValue(lecture.startsAt).slice(0, 10);
    if (!byDay.has(key)) byDay.set(key, []);
    byDay.get(key).push(lecture);
  }
  return [...byDay.entries()].map(([day, items]) => ({ day, items }));
}
