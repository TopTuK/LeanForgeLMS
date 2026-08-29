<script setup>
import BaseLayout from './BaseLayout.vue';
import UnauthorizedHeader from './UnauthorizedHeader.vue';
import PublicFooter from './PublicFooter.vue';
</script>

<template>
  <BaseLayout>
    <template #header>
      <UnauthorizedHeader />
    </template>

    <template #default>
      <router-view />
    </template>

    <template #footer>
      <PublicFooter />
    </template>
  </BaseLayout>
</template>
