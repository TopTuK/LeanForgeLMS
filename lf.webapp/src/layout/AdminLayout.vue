<script setup>
import { ref } from 'vue';
import { useRoute } from 'vue-router';
import { BookOpen, PanelLeft, Tags, Users } from 'lucide-vue-next';

const route = useRoute();
const isMinimized = ref(localStorage.getItem('leanforge-admin-sidebar-minimized') === 'true');

const links = [
  { name: 'AdminUsers', labelKey: 'admin.sidebar.users', icon: Users },
  { name: 'AdminCourses', labelKey: 'admin.sidebar.courses', icon: BookOpen },
  { name: 'AdminCategories', labelKey: 'admin.sidebar.categories', icon: Tags },
];

function toggleSidebar() {
  isMinimized.value = !isMinimized.value;
  localStorage.setItem('leanforge-admin-sidebar-minimized', String(isMinimized.value));
}
</script>

<template>
  <div class="admin-layout">
    <aside
      class="admin-sidebar"
      :class="{ 'is-minimized': isMinimized }"
    >
      <nav class="admin-sidebar__nav">
        <router-link
          v-for="link in links"
          :key="link.name"
          :to="{ name: link.name }"
          class="admin-sidebar__link"
          :class="{ 'is-active': route.name === link.name }"
        >
          <component
            :is="link.icon"
            class="size-4 shrink-0"
          />
          <span v-if="!isMinimized">{{ $t(link.labelKey) }}</span>
        </router-link>
      </nav>
    </aside>

    <div class="admin-workspace">
      <div class="admin-workspace__toolbar">
        <button
          type="button"
          class="admin-workspace__toggle"
          :aria-label="$t('admin.sidebar.toggle')"
          @click="toggleSidebar"
        >
          <PanelLeft class="size-4" />
        </button>
      </div>
      <div class="admin-workspace__content">
        <router-view />
      </div>
    </div>
  </div>
</template>

<style scoped>
.admin-layout {
  display: flex;
  flex: 1;
  min-height: 0;
}

.admin-sidebar {
  width: 16rem;
  flex-shrink: 0;
  border-right: 1px solid var(--color-border-subtle);
  background: var(--color-surface-900);
  padding: 1.25rem 0.75rem;
  transition: width 0.18s ease;
}

.admin-sidebar.is-minimized {
  width: 4.25rem;
}

.admin-sidebar__nav {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.admin-sidebar__link {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  padding: 0.65rem 0.75rem;
  border-radius: 0.5rem;
  color: var(--color-ink-muted);
  font-size: 0.9rem;
  font-weight: 600;
  text-decoration: none;
}

.admin-sidebar__link:hover,
.admin-sidebar__link.is-active {
  background: var(--color-surface-800);
  color: var(--color-ink);
}

.admin-workspace {
  display: flex;
  min-width: 0;
  flex: 1;
  flex-direction: column;
}

.admin-workspace__toolbar {
  padding: 1rem 1.5rem 0;
}

.admin-workspace__toggle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.25rem;
  height: 2.25rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.5rem;
  background: var(--color-card);
  color: var(--color-ink-muted);
}

.admin-workspace__content {
  flex: 1;
  padding: 1rem 1.5rem 2rem;
}
</style>
