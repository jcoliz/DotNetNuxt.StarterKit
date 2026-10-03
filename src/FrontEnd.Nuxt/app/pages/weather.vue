<script setup lang="ts">
import { ref, onMounted } from 'vue'
import * as api from '../utils/apiclient'

definePageMeta({
  title: 'Weather',
  subtitle: 'This component demonstrates showing data loaded from a backend API service.',
  order: 2,
})

/**
 * Forecast data to display
 */
const forecasts = ref<api.IWeatherForecast[]>()

/**
 * Whether we are loading data from the server presently
 */
const isLoading = ref(false)

/**
 * Whether the data has finished loading
 */
const hasLoaded = ref(false)

/**
 * Client for communicating with server
 */
const client = useApiClient(api.WeatherClient)

/**
 * Get items from the server
 */
async function getData() {
  forecasts.value = undefined
  isLoading.value = true
  hasLoaded.value = false

  try {
    // Errors are reported to problem details by the client; result is undefined on failure
    forecasts.value = await client.get(0, 5)
  } finally {
    isLoading.value = false
    hasLoaded.value = true
  }
}

/**
 * When mounted, get the view data from server
 */
onMounted(() => {
  getData()
})
</script>

<template>
  <div>
    <p v-if="isLoading"><em>Loading...</em></p>
    <table
      v-else
      class="table"
      data-test-id="results"
    >
      <thead>
        <tr>
          <th>Date</th>
          <th>Temp. (C)</th>
          <th>Temp. (F)</th>
          <th>Summary</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="forecast in forecasts"
          :key="forecast.id"
        >
          <td>
            {{ forecast.date?.toLocaleDateString() }} {{ forecast.date?.toLocaleTimeString() }}
          </td>
          <td>{{ forecast.temperatureC }}</td>
          <td>{{ forecast.temperatureF }}</td>
          <td>{{ forecast.summary }}</td>
        </tr>
      </tbody>
    </table>
    <hr
      v-if="hasLoaded"
      data-test-id="has-loaded"
    />
  </div>
</template>
