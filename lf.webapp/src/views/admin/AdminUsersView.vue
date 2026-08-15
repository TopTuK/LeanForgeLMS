<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/authStore';
import { fetchUsers, updateUserInfo, updateUserRole, deleteUser } from '@/services/adminService';

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

const columns = computed(() => [
  { key: 'firstName', label: t('admin.users.name') },
  { key: 'email', label: t('admin.users.email') },
  { key: 'role', label: t('admin.users.role') },
  { key: 'createdAt', label: t('admin.users.created_at') },
  { key: 'actions', label: '', width: 220 },
]);

const roleOptions = computed(() => ROLES.map((role) => ({ value: role, text: t(`profile.roles.${roleI18nKey(role)}`) })));

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

// Edit info modal
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
    await loadUsers();
  } catch {
    errorMessage.value = t('admin.users.save_error');
  }
}

// Role modal
const roleModalShown = ref(false);
const roleTarget = ref(null);
const selectedRole = ref(null);

function openRoleModal(row) {
  roleTarget.value = row;
  selectedRole.value = roleOptions.value.find((o) => o.value === row.role) ?? null;
  roleModalShown.value = true;
}

async function confirmRoleChange() {
  errorMessage.value = '';
  try {
    await updateUserRole(roleTarget.value.id, selectedRole.value.value);
    await loadUsers();
  } catch {
    errorMessage.value = t('admin.users.save_error');
  }
}

// Delete modal
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
    if (users.value.length === 1 && page.value > 1) page.value -= 1;
    else await loadUsers();
  } catch {
    errorMessage.value = t('admin.users.save_error');
  }
}
</script>

<template>
  <div class="admin-users">
    <h1 class="admin-users__title">
      {{ $t('admin.users.title') }}
    </h1>

    <va-alert
      v-if="errorMessage"
      color="danger"
      closeable
      class="admin-users__alert"
      @close="errorMessage = ''"
    >
      {{ errorMessage }}
    </va-alert>

    <va-input
      v-model="search"
      class="admin-users__search"
      :placeholder="$t('admin.users.search_placeholder')"
      clearable
    >
      <template #prependInner>
        <va-icon name="search" />
      </template>
    </va-input>

    <va-data-table
      :items="users"
      :columns="columns"
      :loading="loading"
      :no-data-html="$t('admin.users.no_users')"
    >
      <template #cell(firstName)="{ rowData }">
        {{ rowData.firstName }} {{ rowData.lastName }}
      </template>
      <template #cell(role)="{ rowData }">
        {{ $t(`profile.roles.${roleI18nKey(rowData.role)}`) }}
      </template>
      <template #cell(createdAt)="{ rowData }">
        {{ formatDate(rowData.createdAt) }}
      </template>
      <template #cell(actions)="{ rowData }">
        <div class="admin-users__actions">
          <va-button
            preset="secondary"
            size="small"
            @click="openEditModal(rowData)"
          >
            {{ $t('admin.users.edit') }}
          </va-button>
          <va-button
            preset="secondary"
            size="small"
            :disabled="isSelf(rowData)"
            :title="isSelf(rowData) ? $t('admin.users.self_action_disabled') : ''"
            @click="openRoleModal(rowData)"
          >
            {{ $t('admin.users.change_role') }}
          </va-button>
          <va-button
            preset="secondary"
            size="small"
            color="danger"
            :disabled="isSelf(rowData)"
            :title="isSelf(rowData) ? $t('admin.users.self_action_disabled') : ''"
            @click="openDeleteModal(rowData)"
          >
            {{ $t('admin.users.delete') }}
          </va-button>
        </div>
      </template>
    </va-data-table>

    <va-pagination
      v-model="page"
      class="admin-users__pagination"
      :pages="totalPages"
      :visible-pages="5"
    />

    <va-modal
      v-model="editModalShown"
      :title="$t('admin.users.edit_title')"
      :ok-text="$t('admin.users.save')"
      @ok="confirmEdit"
    >
      <va-input
        v-model="editFirstName"
        class="admin-users__field"
        :label="$t('admin.users.first_name')"
      />
      <va-input
        v-model="editLastName"
        class="admin-users__field"
        :label="$t('admin.users.last_name')"
      />
      <va-textarea
        v-model="editDescription"
        class="admin-users__field"
        :label="$t('admin.users.description')"
        :max-length="500"
        :min-rows="3"
      />
    </va-modal>

    <va-modal
      v-model="roleModalShown"
      :title="$t('admin.users.change_role_title')"
      :ok-text="$t('admin.users.save')"
      @ok="confirmRoleChange"
    >
      <va-select
        v-model="selectedRole"
        :label="$t('admin.users.role')"
        :options="roleOptions"
        text-by="text"
        value-by="value"
      />
    </va-modal>

    <va-modal
      v-model="deleteModalShown"
      :title="$t('admin.users.delete_title')"
      :message="$t('admin.users.delete_confirm', { name: `${deleteTarget?.firstName} ${deleteTarget?.lastName}` })"
      :ok-text="$t('admin.users.delete')"
      @ok="confirmDelete"
    />
  </div>
</template>

<style scoped>
.admin-users__title {
    font-size: 1.5rem;
    font-weight: 700;
    margin-bottom: 1rem;
}

.admin-users__alert {
    margin-bottom: 1rem;
}

.admin-users__search {
    max-width: 20rem;
    margin-bottom: 1rem;
}

.admin-users__actions {
    display: flex;
    gap: 0.5rem;
}

.admin-users__pagination {
    margin-top: 1rem;
    justify-content: center;
}

.admin-users__field {
    margin-bottom: 1rem;
}
</style>
