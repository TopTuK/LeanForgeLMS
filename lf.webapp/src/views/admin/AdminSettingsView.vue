<script setup>
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { fetchPlatformSettings, updateStudentEnrollment } from '@/services/adminService';
import { Button } from '@/components/ui/button';

const { t } = useI18n();

const enabled = ref(false);
const updatedAt = ref(null);
const loading = ref(false);
const saving = ref(false);
const errorMessage = ref('');
const savedMessage = ref('');

async function load() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const settings = await fetchPlatformSettings();
    enabled.value = settings.studentEnrollmentEnabled;
    updatedAt.value = settings.updatedAt;
  } catch {
    errorMessage.value = t('admin.settings.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(load);

async function save() {
  saving.value = true;
  errorMessage.value = '';
  savedMessage.value = '';
  try {
    const settings = await updateStudentEnrollment(enabled.value);
    enabled.value = settings.studentEnrollmentEnabled;
    updatedAt.value = settings.updatedAt;
    savedMessage.value = t('admin.settings.saved');
  } catch {
    errorMessage.value = t('admin.settings.save_error');
  } finally {
    saving.value = false;
  }
}

function formatDate(value) {
  return value ? new Date(value).toLocaleString() : '';
}
</script>

<template>
  <div>
    <h1 class="mb-4 font-display text-2xl font-semibold tracking-tight text-ink">
      {{ $t('admin.settings.title') }}
    </h1>

    <p
      v-if="errorMessage"
      class="mb-4 rounded-md border border-accent-coral bg-accent-soft px-3 py-2 text-sm font-semibold text-accent-coral"
    >
      {{ errorMessage }}
    </p>
    <p
      v-if="savedMessage"
      class="mb-4 rounded-md border border-border-subtle bg-card px-3 py-2 text-sm font-semibold text-ink"
    >
      {{ savedMessage }}
    </p>

    <div class="max-w-2xl rounded-lg border border-border-subtle bg-card p-4">
      <p
        v-if="loading"
        class="text-sm text-ink-muted"
      >
        {{ $t('courses.loading') }}
      </p>

      <template v-else>
        <label class="flex items-start gap-3">
          <input
            v-model="enabled"
            type="checkbox"
            class="mt-1 size-4 shrink-0"
          >
          <span>
            <span class="block text-sm font-semibold text-ink">
              {{ $t('admin.settings.student_enrollment_label') }}
            </span>
            <span class="mt-1 block text-sm text-ink-muted">
              {{ $t('admin.settings.student_enrollment_hint') }}
            </span>
          </span>
        </label>

        <p
          v-if="updatedAt"
          class="mt-3 text-xs text-ink-muted"
        >
          {{ $t('admin.settings.updated_at', { date: formatDate(updatedAt) }) }}
        </p>

        <div class="mt-4">
          <Button
            :disabled="saving"
            @click="save"
          >
            {{ saving ? $t('admin.settings.saving') : $t('admin.settings.save') }}
          </Button>
        </div>
      </template>
    </div>
  </div>
</template>
