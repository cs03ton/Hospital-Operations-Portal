import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type DashboardActionItem = {
  id: string;
  module: "leave" | "fleet" | "repair" | "meeting";
  title: string;
  detail: string;
  path: string;
  createdAt: string;
};

export type DashboardActionItems = {
  leaveCount: number;
  fleetCount: number;
  repairCount: number;
  meetingTodayCount: number;
  items: DashboardActionItem[];
  meetingBookings: DashboardActionItem[];
};

export async function getDashboardActionItems(): Promise<DashboardActionItems> {
  const response = await httpClient.get<ApiResponse<DashboardActionItems>>("/api/dashboard/action-items");
  return response.data.data;
}
