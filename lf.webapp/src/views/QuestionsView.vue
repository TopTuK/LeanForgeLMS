<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { ChevronDown, Inbox, MessagesSquare, Search, X } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/authStore';
import { useQuestionStore } from '@/stores/questionStore';
import {
  closeQuestion,
  fetchQuestionOverview,
  fetchQuestionThread,
  fetchQuestions,
  postQuestionMessage,
  reopenQuestion,
} from '@/services/questionService';
import QuestionInboxItem from '@/components/questions/inbox/QuestionInboxItem.vue';
import QuestionConversation from '@/components/questions/inbox/QuestionConversation.vue';

const PAGE_SIZE = 20;
const SEARCH_DEBOUNCE_MS = 300;
const STATUSES = ['Open', 'Answered', 'Closed'];

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const questionStore = useQuestionStore();
const { canViewTeachingCourses } = storeToRefs(authStore);

// The inbox the page shows. "staff" is only honoured for people who teach; everyone else always
// lands on their own questions.
const scope = ref(route.query?.scope === 'staff' ? 'staff' : 'student');

const overview = ref(null);
const questions = ref([]);
const totalCount = ref(0);
const page = ref(1);
// Starts true so the empty state can't flash before the first page arrives.
const loading = ref(true);
const loadingMore = ref(false);
const listError = ref('');

const status = ref('');
const courseId = ref('');
const searchInput = ref('');
const search = ref('');

const thread = ref(null);
const threadLoading = ref(false);
const threadError = ref('');
const submitting = ref(false);

const effectiveScope = computed(() => (canViewTeachingCourses.value ? scope.value : 'student'));
const hasMore = computed(() => questions.value.length < totalCount.value);
const hasFilters = computed(() => Boolean(status.value || courseId.value || search.value));
const inboxIsEmpty = computed(() => overview.value?.total === 0);
const selectedId = computed(() => thread.value?.id ?? (Number(route.params?.id) || null));

const statusFilters = computed(() => [
  { value: '', label: t('questions.inbox.filter_all'), count: overview.value?.total },
  ...STATUSES.map((value) => ({
    value,
    label: t(`questions.inbox.status.${value.toLowerCase()}`),
    count: overview.value?.[value.toLowerCase()],
  })),
]);

let listRequest = 0;

async function loadOverview() {
  try {
    overview.value = await fetchQuestionOverview(effectiveScope.value);
  } catch {
    // The counts decorate the filters; the list still works without them.
  }
}

async function loadList({ append = false } = {}) {
  // Rapid filter changes can resolve out of order; only the newest request may write the list.
  const request = ++listRequest;
  const nextPage = append ? page.value + 1 : 1;

  if (append) loadingMore.value = true;
  else loading.value = true;
  listError.value = '';

  try {
    const result = await fetchQuestions({
      scope: effectiveScope.value,
      status: status.value || null,
      courseId: courseId.value || null,
      search: search.value || null,
      page: nextPage,
      pageSize: PAGE_SIZE,
    });
    if (request !== listRequest) return;

    questions.value = append ? [...questions.value, ...result.items] : result.items;
    totalCount.value = result.totalCount;
    page.value = nextPage;
  } catch {
    if (request === listRequest) listError.value = t('questions.load_error');
  } finally {
    if (request === listRequest) {
      loading.value = false;
      loadingMore.value = false;
    }
  }
}

// Replaces a row with the thread's latest state and moves it to the top, matching the
// "most recent activity first" order the server uses.
function syncRow(updated, { moveToTop = false } = {}) {
  const index = questions.value.findIndex((q) => q.id === updated.id);
  if (index === -1) return;

  const summary = { ...updated };
  delete summary.messages;
  const next = [...questions.value];
  next.splice(index, 1);
  if (moveToTop) next.unshift(summary);
  else next.splice(index, 0, summary);
  questions.value = next;
}

async function openThread(id) {
  threadLoading.value = true;
  threadError.value = '';
  try {
    thread.value = await fetchQuestionThread(id);
    syncRow(thread.value);
    // Opening a thread clears its unread state server-side.
    questionStore.refreshUnreadCount();
    loadOverview();
  } catch {
    thread.value = null;
    threadError.value = t('questions.load_error');
  } finally {
    threadLoading.value = false;
  }
}

function select(question) {
  openThread(question.id);
  router.push({ name: 'Questions', params: { id: question.id }, query: scopeQuery() });
}

function closeThread() {
  thread.value = null;
  threadError.value = '';
  router.push({ name: 'Questions', query: scopeQuery() });
}

async function reply(body, onSuccess) {
  submitting.value = true;
  threadError.value = '';
  try {
    thread.value = await postQuestionMessage(thread.value.id, body);
    onSuccess();
    syncRow(thread.value, { moveToTop: true });
    loadOverview();
  } catch {
    threadError.value = t('questions.send_error');
  } finally {
    submitting.value = false;
  }
}

async function setResolved(resolved) {
  threadError.value = '';
  try {
    thread.value = resolved ? await closeQuestion(thread.value.id) : await reopenQuestion(thread.value.id);
    syncRow(thread.value);
    loadOverview();
  } catch {
    threadError.value = t('questions.status_error');
  }
}

function scopeQuery() {
  return effectiveScope.value === 'staff' ? { scope: 'staff' } : {};
}

function switchScope(next) {
  if (scope.value === next) return;
  scope.value = next;
  thread.value = null;
  status.value = '';
  courseId.value = '';
  searchInput.value = '';
  search.value = '';
  router.push({ name: 'Questions', query: scopeQuery() });
  loadOverview();
  loadList();
}

function clearFilters() {
  status.value = '';
  courseId.value = '';
  searchInput.value = '';
  search.value = '';
}

let searchTimer = null;
watch(searchInput, (value) => {
  clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    search.value = value.trim();
  }, SEARCH_DEBOUNCE_MS);
});

watch([status, courseId, search], () => loadList());

// Back/forward navigation between threads.
watch(
  () => route.params?.id,
  (id) => {
    const numericId = Number(id) || null;
    if (!numericId) thread.value = null;
    else if (numericId !== thread.value?.id) openThread(numericId);
  },
);

onMounted(() => {
  loadOverview();
  loadList();
  const initialId = Number(route.params?.id);
  if (initialId) openThread(initialId);
});

onBeforeUnmount(() => clearTimeout(searchTimer));
</script>

<template>
  <section class="qa-page">
    <header class="qa-page__header">
      <div>
        <h1 class="qa-page__title">
          {{ $t('questions.title') }}
        </h1>
        <p class="qa-page__subtitle">
          {{ effectiveScope === 'staff' ? $t('questions.inbox.subtitle_staff') : $t('questions.inbox.subtitle_student') }}
        </p>
      </div>

      <div
        v-if="canViewTeachingCourses"
        class="segmented"
        role="tablist"
        :aria-label="$t('questions.title')"
      >
        <button
          type="button"
          role="tab"
          class="segmented__option"
          :aria-selected="effectiveScope === 'student'"
          @click="switchScope('student')"
        >
          {{ $t('questions.tab_mine') }}
        </button>
        <button
          type="button"
          role="tab"
          class="segmented__option"
          :aria-selected="effectiveScope === 'staff'"
          @click="switchScope('staff')"
        >
          {{ $t('questions.tab_incoming') }}
        </button>
      </div>
    </header>

    <!-- Nobody has asked anything yet: one calm invitation instead of two empty panes. -->
    <div
      v-if="inboxIsEmpty && !hasFilters && !loading"
      class="qa-empty"
    >
      <span
        class="qa-empty__icon"
        aria-hidden="true"
      ><MessagesSquare :size="26" /></span>
      <h2 class="qa-empty__title">
        {{ effectiveScope === 'staff' ? $t('questions.inbox.empty_staff_title') : $t('questions.inbox.empty_title') }}
      </h2>
      <p class="qa-empty__hint">
        {{ effectiveScope === 'staff' ? $t('questions.inbox.empty_staff_hint') : $t('questions.inbox.empty_hint') }}
      </p>
      <RouterLink
        v-if="effectiveScope === 'student'"
        :to="{ name: 'CoursesActive' }"
        class="qa-empty__cta"
      >
        {{ $t('questions.inbox.empty_cta') }}
      </RouterLink>
    </div>

    <div
      v-else
      class="qa-inbox"
      :class="{ 'has-thread': selectedId }"
    >
      <aside class="qa-inbox__list-pane">
        <div class="qa-filters">
          <label class="qa-search">
            <Search
              :size="16"
              aria-hidden="true"
            />
            <span class="sr-only">{{ $t('questions.inbox.search_label') }}</span>
            <input
              v-model="searchInput"
              type="search"
              class="qa-search__input"
              :placeholder="$t('questions.inbox.search_placeholder')"
            >
            <button
              v-if="searchInput"
              type="button"
              class="qa-search__clear"
              :aria-label="$t('questions.inbox.clear_search')"
              @click="searchInput = ''"
            >
              <X :size="14" />
            </button>
          </label>

          <div
            class="qa-chips"
            role="group"
            :aria-label="$t('questions.inbox.filter_status')"
          >
            <button
              v-for="filter in statusFilters"
              :key="filter.value || 'all'"
              type="button"
              class="qa-chip"
              :aria-pressed="status === filter.value"
              @click="status = filter.value"
            >
              {{ filter.label }}
              <span
                v-if="filter.count !== undefined"
                class="qa-chip__count"
              >{{ filter.count }}</span>
            </button>
          </div>

          <div
            v-if="overview?.courses?.length > 1"
            class="qa-select"
          >
            <select
              v-model="courseId"
              class="qa-select__input"
              :aria-label="$t('questions.inbox.course_label')"
            >
              <option value="">
                {{ $t('questions.inbox.course_all') }}
              </option>
              <option
                v-for="course in overview.courses"
                :key="course.courseId"
                :value="course.courseId"
              >
                {{ course.courseTitle }} ({{ course.count }})
              </option>
            </select>
            <ChevronDown
              :size="15"
              class="qa-select__chevron"
              aria-hidden="true"
            />
          </div>
        </div>

        <p
          v-if="listError"
          class="qa-inbox__error"
          role="alert"
        >
          {{ listError }}
        </p>

        <div
          v-if="loading"
          class="qa-skeleton"
          aria-hidden="true"
        >
          <span
            v-for="n in 4"
            :key="n"
            class="qa-skeleton__row"
          />
        </div>
        <p
          v-if="loading"
          class="sr-only"
        >
          {{ $t('questions.loading') }}
        </p>

        <div
          v-else-if="!questions.length && !listError"
          class="qa-inbox__no-match"
        >
          <p>{{ $t('questions.inbox.no_match_title') }}</p>
          <button
            v-if="hasFilters"
            type="button"
            class="qa-link-button"
            @click="clearFilters"
          >
            {{ $t('questions.inbox.clear_filters') }}
          </button>
        </div>

        <ul
          v-else
          class="qa-inbox__list"
          :aria-label="$t('questions.title')"
        >
          <QuestionInboxItem
            v-for="question in questions"
            :key="question.id"
            :question="question"
            :selected="selectedId === question.id"
            @select="select"
          />
        </ul>

        <button
          v-if="hasMore && !loading"
          type="button"
          class="qa-load-more"
          :disabled="loadingMore"
          @click="loadList({ append: true })"
        >
          {{ loadingMore ? $t('questions.loading') : $t('questions.inbox.load_more') }}
        </button>
      </aside>

      <div class="qa-inbox__thread-pane">
        <p
          v-if="threadLoading && !thread"
          class="qa-placeholder"
        >
          {{ $t('questions.loading') }}
        </p>
        <QuestionConversation
          v-else-if="thread"
          :thread="thread"
          :submitting="submitting"
          :error-message="threadError"
          show-back
          @back="closeThread"
          @reply="reply"
          @close="setResolved(true)"
          @reopen="setResolved(false)"
        />
        <div
          v-else
          class="qa-placeholder"
        >
          <Inbox
            :size="28"
            aria-hidden="true"
          />
          <p class="qa-placeholder__title">
            {{ $t('questions.inbox.select_title') }}
          </p>
          <p>{{ threadError || (effectiveScope === 'staff' ? $t('questions.inbox.select_hint_staff') : $t('questions.inbox.select_hint')) }}</p>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.qa-page {
  --qa-fill: color-mix(in srgb, var(--color-ink) 6%, transparent);

  /* Fill the layout column: without it the page shrinks to its content and jumps wider when a
     conversation opens. */
  width: 100%;
  max-width: var(--layout-max);
  margin: 0 auto;
  padding: clamp(1.25rem, 4vw, 2.5rem) 1rem;
}

.qa-page__header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.qa-page__title {
  font-family: var(--font-display);
  font-size: clamp(1.6rem, 3.5vw, 2.2rem);
  font-weight: 600;
  letter-spacing: -0.02em;
  color: var(--color-ink);
}

.qa-page__subtitle {
  margin-top: 0.35rem;
  max-width: 38rem;
  color: var(--color-ink-muted);
}

/* Segmented control */
.segmented {
  display: inline-flex;
  gap: 0.25rem;
  border-radius: var(--radius-pill);
  background: var(--qa-fill);
  padding: 0.25rem;
}

.segmented__option {
  border: 0;
  border-radius: var(--radius-pill);
  background: transparent;
  padding: 0.4rem 1rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-ink-muted);
  cursor: pointer;
  transition: background-color 0.15s ease, color 0.15s ease;
}

.segmented__option[aria-selected='true'] {
  background: var(--color-card);
  color: var(--color-ink);
  box-shadow: 0 1px 2px color-mix(in srgb, var(--color-ink) 12%, transparent);
}

/* Two-pane inbox */
.qa-inbox {
  display: grid;
  grid-template-columns: minmax(20rem, 26rem) minmax(0, 1fr);
  height: max(34rem, calc(100dvh - var(--header-height) - 12rem));
  overflow: hidden;
  border: 1px solid var(--color-border-subtle);
  border-radius: calc(var(--radius-card) + 0.25rem);
  background: var(--color-card);
}

.qa-inbox__list-pane {
  display: flex;
  min-width: 0;
  min-height: 0;
  flex-direction: column;
  border-right: 1px solid var(--color-border-subtle);
}

.qa-inbox__thread-pane {
  display: flex;
  min-height: 0;
  min-width: 0;
  flex-direction: column;
}

.qa-filters {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  border-bottom: 1px solid var(--color-border-subtle);
  padding: 1rem;
}

.qa-search {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  border-radius: var(--radius-pill);
  background: var(--qa-fill);
  padding: 0 0.85rem;
  color: var(--color-ink-faint);
  transition: box-shadow 0.15s ease;
}

.qa-search:focus-within {
  box-shadow: 0 0 0 2px var(--color-accent-coral);
}

.qa-search__input {
  flex: 1;
  min-width: 0;
  border: 0;
  background: transparent;
  padding: 0.55rem 0;
  font: inherit;
  font-size: 0.875rem;
  color: var(--color-ink);
  outline: none;
}

.qa-search__input::-webkit-search-cancel-button {
  display: none;
}

.qa-search__clear {
  display: grid;
  place-items: center;
  border: 0;
  border-radius: 999px;
  background: transparent;
  padding: 0.2rem;
  color: var(--color-ink-muted);
  cursor: pointer;
}

/* One quiet row; on narrow panes it scrolls sideways instead of wrapping into a second line. */
.qa-chips {
  display: flex;
  gap: 0.35rem;
  margin: 0 -1rem;
  overflow-x: auto;
  padding: 0 1rem;
  scrollbar-width: none;
}

.qa-chips::-webkit-scrollbar {
  display: none;
}

.qa-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-pill);
  background: transparent;
  flex: none;
  padding: 0.25rem 0.7rem;
  font-size: 0.78rem;
  white-space: nowrap;
  font-weight: 500;
  color: var(--color-ink-muted);
  cursor: pointer;
  transition: all 0.15s ease;
}

.qa-chip:hover {
  color: var(--color-ink);
}

.qa-chip[aria-pressed='true'] {
  border-color: var(--color-ink);
  background: var(--color-ink);
  color: var(--color-surface-950);
}

.qa-chip__count {
  font-variant-numeric: tabular-nums;
  opacity: 0.65;
}

.qa-select {
  position: relative;
  color: var(--color-ink-muted);
}

.qa-select__input {
  width: 100%;
  appearance: none;
  border: 0;
  border-radius: var(--radius-pill);
  background: var(--qa-fill);
  padding: 0.5rem 2.25rem 0.5rem 0.95rem;
  font: inherit;
  font-size: 0.82rem;
  color: var(--color-ink);
  cursor: pointer;
  outline: none;
}

.qa-select__input:focus-visible {
  box-shadow: 0 0 0 2px var(--color-accent-coral);
}

.qa-select__chevron {
  position: absolute;
  top: 50%;
  right: 0.85rem;
  transform: translateY(-50%);
  pointer-events: none;
}

.qa-inbox__list {
  display: flex;
  flex: 1;
  min-height: 0;
  flex-direction: column;
  gap: 0.15rem;
  margin: 0;
  overflow-y: auto;
  padding: 0.5rem;
  list-style: none;
}

.qa-inbox__error {
  margin: 0.75rem 1rem 0;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-accent-coral);
}

.qa-inbox__no-match {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 2.5rem 1rem;
  text-align: center;
  font-size: 0.9rem;
  color: var(--color-ink-muted);
}

.qa-link-button,
.qa-load-more {
  border: 0;
  background: transparent;
  font: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-accent-coral);
  cursor: pointer;
}

.qa-load-more {
  margin: 0.25rem 0.5rem 0.75rem;
  border-radius: var(--radius-md);
  padding: 0.55rem;
}

.qa-load-more:hover:not(:disabled) {
  background: var(--qa-fill);
}

.qa-skeleton {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 1rem;
}

.qa-skeleton__row {
  height: 4.5rem;
  border-radius: var(--radius-card);
  background: linear-gradient(90deg, var(--color-surface-900), var(--color-surface-800), var(--color-surface-900));
  background-size: 200% 100%;
  animation: qa-shimmer 1.4s ease-in-out infinite;
}

@keyframes qa-shimmer {
  from { background-position: 200% 0; }
  to { background-position: -200% 0; }
}

@media (prefers-reduced-motion: reduce) {
  .qa-skeleton__row {
    animation: none;
  }
}

.qa-placeholder {
  display: flex;
  flex: 1;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  padding: 2rem;
  text-align: center;
  font-size: 0.9rem;
  color: var(--color-ink-faint);
}

.qa-placeholder__title {
  margin-top: 0.4rem;
  font-weight: 600;
  color: var(--color-ink-muted);
}

/* Whole-inbox empty state */
.qa-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.6rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: calc(var(--radius-card) + 0.25rem);
  padding: clamp(2.5rem, 8vw, 5rem) 1.5rem;
  text-align: center;
}

.qa-empty__icon {
  display: grid;
  width: 3.5rem;
  height: 3.5rem;
  place-items: center;
  border-radius: 999px;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
}

.qa-empty__title {
  margin-top: 0.5rem;
  font-family: var(--font-display);
  font-size: 1.2rem;
  font-weight: 600;
  color: var(--color-ink);
}

.qa-empty__hint {
  max-width: 28rem;
  color: var(--color-ink-muted);
}

.qa-empty__cta {
  margin-top: 0.75rem;
  border-radius: var(--radius-pill);
  background: var(--color-ink);
  padding: 0.6rem 1.25rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-surface-950);
  text-decoration: none;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

/* Below tablet width the inbox shows one pane at a time: the list, or the open conversation. */
@media (max-width: 56rem) {
  .qa-inbox {
    /* minmax(0, …): a bare 1fr can't shrink below its content, and the one-line chip row would
       push the pane past the viewport. */
    grid-template-columns: minmax(0, 1fr);
    height: calc(100dvh - var(--header-height) - 9rem);
    min-height: 30rem;
  }

  .qa-inbox__list-pane {
    border-right: 0;
  }

  .qa-inbox .qa-inbox__thread-pane,
  .qa-inbox.has-thread .qa-inbox__list-pane {
    display: none;
  }

  .qa-inbox.has-thread .qa-inbox__thread-pane {
    display: flex;
  }
}
</style>
