// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  devServer: {
    port: parseInt(process.env.PORT ?? '5284'),
  },
  css: ['~/assets/scss/custom.scss'],
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
})
