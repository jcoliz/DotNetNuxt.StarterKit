<script setup lang="ts">
import { ApiException } from '~/utils/apiclient'

const emit = defineEmits<{
  (e: 'service-unavailable', value: boolean): void
}>()

const serviceVersion = ref('')
const errors = useProblemDetails()
const isLoading = ref(true)
const serviceUnavailable = ref(false)

const client = useApiClient(VersionClient, { useTokenCheck: false, throwOnError: true })

async function getData() {
  serviceVersion.value = ''
  serviceUnavailable.value = false
  isLoading.value = true
  emit('service-unavailable', false)

  try {
    const result = await client.index()
    serviceVersion.value = await result.data.text()
  } catch (error) {
    if (ApiException.isApiException(error) && error.status === 408) {
      serviceUnavailable.value = true
      emit('service-unavailable', true)
      return
    }

    errors.handleApiError(error, 'Loading failed', 'Failed to fetch version')
  } finally {
    isLoading.value = false
  }
}

const frontEndVersion = useRuntimeConfig().public.solutionVersion

onMounted(() => {
  getData()
})
</script>

<template>
  <BaseTable data-test-id="Table">
    <template #head>
      <tr>
        <th>Property</th>
        <th>Value</th>
      </tr>
    </template>

    <template #body>
      <tr>
        <td>Service Version</td>
        <td>
          <BaseSpinner v-if="isLoading" />
          <span v-if="serviceUnavailable">Unavailable</span>
          {{ serviceVersion }}
        </td>
      </tr>

      <tr>
        <td>Front-End Version</td>
        <td>{{ frontEndVersion }}</td>
      </tr>
    </template>
  </BaseTable>
</template>
