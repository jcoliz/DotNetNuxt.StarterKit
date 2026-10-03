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

  /**
   * Whether the card button is disabled
   */
  disabled?: boolean

  /**
   * Optional Test ID for the card container, else `page-card` is used.
   */
  testId?: string
}>()
</script>
<template>
  <div
    class="col"
    :data-test-id="props.testId ?? 'page-card'"
  >
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
          v-if="!props.disabled"
          data-test-id="to-link"
          :to="props.to"
          class="w-100 btn btn-lg btn-secondary"
          >{{ props.linkText }}</RouterLink
        >
        <button
          v-else
          type="button"
          class="w-100 btn btn-lg btn-secondary"
          disabled
        >
          {{ props.linkText }}
        </button>
      </div>
    </div>
  </div>
</template>
