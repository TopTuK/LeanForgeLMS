<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { updateProfile, uploadAvatar, deleteAvatar } from '@/services/profileService';

const authStore = useAuthStore();

const form = reactive({ firstName: '', lastName: '' });
const fieldErrors = ref({});
const isSaving = ref(false);
const isSaved = ref(false);
const errorMessage = ref('');

const avatarInput = ref(null);
const isUpdatingAvatar = ref(false);
const avatarError = ref('');

const email = computed(() => authStore.user?.email ?? '');

const ROLE_I18N_KEYS = {
  Student: 'student',
  Instructor: 'instructor',
  CourseCreator: 'course_creator',
  Admin: 'admin',
  None: 'none',
};
const roleKey = computed(() => ROLE_I18N_KEYS[authStore.user?.role] ?? 'student');

const displayName = computed(() => {
  const first = form.firstName.trim();
  const last = form.lastName.trim();
  if (first || last) return [first, last].filter(Boolean).join(' ');
  return authStore.user?.email ?? '';
});

const initials = computed(() => {
  const first = (form.firstName || authStore.user?.firstName || '').trim();
  const last = (form.lastName || authStore.user?.lastName || '').trim();
  const a = first.charAt(0);
  const b = last.charAt(0);
  if (a || b) return `${a}${b}`.toUpperCase();
  const emailChar = (email.value || '?').charAt(0);
  return emailChar.toUpperCase();
});

function syncFormFromUser() {
  form.firstName = authStore.user?.firstName ?? '';
  form.lastName = authStore.user?.lastName ?? '';
}

onMounted(async () => {
  if (!authStore.user) {
    await authStore.fetchUser();
    await authStore.refreshAvatar();
  }
  syncFormFromUser();
});

async function onAvatarFileSelected(event) {
  const file = event.target.files?.[0];
  event.target.value = '';
  if (!file) return;

  avatarError.value = '';
  isUpdatingAvatar.value = true;
  try {
    const updated = await uploadAvatar(file);
    authStore.updateUser(updated);
    await authStore.refreshAvatar();
  } catch (err) {
    avatarError.value = err.response?.data?.errors?.file?.[0] ?? 'profile.avatar_error_generic';
  } finally {
    isUpdatingAvatar.value = false;
  }
}

async function onRemoveAvatar() {
  avatarError.value = '';
  isUpdatingAvatar.value = true;
  try {
    const updated = await deleteAvatar();
    authStore.updateUser(updated);
    await authStore.refreshAvatar();
  } catch {
    avatarError.value = 'profile.avatar_error_generic';
  } finally {
    isUpdatingAvatar.value = false;
  }
}

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
    <div class="profile-page__curve profile-page__curve--top" aria-hidden="true" />
    <div class="profile-page__curve profile-page__curve--bottom" aria-hidden="true" />
    <div class="profile-page__square profile-page__square--one" aria-hidden="true" />
    <div class="profile-page__square profile-page__square--two" aria-hidden="true" />

    <div class="container mx-auto px-6 py-12 md:py-20 relative z-10">
      <header class="profile-heading">
        <div class="profile-heading__copy">
          <span class="profile-heading__eyebrow">{{ $t('profile.eyebrow') }}</span>
          <h1>{{ $t('profile.title') }}</h1>
          <p>{{ $t('profile.subtitle') }}</p>
        </div>
        <div class="profile-heading__meta" aria-hidden="true">
          <span>LF—PROFILE</span>
          <span>A.02</span>
        </div>
      </header>

      <div class="profile-grid">
        <aside class="profile-panel profile-panel--identity">
          <span class="profile-panel__index">01</span>
          <span class="profile-panel__label">{{ $t('profile.identity_label') }}</span>

          <div class="profile-avatar">
            <div
              class="profile-avatar__frame"
              :class="{ 'profile-avatar__frame--busy': isUpdatingAvatar }"
            >
              <img
                v-if="authStore.avatarUrl"
                :src="authStore.avatarUrl"
                alt=""
                class="profile-avatar__image"
              >
              <span
                v-else
                class="profile-avatar__fallback"
              >{{ initials }}</span>
            </div>

            <div class="profile-avatar__meta">
              <strong>{{ displayName || $t('profile.title') }}</strong>
              <span>{{ email }}</span>
              <span class="profile-role-badge">{{ $t(`profile.roles.${roleKey}`) }}</span>
            </div>
          </div>

          <div class="profile-avatar__actions">
            <button
              type="button"
              class="profile-btn profile-btn--secondary"
              :disabled="isUpdatingAvatar"
              @click="avatarInput.click()"
            >
              {{ $t('profile.avatar_change') }}
            </button>
            <button
              type="button"
              class="profile-btn profile-btn--ghost"
              :disabled="isUpdatingAvatar || !authStore.avatarUrl"
              @click="onRemoveAvatar"
            >
              {{ $t('profile.avatar_remove') }}
            </button>
            <input
              ref="avatarInput"
              type="file"
              accept="image/png,image/jpeg,image/webp"
              class="profile-avatar__input"
              @change="onAvatarFileSelected"
            >
          </div>

          <p class="profile-avatar__hint">
            {{ $t('profile.avatar_hint') }}
          </p>
          <span
            v-if="avatarError"
            class="profile-field__error"
          >{{ $t(avatarError) }}</span>
        </aside>

        <div class="profile-panel profile-panel--details">
          <span class="profile-panel__index">02</span>
          <span class="profile-panel__label">{{ $t('profile.details_label') }}</span>

          <form
            class="profile-form"
            @submit.prevent="onSave"
          >
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

            <div class="profile-field-row">
              <div class="profile-field">
                <label for="profile-first-name">{{ $t('profile.first_name_label') }}</label>
                <input
                  id="profile-first-name"
                  v-model="form.firstName"
                  type="text"
                  maxlength="100"
                  required
                  autocomplete="given-name"
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
                  autocomplete="family-name"
                >
                <span
                  v-if="fieldErrors.LastName"
                  class="profile-field__error"
                >{{ fieldErrors.LastName[0] }}</span>
              </div>
            </div>

            <div class="profile-form__footer">
              <p
                v-if="errorMessage"
                class="profile-form__status profile-form__status--error"
              >
                {{ $t(errorMessage) }}
              </p>
              <p
                v-else-if="isSaved"
                class="profile-form__status profile-form__status--ok"
              >
                {{ $t('profile.saved_confirmation') }}
              </p>
              <p
                v-else
                class="profile-form__status"
              >
                {{ $t('profile.save_hint') }}
              </p>

              <button
                type="submit"
                class="profile-btn profile-btn--primary"
                :disabled="isSaving"
              >
                <span>{{ isSaving ? $t('profile.saving') : $t('profile.save') }}</span>
                <span
                  class="profile-btn__arrow"
                  aria-hidden="true"
                >→</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.profile-page {
  position: relative;
  min-height: calc(100vh - 4.5rem);
  overflow: hidden;
  isolation: isolate;
  background-color: var(--color-surface-950);
  background-image:
    linear-gradient(var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(
      115deg,
      var(--industrial-hero-start) 0%,
      var(--industrial-hero-middle) 58%,
      var(--industrial-hero-end) 100%
    );
  background-size: 40px 40px, 40px 40px, auto;
}

.profile-heading {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 2rem;
  margin-bottom: 2.5rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid var(--industrial-line);
}

.profile-heading__eyebrow {
  display: inline-block;
  margin-bottom: 0.75rem;
  color: var(--color-accent-coral);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.profile-heading h1 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(2rem, 5vw, 3.25rem);
  font-weight: 800;
  letter-spacing: -0.04em;
  line-height: 1.05;
}

.profile-heading p {
  max-width: 32rem;
  margin: 0.85rem 0 0;
  color: var(--color-ink-muted);
  font-size: 1rem;
  line-height: 1.65;
}

.profile-heading__meta {
  display: none;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.35rem;
  color: var(--color-ink-faint);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  white-space: nowrap;
}

.profile-grid {
  display: grid;
  gap: 1.25rem;
  grid-template-columns: minmax(0, 1fr);
}

.profile-panel {
  position: relative;
  padding: 1.75rem 1.5rem 1.5rem;
  overflow: hidden;
  background: var(--industrial-panel);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  box-shadow: 0 18px 50px rgba(20, 20, 20, 0.06);
  backdrop-filter: blur(12px);
  animation: profile-rise 0.45s ease both;
}

.profile-panel--details {
  animation-delay: 0.08s;
}

.profile-panel::before {
  content: "";
  position: absolute;
  width: 7rem;
  height: 7rem;
  top: -3.5rem;
  right: -2.5rem;
  border: 1px solid var(--industrial-line-strong);
  transform: rotate(18deg);
  transition: transform 0.35s ease;
  pointer-events: none;
}

.profile-panel:hover::before {
  transform: rotate(28deg) scale(1.06);
}

.profile-panel__index {
  position: absolute;
  top: 1.1rem;
  right: 1.25rem;
  color: var(--color-ink-faint);
  font-size: 0.65rem;
  font-weight: 700;
  letter-spacing: 0.14em;
}

.profile-panel__label {
  display: block;
  margin-bottom: 1.5rem;
  color: var(--color-ink-muted);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.profile-avatar {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 1.25rem;
  margin-bottom: 1.5rem;
}

.profile-avatar__frame {
  position: relative;
  display: grid;
  width: 8.5rem;
  height: 8.5rem;
  flex-shrink: 0;
  place-items: center;
  overflow: hidden;
  border-radius: 999px;
  background:
    linear-gradient(135deg, var(--industrial-accent-wash), transparent 60%),
    var(--color-surface-900);
  border: 1px solid var(--industrial-line-strong);
}

.profile-avatar__frame--busy {
  opacity: 0.65;
}

.profile-avatar__image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  object-position: center;
}

.profile-avatar__fallback {
  color: var(--color-accent-coral);
  font-size: 2rem;
  font-weight: 800;
  letter-spacing: 0.04em;
}

.profile-avatar__meta {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 0.25rem;
}

.profile-avatar__meta strong {
  color: var(--color-ink);
  font-size: 1.2rem;
  font-weight: 750;
  letter-spacing: -0.02em;
  word-break: break-word;
}

.profile-avatar__meta span {
  color: var(--color-ink-muted);
  font-size: 0.85rem;
  word-break: break-all;
}

.profile-avatar__meta .profile-role-badge {
  display: inline-flex;
  align-self: flex-start;
  margin-top: 0.15rem;
  padding: 0.2rem 0.65rem;
  border-radius: var(--radius-pill);
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  word-break: normal;
}

.profile-avatar__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.65rem;
}

.profile-avatar__input {
  display: none;
}

.profile-avatar__hint {
  margin: 1rem 0 0;
  color: var(--color-ink-faint);
  font-size: 0.75rem;
  line-height: 1.5;
}

.profile-form {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.profile-field-row {
  display: grid;
  gap: 1rem;
  grid-template-columns: minmax(0, 1fr);
}

.profile-field {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  margin-bottom: 1.15rem;
}

.profile-field label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.profile-field input {
  padding: 0.8rem 0.95rem;
  color: var(--color-ink);
  background: var(--color-surface-950);
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.35rem;
  font-size: 0.95rem;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.profile-field input:disabled {
  color: var(--color-ink-faint);
  background: var(--color-surface-900);
  cursor: not-allowed;
}

.profile-field input:focus {
  outline: none;
  border-color: var(--color-accent-coral);
  box-shadow: 0 0 0 3px var(--industrial-accent-wash);
}

.profile-field__error {
  display: block;
  margin-top: 0.35rem;
  color: var(--color-accent-coral);
  font-size: 0.8rem;
}

.profile-form__footer {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-top: 0.5rem;
  padding-top: 1.25rem;
  border-top: 1px solid var(--industrial-line);
}

.profile-form__status {
  margin: 0;
  color: var(--color-ink-faint);
  font-size: 0.82rem;
  line-height: 1.45;
}

.profile-form__status--error {
  color: var(--color-accent-coral);
}

.profile-form__status--ok {
  color: var(--color-ink-muted);
}

.profile-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.55rem;
  padding: 0.7rem 1.15rem;
  border: 1px solid transparent;
  border-radius: 0.35rem;
  font-size: 0.82rem;
  font-weight: 700;
  letter-spacing: 0.02em;
  cursor: pointer;
  transition:
    transform 0.15s ease,
    background-color 0.15s ease,
    border-color 0.15s ease,
    color 0.15s ease,
    box-shadow 0.15s ease;
}

.profile-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
  transform: none;
}

.profile-btn--primary {
  color: #ffffff;
  background: var(--color-accent-coral);
  border-color: var(--color-accent-coral);
  box-shadow: 0 8px 20px rgba(236, 104, 60, 0.22);
}

.profile-btn--primary:hover:not(:disabled) {
  background: var(--color-accent-coral-dark);
  transform: translateY(-1px);
  box-shadow: 0 12px 26px rgba(236, 104, 60, 0.28);
}

.profile-btn--primary:hover:not(:disabled) .profile-btn__arrow {
  transform: translateX(3px);
}

.profile-btn__arrow {
  display: inline-block;
  transition: transform 0.15s ease;
}

.profile-btn--secondary {
  color: var(--color-ink);
  background: var(--color-surface-950);
  border-color: var(--color-border-subtle);
}

.profile-btn--secondary:hover:not(:disabled) {
  border-color: var(--color-ink-faint);
}

.profile-btn--ghost {
  color: var(--color-ink-muted);
  background: transparent;
  border-color: transparent;
}

.profile-btn--ghost:hover:not(:disabled) {
  color: var(--color-ink);
  border-color: var(--color-border-subtle);
}

.profile-page__curve,
.profile-page__square {
  position: absolute;
  pointer-events: none;
}

.profile-page__curve {
  border: 1px solid var(--industrial-line);
  border-radius: 50%;
}

.profile-page__curve--top {
  width: 28rem;
  height: 28rem;
  top: -22rem;
  right: -6rem;
  box-shadow: 0 0 0 32px var(--industrial-accent-wash);
  animation: profile-drift 12s ease-in-out infinite alternate;
}

.profile-page__curve--bottom {
  width: 20rem;
  height: 20rem;
  bottom: -15rem;
  left: -7rem;
  box-shadow: 0 0 0 24px var(--industrial-grid);
}

.profile-page__square {
  border: 1px solid rgba(236, 104, 60, 0.34);
}

.profile-page__square--one {
  width: 4.5rem;
  height: 4.5rem;
  top: 18%;
  left: 4%;
  transform: rotate(16deg);
  animation: profile-spin 18s linear infinite;
}

.profile-page__square--two {
  width: 2.25rem;
  height: 2.25rem;
  right: 6%;
  bottom: 16%;
  background: var(--industrial-accent-wash);
  transform: rotate(-12deg);
}

@keyframes profile-rise {
  from {
    opacity: 0;
    transform: translateY(12px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes profile-drift {
  from {
    transform: translate(0, 0);
  }
  to {
    transform: translate(-12px, 10px);
  }
}

@keyframes profile-spin {
  from {
    transform: rotate(16deg);
  }
  to {
    transform: rotate(376deg);
  }
}

@media (min-width: 768px) {
  .profile-heading__meta {
    display: flex;
  }

  .profile-grid {
    grid-template-columns: minmax(16rem, 22rem) minmax(0, 1fr);
    align-items: start;
  }

  .profile-panel {
    padding: 2rem 1.75rem 1.75rem;
  }

  .profile-field-row {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .profile-avatar {
    flex-direction: row;
    align-items: center;
  }
}

@media (max-width: 767px) {
  .profile-page__square--one {
    left: -1.5rem;
  }

  .profile-form__footer {
    align-items: stretch;
  }

  .profile-form__footer .profile-btn--primary {
    width: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  .profile-panel,
  .profile-page__curve--top,
  .profile-page__square--one {
    animation: none;
  }
}
</style>
