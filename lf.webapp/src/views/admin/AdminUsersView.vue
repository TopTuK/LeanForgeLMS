<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/authStore';
import { fetchUsers, updateUserInfo, updateUserRole, deleteUser } from '@/services/adminService';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Select } from '@/components/ui/select';
import { Dialog } from '@/components/ui/dialog';

const { t } = useI18n();
const authStore = useAuthStore();

const PAGE_SIZE = 20;
const ROLES = ['Student', 'Instructor', 'CourseCreator', 'Admin'];

const users = ref([]);
const totalCount = ref(0);
const page = ref(1);
const search = ref('');
const loading = ref(false);
const errorMessage = ref('');

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / PAGE_SIZE)));
const roleOptions = computed(() => ROLES.map((role) => ({
  value: role,
  label: t(`profile.roles.${roleI18nKey(role)}`),
})));

function roleI18nKey(role) {
  return { Student: 'student', Instructor: 'instructor', CourseCreator: 'course_creator', Admin: 'admin' }[role] ?? 'none';
}

function isSelf(row) {
  return row.email === authStore.user?.email;
}

function formatDate(value) {
  return new Date(value).toLocaleDateString();
}

async function loadUsers() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchUsers({ page: page.value, pageSize: PAGE_SIZE, search: search.value });
    users.value = result.items;
    totalCount.value = result.totalCount;
  } catch {
    errorMessage.value = t('admin.users.load_error');
  } finally {
    loading.value = false;
  }
}

let searchDebounce = null;
watch(search, () => {
  clearTimeout(searchDebounce);
  searchDebounce = setTimeout(() => {
    page.value = 1;
    loadUsers();
  }, 300);
});

watch(page, loadUsers);
onMounted(loadUsers);

const editModalShown = ref(false);
const editTarget = ref(null);
const editFirstName = ref('');
const editLastName = ref('');
const editDescription = ref('');

function openEditModal(row) {
  editTarget.value = row;
  editFirstName.value = row.firstName;
  editLastName.value = row.lastName;
  editDescription.value = row.description ?? '';
  editModalShown.value = true;
}

async function confirmEdit() {
  errorMessage.value = '';
  try {
    await updateUserInfo(editTarget.value.id, {
      firstName: editFirstName.value,
      lastName: editLastName.value || null,
      description: editDescription.value || null,
    });
    editModalShown.value = false;
    await loadUsers();
  } catch {
    errorMessage.value = t('admin.users.save_error');
  }
}

const roleModalShown = ref(false);
const roleTarget = ref(null);
const selectedRole = ref('');

function openRoleModal(row) {
  roleTarget.value = row;
  selectedRole.value = row.role;
  roleModalShown.value = true;
}

async function confirmRoleChange() {
  errorMessage.value = '';
  try {
    await updateUserRole(roleTarget.value.id, selectedRole.value);
    roleModalShown.value = false;
    await loadUsers();
  } catch {
    errorMessage.value = t('admin.users.save_error');
  }
}

const deleteModalShown = ref(false);
const deleteTarget = ref(null);

function openDeleteModal(row) {
  deleteTarget.value = row;
  deleteModalShown.value = true;
}

async function confirmDelete() {
  errorMessage.value = '';
  try {
    await deleteUser(deleteTarget.value.id);
    deleteModalShown.value = false;
    if (users.value.length === 1 && page.value > 1) page.value -= 1;
    else await loadUsers();
  } catch {
    errorMessage.value = t('admin.users.save_error');
  }
}
</script>

<template>
  <div class="admin-users">
    <h1 class="font-display text-2xl font-semibold tracking-tight text-ink">
      {{ $t('admin.users.title') }}
    </h1>

    <p
      v-if="errorMessage"
      class="mt-4 rounded-md border border-accent-coral bg-accent-soft px-3 py-2 text-sm font-semibold text-accent-coral"
    >
      {{ errorMessage }}
    </p>

    <div class="mt-4 max-w-xs">
      <Input
        v-model="search"
        :placeholder="$t('admin.users.search_placeholder')"
      />
    </div>

    <div class="mt-4 overflow-x-auto rounded-lg border border-border-subtle bg-card">
      <table class="w-full text-left text-sm">
        <thead class="border-b border-border-subtle text-ink-muted">
          <tr>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.users.name') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.users.email') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.users.role') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.users.created_at') }}
            </th>
            <th class="px-3 py-2" />
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="5"
            >
              {{ $t('courses.loading') }}
            </td>
          </tr>
          <tr v-else-if="!users.length">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="5"
            >
              {{ $t('admin.users.no_users') }}
            </td>
          </tr>
          <tr
            v-for="row in users"
            v-else
            :key="row.id"
            class="border-t border-border-subtle"
          >
            <td class="px-3 py-2 text-ink">
              {{ row.firstName }} {{ row.lastName }}
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ row.email }}
            </td>
            <td class="px-3 py-2">
              {{ $t(`profile.roles.${roleI18nKey(row.role)}`) }}
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ formatDate(row.createdAt) }}
            </td>
            <td class="px-3 py-2">
              <div class="flex flex-wrap gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  @click="openEditModal(row)"
                >
                  {{ $t('admin.users.edit') }}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  :disabled="isSelf(row)"
                  :title="isSelf(row) ? $t('admin.users.self_action_disabled') : ''"
                  @click="openRoleModal(row)"
                >
                  {{ $t('admin.users.change_role') }}
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  :disabled="isSelf(row)"
                  :title="isSelf(row) ? $t('admin.users.self_action_disabled') : ''"
                  @click="openDeleteModal(row)"
                >
                  {{ $t('admin.users.delete') }}
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
      v-model:open="editModalShown"
      :title="$t('admin.users.edit_title')"
      :confirm-label="$t('admin.users.save')"
      :cancel-label="$t('admin.users.cancel')"
      @confirm="confirmEdit"
    >
      <div class="space-y-3">
        <label class="block text-sm font-medium text-ink-muted">
          {{ $t('admin.users.first_name') }}
          <Input
            v-model="editFirstName"
            class="mt-1"
          />
        </label>
        <label class="block text-sm font-medium text-ink-muted">
          {{ $t('admin.users.last_name') }}
          <Input
            v-model="editLastName"
            class="mt-1"
          />
        </label>
        <label class="block text-sm font-medium text-ink-muted">
          {{ $t('admin.users.description') }}
          <Textarea
            v-model="editDescription"
            class="mt-1"
          />
        </label>
      </div>
    </Dialog>

    <Dialog
      v-model:open="roleModalShown"
      :title="$t('admin.users.change_role_title')"
      :confirm-label="$t('admin.users.save')"
      @confirm="confirmRoleChange"
    >
      <Select
        v-model="selectedRole"
        :options="roleOptions"
      />
    </Dialog>

    <Dialog
      v-model:open="deleteModalShown"
      :title="$t('admin.users.delete_title')"
      :description="$t('admin.users.delete_confirm', { name: `${deleteTarget?.firstName} ${deleteTarget?.lastName}` })"
      :confirm-label="$t('admin.users.delete')"
      danger
      @confirm="confirmDelete"
    />
  </div>
</template>
