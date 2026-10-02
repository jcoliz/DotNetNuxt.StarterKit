<script setup lang="ts">
/**
 * Standardized display of icons from `feather-icons`
 */

const props = defineProps<{
  /**
   * Card title
   */
  title: string

  /**
   * Lines of detail text
   */
  details: string[]

  /**
   * Text to show in the link
   */
  linkText: string

  /**
   * The page to link out to
   */
  to: string
}>()
</script>
<template>
  <div class="col">
    <div class="card mb-4 rounded-3 shadow-sm">
      <div class="card-header py-3 text-bg-primary">
        <h4 class="my-0 fw-normal">{{ props.title }}</h4>
      </div>
      <div class="card-body">
        <ul class="list-unstyled mt-3 mb-4">
          <li
            v-for="detail in props.details"
            :key="detail"
          >
            {{ detail }}
          </li>
        </ul>
        <RouterLink
          :to="props.to"
          class="w-100 btn btn-lg btn-secondary"
          >{{ props.linkText }}</RouterLink
        >
      </div>
    </div>
  </div>
</template>
