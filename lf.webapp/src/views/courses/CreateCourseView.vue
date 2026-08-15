<script setup>
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { fetchCategories, fetchCourses, createCourse } from '@/services/courseService';
import ForgeField from '@/components/courses/forge/ForgeField.vue';

const { t } = useI18n();
const router = useRouter();

const title = ref('');
const shortIntroduction = ref('');
const description = ref('');
const category = ref(null);
const categories = ref([]);

const submitting = ref(false);
const errorMessage = ref('');

const drafts = ref([]);
const draftsLoading = ref(false);
const draftsError = ref('');

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

const canSubmit = computed(() =>
  Boolean(
    title.value.trim()
    && shortIntroduction.value.trim()
    && description.value.trim()
    && category.value,
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
  <div class="course-forge">
    <div
      class="course-forge__square course-forge__square--one"
      aria-hidden="true"
    />
    <div
      class="course-forge__square course-forge__square--two"
      aria-hidden="true"
    />
    <div
      class="course-forge__curve"
      aria-hidden="true"
    />

    <div class="container mx-auto px-6 py-12 md:py-16 relative z-10">
      <header class="course-forge__heading">
        <div>
          <p class="course-forge__eyebrow">
            {{ $t('courses.create.eyebrow') }}
          </p>
          <h1>{{ $t('courses.create.title') }}</h1>
          <p class="course-forge__subtitle">
            {{ $t('courses.create.subtitle') }}
          </p>
        </div>
        <div
          class="course-forge__meta"
          aria-hidden="true"
        >
          <span>SPEC / 01</span>
          <span>DRAFT BAY</span>
        </div>
      </header>

      <div
        v-if="errorMessage"
        class="course-forge__alert"
        role="alert"
      >
        <span>{{ errorMessage }}</span>
        <button
          type="button"
          class="course-forge__alert-close"
          :aria-label="$t('courses.create.dismiss_error')"
          @click="errorMessage = ''"
        >
          ×
        </button>
      </div>

      <div class="course-forge__layout">
        <form
          class="course-forge__panel"
          @submit.prevent="submit"
        >
          <div class="course-forge__panel-mark" aria-hidden="true">
            LF—CREATE
          </div>

          <ForgeField
            v-model="title"
            index="01"
            :label="$t('courses.create.field_title')"
            required
          />
          <ForgeField
            v-model="shortIntroduction"
            index="02"
            type="textarea"
            :rows="3"
            :label="$t('courses.create.field_short_introduction')"
            required
          />
          <ForgeField
            v-model="description"
            index="03"
            type="textarea"
            :rows="5"
            :label="$t('courses.create.field_description')"
            required
          />

          <fieldset class="course-forge__category">
            <legend class="course-forge__category-legend">
              <span>{{ $t('courses.create.field_category') }}</span>
              <span aria-hidden="true">04</span>
            </legend>
            <p
              v-if="!categories.length"
              class="course-forge__hint"
            >
              {{ $t('courses.create.category_placeholder') }}
            </p>
            <div
              v-else
              class="course-forge__chips"
              role="listbox"
              :aria-label="$t('courses.create.field_category')"
            >
              <button
                v-for="item in categories"
                :key="item.id"
                type="button"
                class="course-forge__chip"
                role="option"
                :aria-selected="category === item.id"
                :class="{ 'is-active': category === item.id }"
                @click="category = item.id"
              >
                {{ item.name }}
              </button>
            </div>
          </fieldset>

          <div class="course-forge__actions">
            <button
              type="submit"
              class="course-forge__submit btn-accent"
              :disabled="submitting"
            >
              <span v-if="submitting">{{ $t('courses.create.submitting') }}</span>
              <span v-else>{{ $t('courses.create.submit') }}</span>
              <span
                v-if="!submitting"
                aria-hidden="true"
              >→</span>
            </button>
            <router-link
              :to="{ name: 'CoursesAvailable' }"
              class="course-forge__back"
            >
              {{ $t('courses.create.back') }}
            </router-link>
          </div>
        </form>

        <aside class="course-forge__rail">
          <div class="course-forge__rail-panel">
            <h2>{{ $t('courses.create.your_drafts_title') }}</h2>

            <p
              v-if="draftsError"
              class="course-forge__hint course-forge__hint--error"
            >
              {{ draftsError }}
            </p>
            <p
              v-else-if="draftsLoading"
              class="course-forge__hint"
            >
              {{ $t('courses.create.drafts_loading') }}
            </p>
            <p
              v-else-if="drafts.length === 0"
              class="course-forge__hint"
            >
              {{ $t('courses.create.your_drafts_empty') }}
            </p>
            <ul
              v-else
              class="course-forge__drafts"
            >
              <li
                v-for="(draft, index) in drafts"
                :key="draft.id"
              >
                <router-link
                  :to="{ name: 'CourseEdit', params: { id: draft.id } }"
                  class="course-forge__draft"
                >
                  <span
                    class="course-forge__draft-index"
                    aria-hidden="true"
                  >{{ String(index + 1).padStart(2, '0') }}</span>
                  <span class="course-forge__draft-title">{{ draft.title }}</span>
                  <span class="course-forge__draft-action">{{ $t('courses.create.edit_action') }} →</span>
                </router-link>
              </li>
            </ul>
          </div>

          <div class="course-forge__soon">
            <h2>{{ $t('courses.create.coming_soon_section_title') }}</h2>
            <p>{{ $t('courses.create.coming_soon') }}</p>
          </div>
        </aside>
      </div>
    </div>
  </div>
</template>

<style scoped>
.course-forge {
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

.course-forge__square,
.course-forge__curve {
  position: absolute;
  pointer-events: none;
  z-index: 0;
}

.course-forge__square {
  width: 7rem;
  height: 7rem;
  border: 1px solid var(--industrial-line);
}

.course-forge__square::after {
  content: "";
  position: absolute;
  inset: 0.75rem;
  background: var(--industrial-accent-wash);
  border: 1px solid var(--industrial-line);
}

.course-forge__square--one {
  top: 4rem;
  right: 8%;
  transform: rotate(18deg);
  animation: forge-drift 10s ease-in-out infinite alternate;
}

.course-forge__square--two {
  bottom: 12%;
  left: 4%;
  width: 4.5rem;
  height: 4.5rem;
  opacity: 0.7;
}

.course-forge__curve {
  width: 18rem;
  height: 18rem;
  border: 1px solid var(--industrial-line);
  border-radius: 50%;
  bottom: -6rem;
  right: -4rem;
  box-shadow: 0 0 0 48px var(--industrial-grid);
}

.course-forge__heading {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 2rem;
  margin-bottom: 2rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid var(--industrial-line);
  animation: forge-rise 0.4s ease both;
}

.course-forge__eyebrow {
  margin: 0 0 0.75rem;
  color: var(--color-accent-coral);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.course-forge__heading h1 {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(2rem, 5vw, 3.5rem);
  font-weight: 800;
  letter-spacing: -0.045em;
  line-height: 1.05;
}

.course-forge__subtitle {
  max-width: 36rem;
  margin: 0.9rem 0 0;
  color: var(--color-ink-muted);
  font-size: 1rem;
  line-height: 1.65;
}

.course-forge__meta {
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

.course-forge__alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
  padding: 0.85rem 1rem;
  color: var(--color-accent-coral);
  background: var(--color-accent-soft);
  border: 1px solid var(--color-accent-coral);
  border-radius: var(--radius-card);
  font-size: 0.9rem;
  font-weight: 600;
}

.course-forge__alert-close {
  border: 0;
  background: transparent;
  color: inherit;
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
}

.course-forge__layout {
  display: grid;
  gap: 1.25rem;
  grid-template-columns: minmax(0, 1fr);
}

.course-forge__panel,
.course-forge__rail-panel,
.course-forge__soon {
  position: relative;
  background: var(--industrial-panel);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  backdrop-filter: blur(12px);
  box-shadow: 0 18px 50px rgba(20, 20, 20, 0.06);
}

.course-forge__panel {
  display: flex;
  flex-direction: column;
  gap: 1.15rem;
  padding: 1.75rem 1.5rem 1.5rem;
  animation: forge-rise 0.45s ease 0.06s both;
}

.course-forge__panel-mark {
  position: absolute;
  top: 1.1rem;
  right: 1.25rem;
  color: var(--color-ink-faint);
  font-size: 0.65rem;
  font-weight: 700;
  letter-spacing: 0.14em;
}

.course-forge__category {
  margin: 0;
  padding: 0;
  border: 0;
}

.course-forge__category-legend {
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.65rem;
  color: var(--color-ink-muted);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.course-forge__category-legend span:last-child {
  color: var(--color-ink-faint);
  letter-spacing: 0.14em;
}

.course-forge__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.55rem;
}

.course-forge__chip {
  padding: 0.55rem 0.95rem;
  color: var(--color-ink-muted);
  background: transparent;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-pill);
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: color 0.15s ease, border-color 0.15s ease, background-color 0.15s ease, transform 0.12s ease;
}

.course-forge__chip:hover {
  color: var(--color-ink);
  border-color: var(--color-ink-faint);
}

.course-forge__chip.is-active {
  color: #fff;
  background: var(--color-ink);
  border-color: var(--color-ink);
}

.course-forge__chip:active {
  transform: scale(0.97);
}

.course-forge__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 1rem;
  margin-top: 0.5rem;
  padding-top: 1.25rem;
  border-top: 1px solid var(--industrial-line);
}

.course-forge__submit {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.55rem;
  min-height: 3rem;
  padding: 0.75rem 1.5rem;
  border: 0;
  border-radius: var(--radius-pill);
  font-family: inherit;
  font-size: 0.9rem;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.15s ease, transform 0.12s ease, opacity 0.15s ease;
}

.course-forge__submit:hover:not(:disabled) {
  transform: translateY(-1px);
}

.course-forge__submit:active:not(:disabled) {
  transform: scale(0.97);
}

.course-forge__submit:disabled {
  opacity: 0.7;
  cursor: wait;
}

.course-forge__back {
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  font-weight: 600;
  text-decoration: none;
  transition: color 0.15s ease;
}

.course-forge__back:hover {
  color: var(--color-accent-coral);
}

.course-forge__rail {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  animation: forge-rise 0.45s ease 0.12s both;
}

.course-forge__rail-panel {
  padding: 1.5rem;
}

.course-forge__rail-panel h2,
.course-forge__soon h2 {
  margin: 0 0 1rem;
  color: var(--color-ink);
  font-size: 0.95rem;
  font-weight: 800;
  letter-spacing: -0.01em;
}

.course-forge__hint {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.88rem;
  line-height: 1.5;
}

.course-forge__hint--error {
  color: var(--color-accent-coral);
}

.course-forge__drafts {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.course-forge__draft {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  align-items: center;
  gap: 0.75rem;
  padding: 0.8rem 0.9rem;
  color: inherit;
  text-decoration: none;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.35rem;
  transition: border-color 0.15s ease, background-color 0.15s ease;
}

.course-forge__draft:hover {
  border-color: var(--industrial-line-strong);
  background: var(--industrial-accent-wash);
}

.course-forge__draft-index {
  color: var(--color-ink-faint);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.08em;
}

.course-forge__draft-title {
  color: var(--color-ink);
  font-size: 0.9rem;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.course-forge__draft-action {
  color: var(--color-accent-coral);
  font-size: 0.78rem;
  font-weight: 700;
  white-space: nowrap;
}

.course-forge__soon {
  padding: 1.15rem 1.35rem;
  border-style: dashed;
  color: var(--color-ink-muted);
}

.course-forge__soon h2 {
  margin-bottom: 0.4rem;
}

.course-forge__soon p {
  margin: 0;
  font-size: 0.88rem;
  line-height: 1.55;
}

@media (min-width: 960px) {
  .course-forge__meta {
    display: flex;
  }

  .course-forge__layout {
    grid-template-columns: minmax(0, 1.4fr) minmax(16rem, 0.85fr);
    align-items: start;
  }
}

@keyframes forge-rise {
  from {
    opacity: 0;
    transform: translateY(0.75rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes forge-drift {
  from {
    transform: rotate(18deg) translateY(0);
  }
  to {
    transform: rotate(26deg) translateY(0.5rem);
  }
}
</style>
