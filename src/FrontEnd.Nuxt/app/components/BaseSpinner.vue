<script setup lang="ts">
defineProps<{
  /**
   * Whether to show the text "Loading..." (optional)
   *
   * Else only the spinner itself will be shown
   */
  showLoadingText?: boolean
}>()
</script>
<template>
  <div
    data-test-id="base-spinner"
    class="d-flex align-items-center"
  >
    <div
      class="spinner-border spinner-border-sm"
      aria-hidden="true"
    />
    <div
      v-if="showLoadingText"
      class="ms-2"
      style="padding-bottom: 2px"
    >
      Loading...
    </div>
  </div>
</template>
