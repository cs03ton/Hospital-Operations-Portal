export function extractApiErrorMessage(error: unknown, fallback: string) {
  const maybeError = error as {
    response?: {
      data?: {
        message?: string;
        errors?: string[] | Record<string, string[]>;
        data?: {
          message?: string;
        };
      };
    };
    message?: string;
  };

  const payload = maybeError.response?.data;
  if (payload?.data?.message) {
    return payload.data.message;
  }

  if (payload?.message) {
    return payload.message;
  }

  if (Array.isArray(payload?.errors) && payload.errors.length > 0) {
    return payload.errors.join(", ");
  }

  if (payload?.errors && typeof payload.errors === "object") {
    const first = Object.values(payload.errors).flat()[0];
    if (first) {
      return first;
    }
  }

  return maybeError.message || fallback;
}
