<script setup>
import FooterLayout from './FooterLayout.vue';
</script>

<template>
  <div class="base-layout">
    <header class="base-header">
      <slot name="header" />
    </header>

    <div class="base-scroll">
      <main class="base-workspace">
        <slot />
      </main>

      <footer class="base-footer">
        <slot name="footer">
          <FooterLayout />
        </slot>
      </footer>
    </div>
  </div>
</template>

<style scoped>
.base-layout {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}

.base-header {
  flex-shrink: 0;
  z-index: 50;
  height: var(--header-height, 4.5rem);
}

.base-scroll {
  flex: 1;
  min-height: 0;
  overflow-x: hidden;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
}

.base-workspace {
  flex: 1 0 auto;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.base-footer {
  flex-shrink: 0;
}
</style>
