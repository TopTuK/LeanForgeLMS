<script setup>
import { computed, onMounted, ref } from 'vue';
import { ArrowLeft } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/authStore';
import { fetchPublicNews } from '@/services/newsService';
import NewsCard from '@/components/news/NewsCard.vue';
import GeometricBackdrop from '@/components/layout/GeometricBackdrop.vue';
import { Button } from '@/components/ui/button';

const PAGE_SIZE = 9;

const authStore = useAuthStore();

const posts = ref([]);
const totalCount = ref(0);
const page = ref(0);
// Starts true so the empty state can't flash before the first page arrives.
const loading = ref(true);
const loadFailed = ref(false);

const hasMore = computed(() => posts.value.length < totalCount.value);

async function loadMore() {
  loading.value = true;
  loadFailed.value = false;
  try {
    const result = await fetchPublicNews({ page: page.value + 1, pageSize: PAGE_SIZE });
    posts.value = [...posts.value, ...result.items];
    totalCount.value = result.totalCount;
    page.value += 1;
  } catch {
    loadFailed.value = true;
  } finally {
    loading.value = false;
  }
}

onMounted(loadMore);
</script>

<template>
  <section class="news-page">
    <GeometricBackdrop dense />

    <div class="news-page__inner layout-max">
      <router-link
        v-if="!authStore.isAuthenticated"
        :to="{ name: 'Home' }"
        class="news-page__back"
      >
        <ArrowLeft
          class="size-4"
          aria-hidden="true"
        />
        {{ $t('news.back_home') }}
      </router-link>

      <p class="mono-label news-page__eyebrow">
        {{ $t('news.eyebrow') }}
      </p>
      <h1 class="news-page__title font-display">
        {{ $t('news.title') }}
      </h1>
      <p class="news-page__subtitle">
        {{ $t('news.subtitle') }}
      </p>

      <p
        v-if="loadFailed"
        class="news-page__error"
        role="alert"
      >
        {{ $t('news.load_error') }}
      </p>
      <p
        v-else-if="!loading && !posts.length"
        class="news-page__hint"
      >
        {{ $t('news.empty') }}
      </p>

      <div
        v-if="posts.length"
        class="news-page__grid"
      >
        <NewsCard
          v-for="post in posts"
          :key="post.id"
          :post="post"
          :to="{ name: 'NewsDetail', params: { id: post.id } }"
        />
      </div>

      <p
        v-if="loading"
        class="news-page__hint"
      >
        {{ $t('news.loading') }}
      </p>

      <div
        v-if="hasMore && !loading"
        class="news-page__more"
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
.news-page {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  min-height: calc(100vh - var(--header-height));
  padding: clamp(2.5rem, 6vw, 4.5rem) 1.5rem clamp(4rem, 8vw, 6rem);
}

.news-page__inner {
  position: relative;
  z-index: 1;
}

.news-page__back {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  margin-bottom: 2rem;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 500;
  transition: color 0.15s ease;
}

.news-page__back:hover {
  color: var(--color-accent-coral);
}

.news-page__eyebrow {
  margin: 0 0 1rem;
  color: var(--color-accent-coral);
}

.news-page__title {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(2rem, 5vw, 2.9rem);
  font-weight: 600;
  letter-spacing: -0.03em;
  line-height: 1.1;
}

.news-page__subtitle {
  margin: 1rem 0 0;
  max-width: 38rem;
  color: var(--color-ink-muted);
  font-size: 1rem;
  line-height: 1.6;
}

.news-page__grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1.25rem;
  margin-top: 2.5rem;
}

.news-page__hint {
  margin: 2rem 0 0;
  color: var(--color-ink-muted);
}

.news-page__error {
  margin: 2rem 0 0;
  padding: 0.6rem 0.85rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.5rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
  font-size: 0.9rem;
  font-weight: 600;
}

.news-page__more {
  display: flex;
  justify-content: center;
  margin-top: 2rem;
}

@media (min-width: 640px) {
  .news-page__grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (min-width: 1024px) {
  .news-page__grid {
    grid-template-columns: repeat(3, 1fr);
  }
}
</style>
