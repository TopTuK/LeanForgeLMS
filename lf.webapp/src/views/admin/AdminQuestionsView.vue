<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  deleteAdminQuestion,
  deleteAdminQuestionMessage,
  fetchAdminQuestionThread,
  fetchAdminQuestions,
  postAdminQuestionMessage,
} from '@/services/adminService';
import QuestionThread from '@/components/questions/QuestionThread.vue';
import QuestionStatusBadge from '@/components/questions/QuestionStatusBadge.vue';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog } from '@/components/ui/dialog';
import { formatQuestionTimestamp } from '@/lib/questions';

const PAGE_SIZE = 20;
const STATUSES = ['Open', 'Answered', 'Closed'];

const { t, locale } = useI18n();

const questions = ref([]);
const totalCount = ref(0);
const page = ref(1);
const search = ref('');
const status = ref('');
// Starts true so the empty state can't flash before the first page arrives.
const loading = ref(true);
const errorMessage = ref('');

const thread = ref(null);
const threadError = ref('');
const submitting = ref(false);

const deleteThreadShown = ref(false);
const deleteMessageTarget = ref(null);

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / PAGE_SIZE)));

async function loadQuestions() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchAdminQuestions({
      status: status.value || null,
      search: search.value || null,
      page: page.value,
      pageSize: PAGE_SIZE,
    });
    questions.value = result.items;
    totalCount.value = result.totalCount;
  } catch {
    errorMessage.value = t('admin.questions.load_error');
  } finally {
    loading.value = false;
  }
}

async function openThread(question) {
  threadError.value = '';
  try {
    thread.value = await fetchAdminQuestionThread(question.id);
  } catch {
    threadError.value = t('admin.questions.load_error');
  }
}

async function reply(body, onSuccess) {
  submitting.value = true;
  threadError.value = '';
  try {
    thread.value = await postAdminQuestionMessage(thread.value.id, body);
    onSuccess();
    await loadQuestions();
  } catch {
    threadError.value = t('admin.questions.send_error');
  } finally {
    submitting.value = false;
  }
}

async function confirmDeleteMessage() {
  const target = deleteMessageTarget.value;
  deleteMessageTarget.value = null;
  try {
    thread.value = await deleteAdminQuestionMessage(thread.value.id, target.id);
  } catch {
    threadError.value = t('admin.questions.delete_message_error');
  }
}

async function confirmDeleteThread() {
  deleteThreadShown.value = false;
  try {
    await deleteAdminQuestion(thread.value.id);
    thread.value = null;

    // Deleting the only row on a later page would strand the user on an empty page.
    if (questions.value.length === 1 && page.value > 1) page.value -= 1;
    else await loadQuestions();
  } catch {
    errorMessage.value = t('admin.questions.delete_error');
  }
}

function applyFilters() {
  page.value = 1;
  loadQuestions();
}

watch(page, loadQuestions);
onMounted(loadQuestions);
</script>

<template>
  <div class="admin-questions">
    <div>
      <h1 class="font-display text-2xl font-semibold tracking-tight text-ink">
        {{ $t('admin.questions.title') }}
      </h1>
      <p class="mt-1 text-sm text-ink-muted">
        {{ $t('admin.questions.subtitle') }}
      </p>
    </div>

    <form
      class="mt-4 flex flex-wrap items-center gap-2"
      @submit.prevent="applyFilters"
    >
      <Input
        v-model="search"
        class="max-w-xs"
        :placeholder="$t('admin.questions.search_placeholder')"
        :aria-label="$t('admin.questions.search_placeholder')"
      />
      <select
        v-model="status"
        class="h-9 rounded-md border border-border-subtle bg-card px-3 text-sm text-ink"
        :aria-label="$t('admin.questions.filter_status')"
      >
        <option value="">
          {{ $t('admin.questions.status_any') }}
        </option>
        <option
          v-for="value in STATUSES"
          :key="value"
          :value="value"
        >
          {{ $t(`questions.status.${value.toLowerCase()}`) }}
        </option>
      </select>
      <Button type="submit">
        {{ $t('admin.questions.apply_filters') }}
      </Button>
    </form>

    <p
      v-if="errorMessage"
      class="mt-4 rounded-md border border-accent-coral bg-accent-soft px-3 py-2 text-sm font-semibold text-accent-coral"
      role="alert"
    >
      {{ errorMessage }}
    </p>

    <div class="mt-4 overflow-x-auto rounded-lg border border-border-subtle bg-card">
      <table class="w-full text-left text-sm">
        <thead class="border-b border-border-subtle text-ink-muted">
          <tr>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.questions.col_question') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.questions.col_course') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.questions.col_student') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.questions.col_status') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.questions.col_last_message') }}
            </th>
            <th class="px-3 py-2" />
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="6"
            >
              {{ $t('questions.loading') }}
            </td>
          </tr>
          <tr v-else-if="!questions.length">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="6"
            >
              {{ $t('admin.questions.empty') }}
            </td>
          </tr>
          <tr
            v-for="question in questions"
            v-else
            :key="question.id"
            class="border-t border-border-subtle"
          >
            <td class="px-3 py-2 text-ink">
              {{ question.title }}
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ question.courseTitle }} / {{ question.lessonTitle }}
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ question.studentName }}
            </td>
            <td class="px-3 py-2">
              <QuestionStatusBadge :status="question.status" />
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ formatQuestionTimestamp(question.lastMessageAt, locale) }}
            </td>
            <td class="px-3 py-2">
              <div class="flex justify-end">
                <Button
                  variant="outline"
                  size="sm"
                  @click="openThread(question)"
                >
                  {{ $t('admin.questions.open') }}
                </Button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div
      v-if="totalPages > 1"
      class="mt-3 flex items-center justify-between gap-2"
    >
      <Button
        variant="outline"
        size="sm"
        :disabled="page <= 1"
        @click="page -= 1"
      >
        {{ $t('questions.previous') }}
      </Button>
      <span class="text-sm text-ink-muted">{{ page }} / {{ totalPages }}</span>
      <Button
        variant="outline"
        size="sm"
        :disabled="page >= totalPages"
        @click="page += 1"
      >
        {{ $t('questions.next') }}
      </Button>
    </div>

    <div
      v-if="thread"
      class="mt-6"
    >
      <QuestionThread
        :thread="thread"
        can-delete
        :can-change-status="false"
        :submitting="submitting"
        :error-message="threadError"
        @reply="reply"
        @delete-message="deleteMessageTarget = $event"
      />

      <div class="mt-3 flex justify-end">
        <Button
          variant="destructive"
          size="sm"
          @click="deleteThreadShown = true"
        >
          {{ $t('admin.questions.delete_thread') }}
        </Button>
      </div>
    </div>

    <Dialog
      :open="deleteMessageTarget !== null"
      :title="$t('admin.questions.delete_message_title')"
      :description="$t('admin.questions.delete_message_confirm')"
      :confirm-label="$t('admin.questions.delete')"
      :cancel-label="$t('questions.cancel')"
      danger
      @update:open="(value) => { if (!value) deleteMessageTarget = null; }"
      @confirm="confirmDeleteMessage"
      @cancel="deleteMessageTarget = null"
    />

    <Dialog
      v-model:open="deleteThreadShown"
      :title="$t('admin.questions.delete_thread_title')"
      :description="$t('admin.questions.delete_thread_confirm', { title: thread?.title ?? '' })"
      :confirm-label="$t('admin.questions.delete')"
      :cancel-label="$t('questions.cancel')"
      danger
      @confirm="confirmDeleteThread"
    />
  </div>
</template>
