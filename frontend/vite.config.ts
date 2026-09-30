import { defineConfig, loadEnv } from 'vite'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  if (!env.VITE_API_URL) {
    throw new Error(
      'VITE_API_URL não foi definida. Crie o arquivo frontend/.env com a URL do backend.'
    )
  }

  return {
    server: {
      proxy: {
        '/api': {
          target: env.VITE_API_URL,
          changeOrigin: true,
          secure: false,
        },
      },
    },

    plugins: [tailwindcss()],
  }
})