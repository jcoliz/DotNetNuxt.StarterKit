<script setup lang="ts">
import type { LoginStateModel } from '~/utils/loginState'

const appConfig = useAppConfig()

// TODO: Populate from a real identity provider
const loginState = reactive<LoginStateModel>({
  isLoggedIn: false,
  name: undefined,
  photo: undefined,
  profileRoute: '/identity',
  onLogin: () => {
    loginState.isLoggedIn = true
    loginState.name = 'User Name'
  },
  onLogout: () => {
    loginState.isLoggedIn = false
    loginState.name = undefined
    loginState.photo = undefined
  },
})
const displayRoutes = useRouter()
  .getRoutes()
  .filter((x) => x.meta.order)
  .sort((x, y) => (x.meta.order as number) - (y.meta.order as number))
</script>
<template>
  <div
    class="container"
    data-test-id="site-header"
  >
    <header class="d-flex flex-wrap justify-content-center py-3 mb-4 border-bottom">
      <NuxtLink
        to="/"
        class="d-flex align-items-center mb-3 mb-md-0 me-md-auto link-body-emphasis text-decoration-none"
      >
        <FeatherIcon
          icon="users"
          size="32"
        />
        <span class="ms-3 fs-4 me-3">{{ appConfig.name }}</span>
      </NuxtLink>

      <ul class="nav nav-pills">
        <NuxtLink
          v-for="route of displayRoutes"
          :key="route.meta.order as number"
          class="nav-link"
          :to="route.path"
          >{{ route.meta.title }}</NuxtLink
        >
      </ul>

      <BaseLoginState :state="loginState" />
    </header>
  </div>
</template>
