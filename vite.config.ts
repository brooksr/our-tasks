import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';
import { loadEnv } from 'vite';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '');
  return {
    base: env.VITE_BASE_PATH || '/',
    plugins: [react()],
    test: {
      css: true,
      environment: 'jsdom',
      globals: true,
      setupFiles: './src/test/setup.ts'
    }
  };
});
