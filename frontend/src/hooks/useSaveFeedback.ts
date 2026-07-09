import { useCallback, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { extractApiErrorMessage } from "../utils/apiError";
import { useNotification } from "./useNotification";

type SuccessRedirectOptions = {
  successMessage: string;
  redirectTo?: string;
  delayMs?: number;
};

const DEFAULT_REDIRECT_DELAY_MS = 3000;

export function useSaveFeedback() {
  const navigate = useNavigate();
  const { showError, showSuccess } = useNotification();
  const redirectTimerRef = useRef<number | null>(null);

  useEffect(() => {
    return () => {
      if (redirectTimerRef.current !== null) {
        window.clearTimeout(redirectTimerRef.current);
      }
    };
  }, []);

  const showSuccessAndRedirect = useCallback(
    ({ successMessage, redirectTo, delayMs = DEFAULT_REDIRECT_DELAY_MS }: SuccessRedirectOptions) => {
      showSuccess(successMessage);

      if (!redirectTo) {
        return;
      }

      if (redirectTimerRef.current !== null) {
        window.clearTimeout(redirectTimerRef.current);
      }

      redirectTimerRef.current = window.setTimeout(() => {
        navigate(redirectTo);
      }, delayMs);
    },
    [navigate, showSuccess],
  );

  const showSaveError = useCallback(
    (error: unknown, fallbackMessage = "ไม่สามารถบันทึกข้อมูลได้ กรุณาตรวจสอบข้อมูลอีกครั้ง") => {
      showError(extractApiErrorMessage(error, fallbackMessage));
    },
    [showError],
  );

  return {
    showSaveError,
    showSuccessAndRedirect,
  };
}
