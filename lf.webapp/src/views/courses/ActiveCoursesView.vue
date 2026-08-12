<script setup>
import CourseCard from '@/components/courses/CourseCard.vue';
import { activeCourses } from './mockCourses';
</script>

<template>
  <div>
    <div class="max-w-2xl mb-8">
      <h2 class="text-xl font-bold text-ink">
        {{ $t('courses.active.title') }}
      </h2>
      <p class="mt-2 text-sm text-ink-muted leading-relaxed">
        {{ $t('courses.active.subtitle') }}
      </p>
    </div>

    <div
      v-if="activeCourses.length"
      class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5"
    >
      <CourseCard
        v-for="course in activeCourses"
        :key="course.id"
        status="active"
        :title="course.title"
        :description="course.description"
        :category="course.category"
        :duration="course.duration"
        :instructor="course.instructor"
        :progress="course.progress"
      />
    </div>
    <p
      v-else
      class="text-sm text-ink-muted"
    >
      {{ $t('courses.active.empty') }}
    </p>
  </div>
</template>
