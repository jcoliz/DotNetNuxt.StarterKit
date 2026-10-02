<script setup lang="ts">
/**
 * Displays the current user's identity and login/logout actions.
 *
 * In practice, we would create a component unique to our auth provider, and
 * fill these LoginStateModel values accordingly.
 */

import type { LoginStateModel } from '~/utils/loginState'

defineProps<{
  /** Current user identity and the actions used to log in or out. */
  state: LoginStateModel
}>()
</script>

<template>
  <DropDownPortable class="ms-2 my-1 d-flex align-items-middle">
    <template #trigger>
      <a
        class="d-flex align-items-center link-body-emphasis text-decoration-none p-0 dropdown-toggle"
        data-bs-toggle="dropdown"
        aria-expanded="false"
      >
        <template v-if="state.isLoggedIn">
          <img
            v-if="state.photo"
            :src="state.photo"
            alt=""
            width="32"
            height="32"
            class="rounded-circle me-2"
          />
          <strong>{{ state.name }}</strong>
        </template>
        <FeatherIcon
          v-else
          icon="user"
          size="24"
          class="rounded-circle me-2"
        />
      </a>
    </template>
    <template #default>
      <!-- Note that popper is handling absolute positioning of the drop-down -->
      <ul class="dropdown-menu dropdown-menu-end text-small shadow">
        <template v-if="state.isLoggedIn">
          <li>
            <NuxtLink
              class="dropdown-item"
              :to="state.profileRoute"
              >Profile</NuxtLink
            >
          </li>
          <li><hr class="dropdown-divider" /></li>
          <li>
            <a
              class="dropdown-item"
              @click="state.onLogout()"
              >Sign out</a
            >
          </li>
        </template>
        <template v-else>
          <li>
            <a
              class="dropdown-item"
              @click="state.onLogin()"
              >Sign in</a
            >
          </li>
        </template>
      </ul>
    </template>
  </DropDownPortable>
</template>
