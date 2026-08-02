<script setup>
import FooterLayout from './FooterLayout.vue';
</script>

<template>
  <div class="base-layout">
    <header class="base-header">
      <slot name="header" />
    </header>

    <main class="base-workspace">
      <slot />
    </main>

    <footer class="base-footer">
      <FooterLayout />
    </footer>
  </div>
</template>

<style scoped>
.base-layout {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
}

.base-header {
    position: fixed;
    top: 0;
    inset-inline: 0;
    z-index: 50;
    height: 4.5rem;
}

.base-workspace {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 0;
    padding-top: 4.5rem;
}
</style>