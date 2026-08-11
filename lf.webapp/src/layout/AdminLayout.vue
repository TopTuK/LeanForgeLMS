<script setup>
import { ref } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();
const isMinimized = ref(localStorage.getItem('leanforge-admin-sidebar-minimized') === 'true');

function toggleSidebar() {
  isMinimized.value = !isMinimized.value;
  localStorage.setItem('leanforge-admin-sidebar-minimized', String(isMinimized.value));
}
</script>

<template>
  <div class="admin-layout">
    <va-sidebar
      :minimized="isMinimized"
      width="16rem"
      minimized-width="4.5rem"
      color="background-secondary"
    >
      <va-sidebar-item
        :to="{ name: 'AdminUsers' }"
        :active="route.name === 'AdminUsers'"
      >
        <va-sidebar-item-content>
          <va-icon name="group" />
          <va-sidebar-item-title>{{ $t('admin.sidebar.users') }}</va-sidebar-item-title>
        </va-sidebar-item-content>
      </va-sidebar-item>
      <va-sidebar-item
        :to="{ name: 'AdminCourses' }"
        :active="route.name === 'AdminCourses'"
      >
        <va-sidebar-item-content>
          <va-icon name="school" />
          <va-sidebar-item-title>{{ $t('admin.sidebar.courses') }}</va-sidebar-item-title>
        </va-sidebar-item-content>
      </va-sidebar-item>
    </va-sidebar>

    <div class="admin-workspace">
      <div class="admin-workspace__toolbar">
        <va-button
          preset="secondary"
          icon="menu"
          round
          :aria-label="$t('admin.sidebar.toggle')"
          @click="toggleSidebar"
        />
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
    align-items: stretch;
    min-height: 0;
}

.admin-workspace {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-width: 0;
}

.admin-workspace__toolbar {
    padding: 1rem 1.5rem 0;
}

.admin-workspace__content {
    flex: 1;
    padding: 1rem 1.5rem 2rem;
}
</style>
