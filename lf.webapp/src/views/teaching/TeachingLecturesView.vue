<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Ban, Pencil, Plus, Trash2 } from 'lucide-vue-next';
import { fetchCourseGroups } from '@/services/groupService';
import {
  cancelLecture,
  fetchCourseLectures,
  removeLecture,
  scheduleLecture,
  updateLecture,
} from '@/services/lectureService';
import {
  formatLectureDateTime,
  fromLocalInputValue,
  lectureEnd,
  toLocalInputValue,
} from '@/lib/lectures';
import GroupsPageShell from '@/components/groups/GroupsPageShell.vue';
import TeachingCourseTabs from '@/components/groups/TeachingCourseTabs.vue';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Dialog } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';

const DEFAULT_DURATION = 90;

const route = useRoute();
const { t, locale } = useI18n();

const courseId = computed(() => Number(route.params.courseId));

const lectures = ref([]);
const groups = ref([]);
const loading = ref(true);
const errorMessage = ref('');
const view = ref('upcoming');

const formOpen = ref(false);
const editing = ref(null);
const form = ref(emptyForm());
const formError = ref('');
const saving = ref(false);

const confirmAction = ref(null);

const visible = computed(() => {
  const now = new Date();
  const list = lectures.value.filter((l) => (view.value === 'upcoming' ? lectureEnd(l) > now : lectureEnd(l) <= now));
  return view.value === 'past' ? [...list].reverse() : list;
});

function emptyForm() {
  // Defaults to tomorrow at 10:00 local time, a sensible starting point for the picker.
  const start = new Date();
  start.setDate(start.getDate() + 1);
  start.setHours(10, 0, 0, 0);
  return {
    title: '',
    description: '',
    startsAt: toLocalInputValue(start),
    durationMinutes: DEFAULT_DURATION,
    meetingUrl: '',
    groupIds: [],
  };
}

async function load() {
  loading.value = true;
  errorMessage.value = '';
  try {
    [lectures.value, groups.value] = await Promise.all([
      fetchCourseLectures(courseId.value),
      fetchCourseGroups(courseId.value),
    ]);
  } catch {
    errorMessage.value = t('teaching.lectures.load_error');
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  editing.value = null;
  form.value = emptyForm();
  if (groups.value.length === 1) form.value.groupIds = [groups.value[0].id];
  formError.value = '';
  formOpen.value = true;
}

function openEdit(lecture) {
  editing.value = lecture;
  form.value = {
    title: lecture.title,
    description: lecture.description ?? '',
    startsAt: toLocalInputValue(lecture.startsAt),
    durationMinutes: lecture.durationMinutes,
    meetingUrl: lecture.meetingUrl ?? '',
    groupIds: lecture.groups.map((g) => g.id),
  };
  formError.value = '';
  formOpen.value = true;
}

async function save() {
  if (saving.value) return;
  if (!form.value.groupIds.length) {
    formError.value = t('teaching.lectures.groups_required');
    return;
  }

  saving.value = true;
  formError.value = '';
  const payload = {
    title: form.value.title.trim(),
    description: form.value.description.trim() || null,
    startsAt: fromLocalInputValue(form.value.startsAt),
    durationMinutes: Number(form.value.durationMinutes),
    meetingUrl: form.value.meetingUrl.trim() || null,
    groupIds: form.value.groupIds,
  };

  try {
    if (editing.value) {
      await updateLecture(editing.value.id, payload);
    } else {
      await scheduleLecture(courseId.value, payload);
    }
    formOpen.value = false;
    await load();
  } catch (err) {
    const errors = err?.response?.data?.errors;
    formError.value = errors ? Object.values(errors).flat().join(' ') : t('teaching.lectures.save_error');
  } finally {
    saving.value = false;
  }
}

function askCancel(lecture) {
  confirmAction.value = {
    kind: 'cancel',
    lecture,
    title: t('teaching.lectures.cancel_confirm_title'),
    body: t('teaching.lectures.cancel_confirm_body', { title: lecture.title }),
    confirm: t('teaching.lectures.cancel_lecture'),
  };
}

function askDelete(lecture) {
  confirmAction.value = {
    kind: 'delete',
    lecture,
    title: t('teaching.lectures.delete_confirm_title'),
    body: t('teaching.lectures.delete_confirm_body', { title: lecture.title }),
    confirm: t('teaching.lectures.delete'),
  };
}

async function runConfirmed() {
  const action = confirmAction.value;
  confirmAction.value = null;
  try {
    if (action.kind === 'cancel') await cancelLecture(action.lecture.id);
    else await removeLecture(action.lecture.id);
    await load();
  } catch {
    errorMessage.value = t('teaching.lectures.save_error');
  }
}

const confirmOpen = computed({
  get: () => confirmAction.value !== null,
  set: (open) => {
    if (!open) confirmAction.value = null;
  },
});

onMounted(load);
</script>

<template>
  <GroupsPageShell
    :title="t('teaching.lectures.title')"
    :subtitle="t('teaching.lectures.subtitle')"
    wide
  >
    <template #before-heading>
      <TeachingCourseTabs :course-id="courseId" />
    </template>

    <template #actions>
      <Button
        :disabled="!groups.length"
        @click="openCreate"
      >
        <Plus class="size-4" />
        {{ $t('teaching.lectures.schedule') }}
      </Button>
    </template>

    <p
      v-if="errorMessage"
      class="lf-page__error"
      role="alert"
    >
      {{ errorMessage }}
    </p>
    <p
      v-if="!loading && !groups.length"
      class="lf-page__hint"
    >
      {{ $t('teaching.lectures.no_groups_hint') }}
    </p>

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
        {{ $t(`teaching.lectures.${option}`) }}
      </button>
    </div>

    <p
      v-if="loading"
      class="lf-page__hint"
    >
      {{ $t('courses.loading') }}
    </p>
    <p
      v-else-if="!visible.length"
      class="lf-page__hint"
    >
      {{ $t('teaching.lectures.empty') }}
    </p>

    <ul
      v-else
      class="lf-page__list"
    >
      <li
        v-for="lecture in visible"
        :key="lecture.id"
        class="lf-page__card teaching-lecture"
        :class="{ 'is-cancelled': lecture.isCancelled }"
      >
        <div class="teaching-lecture__body">
          <p class="lf-page__meta">
            {{ formatLectureDateTime(lecture.startsAt, locale) }}
            · {{ $t('schedule.duration', { minutes: lecture.durationMinutes }) }}
          </p>
          <h2 class="teaching-lecture__title">
            {{ lecture.title }}
            <Badge
              v-if="lecture.isCancelled"
              variant="coral"
            >
              {{ $t('teaching.lectures.cancelled') }}
            </Badge>
          </h2>
          <p class="lf-page__meta">
            {{ lecture.groups.map((g) => g.name).join(', ') }}
          </p>
          <a
            v-if="lecture.meetingUrl"
            :href="lecture.meetingUrl"
            target="_blank"
            rel="noopener noreferrer"
            class="teaching-lecture__link"
          >{{ lecture.meetingUrl }}</a>
        </div>

        <div
          v-if="!lecture.isCancelled"
          class="lf-page__actions"
        >
          <Button
            variant="ghost"
            size="sm"
            :aria-label="$t('teaching.lectures.edit')"
            @click="openEdit(lecture)"
          >
            <Pencil class="size-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            :aria-label="$t('teaching.lectures.cancel_lecture')"
            @click="askCancel(lecture)"
          >
            <Ban class="size-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            :aria-label="$t('teaching.lectures.delete')"
            @click="askDelete(lecture)"
          >
            <Trash2 class="size-4" />
          </Button>
        </div>
        <Button
          v-else
          variant="ghost"
          size="sm"
          :aria-label="$t('teaching.lectures.delete')"
          @click="askDelete(lecture)"
        >
          <Trash2 class="size-4" />
        </Button>
      </li>
    </ul>

    <Dialog
      v-model:open="formOpen"
      :title="editing ? $t('teaching.lectures.edit') : $t('teaching.lectures.schedule')"
      :confirm-label="$t('teaching.lectures.save')"
      :cancel-label="$t('teaching.lectures.cancel')"
      @confirm="save"
    >
      <form @submit.prevent="save">
        <p
          v-if="formError"
          class="lf-page__error"
          role="alert"
        >
          {{ formError }}
        </p>
        <div class="lf-page__field">
          <label for="lecture-title">{{ $t('teaching.lectures.title_label') }}</label>
          <Input
            id="lecture-title"
            v-model="form.title"
            maxlength="200"
          />
        </div>
        <div class="teaching-lecture__row">
          <div class="lf-page__field">
            <label for="lecture-start">{{ $t('teaching.lectures.starts_at_label') }}</label>
            <Input
              id="lecture-start"
              v-model="form.startsAt"
              type="datetime-local"
            />
          </div>
          <div class="lf-page__field">
            <label for="lecture-duration">{{ $t('teaching.lectures.duration_label') }}</label>
            <Input
              id="lecture-duration"
              v-model="form.durationMinutes"
              type="number"
              min="5"
              max="600"
              step="5"
            />
          </div>
        </div>
        <div class="lf-page__field">
          <label for="lecture-url">{{ $t('teaching.lectures.meeting_url_label') }}</label>
          <Input
            id="lecture-url"
            v-model="form.meetingUrl"
            type="url"
            :placeholder="$t('teaching.lectures.meeting_url_placeholder')"
          />
        </div>
        <fieldset class="lf-page__field">
          <legend>{{ $t('teaching.lectures.groups_label') }}</legend>
          <label
            v-for="group in groups"
            :key="group.id"
            class="teaching-lecture__group"
          >
            <input
              v-model="form.groupIds"
              type="checkbox"
              :value="group.id"
            >
            {{ group.name }}
          </label>
        </fieldset>
        <div class="lf-page__field">
          <label for="lecture-description">{{ $t('teaching.lectures.description_label') }}</label>
          <Textarea
            id="lecture-description"
            v-model="form.description"
            :rows="3"
            maxlength="2000"
          />
        </div>
      </form>
    </Dialog>

    <Dialog
      v-model:open="confirmOpen"
      :title="confirmAction?.title ?? ''"
      :description="confirmAction?.body ?? ''"
      :confirm-label="confirmAction?.confirm ?? ''"
      :cancel-label="$t('teaching.lectures.cancel')"
      danger
      @confirm="runConfirmed"
    />
  </GroupsPageShell>
</template>

<style scoped>
.teaching-lecture {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.teaching-lecture.is-cancelled {
  opacity: 0.65;
}

.teaching-lecture__body {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  min-width: 0;
}

.teaching-lecture__title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1.05rem;
  font-weight: 600;
  color: var(--color-ink);
}

.teaching-lecture__link {
  font-size: 0.85rem;
  color: var(--color-accent-coral);
  overflow-wrap: anywhere;
}

.teaching-lecture__row {
  display: grid;
  grid-template-columns: 1fr 10rem;
  gap: 0.75rem;
}

.teaching-lecture__group {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  font-weight: 400;
  color: var(--color-ink);
}

fieldset.lf-page__field {
  border: 0;
  padding: 0;
}
</style>
