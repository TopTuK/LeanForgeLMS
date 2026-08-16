<script setup>
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { fetchAdminCategories, createCategory, deleteCategory } from '@/services/adminService';

const { t } = useI18n();

const categories = ref([]);
const loading = ref(false);
const errorMessage = ref('');

const columns = [
  { key: 'name', label: t('admin.categories.name') },
  { key: 'actions', label: '', width: 160 },
];

async function loadCategories() {
  loading.value = true;
  errorMessage.value = '';
  try {
    categories.value = await fetchAdminCategories();
  } catch {
    errorMessage.value = t('admin.categories.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(loadCategories);

// Add modal
const addModalShown = ref(false);
const newCategoryName = ref('');

function openAddModal() {
  newCategoryName.value = '';
  addModalShown.value = true;
}

async function confirmAdd() {
  errorMessage.value = '';
  try {
    await createCategory(newCategoryName.value);
    await loadCategories();
  } catch (err) {
    const fieldErrors = err.response?.data?.errors;
    errorMessage.value = err.response?.status === 400 && fieldErrors?.name
      ? fieldErrors.name[0]
      : t('admin.categories.save_error');
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
    await deleteCategory(deleteTarget.value.id);
    await loadCategories();
  } catch (err) {
    errorMessage.value = err.response?.status === 409 && typeof err.response.data === 'string'
      ? err.response.data
      : t('admin.categories.save_error');
  }
}
</script>

<template>
  <div class="admin-categories">
    <div class="admin-categories__header">
      <h1 class="admin-categories__title">
        {{ $t('admin.categories.title') }}
      </h1>
      <va-button
        icon="add"
        @click="openAddModal"
      >
        {{ $t('admin.categories.add_action') }}
      </va-button>
    </div>

    <va-alert
      v-if="errorMessage"
      color="danger"
      closeable
      class="admin-categories__alert"
      @close="errorMessage = ''"
    >
      {{ errorMessage }}
    </va-alert>

    <va-data-table
      :items="categories"
      :columns="columns"
      :loading="loading"
      :no-data-html="$t('admin.categories.no_categories')"
    >
      <template #cell(name)="{ rowData }">
        {{ rowData.name }}
        <va-chip
          v-if="rowData.isDefault"
          size="small"
          class="admin-categories__default-chip"
        >
          {{ $t('admin.categories.default_badge') }}
        </va-chip>
      </template>
      <template #cell(actions)="{ rowData }">
        <va-button
          preset="secondary"
          size="small"
          color="danger"
          :disabled="rowData.isDefault"
          :title="rowData.isDefault ? $t('admin.categories.default_protected') : ''"
          @click="openDeleteModal(rowData)"
        >
          {{ $t('admin.categories.delete') }}
        </va-button>
      </template>
    </va-data-table>

    <va-modal
      v-model="addModalShown"
      :title="$t('admin.categories.add_title')"
      :ok-text="$t('admin.categories.save')"
      @ok="confirmAdd"
    >
      <va-input
        v-model="newCategoryName"
        class="admin-categories__field"
        :label="$t('admin.categories.name')"
      />
    </va-modal>

    <va-modal
      v-model="deleteModalShown"
      :title="$t('admin.categories.delete_title')"
      :message="$t('admin.categories.delete_confirm', { name: deleteTarget?.name })"
      :ok-text="$t('admin.categories.delete')"
      @ok="confirmDelete"
    />
  </div>
</template>

<style scoped>
.admin-categories__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 1rem;
}

.admin-categories__title {
    font-size: 1.5rem;
    font-weight: 700;
}

.admin-categories__alert {
    margin-bottom: 1rem;
}

.admin-categories__default-chip {
    margin-left: 0.5rem;
}

.admin-categories__field {
    margin-bottom: 1rem;
}
</style>
