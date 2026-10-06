<script setup>
import { onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { ArrowLeft } from 'lucide-vue-next';
import { fetchTeachingCourses } from '@/services/teachingService';

const props = defineProps({
  courseId: { type: Number, required: true },
});

const route = useRoute();
const course = ref(null);

const tabs = [
  { name: 'TeachingGroups', label: 'teaching.groups_link' },
  { name: 'TeachingLectures', label: 'teaching.lectures_link' },
];

onMounted(async () => {
  try {
    course.value = (await fetchTeachingCourses()).find((c) => c.id === props.courseId) ?? null;
  } catch {
    // The header is navigation only; the page below reports its own load errors.
  }
});
</script>

<template>
  <div class="teaching-tabs">
    <router-link
      :to="{ name: 'CoursesTeaching' }"
      class="teaching-tabs__back"
    >
      <ArrowLeft class="size-4" />
      {{ $t('teaching.back') }}
    </router-link>

    <p
      v-if="course"
      class="teaching-tabs__course"
    >
      {{ course.title }}
    </p>

    <nav class="teaching-tabs__nav">
      <router-link
        v-for="tab in tabs"
        :key="tab.name"
        :to="{ name: tab.name, params: { courseId } }"
        class="teaching-tabs__tab"
        :class="{ 'is-active': route.name === tab.name }"
      >
        {{ $t(tab.label) }}
      </router-link>
      <router-link
        v-if="course?.canEditContent"
        :to="{ name: 'CourseEdit', params: { id: courseId } }"
        class="teaching-tabs__tab"
      >
        {{ $t('teaching.edit_content') }}
      </router-link>
    </nav>
  </div>
</template>

<style scoped>
.teaching-tabs {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-bottom: 1.75rem;
}

.teaching-tabs__back {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-ink-muted);
}

.teaching-tabs__back:hover {
  color: var(--color-ink);
}

.teaching-tabs__course {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--color-accent-coral);
}

.teaching-tabs__nav {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.teaching-tabs__tab {
  padding: 0.35rem 0.9rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 999px;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-ink-muted);
}

.teaching-tabs__tab.is-active {
  border-color: var(--color-ink);
  background: var(--color-ink);
  color: var(--color-surface-950);
}
</style>
