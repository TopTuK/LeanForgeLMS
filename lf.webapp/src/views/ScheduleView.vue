<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Video } from 'lucide-vue-next';
import { fetchMySchedule } from '@/services/lectureService';
import {
  canJoinLecture,
  formatLectureDay,
  formatLectureTime,
  groupLecturesByDay,
  lectureEnd,
} from '@/lib/lectures';
import GroupsPageShell from '@/components/groups/GroupsPageShell.vue';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

const { t, locale } = useI18n();

const lectures = ref([]);
const loading = ref(true);
const loadFailed = ref(false);
const view = ref('upcoming');

// Re-evaluated every 30s so the Join button switches on without a reload.
const now = ref(new Date());
let ticker = null;

const visible = computed(() => {
  const list = lectures.value.filter((l) => (
    view.value === 'upcoming' ? lectureEnd(l) > now.value : lectureEnd(l) <= now.value
  ));
  const days = groupLecturesByDay(list);
  return view.value === 'past' ? days.reverse() : days;
});

async function load() {
  loading.value = true;
  loadFailed.value = false;
  try {
    lectures.value = await fetchMySchedule();
  } catch {
    loadFailed.value = true;
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  load();
  ticker = setInterval(() => {
    now.value = new Date();
  }, 30_000);
});

onBeforeUnmount(() => clearInterval(ticker));
</script>

<template>
  <GroupsPageShell
    :eyebrow="t('schedule.eyebrow')"
    :title="t('schedule.title')"
    :subtitle="t('schedule.subtitle')"
  >
    <div
      class="lf-page__segmented"
      role="tablist"
    >
      <button
        v-for="option in ['upcoming', 'past']"
        :key="option"
        type="button"
        role="tab"
        :aria-selected="view === option"
        :class="{ 'is-active': view === option }"
        @click="view = option"
      >
        {{ $t(`schedule.${option}`) }}
      </button>
    </div>

    <p
      v-if="loadFailed"
      class="lf-page__error"
      role="alert"
    >
      {{ $t('schedule.load_error') }}
    </p>
    <p
      v-else-if="loading"
      class="lf-page__hint"
    >
      {{ $t('courses.loading') }}
    </p>
    <p
      v-else-if="!visible.length"
      class="lf-page__hint"
    >
      {{ view === 'upcoming' ? $t('schedule.empty_upcoming') : $t('schedule.empty_past') }}
    </p>

    <div
      v-for="day in visible"
      :key="day.day"
      class="schedule-day"
    >
      <h2 class="schedule-day__title">
        {{ formatLectureDay(day.items[0].startsAt, locale) }}
      </h2>

      <ul class="schedule-day__items">
        <li
          v-for="lecture in day.items"
          :key="lecture.id"
          class="lf-page__card schedule-item"
          :class="{ 'is-cancelled': lecture.isCancelled }"
        >
          <div class="schedule-item__time">
            {{ formatLectureTime(lecture.startsAt, locale) }}
            <span class="lf-page__meta">{{ $t('schedule.duration', { minutes: lecture.durationMinutes }) }}</span>
          </div>

          <div class="schedule-item__body">
            <p class="lf-page__meta">
              {{ lecture.courseTitle }} · {{ lecture.groups.map((g) => g.name).join(', ') }}
            </p>
            <h3 class="schedule-item__title">
              <span class="schedule-item__title-text">{{ lecture.title }}</span>
              <Badge
                v-if="lecture.isCancelled"
                variant="coral"
              >
                {{ $t('schedule.cancelled') }}
              </Badge>
              <Badge
                v-else-if="lecture.isTeaching"
                variant="muted"
              >
                {{ $t('schedule.teaching') }}
              </Badge>
            </h3>
            <p
              v-if="lecture.description"
              class="schedule-item__description"
            >
              {{ lecture.description }}
            </p>
          </div>

          <div
            v-if="!lecture.isCancelled && view === 'upcoming'"
            class="schedule-item__join"
          >
            <Button
              v-if="canJoinLecture(lecture, now)"
              as-child
            >
              <a
                :href="lecture.meetingUrl"
                target="_blank"
                rel="noopener noreferrer"
              >
                <Video class="size-4" />
                {{ $t('schedule.join') }}
              </a>
            </Button>
            <span
              v-else
              class="lf-page__meta"
            >
              {{ lecture.meetingUrl ? $t('schedule.join_hint') : $t('schedule.no_link') }}
            </span>
          </div>
        </li>
      </ul>
    </div>
  </GroupsPageShell>
</template>

<style scoped>
.schedule-day {
  margin-top: 2rem;
}

.schedule-day__title {
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--color-ink);
  text-transform: capitalize;
}

.schedule-day__items {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin: 0.75rem 0 0;
  padding: 0;
  list-style: none;
}

.schedule-item {
  display: grid;
  grid-template-columns: 5rem 1fr auto;
  gap: 1rem;
  align-items: center;
}

.schedule-item.is-cancelled {
  opacity: 0.65;
}

.schedule-item.is-cancelled .schedule-item__title-text {
  text-decoration: line-through;
}

.schedule-item__time {
  display: flex;
  flex-direction: column;
  font-size: 1.1rem;
  font-weight: 700;
  color: var(--color-ink);
}

.schedule-item__body {
  min-width: 0;
}

.schedule-item__title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-top: 0.15rem;
  font-weight: 600;
  color: var(--color-ink);
}

.schedule-item__description {
  margin-top: 0.25rem;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  line-height: 1.5;
}

.schedule-item__join {
  max-width: 12rem;
  text-align: right;
}

@media (max-width: 640px) {
  .schedule-item {
    grid-template-columns: 1fr;
  }

  .schedule-item__join {
    max-width: none;
    text-align: left;
  }
}
</style>
