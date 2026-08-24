<script setup>
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { fetchAdminCategories, createCategory, deleteCategory } from '@/services/adminService';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';

const { t } = useI18n();

const categories = ref([]);
const loading = ref(false);
const errorMessage = ref('');

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
    addModalShown.value = false;
    await loadCategories();
  } catch (err) {
    const fieldErrors = err.response?.data?.errors;
    errorMessage.value = err.response?.status === 400 && fieldErrors?.name
      ? fieldErrors.name[0]
      : t('admin.categories.save_error');
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
    await deleteCategory(deleteTarget.value.id);
    deleteModalShown.value = false;
    await loadCategories();
  } catch (err) {
    errorMessage.value = err.response?.status === 409 && typeof err.response.data === 'string'
      ? err.response.data
      : t('admin.categories.save_error');
  }
}
</script>

<template>
  <div>
    <div class="mb-4 flex items-center justify-between gap-4">
      <h1 class="font-display text-2xl font-semibold tracking-tight text-ink">
        {{ $t('admin.categories.title') }}
      </h1>
      <Button @click="openAddModal">
        {{ $t('admin.categories.add_action') }}
      </Button>
    </div>

    <p
      v-if="errorMessage"
      class="mb-4 rounded-md border border-accent-coral bg-accent-soft px-3 py-2 text-sm font-semibold text-accent-coral"
    >
      {{ errorMessage }}
    </p>

    <div class="overflow-x-auto rounded-lg border border-border-subtle bg-card">
      <table class="w-full text-left text-sm">
        <thead class="border-b border-border-subtle text-ink-muted">
          <tr>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.categories.name') }}
            </th>
            <th class="px-3 py-2" />
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="2"
            >
              {{ $t('courses.loading') }}
            </td>
          </tr>
          <tr v-else-if="!categories.length">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="2"
            >
              {{ $t('admin.categories.no_categories') }}
            </td>
          </tr>
          <tr
            v-for="row in categories"
            v-else
            :key="row.id"
            class="border-t border-border-subtle"
          >
            <td class="px-3 py-2">
              <span class="text-ink">{{ row.name }}</span>
              <Badge
                v-if="row.isDefault"
                variant="muted"
                class="ml-2"
              >
                {{ $t('admin.categories.default_badge') }}
              </Badge>
            </td>
            <td class="px-3 py-2 text-right">
              <Button
                variant="destructive"
                size="sm"
                :disabled="row.isDefault"
                :title="row.isDefault ? $t('admin.categories.default_protected') : ''"
                @click="openDeleteModal(row)"
              >
                {{ $t('admin.categories.delete') }}
              </Button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <Dialog
      v-model:open="addModalShown"
      :title="$t('admin.categories.add_title')"
      :confirm-label="$t('admin.categories.save')"
      @confirm="confirmAdd"
    >
      <label class="block text-sm font-medium text-ink-muted">
        {{ $t('admin.categories.name') }}
        <Input
          v-model="newCategoryName"
          class="mt-1"
        />
      </label>
    </Dialog>

    <Dialog
      v-model:open="deleteModalShown"
      :title="$t('admin.categories.delete_title')"
      :description="$t('admin.categories.delete_confirm', { name: deleteTarget?.name })"
      :confirm-label="$t('admin.categories.delete')"
      danger
      @confirm="confirmDelete"
    />
  </div>
</template>
