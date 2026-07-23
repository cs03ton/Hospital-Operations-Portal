import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type AnnouncementCategory = {
  id: string;
  name: string;
  description?: string | null;
  color?: string | null;
  isActive: boolean;
  displayOrder: number;
};

export type AnnouncementTarget = {
  id?: string;
  targetType: string;
  targetValue?: string | null;
};

export type AnnouncementSummary = {
  id: string;
  title: string;
  summary: string;
  status: string;
  priority: string;
  category?: AnnouncementCategory | null;
  isFeatured: boolean;
  showAsPopup: boolean;
  showAsBanner: boolean;
  requiresAcknowledgement: boolean;
  isRead: boolean;
  isAcknowledged: boolean;
  publishAt?: string | null;
  expiresAt?: string | null;
  createdAt: string;
  publishedAt?: string | null;
  createdByName?: string | null;
  coverImage?: AnnouncementImage | null;
  legacyCoverImageUrl?: string | null;
  tags?: string | null;
  viewCount: number;
  acknowledgedCount: number;
  notifyInApp: boolean;
  notifyViaLine: boolean;
  notificationDispatchStatus?: string | null;
  inAppRecipientCount: number;
  lineEligibleRecipientCount: number;
  lineQueuedCount: number;
  lineSentCount: number;
  lineFailedCount: number;
};

export type AnnouncementDetail = AnnouncementSummary & {
  body: string;
  targets: AnnouncementTarget[];
  files: AnnouncementFile[];
  images: AnnouncementImage[];
  readAt?: string | null;
  acknowledgedAt?: string | null;
  updatedAt?: string | null;
  publishedByName?: string | null;
  notificationDispatchError?: string | null;
  notificationSentAt?: string | null;
  lineNotificationQueuedAt?: string | null;
};

export type AnnouncementFile = {
  id: string;
  fileName: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  fileRole: string;
  downloadUrl: string;
};

export type AnnouncementImage = {
  id: string;
  thumbnailUrl: string;
  mediumUrl: string;
  largeUrl: string;
  originalUrl: string;
  displayOrder: number;
  isCover: boolean;
  width?: number | null;
  height?: number | null;
  fileSize: number;
};

export type AnnouncementPayload = {
  title: string;
  summary?: string;
  body: string;
  priority: string;
  categoryId?: string | null;
  publishAt?: string | null;
  expiresAt?: string | null;
  isFeatured: boolean;
  showAsPopup: boolean;
  showAsBanner: boolean;
  requiresAcknowledgement: boolean;
  tags?: string | null;
  notifyInApp?: boolean;
  notifyViaLine?: boolean;
  targets?: Array<{ targetType: string; targetValue?: string | null }>;
};

export type AnnouncementNotificationPreview = {
  announcementId: string;
  notifyInApp: boolean;
  notifyViaLine: boolean;
  totalTargetUsers: number;
  inAppRecipientCount: number;
  lineBoundRecipientCount: number;
  lineUnboundRecipientCount: number;
  inactiveUserCount: number;
  estimatedQueueItems: number;
  warnings: string[];
};

export type AnnouncementQuery = {
  page?: number;
  pageSize?: number;
  status?: string;
  priority?: string;
  categoryId?: string;
  search?: string;
};

export async function getAnnouncementFeed(params?: AnnouncementQuery) {
  const response = await httpClient.get<ApiResponse<PagedResponse<AnnouncementSummary>>>("/api/announcements/feed", { params });
  return response.data.data;
}

export async function getFeaturedAnnouncements() {
  const response = await httpClient.get<ApiResponse<AnnouncementSummary[]>>("/api/announcements/featured");
  return response.data.data;
}

export async function getPopupAnnouncements() {
  const response = await httpClient.get<ApiResponse<AnnouncementSummary[]>>("/api/announcements/popup");
  return response.data.data;
}

export async function getAnnouncementDetail(id: string) {
  const response = await httpClient.get<ApiResponse<AnnouncementDetail>>(`/api/announcements/${id}`);
  return response.data.data;
}

export async function acknowledgeAnnouncement(id: string) {
  const response = await httpClient.post<ApiResponse<{ announcementId: string; isRead: boolean; isAcknowledged: boolean }>>(`/api/announcements/${id}/acknowledge`);
  return response.data.data;
}

export async function getAdminAnnouncements(params?: AnnouncementQuery) {
  const response = await httpClient.get<ApiResponse<PagedResponse<AnnouncementSummary>>>("/api/admin/announcements", { params });
  return response.data.data;
}

export async function getAdminAnnouncementDetail(id: string) {
  const response = await httpClient.get<ApiResponse<AnnouncementDetail>>(`/api/admin/announcements/${id}`);
  return response.data.data;
}

export async function getAnnouncementCategories() {
  const response = await httpClient.get<ApiResponse<AnnouncementCategory[]>>("/api/admin/announcements/categories");
  return response.data.data;
}

export async function createAnnouncement(payload: AnnouncementPayload) {
  const response = await httpClient.post<ApiResponse<AnnouncementDetail>>("/api/admin/announcements", payload);
  return response.data.data;
}

export async function updateAnnouncement(id: string, payload: AnnouncementPayload) {
  const response = await httpClient.put<ApiResponse<AnnouncementDetail>>(`/api/admin/announcements/${id}`, payload);
  return response.data.data;
}

export async function publishAnnouncement(id: string) {
  const response = await httpClient.post<ApiResponse<AnnouncementDetail>>(`/api/admin/announcements/${id}/publish`);
  return response.data.data;
}

export async function previewAnnouncementNotification(id: string, payload?: { notifyInApp?: boolean; notifyViaLine?: boolean }) {
  const response = await httpClient.post<ApiResponse<AnnouncementNotificationPreview>>(`/api/admin/announcements/${id}/notification-preview`, payload ?? {});
  return response.data.data;
}

export async function unpublishAnnouncement(id: string) {
  const response = await httpClient.post<ApiResponse<AnnouncementDetail>>(`/api/admin/announcements/${id}/unpublish`);
  return response.data.data;
}

export async function archiveAnnouncement(id: string) {
  const response = await httpClient.post<ApiResponse<AnnouncementDetail>>(`/api/admin/announcements/${id}/archive`);
  return response.data.data;
}

export async function cancelAnnouncement(id: string) {
  const response = await httpClient.post<ApiResponse<AnnouncementDetail>>(`/api/admin/announcements/${id}/cancel`);
  return response.data.data;
}

export async function duplicateAnnouncement(id: string) {
  const response = await httpClient.post<ApiResponse<AnnouncementDetail>>(`/api/admin/announcements/${id}/duplicate`);
  return response.data.data;
}

export async function deleteAnnouncement(id: string) {
  const response = await httpClient.delete<ApiResponse<string>>(`/api/admin/announcements/${id}`);
  return response.data.data;
}

export async function uploadAnnouncementImage(id: string, file: File, isCover: boolean, displayOrder?: number) {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("isCover", String(isCover));
  if (displayOrder !== undefined) {
    formData.append("displayOrder", String(displayOrder));
  }
  const response = await httpClient.post<ApiResponse<AnnouncementImage>>(`/api/admin/announcements/${id}/images`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return response.data.data;
}

export async function getAdminAnnouncementImages(id: string) {
  const response = await httpClient.get<ApiResponse<AnnouncementImage[]>>(`/api/admin/announcements/${id}/images`);
  return response.data.data;
}

export async function reorderAnnouncementImages(id: string, items: Array<{ imageId: string; displayOrder: number }>) {
  const response = await httpClient.put<ApiResponse<AnnouncementImage[]>>(`/api/admin/announcements/${id}/images/order`, { items });
  return response.data.data;
}

export async function deleteAnnouncementImage(imageId: string) {
  const response = await httpClient.delete<ApiResponse<string>>(`/api/admin/announcements/images/${imageId}`);
  return response.data.data;
}

export async function uploadAnnouncementAttachments(id: string, files: File[]) {
  const formData = new FormData();
  files.forEach((file) => formData.append("files", file));
  const response = await httpClient.post<ApiResponse<AnnouncementFile[]>>(`/api/admin/announcements/${id}/attachments`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return response.data.data;
}

export async function deleteAnnouncementFile(fileId: string) {
  const response = await httpClient.delete<ApiResponse<string>>(`/api/admin/announcements/files/${fileId}`);
  return response.data.data;
}
