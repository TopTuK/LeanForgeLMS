<script setup>
import CourseCard from '@/components/courses/CourseCard.vue';
import { finishedCourses } from './mockCourses';
</script>

<template>
  <div>
    <div class="max-w-2xl mb-8">
      <h2 class="text-xl font-bold text-ink">
        {{ $t('courses.finished.title') }}
      </h2>
      <p class="mt-2 text-sm text-ink-muted leading-relaxed">
        {{ $t('courses.finished.subtitle') }}
      </p>
    </div>

    <div
      v-if="finishedCourses.length"
      class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5"
    >
      <CourseCard
        v-for="course in finishedCourses"
        :key="course.id"
        status="finished"
        :title="course.title"
        :description="course.description"
        :category="course.category"
        :duration="course.duration"
        :instructor="course.instructor"
        :completed-on="course.completedOn"
      />
    </div>
    <p
      v-else
      class="text-sm text-ink-muted"
    >
      {{ $t('courses.finished.empty') }}
    </p>
  </div>
</template>
