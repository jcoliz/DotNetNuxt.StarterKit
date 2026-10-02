<script setup lang="ts">
import App from '~/app.vue'

definePageMeta({
  title: 'About',
  subtitle: 'Information about this app.',
  order: 100,
})
</script>
<template>
  <AppVersionTable />
</template>
