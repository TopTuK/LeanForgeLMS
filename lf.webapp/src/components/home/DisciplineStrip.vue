<script setup>
defineProps({
  items: {
    type: Array,
    default: () => [],
  },
});
</script>

<template>
  <ul
    class="discipline-strip"
    aria-hidden="true"
  >
    <li
      v-for="(item, index) in items"
      :key="index"
      class="discipline-strip__item mono-label"
    >
      {{ item }}
    </li>
  </ul>
</template>

<style scoped>
.discipline-strip {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 1.75rem;
  margin: 0;
  padding: 0;
  list-style: none;
  color: var(--color-ink-muted);
}

.discipline-strip__item {
  position: relative;
  padding-left: 1.6rem;
}

.discipline-strip__item::before {
  content: "";
  position: absolute;
  left: 0;
  top: 50%;
  width: 0.7rem;
  height: 1px;
  background: var(--color-accent-coral);
  transform: translateY(-50%);
}
</style>
