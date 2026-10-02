<script setup lang="ts">
definePageMeta({
  title: 'Home',
  subtitle: 'Welcome to the DotNetNuxt Starter Kit.',
  order: 1,
})

//TODO: const identityStore = useIdentityStore()
</script>
<template>
  <div class="row row-cols-1 row-cols-lg-3 mb-3 text-center">
    <PageCard
      to="/weather"
      title="Weather Forecasts"
      link-text="View forecasts"
      :details="['Demonstrates API calls to backend']"
    />
    <PageCard
      to="/profile"
      title="Profile"
      link-text="View user details"
      :details="['Profile details of current user']"
    />
    <PageCard
      to="/about"
      title="About"
      link-text="Learn more"
      :details="['Information about this app']"
    />
  </div>
</template>
