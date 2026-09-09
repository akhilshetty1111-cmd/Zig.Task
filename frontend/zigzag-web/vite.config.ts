// `defineConfig` is imported from 'vitest/config' rather than 'vite' so the
// `test` block below is type-checked. The one from 'vite' does not know about it.
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'node:path';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],

  resolve: {
    // Array form (not the object shorthand) so anchored regexes can be used.
    alias: [
      // Absolute imports: `import { apiClient } from '@/api/client'`.
      // Keeps deep feature folders from producing '../../../..' import chains.
      { find: /^@\//, replacement: `${path.resolve(__dirname, './src')}/` },
    ],

    // MUI packages declare `main` -> ./node/*.js (CommonJS) and
    // `module` -> ./index.js (ESM), with no "exports" map to arbitrate.
    // Preferring `module` keeps MUI on its ESM build, which matters because the
    // CommonJS build require()s @emotion/styled - and Emotion's exports map
    // resolves that to an ESM file, producing "Cannot use import statement
    // outside a module". Staying ESM end to end avoids the mismatch entirely.
    mainFields: ['module', 'browser', 'main'],
  },

  // Vitest runs modules through Vite's SSR pipeline, which has its own
  // resolution settings; the client-side mainFields above do not apply there.
  ssr: {
    resolve: {
      mainFields: ['module', 'main'],
    },
  },

  server: {
    port: 5173,
    // Fail loudly instead of silently moving to 5174 - the backend CORS policy
    // whitelists 5173 exactly, so a shifted port would produce confusing errors.
    strictPort: true,
  },

  build: {
    outDir: 'dist',
    sourcemap: true,
  },

  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test/setup.ts',
    css: false,
  },
});
