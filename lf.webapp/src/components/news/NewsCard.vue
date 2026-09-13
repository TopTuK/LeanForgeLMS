<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ArrowUpRight, Newspaper } from 'lucide-vue-next';
import { formatNewsDate, htmlToText } from '@/lib/news';

const props = defineProps({
  post: { type: Object, required: true },
  to: { type: [Object, String], required: true },
});

const { locale } = useI18n();

const cover = computed(() => props.post.images?.[0] ?? null);
const excerpt = computed(() => htmlToText(props.post.html));
const date = computed(() => formatNewsDate(props.post.publishedAt, locale.value));
</script>

<template>
  <article class="news-card">
    <div class="news-card__media">
      <img
        v-if="cover"
        :src="cover.url"
        alt=""
        loading="lazy"
        decoding="async"
      >
      <span
        v-else
        class="news-card__placeholder"
        aria-hidden="true"
      >
        <Newspaper :size="28" />
      </span>
    </div>

    <div class="news-card__body">
      <time
        class="mono-label news-card__date"
        :datetime="post.publishedAt"
      >{{ date }}</time>
      <h3 class="news-card__title font-display">
        <router-link
          :to="to"
          class="news-card__link"
        >
          {{ post.title }}
        </router-link>
      </h3>
      <p class="news-card__excerpt">
        {{ excerpt }}
      </p>
      <span
        class="news-card__more"
        aria-hidden="true"
      >
        {{ $t('news.read_more') }}
        <ArrowUpRight :size="14" />
      </span>
    </div>
  </article>
</template>

<style scoped>
.news-card {
  position: relative;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-card);
  transition: border-color 0.15s ease, transform 0.15s ease;
}

.news-card:hover,
.news-card:focus-within {
  border-color: var(--color-accent-coral);
  transform: translateY(-2px);
}

.news-card__media {
  aspect-ratio: 16 / 9;
  overflow: hidden;
  background: var(--color-surface-900);
}

.news-card__media img {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.news-card__placeholder {
  display: grid;
  place-items: center;
  width: 100%;
  height: 100%;
  color: var(--color-ink-faint);
}

.news-card__body {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 0.55rem;
  padding: 1.1rem 1.25rem 1.25rem;
}

.news-card__date {
  color: var(--color-ink-faint);
}

.news-card__title {
  margin: 0;
  color: var(--color-ink);
  font-size: 1.15rem;
  font-weight: 600;
  letter-spacing: -0.02em;
  line-height: 1.3;
}

/* Stretch the title link over the whole card so the card is one click target with one accessible name. */
.news-card__link::after {
  content: '';
  position: absolute;
  inset: 0;
}

.news-card__link:focus-visible {
  outline: none;
}

.news-card__excerpt {
  display: -webkit-box;
  margin: 0;
  overflow: hidden;
  color: var(--color-ink-muted);
  font-size: 0.92rem;
  line-height: 1.55;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 3;
}

.news-card__more {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  margin-top: auto;
  padding-top: 0.35rem;
  color: var(--color-accent-coral);
  font-size: 0.85rem;
  font-weight: 600;
}
</style>
