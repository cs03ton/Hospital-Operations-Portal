import { useEffect, useState } from "react";
import { httpClient } from "../api/httpClient";

export function useAuthenticatedMediaUrl(sourceUrl?: string | null) {
  const [objectUrl, setObjectUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isError, setIsError] = useState(false);

  useEffect(() => {
    let disposed = false;
    let nextObjectUrl: string | null = null;

    setObjectUrl(null);
    setIsError(false);

    if (!sourceUrl) {
      setIsLoading(false);
      return () => undefined;
    }

    setIsLoading(true);
    const requestUrl = normalizeMediaUrl(sourceUrl);
    httpClient
      .get<Blob>(requestUrl, { responseType: "blob" })
      .then((response) => {
        if (disposed) return;
        nextObjectUrl = URL.createObjectURL(response.data);
        setObjectUrl(nextObjectUrl);
      })
      .catch(() => {
        if (!disposed) {
          setIsError(true);
        }
      })
      .finally(() => {
        if (!disposed) {
          setIsLoading(false);
        }
      });

    return () => {
      disposed = true;
      if (nextObjectUrl) {
        URL.revokeObjectURL(nextObjectUrl);
      }
    };
  }, [sourceUrl]);

  return { mediaUrl: objectUrl, isLoading, isError };
}

function normalizeMediaUrl(sourceUrl: string) {
  if (!sourceUrl.startsWith("http")) {
    return sourceUrl;
  }

  try {
    const url = new URL(sourceUrl);
    if (url.origin === window.location.origin) {
      return `${url.pathname}${url.search}${url.hash}`;
    }
  } catch {
    return sourceUrl;
  }

  return sourceUrl;
}
