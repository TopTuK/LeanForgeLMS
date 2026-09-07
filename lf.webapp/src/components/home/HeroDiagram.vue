<script setup>
defineProps({
  labels: {
    type: Array,
    default: () => [],
  },
  imageSrc: {
    type: String,
    default: '',
  },
  imageAlt: {
    type: String,
    default: '',
  },
  detailImageSrc: {
    type: String,
    default: '',
  },
  detailImageAlt: {
    type: String,
    default: '',
  },
});
</script>

<template>
  <div class="hero-diagram">
    <div class="hero-diagram__main">
      <img
        v-if="imageSrc"
        :src="imageSrc"
        :alt="imageAlt"
        class="hero-diagram__image"
        fetchpriority="high"
      >
      <span
        class="hero-diagram__scrim"
        aria-hidden="true"
      />
    </div>

    <div
      v-if="detailImageSrc"
      class="hero-diagram__detail"
    >
      <img
        :src="detailImageSrc"
        :alt="detailImageAlt"
        decoding="async"
      >
    </div>

    <span
      class="hero-diagram__grid"
      aria-hidden="true"
    />

    <svg
      class="hero-diagram__draw"
      viewBox="0 0 480 480"
      fill="none"
      aria-hidden="true"
    >
      <rect
        x="32.5"
        y="32.5"
        width="415"
        height="415"
        stroke="currentColor"
        stroke-opacity="0.22"
      />
      <rect
        x="70"
        y="58"
        width="142"
        height="142"
        stroke="var(--band-accent)"
        stroke-width="1.25"
        transform="rotate(11 141 129)"
      />
      <circle
        cx="346"
        cy="303"
        r="74"
        stroke="currentColor"
        stroke-opacity="0.3"
      />
      <path
        d="M32 303h240M346 32v197"
        stroke="currentColor"
        stroke-opacity="0.2"
        stroke-dasharray="4 7"
      />
      <path
        d="M408 123a103 103 0 0 1 0 146"
        stroke="var(--band-accent)"
        stroke-width="1.25"
      />
      <circle
        cx="70"
        cy="58"
        r="3"
        fill="var(--band-accent)"
      />
      <circle
        cx="346"
        cy="303"
        r="3"
        fill="currentColor"
      />
    </svg>

    <ul
      v-if="labels.length"
      class="hero-diagram__labels"
    >
      <li
        v-for="(label, index) in labels"
        :key="index"
        class="mono-label"
      >
        <span class="hero-diagram__tick">{{ String(index + 1).padStart(2, '0') }}</span>
        {{ label }}
      </li>
    </ul>
  </div>
</template>

<style scoped>
.hero-diagram {
  position: relative;
  aspect-ratio: 1;
  width: 100%;
  max-width: 34rem;
  margin-inline: auto;
  border: 1px solid var(--band-line);
  border-radius: var(--radius-card);
  background: var(--band-panel);
  color: var(--band-ink);
  overflow: hidden;
  box-shadow: 0 2rem 5rem color-mix(in srgb, #000 32%, transparent);
}

.hero-diagram__main {
  position: absolute;
  inset: 0.8rem 0.8rem 5.4rem;
  overflow: hidden;
  border-radius: calc(var(--radius-card) - 0.25rem);
}

.hero-diagram__image,
.hero-diagram__scrim {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
}

.hero-diagram__image {
  object-fit: cover;
  object-position: 54% center;
}

.hero-diagram__scrim {
  background:
    linear-gradient(180deg, transparent 42%, color-mix(in srgb, var(--band-bg) 72%, transparent) 100%),
    color-mix(in srgb, var(--band-bg) 8%, transparent);
}

.hero-diagram__detail {
  position: absolute;
  z-index: 4;
  right: 1.2rem;
  bottom: 5.8rem;
  width: 40%;
  aspect-ratio: 4 / 3;
  overflow: hidden;
  border: 0.35rem solid var(--band-bg);
  border-radius: calc(var(--radius-card) * 0.75);
  box-shadow: 0 1.25rem 2.5rem color-mix(in srgb, #000 42%, transparent);
}

.hero-diagram__detail img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.hero-diagram__grid {
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(var(--band-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--band-grid) 1px, transparent 1px);
  background-size: 32px 32px;
}

.hero-diagram__draw {
  position: absolute;
  z-index: 3;
  inset: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
}

@media (prefers-reduced-motion: no-preference) {
  .hero-diagram__draw {
    animation: hero-diagram-drift 16s ease-in-out infinite alternate;
  }
}

.hero-diagram__labels {
  position: absolute;
  z-index: 5;
  left: 0;
  bottom: 0;
  width: 100%;
  min-height: 4.7rem;
  margin: 0;
  padding: 0.8rem 1rem;
  list-style: none;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  align-content: center;
  gap: 0.45rem 1.25rem;
  border-top: 1px solid var(--band-line);
  background: color-mix(in srgb, var(--band-bg) 94%, transparent);
  color: var(--band-ink);
}

.hero-diagram__labels li {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  min-width: 0;
  overflow: hidden;
  font-size: clamp(0.58rem, 1.3vw, 0.66rem);
  line-height: 1.25;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.hero-diagram__tick {
  color: var(--band-accent);
}

@keyframes hero-diagram-drift {
  from { transform: translate(0, 0); }
  to { transform: translate(-6px, 8px); }
}
</style>
