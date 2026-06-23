import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { AppSnackbar } from "../components/common/AppSnackbar";

export type NotificationType = "success" | "info" | "warning" | "error";

export type AppNotification = {
  id: string;
  type: NotificationType;
  message: string;
  createdAt: string;
};

type NotificationContextValue = {
  history: AppNotification[];
  show: (type: NotificationType, message: string) => void;
  showSuccess: (message: string) => void;
  showInfo: (message: string) => void;
  showWarning: (message: string) => void;
  showError: (message: string) => void;
};

const NotificationContext = createContext<NotificationContextValue | null>(null);
const listeners = new Set<(type: NotificationType, message: string) => void>();

export function notifyGlobal(type: NotificationType, message: string) {
  listeners.forEach((listener) => listener(type, message));
}

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [history, setHistory] = useState<AppNotification[]>([]);
  const [current, setCurrent] = useState<AppNotification | null>(null);
  const [open, setOpen] = useState(false);

  const show = useCallback((type: NotificationType, message: string) => {
    const notification = {
      id: crypto.randomUUID(),
      type,
      message,
      createdAt: new Date().toISOString(),
    };
    setHistory((items) => [notification, ...items].slice(0, 50));
    setCurrent(notification);
    setOpen(true);
  }, []);

  useEffect(() => {
    listeners.add(show);
    return () => {
      listeners.delete(show);
    };
  }, [show]);

  const value = useMemo<NotificationContextValue>(
    () => ({
      history,
      show,
      showSuccess: (message) => show("success", message),
      showInfo: (message) => show("info", message),
      showWarning: (message) => show("warning", message),
      showError: (message) => show("error", message),
    }),
    [history, show],
  );

  return (
    <NotificationContext.Provider value={value}>
      {children}
      <AppSnackbar notification={current} open={open} onClose={() => setOpen(false)} />
    </NotificationContext.Provider>
  );
}

export function useNotificationContext() {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error("useNotification must be used inside NotificationProvider.");
  }

  return context;
}
