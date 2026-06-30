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
  hasProfileImage: boolean;
  profileImageUpdatedAt?: string | null;
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
};

export type ProfileImageUploadResponse = {
  profileImageUrl: string;
  message: string;
};

export async function getMyProfile() {
  const response = await httpClient.get<ApiResponse<UserProfile>>("/api/me/profile");
  return response.data.data;
}

export async function updateMyProfile(payload: UpdateUserProfileRequest) {
  const response = await httpClient.put<ApiResponse<UserProfile>>("/api/me/profile", payload);
  return response.data.data;
}

export async function uploadMyProfileImage(file: File) {
  const formData = new FormData();
  formData.append("file", file);
  const response = await httpClient.post<ApiResponse<ProfileImageUploadResponse>>("/api/me/profile/image", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return response.data.data;
}

export async function deleteMyProfileImage() {
  await httpClient.delete("/api/me/profile/image");
}
