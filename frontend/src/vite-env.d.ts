/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_APP_NAME?: string;
  readonly VITE_HOSPITAL_NAME?: string;
  readonly VITE_LIFF_ID?: string;
  readonly VITE_LIFF_BASE_URL?: string;
  readonly VITE_API_URL?: string;
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_AUTH_TOKEN_STORAGE_MODE?: string;
  readonly VITE_AUTH_CSRF_COOKIE_NAME?: string;
  readonly VITE_AUTH_CSRF_HEADER_NAME?: string;
}
