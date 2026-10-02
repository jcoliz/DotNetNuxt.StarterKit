<script setup lang="ts">
/**
 * Bootstrap drop-down
 *
 * This component can only be used client-side. Be sure to wrap it in
 * <ClientOnly>
 *
 * Note: We cannot use slot VNode `.el` to find the trigger element, because
 * calling a slot function in script returns detached VNodes (never mounted by
 * Vue's renderer), so `.el` is always null. Instead we use a templateRef on
 * the wrapper div and access `firstElementChild` to reach the actual mounted
 * trigger button.
 */

import { Dropdown } from 'bootstrap'
import { ref, onMounted } from 'vue'

const { logger } = useDebugLog('DropDownClientOnly')

const dropdown = ref<Dropdown>()
const triggerWrapperRef = ref<HTMLElement>()

onMounted(() => {
  // Wait for DOM to fully render
  nextTick(() => {
    // Use the templateRef on the wrapper div to reach the actual mounted
    // trigger element. Slot VNode `.el` is always null for detached VNodes
    // returned by calling a slot function in script.
    const el = triggerWrapperRef.value?.firstElementChild as HTMLElement | null

    if (!el) {
      logger.error('No trigger element found')
      return
    }

    if (!Dropdown.getInstance(el)) {
      dropdown.value = new Dropdown(el)
      logger.debug('OK')
    }
  })
})
</script>
<template>
  <div
    ref="triggerWrapperRef"
    class="dropdown d-flex"
  >
    <slot name="trigger" />
    <slot />
  </div>
</template>
