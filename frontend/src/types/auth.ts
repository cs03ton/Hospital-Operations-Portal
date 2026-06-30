export type AuthUser = {
  id: string;
  fullname: string;
  username: string;
  role: string;
  department?: string | null;
  profileImageUrl?: string | null;
  permissions: string[];
};

export type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
};

export type ApiResponse<T> = {
  success: boolean;
  message: string;
  data: T;
};

export const authStorageKeys = {
  accessToken: "hop.accessToken",
  refreshToken: "hop.refreshToken",
  user: "hop.user",
};
