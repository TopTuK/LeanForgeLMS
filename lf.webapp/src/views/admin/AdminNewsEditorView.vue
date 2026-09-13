<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import draggable from 'vuedraggable';
import { ArrowLeft, ChevronLeft, ChevronRight, ImagePlus, X } from 'lucide-vue-next';
import RichEditor from '@/components/courses/form/RichEditor.vue';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  createNewsPost,
  fetchAdminNewsPost,
  updateNewsPost,
  uploadNewsImage,
} from '@/services/adminService';

// Mirrors NewsPost.MaxImages and NewsImageUpload on the server; the API rejects anything past these anyway.
const MAX_IMAGES = 10;
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const ACCEPTED_TYPES = ['image/png', 'image/jpeg', 'image/webp'];

const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const postId = computed(() => (route.params.id ? Number(route.params.id) : null));
const isEdit = computed(() => postId.value !== null);

const form = reactive({
  title: '',
  html: '',
  visibility: 'Public',
  isPublished: true,
});

// Each entry: { key, storageObjectId, url, objectUrl, uploading, failed }. Images upload as soon as
// they're picked, so Save only sends the ordered storage object ids.
const images = ref([]);
let nextImageKey = 0;

const loading = ref(isEdit.value);
const saving = ref(false);
const loadError = ref('');
const formError = ref('');
const imageError = ref('');
const dragging = ref(false);
const fileInput = ref(null);

const uploadsPending = computed(() => images.value.some((image) => image.uploading));
const canAddImages = computed(() => images.value.length < MAX_IMAGES);

const visibilityOptions = computed(() => [
  { value: 'Public', label: t('admin.news.visibility_public'), hint: t('admin.news.editor.visibility_public_hint') },
  { value: 'MembersOnly', label: t('admin.news.visibility_members'), hint: t('admin.news.editor.visibility_members_hint') },
]);

async function loadPost() {
  if (!isEdit.value) return;
  loading.value = true;
  try {
    const post = await fetchAdminNewsPost(postId.value);
    form.title = post.title;
    form.html = post.html;
    form.visibility = post.visibility;
    form.isPublished = post.isPublished;
    images.value = post.images.map((image) => ({
      key: `existing-${image.id}`,
      storageObjectId: image.storageObjectId,
      url: image.url,
      objectUrl: null,
      uploading: false,
      failed: false,
    }));
  } catch {
    loadError.value = t('admin.news.editor.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(loadPost);

onBeforeUnmount(() => {
  for (const image of images.value) {
    if (image.objectUrl) URL.revokeObjectURL(image.objectUrl);
  }
});

async function upload(entry, file) {
  try {
    const result = await uploadNewsImage(file);
    entry.storageObjectId = result.storageObjectId;
  } catch {
    entry.failed = true;
  } finally {
    entry.uploading = false;
  }
}

function addFiles(fileList) {
  imageError.value = '';
  for (const file of Array.from(fileList ?? [])) {
    if (!canAddImages.value) {
      imageError.value = t('admin.news.editor.images_limit', { max: MAX_IMAGES });
      break;
    }

    if (!ACCEPTED_TYPES.includes(file.type) || file.size > MAX_IMAGE_BYTES) {
      imageError.value = t('admin.news.editor.image_invalid', { name: file.name });
      continue;
    }

    const objectUrl = URL.createObjectURL(file);
    // reactive() so the upload callback's mutations reach the rendered tile.
    const entry = reactive({
      key: `new-${nextImageKey++}`,
      storageObjectId: null,
      url: objectUrl,
      objectUrl,
      uploading: true,
      failed: false,
    });
    images.value.push(entry);
    upload(entry, file);
  }
}

function onInputChange(event) {
  addFiles(event.target.files);
  event.target.value = '';
}

function onDrop(event) {
  event.preventDefault();
  dragging.value = false;
  addFiles(event.dataTransfer?.files);
}

function removeImage(index) {
  const [removed] = images.value.splice(index, 1);
  if (removed?.objectUrl) URL.revokeObjectURL(removed.objectUrl);
}

// Keyboard-accessible alternative to dragging.
function moveImage(index, delta) {
  const target = index + delta;
  if (target < 0 || target >= images.value.length) return;
  const reordered = [...images.value];
  [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
  images.value = reordered;
}

function firstValidationMessage(err) {
  const errors = err.response?.data?.errors;
  const first = errors && typeof errors === 'object' ? Object.values(errors).flat()[0] : null;
  return first || t('admin.news.editor.save_error');
}

async function save() {
  formError.value = '';

  if (!form.title.trim()) {
    formError.value = t('admin.news.editor.title_required');
    return;
  }
  if (!form.html.trim()) {
    formError.value = t('admin.news.editor.body_required');
    return;
  }
  if (uploadsPending.value) {
    formError.value = t('admin.news.editor.uploads_pending');
    return;
  }
  if (images.value.some((image) => image.failed)) {
    formError.value = t('admin.news.editor.uploads_failed');
    return;
  }

  const payload = {
    title: form.title.trim(),
    html: form.html,
    visibility: form.visibility,
    isPublished: form.isPublished,
    imageStorageObjectIds: images.value.map((image) => image.storageObjectId),
  };

  saving.value = true;
  try {
    if (isEdit.value) await updateNewsPost(postId.value, payload);
    else await createNewsPost(payload);
    router.push({ name: 'AdminNews' });
  } catch (err) {
    formError.value = err.response?.status === 400
      ? firstValidationMessage(err)
      : t('admin.news.editor.save_error');
  } finally {
    saving.value = false;
  }
}

function cancel() {
  router.push({ name: 'AdminNews' });
}
</script>

<template>
  <div class="admin-news-editor">
    <router-link
      :to="{ name: 'AdminNews' }"
      class="inline-flex items-center gap-1.5 text-sm font-medium text-ink-muted hover:text-ink"
    >
      <ArrowLeft
        class="size-4"
        aria-hidden="true"
      />
      {{ $t('admin.news.editor.back') }}
    </router-link>

    <h1 class="mt-3 font-display text-2xl font-semibold tracking-tight text-ink">
      {{ isEdit ? $t('admin.news.editor.edit_title') : $t('admin.news.editor.create_title') }}
    </h1>

    <p
      v-if="loadError"
      class="mt-4 rounded-md border border-accent-coral bg-accent-soft px-3 py-2 text-sm font-semibold text-accent-coral"
    >
      {{ loadError }}
    </p>
    <p
      v-else-if="loading"
      class="mt-4 text-sm text-ink-muted"
    >
      {{ $t('courses.loading') }}
    </p>

    <div
      v-else
      class="mt-6 max-w-4xl space-y-6"
    >
      <label class="block text-sm font-medium text-ink-muted">
        {{ $t('admin.news.editor.title_label') }}
        <Input
          v-model="form.title"
          class="mt-1"
          maxlength="200"
          :placeholder="$t('admin.news.editor.title_placeholder')"
        />
      </label>

      <div>
        <p class="text-sm font-medium text-ink-muted">
          {{ $t('admin.news.editor.body_label') }}
        </p>
        <div class="mt-1">
          <RichEditor
            v-model="form.html"
            :placeholder="$t('admin.news.editor.body_placeholder')"
            :allow-image="false"
            :disabled="saving"
          />
        </div>
      </div>

      <fieldset>
        <legend class="text-sm font-medium text-ink-muted">
          {{ $t('admin.news.editor.visibility_label') }}
        </legend>
        <div class="mt-2 grid gap-2 sm:grid-cols-2">
          <label
            v-for="option in visibilityOptions"
            :key="option.value"
            class="admin-news-editor__choice"
            :class="{ 'is-selected': form.visibility === option.value }"
          >
            <input
              v-model="form.visibility"
              type="radio"
              name="news-visibility"
              :value="option.value"
              class="mt-1"
            >
            <span>
              <span class="block text-sm font-semibold text-ink">{{ option.label }}</span>
              <span class="block text-xs text-ink-muted">{{ option.hint }}</span>
            </span>
          </label>
        </div>
      </fieldset>

      <label class="flex items-start gap-2">
        <input
          v-model="form.isPublished"
          type="checkbox"
          class="mt-1"
        >
        <span>
          <span class="block text-sm font-semibold text-ink">{{ $t('admin.news.editor.published_label') }}</span>
          <span class="block text-xs text-ink-muted">{{ $t('admin.news.editor.published_hint') }}</span>
        </span>
      </label>

      <section aria-labelledby="news-images-heading">
        <h2
          id="news-images-heading"
          class="text-sm font-medium text-ink-muted"
        >
          {{ $t('admin.news.editor.images_label') }}
        </h2>
        <p class="mt-1 text-xs text-ink-muted">
          {{ $t('admin.news.editor.images_hint', { max: MAX_IMAGES }) }}
        </p>

        <draggable
          v-model="images"
          item-key="key"
          :disabled="saving"
          class="admin-news-editor__images"
        >
          <template #item="{ element: image, index }">
            <div
              class="admin-news-editor__tile"
              :class="{ 'is-failed': image.failed }"
              data-testid="news-image-tile"
            >
              <img
                :src="image.url"
                alt=""
              >
              <span
                v-if="image.uploading"
                class="admin-news-editor__status"
              >
                {{ $t('admin.news.editor.uploading') }}
              </span>
              <span
                v-else-if="image.failed"
                class="admin-news-editor__status admin-news-editor__status--error"
              >
                {{ $t('admin.news.editor.image_upload_error') }}
              </span>
              <div class="admin-news-editor__tile-actions">
                <button
                  type="button"
                  :aria-label="$t('admin.news.editor.move_left', { index: index + 1 })"
                  :disabled="index === 0 || saving"
                  @click="moveImage(index, -1)"
                >
                  <ChevronLeft class="size-4" />
                </button>
                <button
                  type="button"
                  :aria-label="$t('admin.news.editor.move_right', { index: index + 1 })"
                  :disabled="index === images.length - 1 || saving"
                  @click="moveImage(index, 1)"
                >
                  <ChevronRight class="size-4" />
                </button>
                <button
                  type="button"
                  :aria-label="$t('admin.news.editor.remove_image', { index: index + 1 })"
                  :disabled="saving"
                  @click="removeImage(index)"
                >
                  <X class="size-4" />
                </button>
              </div>
            </div>
          </template>
        </draggable>

        <input
          ref="fileInput"
          type="file"
          class="hidden"
          multiple
          :accept="ACCEPTED_TYPES.join(',')"
          :aria-label="$t('admin.news.editor.images_add')"
          @change="onInputChange"
        >
        <button
          v-if="canAddImages"
          type="button"
          class="admin-news-editor__dropzone"
          :class="{ 'is-dragging': dragging }"
          :disabled="saving"
          @click="fileInput?.click()"
          @dragover.prevent="dragging = true"
          @dragleave="dragging = false"
          @drop="onDrop"
        >
          <ImagePlus
            class="size-5"
            aria-hidden="true"
          />
          <span>{{ $t('admin.news.editor.images_drop') }}</span>
        </button>

        <p
          v-if="imageError"
          class="mt-2 text-sm font-semibold text-accent-coral"
        >
          {{ imageError }}
        </p>
      </section>

      <p
        v-if="formError"
        class="rounded-md border border-accent-coral bg-accent-soft px-3 py-2 text-sm font-semibold text-accent-coral"
        role="alert"
      >
        {{ formError }}
      </p>

      <div class="flex flex-wrap gap-2">
        <Button
          :disabled="saving || uploadsPending"
          @click="save"
        >
          {{ saving ? $t('admin.news.editor.saving') : $t('admin.news.editor.save') }}
        </Button>
        <Button
          variant="outline"
          :disabled="saving"
          @click="cancel"
        >
          {{ $t('admin.news.editor.cancel') }}
        </Button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.admin-news-editor__choice {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
  padding: 0.75rem 0.85rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.6rem;
  background: var(--color-card);
  cursor: pointer;
}

.admin-news-editor__choice.is-selected {
  border-color: var(--color-accent-coral);
}

.admin-news-editor__images {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(9rem, 1fr));
  gap: 0.75rem;
  margin-top: 0.75rem;
}

.admin-news-editor__images:empty {
  display: none;
}

.admin-news-editor__tile {
  position: relative;
  overflow: hidden;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.6rem;
  background: var(--color-surface-900);
  cursor: grab;
}

.admin-news-editor__tile.is-failed {
  border-color: var(--color-accent-coral);
}

.admin-news-editor__tile img {
  display: block;
  width: 100%;
  aspect-ratio: 4 / 3;
  object-fit: cover;
}

.admin-news-editor__status {
  position: absolute;
  top: 0.4rem;
  left: 0.4rem;
  padding: 0.15rem 0.45rem;
  border-radius: 0.35rem;
  background: rgb(0 0 0 / 0.6);
  color: #fff;
  font-size: 0.7rem;
  font-weight: 600;
}

.admin-news-editor__status--error {
  background: var(--color-accent-coral);
}

.admin-news-editor__tile-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.25rem;
  padding: 0.35rem;
}

.admin-news-editor__tile-actions button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.75rem;
  height: 1.75rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.4rem;
  background: var(--color-card);
  color: var(--color-ink-muted);
}

.admin-news-editor__tile-actions button:hover:not(:disabled) {
  color: var(--color-ink);
}

.admin-news-editor__tile-actions button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.admin-news-editor__dropzone {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.6rem;
  width: 100%;
  margin-top: 0.75rem;
  padding: 1.1rem 1rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.65rem;
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  font-weight: 600;
  transition: border-color 0.12s ease, color 0.12s ease;
}

.admin-news-editor__dropzone:hover:not(:disabled),
.admin-news-editor__dropzone.is-dragging {
  border-color: var(--color-accent-coral);
  color: var(--color-ink);
}
</style>
