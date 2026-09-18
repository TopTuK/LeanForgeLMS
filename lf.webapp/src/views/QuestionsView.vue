<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores/authStore';
import { useQuestionStore } from '@/stores/questionStore';
import {
  closeQuestion,
  fetchQuestionThread,
  fetchQuestions,
  postQuestionMessage,
  reopenQuestion,
} from '@/services/questionService';
import QuestionListItem from '@/components/questions/QuestionListItem.vue';
import QuestionThread from '@/components/questions/QuestionThread.vue';
import GeometricBackdrop from '@/components/layout/GeometricBackdrop.vue';
import { Button } from '@/components/ui/button';

const PAGE_SIZE = 20;

const { t } = useI18n();
const authStore = useAuthStore();
const questionStore = useQuestionStore();
const { canViewTeachingCourses } = storeToRefs(authStore);

const scope = ref('student');
const questions = ref([]);
const totalCount = ref(0);
const page = ref(1);
const loading = ref(true);
const errorMessage = ref('');

const thread = ref(null);
const threadLoading = ref(false);
const threadError = ref('');
const submitting = ref(false);

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / PAGE_SIZE)));

async function loadQuestions() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchQuestions({ scope: scope.value, page: page.value, pageSize: PAGE_SIZE });
    questions.value = result.items;
    totalCount.value = result.totalCount;

    // Keep the open thread in step with the list; drop it if it is no longer in this scope.
    if (thread.value && !result.items.some((q) => q.id === thread.value.id)) {
      thread.value = null;
    }
  } catch {
    errorMessage.value = t('questions.load_error');
  } finally {
    loading.value = false;
  }
}

async function openThread(question) {
  threadLoading.value = true;
  threadError.value = '';
  try {
    thread.value = await fetchQuestionThread(question.id);
    // Opening a thread clears its badge server-side, so both counters are refreshed.
    await Promise.all([questionStore.refreshUnreadCount(), loadQuestions()]);
  } catch {
    threadError.value = t('questions.load_error');
  } finally {
    threadLoading.value = false;
  }
}

async function reply(body, onSuccess) {
  submitting.value = true;
  threadError.value = '';
  try {
    thread.value = await postQuestionMessage(thread.value.id, body);
    onSuccess();
    await loadQuestions();
  } catch {
    threadError.value = t('questions.send_error');
  } finally {
    submitting.value = false;
  }
}

async function setStatus(close) {
  threadError.value = '';
  try {
    thread.value = close ? await closeQuestion(thread.value.id) : await reopenQuestion(thread.value.id);
    await loadQuestions();
  } catch {
    threadError.value = t('questions.status_error');
  }
}

function switchScope(next) {
  if (scope.value === next) return;
  scope.value = next;
  page.value = 1;
  thread.value = null;
}

watch([scope, page], loadQuestions);
onMounted(loadQuestions);
</script>

<template>
  <section class="questions-page">
    <GeometricBackdrop dense />

    <div class="questions-page__inner">
      <p class="mono-label questions-page__eyebrow">
        {{ $t('questions.eyebrow') }}
      </p>
      <h1 class="questions-page__title font-display">
        {{ $t('questions.title') }}
      </h1>
      <p class="questions-page__subtitle">
        {{ $t('questions.subtitle') }}
      </p>

      <div
        v-if="canViewTeachingCourses"
        class="questions-page__tabs"
        role="tablist"
      >
        <button
          type="button"
          role="tab"
          class="questions-page__tab"
          :class="{ 'is-active': scope === 'student' }"
          :aria-selected="scope === 'student'"
          @click="switchScope('student')"
        >
          {{ $t('questions.tab_mine') }}
        </button>
        <button
          type="button"
          role="tab"
          class="questions-page__tab"
          :class="{ 'is-active': scope === 'staff' }"
          :aria-selected="scope === 'staff'"
          @click="switchScope('staff')"
        >
          {{ $t('questions.tab_incoming') }}
        </button>
      </div>

      <p
        v-if="errorMessage"
        class="questions-page__error"
        role="alert"
      >
        {{ errorMessage }}
      </p>

      <div class="questions-page__body">
        <div class="questions-page__list-column">
          <p
            v-if="loading"
            class="questions-page__hint"
          >
            {{ $t('questions.loading') }}
          </p>
          <p
            v-else-if="!questions.length"
            class="questions-page__hint"
          >
            {{ scope === 'staff' ? $t('questions.empty_incoming') : $t('questions.empty_mine') }}
          </p>

          <ul
            v-else
            class="questions-page__list"
          >
            <QuestionListItem
              v-for="question in questions"
              :key="question.id"
              :question="question"
              :selected="thread?.id === question.id"
              @select="openThread"
            />
          </ul>

          <div
            v-if="totalPages > 1"
            class="questions-page__pager"
          >
            <Button
              variant="outline"
              size="sm"
              :disabled="page <= 1"
              @click="page -= 1"
            >
              {{ $t('questions.previous') }}
            </Button>
            <span class="questions-page__hint">{{ page }} / {{ totalPages }}</span>
            <Button
              variant="outline"
              size="sm"
              :disabled="page >= totalPages"
              @click="page += 1"
            >
              {{ $t('questions.next') }}
            </Button>
          </div>
        </div>

        <div class="questions-page__thread-column">
          <p
            v-if="threadLoading"
            class="questions-page__hint"
          >
            {{ $t('questions.loading') }}
          </p>
          <QuestionThread
            v-else-if="thread"
            :thread="thread"
            :submitting="submitting"
            :error-message="threadError"
            @reply="reply"
            @close="setStatus(true)"
            @reopen="setStatus(false)"
          />
          <p
            v-else
            class="questions-page__hint"
          >
            {{ $t('questions.select_hint') }}
          </p>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.questions-page {
  position: relative;
  padding: clamp(1.5rem, 5vw, 3rem) 1rem;
}

.questions-page__inner {
  position: relative;
  max-width: var(--layout-max);
  margin: 0 auto;
}

.questions-page__eyebrow {
  color: var(--color-accent-coral);
}

.questions-page__title {
  margin-top: 0.5rem;
  font-size: clamp(1.75rem, 4vw, 2.5rem);
  font-weight: 600;
  letter-spacing: -0.02em;
  color: var(--color-ink);
}

.questions-page__subtitle {
  margin-top: 0.5rem;
  color: var(--color-ink-muted);
}

.questions-page__tabs {
  display: flex;
  gap: 0.5rem;
  margin-top: 1.5rem;
}

.questions-page__tab {
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-pill);
  background: var(--color-card);
  padding: 0.4rem 1rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-ink-muted);
  cursor: pointer;
}

.questions-page__tab.is-active {
  border-color: var(--color-accent-coral);
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
}

.questions-page__body {
  display: grid;
  gap: 1.25rem;
  margin-top: 1.5rem;
  grid-template-columns: 1fr;
}

/* Two columns once there is room for a list beside a readable thread. */
@media (min-width: 60rem) {
  .questions-page__body {
    grid-template-columns: minmax(18rem, 22rem) 1fr;
    align-items: start;
  }
}

.questions-page__list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.questions-page__pager {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-top: 0.75rem;
}

.questions-page__hint {
  font-size: 0.9rem;
  color: var(--color-ink-muted);
}

.questions-page__error {
  margin-top: 1rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: var(--radius-card);
  background: var(--color-accent-soft);
  padding: 0.5rem 0.75rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-accent-coral);
}
</style>
