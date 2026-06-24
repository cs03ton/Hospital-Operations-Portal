import { httpClient } from "./httpClient";
import type { ApiResponse } from "../types/auth";

export type UserProfile = {
  id: string;
  employeeCode?: string | null;
  fullname: string;
  username: string;
  position?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  leaveContactAddress?: string | null;
  profileImageUrl?: string | null;
  roles: string[];
  departmentId?: string | null;
  departmentName?: string | null;
  leaveApprovalRuleId?: string | null;
  leaveApprovalRuleName?: string | null;
  lineUserId?: string | null;
  isActive: boolean;
  permissions: string[];
};

export type UpdateUserProfileRequest = {
  fullname: string;
  position?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  leaveContactAddress?: string | null;
  profileImageUrl?: string | null;
};

export async function getMyProfile() {
  const response = await httpClient.get<ApiResponse<UserProfile>>("/api/me/profile");
  return response.data.data;
}

export async function updateMyProfile(payload: UpdateUserProfileRequest) {
  const response = await httpClient.put<ApiResponse<UserProfile>>("/api/me/profile", payload);
  return response.data.data;
}
