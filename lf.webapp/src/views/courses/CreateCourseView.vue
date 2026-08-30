<script setup>
import { computed, onMounted, onBeforeUnmount, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { fetchCategories, fetchCourses, createCourse, uploadCourseCoverImage } from '@/services/courseService';
import FormField from '@/components/courses/form/FormField.vue';
import RichEditor from '@/components/courses/form/RichEditor.vue';
import StudioShell from '@/components/courses/studio/StudioShell.vue';
import StudioButton from '@/components/courses/studio/StudioButton.vue';

const { t } = useI18n();
const router = useRouter();

const COVER_COLORS = ['Coral', 'Ocean', 'Forest', 'Amber', 'Slate', 'Berry'];

const title = ref('');
const shortIntroduction = ref('');
const description = ref('');
const category = ref(null);
const categories = ref([]);

const pricingType = ref('Free');
const price = ref(null);
const enrollmentMode = ref('Open');

const coverMode = ref('Color');
const coverColor = ref(COVER_COLORS[0]);
const coverImagePreviewUrl = ref('');
const coverImageStorageObjectId = ref(null);
const coverImageUploading = ref(false);
const coverImageError = ref('');

const submitting = ref(false);
const errorMessage = ref('');

const drafts = ref([]);
const draftsLoading = ref(false);
const draftsError = ref('');

function descriptionHasText(html) {
  return html.replace(/<[^>]*>/g, '').replace(/&nbsp;/g, ' ').trim().length > 0;
}

async function loadCategories() {
  try {
    categories.value = await fetchCategories();
  } catch {
    errorMessage.value = t('courses.create.load_error');
  }
}

async function loadDrafts() {
  draftsLoading.value = true;
  draftsError.value = '';
  try {
    const result = await fetchCourses({ page: 1, pageSize: 50 });
    drafts.value = result.items.filter((c) => !c.isPublished);
  } catch {
    draftsError.value = t('courses.create.load_error');
  } finally {
    draftsLoading.value = false;
  }
}

onMounted(() => {
  loadCategories();
  loadDrafts();
});

onBeforeUnmount(() => {
  if (coverImagePreviewUrl.value) URL.revokeObjectURL(coverImagePreviewUrl.value);
});

async function onCoverImageSelected(event) {
  const file = event.target.files?.[0];
  if (!file) return;

  coverImageError.value = '';
  coverImageStorageObjectId.value = null;
  if (coverImagePreviewUrl.value) URL.revokeObjectURL(coverImagePreviewUrl.value);

  coverImagePreviewUrl.value = URL.createObjectURL(file);
  coverImageUploading.value = true;

  try {
    const uploaded = await uploadCourseCoverImage(file);
    coverImageStorageObjectId.value = uploaded.storageObjectId;
  } catch {
    coverImageError.value = t('courses.create.cover_image_upload_error');
  } finally {
    coverImageUploading.value = false;
  }
}

const priceIsValid = computed(() => pricingType.value === 'Free' || Number(price.value) > 0);

const canSubmit = computed(() =>
  Boolean(
    title.value.trim()
    && shortIntroduction.value.trim()
    && descriptionHasText(description.value)
    && category.value
    && priceIsValid.value
    && (coverMode.value === 'Color' ? coverColor.value : coverImageStorageObjectId.value),
  ),
);

async function submit() {
  errorMessage.value = '';

  if (!canSubmit.value) {
    errorMessage.value = t('courses.create.validation_error');
    return;
  }

  submitting.value = true;
  try {
    const course = await createCourse({
      title: title.value,
      shortIntroduction: shortIntroduction.value,
      description: description.value,
      categoryId: category.value,
      pricingType: pricingType.value,
      price: pricingType.value === 'Paid' ? Number(price.value) : null,
      enrollmentMode: enrollmentMode.value,
      coverType: coverMode.value,
      coverColor: coverMode.value === 'Color' ? coverColor.value : null,
      coverImageStorageObjectId: coverMode.value === 'Image' ? coverImageStorageObjectId.value : null,
    });
    router.push({ name: 'CourseEdit', params: { id: course.id } });
  } catch (err) {
    errorMessage.value = err.response?.status === 400
      ? t('courses.create.validation_error')
      : t('courses.create.load_error');
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <StudioShell>
    <header class="create-header">
      <router-link
        :to="{ name: 'CoursesAvailable' }"
        class="create-header__back"
      >
        {{ $t('courses.create.back') }}
      </router-link>
      <h1>{{ $t('courses.create.title') }}</h1>
      <p class="create-header__subtitle">
        {{ $t('courses.create.subtitle') }}
      </p>
    </header>

    <div
      v-if="errorMessage"
      class="create-alert"
      role="alert"
    >
      <span>{{ errorMessage }}</span>
      <button
        type="button"
        class="create-alert__close"
        :aria-label="$t('courses.create.dismiss_error')"
        @click="errorMessage = ''"
      >
        ×
      </button>
    </div>

    <div class="create-layout">
      <form
        class="create-form"
        @submit.prevent="submit"
      >
        <FormField
          v-model="title"
          :label="$t('courses.create.field_title')"
          required
        />
        <FormField
          v-model="shortIntroduction"
          type="textarea"
          :rows="3"
          :label="$t('courses.create.field_short_introduction')"
          required
        />

        <div class="create-field">
          <span class="create-field__label">{{ $t('courses.create.field_description') }}</span>
          <RichEditor
            v-model="description"
            :placeholder="$t('courses.create.field_description')"
            :allow-image="false"
          />
        </div>

        <fieldset class="create-fieldset">
          <legend>{{ $t('courses.create.field_category') }}</legend>
          <p
            v-if="!categories.length"
            class="create-hint"
          >
            {{ $t('courses.create.category_placeholder') }}
          </p>
          <div
            v-else
            class="create-chips"
            role="listbox"
            :aria-label="$t('courses.create.field_category')"
          >
            <button
              v-for="item in categories"
              :key="item.id"
              type="button"
              class="create-chip"
              role="option"
              :aria-selected="category === item.id"
              :class="{ 'is-active': category === item.id }"
              @click="category = item.id"
            >
              {{ item.name }}
            </button>
          </div>
        </fieldset>

        <fieldset class="create-fieldset">
          <legend>{{ $t('courses.create.field_pricing') }}</legend>
          <div
            class="create-chips"
            role="listbox"
            :aria-label="$t('courses.create.field_pricing')"
          >
            <button
              v-for="option in ['Free', 'Paid']"
              :key="option"
              type="button"
              class="create-chip"
              role="option"
              :aria-selected="pricingType === option"
              :class="{ 'is-active': pricingType === option }"
              @click="pricingType = option"
            >
              {{ option === 'Free' ? $t('courses.create.pricing_free') : $t('courses.create.pricing_paid') }}
            </button>
          </div>
          <label
            v-if="pricingType === 'Paid'"
            class="create-field"
          >
            <span class="create-field__label">{{ $t('courses.create.field_price') }}</span>
            <input
              v-model.number="price"
              type="number"
              min="1"
              step="1"
              inputmode="numeric"
              class="create-price-input"
            >
          </label>
        </fieldset>

        <fieldset class="create-fieldset">
          <legend>{{ $t('courses.create.field_enrollment_mode') }}</legend>
          <div
            class="create-chips"
            role="listbox"
            :aria-label="$t('courses.create.field_enrollment_mode')"
          >
            <button
              v-for="option in ['Open', 'Managed']"
              :key="option"
              type="button"
              class="create-chip"
              role="option"
              :aria-selected="enrollmentMode === option"
              :class="{ 'is-active': enrollmentMode === option }"
              @click="enrollmentMode = option"
            >
              {{ option === 'Open' ? $t('courses.create.mode_open') : $t('courses.create.mode_managed') }}
            </button>
          </div>
          <p class="create-hint">
            {{ enrollmentMode === 'Open' ? $t('courses.create.mode_open_hint') : $t('courses.create.mode_managed_hint') }}
          </p>
        </fieldset>

        <fieldset class="create-fieldset">
          <legend>{{ $t('courses.create.field_cover') }}</legend>
          <div
            class="create-chips"
            role="listbox"
            :aria-label="$t('courses.create.field_cover')"
          >
            <button
              type="button"
              class="create-chip"
              role="option"
              :aria-selected="coverMode === 'Color'"
              :class="{ 'is-active': coverMode === 'Color' }"
              @click="coverMode = 'Color'"
            >
              {{ $t('courses.create.cover_mode_color') }}
            </button>
            <button
              type="button"
              class="create-chip"
              role="option"
              :aria-selected="coverMode === 'Image'"
              :class="{ 'is-active': coverMode === 'Image' }"
              @click="coverMode = 'Image'"
            >
              {{ $t('courses.create.cover_mode_image') }}
            </button>
          </div>

          <div
            v-if="coverMode === 'Color'"
            class="create-swatches"
            role="listbox"
            :aria-label="$t('courses.create.cover_mode_color')"
          >
            <button
              v-for="color in COVER_COLORS"
              :key="color"
              type="button"
              class="create-swatch"
              role="option"
              :aria-selected="coverColor === color"
              :class="{ 'is-active': coverColor === color }"
              :style="{ backgroundColor: `var(--color-cover-${color.toLowerCase()})` }"
              :title="$t(`courses.create.cover_colors.${color.toLowerCase()}`)"
              @click="coverColor = color"
            />
          </div>

          <div
            v-else
            class="create-cover-image"
          >
            <label class="create-cover-upload">
              <input
                type="file"
                accept="image/png,image/jpeg,image/webp"
                class="create-cover-input"
                @change="onCoverImageSelected"
              >
              <span>{{ coverImageUploading ? $t('courses.create.cover_image_uploading') : $t('courses.create.cover_image_choose') }}</span>
            </label>
            <img
              v-if="coverImagePreviewUrl"
              :src="coverImagePreviewUrl"
              :alt="$t('courses.create.cover_image_preview_alt')"
              class="create-cover-preview"
            >
            <p
              v-if="coverImageError"
              class="create-hint create-hint--error"
            >
              {{ coverImageError }}
            </p>
          </div>
        </fieldset>

        <div class="create-actions">
          <StudioButton
            type="submit"
            variant="primary"
            :disabled="submitting"
          >
            {{ submitting ? $t('courses.create.submitting') : $t('courses.create.submit') }}
          </StudioButton>
        </div>
      </form>

      <aside class="create-rail">
        <h2>{{ $t('courses.create.your_drafts_title') }}</h2>
        <p
          v-if="draftsError"
          class="create-hint create-hint--error"
        >
          {{ draftsError }}
        </p>
        <p
          v-else-if="draftsLoading"
          class="create-hint"
        >
          {{ $t('courses.create.drafts_loading') }}
        </p>
        <p
          v-else-if="drafts.length === 0"
          class="create-hint"
        >
          {{ $t('courses.create.your_drafts_empty') }}
        </p>
        <ul
          v-else
          class="create-drafts"
        >
          <li
            v-for="draft in drafts"
            :key="draft.id"
          >
            <router-link
              :to="{ name: 'CourseEdit', params: { id: draft.id } }"
              class="create-draft"
            >
              <span class="create-draft__title">{{ draft.title }}</span>
              <span class="create-draft__action">{{ $t('courses.create.edit_action') }}</span>
            </router-link>
          </li>
        </ul>
      </aside>
    </div>
  </StudioShell>
</template>

<style scoped>
.create-header {
  margin-bottom: 1.75rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid var(--color-border-subtle);
}

.create-header__back {
  display: inline-block;
  margin-bottom: 0.65rem;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
  font-weight: 600;
  text-decoration: none;
}

.create-header__back:hover {
  color: var(--color-ink);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.create-header h1 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.6rem, 3vw, 2.1rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.15;
}

.create-header__subtitle {
  max-width: 36rem;
  margin: 0.65rem 0 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
  line-height: 1.6;
}

.create-alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.15rem;
  padding: 0.8rem 1rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.6rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral-dark);
  font-size: 0.9rem;
  font-weight: 600;
}

.create-alert__close {
  border: 0;
  background: transparent;
  color: inherit;
  font-size: 1.2rem;
  cursor: pointer;
}

.create-layout {
  display: grid;
  gap: 1.75rem;
}

@media (min-width: 960px) {
  .create-layout {
    grid-template-columns: minmax(0, 1.5fr) minmax(14rem, 0.7fr);
    align-items: start;
  }
}

.create-form {
  display: flex;
  flex-direction: column;
  gap: 1.15rem;
}

.create-field {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.create-field__label {
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 600;
}

.create-fieldset {
  margin: 0;
  padding: 0;
  border: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.create-fieldset legend {
  margin-bottom: 0.15rem;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 600;
  padding: 0;
}

.create-hint {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
}

.create-hint--error {
  color: var(--color-accent-coral-dark);
}

.create-price-input {
  width: 12rem;
  padding: 0.5rem 0.7rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.45rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  font: inherit;
}

.create-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
}

.create-chip {
  padding: 0.4rem 0.75rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.45rem;
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
  font: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
}

.create-chip:hover {
  color: var(--color-ink);
  background: var(--color-surface-900);
}

.create-chip.is-active {
  border-color: transparent;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral-dark);
}

.create-swatches {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.create-swatch {
  width: 2rem;
  height: 2rem;
  padding: 0;
  border: 2px solid transparent;
  border-radius: 999px;
  cursor: pointer;
}

.create-swatch.is-active {
  border-color: var(--color-ink);
  outline: 2px solid var(--color-surface-950);
  outline-offset: -4px;
}

.create-cover-image {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.create-cover-upload {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: fit-content;
  padding: 0.55rem 0.9rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.5rem;
  color: var(--color-ink-muted);
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
}

.create-cover-upload:hover {
  color: var(--color-ink);
  background: var(--color-surface-900);
}

.create-cover-input {
  position: absolute;
  width: 1px;
  height: 1px;
  opacity: 0;
  overflow: hidden;
}

.create-cover-preview {
  max-width: 16rem;
  max-height: 10rem;
  object-fit: cover;
  border-radius: 0.5rem;
  border: 1px solid var(--color-border-subtle);
}

.create-actions {
  padding-top: 0.35rem;
}

.create-rail {
  padding: 1.15rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.75rem;
  background: var(--color-surface-900);
}

.create-rail h2 {
  margin: 0 0 0.85rem;
  color: var(--color-ink);
  font-size: 0.95rem;
  font-weight: 700;
}

.create-drafts {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.create-draft {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.55rem 0.65rem;
  border-radius: 0.45rem;
  color: var(--color-ink);
  text-decoration: none;
}

.create-draft:hover {
  background: var(--color-surface-950);
}

.create-draft__title {
  font-size: 0.88rem;
  font-weight: 600;
  overflow-wrap: anywhere;
}

.create-draft__action {
  flex-shrink: 0;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 600;
}
</style>
