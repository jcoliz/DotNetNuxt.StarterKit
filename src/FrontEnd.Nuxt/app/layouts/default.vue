<template>
  <div data-test-id="default-layout">
    <SiteHeader />
    <main class="container">
      <RoutePageHeader />
      <ProblemDetailsViewer />
      <slot />
    </main>
    <!-- ErrorToast/> -->
  </div>
</template>
