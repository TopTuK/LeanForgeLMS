<script setup>
import { computed, onBeforeUnmount, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { uploadCourseCoverImage } from '@/services/courseService';
import FormField from '@/components/courses/form/FormField.vue';
import RichEditor from '@/components/courses/form/RichEditor.vue';
import StudioButton from '@/components/courses/studio/StudioButton.vue';

const COVER_COLORS = ['Coral', 'Ocean', 'Forest', 'Amber', 'Slate', 'Berry'];

// { title, shortIntroduction, description, categoryId, pricingType, price, enrollmentMode,
//   coverType, coverColor, coverImageStorageObjectId }
const details = defineModel({ type: Object, required: true });

const props = defineProps({
  categories: { type: Array, default: () => [] },
  submitting: { type: Boolean, default: false },
  submitLabel: { type: String, required: true },
  submittingLabel: { type: String, required: true },
  // Lets an Image cover stay valid without a fresh upload when editing a course that already has one.
  existingCoverImageUrl: { type: String, default: '' },
  hasExistingCoverImage: { type: Boolean, default: false },
});

const emit = defineEmits(['submit', 'invalid']);

const { t } = useI18n();

const uploadPreviewUrl = ref('');
const coverImageUploading = ref(false);
const coverImageError = ref('');

const coverPreviewUrl = computed(() => uploadPreviewUrl.value || props.existingCoverImageUrl);

onBeforeUnmount(() => {
  if (uploadPreviewUrl.value) URL.revokeObjectURL(uploadPreviewUrl.value);
});

function descriptionHasText(html) {
  return (html ?? '').replace(/<[^>]*>/g, '').replace(/&nbsp;/g, ' ').trim().length > 0;
}

function clearUploadPreview() {
  if (uploadPreviewUrl.value) URL.revokeObjectURL(uploadPreviewUrl.value);
  uploadPreviewUrl.value = '';
}

async function handleCoverImageSelected(event) {
  const file = event.target.files?.[0];
  if (!file) return;

  coverImageError.value = '';
  details.value.coverImageStorageObjectId = null;
  clearUploadPreview();

  uploadPreviewUrl.value = URL.createObjectURL(file);
  coverImageUploading.value = true;

  try {
    const uploaded = await uploadCourseCoverImage(file);
    details.value.coverImageStorageObjectId = uploaded.storageObjectId;
  } catch {
    clearUploadPreview();
    coverImageError.value = t('courses.create.cover_image_upload_error');
  } finally {
    coverImageUploading.value = false;
  }
}

const priceIsValid = computed(() => details.value.pricingType === 'Free' || Number(details.value.price) > 0);

const coverIsValid = computed(() => {
  if (details.value.coverType === 'Color') return Boolean(details.value.coverColor);
  if (coverImageUploading.value) return false;
  return Boolean(details.value.coverImageStorageObjectId || props.hasExistingCoverImage);
});

const canSubmit = computed(() =>
  Boolean(
    details.value.title?.trim()
    && details.value.shortIntroduction?.trim()
    && descriptionHasText(details.value.description)
    && details.value.categoryId
    && priceIsValid.value
    && coverIsValid.value,
  ),
);

function handleSubmit() {
  if (!canSubmit.value) {
    emit('invalid');
    return;
  }
  emit('submit');
}
</script>

<template>
  <form
    class="details-form"
    @submit.prevent="handleSubmit"
  >
    <FormField
      v-model="details.title"
      :label="$t('courses.create.field_title')"
      required
    />
    <FormField
      v-model="details.shortIntroduction"
      type="textarea"
      :rows="3"
      :label="$t('courses.create.field_short_introduction')"
      required
    />

    <div class="details-field">
      <span class="details-field__label">{{ $t('courses.create.field_description') }}</span>
      <RichEditor
        v-model="details.description"
        :placeholder="$t('courses.create.field_description')"
        :allow-image="false"
      />
    </div>

    <fieldset class="details-fieldset">
      <legend>{{ $t('courses.create.field_category') }}</legend>
      <p
        v-if="!categories.length"
        class="details-hint"
      >
        {{ $t('courses.create.category_placeholder') }}
      </p>
      <div
        v-else
        class="details-chips"
        role="listbox"
        :aria-label="$t('courses.create.field_category')"
      >
        <button
          v-for="item in categories"
          :key="item.id"
          type="button"
          class="details-chip"
          role="option"
          :aria-selected="details.categoryId === item.id"
          :class="{ 'is-active': details.categoryId === item.id }"
          @click="details.categoryId = item.id"
        >
          {{ item.name }}
        </button>
      </div>
    </fieldset>

    <fieldset class="details-fieldset">
      <legend>{{ $t('courses.create.field_pricing') }}</legend>
      <div
        class="details-chips"
        role="listbox"
        :aria-label="$t('courses.create.field_pricing')"
      >
        <button
          v-for="option in ['Free', 'Paid']"
          :key="option"
          type="button"
          class="details-chip"
          role="option"
          :aria-selected="details.pricingType === option"
          :class="{ 'is-active': details.pricingType === option }"
          @click="details.pricingType = option"
        >
          {{ option === 'Free' ? $t('courses.create.pricing_free') : $t('courses.create.pricing_paid') }}
        </button>
      </div>
      <label
        v-if="details.pricingType === 'Paid'"
        class="details-field"
      >
        <span class="details-field__label">{{ $t('courses.create.field_price') }}</span>
        <input
          v-model.number="details.price"
          type="number"
          min="1"
          step="1"
          inputmode="numeric"
          class="details-price-input"
        >
      </label>
    </fieldset>

    <fieldset class="details-fieldset">
      <legend>{{ $t('courses.create.field_enrollment_mode') }}</legend>
      <div
        class="details-chips"
        role="listbox"
        :aria-label="$t('courses.create.field_enrollment_mode')"
      >
        <button
          v-for="option in ['Open', 'Managed']"
          :key="option"
          type="button"
          class="details-chip"
          role="option"
          :aria-selected="details.enrollmentMode === option"
          :class="{ 'is-active': details.enrollmentMode === option }"
          @click="details.enrollmentMode = option"
        >
          {{ option === 'Open' ? $t('courses.create.mode_open') : $t('courses.create.mode_managed') }}
        </button>
      </div>
      <p class="details-hint">
        {{ details.enrollmentMode === 'Open' ? $t('courses.create.mode_open_hint') : $t('courses.create.mode_managed_hint') }}
      </p>
    </fieldset>

    <fieldset class="details-fieldset">
      <legend>{{ $t('courses.create.field_cover') }}</legend>
      <div
        class="details-chips"
        role="listbox"
        :aria-label="$t('courses.create.field_cover')"
      >
        <button
          type="button"
          class="details-chip"
          role="option"
          :aria-selected="details.coverType === 'Color'"
          :class="{ 'is-active': details.coverType === 'Color' }"
          @click="details.coverType = 'Color'"
        >
          {{ $t('courses.create.cover_mode_color') }}
        </button>
        <button
          type="button"
          class="details-chip"
          role="option"
          :aria-selected="details.coverType === 'Image'"
          :class="{ 'is-active': details.coverType === 'Image' }"
          @click="details.coverType = 'Image'"
        >
          {{ $t('courses.create.cover_mode_image') }}
        </button>
      </div>

      <div
        v-if="details.coverType === 'Color'"
        class="details-swatches"
        role="listbox"
        :aria-label="$t('courses.create.cover_mode_color')"
      >
        <button
          v-for="color in COVER_COLORS"
          :key="color"
          type="button"
          class="details-swatch"
          role="option"
          :aria-selected="details.coverColor === color"
          :aria-label="$t(`courses.create.cover_colors.${color.toLowerCase()}`)"
          :class="{ 'is-active': details.coverColor === color }"
          :style="{ backgroundColor: `var(--color-cover-${color.toLowerCase()})` }"
          :title="$t(`courses.create.cover_colors.${color.toLowerCase()}`)"
          @click="details.coverColor = color"
        />
      </div>

      <div
        v-else
        class="details-cover-image"
      >
        <label class="details-cover-upload">
          <input
            type="file"
            accept="image/png,image/jpeg,image/webp"
            class="details-cover-input"
            @change="handleCoverImageSelected"
          >
          <span>{{ coverImageUploading ? $t('courses.create.cover_image_uploading') : $t('courses.create.cover_image_choose') }}</span>
        </label>
        <img
          v-if="coverPreviewUrl"
          :src="coverPreviewUrl"
          :alt="$t('courses.create.cover_image_preview_alt')"
          class="details-cover-preview"
        >
        <p
          v-if="coverImageError"
          class="details-hint details-hint--error"
        >
          {{ coverImageError }}
        </p>
      </div>
    </fieldset>

    <div class="details-actions">
      <slot name="actions" />
      <StudioButton
        type="submit"
        variant="primary"
        :disabled="submitting"
      >
        {{ submitting ? submittingLabel : submitLabel }}
      </StudioButton>
    </div>
  </form>
</template>

<style scoped>
.details-form {
  display: flex;
  flex-direction: column;
  gap: 1.15rem;
}

.details-field {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.details-field__label {
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 600;
}

.details-fieldset {
  margin: 0;
  padding: 0;
  border: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.details-fieldset legend {
  margin-bottom: 0.15rem;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 600;
  padding: 0;
}

.details-hint {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
}

.details-hint--error {
  color: var(--color-accent-coral-dark);
}

.details-price-input {
  width: 12rem;
  padding: 0.5rem 0.7rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.45rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  font: inherit;
}

.details-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
}

.details-chip {
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

.details-chip:hover {
  color: var(--color-ink);
  background: var(--color-surface-900);
}

.details-chip.is-active {
  border-color: transparent;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral-dark);
}

.details-swatches {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.details-swatch {
  width: 2rem;
  height: 2rem;
  padding: 0;
  border: 2px solid transparent;
  border-radius: 999px;
  cursor: pointer;
}

.details-swatch.is-active {
  border-color: var(--color-ink);
  outline: 2px solid var(--color-surface-950);
  outline-offset: -4px;
}

.details-cover-image {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.details-cover-upload {
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

.details-cover-upload:hover {
  color: var(--color-ink);
  background: var(--color-surface-900);
}

.details-cover-input {
  position: absolute;
  width: 1px;
  height: 1px;
  opacity: 0;
  overflow: hidden;
}

.details-cover-preview {
  max-width: 16rem;
  max-height: 10rem;
  object-fit: cover;
  border-radius: 0.5rem;
  border: 1px solid var(--color-border-subtle);
}

.details-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
  padding-top: 0.35rem;
}
</style>
