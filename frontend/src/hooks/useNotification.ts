import { useNotificationContext } from "../contexts/NotificationContext";

export function useNotification() {
  return useNotificationContext();
}
