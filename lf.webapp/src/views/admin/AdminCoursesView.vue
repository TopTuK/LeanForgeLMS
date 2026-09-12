<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import {
  fetchAdminCourses,
  fetchCourseEnrollments,
  enrollStudent,
  removeEnrollment,
  deleteCourse,
  fetchUsers,
} from '@/services/adminService';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog } from '@/components/ui/dialog';

const { t } = useI18n();
const router = useRouter();

const PAGE_SIZE = 20;

const courses = ref([]);
const totalCount = ref(0);
const page = ref(1);
const loading = ref(false);
const errorMessage = ref('');

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / PAGE_SIZE)));

function formatDate(value) {
  return new Date(value).toLocaleDateString();
}

function formatPrice(course) {
  return course.pricingType === 'Paid' ? `${course.price} ₽` : t('admin.courses.free');
}

// 409 bodies are plain strings carrying the server's reason (e.g. which student has paid);
// surface them rather than swallowing them behind a generic message.
function toMessage(err, fallbackKey) {
  return err.response?.status === 409 && typeof err.response.data === 'string'
    ? err.response.data
    : t(fallbackKey);
}

async function loadCourses() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchAdminCourses({ page: page.value, pageSize: PAGE_SIZE });
    courses.value = result.items;
    totalCount.value = result.totalCount;
  } catch {
    errorMessage.value = t('admin.courses.load_error');
  } finally {
    loading.value = false;
  }
}

watch(page, loadCourses);
onMounted(loadCourses);

// The course editor already accepts an admin on any course — EnsureOwnership passes for admins
// on every mutation — so this is a plain navigation, not a separate admin-only editor.
function openEditor(course) {
  router.push({ name: 'CourseEdit', params: { id: course.id } });
}

/* ---- Students dialog ---- */

const studentsModalShown = ref(false);
const studentsCourse = ref(null);
const enrollments = ref([]);
const enrollmentsLoading = ref(false);
const studentsError = ref('');

async function loadEnrollments() {
  if (!studentsCourse.value) return;
  enrollmentsLoading.value = true;
  studentsError.value = '';
  try {
    const result = await fetchCourseEnrollments(studentsCourse.value.id, { page: 1, pageSize: 100 });
    enrollments.value = result.items;
  } catch {
    studentsError.value = t('admin.courses.load_error');
  } finally {
    enrollmentsLoading.value = false;
  }
}

function openStudentsModal(course) {
  studentsCourse.value = course;
  enrollments.value = [];
  studentSearch.value = '';
  studentResults.value = [];
  studentsModalShown.value = true;
  loadEnrollments();
}

function authorName(row) {
  const name = [row.authorFirstName, row.authorLastName].filter(Boolean).join(' ');
  return name || t('admin.courses.unknown_author', { id: row.createdByUserId });
}

function studentName(row) {
  const name = [row.studentFirstName, row.studentLastName].filter(Boolean).join(' ');
  return name || t('admin.courses.unknown_student', { id: row.userId });
}

/* ---- Enroll a student ---- */

const studentSearch = ref('');
const studentResults = ref([]);
const enrolling = ref(false);

let searchDebounce = null;
watch(studentSearch, () => {
  clearTimeout(searchDebounce);
  searchDebounce = setTimeout(async () => {
    if (!studentSearch.value.trim()) {
      studentResults.value = [];
      return;
    }
    try {
      const result = await fetchUsers({ page: 1, pageSize: 5, search: studentSearch.value });
      studentResults.value = result.items;
    } catch {
      studentResults.value = [];
    }
  }, 300);
});

const enrolledUserIds = computed(() => new Set(enrollments.value.map((e) => e.userId)));

async function confirmEnroll(user) {
  enrolling.value = true;
  studentsError.value = '';
  try {
    await enrollStudent(studentsCourse.value.id, user.id);
    studentSearch.value = '';
    studentResults.value = [];
    await loadEnrollments();
  } catch (err) {
    studentsError.value = toMessage(err, 'admin.courses.enroll_error');
  } finally {
    enrolling.value = false;
  }
}

/* ---- Remove a student ---- */

const removeModalShown = ref(false);
const removeTarget = ref(null);

function openRemoveModal(row) {
  removeTarget.value = row;
  removeModalShown.value = true;
}

const removeDescription = computed(() => {
  if (!removeTarget.value) return '';
  return removeTarget.value.isPaid
    ? t('admin.courses.remove_student_paid_confirm', {
      name: studentName(removeTarget.value),
      amount: removeTarget.value.pricePaid,
    })
    : t('admin.courses.remove_student_confirm', { name: studentName(removeTarget.value) });
});

async function confirmRemoveStudent() {
  studentsError.value = '';
  try {
    await removeEnrollment(studentsCourse.value.id, removeTarget.value.id);
    removeModalShown.value = false;
    await loadEnrollments();
  } catch (err) {
    studentsError.value = toMessage(err, 'admin.courses.remove_student_error');
  }
}

/* ---- Delete a course ---- */

const deleteModalShown = ref(false);
const deleteTarget = ref(null);
const deleteStats = ref(null);
const deleteBlockedMessage = ref('');
const deleteAcknowledged = ref(false);
const deleting = ref(false);

async function openDeleteModal(course) {
  deleteTarget.value = course;
  deleteStats.value = null;
  deleteBlockedMessage.value = '';
  deleteAcknowledged.value = false;
  deleteModalShown.value = true;

  // Show the real blast radius before asking: how many students, and how many actually paid.
  try {
    const result = await fetchCourseEnrollments(course.id, { page: 1, pageSize: 100 });
    deleteStats.value = {
      total: result.totalCount,
      paid: result.items.filter((e) => e.isPaid).length,
    };
  } catch {
    deleteStats.value = null;
  }
}

const hasPaidStudents = computed(() => (deleteStats.value?.paid ?? 0) > 0);
const deleteDisabled = computed(() => deleting.value || (hasPaidStudents.value && !deleteAcknowledged.value));

async function confirmDeleteCourse() {
  if (deleteDisabled.value) return;
  deleting.value = true;
  deleteBlockedMessage.value = '';
  try {
    await deleteCourse(deleteTarget.value.id, { force: hasPaidStudents.value });
    deleteModalShown.value = false;
    if (courses.value.length === 1 && page.value > 1) page.value -= 1;
    else await loadCourses();
  } catch (err) {
    deleteBlockedMessage.value = toMessage(err, 'admin.courses.delete_error');
  } finally {
    deleting.value = false;
  }
}
</script>

<template>
  <div class="admin-courses">
    <h1 class="font-display text-2xl font-semibold tracking-tight text-ink">
      {{ $t('admin.courses.title') }}
    </h1>

    <p
      v-if="errorMessage"
      class="mt-4 rounded-md border border-accent-coral bg-accent-soft px-3 py-2 text-sm font-semibold text-accent-coral"
    >
      {{ errorMessage }}
    </p>

    <div class="mt-4 overflow-x-auto rounded-lg border border-border-subtle bg-card">
      <table class="w-full text-left text-sm">
        <thead class="border-b border-border-subtle text-ink-muted">
          <tr>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.courses.course') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.courses.author') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.courses.category') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.courses.price') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.courses.enrollment_mode') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.courses.status') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.courses.created_at') }}
            </th>
            <th class="px-3 py-2" />
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="8"
            >
              {{ $t('courses.loading') }}
            </td>
          </tr>
          <tr v-else-if="!courses.length">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="8"
            >
              {{ $t('admin.courses.no_courses') }}
            </td>
          </tr>
          <tr
            v-for="row in courses"
            v-else
            :key="row.id"
            class="border-t border-border-subtle"
          >
            <td class="px-3 py-2 text-ink">
              {{ row.title }}
            </td>
            <td class="px-3 py-2">
              <span class="block text-ink">{{ authorName(row) }}</span>
              <span class="block text-xs text-ink-muted">{{ row.authorEmail }}</span>
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ row.categoryName }}
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ formatPrice(row) }}
            </td>
            <td class="px-3 py-2">
              <span :class="row.enrollmentMode === 'Managed' ? 'font-semibold text-ink' : 'text-ink-muted'">
                {{ row.enrollmentMode === 'Managed'
                  ? $t('admin.courses.mode_managed')
                  : $t('admin.courses.mode_open') }}
              </span>
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ row.isPublished ? $t('admin.courses.published') : $t('admin.courses.draft') }}
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ formatDate(row.createdAt) }}
            </td>
            <td class="px-3 py-2">
              <div class="flex flex-wrap gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  @click="openEditor(row)"
                >
                  {{ $t('admin.courses.edit') }}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  @click="openStudentsModal(row)"
                >
                  {{ $t('admin.courses.students') }}
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  @click="openDeleteModal(row)"
                >
                  {{ $t('admin.courses.delete') }}
                </Button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="mt-4 flex items-center justify-center gap-3 text-sm text-ink-muted">
      <Button
        variant="outline"
        size="sm"
        :disabled="page <= 1"
        @click="page -= 1"
      >
        ‹
      </Button>
      <span>{{ page }} / {{ totalPages }}</span>
      <Button
        variant="outline"
        size="sm"
        :disabled="page >= totalPages"
        @click="page += 1"
      >
        ›
      </Button>
    </div>

    <Dialog
      v-model:open="studentsModalShown"
      :title="$t('admin.courses.students_title', { title: studentsCourse?.title ?? '' })"
      :description="$t('admin.courses.students_description')"
      :cancel-label="$t('admin.courses.close')"
    >
      <div class="space-y-4">
        <p
          v-if="studentsError"
          class="rounded-md border border-accent-coral bg-accent-soft px-3 py-2 text-sm font-semibold text-accent-coral"
        >
          {{ studentsError }}
        </p>

        <div>
          <label class="block text-sm font-medium text-ink-muted">
            {{ $t('admin.courses.enroll_label') }}
            <Input
              v-model="studentSearch"
              class="mt-1"
              :placeholder="$t('admin.courses.enroll_placeholder')"
            />
          </label>
          <ul
            v-if="studentResults.length"
            class="mt-2 divide-y divide-border-subtle rounded-md border border-border-subtle"
          >
            <li
              v-for="user in studentResults"
              :key="user.id"
              class="flex items-center justify-between gap-3 px-3 py-2 text-sm"
            >
              <span class="min-w-0">
                <span class="block truncate text-ink">{{ user.firstName }} {{ user.lastName }}</span>
                <span class="block truncate text-xs text-ink-muted">{{ user.email }}</span>
              </span>
              <Button
                size="sm"
                :disabled="enrolling || enrolledUserIds.has(user.id)"
                @click="confirmEnroll(user)"
              >
                {{ enrolledUserIds.has(user.id)
                  ? $t('admin.courses.already_enrolled')
                  : $t('admin.courses.enroll_action') }}
              </Button>
            </li>
          </ul>
        </div>

        <div class="max-h-64 overflow-y-auto rounded-md border border-border-subtle">
          <table class="w-full text-left text-sm">
            <thead class="border-b border-border-subtle text-ink-muted">
              <tr>
                <th class="px-3 py-2 font-semibold">
                  {{ $t('admin.courses.student') }}
                </th>
                <th class="px-3 py-2 font-semibold">
                  {{ $t('admin.courses.progress') }}
                </th>
                <th class="px-3 py-2 font-semibold">
                  {{ $t('admin.courses.paid') }}
                </th>
                <th class="px-3 py-2" />
              </tr>
            </thead>
            <tbody>
              <tr v-if="enrollmentsLoading">
                <td
                  class="px-3 py-6 text-ink-muted"
                  colspan="4"
                >
                  {{ $t('courses.loading') }}
                </td>
              </tr>
              <tr v-else-if="!enrollments.length">
                <td
                  class="px-3 py-6 text-ink-muted"
                  colspan="4"
                >
                  {{ $t('admin.courses.no_students') }}
                </td>
              </tr>
              <tr
                v-for="row in enrollments"
                v-else
                :key="row.id"
                class="border-t border-border-subtle"
              >
                <td class="px-3 py-2">
                  <span class="block text-ink">{{ studentName(row) }}</span>
                  <span class="block text-xs text-ink-muted">{{ row.studentEmail }}</span>
                </td>
                <td class="px-3 py-2 text-ink-muted">
                  {{ row.completedLessonCount }} / {{ row.totalLessonCount }} ({{ row.progressPercent }}%)
                </td>
                <td class="px-3 py-2 text-ink-muted">
                  {{ row.isPaid ? `${row.pricePaid} ₽` : '—' }}
                </td>
                <td class="px-3 py-2">
                  <Button
                    variant="destructive"
                    size="sm"
                    @click="openRemoveModal(row)"
                  >
                    {{ $t('admin.courses.remove_student') }}
                  </Button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </Dialog>

    <Dialog
      v-model:open="removeModalShown"
      :title="$t('admin.courses.remove_student_title')"
      :description="removeDescription"
      :confirm-label="$t('admin.courses.remove_student')"
      :cancel-label="$t('admin.courses.cancel')"
      danger
      @confirm="confirmRemoveStudent"
    />

    <Dialog
      v-model:open="deleteModalShown"
      :title="$t('admin.courses.delete_title')"
      :description="$t('admin.courses.delete_confirm', { title: deleteTarget?.title ?? '' })"
      :cancel-label="$t('admin.courses.cancel')"
    >
      <div class="space-y-3 text-sm">
        <p
          v-if="deleteStats"
          class="text-ink-muted"
        >
          {{ $t('admin.courses.delete_stats', { total: deleteStats.total, paid: deleteStats.paid }) }}
        </p>

        <label
          v-if="hasPaidStudents"
          class="flex items-start gap-2 rounded-md border border-accent-coral bg-accent-soft px-3 py-2 font-semibold text-accent-coral"
        >
          <input
            v-model="deleteAcknowledged"
            type="checkbox"
            class="mt-0.5"
          >
          <span>{{ $t('admin.courses.delete_paid_ack', { paid: deleteStats.paid }) }}</span>
        </label>

        <p
          v-if="deleteBlockedMessage"
          class="rounded-md border border-accent-coral bg-accent-soft px-3 py-2 font-semibold text-accent-coral"
        >
          {{ deleteBlockedMessage }}
        </p>
      </div>

      <template #footer>
        <Button
          variant="outline"
          @click="deleteModalShown = false"
        >
          {{ $t('admin.courses.cancel') }}
        </Button>
        <Button
          variant="destructive"
          :disabled="deleteDisabled"
          @click="confirmDeleteCourse"
        >
          {{ $t('admin.courses.delete') }}
        </Button>
      </template>
    </Dialog>
  </div>
</template>
