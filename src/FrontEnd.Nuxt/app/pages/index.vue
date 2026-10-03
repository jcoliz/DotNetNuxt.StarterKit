<script setup lang="ts">
definePageMeta({
  title: 'Home',
  subtitle: 'Welcome to the DotNetNuxt Starter Kit.',
  order: 1,
})

/**
 * Page is ready for user input
 */
const ready = ref(false)

onMounted(() => {
  ready.value = true
})
</script>
<template>
  <div class="row row-cols-1 row-cols-lg-3 mb-3 text-center">
    <PageCard
      to="/weather"
      test-id="weather-card"
      title="Weather Forecasts"
      link-text="View forecasts"
      :disabled="!ready"
      :details="['Demonstrates API calls to backend']"
    />
    <PageCard
      to="/identity"
      test-id="profile-card"
      title="Profile"
      link-text="View user details"
      :disabled="!ready"
      :details="['Profile details of current user']"
    />
    <PageCard
      to="/about"
      test-id="about-card"
      title="About"
      link-text="Learn more"
      :disabled="!ready"
      :details="['Information about this app']"
    />
  </div>
</template>
