<script setup>
import { computed, onMounted, ref } from 'vue';
import { fetchNotifications } from '@/services/newsService';
import { useNotificationStore } from '@/stores/notificationStore';
import NewsArticle from '@/components/news/NewsArticle.vue';
import GeometricBackdrop from '@/components/layout/GeometricBackdrop.vue';
import { Button } from '@/components/ui/button';

const PAGE_SIZE = 10;

const notificationStore = useNotificationStore();

const posts = ref([]);
const totalCount = ref(0);
const page = ref(0);
// Starts true so the empty state can't flash before the first page arrives.
const loading = ref(true);
const loadFailed = ref(false);

// Taken from the first page only: marking the feed seen advances the server-side marker, and the
// "New" tags must keep describing what was unread when the user arrived.
const lastSeenAt = ref(null);

const hasMore = computed(() => posts.value.length < totalCount.value);

function isNew(post) {
  return !lastSeenAt.value || new Date(post.publishedAt) > new Date(lastSeenAt.value);
}

async function loadMore() {
  loading.value = true;
  loadFailed.value = false;
  try {
    const isFirstPage = page.value === 0;
    const result = await fetchNotifications({ page: page.value + 1, pageSize: PAGE_SIZE });
    posts.value = [...posts.value, ...result.items];
    totalCount.value = result.totalCount;
    page.value += 1;

    if (isFirstPage) {
      lastSeenAt.value = result.lastSeenAt;
      await notificationStore.markAllSeen();
    }
  } catch {
    loadFailed.value = true;
  } finally {
    loading.value = false;
  }
}

onMounted(loadMore);
</script>

<template>
  <section class="notifications-page">
    <GeometricBackdrop dense />

    <div class="notifications-page__inner">
      <p class="mono-label notifications-page__eyebrow">
        {{ $t('notifications.eyebrow') }}
      </p>
      <h1 class="notifications-page__title font-display">
        {{ $t('notifications.title') }}
      </h1>
      <p class="notifications-page__subtitle">
        {{ $t('notifications.subtitle') }}
      </p>

      <p
        v-if="loadFailed"
        class="notifications-page__error"
        role="alert"
      >
        {{ $t('notifications.load_error') }}
      </p>
      <p
        v-else-if="!loading && !posts.length"
        class="notifications-page__hint"
      >
        {{ $t('notifications.empty') }}
      </p>

      <div
        v-if="posts.length"
        class="notifications-page__feed"
      >
        <NewsArticle
          v-for="post in posts"
          :key="post.id"
          :post="post"
          :is-new="isNew(post)"
        />
      </div>

      <p
        v-if="loading"
        class="notifications-page__hint"
      >
        {{ $t('news.loading') }}
      </p>

      <div
        v-if="hasMore && !loading"
        class="notifications-page__more"
      >
        <Button
          variant="outline"
          @click="loadMore"
        >
          {{ $t('news.show_more') }}
        </Button>
      </div>
    </div>
  </section>
</template>

<style scoped>
.notifications-page {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  padding: clamp(2.5rem, 6vw, 4rem) 1.5rem clamp(4rem, 8vw, 6rem);
}

.notifications-page__inner {
  position: relative;
  z-index: 1;
  max-width: 48rem;
  margin-inline: auto;
}

.notifications-page__eyebrow {
  margin: 0 0 1rem;
  color: var(--color-accent-coral);
}

.notifications-page__title {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.9rem, 4vw, 2.5rem);
  font-weight: 600;
  letter-spacing: -0.03em;
  line-height: 1.1;
}

.notifications-page__subtitle {
  margin: 0.85rem 0 0;
  color: var(--color-ink-muted);
  line-height: 1.6;
}

.notifications-page__feed {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  margin-top: 2rem;
}

.notifications-page__hint {
  margin: 2rem 0 0;
  color: var(--color-ink-muted);
}

.notifications-page__error {
  margin: 2rem 0 0;
  padding: 0.6rem 0.85rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.5rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
  font-size: 0.9rem;
  font-weight: 600;
}

.notifications-page__more {
  display: flex;
  justify-content: center;
  margin-top: 2rem;
}
</style>
