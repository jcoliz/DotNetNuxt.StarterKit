<script setup lang="ts">
/**
 * A Page Header using the current route title for its text
 *
 * If no title is defined, will not display.
 *
 * Subtitle is resolved from route meta (in priority order):
 * 1. `route.meta.showCurrentList` — dynamic current list name
 * 2. `route.meta.subtitle` — static subtitle string
 */

const route = useRoute()

/**
 * Debug loggging for AB#2102. Consider removing this after the issue is resolved.
 */
const { logger } = useDebugLog('RoutePageHeader')

/**
 * App name from app config for subtitle substitutions
 */
const { name: appName } = useAppConfig()

/**
 * Whether to show the current list name as subtitle
 */
const shouldShowCurrentList = computed(() => !!route.meta.showCurrentList)

/**
 * Static subtitle from route meta
 */
const metaSubtitle = computed(() => route.meta.subtitle as string | undefined)

/**
 * Resolved subtitle after substitutions
 */
const resolvedMetaSubtitle = computed(() => {
  if (!metaSubtitle.value) return undefined
  return metaSubtitle.value.replaceAll('{appConfig.name}', appName)
})
</script>
<template>
  <BaseHeader v-if="$route.meta.title">
    <template #default>{{ $route.meta.title }}</template>
    <template
      v-if="resolvedMetaSubtitle"
      #subtitle
    >
      {{ resolvedMetaSubtitle }}
    </template>
    <template
      v-else
      #subtitle
    >
    </template>
  </BaseHeader>
</template>
