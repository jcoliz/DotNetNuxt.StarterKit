<script setup lang="ts">
/**
 * Base styling for tables
 */

const props = defineProps<{
  testId?: string
}>()

const slots = useSlots()
</script>

<template>
  <section :data-test-id="props.testId ?? 'base-table'">
    <table class="table">
      <thead v-if="slots.head">
        <slot name="head" />
      </thead>
      <tbody v-if="slots.body">
        <slot name="body" />
      </tbody>
    </table>
    <slot name="after" />
  </section>
</template>
