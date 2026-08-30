<script setup>
import { onMounted, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { fetchPromoCodes, createPromoCode, deactivatePromoCode } from '@/services/adminService';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';

const { t } = useI18n();

const promoCodes = ref([]);
const loading = ref(false);
const errorMessage = ref('');

async function load() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchPromoCodes({ page: 1, pageSize: 100 });
    promoCodes.value = result.items;
  } catch {
    errorMessage.value = t('admin.promo_codes.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(load);

const addModalShown = ref(false);
const form = reactive({
  code: '',
  discountType: 'Percentage',
  discountValue: 10,
  courseId: '',
  expiresAt: '',
  maxRedemptions: '',
});

function openAddModal() {
  Object.assign(form, { code: '', discountType: 'Percentage', discountValue: 10, courseId: '', expiresAt: '', maxRedemptions: '' });
  addModalShown.value = true;
}

async function confirmAdd() {
  errorMessage.value = '';
  try {
    await createPromoCode({
      code: form.code,
      discountType: form.discountType,
      discountValue: Number(form.discountValue),
      courseId: form.courseId ? Number(form.courseId) : null,
      expiresAt: form.expiresAt ? new Date(form.expiresAt).toISOString() : null,
      maxRedemptions: form.maxRedemptions ? Number(form.maxRedemptions) : null,
    });
    addModalShown.value = false;
    await load();
  } catch (err) {
    const fieldErrors = err.response?.data?.errors;
    errorMessage.value = err.response?.status === 400 && fieldErrors?.code
      ? fieldErrors.code[0]
      : t('admin.promo_codes.save_error');
  }
}

const deactivateModalShown = ref(false);
const deactivateTarget = ref(null);

function openDeactivateModal(row) {
  deactivateTarget.value = row;
  deactivateModalShown.value = true;
}

async function confirmDeactivate() {
  errorMessage.value = '';
  try {
    await deactivatePromoCode(deactivateTarget.value.id);
    deactivateModalShown.value = false;
    await load();
  } catch {
    errorMessage.value = t('admin.promo_codes.save_error');
  }
}

function discountLabel(row) {
  return row.discountType === 'Percentage'
    ? `${row.discountValue}%`
    : `${new Intl.NumberFormat('ru-RU').format(row.discountValue)} ₽`;
}
</script>

<template>
  <div>
    <div class="mb-4 flex items-center justify-between gap-4">
      <h1 class="font-display text-2xl font-semibold tracking-tight text-ink">
        {{ $t('admin.promo_codes.title') }}
      </h1>
      <Button @click="openAddModal">
        {{ $t('admin.promo_codes.add_action') }}
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
              {{ $t('admin.promo_codes.code') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.promo_codes.discount') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.promo_codes.scope') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.promo_codes.redemptions') }}
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
          <tr v-else-if="!promoCodes.length">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="5"
            >
              {{ $t('admin.promo_codes.empty') }}
            </td>
          </tr>
          <tr
            v-for="row in promoCodes"
            v-else
            :key="row.id"
            class="border-t border-border-subtle"
          >
            <td class="px-3 py-2">
              <span class="font-mono text-ink">{{ row.code }}</span>
              <Badge
                v-if="!row.isActive"
                variant="muted"
                class="ml-2"
              >
                {{ $t('admin.promo_codes.inactive') }}
              </Badge>
            </td>
            <td class="px-3 py-2 text-ink">
              {{ discountLabel(row) }}
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ row.courseTitle || $t('admin.promo_codes.global') }}
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ row.redemptionCount }}{{ row.maxRedemptions ? ` / ${row.maxRedemptions}` : '' }}
            </td>
            <td class="px-3 py-2 text-right">
              <Button
                variant="destructive"
                size="sm"
                :disabled="!row.isActive"
                @click="openDeactivateModal(row)"
              >
                {{ $t('admin.promo_codes.deactivate') }}
              </Button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <Dialog
      v-model:open="addModalShown"
      :title="$t('admin.promo_codes.add_title')"
      :confirm-label="$t('admin.promo_codes.save')"
      @confirm="confirmAdd"
    >
      <div class="flex flex-col gap-3">
        <label class="block text-sm font-medium text-ink-muted">
          {{ $t('admin.promo_codes.code') }}
          <Input
            v-model="form.code"
            class="mt-1"
          />
        </label>
        <label class="block text-sm font-medium text-ink-muted">
          {{ $t('admin.promo_codes.discount_type') }}
          <select
            v-model="form.discountType"
            class="mt-1 w-full rounded-md border border-border-subtle bg-card px-3 py-2 text-sm text-ink"
          >
            <option value="Percentage">
              {{ $t('admin.promo_codes.percentage') }}
            </option>
            <option value="FixedAmount">
              {{ $t('admin.promo_codes.fixed_amount') }}
            </option>
          </select>
        </label>
        <label class="block text-sm font-medium text-ink-muted">
          {{ $t('admin.promo_codes.discount_value') }}
          <Input
            v-model.number="form.discountValue"
            type="number"
            min="1"
            class="mt-1"
          />
        </label>
        <label class="block text-sm font-medium text-ink-muted">
          {{ $t('admin.promo_codes.course_id_optional') }}
          <Input
            v-model="form.courseId"
            type="number"
            min="1"
            class="mt-1"
          />
        </label>
        <label class="block text-sm font-medium text-ink-muted">
          {{ $t('admin.promo_codes.expires_at_optional') }}
          <Input
            v-model="form.expiresAt"
            type="date"
            class="mt-1"
          />
        </label>
        <label class="block text-sm font-medium text-ink-muted">
          {{ $t('admin.promo_codes.max_redemptions_optional') }}
          <Input
            v-model="form.maxRedemptions"
            type="number"
            min="1"
            class="mt-1"
          />
        </label>
      </div>
    </Dialog>

    <Dialog
      v-model:open="deactivateModalShown"
      :title="$t('admin.promo_codes.deactivate_title')"
      :description="$t('admin.promo_codes.deactivate_confirm', { code: deactivateTarget?.code })"
      :confirm-label="$t('admin.promo_codes.deactivate')"
      danger
      @confirm="confirmDeactivate"
    />
  </div>
</template>
