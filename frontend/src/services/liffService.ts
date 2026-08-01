import liff from "@line/liff";

let initPromise: Promise<void> | null = null;

export function getLiffId() {
  return import.meta.env.VITE_LIFF_ID ?? "";
}

export async function initializeLiff() {
  const liffId = getLiffId();
  if (!liffId) {
    throw new Error("VITE_LIFF_ID is not configured.");
  }

  initPromise ??= liff.init({ liffId });
  return initPromise;
}

export async function ensureLiffLogin() {
  await initializeLiff();
  if (!liff.isLoggedIn()) {
    liff.login({ redirectUri: window.location.href });
    return false;
  }

  return true;
}

export async function getLiffIdToken() {
  await initializeLiff();
  const token = liff.getIDToken();
  if (!token) {
    throw new Error("LINE ID token is missing.");
  }

  return token;
}

export async function closeLiffWindow() {
  await initializeLiff();
  if (liff.isInClient()) {
    liff.closeWindow();
  }
}

export async function isLiffInClient() {
  await initializeLiff();
  return liff.isInClient();
}
