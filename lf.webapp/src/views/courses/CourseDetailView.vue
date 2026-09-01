<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { Lock } from 'lucide-vue-next';
import {
  fetchCoursePreview,
  fetchCourseCoverImageObjectUrl,
  fetchCoursePreviewLessonMediaObjectUrl,
  fetchCoursePreviewLessonPartFileObjectUrl,
  validatePromoCode,
} from '@/services/enrollmentService';
import { createCheckout } from '@/services/paymentService';
import { useCourseCoverImages } from '@/composables/useCourseCoverImages';

const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const courseId = computed(() => Number(route.params.id));

const course = ref(null);
const loading = ref(true);
const notFound = ref(false);
const errorMessage = ref('');
const enrolling = ref(false);
const pendingPayment = ref(false);

const isPaid = computed(() => course.value?.pricingType === 'Paid');
const rubFormatter = new Intl.NumberFormat('ru-RU');

const promoCode = ref('');
const promoChecking = ref(false);
const promoResult = ref(null);

const effectivePrice = computed(() => {
  if (!isPaid.value) return null;
  if (promoResult.value?.isValid) return promoResult.value.discountedPrice;
  return course.value.price;
});

const priceLabel = computed(() => {
  if (!course.value) return '';
  if (!isPaid.value) return t('courses.detail.price_free');
  return `${rubFormatter.format(effectivePrice.value)} ₽`;
});

async function applyPromoCode() {
  const code = promoCode.value.trim();
  if (!code || !course.value) return;
  promoChecking.value = true;
  promoResult.value = null;
  try {
    promoResult.value = await validatePromoCode(code, course.value.id);
  } catch {
    promoResult.value = { isValid: false, reason: t('courses.detail.promo_error') };
  } finally {
    promoChecking.value = false;
  }
}

const { coverImageUrls, load: loadCoverImages } = useCourseCoverImages(fetchCourseCoverImageObjectUrl);
const coverImageUrl = computed(() => (course.value ? coverImageUrls.value[course.value.id] ?? null : null));
const showCoverImage = computed(() => course.value?.coverType === 'Image' && Boolean(coverImageUrl.value));
const hasCover = computed(() => course.value?.coverType === 'Color' || showCoverImage.value);
const coverStyle = computed(() => (
  course.value?.coverType === 'Color' && course.value.coverColor
    ? { backgroundColor: `var(--color-cover-${course.value.coverColor.toLowerCase()})` }
    : {}
));

async function load() {
  loading.value = true;
  notFound.value = false;
  errorMessage.value = '';
  expandedLessonIds.value = new Set();
  clearMediaObjectUrls();

  try {
    course.value = await fetchCoursePreview(courseId.value);
    if (course.value.coverType === 'Image') await loadCoverImages([course.value], (c) => c.id);
  } catch (err) {
    if (err.response?.status === 404) notFound.value = true;
    else errorMessage.value = t('courses.detail.load_error');
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(() => route.params.id, load);

function goToCatalog() {
  router.push({ name: 'CoursesAvailable' });
}

async function onCtaClick() {
  if (!course.value) return;

  if (course.value.isEnrolled) {
    router.push({ name: 'CourseLearn', params: { enrollmentId: course.value.enrollmentId } });
    return;
  }

  enrolling.value = true;
  errorMessage.value = '';
  try {
    const appliedCode = promoResult.value?.isValid ? promoCode.value.trim() : null;
    const checkout = await createCheckout(course.value.id, appliedCode);
    if (checkout.paymentUrl) {
      pendingPayment.value = true;
      window.location.assign(checkout.paymentUrl);
      return;
    }
    router.push({ name: 'CourseLearn', params: { enrollmentId: checkout.enrollmentId } });
  } catch (err) {
    errorMessage.value = err.response?.status === 403
      ? t('courses.detail.managed_only')
      : t('courses.detail.enroll_error');
  } finally {
    enrolling.value = false;
  }
}

const expandedLessonIds = ref(new Set());
const mediaObjectUrls = ref({});

function clearMediaObjectUrls() {
  Object.values(mediaObjectUrls.value).forEach((url) => URL.revokeObjectURL(url));
  mediaObjectUrls.value = {};
}

onBeforeUnmount(clearMediaObjectUrls);

function lessonParts(lesson) {
  if (!Array.isArray(lesson.parts) || lesson.parts.length === 0) return [];
  return lesson.parts.map((part) => ({
    id: part.id,
    type: String(part.partType).toLowerCase(),
    html: part.html ?? '',
    mediaUrl: part.mediaUrl ?? null,
    files: part.files ?? [],
  }));
}

async function onLessonToggle(lesson, event) {
  if (!event.target.open || expandedLessonIds.value.has(lesson.id)) return;
  expandedLessonIds.value = new Set(expandedLessonIds.value).add(lesson.id);

  const pending = lessonParts(lesson).filter(
    (part) => part.mediaUrl && !mediaObjectUrls.value[part.id],
  );
  await Promise.all(pending.map(async (part) => {
    try {
      const objectUrl = await fetchCoursePreviewLessonMediaObjectUrl(course.value.id, lesson.id, part.id);
      mediaObjectUrls.value = { ...mediaObjectUrls.value, [part.id]: objectUrl };
    } catch {
      // Leave unresolved; the media block just won't render for this part.
    }
  }));
}

const downloadingFileId = ref(null);
const downloadErrorFileId = ref(null);

async function downloadFile(lesson, part, file) {
  if (downloadingFileId.value === file.id) return;

  downloadingFileId.value = file.id;
  downloadErrorFileId.value = null;
  let objectUrl;
  try {
    objectUrl = await fetchCoursePreviewLessonPartFileObjectUrl(course.value.id, lesson.id, part.id, file.id);
    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = file.fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
  } catch {
    downloadErrorFileId.value = file.id;
  } finally {
    if (objectUrl) URL.revokeObjectURL(objectUrl);
    downloadingFileId.value = null;
  }
}
</script>

<template>
  <div class="course-detail">
    <p
      v-if="loading"
      class="course-detail__hint"
    >
      {{ $t('courses.loading') }}
    </p>

    <div
      v-else-if="notFound"
      class="course-detail__state"
    >
      <h1>{{ $t('courses.detail.not_found') }}</h1>
      <button
        type="button"
        class="course-detail__text-btn"
        @click="goToCatalog"
      >
        {{ $t('courses.detail.back') }}
      </button>
    </div>

    <template v-else-if="course">
      <button
        type="button"
        class="course-detail__text-btn course-detail__back"
        @click="goToCatalog"
      >
        {{ $t('courses.detail.back') }}
      </button>

      <header
        class="course-detail__hero"
        :class="{ 'course-detail__hero--empty': !hasCover }"
        :style="coverStyle"
      >
        <img
          v-if="showCoverImage"
          :src="coverImageUrl"
          alt=""
          class="course-detail__hero-image"
        >
        <div
          v-if="hasCover"
          class="course-detail__hero-scrim"
          aria-hidden="true"
        />
        <div
          class="course-detail__hero-content"
          :class="{ 'course-detail__hero-content--light': hasCover }"
        >
          <div class="course-detail__hero-badges">
            <span class="course-detail__pill">{{ course.categoryName }}</span>
            <span class="course-detail__pill course-detail__pill--muted">
              {{ $t('courses.detail.lessons_count', { count: course.lessonCount }) }}
            </span>
          </div>
          <h1>{{ course.title }}</h1>
          <p
            v-if="course.shortIntroduction"
            class="course-detail__short-intro"
          >
            {{ course.shortIntroduction }}
          </p>
        </div>
      </header>

      <p
        v-if="errorMessage"
        class="course-detail__alert"
        role="alert"
      >
        {{ errorMessage }}
      </p>

      <div class="course-detail__body">
        <div class="course-detail__main">
          <section class="course-detail__section">
            <div
              v-if="course.description"
              v-safe-html="course.description"
              class="course-detail__prose"
            />
            <p
              v-else
              class="course-detail__hint"
            >
              {{ $t('courses.detail.description_empty') }}
            </p>
          </section>

          <section class="course-detail__section">
            <h2 class="course-detail__section-title">
              {{ $t('courses.detail.content_title') }}
            </h2>

            <details
              v-for="chapter in course.chapters"
              :key="chapter.id"
              class="course-detail__chapter"
              open
            >
              <summary class="course-detail__chapter-title">
                {{ chapter.title }}
              </summary>

              <ul class="course-detail__lessons">
                <li
                  v-for="lesson in chapter.lessons"
                  :key="lesson.id"
                >
                  <details
                    v-if="lesson.includeInPreview"
                    class="course-detail__lesson"
                    @toggle="onLessonToggle(lesson, $event)"
                  >
                    <summary class="course-detail__lesson-summary">
                      <span class="course-detail__preview-badge">{{ $t('courses.detail.preview_badge') }}</span>
                      {{ lesson.title }}
                    </summary>

                    <div
                      v-if="lessonParts(lesson).length > 0"
                      class="course-detail__lesson-parts"
                    >
                      <template
                        v-for="part in lessonParts(lesson)"
                        :key="part.id"
                      >
                        <div
                          v-if="part.type === 'text'"
                          v-safe-html="part.html"
                          class="course-detail__prose"
                        />

                        <p
                          v-else-if="part.type === 'quiz'"
                          class="course-detail__quiz-badge"
                        >
                          {{ $t('courses.detail.quiz_badge') }}
                        </p>

                        <ul
                          v-else-if="part.type === 'files'"
                          class="course-detail__files"
                        >
                          <li
                            v-for="file in part.files"
                            :key="file.id"
                            class="course-detail__files-item"
                          >
                            <span class="course-detail__files-name">{{ file.fileName }}</span>
                            <button
                              type="button"
                              class="course-detail__files-download"
                              :disabled="downloadingFileId === file.id"
                              @click="downloadFile(lesson, part, file)"
                            >
                              {{ downloadingFileId === file.id ? t('courses.detail.files.downloading') : t('courses.detail.files.download') }}
                            </button>
                            <span
                              v-if="downloadErrorFileId === file.id"
                              class="course-detail__files-error"
                            >
                              {{ t('courses.detail.files.download_error') }}
                            </span>
                          </li>
                        </ul>

                        <div
                          v-else
                          class="course-detail__media"
                        >
                          <img
                            v-if="part.type === 'image' && mediaObjectUrls[part.id]"
                            :src="mediaObjectUrls[part.id]"
                            alt=""
                            class="course-detail__media-image"
                          >
                          <video
                            v-else-if="part.type === 'video' && mediaObjectUrls[part.id]"
                            :src="mediaObjectUrls[part.id]"
                            class="course-detail__media-player"
                            controls
                            preload="metadata"
                          />
                          <audio
                            v-else-if="part.type === 'audio' && mediaObjectUrls[part.id]"
                            :src="mediaObjectUrls[part.id]"
                            class="course-detail__media-player course-detail__media-player--audio"
                            controls
                            preload="metadata"
                          />
                        </div>
                      </template>
                    </div>
                    <div
                      v-else
                      v-safe-html="lesson.content"
                      class="course-detail__prose"
                    />
                  </details>

                  <div
                    v-else
                    class="course-detail__lesson course-detail__lesson--locked"
                  >
                    <Lock
                      :size="14"
                      aria-hidden="true"
                    />
                    <span class="course-detail__lesson-title">{{ lesson.title }}</span>
                    <span class="course-detail__locked-hint">{{ $t('courses.detail.locked_hint') }}</span>
                  </div>
                </li>
              </ul>
            </details>
          </section>
        </div>

        <aside class="course-detail__cta-panel">
          <p class="course-detail__price">
            {{ priceLabel }}
            <span
              v-if="isPaid && promoResult?.isValid"
              class="course-detail__price-old"
            >{{ rubFormatter.format(course.price) }} ₽</span>
          </p>

          <div
            v-if="isPaid && !course.isEnrolled && !pendingPayment"
            class="course-detail__promo"
          >
            <label
              class="course-detail__promo-label"
              :for="'promo-code'"
            >{{ $t('courses.detail.promo_label') }}</label>
            <div class="course-detail__promo-row">
              <input
                id="promo-code"
                v-model="promoCode"
                type="text"
                class="course-detail__promo-input"
                :placeholder="$t('courses.detail.promo_placeholder')"
              >
              <button
                type="button"
                class="course-detail__promo-btn"
                :disabled="promoChecking || !promoCode.trim()"
                @click="applyPromoCode"
              >
                {{ promoChecking ? $t('courses.detail.promo_checking') : $t('courses.detail.promo_apply') }}
              </button>
            </div>
            <p
              v-if="promoResult && !promoResult.isValid"
              class="course-detail__promo-msg course-detail__promo-msg--error"
            >
              {{ promoResult.reason || $t('courses.detail.promo_invalid') }}
            </p>
            <p
              v-else-if="promoResult?.isValid"
              class="course-detail__promo-msg"
            >
              {{ $t('courses.detail.promo_applied', { amount: rubFormatter.format(promoResult.discountAmount) }) }}
            </p>
          </div>

          <p
            v-if="pendingPayment"
            class="course-detail__pending"
          >
            {{ $t('courses.detail.redirecting') }}
          </p>
          <button
            v-else
            type="button"
            class="course-detail__cta-btn"
            :disabled="enrolling"
            @click="onCtaClick"
          >
            {{ course.isEnrolled
              ? $t('courses.detail.continue')
              : (enrolling ? $t('courses.detail.enrolling') : $t('courses.detail.enroll')) }}
          </button>
        </aside>
      </div>
    </template>
  </div>
</template>

<style scoped>
.course-detail {
  min-height: calc(100vh - 4.5rem);
  background: var(--color-surface-950);
  padding: 1.25rem 1.25rem 3rem;
}

@media (min-width: 768px) {
  .course-detail {
    padding: 1.5rem 1.75rem 3.5rem;
  }
}

.course-detail__hint {
  margin: 2rem 0;
  color: var(--color-ink-muted);
  font-size: 0.95rem;
}

.course-detail__state {
  max-width: 36rem;
  padding: 2rem 0;
}

.course-detail__state h1 {
  margin: 0 0 1.25rem;
  color: var(--color-ink);
  font-size: 1.75rem;
  font-weight: 800;
}

.course-detail__text-btn {
  display: inline-flex;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-ink-muted);
  font: inherit;
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
}

.course-detail__text-btn:hover {
  color: var(--color-ink);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.course-detail__back {
  margin-bottom: 1rem;
}

.course-detail__hero {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  min-height: 16rem;
  overflow: hidden;
  padding: 1.5rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-surface-800);
}

.course-detail__hero--empty {
  background-image:
    linear-gradient(var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(135deg, var(--color-surface-900), var(--color-surface-800));
  background-size: 18px 18px, 18px 18px, auto;
}

.course-detail__hero-image {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.course-detail__hero-scrim {
  position: absolute;
  inset: 0;
  background: linear-gradient(to top, rgba(10, 10, 10, 0.78) 0%, rgba(10, 10, 10, 0.35) 55%, rgba(10, 10, 10, 0) 90%);
}

.course-detail__hero-content {
  position: relative;
  z-index: 1;
}

.course-detail__hero-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
  margin-bottom: 0.75rem;
}

.course-detail__pill {
  border-radius: var(--radius-pill);
  padding: 0.3rem 0.75rem;
  font-size: 0.72rem;
  font-weight: 700;
  background: #ffffff;
  color: var(--color-accent-coral-dark);
}

.course-detail__pill--muted {
  background: var(--color-surface-950);
  color: var(--color-ink-muted);
  font-weight: 600;
}

.course-detail__hero-content h1 {
  margin: 0 0 0.5rem;
  color: var(--color-ink);
  font-size: clamp(1.6rem, 3vw, 2.25rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.15;
}

.course-detail__short-intro {
  margin: 0;
  max-width: 42rem;
  color: var(--color-ink-muted);
  font-size: 1rem;
  line-height: 1.55;
}

.course-detail__hero-content--light h1 {
  color: #ffffff;
  text-shadow: 0 1px 4px rgba(0, 0, 0, 0.35);
}

.course-detail__hero-content--light .course-detail__short-intro {
  color: rgba(255, 255, 255, 0.9);
  text-shadow: 0 1px 4px rgba(0, 0, 0, 0.35);
}

.course-detail__alert {
  margin: 1rem 0;
  padding: 0.75rem 1rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.5rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral-dark);
  font-size: 0.9rem;
  font-weight: 600;
}

.course-detail__body {
  display: grid;
  gap: 1.5rem;
  margin-top: 1.5rem;
  align-items: start;
}

@media (min-width: 1024px) {
  .course-detail__body {
    grid-template-columns: minmax(0, 1fr) 16rem;
  }
}

.course-detail__main {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.course-detail__section-title {
  margin: 0 0 1rem;
  color: var(--color-ink);
  font-size: 1.2rem;
  font-weight: 800;
}

.course-detail__prose {
  color: var(--color-ink);
  font-size: 1rem;
  line-height: 1.7;
  overflow-wrap: anywhere;
}

.course-detail__prose :deep(h1),
.course-detail__prose :deep(h2) {
  margin: 1.1rem 0 0.55rem;
  font-size: 1.3rem;
  font-weight: 800;
}

.course-detail__prose :deep(h3) {
  margin: 1rem 0 0.45rem;
  font-size: 1.05rem;
  font-weight: 700;
}

.course-detail__prose :deep(p) {
  margin: 0.55rem 0;
}

.course-detail__prose :deep(ul),
.course-detail__prose :deep(ol) {
  margin: 0.55rem 0;
  padding-left: 1.35rem;
}

.course-detail__prose :deep(a) {
  color: var(--color-accent-coral-dark);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.course-detail__prose :deep(img) {
  display: block;
  max-width: 100%;
  height: auto;
  margin: 0.85rem 0;
  border-radius: 0.5rem;
}

.course-detail__chapter {
  margin-bottom: 0.75rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.65rem;
  background: var(--color-surface-900);
  overflow: hidden;
}

.course-detail__chapter-title {
  padding: 0.85rem 1rem;
  color: var(--color-ink);
  font-size: 0.92rem;
  font-weight: 700;
  cursor: pointer;
}

.course-detail__lessons {
  list-style: none;
  margin: 0;
  padding: 0 1rem 0.85rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.course-detail__lesson {
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.5rem;
  background: var(--color-surface-950);
}

.course-detail__lesson-summary {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  padding: 0.65rem 0.85rem;
  color: var(--color-ink);
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
}

.course-detail__preview-badge {
  flex-shrink: 0;
  border-radius: var(--radius-pill);
  padding: 0.15rem 0.55rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral-dark);
  font-size: 0.68rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.course-detail__lesson-parts {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 0 0.85rem 1rem;
}

.course-detail__quiz-badge {
  margin: 0;
  padding: 0.65rem 0.85rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.5rem;
  color: var(--color-ink-muted);
  font-size: 0.85rem;
  font-weight: 600;
}

.course-detail__media-image,
.course-detail__media-player {
  display: block;
  width: 100%;
  max-width: 100%;
  border-radius: 0.5rem;
}

.course-detail__media-player--audio {
  height: 2.75rem;
}

.course-detail__files {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.course-detail__files-item {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.65rem;
  padding: 0.6rem 0.7rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.5rem;
}

.course-detail__files-name {
  flex: 1;
  min-width: 8rem;
  font-size: 0.88rem;
  font-weight: 600;
  overflow-wrap: anywhere;
}

.course-detail__files-download {
  padding: 0.35rem 0.65rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.4rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  font: inherit;
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
}

.course-detail__files-download:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.course-detail__files-error {
  width: 100%;
  color: var(--color-accent-coral-dark);
  font-size: 0.78rem;
}

.course-detail__lesson--locked {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  padding: 0.65rem 0.85rem;
  color: var(--color-ink-faint);
  font-size: 0.9rem;
  font-weight: 600;
}

.course-detail__lesson-title {
  flex: 1;
  min-width: 0;
  overflow-wrap: anywhere;
}

.course-detail__locked-hint {
  flex-shrink: 0;
  color: var(--color-ink-faint);
  font-size: 0.75rem;
  font-weight: 600;
}

.course-detail__cta-panel {
  position: sticky;
  top: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  padding: 1.1rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-surface-900);
}

.course-detail__price {
  margin: 0;
  color: var(--color-ink);
  font-size: 1.4rem;
  font-weight: 800;
}

.course-detail__price-old {
  margin-left: 0.5rem;
  color: var(--color-ink-faint);
  font-size: 0.9rem;
  font-weight: 600;
  text-decoration: line-through;
}

.course-detail__promo {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.course-detail__promo-label {
  color: var(--color-ink-muted);
  font-size: 0.8rem;
  font-weight: 600;
}

.course-detail__promo-row {
  display: flex;
  gap: 0.4rem;
}

.course-detail__promo-input {
  flex: 1;
  min-width: 0;
  padding: 0.5rem 0.6rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.4rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  font: inherit;
  font-size: 0.85rem;
}

.course-detail__promo-btn {
  flex-shrink: 0;
  padding: 0.5rem 0.7rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.4rem;
  background: var(--color-surface-950);
  color: var(--color-ink);
  font: inherit;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
}

.course-detail__promo-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.course-detail__promo-msg {
  margin: 0;
  font-size: 0.8rem;
  color: var(--color-ink-muted);
}

.course-detail__promo-msg--error {
  color: var(--color-accent-coral-dark);
}

.course-detail__pending {
  margin: 0;
  padding: 0.7rem 0.8rem;
  border: 1px dashed var(--color-border-subtle);
  border-radius: 0.5rem;
  color: var(--color-ink-muted);
  font-size: 0.85rem;
  font-weight: 600;
}

.course-detail__cta-btn {
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: center;
  padding: 0.8rem 1rem;
  border: 0;
  border-radius: var(--radius-pill);
  background: var(--color-accent-coral);
  color: #ffffff;
  font-family: inherit;
  font-size: 0.92rem;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.15s ease, transform 0.12s ease, opacity 0.15s ease;
}

.course-detail__cta-btn:hover:not(:disabled) {
  background-color: var(--color-accent-coral-dark);
  transform: translateY(-1px);
}

.course-detail__cta-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}
</style>
