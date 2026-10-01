<script setup lang="ts">
/**
 * Root Application Component
 *
 * The main application wrapper that sets up the layout system and page titles.
 * Dynamically updates the document title based on the current route's meta.title.
 */

const appConfig = useAppConfig()
const route = useRoute()

// Create a reactive computed title
const pageTitle = computed(() => {
  return route.meta.title ? `${route.meta.title} - ${appConfig.name}` : appConfig.name
})

useHead({
  htmlAttrs: {
    lang: 'en',
  },
  title: pageTitle,
  /* TODO:
  link: [
    { rel: 'icon', type: 'image/png', sizes: '32x32', href: '/favicon-32x32.png' },
    { rel: 'icon', type: 'image/png', sizes: '16x16', href: '/favicon-16x16.png' },
    { rel: 'apple-touch-icon', sizes: '180x180', href: '/apple-touch-icon.png' },
    { rel: 'manifest', href: '/site.webmanifest' },
  ],
  meta: [
    { name: 'theme-color', content: '#ffffff' },
    { property: 'og:title', content: appConfig.name },
    {
      property: 'og:description',
      content: appConfig.description as string,
    },
    { property: 'og:url', content: appConfig.siteUrl as string },
    { property: 'og:image', content: appConfig.ogImage as string },
    { property: 'og:type', content: 'website' },
  ],
*/
})
</script>
<template>
  <div>
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
    <AppToastHost />
  </div>
</template>
