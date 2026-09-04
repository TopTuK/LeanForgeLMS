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
});
</script>

<template>
  <div
    class="hero-diagram"
  >
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
    <span
      class="hero-diagram__grid"
      aria-hidden="true"
    />

    <svg
      class="hero-diagram__draw"
      viewBox="0 0 320 320"
      fill="none"
      aria-hidden="true"
    >
      <rect
        x="40.5"
        y="40.5"
        width="239"
        height="239"
        stroke="currentColor"
        stroke-opacity="0.35"
      />
      <rect
        x="96"
        y="70"
        width="96"
        height="96"
        stroke="var(--band-accent)"
        stroke-width="1.25"
        transform="rotate(14 144 118)"
      />
      <circle
        cx="205"
        cy="205"
        r="52"
        stroke="currentColor"
        stroke-opacity="0.4"
      />
      <path
        d="M40 205h130M205 40v130"
        stroke="currentColor"
        stroke-opacity="0.25"
        stroke-dasharray="3 5"
      />
      <path
        d="M255 96a70 70 0 0 1 0 99"
        stroke="var(--band-accent)"
        stroke-width="1.25"
      />
      <circle
        cx="96"
        cy="70"
        r="2.5"
        fill="var(--band-accent)"
      />
      <circle
        cx="205"
        cy="205"
        r="2.5"
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
  max-width: 28rem;
  margin-left: auto;
  border: 1px solid var(--band-line);
  background: var(--band-panel);
  color: var(--band-ink);
  overflow: hidden;
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
}

.hero-diagram__scrim {
  background:
    linear-gradient(180deg, transparent 40%, color-mix(in srgb, var(--band-bg) 88%, transparent) 100%),
    color-mix(in srgb, var(--band-bg) 12%, transparent);
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
  inset: 0;
  width: 100%;
  height: 100%;
}

@media (prefers-reduced-motion: no-preference) {
  .hero-diagram__draw {
    animation: hero-diagram-drift 16s ease-in-out infinite alternate;
  }
}

.hero-diagram__labels {
  position: absolute;
  left: 1.1rem;
  bottom: 1.1rem;
  right: 1.1rem;
  margin: 0;
  padding: 0;
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  color: var(--band-ink);
  text-shadow: 0 1px 12px var(--band-bg);
}

.hero-diagram__labels li {
  display: flex;
  align-items: baseline;
  gap: 0.55rem;
}

.hero-diagram__tick {
  color: var(--band-accent);
}

@keyframes hero-diagram-drift {
  from { transform: translate(0, 0); }
  to { transform: translate(-6px, 8px); }
}
</style>
