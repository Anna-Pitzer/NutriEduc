import { defineConfig } from 'vite'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5243',
        changeOrigin: true,
      },
    },
  },
  plugins: [tailwindcss()],
})