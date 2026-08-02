<script setup>
import BaseLayout from './BaseLayout.vue';
import UnauthorizedHeader from './UnauthorizedHeader.vue';
</script>

<template>
  <BaseLayout>
    <template #header>
      <UnauthorizedHeader />
    </template>

    <template #default>
      <router-view />
    </template>
  </BaseLayout>
</template>