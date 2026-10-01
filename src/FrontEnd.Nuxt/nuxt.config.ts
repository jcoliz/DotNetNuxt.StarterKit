// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

  devServer: {
    port: parseInt(process.env.PORT ?? '5284'),
  },

  css: ['~/assets/scss/custom.scss'],

  router: {
    options: {
      linkActiveClass: 'active',
    },
  },

  vite: {
    css: {
      preprocessorOptions: {
        scss: {
          quietDeps: true,
          // You can also silence specific deprecation types from your own code if needed
          silenceDeprecations: ['import', 'color-functions'],
        },
      },
    },
    // Fix for Node.js v24+ ESM compatibility issues during SSR prerendering
    ssr: {
      // Force these packages to be bundled with proper ESM interop
      noExternal: ['vue', 'feather-icons', '@coliz/vue-base-controls'],
    },
  },

  appConfig:
  {
    name: "DotNetNuxt.StarterKit",
  },

  runtimeConfig: {
    public: {
      // This will be replaced by NUXT_PUBLIC_SOLUTION_VERSION during build
      solutionVersion: 'nuxt.config.ts',
      // This value is overwritten during the static generation step
      // by the NUXT_PUBLIC_API_BASE_URL environment variable.
      // Don't fill this in with a default value here, or it will cause problems!
      apiBaseUrl: ``,
      // WARNING: Capitalization must match underscores exactly when overriding from environment variable
      // Leave empty — overridden at build/dev time by NUXT_PUBLIC_APPLICATION_INSIGHTS_CONNECTION_STRING
      applicationInsightsConnectionString: '',
    },
  },

  modules: ['@nuxt/eslint'],
})