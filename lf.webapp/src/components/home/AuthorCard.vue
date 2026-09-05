<script setup>
defineProps({
  name: { type: String, required: true },
  role: { type: String, required: true },
  bio: { type: String, required: true },
  highlights: { type: Array, default: () => [] },
  cta: { type: String, required: true },
  href: { type: String, required: true },
  initials: { type: String, default: 'SS' },
  photo: { type: String, default: '' },
});
</script>

<template>
  <article class="author-card">
    <div class="author-card__aside">
      <img
        v-if="photo"
        :src="photo"
        :alt="name"
        class="author-card__photo"
      >
      <span
        v-else
        class="author-card__mark"
        aria-hidden="true"
      >{{ initials }}</span>
      <span class="author-card__coords mono-label">s-sidorov.ru</span>
    </div>

    <div class="author-card__body">
      <p class="mono-label author-card__role">
        {{ role }}
      </p>
      <h3 class="author-card__name font-display">
        {{ name }}
      </h3>
      <p class="author-card__bio">
        {{ bio }}
      </p>

      <ul
        v-if="highlights.length"
        class="author-card__tags"
      >
        <li
          v-for="item in highlights"
          :key="item"
        >
          {{ item }}
        </li>
      </ul>

      <a
        :href="href"
        target="_blank"
        rel="noopener noreferrer"
        class="author-card__cta"
      >
        {{ cta }}
        <span aria-hidden="true">→</span>
      </a>
    </div>
  </article>
</template>

<style scoped>
.author-card {
  display: grid;
  grid-template-columns: minmax(0, 14rem) 1fr;
  gap: clamp(1.5rem, 4vw, 3rem);
  padding: clamp(1.75rem, 4vw, 2.75rem);
  background: var(--industrial-panel);
  backdrop-filter: blur(10px);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
}

.author-card__aside {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.author-card__mark,
.author-card__photo {
  display: grid;
  place-items: center;
  width: 100%;
  max-width: 9rem;
  aspect-ratio: 1;
  border-radius: var(--radius-md);
}

.author-card__mark {
  background: var(--color-accent-soft);
  color: var(--color-accent-coral);
  font-family: var(--font-display);
  font-size: 2.25rem;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.author-card__photo {
  object-fit: cover;
  object-position: top center;
  border: 1px solid var(--color-border-subtle);
}

.author-card__coords {
  color: var(--color-ink-faint);
}

.author-card__role {
  margin: 0 0 0.65rem;
  color: var(--color-accent-coral);
}

.author-card__name {
  margin: 0;
  color: var(--color-ink);
  font-size: clamp(1.6rem, 3.5vw, 2.25rem);
  font-weight: 600;
  letter-spacing: -0.03em;
}

.author-card__bio {
  margin: 1rem 0 0;
  max-width: 40rem;
  color: var(--color-ink-muted);
  font-size: 0.98rem;
  line-height: 1.7;
}

.author-card__tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin: 1.5rem 0 0;
  padding: 0;
  list-style: none;
}

.author-card__tags li {
  padding: 0.3rem 0.7rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-pill);
  color: var(--color-ink-muted);
  font-size: 0.75rem;
  font-weight: 500;
}

.author-card__cta {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  width: fit-content;
  margin-top: 1.75rem;
  color: var(--color-accent-coral);
  font-size: 0.9rem;
  font-weight: 600;
  border-bottom: 1px solid color-mix(in srgb, var(--color-accent-coral) 40%, transparent);
  transition: color 0.15s ease, border-color 0.15s ease;
}

.author-card__cta:hover {
  color: var(--color-accent-coral-dark);
  border-color: var(--color-accent-coral-dark);
}

@media (max-width: 767px) {
  .author-card {
    grid-template-columns: 1fr;
  }

  .author-card__aside {
    flex-direction: row;
    align-items: center;
  }

  .author-card__mark,
  .author-card__photo {
    max-width: 5rem;
  }
}
</style>
