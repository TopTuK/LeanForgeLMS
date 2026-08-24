<template>
  <footer class="app-footer">
    <div class="container mx-auto px-6 py-8">
      <div class="app-footer__content">
        <p class="app-footer__meta">
          <span class="app-footer__brand">{{ $t('common.brand_name') }}</span>
          <span class="app-footer__dot">·</span>
          <span class="app-footer__author">{{ $t('common.author_name') }}</span>
          <span class="app-footer__year">&copy; {{ $t('common.created_date') }}</span>
        </p>

        <nav class="app-footer__links">
          <a href="#contacts">{{ $t('footer.privacy') }}</a>
          <a href="#contacts">{{ $t('footer.offer') }}</a>
          <a href="#contacts">{{ $t('footer.cookies') }}</a>
        </nav>
      </div>
    </div>
  </footer>
</template>

<style scoped>
.app-footer {
  border-top: 1px solid var(--color-border-subtle);
  background-color: var(--color-surface-900);
}

.app-footer__content {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.app-footer__meta {
  margin: 0;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.45rem;
  font-size: 0.875rem;
  color: var(--color-ink-muted);
}

.app-footer__brand,
.app-footer__author {
  color: var(--color-ink);
  font-weight: 600;
}

.app-footer__links {
  display: flex;
  flex-wrap: wrap;
  gap: 1.25rem;
}

.app-footer__links a {
  font-size: 0.875rem;
  color: var(--color-ink-muted);
  text-decoration: none;
  border-bottom: 1px solid transparent;
}

.app-footer__links a:hover {
  color: var(--color-ink);
  border-bottom-color: var(--color-ink);
}
</style>
