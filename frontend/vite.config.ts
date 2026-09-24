import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  // Прокси пересылает запросы с путём /api на локальный бэкенд,
  // поэтому фронтенду не нужен CORS.
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
    },
  },
})
