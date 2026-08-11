<template>
  <div class="admin-courses">
    <h1 class="admin-courses__title">
      {{ $t('admin.sidebar.courses') }}
    </h1>
    <p class="admin-courses__placeholder">
      {{ $t('admin.courses.coming_soon') }}
    </p>
  </div>
</template>

<style scoped>
.admin-courses__title {
    font-size: 1.5rem;
    font-weight: 700;
    margin-bottom: 1rem;
}

.admin-courses__placeholder {
    color: var(--color-ink-muted);
}
</style>
