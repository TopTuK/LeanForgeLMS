<script setup>
import { computed, ref } from 'vue';
import { DialogClose, DialogContent, DialogOverlay, DialogPortal, DialogRoot, DialogTitle } from 'reka-ui';
import { ChevronLeft, ChevronRight, X } from 'lucide-vue-next';

const props = defineProps({
  images: { type: Array, default: () => [] },
  title: { type: String, default: '' },
});

const open = ref(false);
const activeIndex = ref(0);

const activeImage = computed(() => props.images[activeIndex.value] ?? null);
const hasMany = computed(() => props.images.length > 1);
const layoutClass = computed(() => `news-gallery--count-${Math.min(props.images.length, 3)}`);

function openAt(index) {
  activeIndex.value = index;
  open.value = true;
}

function step(delta) {
  const count = props.images.length;
  activeIndex.value = (activeIndex.value + delta + count) % count;
}

function onKeydown(event) {
  if (!hasMany.value) return;
  if (event.key === 'ArrowRight') step(1);
  else if (event.key === 'ArrowLeft') step(-1);
}
</script>

<template>
  <div class="news-gallery-root">
    <div
      v-if="images.length"
      class="news-gallery"
      :class="layoutClass"
    >
      <button
        v-for="(image, i) in images"
        :key="image.id"
        type="button"
        class="news-gallery__item"
        :aria-label="$t('news.open_image', { index: i + 1, total: images.length })"
        @click="openAt(i)"
      >
        <img
          :src="image.url"
          :alt="$t('news.image_alt', { title, index: i + 1 })"
          loading="lazy"
          decoding="async"
        >
      </button>
    </div>

    <DialogRoot v-model:open="open">
      <DialogPortal>
        <DialogOverlay class="fixed inset-0 z-50 bg-black/85" />
        <DialogContent
          class="news-lightbox"
          :aria-describedby="undefined"
          @keydown="onKeydown"
        >
          <DialogTitle class="sr-only">
            {{ title }}
          </DialogTitle>

          <img
            v-if="activeImage"
            :src="activeImage.url"
            :alt="$t('news.image_alt', { title, index: activeIndex + 1 })"
            class="news-lightbox__image"
          >

          <div class="news-lightbox__controls">
            <button
              v-if="hasMany"
              type="button"
              class="news-lightbox__button"
              :aria-label="$t('news.lightbox_prev')"
              @click="step(-1)"
            >
              <ChevronLeft class="size-5" />
            </button>
            <span
              v-if="hasMany"
              class="news-lightbox__counter"
            >
              {{ $t('news.lightbox_counter', { index: activeIndex + 1, total: images.length }) }}
            </span>
            <button
              v-if="hasMany"
              type="button"
              class="news-lightbox__button"
              :aria-label="$t('news.lightbox_next')"
              @click="step(1)"
            >
              <ChevronRight class="size-5" />
            </button>
            <DialogClose
              class="news-lightbox__button"
              :aria-label="$t('news.lightbox_close')"
            >
              <X class="size-5" />
            </DialogClose>
          </div>
        </DialogContent>
      </DialogPortal>
    </DialogRoot>
  </div>
</template>

<style scoped>
.news-gallery {
  display: grid;
  gap: 0.6rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.news-gallery--count-1 {
  grid-template-columns: 1fr;
}

.news-gallery__item {
  display: block;
  padding: 0;
  overflow: hidden;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.6rem;
  background: var(--color-surface-900);
  cursor: zoom-in;
}

.news-gallery__item img {
  display: block;
  width: 100%;
  aspect-ratio: 4 / 3;
  object-fit: cover;
  transition: transform 0.25s ease;
}

.news-gallery--count-1 .news-gallery__item img {
  aspect-ratio: auto;
  max-height: 30rem;
}

.news-gallery__item:hover img {
  transform: scale(1.03);
}

.news-gallery__item:focus-visible {
  outline: 2px solid var(--color-accent-coral);
  outline-offset: 2px;
}

.news-lightbox {
  position: fixed;
  inset: 0;
  z-index: 50;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  padding: 1rem;
}

.news-lightbox:focus {
  outline: none;
}

.news-lightbox__image {
  max-width: 100%;
  max-height: calc(100vh - 6rem);
  border-radius: 0.5rem;
  object-fit: contain;
}

.news-lightbox__controls {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  color: #fff;
}

.news-lightbox__button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.5rem;
  height: 2.5rem;
  border: 1px solid rgb(255 255 255 / 0.3);
  border-radius: 999px;
  background: rgb(255 255 255 / 0.08);
  color: #fff;
  transition: background-color 0.15s ease;
}

.news-lightbox__button:hover {
  background: rgb(255 255 255 / 0.2);
}

.news-lightbox__counter {
  min-width: 3.5rem;
  font-family: var(--font-mono);
  font-size: 0.8rem;
  text-align: center;
}

@media (min-width: 640px) {
  .news-gallery--count-3 {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}
</style>
