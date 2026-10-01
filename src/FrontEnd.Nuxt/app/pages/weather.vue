<script setup lang="ts">
import { ref, onMounted } from "vue"
import * as api from "../utils/apiclient"

definePageMeta({
    title: 'Weather',
    order: 2
})

/**
 * Forecast data to display
 */
const forecasts = ref<api.IWeatherForecast[]>()

/**
 * Error display state
 */
const errors = useProblemDetails()

/**
 * Whether we are loading data from the server presently
 */
 const isLoading = ref(false)

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

  client.get(0, 5)
    .then((result) => {
      forecasts.value = result
    })
    .catch((error) => {
      errors.handleApiError(error, 'Loading failed', 'Failed to fetch weather data')
    })
    .finally(() => {
      isLoading.value = false
    })
}

/**
 * When mounted, get the view data from server
 */
 onMounted(() => {
  getData()
})

</script>

<template>
    <main>
        <h1>Weather</h1>

        <p>This component demonstrates showing data loaded from a backend API service.</p>

        <ProblemDetailsViewer />

        <p v-if="isLoading"><em>Loading...</em></p>
        <table v-else class="table">
            <thead>
            <tr>
                <th>Date</th>
                <th>Temp. (C)</th>
                <th>Temp. (F)</th>
                <th>Summary</th>
            </tr>
            </thead>
            <tbody>
            <tr v-for="forecast in forecasts" :key="forecast.id">
                <td>{{ forecast.date?.toLocaleDateString() }} {{ forecast.date?.toLocaleTimeString() }}</td>
                <td>{{ forecast.temperatureC }}</td>
                <td>{{ forecast.temperatureF }}</td>
                <td>{{ forecast.summary }}</td>
            </tr>
            </tbody>
        </table>
    </main>

</template>
