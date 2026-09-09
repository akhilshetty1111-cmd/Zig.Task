/// <reference types="vite/client" />

/**
 * Typed environment variables. Vite only exposes vars prefixed with VITE_ to the
 * browser bundle - anything secret must stay on the server, since everything here
 * ships to the client in plain text.
 */
interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string;
  readonly VITE_APP_NAME: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
