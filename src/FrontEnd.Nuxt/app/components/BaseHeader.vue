<script setup lang="ts">
/**
 * Standard page header styling
 *
 * Should only be one of these per `main` tag
 */

const props = defineProps<{
  /**
   * (optional) override base classes
   */
  className?: any
}>()

const displayClasses = computed(() => {
  return props.className ?? 'mb-4'
})

/**
 * Get details about slots, for conditional rendering
 */
const slots = useSlots()
</script>

<template>
  <section
    id="PageHeader"
    class="row"
    :class="displayClasses"
  >
    <div class="col-12">
      <h1>
        <slot />
      </h1>
      <Transition>
        <div
          v-if="slots.subtitle"
          class="lead text-muted"
          data-test-id="Subtitle"
        >
          <slot name="subtitle" />
        </div>
      </Transition>
    </div>
  </section>
</template>

<style scoped>
.v-enter-active,
.v-leave-active {
  transition: all 0.35s ease;
  max-height: 32px;
  overflow: hidden;
}

.v-enter-from,
.v-leave-to {
  max-height: 0;
  opacity: 0;
}
</style>
