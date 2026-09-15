<script setup>
defineProps({
  images: { type: Array, default: () => [] },
});
</script>

<template>
  <div
    class="image-row"
    :class="{ 'image-row--single': images.length === 1 }"
  >
    <figure
      v-for="image in images"
      :key="image.key"
      class="image-row__item"
    >
      <img
        v-if="image.src"
        :src="image.src"
        :alt="image.alt || ''"
        class="image-row__img"
      >
      <slot
        name="item"
        :image="image"
      />
    </figure>
  </div>
</template>

<style scoped>
.image-row {
  --image-row-gap: 0.65rem;
  display: flex;
  flex-wrap: wrap;
  gap: var(--image-row-gap);
}

.image-row__item {
  position: relative;
  flex: 1 1 0;
  min-width: 0;
  height: clamp(9rem, 22vw, 16rem);
  margin: 0;
  overflow: hidden;
  border-radius: 0.55rem;
  background: var(--color-surface-900);
}

.image-row__img {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* A lone image keeps its natural aspect ratio instead of being cropped to the row height. */
.image-row--single .image-row__item {
  height: auto;
  min-height: 6rem;
  background: transparent;
}

.image-row--single .image-row__img {
  width: auto;
  max-width: 100%;
  height: auto;
  max-height: 22rem;
  margin: 0 auto;
  border-radius: 0.55rem;
  object-fit: contain;
}

@media (max-width: 40rem) {
  .image-row__item {
    flex-basis: calc(50% - var(--image-row-gap) / 2);
  }
}

@media (max-width: 24rem) {
  .image-row__item {
    flex-basis: 100%;
  }
}
</style>
