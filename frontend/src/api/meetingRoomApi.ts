import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export const meetingRoomPermissions = {
  calendar: "MeetingRoom.Calendar.View",
  viewOwn: "MeetingRoom.Booking.ViewOwn",
  create: "MeetingRoom.Booking.Create",
  manage: "MeetingRoom.Booking.Manage",
  manageRooms: "MeetingRoom.Room.Manage",
} as const;

export type MeetingRoom = { id: string; code: string; name: string; location: string; capacity: number; isActive: boolean; concurrencyToken: string };
export type MeetingBooking = { id: string; number: number; roomId: string; roomName: string; bookerId: string; bookerName: string; departmentId?: string; departmentName?: string; subject: string; purpose: string; startAt: string; endAt: string; attendeeCount: number; meetingLink?: string; additionalRequest?: string; status: "Confirmed" | "Cancelled"; cancellationReason?: string; concurrencyToken: string; createdAt: string; updatedAt: string };
export type BookingInput = { roomId: string; date: string; startTime: string; endTime: string; subject: string; purpose: string; attendeeCount: number; meetingLink?: string; additionalRequest?: string };

const data = <T>(response: { data: ApiResponse<T> }) => response.data.data;
export async function getMeetingRooms(includeInactive = false) { return data(await httpClient.get<ApiResponse<MeetingRoom[]>>("/api/meeting-rooms/rooms", { params: { includeInactive } })); }
export async function getMeetingCalendar(start: string, end: string, roomId?: string) { return data(await httpClient.get<ApiResponse<MeetingBooking[]>>("/api/meeting-rooms/calendar", { params: { start, end, roomId: roomId || undefined } })); }
export async function getMeetingBookings(params: { scope?: string; search?: string; status?: string; page?: number; pageSize?: number }) { return data(await httpClient.get<ApiResponse<{ items: MeetingBooking[]; total: number; page: number; pageSize: number }>>("/api/meeting-rooms/bookings", { params })); }
export async function getMeetingBooking(id: string) { return data(await httpClient.get<ApiResponse<{ booking: MeetingBooking; history: Array<{ id: string; action: string; detail?: string; createdAt: string; actorName: string }>; attachments: Array<{ id: string; originalFileName: string; fileSize: number }> }>>(`/api/meeting-rooms/bookings/${id}`)); }
export async function createMeetingBooking(input: BookingInput) { return data(await httpClient.post<ApiResponse<MeetingBooking>>("/api/meeting-rooms/bookings", input)); }
export async function updateMeetingBooking(id: string, input: BookingInput & { concurrencyToken: string }) { return data(await httpClient.put<ApiResponse<MeetingBooking>>(`/api/meeting-rooms/bookings/${id}`, input)); }
export async function cancelMeetingBooking(id: string, concurrencyToken: string, reason: string) { return data(await httpClient.post<ApiResponse<MeetingBooking>>(`/api/meeting-rooms/bookings/${id}/cancel`, { concurrencyToken, reason })); }
export async function saveMeetingRoom(input: Partial<MeetingRoom> & Pick<MeetingRoom, "code" | "name" | "location" | "capacity" | "isActive">) { return data(await httpClient.post<ApiResponse<MeetingRoom>>("/api/meeting-rooms/rooms", input)); }
export async function uploadMeetingAttachments(id: string, files: File[]) { const body = new FormData(); files.forEach((file) => body.append("files", file)); return httpClient.post(`/api/meeting-rooms/bookings/${id}/attachments`, body); }
export function meetingAttachmentUrl(id: string) { return `/api/meeting-rooms/attachments/${id}`; }
