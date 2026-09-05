<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { downloadPaymentsCsv, fetchPayments } from '@/services/adminService';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

const { t } = useI18n();

const PAGE_SIZE = 20;

const rows = ref([]);
const totalCount = ref(0);
const totalAmount = ref(0);
const page = ref(1);
const from = ref('');
const to = ref('');
const loading = ref(false);
const downloading = ref(false);
const errorMessage = ref('');

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / PAGE_SIZE)));
const amountFormatter = new Intl.NumberFormat('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

async function load() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchPayments({ page: page.value, pageSize: PAGE_SIZE, from: from.value, to: to.value });
    rows.value = result.items;
    totalCount.value = result.totalCount;
    totalAmount.value = result.totalAmount;
  } catch {
    errorMessage.value = t('admin.payments.load_error');
  } finally {
    loading.value = false;
  }
}

function applyFilter() {
  page.value = 1;
  load();
}

watch(page, load);
onMounted(load);

async function download() {
  downloading.value = true;
  errorMessage.value = '';
  try {
    await downloadPaymentsCsv({ from: from.value, to: to.value });
  } catch {
    errorMessage.value = t('admin.payments.download_error');
  } finally {
    downloading.value = false;
  }
}

function formatDate(value) {
  return new Date(value).toLocaleString();
}

function formatAmount(value) {
  return `${amountFormatter.format(value)} ₽`;
}
</script>

<template>
  <div>
    <div class="mb-4 flex flex-wrap items-center justify-between gap-4">
      <h1 class="font-display text-2xl font-semibold tracking-tight text-ink">
        {{ $t('admin.payments.title') }}
      </h1>
      <Button
        :disabled="downloading || !rows.length"
        @click="download"
      >
        {{ downloading ? $t('admin.payments.downloading') : $t('admin.payments.download') }}
      </Button>
    </div>

    <p
      v-if="errorMessage"
      class="mb-4 rounded-md border border-accent-coral bg-accent-soft px-3 py-2 text-sm font-semibold text-accent-coral"
    >
      {{ errorMessage }}
    </p>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <label class="text-sm font-medium text-ink-muted">
        {{ $t('admin.payments.from') }}
        <Input
          v-model="from"
          type="date"
          class="mt-1"
        />
      </label>
      <label class="text-sm font-medium text-ink-muted">
        {{ $t('admin.payments.to') }}
        <Input
          v-model="to"
          type="date"
          class="mt-1"
        />
      </label>
      <Button
        variant="outline"
        @click="applyFilter"
      >
        {{ $t('admin.payments.apply_filter') }}
      </Button>
    </div>

    <div class="overflow-x-auto rounded-lg border border-border-subtle bg-card">
      <table class="w-full text-left text-sm">
        <thead class="border-b border-border-subtle text-ink-muted">
          <tr>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.payments.paid_at') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.payments.student') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.payments.course') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.payments.amount') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.payments.promo_code') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.payments.operation_id') }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="6"
            >
              {{ $t('courses.loading') }}
            </td>
          </tr>
          <tr v-else-if="!rows.length">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="6"
            >
              {{ $t('admin.payments.empty') }}
            </td>
          </tr>
          <tr
            v-for="row in rows"
            v-else
            :key="row.id"
            class="border-t border-border-subtle"
          >
            <td class="px-3 py-2 text-ink-muted">
              {{ formatDate(row.paidAt) }}
            </td>
            <td class="px-3 py-2 text-ink">
              <span class="block">{{ row.studentName }}</span>
              <span class="block text-xs text-ink-muted">{{ row.studentEmail }}</span>
            </td>
            <td class="px-3 py-2 text-ink">
              {{ row.courseTitle }}
            </td>
            <td class="px-3 py-2 text-ink">
              {{ formatAmount(row.amount) }}
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ row.promoCode || '—' }}
            </td>
            <td class="px-3 py-2 font-mono text-xs text-ink-muted">
              {{ row.providerOperationId || '—' }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="mt-4 flex flex-wrap items-center justify-between gap-3 text-sm text-ink-muted">
      <span>{{ $t('admin.payments.summary', { count: totalCount, total: formatAmount(totalAmount) }) }}</span>
      <span class="flex items-center gap-3">
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
      </span>
    </div>
  </div>
</template>
