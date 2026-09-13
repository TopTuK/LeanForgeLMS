<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { Badge } from '@/components/ui/badge';
import NewsGallery from '@/components/news/NewsGallery.vue';
import { formatNewsDate } from '@/lib/news';

const props = defineProps({
  post: { type: Object, required: true },
  isNew: { type: Boolean, default: false },
  headingTag: { type: String, default: 'h2' },
});

const { locale } = useI18n();

const date = computed(() => formatNewsDate(props.post.publishedAt, locale.value));
const membersOnly = computed(() => props.post.visibility === 'MembersOnly');
const headingId = computed(() => `news-article-${props.post.id}`);
</script>

<template>
  <article
    class="news-article"
    :class="{ 'news-article--new': isNew }"
    :aria-labelledby="headingId"
  >
    <header>
      <div class="news-article__meta">
        <time
          class="mono-label"
          :datetime="post.publishedAt"
        >{{ date }}</time>
        <Badge
          v-if="isNew"
          variant="coral"
        >
          {{ $t('notifications.new_badge') }}
        </Badge>
        <Badge
          v-if="membersOnly"
          variant="muted"
        >
          {{ $t('news.members_only') }}
        </Badge>
      </div>
      <component
        :is="headingTag"
        :id="headingId"
        class="news-article__title font-display"
      >
        {{ post.title }}
      </component>
    </header>

    <div
      v-safe-html="post.html"
      class="news-article__prose"
    />

    <NewsGallery
      v-if="post.images?.length"
      :images="post.images"
      :title="post.title"
      class="news-article__gallery"
    />
  </article>
</template>

<style scoped>
.news-article {
  padding: clamp(1.25rem, 3vw, 1.75rem);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-card);
}

.news-article--new {
  border-left: 3px solid var(--color-accent-coral);
}

.news-article__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.6rem;
  color: var(--color-ink-faint);
}

.news-article__title {
  margin: 0.65rem 0 0;
  color: var(--color-ink);
  font-size: clamp(1.35rem, 3vw, 1.75rem);
  font-weight: 600;
  letter-spacing: -0.025em;
  line-height: 1.2;
}

.news-article__prose {
  margin-top: 1rem;
  color: var(--color-ink);
  font-size: 1rem;
  line-height: 1.7;
  overflow-wrap: anywhere;
}

.news-article__prose :deep(h1),
.news-article__prose :deep(h2) {
  margin: 1.1rem 0 0.55rem;
  font-size: 1.3rem;
  font-weight: 800;
}

.news-article__prose :deep(h3) {
  margin: 1rem 0 0.45rem;
  font-size: 1.05rem;
  font-weight: 700;
}

.news-article__prose :deep(p) {
  margin: 0.55rem 0;
}

.news-article__prose :deep(ul),
.news-article__prose :deep(ol) {
  margin: 0.55rem 0;
  padding-left: 1.35rem;
}

.news-article__prose :deep(ul) {
  list-style: disc;
}

.news-article__prose :deep(ol) {
  list-style: decimal;
}

.news-article__prose :deep(blockquote) {
  margin: 0.85rem 0;
  padding-left: 1rem;
  border-left: 3px solid var(--color-border-subtle);
  color: var(--color-ink-muted);
}

.news-article__prose :deep(a) {
  color: var(--color-accent-coral-dark);
  text-decoration: underline;
  text-underline-offset: 0.15em;
}

.news-article__gallery {
  margin-top: 1.25rem;
}
</style>
