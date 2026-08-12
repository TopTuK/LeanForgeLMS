<script setup>
</script>

<template>
  <div class="flat-card rounded-card p-10 md:p-14 flex flex-col items-center text-center gap-4 max-w-xl mx-auto">
    <div class="w-14 h-14 rounded-md bg-accent-soft flex items-center justify-center text-accent-coral">
      <svg
        width="24"
        height="24"
        viewBox="0 0 24 24"
        fill="none"
        aria-hidden="true"
      >
        <path
          d="M4 7h6v6H4V7zm10 0h6v4h-6V7zM4 15h6v4H4v-4zm10 2h6v2h-6v-2z"
          stroke="currentColor"
          stroke-width="1.5"
        />
      </svg>
    </div>
    <h2 class="text-xl font-bold text-ink">
      {{ $t('courses.create.title') }}
    </h2>
    <p class="text-sm text-ink-muted leading-relaxed">
      {{ $t('courses.create.coming_soon') }}
    </p>
    <router-link
      :to="{ name: 'CoursesAvailable' }"
      class="text-sm font-semibold text-accent-coral hover:text-accent-coral-dark transition border-b border-accent-coral/40 hover:border-accent-coral-dark"
    >
      {{ $t('courses.create.back') }}
    </router-link>
  </div>
</template>
