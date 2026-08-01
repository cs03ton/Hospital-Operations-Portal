const fallbackReturnUrl = "/dashboard";

export function sanitizeInternalReturnUrl(value: string | null | undefined) {
  if (!value) {
    return fallbackReturnUrl;
  }

  let decoded = value.trim();
  for (let index = 0; index < 2; index += 1) {
    try {
      decoded = decodeURIComponent(decoded);
    } catch {
      break;
    }
  }

  const lower = decoded.toLowerCase();
  if (
    !decoded.startsWith("/") ||
    decoded.startsWith("//") ||
    decoded.startsWith("/\\") ||
    lower.includes("://") ||
    lower.startsWith("/javascript:") ||
    lower.startsWith("/data:") ||
    lower.startsWith("/http:") ||
    lower.startsWith("/https:")
  ) {
    return fallbackReturnUrl;
  }

  const allowedPrefixes = [
    "/dashboard",
    "/leave",
    "/line/leave-approval",
    "/reports",
    "/notifications",
    "/announcements",
    "/profile",
    "/docs",
  ];

  return allowedPrefixes.some((prefix) => decoded === prefix || decoded.startsWith(`${prefix}/`) || decoded.startsWith(`${prefix}?`))
    ? decoded
    : fallbackReturnUrl;
}

export function buildLiffDeepLink(liffId: string | null | undefined, returnUrl: string) {
  if (!liffId) {
    return sanitizeInternalReturnUrl(returnUrl);
  }

  const baseUrl = normalizeLiffBaseUrl(import.meta.env.VITE_LIFF_BASE_URL);
  return `${baseUrl}/${encodeURIComponent(liffId)}?returnUrl=${encodeURIComponent(sanitizeInternalReturnUrl(returnUrl))}`;
}

function normalizeLiffBaseUrl(value: string | null | undefined) {
  const fallback = "https://miniapp.line.me";
  if (!value) {
    return fallback;
  }

  const trimmed = value.trim().replace(/\/+$/, "");
  if (!/^https:\/\/(miniapp|liff)\.line\.me$/i.test(trimmed)) {
    return fallback;
  }

  return trimmed;
}
