import { getMyNotifications, type LeaveNotificationItem } from "../api/leaveApi";

export type NotificationItem = {
  id: string;
  title: string;
  message: string;
  createdAt: string;
  unread: boolean;
  path?: string;
};

export async function getNotificationItems(): Promise<NotificationItem[]> {
  try {
    const notifications = await getMyNotifications();
    return notifications.map(toNotificationItem);
  } catch {
    return [
      {
        id: "notification-placeholder",
        title: "ระบบแจ้งเตือน",
        message: "ยังไม่สามารถโหลดงานรออนุมัติได้",
        createdAt: new Date().toISOString(),
        unread: false,
      },
    ];
  }
}

function toNotificationItem(item: LeaveNotificationItem): NotificationItem {
  return {
    id: item.id,
    title: item.title,
    message: item.message,
    createdAt: item.createdAt,
    unread: item.unread,
    path: item.path,
  };
}
