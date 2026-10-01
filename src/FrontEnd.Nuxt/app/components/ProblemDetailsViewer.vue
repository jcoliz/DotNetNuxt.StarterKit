<script setup lang="ts">
/**
 * Error Display Component
 *
 * Displays error messages in a Bootstrap alert with optional expandable details.
 * Supports RFC 7807 Problem Details format and shows trace IDs for server errors.
 */

const showMore = ref(false)

const errors = useProblemDetails()

const isServerError = computed(() => {
  const status = errors.problem.value?.status
  return status !== undefined && status >= 500
})

const additionalFields = computed(() => {
  if (!errors.problem.value) return undefined

  const standardFields = ['type', 'title', 'status', 'detail', 'instance', 'errors']
  const entries = Object.entries(errors.problem.value).filter(
    ([key]) => !standardFields.includes(key),
  )

  return entries.length > 0 ? Object.fromEntries(entries) : undefined
})

// Validation problems list what the user can fix, so they are shown inline, not under details
const validationErrors = computed(() => {
  const raw = errors.problem.value?.errors
  if (!raw || typeof raw !== 'object') return undefined

  const items = Object.entries(raw as Record<string, unknown>).flatMap(([field, messages]) =>
    (Array.isArray(messages) ? messages : [messages]).map((message) => ({
      field,
      message: String(message),
    })),
  )

  return items.length > 0 ? items : undefined
})

// Auto-expand for server errors when there are additional fields
// Reset when switching from server error to non-server error
watch(
  () => [errors.problem.value, additionalFields.value, isServerError.value],
  () => {
    if (isServerError.value && additionalFields.value) {
      showMore.value = true
    } else if (!isServerError.value) {
      showMore.value = false
    }
  },
  { immediate: true },
)

const close = () => {
  errors.clearError()
}

const toggleMore = () => {
  showMore.value = !showMore.value
}
</script>
<template>
  <div
    v-if="errors.problem.value"
    class="alert alert-danger alert-dismissible fade show"
    role="alert"
    data-test-id="problem-details-viewer"
  >
    <strong data-test-id="title-display">{{
      errors.problem.value?.title || 'Please fix the following errors:'
    }}</strong
    ><br />
    <span
      v-if="errors.problem.value?.detail"
      data-test-id="detail-display"
    >
      {{ errors.problem.value?.detail }}
    </span>
    <ul
      v-if="validationErrors"
      class="mt-2 mb-0"
      data-test-id="validation-errors"
    >
      <li
        v-for="(item, index) in validationErrors"
        :key="index"
      >
        <strong v-if="item.field">{{ item.field }}:</strong>
        {{ item.message }}
      </li>
    </ul>
    <div
      v-if="additionalFields"
      class="mt-2"
    >
      <a
        href="#"
        class="small text-danger text-decoration-none"
        data-test-id="more-button"
        @click.prevent="toggleMore"
      >
        <FeatherIcon
          :icon="showMore ? 'chevron-up' : 'chevron-down'"
          size="16"
          class="me-1"
        />
        {{ showMore ? 'Hide details' : 'Show details' }}
      </a>
      <div
        v-if="showMore"
        class="mt-2 small"
        data-test-id="more-text"
      >
        <p class="mb-2">
          <strong v-if="isServerError"
            >Please contact support immediately so we can resolve this issue.</strong
          >
          <span
            v-else
            class="text-muted"
            >Error details (provide this information if contacting support):</span
          >
        </p>
        <div
          v-for="[key, value] in Object.entries(additionalFields)"
          :key="key"
          class="mb-1"
        >
          <strong>{{ key }}:</strong>
          <span class="ms-1">{{ typeof value === 'object' ? JSON.stringify(value) : value }}</span>
        </div>
      </div>
    </div>
    <button
      type="button"
      class="btn-close"
      aria-label="Close"
      data-test-id="close-button"
      @click="close"
    ></button>
  </div>
</template>
