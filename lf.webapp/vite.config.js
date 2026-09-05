/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin(), tailwindcss()],
    server: {
      host: true,
      port: 5173,
      // LF.WebApi proxies SPA requests here and forwards the original Host header; allow the
      // ngrok tunnel domains used by the AppHost "payment-check" profile. Inert for localhost dev.
      allowedHosts: ['.ngrok-free.app', '.ngrok.app', '.ngrok.io'],
    },
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url))
      },
    },
    build: {
      outDir: '../LF.WebApi/wwwroot',
      emptyOutDir: true,
    },
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./vitest.setup.js'],
      css: false,
      include: ['src/**/*.spec.js'],
      reporters: process.env.CI ? ['default', 'junit'] : ['default'],
      outputFile: { junit: './test-results/junit.xml' },
      coverage: {
        provider: 'v8',
        reportsDirectory: './coverage',
        include: ['src/**/*.{js,vue}'],
        exclude: ['src/main.js', 'src/**/*.spec.js', 'src/i18n/**', 'src/assets/**', 'src/test/**'],
      },
    },
})
