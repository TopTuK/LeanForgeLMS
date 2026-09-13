<script setup>
import { ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { ArrowLeft } from 'lucide-vue-next';
import { fetchPublicNewsPost } from '@/services/newsService';
import NewsArticle from '@/components/news/NewsArticle.vue';
import GeometricBackdrop from '@/components/layout/GeometricBackdrop.vue';

const route = useRoute();

const post = ref(null);
const loading = ref(false);
const notFound = ref(false);
const loadFailed = ref(false);

async function load(id) {
  loading.value = true;
  notFound.value = false;
  loadFailed.value = false;
  post.value = null;
  try {
    post.value = await fetchPublicNewsPost(id);
  } catch (err) {
    if (err.response?.status === 404) notFound.value = true;
    else loadFailed.value = true;
  } finally {
    loading.value = false;
  }
}

watch(() => route.params.id, (id) => {
  if (id) load(id);
}, { immediate: true });
</script>

<template>
  <section class="news-detail">
    <GeometricBackdrop dense />

    <div class="news-detail__inner">
      <router-link
        :to="{ name: 'NewsList' }"
        class="news-detail__back"
      >
        <ArrowLeft
          class="size-4"
          aria-hidden="true"
        />
        {{ $t('news.back_to_list') }}
      </router-link>

      <p
        v-if="loading"
        class="news-detail__hint"
      >
        {{ $t('news.loading') }}
      </p>
      <p
        v-else-if="notFound"
        class="news-detail__hint"
      >
        {{ $t('news.not_found') }}
      </p>
      <p
        v-else-if="loadFailed"
        class="news-detail__error"
        role="alert"
      >
        {{ $t('news.load_error') }}
      </p>
      <NewsArticle
        v-else-if="post"
        :post="post"
        heading-tag="h1"
      />
    </div>
  </section>
</template>

<style scoped>
.news-detail {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  min-height: calc(100vh - var(--header-height));
  padding: clamp(2.5rem, 6vw, 4.5rem) 1.5rem clamp(4rem, 8vw, 6rem);
}

.news-detail__inner {
  position: relative;
  z-index: 1;
  max-width: 48rem;
  margin-inline: auto;
}

.news-detail__back {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  margin-bottom: 1.5rem;
  color: var(--color-ink-muted);
  font-size: 0.82rem;
  font-weight: 500;
  transition: color 0.15s ease;
}

.news-detail__back:hover {
  color: var(--color-accent-coral);
}

.news-detail__hint {
  color: var(--color-ink-muted);
}

.news-detail__error {
  padding: 0.6rem 0.85rem;
  border: 1px solid var(--color-accent-coral);
  border-radius: 0.5rem;
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
  font-size: 0.9rem;
  font-weight: 600;
}
</style>
