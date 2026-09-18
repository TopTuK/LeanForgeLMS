<script setup>
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Trash2 } from 'lucide-vue-next';
import { assignCourseInstructor, fetchCourseInstructors, removeCourseInstructor } from '@/services/questionService';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

const props = defineProps({
  courseId: { type: Number, required: true },
});

const { t } = useI18n();

// The API returns PascalCase role names; profile.roles is keyed in snake_case.
const ROLE_KEYS = {
  Student: 'student',
  Instructor: 'instructor',
  CourseCreator: 'course_creator',
  Admin: 'admin',
  None: 'none',
};

const roleLabel = (role) => t(`profile.roles.${ROLE_KEYS[role] ?? 'none'}`);

const team = ref([]);
const email = ref('');
const loading = ref(true);
const submitting = ref(false);
const errorMessage = ref('');

async function load() {
  loading.value = true;
  errorMessage.value = '';
  try {
    team.value = await fetchCourseInstructors(props.courseId);
  } catch {
    errorMessage.value = t('courses.teaching_team.load_error');
  } finally {
    loading.value = false;
  }
}

async function assign() {
  if (!email.value.trim() || submitting.value) return;

  submitting.value = true;
  errorMessage.value = '';
  try {
    team.value = await assignCourseInstructor(props.courseId, email.value.trim());
    email.value = '';
  } catch (err) {
    // The API answers 409 with a specific reason (unknown user, wrong role, already assigned),
    // which is more useful to the author than a generic failure message.
    errorMessage.value = err?.response?.status === 409 && typeof err.response.data === 'string'
      ? err.response.data
      : t('courses.teaching_team.assign_error');
  } finally {
    submitting.value = false;
  }
}

async function remove(member) {
  errorMessage.value = '';
  try {
    team.value = await removeCourseInstructor(props.courseId, member.userId);
  } catch {
    errorMessage.value = t('courses.teaching_team.remove_error');
  }
}

onMounted(load);
</script>

<template>
  <section class="teaching-team">
    <header>
      <h3 class="teaching-team__title">
        {{ $t('courses.teaching_team.title') }}
      </h3>
      <p class="teaching-team__subtitle">
        {{ $t('courses.teaching_team.subtitle') }}
      </p>
    </header>

    <p
      v-if="errorMessage"
      class="teaching-team__error"
      role="alert"
    >
      {{ errorMessage }}
    </p>

    <p
      v-if="loading"
      class="teaching-team__hint"
    >
      {{ $t('questions.loading') }}
    </p>

    <ul
      v-else
      class="teaching-team__list"
    >
      <li
        v-for="member in team"
        :key="member.userId"
        class="teaching-team__member"
      >
        <span class="teaching-team__member-name">{{ member.firstName }} {{ member.lastName }}</span>
        <span class="teaching-team__member-email">{{ member.email }}</span>
        <Badge :variant="member.isCreator ? 'coral' : 'muted'">
          {{ member.isCreator ? $t('courses.teaching_team.creator') : roleLabel(member.role) }}
        </Badge>

        <!-- The creator's authorship is not an assignment, so it cannot be revoked here. -->
        <Button
          v-if="!member.isCreator"
          variant="ghost"
          size="sm"
          :aria-label="$t('courses.teaching_team.remove')"
          @click="remove(member)"
        >
          <Trash2 class="size-4" />
        </Button>
      </li>
    </ul>

    <form
      class="teaching-team__form"
      @submit.prevent="assign"
    >
      <label
        class="teaching-team__label"
        for="teaching-team-email"
      >{{ $t('courses.teaching_team.email_label') }}</label>
      <div class="teaching-team__form-row">
        <Input
          id="teaching-team-email"
          v-model="email"
          type="email"
          :placeholder="$t('courses.teaching_team.email_placeholder')"
        />
        <Button
          type="submit"
          :disabled="!email.trim() || submitting"
        >
          {{ submitting ? $t('questions.sending') : $t('courses.teaching_team.assign') }}
        </Button>
      </div>
    </form>
  </section>
</template>

<style scoped>
.teaching-team {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-card);
  padding: clamp(1rem, 3vw, 1.5rem);
}

.teaching-team__title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-ink);
}

.teaching-team__subtitle {
  margin-top: 0.25rem;
  font-size: 0.85rem;
  color: var(--color-ink-muted);
}

.teaching-team__list {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.teaching-team__member {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  padding: 0.5rem 0.75rem;
}

.teaching-team__member-name {
  font-weight: 600;
  color: var(--color-ink);
}

.teaching-team__member-email {
  font-size: 0.85rem;
  color: var(--color-ink-muted);
  margin-right: auto;
}

.teaching-team__form {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.teaching-team__form-row {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.teaching-team__form-row > :first-child {
  flex: 1 1 14rem;
}

.teaching-team__label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-ink);
}

.teaching-team__hint {
  font-size: 0.85rem;
  color: var(--color-ink-muted);
}

.teaching-team__error {
  border: 1px solid var(--color-accent-coral);
  border-radius: var(--radius-card);
  background: var(--color-accent-soft);
  padding: 0.5rem 0.75rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-accent-coral);
}
</style>
