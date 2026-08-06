<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { updateProfile } from '@/services/profileService';

const authStore = useAuthStore();

const form = reactive({ firstName: '', lastName: '' });
const fieldErrors = ref({});
const isSaving = ref(false);
const isSaved = ref(false);
const errorMessage = ref('');

const email = computed(() => authStore.user?.email ?? '');

function syncFormFromUser() {
  form.firstName = authStore.user?.firstName ?? '';
  form.lastName = authStore.user?.lastName ?? '';
}

onMounted(async () => {
  if (!authStore.user) {
    await authStore.fetchUser();
  }
  syncFormFromUser();
});

async function onSave() {
  isSaving.value = true;
  isSaved.value = false;
  errorMessage.value = '';
  fieldErrors.value = {};

  try {
    const updated = await updateProfile({ firstName: form.firstName, lastName: form.lastName });
    authStore.updateUser(updated);
    syncFormFromUser();
    isSaved.value = true;
  } catch (err) {
    const errors = err.response?.data?.errors;
    if (err.response?.status === 400 && errors) {
      fieldErrors.value = errors;
    } else {
      errorMessage.value = 'profile.error_generic';
    }
  } finally {
    isSaving.value = false;
  }
}
</script>

<template>
  <section class="profile-page">
    <div class="container mx-auto px-6 py-16 md:py-24 relative z-10">
      <div class="profile-card">
        <h1>{{ $t('profile.title') }}</h1>

        <form @submit.prevent="onSave">
          <div class="profile-field">
            <label for="profile-email">{{ $t('profile.email_label') }}</label>
            <input
              id="profile-email"
              type="email"
              :value="email"
              disabled
              readonly
            >
          </div>

          <div class="profile-field">
            <label for="profile-first-name">{{ $t('profile.first_name_label') }}</label>
            <input
              id="profile-first-name"
              v-model="form.firstName"
              type="text"
              maxlength="100"
              required
            >
            <span
              v-if="fieldErrors.FirstName"
              class="profile-field__error"
            >{{ fieldErrors.FirstName[0] }}</span>
          </div>

          <div class="profile-field">
            <label for="profile-last-name">{{ $t('profile.last_name_label') }}</label>
            <input
              id="profile-last-name"
              v-model="form.lastName"
              type="text"
              maxlength="100"
            >
            <span
              v-if="fieldErrors.LastName"
              class="profile-field__error"
            >{{ fieldErrors.LastName[0] }}</span>
          </div>

          <p
            v-if="errorMessage"
            class="profile-page__error"
          >
            {{ $t(errorMessage) }}
          </p>
          <p
            v-if="isSaved"
            class="profile-page__saved"
          >
            {{ $t('profile.saved_confirmation') }}
          </p>

          <button
            type="submit"
            class="profile-save"
            :disabled="isSaving"
          >
            {{ $t('profile.save') }}
          </button>
        </form>
      </div>
    </div>
  </section>
</template>

<style scoped>
.profile-page {
    min-height: calc(100vh - 4.5rem);
    background-color: var(--color-surface-950);
}

.profile-card {
    max-width: 32rem;
    margin: 0 auto;
    padding: 2.5rem;
    background: var(--color-surface-900);
    border: 1px solid var(--color-border-subtle);
    border-radius: var(--radius-card);
}

.profile-card h1 {
    margin: 0 0 2rem;
    color: var(--color-ink);
    font-size: 1.75rem;
    font-weight: 800;
    letter-spacing: -0.02em;
}

.profile-field {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    margin-bottom: 1.25rem;
}

.profile-field label {
    color: var(--color-ink-muted);
    font-size: 0.85rem;
    font-weight: 600;
}

.profile-field input {
    padding: 0.65rem 0.9rem;
    color: var(--color-ink);
    background: var(--color-surface-950);
    border: 1px solid var(--color-border-subtle);
    border-radius: 0.5rem;
    font-size: 0.95rem;
}

.profile-field input:disabled {
    color: var(--color-ink-faint);
    cursor: not-allowed;
}

.profile-field input:focus {
    outline: none;
    border-color: var(--color-accent-coral);
}

.profile-field__error {
    color: var(--color-accent-coral);
    font-size: 0.8rem;
}

.profile-page__error {
    margin: 0 0 1rem;
    color: var(--color-accent-coral);
    font-size: 0.85rem;
}

.profile-page__saved {
    margin: 0 0 1rem;
    color: var(--color-ink-muted);
    font-size: 0.85rem;
}

.profile-save {
    display: inline-flex;
    align-items: center;
    padding: 0.65rem 1.4rem;
    color: #ffffff;
    background: var(--color-accent-coral);
    border: 1px solid var(--color-accent-coral);
    border-radius: 999px;
    font-size: 0.875rem;
    font-weight: 700;
    cursor: pointer;
    transition: transform 0.15s ease, background-color 0.15s ease;
}

.profile-save:hover:not(:disabled) {
    background: var(--color-accent-coral-dark);
    transform: translateY(-1px);
}

.profile-save:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}
</style>
