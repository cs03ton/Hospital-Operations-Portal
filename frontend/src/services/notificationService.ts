import {
  getMyNotifications,
  getNotificationBadgeCount,
  getNotificationCenter,
  markNotificationAsRead,
  type LeaveNotificationItem,
  type NotificationCenterQuery,
} from "../api/leaveApi";

export type NotificationItem = {
  id: string;
  type: string;
  title: string;
  message: string;
  createdAt: string;
  unread: boolean;
  category: string;
  priority: string;
  notificationType: string;
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
        type: "Info",
        title: "ระบบแจ้งเตือน",
        message: "ยังไม่สามารถโหลดงานรออนุมัติได้",
        createdAt: new Date().toISOString(),
        unread: false,
        category: "Notification",
        priority: "Information",
        notificationType: "Information",
      },
    ];
  }
}

export async function getNotificationCenterItems(params: NotificationCenterQuery = {}) {
  return getNotificationCenter(params);
}

export async function getNotificationBadge() {
  return getNotificationBadgeCount();
}

export async function markNotificationRead(id: string) {
  return markNotificationAsRead(id);
}

function toNotificationItem(item: LeaveNotificationItem): NotificationItem {
  return {
    id: item.id,
    type: item.type,
    title: item.title,
    message: item.message,
    createdAt: item.createdAt,
    unread: item.unread,
    category: item.category,
    priority: item.priority,
    notificationType: item.notificationType,
    path: item.path,
  };
}
