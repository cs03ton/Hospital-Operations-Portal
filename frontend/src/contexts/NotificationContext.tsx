/* eslint-disable react-refresh/only-export-components */
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
  const [queue, setQueue] = useState<AppNotification[]>([]);
  const [open, setOpen] = useState(false);

  const show = useCallback((type: NotificationType, message: string) => {
    const notification = {
      id: createNotificationId(),
      type,
      message,
      createdAt: new Date().toISOString(),
    };
    setHistory((items) => [notification, ...items].slice(0, 50));
    setQueue((items) => [...items, notification]);
  }, []);

  useEffect(() => {
    if (!open && !current && queue.length > 0) {
      const [next, ...remaining] = queue;
      setCurrent(next);
      setQueue(remaining);
      setOpen(true);
    }
  }, [current, open, queue]);

  const closeCurrent = useCallback(() => {
    setOpen(false);
    window.setTimeout(() => setCurrent(null), 150);
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
      <AppSnackbar notification={current} open={open} onClose={closeCurrent} />
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

function createNotificationId() {
  if ("crypto" in window && typeof window.crypto.randomUUID === "function") {
    return window.crypto.randomUUID();
  }

  return `notification-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}
